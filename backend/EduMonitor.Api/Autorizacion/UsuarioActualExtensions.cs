using System.Security.Claims;
using EduMonitor.Application.Abstractions;
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

    /// <summary>True si el token es el de servicio del agente (rol AGENTE).</summary>
    public static bool EsAgente(this ClaimsPrincipal principal) => principal.FindFirstValue("role") == TokenAgente.Rol;

    /// <summary>
    /// Usuario con el que se autoriza la petición. Con el JWT del docente, el de <c>sub</c> y <c>role</c>; con el token de servicio
    /// del agente, el docente del claim <c>docente</c> con su rol real (TA-14.2). Null si el token no trae un usuario válido.
    /// </summary>
    public static async Task<UsuarioActual?> ResolverUsuarioAsync(this ClaimsPrincipal principal, CursoService cursos, CancellationToken ct)
    {
        if (!principal.EsAgente())
        {
            return principal.ComoUsuarioActual();
        }

        return int.TryParse(principal.FindFirstValue(TokenAgente.ClaimDocente), out var docenteId)
            ? await cursos.UsuarioDelAgenteAsync(docenteId, ct)
            : null;
    }
}
