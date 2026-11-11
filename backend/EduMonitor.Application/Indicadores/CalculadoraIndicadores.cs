using System.Globalization;
using System.Text;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Application.Indicadores;

/// <summary>Datos guardados de un curso con los que se calculan los indicadores. Solo usa claves (UnidadId, ActividadId, EstudianteId), no navegaciones.</summary>
public sealed record DatosCurso(
    IReadOnlyList<Unidad> Unidades,
    IReadOnlyList<Actividad> Actividades,
    IReadOnlyList<Estudiante> Estudiantes,
    IReadOnlyList<Calificacion> Calificaciones,
    bool FinalizacionDisponible = true);

/// <summary>Nota calificada de un estudiante, ya en escala 0 a 10, con su actividad.</summary>
public sealed record NotaCalculo(Actividad Actividad, decimal Nota);

/// <summary>
/// TA-09.1 · Indicadores de la planificación (tabla 5), en código determinista y sin acceso a la base:
/// promedio ponderado UTA 40/30/30 con normalización, media, mediana, desviación muestral, grupos, z,
/// tasa de finalización, tendencia con N = 3, distribución y resumen por unidad.
/// Solo las notas CALIFICADO (incluida la 0) entran a los promedios; SIN_ENTREGA y PENDIENTE no son 0.
/// </summary>
public static class CalculadoraIndicadores
{
    public const int Escala = 10;
    public const decimal NotaAprobacion = 7.0m;
    public const int NTendencia = 3;
    public const decimal UmbralAlto = 8.5m;
    public const decimal UmbralMedio = 6.0m;

    public const string GrupoAlto = "ALTO";
    public const string GrupoMedio = "MEDIO";
    public const string GrupoBajo = "BAJO";

    /// <summary>Ponderación UTA por categoría del libro de calificaciones (nombre sin tildes y en mayúsculas).</summary>
    public static readonly IReadOnlyDictionary<string, decimal> PesosCategoria = new Dictionary<string, decimal>
    {
        ["EXAMENES"] = 0.40m,
        ["TAREAS"] = 0.30m,
        ["APE"] = 0.30m,
    };

    /// <summary>Indicadores de todos los estudiantes, del curso, la distribución, los grupos y las unidades.</summary>
    public static IndicadoresCalculados Calcular(DatosCurso datos)
    {
        var actividades = OrdenarActividades(datos.Actividades, datos.Unidades);
        var porEstudiante = datos.Calificaciones.ToLookup(c => c.EstudianteId);

        var parciales = datos.Estudiantes
            .Select(e => CalcularEstudiante(e, actividades, porEstudiante[e.Id].ToList(), datos.FinalizacionDisponible))
            .ToList();

        var promedios = parciales.Where(p => p.Promedio is not null).Select(p => p.Promedio!.Value).ToList();
        var media = Media(promedios);
        var desviacion = DesviacionMuestral(promedios);

        var estudiantes = parciales.Select(p => new IndicadoresEstudianteDto(
                p.Estudiante.Id,
                p.Estudiante.Nombre,
                p.Estudiante.Apellido,
                Redondear(p.Promedio),
                p.Parcial,
                Redondear(PuntuacionZ(p.Promedio, media, desviacion)),
                Grupo(Redondear(p.Promedio)),
                p.Notas,
                Redondear(p.TasaFinalizacion),
                Redondear(p.Tendencia)))
            .ToList();

        var calificadas = datos.Calificaciones.Where(EsCalificada).ToList();
        var actividadesConNota = calificadas.Select(c => c.ActividadId).ToHashSet();

        var curso = new ResumenIndicadoresCursoDto(
            datos.Estudiantes.Count,
            actividades.Count(a => actividadesConNota.Contains(a.Id)),
            Redondear(media),
            Redondear(Mediana(promedios)),
            Redondear(desviacion),
            Redondear(TasaFinalizacion(datos.Calificaciones, datos.FinalizacionDisponible)),
            Redondear(TendenciaCurso(actividades, calificadas)));

        var unidades = datos.Unidades
            .OrderBy(u => u.Numero)
            .Select(u =>
            {
                var deLaUnidad = actividades.Where(a => a.UnidadId == u.Id).ToList();
                return new UnidadIndicadoresDto(u.Numero, u.Nombre, u.Parcial, deLaUnidad.Count, deLaUnidad.Count(a => actividadesConNota.Contains(a.Id)));
            })
            .ToList();

        var redondeados = estudiantes.Where(e => e.PromedioPonderado is not null).Select(e => e.PromedioPonderado!.Value).ToList();
        var grupos = new GruposRendimientoDto(
            estudiantes.Count(e => e.Grupo == GrupoAlto),
            estudiantes.Count(e => e.Grupo == GrupoMedio),
            estudiantes.Count(e => e.Grupo == GrupoBajo));

        return new IndicadoresCalculados(unidades, curso, Distribucion(redondeados), grupos, estudiantes);
    }

