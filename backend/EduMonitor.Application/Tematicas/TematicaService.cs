using System.Text.RegularExpressions;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Indicadores;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Tematicas;

/// <summary>
/// HU-08 · Mapeo de actividades y criterios de rúbrica a los temas de cada unidad, y desempeño por tema (HU-22). Lee y escribe solo
/// en EduMonitor; Moodle no se toca.
/// </summary>
public sealed partial class TematicaService(IEduMonitorDbContext db, IndicadorService indicadores, TimeProvider reloj)
{
    public const int LargoMaximoNombre = 200;

    public async Task<ResultadoTematica<MapeoDto>> MapeoAsync(int cursoId, CancellationToken ct = default)
    {
        var previo = await VerificarCursoAsync<MapeoDto>(cursoId, ct);
        if (previo is not null)
        {
            return previo;
        }

        var unidades = await db.Unidades.AsNoTracking().Where(u => u.CursoId == cursoId).OrderBy(u => u.Numero).ToListAsync(ct);
        var temas = await db.Temas.AsNoTracking().Where(t => t.Unidad.CursoId == cursoId).OrderBy(t => t.Id).ToListAsync(ct);
        var actividades = CalculadoraIndicadores.OrdenarActividades(
            await db.Actividades.AsNoTracking().Where(a => a.CursoId == cursoId).ToListAsync(ct), unidades);
        var mapeo = await TemasPorActividadAsync(cursoId, ct);
        var criterios = await db.CriteriosRubrica.AsNoTracking().Where(c => c.Actividad.CursoId == cursoId).OrderBy(c => c.Orden).ToListAsync(ct);
        var numero = unidades.ToDictionary(u => u.Id, u => u.Numero);

        return new ResultadoTematica<MapeoDto>(EstadoConsultaTematica.Correcta, new MapeoDto(
            cursoId,
            unidades.Select(u => new UnidadMapeoDto(u.Id, u.Numero, u.Nombre, u.Parcial,
                temas.Where(t => t.UnidadId == u.Id).Select(t => new TemaDto(t.Id, t.Nombre)).ToList())).ToList(),
            actividades.Select(a => AMapeo(a, numero, mapeo, criterios)).ToList()));
    }

    public async Task<ResultadoTematica<IReadOnlyList<TemaDto>>> TemasDeUnidadAsync(int cursoId, int unidadId, CancellationToken ct = default)
    {
        var unidad = await db.Unidades.AsNoTracking().SingleOrDefaultAsync(u => u.Id == unidadId && u.CursoId == cursoId, ct);
        if (unidad is null)
        {
            return new(EstadoConsultaTematica.NoEncontrado, Detalle: $"La unidad {unidadId} no es del curso {cursoId}.");
        }

        var temas = await db.Temas.AsNoTracking().Where(t => t.UnidadId == unidadId).OrderBy(t => t.Id).Select(t => new TemaDto(t.Id, t.Nombre)).ToListAsync(ct);
        return new(EstadoConsultaTematica.Correcta, temas);
    }

    public async Task<ResultadoTematica<TemaDto>> CrearTemaAsync(int cursoId, int unidadId, NuevoTemaDto nuevo, CancellationToken ct = default)
    {
        var unidad = await db.Unidades.SingleOrDefaultAsync(u => u.Id == unidadId && u.CursoId == cursoId, ct);
        if (unidad is null)
        {
            return new(EstadoConsultaTematica.NoEncontrado, Detalle: $"La unidad {unidadId} no es del curso {cursoId}.");
        }

        var nombre = Espacios().Replace(nuevo.Nombre ?? string.Empty, " ").Trim();
        if (nombre.Length == 0 || nombre.Length > LargoMaximoNombre)
        {
            return new(EstadoConsultaTematica.Invalida, Detalle: $"El nombre del tema es obligatorio y tiene hasta {LargoMaximoNombre} caracteres.");
        }

        var existentes = await db.Temas.Where(t => t.UnidadId == unidadId).Select(t => t.Nombre).ToListAsync(ct);
        if (existentes.Any(e => string.Equals(Espacios().Replace(e, " ").Trim(), nombre, StringComparison.OrdinalIgnoreCase)))
        {
            return new(EstadoConsultaTematica.Invalida, Detalle: $"La unidad ya tiene el tema «{nombre}».");
        }

        var tema = new Tema { UnidadId = unidadId, Nombre = nombre };
        db.Temas.Add(tema);
        await db.SaveChangesAsync(ct);
        return new(EstadoConsultaTematica.Correcta, new TemaDto(tema.Id, tema.Nombre));
    }

