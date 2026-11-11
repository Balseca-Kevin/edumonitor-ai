using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Cursos;
using EduMonitor.Application.Herramientas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>Herramientas que llama el agente de IA. Cada llamada queda registrada (herramienta, docente, fecha, resultado), sin el token.</summary>
[ApiController]
[Route("api/tools")]
[Authorize(Roles = "DOCENTE,ADMIN")]
public sealed class ToolsController(CursoService cursos, HerramientasService herramientas, TimeProvider reloj, ILogger<ToolsController> logger) : ControllerBase
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
            Registrar(CursoService.HerramientaCursosAutorizados, usuario.Id, null, "OK", respuesta.Cursos.Count);
            return Ok(respuesta);
        }
        catch (Exception)
        {
            Registrar(CursoService.HerramientaCursosAutorizados, usuario.Id, null, "ERROR", 0);
            throw;
        }
    }

    /// <summary>
    /// TA-13.1 · Notas y estados por actividad de un curso, o de un estudiante con <c>estudianteId</c>.
    /// 400 sin <c>cursoId</c>; 403 si el curso no es del docente; 404 si el curso no existe o el estudiante no está matriculado.
    /// </summary>
    [HttpGet("calificaciones")]
    [Authorize(Policy = CursoAutorizadoRequirement.Politica)]
    [ProducesResponseType<RespuestaCalificacionesHerramienta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Calificaciones([FromQuery] int? cursoId, [FromQuery] int? estudianteId, CancellationToken ct)
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

        try
        {
            var resultado = await herramientas.CalificacionesAsync(cursoId.Value, estudianteId, ct);
            if (resultado is null)
            {
                Registrar(HerramientasService.HerramientaCalificaciones, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {cursoId} no existe.");
            }

            if (resultado.EstudianteNoMatriculado)
            {
                Registrar(HerramientasService.HerramientaCalificaciones, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El estudiante {estudianteId} no está matriculado en el curso {cursoId}.");
            }

            Registrar(HerramientasService.HerramientaCalificaciones, usuario.Id, cursoId, "OK", resultado.Respuesta!.Estudiantes.Count);
            return Ok(resultado.Respuesta);
        }
        catch (Exception)
        {
            Registrar(HerramientasService.HerramientaCalificaciones, usuario.Id, cursoId, "ERROR", 0);
            throw;
        }
    }

    // Traza de cada llamada en el log de la API, sin el token. Las trazas que ve el docente se guardan con el chat (TRAZA_HERRAMIENTA, HU-14).
    private void Registrar(string herramienta, int docenteId, int? cursoId, string resultado, int cantidad) =>
        logger.LogInformation(
            "Herramienta {Herramienta} docente {DocenteId} curso {CursoId} fecha {Fecha:O} resultado {Resultado} registros {Cantidad}",
            herramienta, docenteId, cursoId, reloj.GetLocalNow(), resultado, cantidad);
}
