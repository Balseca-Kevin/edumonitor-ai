using System.ComponentModel.DataAnnotations;

namespace EduMonitor.Application.Auth;

/// <summary>Cuerpo de <c>POST /api/auth/login</c>.</summary>
public sealed record SolicitudLogin(
    [Required(ErrorMessage = "El usuario es obligatorio.")] string Usuario,
    [Required(ErrorMessage = "La contraseña es obligatoria.")] string Contrasena);

/// <summary>Respuesta 200 de <c>POST /api/auth/login</c> (docs/contratos-api.md).</summary>
public sealed record RespuestaLogin(string Token, DateTimeOffset ExpiraEn, UsuarioSesion Usuario);

public sealed record UsuarioSesion(int Id, string Usuario, string Nombre, string Rol);
