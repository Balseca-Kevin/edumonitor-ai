using EduMonitor.Application.Abstractions;
using EduMonitor.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace EduMonitor.Infrastructure.Seguridad;

/// <summary>PasswordHasher de ASP.NET Core Identity (PBKDF2 con sal aleatoria).</summary>
public sealed class HasherContrasenas : IHasherContrasenas
{
    private static readonly PasswordHasher<Usuario> Hasher = new();
    private static readonly Usuario SinUso = new();

    public string Hashear(string contrasena) => Hasher.HashPassword(SinUso, contrasena);

    public bool Verificar(string hash, string contrasena) =>
        Hasher.VerifyHashedPassword(SinUso, hash, contrasena) is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
}
