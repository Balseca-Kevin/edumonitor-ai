using EduMonitor.Application.Configuracion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>HU-02 · Configuración de la conexión con Moodle. Solo ADMIN; el token completo nunca sale en la respuesta.</summary>
[ApiController]
[Route("api/admin/moodle-config")]
[Authorize(Roles = "ADMIN")]
public sealed class ConfiguracionMoodleController(ConfiguracionMoodleService configuracion) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ConfiguracionMoodleDto>(StatusCodes.Status200OK)]
    public IActionResult Obtener() => Ok(configuracion.Obtener());

    /// <summary>Cambia la URL y el token de Moodle mientras corre la API. 400 si la URL no es http(s) absoluta.</summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Actualizar([FromBody] ActualizarConfiguracionMoodle cuerpo) =>
        configuracion.Actualizar(cuerpo.Url, cuerpo.Token)
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status400BadRequest, detail: "La URL de Moodle debe ser absoluta y empezar por http:// o https://.");

    /// <summary>Prueba la conexión con core_webservice_get_site_info. Siempre 200: si Moodle no responde, conectado = false.</summary>
    [HttpPost("test")]
    [ProducesResponseType<PruebaConexionMoodleDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Probar(CancellationToken ct) => Ok(await configuracion.ProbarAsync(ct));
}
