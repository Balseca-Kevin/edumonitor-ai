using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Fichas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>HU-10 · Ficha del estudiante: notas y estados por actividad, evolución frente a 7,0, temas débiles, comentarios y alertas.</summary>
[ApiController]
[Route("api/cursos/{id:int}/estudiantes/{eid:int}")]
[Authorize(Policy = CursoAutorizadoRequirement.Politica)]
public sealed class FichaEstudianteController(FichaEstudianteService fichas) : ControllerBase
{
    /// <summary>401, 403 y 404 del curso por la política; 404 si el estudiante no está matriculado; 409 si el curso no se sincronizó.</summary>
    [HttpGet]
    [ProducesResponseType<FichaEstudianteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Obtener(int id, int eid, CancellationToken ct)
    {
        var consulta = await fichas.ConsultarAsync(id, eid, ct);
        return consulta.Estado switch
        {
            EstadoConsultaFicha.EstudianteNoExiste => Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El estudiante {eid} no está matriculado en el curso {id}."),
            EstadoConsultaFicha.SinSincronizar => Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar",
                detail: "El curso todavía no tiene datos de Moodle. Sincronízalo para ver la ficha."),
            _ => Ok(consulta.Ficha),
        };
    }
}