    /// <summary>Notas calificadas de un estudiante en escala 0 a 10, en el orden del curso (unidad, fecha límite, ítem de Moodle).</summary>
    public static IReadOnlyList<NotaCalculo> NotasOrdenadas(DatosCurso datos, int estudianteId)
    {
        var actividades = OrdenarActividades(datos.Actividades, datos.Unidades);
        var calificaciones = datos.Calificaciones.Where(c => c.EstudianteId == estudianteId).ToList();
        return NotasOrdenadas(actividades, calificaciones);
    }

    /// <summary>Convierte la nota de Moodle a la escala 0 a 10 con la nota máxima de la actividad.</summary>
    public static decimal? NotaEnEscala(decimal? nota, decimal? notaMaxima) =>
        nota is null ? null
        : notaMaxima is > 0 ? nota.Value * Escala / notaMaxima.Value
        : nota.Value;

    public static decimal? Media(IReadOnlyCollection<decimal> valores) =>
        valores.Count == 0 ? null : valores.Sum() / valores.Count;

    public static decimal? Mediana(IReadOnlyCollection<decimal> valores)
    {
        if (valores.Count == 0)
        {
            return null;
        }

        var ordenados = valores.Order().ToList();
        var mitad = ordenados.Count / 2;
        return ordenados.Count % 2 == 1 ? ordenados[mitad] : (ordenados[mitad - 1] + ordenados[mitad]) / 2;
    }

    /// <summary>Desviación estándar muestral (n − 1). Null con menos de 2 valores.</summary>
    public static decimal? DesviacionMuestral(IReadOnlyCollection<decimal> valores)
    {
        if (valores.Count < 2)
        {
            return null;
        }

        var media = valores.Sum() / valores.Count;
        var suma = valores.Sum(v => (v - media) * (v - media));
        return (decimal)Math.Sqrt((double)(suma / (valores.Count - 1)));
    }

    /// <summary>
    /// 0,40 × prom(Exámenes) + 0,30 × prom(Tareas) + 0,30 × prom(APE). Si falta una categoría, los pesos de las presentes
    /// se normalizan y el resultado es parcial. Las notas de categorías fuera de la ponderación no entran.
    /// </summary>
    public static (decimal? Valor, bool Parcial) PromedioPonderado(IEnumerable<(string? Categoria, decimal Nota)> calificadas)
    {
        var porCategoria = calificadas
            .Select(c => (Clave: ClaveCategoria(c.Categoria), c.Nota))
            .Where(c => c.Clave is not null && PesosCategoria.ContainsKey(c.Clave))
            .GroupBy(c => c.Clave!)
            .Select(g => (Peso: PesosCategoria[g.Key], Promedio: g.Average(c => c.Nota)))
            .ToList();

        if (porCategoria.Count == 0)
        {
            return (null, false);
        }

        var pesoPresente = porCategoria.Sum(c => c.Peso);
        var valor = porCategoria.Sum(c => c.Peso * c.Promedio) / pesoPresente;
        return (valor, porCategoria.Count < PesosCategoria.Count);
    }

    /// <summary>Promedio de las últimas <paramref name="n"/> notas menos el promedio de las anteriores. Null si no hay al menos n + 1 notas.</summary>
    public static decimal? Tendencia(IReadOnlyList<decimal> notasOrdenadas, int n = NTendencia)
    {
        if (notasOrdenadas.Count < n + 1)
        {
            return null;
        }

        var ultimas = notasOrdenadas.Skip(notasOrdenadas.Count - n).ToList();
        var anteriores = notasOrdenadas.Take(notasOrdenadas.Count - n).ToList();
        return ultimas.Average() - anteriores.Average();
    }

    /// <summary>Alto ≥ 8,5 · Medio 6,0 a 8,49 · Bajo &lt; 6,0. Null sin promedio.</summary>
    public static string? Grupo(decimal? promedio) =>
        promedio switch
        {
            null => null,
            >= UmbralAlto => GrupoAlto,
            >= UmbralMedio => GrupoMedio,
            _ => GrupoBajo,
        };

    /// <summary>z = (valor − media) / desviación. Null si falta un dato o la desviación es 0.</summary>
    public static decimal? PuntuacionZ(decimal? valor, decimal? media, decimal? desviacion) =>
        valor is null || media is null || desviacion is null or 0 ? null : (valor.Value - media.Value) / desviacion.Value;

    /// <summary>10 tramos de 1 punto: 0–1 … 9–10. El 10 cae en el último tramo.</summary>
    public static IReadOnlyList<TramoDistribucionDto> Distribucion(IEnumerable<decimal> promedios)
    {
        var cantidades = new int[Escala];
        foreach (var promedio in promedios)
        {
            var tramo = (int)Math.Floor(Math.Clamp(promedio, 0, Escala));
            cantidades[Math.Min(tramo, Escala - 1)]++;
        }

        return Enumerable.Range(0, Escala).Select(i => new TramoDistribucionDto(i, i + 1, cantidades[i])).ToList();
    }

