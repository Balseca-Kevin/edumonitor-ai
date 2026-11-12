using EduMonitor.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Inicia sesión. 200 con el JWT; 401 genérico si el usuario o la contraseña no coinciden.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<RespuestaLogin>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(SolicitudLogin solicitud, CancellationToken ct)
    {
        var respuesta = await auth.LoginAsync(solicitud, ct);
        if (respuesta is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciales incorrectas",
                detail: "El usuario o la contraseña no son válidos.");
        }

        return Ok(respuesta);
    }
}
