using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Evaluacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>HU-18 · TA-18.A1 · Evaluación de 18 preguntas con los dos modelos, para «Calidad del asistente». Solo ADMIN.</summary>
[ApiController]
[Route("api/admin/evaluacion-agente")]
[Authorize(Roles = "ADMIN")]
public sealed class EvaluacionController(EvaluacionService evaluaciones) : ControllerBase
{
    /// <summary>Inicia la evaluación y responde 202; corre en segundo plano. 400 sin cursoId; 404 curso inexistente; 409 si hay una en curso.</summary>
    [HttpPost]
    [Auditar("EVALUAR_ASISTENTE")]
    [ProducesResponseType<EvaluacionDto>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Iniciar([FromBody] PedidoEvaluacion pedido, CancellationToken ct)
    {
        var usuario = User.ComoUsuarioActual();
        if (usuario is null)
        {
            return Unauthorized();
        }

        var resultado = await evaluaciones.IniciarAsync(usuario, pedido.CursoId, ct);
        return resultado.Estado switch
        {
            EstadoIniciar.Iniciada => Accepted($"/api/admin/evaluacion-agente/{resultado.Evaluacion!.Id}", resultado.Evaluacion),
            EstadoIniciar.Invalido => Problem(statusCode: StatusCodes.Status400BadRequest, detail: resultado.Detalle),
            EstadoIniciar.CursoNoExiste => Problem(statusCode: StatusCodes.Status404NotFound, detail: resultado.Detalle),
            _ => Problem(statusCode: StatusCodes.Status409Conflict, title: "Evaluación en curso", detail: resultado.Detalle),
        };
    }

    /// <summary>Evaluaciones de la más reciente a la más antigua, con el resumen por modelo.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EvaluacionResumenDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await evaluaciones.ListarAsync(ct));

    /// <summary>Una evaluación con sus 18 preguntas y los resultados que ya llegaron. 404 si no existe.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<EvaluacionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct) =>
        await evaluaciones.ObtenerAsync(id, ct) is { } evaluacion
            ? Ok(evaluacion)
            : Problem(statusCode: StatusCodes.Status404NotFound, detail: $"La evaluación {id} no existe.");
}
