using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Indicadores;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Sincronizaciones;

/// <summary>Estado de una celda del libro: la nota y el estado de la calificación guardada.</summary>
public readonly record struct EstadoCelda(decimal? Nota, EstadoCalificacion? Estado);

/// <summary>Celda que cambió entre dos fotos de las calificaciones del curso.</summary>
public sealed record CambioCelda(int ActividadId, int EstudianteId, string Tipo, EstadoCelda? Antes, EstadoCelda Ahora);

/// <summary>
/// TA-21.A1 · Comparación determinista de las calificaciones del curso antes y después de sincronizar
/// (docs/contratos-api.md, «Cambios entre sincronizaciones»).
/// </summary>
public static class ComparadorCalificaciones
{
    public const string NotaNueva = "NOTA_NUEVA";
    public const string NotaCambiada = "NOTA_CAMBIADA";
    public const string EntregaNueva = "ENTREGA_NUEVA";
    public const string Otro = "OTRO";

    /// <summary>
    /// Compara cada celda que existe después de sincronizar con la de antes (null si no existía). Pasar de «sin dato» a «sin
    /// entrega», o crearse una celda sin entrega, no es un cambio. Las celdas que desaparecen no cuentan: su estudiante o su
    /// actividad ya no están en el curso.
    /// </summary>
    public static IReadOnlyList<CambioCelda> Comparar(
        IReadOnlyDictionary<(int ActividadId, int EstudianteId), EstadoCelda> antes,
        IReadOnlyDictionary<(int ActividadId, int EstudianteId), EstadoCelda> despues)
    {
        var cambios = new List<CambioCelda>();
        foreach (var (clave, ahora) in despues)
        {
            EstadoCelda? previo = antes.TryGetValue(clave, out var a) ? a : null;
            var tipo = Clasificar(previo, ahora);
            if (tipo is not null)
            {
                cambios.Add(new CambioCelda(clave.ActividadId, clave.EstudianteId, tipo, previo, ahora));
            }
        }

        return cambios;
    }

    /// <summary>Tipo del cambio de una celda, o null si no cambió nada que el docente tenga que mirar.</summary>
    public static string? Clasificar(EstadoCelda? antes, EstadoCelda ahora)
    {
        var estadoAntes = antes?.Estado;
        var calificadaAntes = estadoAntes == EstadoCalificacion.CALIFICADO;
        if (ahora.Estado == EstadoCalificacion.CALIFICADO)
        {
            if (!calificadaAntes) return NotaNueva;
            return antes!.Value.Nota == ahora.Nota ? null : NotaCambiada;
        }

        if (estadoAntes == ahora.Estado) return null;
        var sinEntregaAntes = estadoAntes is null or EstadoCalificacion.SIN_ENTREGA;
        if (ahora.Estado == EstadoCalificacion.PENDIENTE && sinEntregaAntes) return EntregaNueva;
        // Sin dato ↔ sin entrega (o una celda nueva sin entrega) no es nada nuevo.
        if (sinEntregaAntes && ahora.Estado is null or EstadoCalificacion.SIN_ENTREGA) return null;
        return Otro;
    }

    /// <summary>Foto de las calificaciones guardadas del curso, por actividad y estudiante.</summary>
    public static async Task<Dictionary<(int ActividadId, int EstudianteId), EstadoCelda>> FotoAsync(IEduMonitorDbContext db, int cursoId, CancellationToken ct)
    {
        var filas = await db.Calificaciones.AsNoTracking()
            .Where(c => c.Actividad.CursoId == cursoId)
            .Select(c => new { c.ActividadId, c.EstudianteId, c.Nota, c.Estado })
            .ToListAsync(ct);
        return filas.ToDictionary(f => (f.ActividadId, f.EstudianteId), f => new EstadoCelda(f.Nota, f.Estado));
    }
}

public sealed record ConteosCambiosDto(int Total, int NotasNuevas, int NotasCambiadas, int EntregasNuevas, int Otros)
{
    public static readonly ConteosCambiosDto Cero = new(0, 0, 0, 0, 0);
}

