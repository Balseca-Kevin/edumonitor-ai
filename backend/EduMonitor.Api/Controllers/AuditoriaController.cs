using EduMonitor.Application.Auditorias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>HU-17 · Registro de auditoría para la pantalla «Auditoría». Solo ADMIN; 403 para DOCENTE.</summary>
[ApiController]
[Route("api/auditoria")]
[Authorize(Roles = "ADMIN")]
public sealed class AuditoriaController(AuditoriaService auditoria) : ControllerBase
{
    /// <summary>
    /// Del más reciente al más antiguo. <c>accion</c> filtra por acción; <c>usuario</c> por nombre de usuario; <c>limite</c> va de 1
    /// a 500 (200 por defecto). 400 si el límite sale de ese rango.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<RespuestaAuditoria>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar([FromQuery] string? accion, [FromQuery] string? usuario, [FromQuery] int? limite, CancellationToken ct)
    {
        if (limite is < 1 or > AuditoriaService.LimiteMaximo)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"limite tiene que estar entre 1 y {AuditoriaService.LimiteMaximo}.");
        }

        return Ok(await auditoria.ListarAsync(accion, usuario, limite, ct));
    }
}
