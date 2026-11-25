using EduMonitor.Application.Tematicas;

namespace EduMonitor.Application.Refuerzos;

/// <summary>TA-15.B1 · Estudiante de un grupo de refuerzo: su promedio en el tema y cuántas notas lo componen.</summary>
public sealed record EstudianteGrupoDto(int Id, string Nombre, string Apellido, decimal Promedio, int Notas);

/// <summary>
/// TA-15.B1 · Grupo de refuerzo de un tema. <c>ActividadId</c> es la última actividad evaluada del tema en el orden del curso: el plan grupal
/// se arma sobre ella con el mismo flujo de TA-15.A3.
/// </summary>
public sealed record GrupoRefuerzoDto(
    int TemaId,
    string Tema,
    UnidadTemaDto Unidad,
    ActividadPlanDto Actividad,
    IReadOnlyList<EstudianteGrupoDto> Estudiantes,
    IReadOnlyList<ProximaEvaluacionDto> Proximas);

/// <summary>Respuesta de <c>GET /api/cursos/{id}/refuerzos/grupos</c> (docs/contratos-api.md, «Grupos de refuerzo por tema»).</summary>
public sealed record GruposRefuerzoDto(int CursoId, decimal Umbral, int MinimoEstudiantes, IReadOnlyList<GrupoRefuerzoDto> Grupos);

/// <summary>
/// TA-15.B1 · Agrupa por tema a los estudiantes con promedio del tema menor a 6,0, con el cálculo de «Desempeño por tema». Un estudiante sin
/// notas del tema no entra (no es 0). Solo quedan los temas con 2 estudiantes o más. Los grupos van de más a menos estudiantes.
/// </summary>
public static class CalculadoraGruposRefuerzo
{
    public const decimal Umbral = 6.0m;
    public const int MinimoEstudiantes = 2;

    public static GruposRefuerzoDto Agrupar(TematicasDto desempeno)
    {
        var grupos = new List<GrupoRefuerzoDto>();
        foreach (var tema in desempeno.Temas)
        {
            var bajos = tema.Estudiantes
                .Where(e => e.Notas.Count > 0 && e.Nota < Umbral)
                .OrderBy(e => e.Nota).ThenBy(e => e.Apellido, StringComparer.CurrentCulture).ThenBy(e => e.Nombre, StringComparer.CurrentCulture)
                .ToList();
            if (bajos.Count < MinimoEstudiantes)
            {
                continue;
            }

            // La última evaluación del tema, en el orden del curso, donde el grupo ya tiene nota; nunca una que todavía no se califica.
            var conNota = bajos.SelectMany(e => e.Notas).Select(n => n.ActividadId).ToHashSet();
            var ultima = tema.Evaluaciones.LastOrDefault(e => conNota.Contains(e.ActividadId));
            if (ultima is null)
            {
                continue;
            }

            var estudiantes = bajos.Select(e => new EstudianteGrupoDto(e.Id, e.Nombre, e.Apellido, e.Nota, e.Notas.Count)).ToList();
            grupos.Add(new GrupoRefuerzoDto(
                tema.Id, tema.Nombre, tema.Unidad, new ActividadPlanDto(ultima.ActividadId, ultima.Actividad), estudiantes, tema.Proximas));
        }

        var ordenados = grupos
            .OrderByDescending(g => g.Estudiantes.Count)
            .ThenBy(g => g.Unidad.Numero)
            .ThenBy(g => g.Tema, StringComparer.CurrentCulture)
            .ToList();
        return new GruposRefuerzoDto(desempeno.CursoId, Umbral, MinimoEstudiantes, ordenados);
    }
}
