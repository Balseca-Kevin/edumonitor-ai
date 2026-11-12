using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Alertas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>
/// HU-12 · Reglas configurables y alertas del curso. Las alertas las calcula el backend con código determinista
/// (TA-12.1); el modelo de IA nunca interviene y el título de toda alerta es «Requiere atención».
/// </summary>
[ApiController]
[Route("api/cursos/{id:int}")]
[Authorize(Policy = CursoAutorizadoRequirement.Politica)]
public sealed class AlertasController(AlertasService alertas) : ControllerBase
{
    /// <summary>200 con las tres reglas del curso (se crean por defecto en la primera consulta); 404 si el curso no existe.</summary>
    [HttpGet("reglas")]
    [ProducesResponseType<IReadOnlyList<ReglaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerReglas(int id, CancellationToken ct)
    {
        var reglas = await alertas.ObtenerReglasAsync(id, ct);
        return reglas is null ? CursoNoExiste(id) : Ok(reglas);
    }

    /// <summary>204 y recálculo de las alertas; 400 si el umbral sale de rango o el código no existe; 404 si el curso no existe.</summary>
    [HttpPut("reglas")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActualizarReglas(int id, [FromBody] IReadOnlyList<CambioRegla> cambios, CancellationToken ct)
    {
        var resultado = await alertas.ActualizarReglasAsync(id, cambios, ct);
        if (!resultado.CursoExiste)
        {
            return CursoNoExiste(id);
        }

        return resultado.Detalle is null ? NoContent() : Problem(statusCode: StatusCodes.Status400BadRequest, detail: resultado.Detalle);
    }

    /// <summary>200 con las alertas del estado (ACTIVA por defecto); 400 si el estado no es ACTIVA ni REVISADA; 404 si el curso no existe.</summary>
    [HttpGet("alertas")]
    [ProducesResponseType<IReadOnlyList<AlertaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerAlertas(int id, [FromQuery] string? estado, CancellationToken ct)
    {
        var consulta = await alertas.ObtenerAlertasAsync(id, estado, ct);
        return consulta.Estado switch
        {
            EstadoConsultaAlertas.CursoNoExiste => CursoNoExiste(id),
            EstadoConsultaAlertas.EstadoInvalido => Problem(statusCode: StatusCodes.Status400BadRequest, detail: consulta.Detalle),
            _ => Ok(consulta.Alertas),
        };
    }

    /// <summary>200 con la alerta actualizada; 404 si la alerta no es de ese curso; 400 si el estado no es REVISADA.</summary>
    [HttpPatch("alertas/{alertaId:int}")]
    [ProducesResponseType<AlertaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarAlerta(int id, int alertaId, [FromBody] CambioAlerta cambio, CancellationToken ct)
    {
        var resultado = await alertas.MarcarAsync(id, alertaId, cambio.Estado, ct);
        return resultado.Estado switch
        {
            EstadoResultadoAlerta.NoExiste =>
                Problem(statusCode: StatusCodes.Status404NotFound, detail: $"La alerta {alertaId} no existe en el curso {id}."),
            EstadoResultadoAlerta.EstadoInvalido =>
                Problem(statusCode: StatusCodes.Status400BadRequest, detail: resultado.Detalle),
            _ => Ok(resultado.Alerta),
        };
    }

    private ObjectResult CursoNoExiste(int id) =>
        Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {id} no existe.");
}
