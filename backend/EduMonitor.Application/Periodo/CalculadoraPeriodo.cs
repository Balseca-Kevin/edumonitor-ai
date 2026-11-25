using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EduMonitor.Application.Indicadores;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Application.Periodo;

/// <summary>
/// TA-22.A4 · Estado del período en código determinista (docs/contratos-api.md, «Estado del período»): semana actual y total,
/// unidad actual, cada actividad con su semana y su estado, y los bloques vencidas, vence esta semana y siguiente. Las fechas se
/// llevan a Ecuador (America/Guayaquil) antes de contar semanas; el frontend no calcula fechas.
/// </summary>
public static partial class CalculadoraPeriodo
{
    public const string Vencida = "VENCIDA";
    public const string VenceEstaSemana = "VENCE_ESTA_SEMANA";
    public const string Proxima = "PROXIMA";
    public const string SinFecha = "SIN_FECHA";
    public const string NoDisponibleInicio = "inicio";

    /// <summary>Ecuador no cambia de hora: si el sistema no tiene la zona, se usa −05:00.</summary>
    public static readonly TimeZoneInfo Zona = BuscarZona();

    public static PeriodoDto Calcular(
        int cursoId,
        DatosCurso datos,
        DateTimeOffset? inicioCurso,
        DateTimeOffset? finCurso,
        DateTimeOffset ahora,
        IReadOnlyDictionary<int, IReadOnlyList<string>> temasPorActividad)
    {
        var hoy = EnEcuador(ahora);
        var actividades = CalculadoraIndicadores.OrdenarActividades(datos.Actividades, datos.Unidades);
        var unidades = datos.Unidades.ToDictionary(u => u.Id);
        var ultimaFecha = actividades.Where(a => a.FechaLimite is not null).Select(a => a.FechaLimite!.Value).DefaultIfEmpty().Max();
        DateTimeOffset? fin = finCurso ?? (ultimaFecha == default ? null : ultimaFecha);
        var finOrigen = finCurso is not null ? "MOODLE" : "ULTIMA_FECHA_LIMITE";

        int? Semana(DateTimeOffset? fecha) => inicioCurso is null || fecha is null ? null : NumeroSemana(inicioCurso.Value, fecha.Value);
        var total = Semana(fin);
        var actualSinAcotar = Semana(ahora);
        int? actual = actualSinAcotar is null || total is null ? null : Math.Clamp(actualSinAcotar.Value, 1, Math.Max(total.Value, 1));
        string? estado = inicioCurso is null ? null
            : ahora < inicioCurso ? "ANTES"
            : fin is not null && ahora > fin ? "TERMINADO"
            : "EN_CURSO";

        var porActividad = datos.Calificaciones.GroupBy(c => c.ActividadId).ToDictionary(g => g.Key, g => g.ToList());
        var matriculados = datos.Estudiantes.Count;

        var filas = actividades.Select(a =>
        {
            var semana = Semana(a.FechaLimite);
            var estadoActividad = a.FechaLimite is null ? SinFecha
                : a.FechaLimite <= ahora ? Vencida
                : semana is not null && semana == actualSinAcotar ? VenceEstaSemana
                : inicioCurso is null && EnEcuador(a.FechaLimite.Value).Date <= FinDeSemana(hoy) ? VenceEstaSemana
                : Proxima;
            var notas = porActividad.GetValueOrDefault(a.Id) ?? new List<Calificacion>();
            var entregaron = notas.Count(c => c.Estado is EstadoCalificacion.CALIFICADO or EstadoCalificacion.PENDIENTE);
            var faltan = estadoActividad == Vencida ? notas.Count(c => c.Estado == EstadoCalificacion.SIN_ENTREGA) : 0;
            var unidad = a.UnidadId is int id && unidades.TryGetValue(id, out var u) ? u.Numero : (int?)null;
            return new ActividadPeriodoDto(a.Id, a.Nombre, Sigla(a.Nombre), a.Categoria, unidad, a.FechaLimite, semana, estadoActividad, matriculados, entregaron, faltan);
        }).ToList();

        // Unidad actual: la de la siguiente actividad que no ha vencido; si todas vencieron, la última unidad con actividades.
        var conUnidad = filas.Where(f => f.Unidad is not null).ToList();
        var numeroActual = conUnidad.FirstOrDefault(f => f.Estado is VenceEstaSemana or Proxima)?.Unidad ?? conUnidad.LastOrDefault()?.Unidad;
        var unidadActual = datos.Unidades.FirstOrDefault(u => u.Numero == numeroActual) is { } ua ? new UnidadActualDto(ua.Numero, ua.Nombre, ua.Parcial) : null;

        var listaUnidades = new List<UnidadPeriodoDto>();
        int? hastaAnterior = 0;
        foreach (var u in datos.Unidades.OrderBy(u => u.Numero))
        {
            var semanas = filas.Where(f => f.Unidad == u.Numero && f.Semana is not null).Select(f => f.Semana!.Value).ToList();
            if (semanas.Count == 0)
            {
                listaUnidades.Add(new UnidadPeriodoDto(u.Numero, u.Nombre, u.Parcial, null, null));
                continue;
            }

            var hasta = semanas.Max();
            var desde = Math.Min((hastaAnterior ?? 0) + 1, hasta);
            listaUnidades.Add(new UnidadPeriodoDto(u.Numero, u.Nombre, u.Parcial, desde, hasta));
            hastaAnterior = hasta;
        }

        var vencidas = filas.Where(f => f.Estado == Vencida).ToList();
        var bloqueVencidas = new VencidasDto(
            vencidas.Count,
            vencidas.Sum(f => f.Faltan),
            vencidas.Where(f => f.Faltan > 0).Select(f => new ActividadConFaltasDto(f.Id, f.Nombre, f.Faltan)).ToList());
        var estaSemana = filas.Where(f => f.Estado == VenceEstaSemana)
            .Select(f => new VenceEstaSemanaDto(f.Id, f.Nombre, f.FechaLimite!.Value, f.Entregaron, f.Matriculados))
            .ToList();
        var siguiente = filas.FirstOrDefault(f => f.Estado == Proxima) is { } s
            ? new SiguienteDto(s.Id, s.Nombre, s.Categoria, s.Semana, s.FechaLimite!.Value, temasPorActividad.GetValueOrDefault(s.Id) ?? Array.Empty<string>())
            : null;

        return new PeriodoDto(
            cursoId,
            hoy,
            inicioCurso is null ? null : EnEcuador(inicioCurso.Value),
            fin is null ? null : EnEcuador(fin.Value),
            finOrigen,
            estado,
            actual is null || total is null ? null : new SemanaDto(actual.Value, total.Value),
            unidadActual,
            listaUnidades,
            filas.Select(f => f with { FechaLimite = f.FechaLimite is null ? null : EnEcuador(f.FechaLimite.Value) }).ToList(),
            bloqueVencidas,
            estaSemana.Select(v => v with { FechaLimite = EnEcuador(v.FechaLimite) }).ToList(),
            siguiente is null ? null : siguiente with { FechaLimite = EnEcuador(siguiente.FechaLimite) },
            inicioCurso is null ? new[] { NoDisponibleInicio } : Array.Empty<string>());
    }

