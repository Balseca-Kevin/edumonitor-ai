using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Cursos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

[ApiController]
[Route("api/cursos")]
[Authorize(Roles = "DOCENTE,ADMIN")]
public sealed class CursosController(CursoService cursos) : ControllerBase
{
    /// <summary>Cursos asignados al docente del token (un ADMIN recibe todos).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CursoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? periodo, CancellationToken ct)
    {
        var usuario = User.ComoUsuarioActual();
        if (usuario is null)
        {
            return Unauthorized();
        }

        return Ok(await cursos.ListarAsync(usuario, busqueda, periodo, ct));
    }

    /// <summary>Un curso. 403 si no está asignado al docente; 404 si no existe.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = CursoAutorizadoRequirement.Politica)]
    [ProducesResponseType<CursoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var curso = await cursos.ObtenerAsync(id, ct);
        return curso is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {id} no existe.")
            : Ok(curso);
    }
}
