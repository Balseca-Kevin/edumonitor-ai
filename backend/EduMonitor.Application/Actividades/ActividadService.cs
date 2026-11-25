using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Indicadores;
using EduMonitor.Application.Libro;
using EduMonitor.Application.Periodo;
using EduMonitor.Application.Tematicas;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Actividades;

/// <summary>
/// HU-22 · Panel de actividad con lo que guardó la última sincronización: enunciado y adjuntos de Moodle, entregas, distribución de
/// notas, dominio por criterio de la rúbrica, temas evaluados y quiénes requieren atención en esa actividad. No llama a Moodle.
/// </summary>
public sealed class ActividadService(IEduMonitorDbContext db, IndicadorService indicadores, TematicaService tematicas, PeriodoService periodo)
{
    public const string Herramienta = "obtener_detalle_actividad";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ConsultaActividad> ConsultarAsync(int cursoId, int actividadId, CancellationToken ct = default)
    {
        var datos = await indicadores.CargarDatosAsync(cursoId, ct);
        if (datos is null)
        {
            return new ConsultaActividad(EstadoConsultaActividad.SinSincronizar);
        }

        var actividad = datos.Actividades.FirstOrDefault(a => a.Id == actividadId);
        if (actividad is null)
        {
            return new ConsultaActividad(EstadoConsultaActividad.ActividadNoExiste);
        }

        var unidad = actividad.UnidadId is int uid ? datos.Unidades.FirstOrDefault(u => u.Id == uid) : null;
        var calificaciones = datos.Calificaciones.Where(c => c.ActividadId == actividadId).GroupBy(c => c.EstudianteId).ToDictionary(g => g.Key, g => g.First());
        var filas = datos.Estudiantes
            .OrderBy(e => e.Apellido).ThenBy(e => e.Nombre)
            .Select(e =>
            {
                var cal = calificaciones.GetValueOrDefault(e.Id);
                var estado = LibroService.EstadoNota(cal, actividad, datos.Ahora);
                var nota = estado == "CALIFICADO" ? CalculadoraIndicadores.Redondear(CalculadoraIndicadores.NotaEnEscala(cal!.Nota, actividad.NotaMaxima)) : null;
                return new EstudianteActividadDto(e.Id, e.Nombre, e.Apellido, nota, estado, cal?.Comentario);
            })
            .ToList();

        var criterios = await db.CriteriosRubrica.AsNoTracking()
            .Where(c => c.ActividadId == actividadId)
            .OrderBy(c => c.Orden)
            .Select(c => new { c.Id, c.Orden, c.Descripcion, c.PuntajeMaximo, Tema = c.Tema == null ? null : c.Tema.Nombre })
            .ToListAsync(ct);
        var ids = criterios.Select(c => c.Id).ToList();
        var puntajes = await db.CalificacionesCriterio.AsNoTracking()
            .Where(c => ids.Contains(c.CriterioRubricaId))
            .Select(c => new { c.CriterioRubricaId, c.EstudianteId, c.Puntaje })
            .ToListAsync(ct);

        // TA-22.A1: la separación usa a los estudiantes calificados que tienen nivel en todos los criterios de la rúbrica.
        var calificados = filas.Where(f => f.Estado == "CALIFICADO").Select(f => f.Id).ToHashSet();
        var porEstudiante = puntajes
            .Where(p => calificados.Contains(p.EstudianteId))
            .GroupBy(p => p.EstudianteId)
            .Where(g => g.Select(p => p.CriterioRubricaId).Distinct().Count() == criterios.Count)
            .ToDictionary(g => g.Key, g => g.GroupBy(p => p.CriterioRubricaId).ToDictionary(x => x.Key, x => x.First().Puntaje));
        var dominioCriterios = criterios
            .Select(c =>
            {
                var pares = porEstudiante.Values.Select(v => (v[c.Id], v.Where(x => x.Key != c.Id).Sum(x => x.Value))).ToList();
                return CalculadoraActividad.Criterio(c.Id, c.Orden, c.Descripcion, c.PuntajeMaximo, c.Tema,
                    puntajes.Where(p => p.CriterioRubricaId == c.Id).Select(p => p.Puntaje).ToList(),
                    CalculadoraCriterio.Separacion(pares, criterios.Count));
            })
            .ToList();

        var temas = (await tematicas.DesempenoAsync(cursoId, actividadId, ct)).Valor?.Temas
            .Select(t => new TemaActividadDto(t.Id, t.Nombre, t.Domina, t.Promedio, t.EstudiantesConNota))
            .ToList() ?? new List<TemaActividadDto>();

        var conAlerta = (await db.Alertas.AsNoTracking()
                .Where(a => a.Regla.CursoId == cursoId && a.Estado == "ACTIVA")
                .Select(a => a.EstudianteId)
                .ToListAsync(ct))
            .ToHashSet();
        var requieren = filas
            .Where(f => (f.Estado == "CALIFICADO" && f.Nota < CalculadoraActividad.Bajo) || f.Estado == "SIN_ENTREGA")
            .OrderBy(f => f.Estado == "SIN_ENTREGA" ? 1 : 0).ThenBy(f => f.Nota)
            .Select(f => new RequiereAtencionDto(f.Id, f.Nombre, f.Apellido, f.Nota, f.Estado!, conAlerta.Contains(f.Id)))
            .ToList();

        var delPeriodo = (await periodo.ConsultarAsync(cursoId, ct)).Periodo?.Actividades.FirstOrDefault(a => a.Id == actividadId);
        var notas = filas.Where(f => f.Nota is not null).Select(f => f.Nota!.Value).ToList();
        var resumen = CalculadoraActividad.Resumen(filas.Select(f => (f.Estado, f.Nota)).ToList());

        return new ConsultaActividad(EstadoConsultaActividad.Encontrada, new ActividadDetalleDto(
            cursoId,
            new ActividadInfoDto(
                actividad.Id, actividad.Nombre, delPeriodo?.Sigla, actividad.Categoria,
                unidad is null ? null : new UnidadActividadDto(unidad.Numero, unidad.Nombre, unidad.Parcial),
                actividad.FechaLimite, delPeriodo?.Estado, delPeriodo?.Semana,
                actividad.Enunciado, LeerArchivos(actividad.Archivos)),
            resumen,
            CalculadoraIndicadores.Distribucion(notas),
            dominioCriterios,
            temas,
            requieren,
            filas,
            datos.FinalizacionDisponible ? Array.Empty<string>() : new[] { "tasaFinalizacion" }));
    }

