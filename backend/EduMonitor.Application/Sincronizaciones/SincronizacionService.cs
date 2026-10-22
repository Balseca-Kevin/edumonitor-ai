using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Configuracion;
using EduMonitor.Application.Cursos;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduMonitor.Application.Sincronizaciones;

/// <summary>
/// HU-05 · Sincronización manual de un curso. Cada paso importa una parte del curso; si una función de Moodle falla,
/// se anota el error y se siguen los demás pasos (estado PARCIAL). Cada ejecución queda registrada (TA-05.5).
/// La escritura es idempotente: los importadores hacen upsert por id de Moodle.
/// </summary>
public sealed class SincronizacionService(
    IEduMonitorDbContext db,
    ImportadorMatriculados matriculados,
    ImportadorCalificaciones calificaciones,
    IOptions<MoodleOptions> moodle,
    TimeProvider reloj)
{
    /// <summary>Sincroniza el curso y devuelve el registro. Null si el curso no existe.</summary>
    public async Task<SincronizacionDto?> SincronizarAsync(int cursoId, UsuarioActual usuario, CancellationToken ct = default)
    {
        var curso = await db.Cursos.SingleOrDefaultAsync(c => c.Id == cursoId, ct);
        if (curso is null)
        {
            return null;
        }

        var registro = new Sincronizacion
        {
            CursoId = curso.Id,
            UsuarioId = usuario.Id,
            Inicio = reloj.GetLocalNow(),
            Origen = moodle.Value.Url,
        };
        var pasosCorrectos = 0;

        // TA-05.1 (Sebastián): importar secciones como unidades con core_course_get_contents y llenar registro.RegistrosUnidades.

        // Matriculados va antes que notas: las notas solo se guardan para estudiantes matriculados.
        if (await IntentarAsync(registro, async () => registro.RegistrosEstudiantes = await matriculados.ImportarAsync(curso, ct)))
        {
            pasosCorrectos++;
        }

        if (await IntentarAsync(registro, async () =>
            {
                var resultado = await calificaciones.ImportarAsync(curso, ct);
                registro.RegistrosActividades = resultado.Actividades;
                registro.RegistrosCalificaciones = resultado.Calificaciones;
            }))
        {
            pasosCorrectos++;
        }

        // TA-05.4 (Sebastián): importar tareas, fechas, entregas y finalización (mod_assign_get_assignments,
        // mod_assign_get_submissions, core_completion_get_activities_completion_status) y derivar SIN_ENTREGA y PENDIENTE.

        registro.Estado = registro.Errores.Count == 0 ? EstadoSincronizacion.COMPLETADA
            : pasosCorrectos > 0 ? EstadoSincronizacion.PARCIAL
            : EstadoSincronizacion.FALLIDA;
        registro.Fin = reloj.GetLocalNow();

        db.Sincronizaciones.Add(registro);
        await db.SaveChangesAsync(ct);
        return SincronizacionDto.Desde(registro);
    }

    /// <summary>Historial del curso, de la más reciente a la más antigua.</summary>
    public async Task<IReadOnlyList<SincronizacionDto>> HistorialAsync(int cursoId, CancellationToken ct = default)
    {
        var registros = await db.Sincronizaciones.AsNoTracking()
            .Where(s => s.CursoId == cursoId)
            .OrderByDescending(s => s.Id)
            .ToListAsync(ct);
        return registros.Select(SincronizacionDto.Desde).ToList();
    }

    private static async Task<bool> IntentarAsync(Sincronizacion registro, Func<Task> paso)
    {
        try
        {
            await paso();
            return true;
        }
        catch (MoodleNoDisponibleException e)
        {
            registro.Errores.Add(new ErrorSincronizacion(e.Funcion, e.Message));
            return false;
        }
    }
}
