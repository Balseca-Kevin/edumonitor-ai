using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Periodo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>TA-22.A4 · Estado del período: semana, unidades, actividades con su estado y los bloques de la cabecera del curso.</summary>
[ApiController]
[Route("api/cursos/{id:int}/periodo")]
[Authorize(Policy = CursoAutorizadoRequirement.Politica)]
public sealed class PeriodoController(PeriodoService periodo) : ControllerBase
{
    /// <summary>200 con el período; 403 si el curso es ajeno; 404 si no existe; 409 si nunca se sincronizó.</summary>
    [HttpGet]
    [ProducesResponseType<PeriodoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var consulta = await periodo.ConsultarAsync(id, ct);
        return consulta.Estado switch
        {
            EstadoConsultaPeriodo.CursoNoExiste => Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {id} no existe."),
            EstadoConsultaPeriodo.SinSincronizar => Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar",
                detail: "El curso todavía no tiene datos de Moodle. Sincronízalo para ver el estado del período."),
            _ => Ok(consulta.Periodo),
        };
    }
}
