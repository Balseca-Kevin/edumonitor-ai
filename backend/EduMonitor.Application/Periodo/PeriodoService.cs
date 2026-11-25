using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Indicadores;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Periodo;

/// <summary>
/// TA-22.A4 · Estado del período del curso con lo que guardó la última sincronización (fechas del curso, fechas límite y entregas) y
/// la fecha del servidor; nunca llama a Moodle. Lo usan la cabecera del curso y la herramienta del asistente.
/// </summary>
public sealed class PeriodoService(IEduMonitorDbContext db, IndicadorService indicadores, TimeProvider reloj)
{
    public const string Herramienta = "consultar_estado_periodo";

    public async Task<ConsultaPeriodo> ConsultarAsync(int cursoId, CancellationToken ct = default)
    {
        var curso = await db.Cursos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cursoId, ct);
        if (curso is null)
        {
            return new ConsultaPeriodo(EstadoConsultaPeriodo.CursoNoExiste);
        }

        var datos = await indicadores.CargarDatosAsync(cursoId, ct);
        if (datos is null)
        {
            return new ConsultaPeriodo(EstadoConsultaPeriodo.SinSincronizar, NombreCorto: curso.NombreCorto);
        }

        var ids = datos.Actividades.Select(a => a.Id).ToList();
        var temas = (await db.ActividadesTemas.AsNoTracking()
                .Where(at => ids.Contains(at.ActividadId))
                .Select(at => new { at.ActividadId, at.Tema.Nombre })
                .ToListAsync(ct))
            .GroupBy(t => t.ActividadId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(t => t.Nombre).ToList());

        var periodo = CalculadoraPeriodo.Calcular(cursoId, datos, curso.Inicio, curso.Fin, reloj.GetUtcNow(), temas);
        return new ConsultaPeriodo(EstadoConsultaPeriodo.Calculado, periodo, curso.NombreCorto);
    }

    /// <summary>Forma corta para el asistente. Null si el curso no existe; un curso sin sincronizar va con las listas vacías.</summary>
    public async Task<RespuestaEstadoPeriodoHerramienta?> HerramientaAsync(int cursoId, CancellationToken ct = default)
    {
        var consulta = await ConsultarAsync(cursoId, ct);
        if (consulta.Estado == EstadoConsultaPeriodo.CursoNoExiste)
        {
            return null;
        }

        var generado = CalculadoraPeriodo.EnEcuador(reloj.GetUtcNow());
        var curso = new CursoPeriodoHerramientaDto(cursoId, consulta.NombreCorto ?? string.Empty);
        var p = consulta.Periodo;
        if (p is null)
        {
            return new RespuestaEstadoPeriodoHerramienta(Herramienta, generado, curso, Dia(generado), null, null, null,
                Array.Empty<VencidaHerramientaDto>(), Array.Empty<EstaSemanaHerramientaDto>(), null);
        }

        return new RespuestaEstadoPeriodoHerramienta(
            Herramienta,
            generado,
            curso,
            Dia(p.Hoy),
            p.Semana?.Actual,
            p.Semana?.Total,
            p.UnidadActual is { } u ? $"U{u.Numero} · {u.Nombre}" : null,
            p.Vencidas.ConFaltas.Select(v => new VencidaHerramientaDto(v.Nombre, v.Faltan)).ToList(),
            p.VenceEstaSemana.Select(v => new EstaSemanaHerramientaDto(v.Nombre, Dia(v.FechaLimite), v.Entregaron, v.Matriculados)).ToList(),
            p.Siguiente is { } s ? new SiguienteHerramientaDto(s.Nombre, s.Semana, Dia(s.FechaLimite), s.Temas) : null);
    }

    private static string Dia(DateTimeOffset fecha) => fecha.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
