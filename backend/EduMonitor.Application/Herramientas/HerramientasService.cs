using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Indicadores;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Herramientas;

/// <summary>
/// Datos de las herramientas del agente (HU-13). Lee lo guardado en EduMonitorDB; nunca llama a Moodle.
/// La autorización por curso ya la resolvió la política CursoAutorizado.
/// </summary>
public sealed class HerramientasService(IEduMonitorDbContext db, IndicadorService indicadores, TimeProvider reloj)
{
    public const string HerramientaCalificaciones = "obtener_calificaciones";

    /// <summary>
    /// TA-13.1 · Notas y estados por actividad (escala 0 a 10) y promedio ponderado de cada estudiante, en el orden del curso.
    /// Null si el curso no existe; <see cref="ResultadoCalificaciones.EstudianteNoMatriculado"/> si se pidió un estudiante que no está en el curso.
    /// Un curso sin sincronizar responde con la lista vacía y <c>ultimaSincronizacion</c> en null.
    /// </summary>
    public async Task<ResultadoCalificaciones?> CalificacionesAsync(int cursoId, int? estudianteId, CancellationToken ct = default)
    {
        var curso = await db.Cursos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cursoId, ct);
        if (curso is null)
        {
            return null;
        }

        var sincronizacion = await indicadores.UltimaSincronizacionConDatosAsync(cursoId, ct);
        var cursoDto = new CursoHerramientaDto(curso.Id, curso.NombreCorto, sincronizacion?.Inicio);
        var datos = await indicadores.CargarDatosAsync(cursoId, ct);
        if (datos is null)
        {
            return new ResultadoCalificaciones(new RespuestaCalificacionesHerramienta(HerramientaCalificaciones, reloj.GetLocalNow(), cursoDto, Array.Empty<EstudianteCalificacionesDto>()));
        }

        var estudiantes = datos.Estudiantes.Where(e => estudianteId is null || e.Id == estudianteId).ToList();
        if (estudianteId is not null && estudiantes.Count == 0)
        {
            return new ResultadoCalificaciones(null, EstudianteNoMatriculado: true);
        }

        var promedios = CalculadoraIndicadores.Calcular(datos).Estudiantes.ToDictionary(e => e.Id, e => e.PromedioPonderado);
        var unidades = datos.Unidades.ToDictionary(u => u.Id, u => u.Numero);
        var orden = CalculadoraIndicadores.OrdenarActividades(datos.Actividades, datos.Unidades);
        var calificaciones = datos.Calificaciones.GroupBy(c => (c.EstudianteId, c.ActividadId)).ToDictionary(g => g.Key, g => g.First());

        var respuesta = estudiantes.Select(e => new EstudianteCalificacionesDto(
                e.Id,
                e.Nombre,
                e.Apellido,
                promedios.GetValueOrDefault(e.Id),
                orden
                    .Select(a => (Actividad: a, Calificacion: calificaciones.GetValueOrDefault((e.Id, a.Id))))
                    .Where(x => x.Calificacion is not null)
                    .Select(x => new CalificacionHerramientaDto(
                        x.Actividad.Nombre,
                        x.Actividad.Categoria,
                        x.Actividad.UnidadId is int id && unidades.TryGetValue(id, out var numero) ? numero : null,
                        CalculadoraIndicadores.Redondear(CalculadoraIndicadores.NotaEnEscala(x.Calificacion!.Nota, x.Actividad.NotaMaxima)),
                        x.Calificacion.Estado?.ToString()))
                    .ToList()))
            .ToList();

        return new ResultadoCalificaciones(new RespuestaCalificacionesHerramienta(HerramientaCalificaciones, reloj.GetLocalNow(), cursoDto, respuesta));
    }
}

public sealed record ResultadoCalificaciones(RespuestaCalificacionesHerramienta? Respuesta, bool EstudianteNoMatriculado = false);

/// <summary>Respuesta de <c>GET /api/tools/calificaciones</c> (herramienta <c>obtener_calificaciones</c>).</summary>
public sealed record RespuestaCalificacionesHerramienta(
    string Herramienta,
    DateTimeOffset Generado,
    CursoHerramientaDto Curso,
    IReadOnlyList<EstudianteCalificacionesDto> Estudiantes);

public sealed record CursoHerramientaDto(int Id, string NombreCorto, DateTimeOffset? UltimaSincronizacion);

public sealed record EstudianteCalificacionesDto(
    int Id,
    string Nombre,
    string Apellido,
    decimal? PromedioPonderado,
    IReadOnlyList<CalificacionHerramientaDto> Calificaciones);

/// <summary>Nota en escala 0 a 10 (null si no hay nota) y estado: CALIFICADO, SIN_ENTREGA, PENDIENTE o null si no está disponible.</summary>
public sealed record CalificacionHerramientaDto(string Actividad, string? Categoria, int? Unidad, decimal? Nota, string? Estado);
