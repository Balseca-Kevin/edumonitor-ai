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

    /// <summary>HU-19 · Historial del docente en un curso, de la más reciente a la más antigua. 400 sin cursoId; 403 curso ajeno; 404 curso inexistente.</summary>
    [HttpGet("conversaciones")]
    [ProducesResponseType<IReadOnlyList<ConversacionResumenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Conversaciones([FromQuery] int? cursoId, CancellationToken ct)
    {
        var usuario = User.ComoUsuarioActual();
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (cursoId is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Falta el parámetro cursoId.");
        }

        var (estado, conversaciones) = await agente.ListarConversacionesAsync(usuario, cursoId.Value, ct);
        return estado switch
        {
            EstadoChat.Respondido => Ok(conversaciones),
            EstadoChat.CursoNoExiste => Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {cursoId} no existe."),
            _ => Problem(statusCode: StatusCodes.Status403Forbidden, detail: $"El curso {cursoId} no está asignado a este docente."),
        };
    }

    /// <summary>HU-19 · Una conversación con su respuesta y herramientas. 403 si el curso es ajeno o la conversación es de otro docente; 404 si no existe.</summary>
    [HttpGet("conversaciones/{id:int}")]
    [ProducesResponseType<ConversacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Conversacion(int id, CancellationToken ct)
    {
        var usuario = User.ComoUsuarioActual();
        if (usuario is null)
        {
            return Unauthorized();
        }

        var (estado, conversacion) = await agente.ObtenerConversacionAsync(usuario, id, ct);
        return estado switch
        {
            EstadoChat.Respondido => Ok(conversacion),
            EstadoChat.CursoNoExiste => Problem(statusCode: StatusCodes.Status404NotFound, detail: $"La conversación {id} no existe."),
            _ => Problem(statusCode: StatusCodes.Status403Forbidden, detail: $"La conversación {id} no es de este docente."),
        };
    }
}
