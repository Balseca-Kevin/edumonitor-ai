using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Indicadores;
using EduMonitor.Application.Libro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>HU-21 · Libro de calificaciones: la nota de cada estudiante en cada actividad, con los datos de la última sincronización.</summary>
[ApiController]
[Route("api/cursos/{id:int}/libro")]
[Authorize(Policy = CursoAutorizadoRequirement.Politica)]
public sealed class LibroController(LibroService libro) : ControllerBase
{
    /// <summary>
    /// 200 con el libro; 400 si <c>orden</c> no es <c>apellido</c> ni <c>promedio</c>; 403 si el curso es ajeno; 404 si no existe;
    /// 409 si nunca se sincronizó.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<LibroCalificacionesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Obtener(int id, [FromQuery] string? busqueda, [FromQuery] string? orden, [FromQuery] string? tramo, CancellationToken ct)
    {
        if (!Tramo.TryParse(tramo, out var elegido))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "El tramo solo acepta todo, p1, p2 o u1 a un.");
        }

        OrdenLibro ordenLibro;
        switch (orden?.Trim().ToLowerInvariant())
        {
            case null or "" or "apellido":
                ordenLibro = OrdenLibro.Apellido;
                break;
            case "promedio":
                ordenLibro = OrdenLibro.Promedio;
                break;
            default:
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "El parámetro orden solo acepta «apellido» o «promedio».");
        }

        var consulta = await libro.ConsultarAsync(id, busqueda, ordenLibro, elegido, ct);
        return consulta.Estado switch
        {
            EstadoConsultaLibro.CursoNoExiste =>
                Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {id} no existe."),
            EstadoConsultaLibro.SinSincronizar =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar",
                    detail: "El curso todavía no tiene datos de Moodle. Sincronízalo desde Mis cursos para ver su libro de calificaciones."),
            EstadoConsultaLibro.TramoInvalido => Problem(statusCode: StatusCodes.Status400BadRequest, detail: consulta.Detalle),
            _ => Ok(consulta.Libro),
        };
    }
}
