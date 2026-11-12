using EduMonitor.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Auth;

public sealed class AuthService(IEduMonitorDbContext db, IHasherContrasenas hasher, IJwtService jwt)
{
    // Hash de una contraseña que nadie usa: si el usuario no existe se verifica igual, para que el tiempo de respuesta
    // no revele qué usuarios existen.
    private readonly Lazy<string> _hashSenuelo = new(() => hasher.Hashear(Guid.NewGuid().ToString()));

    /// <summary>Devuelve el token si usuario y contraseña coinciden; null en cualquier otro caso, sin decir cuál falló.</summary>
    public async Task<RespuestaLogin?> LoginAsync(SolicitudLogin solicitud, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.AsNoTracking()
            .SingleOrDefaultAsync(u => u.NombreUsuario == solicitud.Usuario, ct);

        if (usuario is null)
        {
            hasher.Verificar(_hashSenuelo.Value, solicitud.Contrasena);
            return null;
        }

        if (!hasher.Verificar(usuario.HashContrasena, solicitud.Contrasena))
        {
            return null;
        }

        var token = jwt.Emitir(usuario);
        return new RespuestaLogin(
            token.Token,
            token.ExpiraEn,
            new UsuarioSesion(usuario.Id, usuario.NombreUsuario, usuario.Nombre, usuario.Rol.ToString()));
    }
}
