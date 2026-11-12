using EduMonitor.Application.Estado;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>HU-20 · Estado del sistema para el ADMIN: api, sqlserver, moodle, agente y modelo. 403 para DOCENTE.</summary>
[ApiController]
[Route("api/admin/estado")]
[Authorize(Roles = "ADMIN")]
public sealed class EstadoController(EstadoSistemaService estado) : ControllerBase
{
    /// <summary>Cada servicio se consulta con un máximo de 3 s; uno caído no detiene a los demás.</summary>
    [HttpGet]
    [ProducesResponseType<EstadoSistemaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(CancellationToken ct) => Ok(await estado.ConsultarAsync(ct));
}
