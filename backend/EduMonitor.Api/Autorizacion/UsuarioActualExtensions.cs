using System.Security.Claims;
using EduMonitor.Application.Cursos;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Api.Autorizacion;

public static class UsuarioActualExtensions
{
    /// <summary>Lee <c>sub</c> y <c>role</c> del JWT ya validado. Null si faltan o no son válidos.</summary>
    public static UsuarioActual? ComoUsuarioActual(this ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue("sub");
        var rol = principal.FindFirstValue("role");
        return int.TryParse(id, out var usuarioId) && Enum.TryParse<RolSistema>(rol, ignoreCase: false, out var rolSistema)
            ? new UsuarioActual(usuarioId, rolSistema)
            : null;
    }
}
