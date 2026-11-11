using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Cursos;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Indicadores;

/// <summary>
/// TA-09.1 · Carga los datos guardados de la última sincronización con datos (COMPLETADA o PARCIAL) y calcula los indicadores
/// con <see cref="CalculadoraIndicadores"/>. Si la última sincronización no pudo leer la finalización, la tasa va en null y
/// aparece en <c>noDisponibles</c>; la interfaz la muestra como «no disponible», nunca como 0.
/// </summary>
public sealed class IndicadorService(IEduMonitorDbContext db)
{
    public const string FuncionFinalizacion = "core_completion_get_activities_completion_status";
    public const string IndicadorTasaFinalizacion = "tasaFinalizacion";

    public async Task<ConsultaIndicadores> ConsultarAsync(int cursoId, CancellationToken ct = default)
    {
        if (!await db.Cursos.AnyAsync(c => c.Id == cursoId, ct))
        {
            return new ConsultaIndicadores(EstadoConsultaIndicadores.CursoNoExiste);
        }

        var sincronizacion = await UltimaSincronizacionConDatosAsync(cursoId, ct);
        if (sincronizacion is null)
        {
            return new ConsultaIndicadores(EstadoConsultaIndicadores.SinSincronizar);
        }

        var datos = await CargarDatosAsync(cursoId, sincronizacion, ct);
        var calculados = CalculadoraIndicadores.Calcular(datos);

        var noDisponibles = new List<string>();
        if (calculados.Curso.TasaFinalizacion is null)
        {
            noDisponibles.Add(IndicadorTasaFinalizacion);
        }

        return new ConsultaIndicadores(
            EstadoConsultaIndicadores.Calculados,
            new IndicadoresCursoDto(
                cursoId,
                CalculadoraIndicadores.Escala,
                CalculadoraIndicadores.NotaAprobacion,
                new ResumenSincronizacion(sincronizacion.Id, sincronizacion.Inicio, sincronizacion.Estado.ToString()),
                calculados.Unidades,
                calculados.Curso,
                calculados.Distribucion,
                calculados.Grupos,
                calculados.Estudiantes,
                noDisponibles));
    }

    /// <summary>Datos guardados del curso para los cálculos; null si nunca se sincronizó con datos. Lo usan también las herramientas del agente y las alertas.</summary>
    public async Task<DatosCurso?> CargarDatosAsync(int cursoId, CancellationToken ct = default)
    {
        var sincronizacion = await UltimaSincronizacionConDatosAsync(cursoId, ct);
        return sincronizacion is null ? null : await CargarDatosAsync(cursoId, sincronizacion, ct);
    }

    /// <summary>La sincronización más reciente que importó datos. Una FALLIDA no cambia lo guardado, así que no cuenta.</summary>
    public Task<Sincronizacion?> UltimaSincronizacionConDatosAsync(int cursoId, CancellationToken ct = default) =>
        db.Sincronizaciones.AsNoTracking()
            .Where(s => s.CursoId == cursoId && s.Estado != EstadoSincronizacion.FALLIDA)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(ct);

    private async Task<DatosCurso> CargarDatosAsync(int cursoId, Sincronizacion sincronizacion, CancellationToken ct)
    {
        var unidades = await db.Unidades.AsNoTracking().Where(u => u.CursoId == cursoId).ToListAsync(ct);
        var actividades = await db.Actividades.AsNoTracking().Where(a => a.CursoId == cursoId).ToListAsync(ct);
        var estudiantes = await db.Matriculas.AsNoTracking()
            .Where(m => m.CursoId == cursoId)
            .Select(m => m.Estudiante)
            .ToListAsync(ct);
        estudiantes = estudiantes.OrderBy(e => e.Apellido).ThenBy(e => e.Nombre).ToList();

        var idsEstudiantes = estudiantes.Select(e => e.Id).ToList();
        var calificaciones = await db.Calificaciones.AsNoTracking()
            .Where(c => c.Actividad.CursoId == cursoId && idsEstudiantes.Contains(c.EstudianteId))
            .ToListAsync(ct);

        var finalizacionDisponible = !sincronizacion.Errores.Any(e => e.Funcion == FuncionFinalizacion);
        return new DatosCurso(unidades, actividades, estudiantes, calificaciones, finalizacionDisponible);
    }
}
