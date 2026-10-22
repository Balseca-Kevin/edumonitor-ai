using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Cursos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>Herramientas que llama el agente de IA con el JWT del docente. Cada llamada queda registrada, sin el token.</summary>
[ApiController]
[Route("api/tools")]
[Authorize(Roles = "DOCENTE,ADMIN")]
public sealed class ToolsController(CursoService cursos, TimeProvider reloj, ILogger<ToolsController> logger) : ControllerBase
{
    [HttpGet("cursos-autorizados")]
    [ProducesResponseType<RespuestaCursosAutorizados>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CursosAutorizados(CancellationToken ct)
    {
        var usuario = User.ComoUsuarioActual();
        if (usuario is null)
        {
            return Unauthorized();
        }

        try
        {
            var respuesta = await cursos.CursosAutorizadosAsync(usuario, ct);
            Registrar(usuario.Id, "OK", respuesta.Cursos.Count);
            return Ok(respuesta);
        }
        catch (Exception)
        {
            Registrar(usuario.Id, "ERROR", 0);
            throw;
        }
    }

    // Traza mínima del Sprint 1 en el log de la API. La tabla TRAZA_HERRAMIENTA llega con el proxy del agente (Sprint 2).
    private void Registrar(int usuarioId, string resultado, int cantidad) =>
        logger.LogInformation(
            "Herramienta {Herramienta} usuario {UsuarioId} fecha {Fecha:O} resultado {Resultado} cursos {Cantidad}",
            CursoService.HerramientaCursosAutorizados, usuarioId, reloj.GetLocalNow(), resultado, cantidad);
}
