using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Cursos;
using EduMonitor.Application.Sincronizaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

[ApiController]
[Route("api/cursos/{id:int}/sync")]
[Authorize(Policy = CursoAutorizadoRequirement.Politica)]
public sealed class SincronizacionController(SincronizacionService sincronizacion, CursoService cursos) : ControllerBase
{
    /// <summary>
    /// Sincroniza el curso con Moodle y responde al terminar. 200 con estado COMPLETADA o PARCIAL;
    /// 502 si Moodle no respondió en ninguna función (el intento queda registrado como FALLIDA).
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SincronizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Sincronizar(int id, CancellationToken ct)
    {
        var usuario = User.ComoUsuarioActual();
        if (usuario is null)
        {
            return Unauthorized();
        }

        var resultado = await sincronizacion.SincronizarAsync(id, usuario, ct);
        if (resultado is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {id} no existe.");
        }

        if (resultado.Estado == "FALLIDA")
        {
            var problema = ProblemDetailsFactory.CreateProblemDetails(
                HttpContext,
                statusCode: StatusCodes.Status502BadGateway,
                detail: "Moodle no respondió; no se importó ningún dato. El intento quedó registrado en el historial.");
            problema.Extensions["sincronizacion"] = resultado;
            return new ObjectResult(problema) { StatusCode = StatusCodes.Status502BadGateway };
        }

        return Ok(resultado);
    }

    /// <summary>Historial de sincronizaciones del curso, de la más reciente a la más antigua.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SincronizacionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Historial(int id, CancellationToken ct) =>
        await cursos.ExisteAsync(id, ct)
            ? Ok(await sincronizacion.HistorialAsync(id, ct))
            : Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {id} no existe.");
}
