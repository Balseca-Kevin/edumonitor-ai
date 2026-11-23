using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Alertas;
using EduMonitor.Application.Configuracion;
using EduMonitor.Application.Cursos;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduMonitor.Application.Sincronizaciones;

/// <summary>
/// HU-05 · Sincronización manual de un curso. Cada paso importa una parte del curso; si una función de Moodle falla,
/// se anota el error y se siguen los demás pasos (estado PARCIAL). Cada ejecución queda registrada (TA-05.5).
/// La escritura es idempotente: los importadores hacen upsert por id de Moodle.
/// </summary>
public sealed class SincronizacionService(
    IEduMonitorDbContext db,
    ImportadorUnidades unidades,
    ImportadorMatriculados matriculados,
    ImportadorCalificaciones calificaciones,
    ImportadorEntregas entregas,
    ImportadorRubricas rubricas,
    IConfiguracionMoodle moodle,
    TimeProvider reloj,
    AlertasService alertas,
    ILogger<SincronizacionService> logger)
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
            Origen = moodle.Actual.Url,
        };
        var pasosCorrectos = 0;

        // TA-21.A1 · Foto de las calificaciones antes de importar, para guardar qué celdas cambiaron. En la primera
        // sincronización del curso todo es nuevo y no se marca ninguna celda.
        var hayAnterior = await db.Sincronizaciones.AnyAsync(s => s.CursoId == curso.Id, ct);
        var antes = hayAnterior ? await ComparadorCalificaciones.FotoAsync(db, curso.Id, ct) : null;

        // Unidades va primero: su estructura asigna la unidad a cada actividad y traduce la finalización.
        EstructuraCurso? estructura = null;
        if (await IntentarAsync(registro, async () =>
            {
                estructura = await unidades.ImportarAsync(curso, ct);
                registro.RegistrosUnidades = estructura.Unidades;
            }))
        {
            pasosCorrectos++;
        }

        // Matriculados va antes que notas: las notas solo se guardan para estudiantes matriculados.
        if (await IntentarAsync(registro, async () => registro.RegistrosEstudiantes = await matriculados.ImportarAsync(curso, ct)))
        {
            pasosCorrectos++;
        }

        // TA-22.A4 · Inicio y fin del curso para la semana del período; con un estudiante ya matriculado. Solo cuenta como paso
        // correcto si leyó las fechas: sin estudiantes no llamó a Moodle.
        var fechasLeidas = false;
        if (await IntentarAsync(registro, async () => fechasLeidas = await matriculados.ImportarFechasCursoAsync(curso, ct)) && fechasLeidas)
        {
            pasosCorrectos++;
        }

        if (await IntentarAsync(registro, async () =>
            {
                var resultado = await calificaciones.ImportarAsync(curso, estructura, ct);
                registro.RegistrosActividades = resultado.Actividades;
                registro.RegistrosCalificaciones = resultado.Calificaciones;
            }))
        {
            pasosCorrectos++;
        }

        // Entregas va después de las notas: deriva SIN_ENTREGA y PENDIENTE sobre las calificaciones sin nota.
        if (await IntentarAsync(registro, () => entregas.ImportarEntregasAsync(curso, ct)))
        {
            pasosCorrectos++;
        }

        // HU-08 · Rúbricas, niveles por criterio y comentario del docente: después de las notas, porque solo se leen las calificadas.
        // Solo cuenta como paso correcto si leyó Moodle: sin tareas no hay nada que leer.
        int? nivelesLeidos = null;
        if (await IntentarAsync(registro, async () => nivelesLeidos = await rubricas.ImportarAsync(curso, ct)) && nivelesLeidos is not null)
        {
            pasosCorrectos++;
        }

        // Sin la estructura de unidades no se puede relacionar la finalización con las actividades: queda como estaba.
        if (estructura is not null && await IntentarAsync(registro, () => entregas.ImportarFinalizacionAsync(curso, estructura, ct)))
        {
            pasosCorrectos++;
        }

        registro.Estado = registro.Errores.Count == 0 ? EstadoSincronizacion.COMPLETADA
            : pasosCorrectos > 0 ? EstadoSincronizacion.PARCIAL
            : EstadoSincronizacion.FALLIDA;
        registro.Fin = reloj.GetLocalNow();
        registro.CambiosRegistrados = true;

        db.Sincronizaciones.Add(registro);
        if (antes is not null)
        {
            var despues = await ComparadorCalificaciones.FotoAsync(db, curso.Id, ct);
            foreach (var cambio in ComparadorCalificaciones.Comparar(antes, despues))
            {
                db.CambiosSincronizacion.Add(new CambioSincronizacion
                {
                    Sincronizacion = registro,
                    ActividadId = cambio.ActividadId,
                    EstudianteId = cambio.EstudianteId,
                    Tipo = cambio.Tipo,
                    NotaAntes = cambio.Antes?.Nota,
                    EstadoAntes = cambio.Antes?.Estado,
                    NotaAhora = cambio.Ahora.Nota,
                    EstadoAhora = cambio.Ahora.Estado,
                });
            }
        }

        await db.SaveChangesAsync(ct);

        // TA-12.3 · Al terminar la sincronización se recalculan las alertas del curso con los datos recién guardados.
        // Si algo falla, la sincronización ya quedó registrada y no se deshace: solo se anota el error.
        try
        {
            await alertas.RecalcularAsync(curso.Id, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "No se pudieron recalcular las alertas del curso {CursoId}", curso.Id);
        }

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
