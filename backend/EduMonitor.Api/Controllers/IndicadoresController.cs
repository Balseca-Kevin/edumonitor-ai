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
    /// <summary>200 con los indicadores; 403 si el curso es ajeno; 404 si no existe; 409 si nunca se sincronizó.</summary>
    [HttpGet]
    [ProducesResponseType<IndicadoresCursoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var consulta = await indicadores.ConsultarAsync(id, ct);
        return consulta.Estado switch
        {
            EstadoConsultaIndicadores.CursoNoExiste =>
                Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {id} no existe."),
            EstadoConsultaIndicadores.SinSincronizar =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar",
                    detail: "El curso todavía no tiene datos de Moodle. Sincronízalo desde Mis cursos para ver sus indicadores."),
            _ => Ok(consulta.Indicadores),
        };
    }
}
