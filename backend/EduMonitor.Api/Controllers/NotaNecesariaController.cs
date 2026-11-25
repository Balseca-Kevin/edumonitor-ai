using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.NotaNecesaria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>TA-10.A1 · Nota mínima en las actividades pendientes para llegar a 7,0 con las notas actuales.</summary>
[ApiController]
[Route("api/cursos/{id:int}/estudiantes/{eid:int}/nota-necesaria")]
[Authorize(Policy = CursoAutorizadoRequirement.Politica)]
public sealed class NotaNecesariaController(NotaNecesariaService notaNecesaria) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<NotaNecesariaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Obtener(int id, int eid, CancellationToken ct)
    {
        var consulta = await notaNecesaria.ConsultarAsync(id, eid, ct);
        return consulta.Estado switch
        {
            EstadoConsultaNotaNecesaria.CursoNoExiste => Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {id} no existe."),
            EstadoConsultaNotaNecesaria.EstudianteNoEncontrado => Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El estudiante {eid} no está matriculado en el curso {id}."),
            EstadoConsultaNotaNecesaria.SinSincronizar => Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar",
                detail: "El curso todavía no tiene datos de Moodle. Sincronízalo para calcular la nota necesaria."),
            _ => Ok(consulta.Resultado),
        };
    }
}