    /// <summary>Forma corta para el asistente: sin la lista completa de estudiantes ni sus comentarios.</summary>
    public async Task<(EstadoConsultaActividad Estado, RespuestaActividadHerramienta? Respuesta)> HerramientaAsync(int cursoId, int actividadId, DateTimeOffset generado, CancellationToken ct = default)
    {
        var consulta = await ConsultarAsync(cursoId, actividadId, ct);
        if (consulta.Detalle is not { } d)
        {
            return (consulta.Estado, null);
        }

        var enunciado = d.Actividad.Enunciado is { Length: > 1500 } largo ? largo[..1500] + "…" : d.Actividad.Enunciado;
        return (consulta.Estado, new RespuestaActividadHerramienta(
            Herramienta, generado,
            d.Actividad with { Enunciado = enunciado },
            d.Resumen,
            d.Criterios,
            d.Temas,
            d.RequierenAtencion));
    }

    private static IReadOnlyList<ArchivoActividadDto> LeerArchivos(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<ArchivoActividadDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<ArchivoActividadDto>>(json, Json) ?? new List<ArchivoActividadDto>();
        }
        catch (JsonException)
        {
            return Array.Empty<ArchivoActividadDto>();
        }
    }
}

/// <summary>HU-22 · Cálculos del panel de actividad, sin base de datos, con pruebas de casos conocidos.</summary>
public static class CalculadoraActividad
{
    public const decimal Aprobacion = 7.0m;
    public const decimal Bajo = 6.0m;

    /// <summary>Un criterio se domina con el 70 % de su puntaje máximo o más.</summary>
    public const decimal ParteDominio = 0.7m;

    /// <summary>Conteo de entregas y notas de la actividad. Solo las notas calificadas entran al promedio; sin entrega no es 0.</summary>
    public static ResumenActividadDto Resumen(IReadOnlyList<(string? Estado, decimal? Nota)> filas)
    {
        var notas = filas.Where(f => f.Estado == "CALIFICADO" && f.Nota is not null).Select(f => f.Nota!.Value).ToList();
        return new ResumenActividadDto(
            filas.Count,
            notas.Count,
            notas.Count(n => n == 0),
            filas.Count(f => f.Estado == "PENDIENTE"),
            filas.Count(f => f.Estado == "SIN_ENTREGA"),
            filas.Count(f => f.Estado == "AUN_NO_VENCE"),
            filas.Count(f => f.Estado is null),
            CalculadoraIndicadores.Redondear(CalculadoraIndicadores.Media(notas)),
            notas.Count(n => n >= Aprobacion),
            notas.Count(n => n < Bajo));
    }