    /// <summary>Semana n del período: del día <c>inicio + 7·(n−1)</c> al día anterior a <c>inicio + 7·n</c>, en fechas de Ecuador.</summary>
    public static int NumeroSemana(DateTimeOffset inicio, DateTimeOffset fecha)
    {
        var dias = (EnEcuador(fecha).Date - EnEcuador(inicio).Date).Days;
        return (int)Math.Floor(dias / 7.0) + 1;
    }

    public static DateTimeOffset EnEcuador(DateTimeOffset fecha) => TimeZoneInfo.ConvertTime(fecha, Zona);

    /// <summary>Sigla de la línea de tiempo: Tarea n → Tn, Taller n → TLn, APE n → APEn, Examen Parcial n → EX Pn; si no, las iniciales.</summary>
    public static string Sigla(string nombre)
    {
        var limpio = Regex.Replace(nombre.Trim(), @"\s+", " ");
        var numero = Numero().Match(limpio).Value;
        var clave = SinTildes(limpio).ToUpperInvariant();
        if (clave.StartsWith("EXAMEN PARCIAL ") || clave.StartsWith("EXAMEN P")) return $"EX P{numero}".TrimEnd();
        if (clave.StartsWith("TALLER ")) return $"TL{numero}";
        if (clave.StartsWith("TAREA ")) return $"T{numero}";
        if (clave.StartsWith("APE")) return $"APE{numero}";
        var iniciales = string.Concat(limpio.Split(' ').Where(p => p.Length > 0 && char.IsLetter(p[0])).Select(p => char.ToUpperInvariant(p[0])));
        return (iniciales.Length > 3 ? iniciales[..3] : iniciales) + numero;
    }

    private static DateTime FinDeSemana(DateTimeOffset hoy) => hoy.Date.AddDays(6);

    private static string SinTildes(string texto)
    {
        var sb = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static TimeZoneInfo BuscarZona()
    {
        foreach (var id in new[] { "America/Guayaquil", "SA Pacific Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Ecuador", TimeSpan.FromHours(-5), "Ecuador", "Ecuador");
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex Numero();
}
