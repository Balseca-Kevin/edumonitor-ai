using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Indicadores;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.NotaNecesaria;

public enum EstadoConsultaNotaNecesaria
{
    Calculada,
    CursoNoExiste,
    SinSincronizar,
    EstudianteNoEncontrado,
}

public sealed record ConsultaNotaNecesaria(EstadoConsultaNotaNecesaria Estado, NotaNecesariaDto? Resultado = null);

/// <summary>Forma de la herramienta del asistente <c>calcular_nota_necesaria</c>: la misma respuesta con su nombre y la fecha.</summary>
public sealed record RespuestaNotaNecesariaHerramienta(string Herramienta, DateTimeOffset Generado, NotaNecesariaDto Resultado);

/// <summary>TA-10.A1 · Nota necesaria con lo que guardó la última sincronización y la fecha del servidor; nunca llama a Moodle.</summary>
public sealed class NotaNecesariaService(IEduMonitorDbContext db, IndicadorService indicadores, TimeProvider reloj)
{
    public const string Herramienta = "calcular_nota_necesaria";

    public async Task<ConsultaNotaNecesaria> ConsultarAsync(int cursoId, int estudianteId, CancellationToken ct = default)
    {
        if (!await db.Cursos.AnyAsync(c => c.Id == cursoId, ct))
        {
            return new(EstadoConsultaNotaNecesaria.CursoNoExiste);
        }

        var datos = await indicadores.CargarDatosAsync(cursoId, ct);
        if (datos is null)
        {
            return new(EstadoConsultaNotaNecesaria.SinSincronizar);
        }

        var estudiante = datos.Estudiantes.FirstOrDefault(e => e.Id == estudianteId);
        return estudiante is null
            ? new(EstadoConsultaNotaNecesaria.EstudianteNoEncontrado)
            : new(EstadoConsultaNotaNecesaria.Calculada, CalculadoraNotaNecesaria.Calcular(
                cursoId,
                estudiante,
                CalculadoraIndicadores.OrdenarActividades(datos.Actividades, datos.Unidades),
                datos.Calificaciones,
                reloj.GetUtcNow()));
    }
}
