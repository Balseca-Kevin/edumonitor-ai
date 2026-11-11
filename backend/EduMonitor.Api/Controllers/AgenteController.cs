using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Agente;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>HU-14 · Chat del asistente: el navegador nunca habla con el agente; la API valida el curso y hace de proxy.</summary>
[ApiController]
[Route("api/agente")]
[Authorize(Roles = "DOCENTE,ADMIN")]
public sealed class AgenteController(AgenteService agente) : ControllerBase
{
    /// <summary>
    /// 200 con la respuesta y las herramientas usadas; 400 si el mensaje está vacío o pasa de 500 caracteres; 403 si el curso es ajeno
    /// (el agente no se llama); 404 si el curso no existe; 503 «Asistente no disponible» si el agente no responde en 60 s.
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType<RespuestaChatDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Chat([FromBody] PreguntaChatDto pregunta, CancellationToken ct)
    {
        var usuario = User.ComoUsuarioActual();
        if (usuario is null)
        {
            return Unauthorized();
        }

        var resultado = await agente.PreguntarAsync(usuario, pregunta.CursoId, pregunta.Mensaje, ct);
        return resultado.Estado switch
        {
            EstadoChat.Respondido => Ok(resultado.Respuesta),
            EstadoChat.Invalido => Problem(statusCode: StatusCodes.Status400BadRequest, detail: resultado.Detalle),
            EstadoChat.CursoNoExiste => Problem(statusCode: StatusCodes.Status404NotFound, detail: resultado.Detalle),
            EstadoChat.Prohibido => Problem(statusCode: StatusCodes.Status403Forbidden, detail: resultado.Detalle),
            _ => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Asistente no disponible", detail: resultado.Detalle),
        };
    }
}
