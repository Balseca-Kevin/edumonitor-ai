using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Informes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>HU-16 · Informe del curso en PDF con los indicadores y el resumen de una respuesta del asistente.</summary>
[ApiController]
[Route("api/cursos/{id:int}/informe")]
[Authorize(Policy = CursoAutorizadoRequirement.Politica)]
public sealed class InformeController(InformeService informes) : ControllerBase
{
    /// <summary>
    /// 400 si el formato no es pdf o falta conversacionId; 404 si la conversación no es del docente en ese curso; 409 si el curso no se
    /// sincronizó. 401, 403 y 404 del curso por la política.
    /// </summary>
    [HttpGet]
    [Auditar("DESCARGAR_DOCUMENTO")]
    [Produces("application/pdf")]
    public async Task<IActionResult> Descargar(int id, [FromQuery] string? formato, [FromQuery] int? conversacionId, CancellationToken ct)
    {
        var usuario = User.ComoUsuarioActual();
        if (usuario is null)
        {
            return Unauthorized();
        }

        var resultado = await informes.GenerarAsync(usuario, id, formato, conversacionId, ct);
        return resultado.Estado switch
        {
            EstadoInforme.Generado => File(resultado.Contenido!, "application/pdf", resultado.Nombre),
            EstadoInforme.Invalido => Problem(statusCode: StatusCodes.Status400BadRequest, detail: resultado.Detalle),
            EstadoInforme.NoEncontrado => Problem(statusCode: StatusCodes.Status404NotFound, detail: resultado.Detalle),
            _ => Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar", detail: resultado.Detalle),
        };
    }
}