public sealed record EstadoCeldaDto(decimal? Nota, string? Estado);

public sealed record EstudianteCambioDto(int Id, string Nombre, string Apellido);

public sealed record CeldaCambioDto(int ActividadId, string Actividad, EstudianteCambioDto Estudiante, string Tipo, EstadoCeldaDto Antes, EstadoCeldaDto Ahora);

/// <summary>Respuesta de <c>GET /api/cursos/{id}/sync/{sid}/cambios</c>.</summary>
public sealed record CambiosSincronizacionDto(
    int SincronizacionId,
    int CursoId,
    DateTimeOffset Fecha,
    int? AnteriorId,
    ConteosCambiosDto Conteos,
    IReadOnlyList<CeldaCambioDto> Celdas,
    IReadOnlyList<string> NoDisponibles);

/// <summary>TA-21.A1 · Lee los cambios guardados de una sincronización del curso.</summary>
public sealed class CambiosSincronizacionService(IEduMonitorDbContext db)
{
    public const string NoDisponibleCambios = "cambios";

    /// <summary>Null si la sincronización no existe o no es de ese curso.</summary>
    public async Task<CambiosSincronizacionDto?> ConsultarAsync(int cursoId, int sincronizacionId, CancellationToken ct = default)
    {
        var registro = await db.Sincronizaciones.AsNoTracking().SingleOrDefaultAsync(s => s.Id == sincronizacionId && s.CursoId == cursoId, ct);
        if (registro is null)
        {
            return null;
        }

        var anteriorId = await db.Sincronizaciones.AsNoTracking()
            .Where(s => s.CursoId == cursoId && s.Id < sincronizacionId)
            .OrderByDescending(s => s.Id)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

        if (!registro.CambiosRegistrados)
        {
            return new CambiosSincronizacionDto(registro.Id, cursoId, registro.Fin, anteriorId, ConteosCambiosDto.Cero, Array.Empty<CeldaCambioDto>(), new[] { NoDisponibleCambios });
        }

        var cambios = await db.CambiosSincronizacion.AsNoTracking()
            .Where(c => c.SincronizacionId == sincronizacionId)
            .Include(c => c.Actividad)
            .Include(c => c.Estudiante)
            .ToListAsync(ct);
        var unidades = await db.Unidades.AsNoTracking().Where(u => u.CursoId == cursoId).ToListAsync(ct);
        var orden = CalculadoraIndicadores.OrdenarActividades(cambios.Select(c => c.Actividad).DistinctBy(a => a.Id).ToList(), unidades)
            .Select((a, i) => (a.Id, i))
            .ToDictionary(x => x.Id, x => x.i);

        var celdas = cambios
            .OrderBy(c => orden[c.ActividadId])
            .ThenBy(c => c.Estudiante.Apellido)
            .ThenBy(c => c.Estudiante.Nombre)
            .Select(c => new CeldaCambioDto(
                c.ActividadId,
                c.Actividad.Nombre,
                new EstudianteCambioDto(c.EstudianteId, c.Estudiante.Nombre, c.Estudiante.Apellido),
                c.Tipo,
                new EstadoCeldaDto(c.NotaAntes, c.EstadoAntes?.ToString()),
                new EstadoCeldaDto(c.NotaAhora, c.EstadoAhora?.ToString())))
            .ToList();

        int Contar(string tipo) => celdas.Count(c => c.Tipo == tipo);
        var conteos = new ConteosCambiosDto(
            celdas.Count,
            Contar(ComparadorCalificaciones.NotaNueva),
            Contar(ComparadorCalificaciones.NotaCambiada),
            Contar(ComparadorCalificaciones.EntregaNueva),
            Contar(ComparadorCalificaciones.Otro));
        return new CambiosSincronizacionDto(registro.Id, cursoId, registro.Fin, anteriorId, conteos, celdas, Array.Empty<string>());
    }
}