    public async Task<ResultadoTematica<ActividadMapeoDto>> AsociarAsync(int cursoId, int actividadId, TemasActividadDto pedido, CancellationToken ct = default)
    {
        var actividad = await db.Actividades.SingleOrDefaultAsync(a => a.Id == actividadId && a.CursoId == cursoId, ct);
        if (actividad is null)
        {
            return new(EstadoConsultaTematica.NoEncontrado, Detalle: $"La actividad {actividadId} no es del curso {cursoId}.");
        }

        var temasDelCurso = await db.Temas.Where(t => t.Unidad.CursoId == cursoId).Select(t => t.Id).ToListAsync(ct);
        var criterios = await db.CriteriosRubrica.Where(c => c.ActividadId == actividadId).ToListAsync(ct);
        var pedidosTemas = (pedido.Temas ?? Array.Empty<int>()).Distinct().ToList();
        var pedidosCriterios = pedido.Criterios ?? Array.Empty<CriterioTemaDto>();

        var temaAjeno = pedidosTemas.Concat(pedidosCriterios.Where(c => c.TemaId is not null).Select(c => c.TemaId!.Value)).FirstOrDefault(t => !temasDelCurso.Contains(t));
        if (temaAjeno != 0)
        {
            return new(EstadoConsultaTematica.Invalida, Detalle: $"El tema {temaAjeno} no es de una unidad del curso.");
        }

        var criterioAjeno = pedidosCriterios.FirstOrDefault(c => criterios.All(x => x.Id != c.Id));
        if (criterioAjeno is not null)
        {
            return new(EstadoConsultaTematica.Invalida, Detalle: $"El criterio {criterioAjeno.Id} no es de la actividad {actividadId}.");
        }

        foreach (var c in pedidosCriterios)
        {
            criterios.Single(x => x.Id == c.Id).TemaId = c.TemaId;
        }

        var finales = pedidosTemas.Concat(criterios.Where(c => c.TemaId is not null).Select(c => c.TemaId!.Value)).Distinct().ToHashSet();
        var actuales = await db.ActividadesTemas.Where(at => at.ActividadId == actividadId).ToListAsync(ct);
        db.ActividadesTemas.RemoveRange(actuales.Where(at => !finales.Contains(at.TemaId)));
        foreach (var temaId in finales.Where(t => actuales.All(at => at.TemaId != t)))
        {
            db.ActividadesTemas.Add(new ActividadTema { ActividadId = actividadId, TemaId = temaId, PesoRelativo = 1m });
        }

        await db.SaveChangesAsync(ct);

        var unidades = await db.Unidades.AsNoTracking().Where(u => u.CursoId == cursoId).ToDictionaryAsync(u => u.Id, u => u.Numero, ct);
        return new(EstadoConsultaTematica.Correcta, AMapeo(actividad, unidades, await TemasPorActividadAsync(cursoId, ct), criterios.OrderBy(c => c.Orden).ToList()));
    }

