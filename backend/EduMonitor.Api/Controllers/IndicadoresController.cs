using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Indicadores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>HU-09 · Indicadores del curso calculados con los datos de la última sincronización.</summary>
[ApiController]
[Route("api/cursos/{id:int}/indicadores")]
[Authorize(Policy = CursoAutorizadoRequirement.Politica)]
public sealed class IndicadoresController(IndicadorService indicadores) : ControllerBase
{
    /// <summary>200 con los indicadores del <c>tramo</c> elegido (R.2); 400 con un tramo que no existe; 403 si el curso es ajeno; 404 si no existe; 409 si nunca se sincronizó.</summary>
    [HttpGet]
    [ProducesResponseType<IndicadoresCursoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Obtener(int id, [FromQuery] string? tramo, CancellationToken ct)
    {
        if (!Tramo.TryParse(tramo, out var elegido))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "El tramo solo acepta todo, p1, p2 o u1 a un.");
        }

        var consulta = await indicadores.ConsultarAsync(id, elegido, ct);
        return consulta.Estado switch
        {
            EstadoConsultaIndicadores.CursoNoExiste =>
                Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {id} no existe."),
            EstadoConsultaIndicadores.SinSincronizar =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar",
                    detail: "El curso todavía no tiene datos de Moodle. Sincronízalo desde Mis cursos para ver sus indicadores."),
            EstadoConsultaIndicadores.TramoInvalido => Problem(statusCode: StatusCodes.Status400BadRequest, detail: consulta.Detalle),
            _ => Ok(consulta.Indicadores),
        };
    }
}