    /// <summary>Actividades completadas / actividades con seguimiento. Null si la finalización no está disponible o nada tiene seguimiento.</summary>
    public static decimal? TasaFinalizacion(IEnumerable<Calificacion> calificaciones, bool disponible)
    {
        if (!disponible)
        {
            return null;
        }

        var conSeguimiento = calificaciones.Where(c => c.Finalizada is not null).ToList();
        return conSeguimiento.Count == 0 ? null : (decimal)conSeguimiento.Count(c => c.Finalizada == true) / conSeguimiento.Count;
    }

    public static decimal? Redondear(decimal? valor) =>
        valor is null ? null : Math.Round(valor.Value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Nombre de categoría sin tildes, sin espacios a los lados y en mayúsculas (Exámenes → EXAMENES).</summary>
    public static string? ClaveCategoria(string? categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria))
        {
            return null;
        }

        var sinTildes = new StringBuilder();
        foreach (var caracter in categoria.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                sinTildes.Append(caracter);
            }
        }

        return sinTildes.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    private static bool EsCalificada(Calificacion c) => c.Estado == EstadoCalificacion.CALIFICADO && c.Nota is not null;

    private sealed record IndicadoresParciales(
        Estudiante Estudiante, decimal? Promedio, bool Parcial, ConteoNotasDto Notas, decimal? TasaFinalizacion, decimal? Tendencia);

    private static IndicadoresParciales CalcularEstudiante(
        Estudiante estudiante, IReadOnlyList<Actividad> actividades, List<Calificacion> calificaciones, bool finalizacionDisponible)
    {
        var notas = NotasOrdenadas(actividades, calificaciones);
        var (promedio, parcial) = PromedioPonderado(notas.Select(n => (n.Actividad.Categoria, n.Nota)));
        var conteo = new ConteoNotasDto(
            notas.Count,
            calificaciones.Count(c => EsCalificada(c) && c.Nota == 0),
            calificaciones.Count(c => c.Estado == EstadoCalificacion.SIN_ENTREGA),
            calificaciones.Count(c => c.Estado == EstadoCalificacion.PENDIENTE));

        return new IndicadoresParciales(
            estudiante,
            promedio,
            parcial,
            conteo,
            TasaFinalizacion(calificaciones, finalizacionDisponible),
            Tendencia(notas.Select(n => n.Nota).ToList()));
    }

    private static List<NotaCalculo> NotasOrdenadas(IReadOnlyList<Actividad> actividadesOrdenadas, IReadOnlyCollection<Calificacion> calificaciones)
    {
        var porActividad = calificaciones.Where(EsCalificada).GroupBy(c => c.ActividadId).ToDictionary(g => g.Key, g => g.First());
        return actividadesOrdenadas
            .Where(a => porActividad.ContainsKey(a.Id))
            .Select(a => new NotaCalculo(a, NotaEnEscala(porActividad[a.Id].Nota, a.NotaMaxima)!.Value))
            .ToList();
    }

    /// <summary>Tendencia del curso: la misma fórmula sobre el promedio de cada actividad calificada, en orden.</summary>
    private static decimal? TendenciaCurso(IReadOnlyList<Actividad> actividades, IReadOnlyCollection<Calificacion> calificadas)
    {
        var porActividad = calificadas.ToLookup(c => c.ActividadId);
        var promedios = actividades
            .Where(a => porActividad[a.Id].Any())
            .Select(a => porActividad[a.Id].Average(c => NotaEnEscala(c.Nota, a.NotaMaxima)!.Value))
            .ToList();
        return Tendencia(promedios);
    }

    /// <summary>Orden cronológico del curso: número de unidad, fecha límite y, al final, el orden del ítem en Moodle.</summary>
    public static List<Actividad> OrdenarActividades(IReadOnlyList<Actividad> actividades, IReadOnlyList<Unidad> unidades)
    {
        var numeroUnidad = unidades.ToDictionary(u => u.Id, u => u.Numero);
        return actividades
            .OrderBy(a => a.UnidadId is int id && numeroUnidad.TryGetValue(id, out var numero) ? numero : int.MaxValue)
            .ThenBy(a => a.FechaLimite ?? DateTimeOffset.MaxValue)
            .ThenBy(a => a.MoodleItemId)
            .ThenBy(a => a.Id)
            .ToList();
    }
}

/// <summary>Indicadores calculados, sin los datos de la sincronización (los agrega <see cref="IndicadorService"/>).</summary>
public sealed record IndicadoresCalculados(
    IReadOnlyList<UnidadIndicadoresDto> Unidades,
    ResumenIndicadoresCursoDto Curso,
    IReadOnlyList<TramoDistribucionDto> Distribucion,
    GruposRendimientoDto Grupos,
    IReadOnlyList<IndicadoresEstudianteDto> Estudiantes);
