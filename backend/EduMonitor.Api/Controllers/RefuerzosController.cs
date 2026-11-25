using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Cursos;
using EduMonitor.Application.Refuerzos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>
/// HU-15 · TA-15.A3 y TA-15.A4 · Planes de refuerzo del curso: el docente lo arma, el asistente redacta el borrador y el docente lo
/// aprueba. Contrato en docs/contratos-api.md, «Plan de refuerzo».
/// </summary>
[ApiController]
[Route("api/cursos/{id:int}/refuerzos")]
[Authorize(Policy = CursoAutorizadoRequirement.Politica)]
public sealed class RefuerzosController(PlanRefuerzoService planes, CursoService cursos) : ControllerBase
{
    [HttpPost]
    [Auditar("CREAR_REFUERZO")]
    [ProducesResponseType<PlanRefuerzoDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(int id, [FromBody] PedidoPlanRefuerzo pedido, CancellationToken ct)
    {
        var usuario = User.ComoUsuarioActual();
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (!await cursos.ExisteAsync(id, ct))
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {id} no existe.");
        }

        var resultado = await planes.CrearAsync(usuario, id, pedido, ct);
        return resultado.Estado == EstadoOperacionPlan.Correcta
            ? Created($"/api/cursos/{id}/refuerzos/{resultado.Valor!.Id}", resultado.Valor)
            : Responder(resultado);
    }

    /// <summary>Historial de planes del curso, del más reciente al más antiguo, con su estado y el resultado de los aprobados.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PlanRefuerzoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(int id, CancellationToken ct) =>
        await cursos.ExisteAsync(id, ct)
            ? Ok(await planes.ListarAsync(id, ct))
            : Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {id} no existe.");

    /// <summary>TA-15.A2 · Refuerzos aplicados: nota del tema antes y después de cada refuerzo aprobado, con los conteos.</summary>
    [HttpGet("aplicados")]
    [ProducesResponseType<RefuerzosAplicadosDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Aplicados(int id, CancellationToken ct) => Ok(await planes.AplicadosAsync(id, ct));

    [HttpGet("{rid:int}")]
    [ProducesResponseType<PlanRefuerzoDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(int id, int rid, CancellationToken ct) => Responder(await planes.ObtenerAsync(id, rid, ct));

    /// <summary>TA-15.A4 · Edita el texto del borrador o lo aprueba (estado APROBADO).</summary>
    [HttpPatch("{rid:int}")]
    [Auditar("EDITAR_REFUERZO")]
    [ProducesResponseType<PlanRefuerzoDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Cambiar(int id, int rid, [FromBody] PedidoCambioPlan cambio, CancellationToken ct) =>
        Responder(await planes.CambiarAsync(id, rid, cambio, ct));

    /// <summary>TA-15.A4 · Documento del plan aprobado. 400 formato inválido; 404 plan ajeno; 409 si todavía no está aprobado.</summary>
    [HttpGet("{rid:int}/documento")]
    [Auditar("DESCARGAR_DOCUMENTO")]
    [Produces("application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public async Task<IActionResult> Documento(int id, int rid, [FromQuery] string? formato, CancellationToken ct)
    {
        var resultado = await planes.DocumentoAsync(id, rid, formato, ct);
        if (resultado.Estado != EstadoOperacionPlan.Correcta)
        {
            return resultado.Estado == EstadoOperacionPlan.YaAprobado
                ? Problem(statusCode: StatusCodes.Status409Conflict, title: "Plan sin aprobar", detail: resultado.Detalle)
                : Responder(resultado);
        }

        var (contenido, tipo, nombre) = resultado.Valor;
        return File(contenido, tipo, nombre);
    }

    private IActionResult Responder<T>(ResultadoPlan<T> resultado) => resultado.Estado switch
    {
        EstadoOperacionPlan.Correcta => Ok(resultado.Valor),
        EstadoOperacionPlan.NoEncontrado => Problem(statusCode: StatusCodes.Status404NotFound, detail: resultado.Detalle),
        EstadoOperacionPlan.YaAprobado => Problem(statusCode: StatusCodes.Status409Conflict, title: "Plan aprobado", detail: resultado.Detalle),
        _ => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Datos inválidos", detail: resultado.Detalle),
    };
}