    /// <summary>
    /// Dominio de un criterio: la parte de los estudiantes con nivel que sacó el 70 % del puntaje máximo o más, de 0 a 1, y el
    /// puntaje promedio. Sin niveles, los dos van en null. Suma la separación (TA-22.A1) y el aviso que sale del dominio y de ella.
    /// </summary>
    public static CriterioActividadDto Criterio(
        int id, int orden, string descripcion, decimal puntajeMaximo, string? tema, IReadOnlyList<decimal> puntajes, decimal? separacion = null)
    {
        if (puntajes.Count == 0)
        {
            return new CriterioActividadDto(id, orden, descripcion, puntajeMaximo, tema, 0, null, null, null, null);
        }

        var umbral = puntajeMaximo * ParteDominio;
        var domina = Math.Round((decimal)puntajes.Count(p => p >= umbral) / puntajes.Count, 4);
        return new CriterioActividadDto(id, orden, descripcion, puntajeMaximo, tema, puntajes.Count, domina,
            CalculadoraIndicadores.Redondear(puntajes.Average()), separacion, CalculadoraCriterio.Aviso(domina, separacion));
    }
}

public enum EstadoConsultaActividad
{
    Encontrada,
    SinSincronizar,
    ActividadNoExiste,
}

public sealed record ConsultaActividad(EstadoConsultaActividad Estado, ActividadDetalleDto? Detalle = null);

/// <summary>Respuesta de <c>GET /api/cursos/{id}/actividades/{aid}</c> (HU-22).</summary>
public sealed record ActividadDetalleDto(
    int CursoId,
    ActividadInfoDto Actividad,
    ResumenActividadDto Resumen,
    IReadOnlyList<TramoDistribucionDto> Distribucion,
    IReadOnlyList<CriterioActividadDto> Criterios,
    IReadOnlyList<TemaActividadDto> Temas,
    IReadOnlyList<RequiereAtencionDto> RequierenAtencion,
    IReadOnlyList<EstudianteActividadDto> Estudiantes,
    IReadOnlyList<string> NoDisponibles);

public sealed record ActividadInfoDto(
    int Id, string Nombre, string? Sigla, string? Categoria, UnidadActividadDto? Unidad, DateTimeOffset? FechaLimite,
    string? EstadoPeriodo, int? Semana, string? Enunciado, IReadOnlyList<ArchivoActividadDto> Archivos);

public sealed record UnidadActividadDto(int Numero, string Nombre, int Parcial);

public sealed record ArchivoActividadDto(string Nombre, long Tamano, string? Tipo);

public sealed record ResumenActividadDto(
    int Matriculados, int Calificadas, int Ceros, int PorCalificar, int SinEntrega, int AunNoVence, int SinDato,
    decimal? Promedio, int Aprobados, int Bajo6);

public sealed record CriterioActividadDto(
    int Id, int Orden, string Descripcion, decimal PuntajeMaximo, string? Tema, int ConNivel, decimal? Domina, decimal? PuntajePromedio,
    decimal? Separacion = null, string? Aviso = null);

public sealed record TemaActividadDto(int Id, string Nombre, decimal? Domina, decimal? Promedio, int EstudiantesConNota);

public sealed record RequiereAtencionDto(int Id, string Nombre, string Apellido, decimal? Nota, string Estado, bool AlertaActiva);

public sealed record EstudianteActividadDto(int Id, string Nombre, string Apellido, decimal? Nota, string? Estado, string? Comentario);

/// <summary>Respuesta de <c>GET /api/tools/actividad</c>: el panel de actividad en forma corta.</summary>
public sealed record RespuestaActividadHerramienta(
    string Herramienta, DateTimeOffset Generado, ActividadInfoDto Actividad, ResumenActividadDto Resumen,
    IReadOnlyList<CriterioActividadDto> Criterios, IReadOnlyList<TemaActividadDto> Temas, IReadOnlyList<RequiereAtencionDto> RequierenAtencion);
