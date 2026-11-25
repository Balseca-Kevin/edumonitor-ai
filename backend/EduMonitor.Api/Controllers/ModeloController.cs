using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Agente;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>HU-23 · Modelos del asistente y el interruptor «Usar solo el modelo local». Solo ADMIN; 403 para DOCENTE.</summary>
[ApiController]
[Route("api/admin/modelo")]
[Authorize(Roles = "ADMIN")]
public sealed class ModeloController(ModeloAsistenteService modelo) : ControllerBase
{
    /// <summary>Los dos modelos, cuál responde ahora y si está activo «Usar solo el modelo local».</summary>
    [HttpGet]
    [ProducesResponseType<ModeloAsistenteDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(CancellationToken ct) => Ok(await modelo.ConsultarAsync(ct));

    /// <summary>Activa o desactiva «Usar solo el modelo local». Responde 200 con el estado nuevo.</summary>
    [HttpPut]
    [Auditar("CAMBIAR_MODELO")]
    [ProducesResponseType<ModeloAsistenteDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Cambiar([FromBody] CambioModeloDto cambio, CancellationToken ct)
    {
        await modelo.GuardarAsync(cambio.SoloLocal, ct);
        return Ok(await modelo.ConsultarAsync(ct));
    }
}
