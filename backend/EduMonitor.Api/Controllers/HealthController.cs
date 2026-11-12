using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>Comprobación de vida de la API. Pública; no consulta la base ni Moodle.</summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<RespuestaHealth> Get() => Ok(new RespuestaHealth("OK", DateTimeOffset.Now));

    public sealed record RespuestaHealth(string Estado, DateTimeOffset Fecha);
}