    public async Task<ResultadoTematica<TematicasDto>> DesempenoAsync(int cursoId, int? actividadId, CancellationToken ct = default)
    {
        var previo = await VerificarCursoAsync<TematicasDto>(cursoId, ct);
        if (previo is not null)
        {
            return previo;
        }

        var datos = await CargarAsync(cursoId, ct);
        if (actividadId is int id && datos.Actividades.All(a => a.Id != id))
        {
            return new(EstadoConsultaTematica.NoEncontrado, Detalle: $"La actividad {id} no es del curso {cursoId}.");
        }

        return new(EstadoConsultaTematica.Correcta, CalculadoraTematica.Calcular(cursoId, datos, actividadId));
    }

    /// <summary>Datos del desempeño por tema; los usan también el plan de refuerzo y la herramienta del asistente.</summary>
    public async Task<DatosTematica> CargarAsync(int cursoId, CancellationToken ct)
    {
        var curso = await indicadores.CargarDatosAsync(cursoId, ct) ?? throw new InvalidOperationException("Curso sin sincronizar");
        var temas = await db.Temas.AsNoTracking().Where(t => t.Unidad.CursoId == cursoId)
            .Select(t => new TemaCalculo(t.Id, t.Nombre, t.Unidad.Numero, t.Unidad.Nombre))
            .ToListAsync(ct);
        var criterios = await db.CriteriosRubrica.AsNoTracking().Where(c => c.Actividad.CursoId == cursoId)
            .Select(c => new CriterioCalculo(c.Id, c.ActividadId, c.Descripcion, c.PuntajeMaximo, c.TemaId))
            .ToListAsync(ct);
        var niveles = await db.CalificacionesCriterio.AsNoTracking().Where(c => c.CriterioRubrica.Actividad.CursoId == cursoId)
            .Select(c => new NivelCalculo(c.CriterioRubricaId, c.EstudianteId, c.Puntaje))
            .ToListAsync(ct);
        return new DatosTematica(
            temas,
            CalculadoraIndicadores.OrdenarActividades(curso.Actividades, curso.Unidades),
            await TemasPorActividadAsync(cursoId, ct),
            criterios,
            niveles,
            curso.Estudiantes,
            curso.Calificaciones,
            reloj.GetUtcNow());
    }

    private async Task<ResultadoTematica<T>?> VerificarCursoAsync<T>(int cursoId, CancellationToken ct)
    {
        if (!await db.Cursos.AnyAsync(c => c.Id == cursoId, ct))
        {
            return new(EstadoConsultaTematica.CursoNoExiste, Detalle: $"El curso {cursoId} no existe.");
        }

        return await indicadores.UltimaSincronizacionConDatosAsync(cursoId, ct) is null
            ? new(EstadoConsultaTematica.SinSincronizar, Detalle: "El curso todavía no tiene datos de Moodle. Sincronízalo para asociar temas.")
            : null;
    }

    private async Task<IReadOnlyDictionary<int, IReadOnlyCollection<int>>> TemasPorActividadAsync(int cursoId, CancellationToken ct) =>
        (await db.ActividadesTemas.AsNoTracking().Where(at => at.Actividad.CursoId == cursoId).Select(at => new { at.ActividadId, at.TemaId }).ToListAsync(ct))
            .GroupBy(x => x.ActividadId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<int>)g.Select(x => x.TemaId).OrderBy(t => t).ToList());

    private static ActividadMapeoDto AMapeo(
        Actividad a,
        IReadOnlyDictionary<int, int> numeroUnidad,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>> mapeo,
        IReadOnlyList<CriterioRubrica> criterios) =>
        new(
            a.Id,
            a.Nombre,
            a.Categoria,
            a.UnidadId is int u && numeroUnidad.TryGetValue(u, out var n) ? n : null,
            a.FechaLimite,
            (mapeo.GetValueOrDefault(a.Id) ?? Array.Empty<int>()).ToList(),
            criterios.Where(c => c.ActividadId == a.Id).Select(c => new CriterioMapeoDto(c.Id, c.Orden, c.Descripcion, c.PuntajeMaximo, c.TemaId)).ToList());

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}
