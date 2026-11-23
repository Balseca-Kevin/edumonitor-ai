using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Sincronizaciones;

/// <summary>
/// HU-08 · Bloque 2 · Importa las rúbricas de las tareas del curso, el nivel de cada estudiante en cada criterio y el comentario
/// del docente en cada entrega calificada:
/// <list type="number">
///   <item><c>mod_assign_get_assignments</c> da el cmid de cada tarea;</item>
///   <item><c>core_grading_get_definitions</c> da la rúbrica activa con sus criterios y niveles;</item>
///   <item><c>core_grading_get_gradingform_instances</c> da los niveles elegidos en cada nota, por id de la nota;</item>
///   <item><c>mod_assign_get_submission_status</c> enlaza ese id con el estudiante y trae el comentario del docente.</item>
/// </list>
/// Upsert por id de Moodle: el tema que el docente asoció a un criterio se conserva. Si Moodle falla, lanza
/// <see cref="MoodleNoDisponibleException"/> antes de guardar nada.
/// </summary>
public sealed class ImportadorRubricas(IMoodleClient moodle, IEduMonitorDbContext db)
{
    /// <summary>Estado de la calificación con rúbrica que vale en Moodle (gradingform_instance::INSTANCE_STATUS_ACTIVE).</summary>
    public const int InstanciaActiva = 1;

    /// <summary>Devuelve cuántos niveles por criterio quedaron guardados, o null si el curso no tiene tareas y no se llamó a Moodle.</summary>
    public async Task<int?> ImportarAsync(Curso curso, CancellationToken ct = default)
    {
        var actividades = await db.Actividades.Where(a => a.CursoId == curso.Id && a.Modulo == "assign" && a.Instancia != null).ToListAsync(ct);
        if (actividades.Count == 0)
        {
            return null;
        }

        var tareas = await moodle.ObtenerTareasAsync(curso.MoodleCursoId, ct);
        var cmidPorInstancia = tareas.ToDictionary(t => t.Id, t => t.ModuloCursoId);
        var actividadPorCmid = actividades
            .Where(a => cmidPorInstancia.ContainsKey(a.Instancia!.Value))
            .ToDictionary(a => cmidPorInstancia[a.Instancia!.Value]);
        var rubricas = await moodle.ObtenerRubricasAsync(actividadPorCmid.Keys.ToList(), ct);

        var estudiantes = await db.Matriculas.Where(m => m.CursoId == curso.Id).Select(m => m.Estudiante).ToListAsync(ct);
        var calificadas = await db.Calificaciones
            .Where(c => c.Actividad.CursoId == curso.Id && c.Estado == EstadoCalificacion.CALIFICADO)
            .ToListAsync(ct);

        // Todo lo de Moodle se lee antes de escribir: si una función falla, la base queda como estaba.
        var lecturas = new List<(Actividad Actividad, RubricaMoodle Rubrica, Dictionary<int, Estudiante> PorNota, IReadOnlyList<InstanciaRubricaMoodle> Instancias)>();
        var comentarios = new Dictionary<(int ActividadId, int EstudianteId), string?>();
        foreach (var rubrica in rubricas)
        {
            if (!actividadPorCmid.TryGetValue(rubrica.ModuloCursoId, out var actividad))
            {
                continue;
            }

            var instancias = await moodle.ObtenerCalificacionesRubricaAsync(rubrica.DefinicionId, ct);
            var porNota = new Dictionary<int, Estudiante>();
            foreach (var estudiante in estudiantes.Where(e => calificadas.Any(c => c.ActividadId == actividad.Id && c.EstudianteId == e.Id)))
            {
                var estado = await moodle.ObtenerEstadoEntregaAsync(actividad.Instancia!.Value, estudiante.MoodleUsuarioId, ct);
                comentarios[(actividad.Id, estudiante.Id)] = estado.Comentario;
                if (estado.CalificacionId is int notaId)
                {
                    porNota[notaId] = estudiante;
                }
            }

            lecturas.Add((actividad, rubrica, porNota, instancias));
        }

        var guardados = 0;
        foreach (var (actividad, rubrica, porNota, instancias) in lecturas)
        {
            var criterios = await GuardarCriteriosAsync(actividad, rubrica, ct);
            var niveles = rubrica.Criterios.SelectMany(c => c.Niveles.Select(n => (Criterio: c.Id, Nivel: n))).ToDictionary(x => x.Nivel.Id);

            var anteriores = await db.CalificacionesCriterio.Where(c => c.CriterioRubrica.ActividadId == actividad.Id).ToListAsync(ct);
            db.CalificacionesCriterio.RemoveRange(anteriores);

            // La calificación vigente de cada nota es la activa más reciente.
            foreach (var instancia in instancias.Where(i => i.Estado == InstanciaActiva).GroupBy(i => i.ItemId).Select(g => g.MaxBy(i => i.Id)!))
            {
                if (!porNota.TryGetValue(instancia.ItemId, out var estudiante))
                {
                    continue; // nota de alguien que ya no está matriculado
                }

                foreach (var elegido in instancia.Niveles)
                {
                    if (criterios.TryGetValue(elegido.CriterioId, out var criterio) && niveles.TryGetValue(elegido.NivelId, out var nivel))
                    {
                        db.CalificacionesCriterio.Add(new CalificacionCriterio
                        {
                            CriterioRubrica = criterio,
                            EstudianteId = estudiante.Id,
                            Puntaje = nivel.Nivel.Puntaje,
                            Nivel = nivel.Nivel.Definicion,
                        });
                        guardados++;
                    }
                }
            }
        }

        foreach (var calificacion in calificadas)
        {
            if (comentarios.TryGetValue((calificacion.ActividadId, calificacion.EstudianteId), out var comentario))
            {
                calificacion.Comentario = comentario;
            }
        }

        await db.SaveChangesAsync(ct);
        return guardados;
    }

    private async Task<Dictionary<int, CriterioRubrica>> GuardarCriteriosAsync(Actividad actividad, RubricaMoodle rubrica, CancellationToken ct)
    {
        var existentes = await db.CriteriosRubrica.Where(c => c.ActividadId == actividad.Id).ToListAsync(ct);
        var resultado = new Dictionary<int, CriterioRubrica>();
        foreach (var criterio in rubrica.Criterios)
        {
            var guardado = existentes.FirstOrDefault(c => c.MoodleCriterioId == criterio.Id);
            if (guardado is null)
            {
                guardado = new CriterioRubrica { Actividad = actividad, MoodleCriterioId = criterio.Id };
                db.CriteriosRubrica.Add(guardado);
            }

            guardado.Orden = criterio.Orden;
            guardado.Descripcion = criterio.Descripcion;
            guardado.PuntajeMaximo = criterio.Niveles.Count == 0 ? 0m : criterio.Niveles.Max(n => n.Puntaje);
            resultado[criterio.Id] = guardado;
        }

        // Un criterio que ya no está en la rúbrica sale con sus niveles.
        var quitados = existentes.Where(c => rubrica.Criterios.All(r => r.Id != c.MoodleCriterioId)).ToList();
        if (quitados.Count > 0)
        {
            var ids = quitados.Select(q => q.Id).ToList();
            db.CalificacionesCriterio.RemoveRange(await db.CalificacionesCriterio.Where(c => ids.Contains(c.CriterioRubricaId)).ToListAsync(ct));
            db.CriteriosRubrica.RemoveRange(quitados);
        }

        return resultado;
    }
}
