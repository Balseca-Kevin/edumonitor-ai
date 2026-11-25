using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Actividades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>HU-22 · Panel de actividad: enunciado, adjuntos, entregas, distribución, dominio por criterio, temas y quién requiere atención.</summary>
[ApiController]
[Route("api/cursos/{id:int}/actividades/{aid:int}")]
[Authorize(Policy = CursoAutorizadoRequirement.Politica)]
public sealed class ActividadesController(ActividadService actividades) : ControllerBase
{
    /// <summary>401, 403 y 404 del curso por la política; 404 si la actividad no es del curso; 409 si el curso no se sincronizó.</summary>
    [HttpGet]
    [ProducesResponseType<ActividadDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Obtener(int id, int aid, CancellationToken ct)
    {
        var consulta = await actividades.ConsultarAsync(id, aid, ct);
        return consulta.Estado switch
        {
            EstadoConsultaActividad.ActividadNoExiste => Problem(statusCode: StatusCodes.Status404NotFound, detail: $"La actividad {aid} no es del curso {id}."),
            EstadoConsultaActividad.SinSincronizar => Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar",
                detail: "El curso todavía no tiene datos de Moodle. Sincronízalo para ver la actividad."),
            _ => Ok(consulta.Detalle),
        };
    }
}
