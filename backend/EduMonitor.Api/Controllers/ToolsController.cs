using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Cursos;
using EduMonitor.Application.Herramientas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>
/// Herramientas que llama el agente de IA, con el JWT del docente o con el token de servicio (rol AGENTE) que emite el chat (TA-14.2).
/// Cada llamada queda registrada (herramienta, docente, fecha, resultado), sin el token.
/// </summary>
[ApiController]
[Route("api/tools")]
[Authorize(Roles = "DOCENTE,ADMIN," + TokenAgente.Rol)]
public sealed class ToolsController(CursoService cursos, HerramientasService herramientas, TimeProvider reloj, ILogger<ToolsController> logger) : ControllerBase
{
    [HttpGet("cursos-autorizados")]
    [ProducesResponseType<RespuestaCursosAutorizados>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CursosAutorizados(CancellationToken ct)
    {
        var usuario = await User.ResolverUsuarioAsync(cursos, ct);
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
    /// TA-13.1 · Resumen por estudiante de un curso; detalle por actividad de un estudiante con <c>estudianteId</c> o con <c>nombre</c>
    /// (nombre o apellido, sin tildes ni mayúsculas). Varias coincidencias de nombre devuelven la lista corta para elegir.
    /// 400 sin <c>cursoId</c>; 403 si el curso no es del docente; 404 si el curso no existe o ningún estudiante coincide.
    /// </summary>
    [HttpGet("calificaciones")]
    [Authorize(Policy = CursoAutorizadoRequirement.PoliticaHerramientas)]
    [ProducesResponseType<RespuestaCalificacionesHerramienta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Calificaciones([FromQuery] int? cursoId, [FromQuery] int? estudianteId, [FromQuery] string? nombre, CancellationToken ct)
    {
        var usuario = await User.ResolverUsuarioAsync(cursos, ct);
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
            var resultado = await herramientas.CalificacionesAsync(cursoId.Value, estudianteId, nombre, ct);
            if (resultado is null)
            {
                Registrar(HerramientasService.HerramientaCalificaciones, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {cursoId} no existe.");
            }

            if (resultado.EstudianteNoMatriculado)
            {
                Registrar(HerramientasService.HerramientaCalificaciones, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                var detalle = estudianteId is not null
                    ? $"El estudiante {estudianteId} no está matriculado en el curso {cursoId}."
                    : $"Ningún estudiante del curso {cursoId} se llama «{nombre?.Trim()}».";
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: detalle);
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
