using EduMonitor.Application.Indicadores;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Application.NotaNecesaria;

public sealed record ActividadPendienteDto(int ActividadId, string Nombre, DateTimeOffset? FechaLimite, string Estado);

public sealed record CategoriaNotaNecesariaDto(string Categoria, decimal Peso, int Calificadas, decimal? Promedio, IReadOnlyList<ActividadPendienteDto> Pendientes);

public sealed record EstudianteNotaNecesariaDto(int Id, string Nombre, string Apellido);

/// <summary>Respuesta de <c>GET /api/cursos/{id}/estudiantes/{eid}/nota-necesaria</c> (docs/contratos-api.md, «Nota necesaria»).</summary>
public sealed record NotaNecesariaDto(
    int CursoId,
    EstudianteNotaNecesariaDto Estudiante,
    decimal Aprobacion,
    decimal? PromedioActual,
    bool Parcial,
    string Estado,
    decimal? NotaNecesaria,
    decimal? MaximoPosible,
    int Pendientes,
    IReadOnlyList<CategoriaNotaNecesariaDto> Categorias);

/// <summary>
/// TA-10.A1 · Nota mínima que el estudiante necesita, igual en todas sus actividades pendientes, para que el promedio 40/30/30 llegue
/// a 7,0 con las notas actuales. Pendientes: las que no tienen nota y todavía cuentan (aún no vencen, están por calificar o no tienen
/// fecha límite); una vencida sin entrega no entra, igual que en el promedio. En pantalla siempre «cálculo con las notas actuales».
/// </summary>
public static class CalculadoraNotaNecesaria
{
    public const decimal Aprobacion = 7.0m;
    public const decimal Escala = 10m;
    public const string Necesita = "NECESITA";
    public const string YaAlcanza = "YA_ALCANZA";
    public const string NoAlcanza = "NO_ALCANZA";
    public const string SinPendientes = "SIN_PENDIENTES";
    public const string SinDatos = "SIN_DATOS";

    public static NotaNecesariaDto Calcular(int cursoId, Estudiante estudiante, IReadOnlyList<Actividad> actividades, IReadOnlyList<Calificacion> calificaciones, DateTimeOffset ahora)
    {
        var propias = calificaciones.Where(c => c.EstudianteId == estudiante.Id).ToDictionary(c => c.ActividadId);
        var categorias = new List<(string Clave, CategoriaNotaNecesariaDto Dto, decimal Suma)>();
        foreach (var (clave, peso) in CalculadoraIndicadores.PesosCategoria)
        {
            var delaCategoria = actividades.Where(a => CalculadoraIndicadores.ClaveCategoria(a.Categoria) == clave).ToList();
            var notas = new List<decimal>();
            var pendientes = new List<ActividadPendienteDto>();
            foreach (var actividad in delaCategoria)
            {
                propias.TryGetValue(actividad.Id, out var c);
                if (c?.Estado == EstadoCalificacion.CALIFICADO && c.Nota is decimal valor)
                {
                    notas.Add(valor);
                }
                else if (EstadoPendiente(actividad, c, ahora) is { } estado)
                {
                    pendientes.Add(new ActividadPendienteDto(actividad.Id, actividad.Nombre, actividad.FechaLimite, estado));
                }
            }

            var nombre = delaCategoria.FirstOrDefault()?.Categoria ?? clave;
            categorias.Add((clave, new CategoriaNotaNecesariaDto(nombre, peso, notas.Count, notas.Count == 0 ? null : Redondear(notas.Average()), pendientes), notas.Sum()));
        }

        var (promedio, parcial) = CalculadoraIndicadores.PromedioPonderado(
            actividades.Where(a => propias.TryGetValue(a.Id, out var c) && c.Estado == EstadoCalificacion.CALIFICADO && c.Nota is not null)
                .Select(a => (a.Categoria, propias[a.Id].Nota!.Value)));
        var dto = new EstudianteNotaNecesariaDto(estudiante.Id, estudiante.Nombre, estudiante.Apellido);
        var listas = categorias.Select(c => c.Dto).ToList();
        var totalPendientes = listas.Sum(c => c.Pendientes.Count);

        // Final(x) = Σ peso·(suma de notas + x·pendientes)/(notas + pendientes), con los pesos de las categorías que cuentan.
        var cuentan = categorias.Where(c => c.Dto.Calificadas + c.Dto.Pendientes.Count > 0).ToList();
        if (cuentan.Count == 0)
        {
            return new NotaNecesariaDto(cursoId, dto, Aprobacion, null, false, SinDatos, null, null, 0, listas);
        }

        var pesoTotal = cuentan.Sum(c => c.Dto.Peso);
        decimal a = 0m, b = 0m;
        foreach (var (_, categoria, suma) in cuentan)
        {
            var n = categoria.Calificadas + categoria.Pendientes.Count;
            a += categoria.Peso / pesoTotal * suma / n;
            b += categoria.Peso / pesoTotal * categoria.Pendientes.Count / n;
        }

        var promedioActual = promedio is null ? (decimal?)null : Redondear(promedio.Value);
        if (b == 0m)
        {
            return new NotaNecesariaDto(cursoId, dto, Aprobacion, promedioActual, parcial, SinPendientes, null, promedioActual, 0, listas);
        }

        var maximo = Redondear(a + Escala * b);
        var necesaria = (Aprobacion - a) / b;
        var (estadoFinal, nota) = necesaria <= 0m ? (YaAlcanza, (decimal?)null)
            : necesaria > Escala ? (NoAlcanza, (decimal?)null)
            : (Necesita, Math.Ceiling(necesaria * 100m) / 100m);
        return new NotaNecesariaDto(cursoId, dto, Aprobacion, promedioActual, parcial, estadoFinal, nota, maximo, totalPendientes, listas);
    }

    /// <summary>AUN_NO_VENCE, PENDIENTE o SIN_FECHA si la actividad sin nota todavía cuenta; null si venció sin entrega.</summary>
    private static string? EstadoPendiente(Actividad actividad, Calificacion? c, DateTimeOffset ahora)
    {
        if (c?.Estado == EstadoCalificacion.PENDIENTE) return "PENDIENTE";
        if (actividad.FechaLimite is null) return c?.Estado == EstadoCalificacion.SIN_ENTREGA ? null : "SIN_FECHA";
        return actividad.FechaLimite > ahora ? "AUN_NO_VENCE" : null;
    }

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
