using EduMonitor.Application.Abstractions;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Sincronizaciones;

/// <summary>
/// TA-05.4 · Importa tareas, fechas límite, entregas y finalización, y deriva el estado de las calificaciones sin nota:
/// PENDIENTE si el estudiante entregó y falta calificar, SIN_ENTREGA si no entregó. Una nota (incluida la 0) es CALIFICADO
/// y no se toca aquí. Se ejecuta después de importar las calificaciones (TA-05.3).
/// </summary>
public sealed class ImportadorEntregas(IMoodleClient moodle, IEduMonitorDbContext db)
{
    /// <summary>Estado de entrega de Moodle que cuenta como entregada; new, draft y reopened no lo son.</summary>
    public const string EstadoEntregada = "submitted";

    /// <summary>
    /// Tareas y entregas (mod_assign_get_assignments y mod_assign_get_submissions). Devuelve cuántas calificaciones
    /// quedaron como SIN_ENTREGA o PENDIENTE. Lanza <see cref="MoodleNoDisponibleException"/> si Moodle falla, sin tocar la base.
    /// </summary>
    public async Task<int> ImportarEntregasAsync(Curso curso, CancellationToken ct = default)
    {
        var tareas = await moodle.ObtenerTareasAsync(curso.MoodleCursoId, ct);
        var entregas = await moodle.ObtenerEntregasAsync(tareas.Select(t => t.Id).ToList(), ct);
        var entregadas = entregas
            .Where(e => string.Equals(e.Estado, EstadoEntregada, StringComparison.OrdinalIgnoreCase))
            .Select(e => (e.TareaId, e.UsuarioId))
            .ToHashSet();
        var tareasPorInstancia = tareas.ToDictionary(t => t.Id);

        var actividades = await db.Actividades.Where(a => a.CursoId == curso.Id && a.Modulo == "assign").ToListAsync(ct);
        foreach (var actividad in actividades)
        {
            if (actividad.Instancia is int instancia && tareasPorInstancia.TryGetValue(instancia, out var tarea))
            {
                actividad.FechaLimite = tarea.FechaLimite;
            }
        }

        var sinNota = await db.Calificaciones
            .Include(c => c.Actividad)
            .Include(c => c.Estudiante)
            .Where(c => c.Actividad.CursoId == curso.Id && c.Actividad.Modulo == "assign" && c.Nota == null)
            .ToListAsync(ct);
        var derivadas = 0;
        foreach (var calificacion in sinNota)
        {
            if (calificacion.Actividad.Instancia is not int instancia || !tareasPorInstancia.ContainsKey(instancia))
            {
                continue; // tarea que Moodle no devolvió: queda como no disponible
            }

            calificacion.Estado = entregadas.Contains((instancia, calificacion.Estudiante.MoodleUsuarioId))
                ? EstadoCalificacion.PENDIENTE
                : EstadoCalificacion.SIN_ENTREGA;
            derivadas++;
        }

        await db.SaveChangesAsync(ct);
        return derivadas;
    }

    /// <summary>
    /// Finalización de cada estudiante matriculado (core_completion_get_activities_completion_status). Necesita la estructura
    /// de TA-05.1 para pasar del cmid a la actividad. Lee a todos los estudiantes antes de escribir: si Moodle falla con
    /// uno, lanza <see cref="MoodleNoDisponibleException"/> y la finalización queda como estaba.
    /// </summary>
    public async Task<int> ImportarFinalizacionAsync(Curso curso, EstructuraCurso estructura, CancellationToken ct = default)
    {
        var estudiantes = await db.Matriculas.Where(m => m.CursoId == curso.Id).Select(m => m.Estudiante).ToListAsync(ct);
        var leidos = new List<(Estudiante Estudiante, IReadOnlyList<Abstractions.Moodle.FinalizacionActividadMoodle> Estados)>();
        foreach (var estudiante in estudiantes)
        {
            leidos.Add((estudiante, await moodle.ObtenerFinalizacionAsync(curso.MoodleCursoId, estudiante.MoodleUsuarioId, ct)));
        }

        var actividades = await db.Actividades.Where(a => a.CursoId == curso.Id && a.Modulo != null && a.Instancia != null)
            .ToDictionaryAsync(a => (a.Modulo!, a.Instancia!.Value), ct);
        var calificaciones = await db.Calificaciones.Where(c => c.Actividad.CursoId == curso.Id)
            .ToDictionaryAsync(c => (c.ActividadId, c.EstudianteId), ct);

        var registradas = 0;
        foreach (var (estudiante, estados) in leidos)
        {
            foreach (var estado in estados)
            {
                if (estructura.ActividadDeModulo(estado.ModuloCursoId) is not { } clave
                    || !actividades.TryGetValue(clave, out var actividad)
                    || !calificaciones.TryGetValue((actividad.Id, estudiante.Id), out var calificacion))
                {
                    continue;
                }

                calificacion.Finalizada = EstaFinalizada(estado.Estado);
                registradas++;
            }
        }

        await db.SaveChangesAsync(ct);
        return registradas;
    }

    /// <summary>Moodle: 0 incompleta, 1 completa, 2 completa y aprobada, 3 completa y reprobada (cuenta como finalizada).</summary>
    public static bool EstaFinalizada(int estadoMoodle) => estadoMoodle is 1 or 2 or 3;
}
