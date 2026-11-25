using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Application.Tematicas;

/// <summary>Criterio de rúbrica con el tema que le asoció el docente.</summary>
public sealed record CriterioCalculo(int Id, int ActividadId, string Descripcion, decimal PuntajeMaximo, int? TemaId);

/// <summary>Nivel de un estudiante en un criterio.</summary>
public sealed record NivelCalculo(int CriterioId, int EstudianteId, decimal Puntaje);

/// <summary>Tema con su unidad.</summary>
public sealed record TemaCalculo(int Id, string Nombre, int UnidadNumero, string UnidadNombre);

/// <summary>Todo lo que hace falta para el desempeño por tema, ya leído de la base.</summary>
public sealed record DatosTematica(
    IReadOnlyList<TemaCalculo> Temas,
    IReadOnlyList<Actividad> Actividades,
    IReadOnlyDictionary<int, IReadOnlyCollection<int>> TemasPorActividad,
    IReadOnlyList<CriterioCalculo> Criterios,
    IReadOnlyList<NivelCalculo> Niveles,
    IReadOnlyList<Estudiante> Estudiantes,
    IReadOnlyList<Calificacion> Calificaciones,
    DateTimeOffset Ahora);

/// <summary>
/// HU-08 · HU-22 · Desempeño por tema en código determinista (docs/contratos-api.md, «Desempeño por tema»): nota de cada estudiante
/// en cada tema por criterios de rúbrica o por la nota de la actividad, promedio, parte del curso que domina el tema (7,0 o más),
/// quiénes quedaron bajo 6,0 y dónde se vuelve a evaluar.
/// </summary>
public static class CalculadoraTematica
{
    public const string FuenteCriterios = "CRITERIOS";
    public const string FuenteNota = "NOTA";
    public const decimal Domina = 7.0m;
    public const decimal Bajo = 6.0m;

    public static TematicasDto Calcular(int cursoId, DatosTematica datos, int? actividadId)
    {
        var actividades = datos.Actividades.Where(a => actividadId is null || a.Id == actividadId).ToList();
        var idsActividades = actividades.Select(a => a.Id).ToHashSet();
        var temasDeActividad = (int id) => datos.TemasPorActividad.TryGetValue(id, out var t) ? t : Array.Empty<int>();
        var criteriosPorActividad = datos.Criterios.GroupBy(c => c.ActividadId).ToDictionary(g => g.Key, g => g.ToList());
        var niveles = datos.Niveles.ToDictionary(n => (n.CriterioId, n.EstudianteId), n => n.Puntaje);
        var calificadas = datos.Calificaciones
            .Where(c => c.Estado == EstadoCalificacion.CALIFICADO && c.Nota is not null)
            .ToDictionary(c => (c.ActividadId, c.EstudianteId), c => c.Nota!.Value);

        var temasUsados = actividades.SelectMany(a => temasDeActividad(a.Id)).ToHashSet();
        var resultado = new List<TemaDesempenoDto>();
        foreach (var tema in datos.Temas.Where(t => temasUsados.Contains(t.Id)))
        {
            var evaluaciones = new List<EvaluacionTemaDto>();
            var notasPorEstudiante = new Dictionary<int, List<NotaTemaActividadDto>>();
            foreach (var actividad in actividades.Where(a => temasDeActividad(a.Id).Contains(tema.Id)))
            {
                var criterios = (criteriosPorActividad.GetValueOrDefault(actividad.Id) ?? new List<CriterioCalculo>()).Where(c => c.TemaId == tema.Id).ToList();
                evaluaciones.Add(new EvaluacionTemaDto(
                    actividad.Id,
                    actividad.Nombre,
                    criterios.Count > 0 ? FuenteCriterios : FuenteNota,
                    criterios.Select(c => c.Descripcion).ToList()));

                foreach (var estudiante in datos.Estudiantes)
                {
                    var nota = NotaEnActividad(actividad.Id, estudiante.Id, criterios, niveles, calificadas);
                    if (nota is decimal n)
                    {
                        if (!notasPorEstudiante.TryGetValue(estudiante.Id, out var lista))
                        {
                            notasPorEstudiante[estudiante.Id] = lista = new List<NotaTemaActividadDto>();
                        }

                        lista.Add(new NotaTemaActividadDto(actividad.Id, Redondear(n)));
                    }
                }
            }

            var estudiantes = datos.Estudiantes
                .Where(e => notasPorEstudiante.ContainsKey(e.Id))
                .Select(e =>
                {
                    var notas = notasPorEstudiante[e.Id];
                    return new EstudianteTemaDto(e.Id, e.Nombre, e.Apellido, Redondear(notas.Average(x => x.Nota)), notas);
                })
                .OrderBy(e => e.Nota).ThenBy(e => e.Apellido).ThenBy(e => e.Nombre)
                .ToList();

            decimal? promedio = estudiantes.Count == 0 ? null : Redondear(estudiantes.Average(e => e.Nota));
            decimal? domina = estudiantes.Count == 0 ? null : Math.Round((decimal)estudiantes.Count(e => e.Nota >= Domina) / estudiantes.Count, 4);
            var bajo6 = estudiantes.Where(e => e.Nota < Bajo).Select(e => new EstudianteBajoDto(e.Id, e.Nombre, e.Apellido, e.Nota)).ToList();
            var proximas = datos.Actividades
                .Where(a => temasDeActividad(a.Id).Contains(tema.Id) && a.FechaLimite is not null && a.FechaLimite > datos.Ahora)
                .OrderBy(a => a.FechaLimite)
                .Select(a => new ProximaEvaluacionDto(a.Id, a.Nombre, a.FechaLimite!.Value))
                .ToList();

            resultado.Add(new TemaDesempenoDto(
                tema.Id, tema.Nombre, new UnidadTemaDto(tema.UnidadNumero, tema.UnidadNombre),
                evaluaciones, estudiantes.Count, promedio, domina, bajo6, estudiantes, proximas));
        }

        var conNota = calificadas.Keys.Select(k => k.ActividadId).ToHashSet();
        var sinTemas = actividades
            .Where(a => conNota.Contains(a.Id) && temasDeActividad(a.Id).Count == 0)
            .Select(a => new ActividadSinTemaDto(a.Id, a.Nombre))
            .ToList();

        return new TematicasDto(cursoId, actividadId, resultado.OrderBy(t => t.Domina ?? 2m).ThenBy(t => t.Nombre).ToList(), sinTemas);
    }

    /// <summary>Nota del estudiante en el tema dentro de una actividad: por sus criterios del tema o, si no hay, por la nota de la actividad.</summary>
    public static decimal? NotaEnActividad(
        int actividadId,
        int estudianteId,
        IReadOnlyList<CriterioCalculo> criteriosDelTema,
        IReadOnlyDictionary<(int CriterioId, int EstudianteId), decimal> niveles,
        IReadOnlyDictionary<(int ActividadId, int EstudianteId), decimal> calificadas)
    {
        if (criteriosDelTema.Count > 0)
        {
            var conNivel = criteriosDelTema.Where(c => niveles.ContainsKey((c.Id, estudianteId))).ToList();
            var maximo = conNivel.Sum(c => c.PuntajeMaximo);
            if (conNivel.Count == 0 || maximo <= 0)
            {
                return null;
            }

            return conNivel.Sum(c => niveles[(c.Id, estudianteId)]) / maximo * 10m;
        }

        return calificadas.TryGetValue((actividadId, estudianteId), out var nota) ? nota : null;
    }

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
