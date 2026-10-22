using System.Security.Claims;
using System.Text;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Configuracion;
using EduMonitor.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EduMonitor.Infrastructure.Seguridad;

public sealed class JwtService(IOptions<JwtOptions> opciones, TimeProvider reloj) : IJwtService
{
    public const string ClaimId = JwtRegisteredClaimNames.Sub;
    public const string ClaimNombre = "name";
    public const string ClaimRol = "role";

    public TokenEmitido Emitir(Usuario usuario)
    {
        var jwt = opciones.Value;
        var ahora = reloj.GetUtcNow();
        var expira = ahora.AddMinutes(jwt.ExpiraMinutos);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = expira.UtcDateTime,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimId, usuario.Id.ToString()),
                new Claim(ClaimNombre, usuario.NombreUsuario),
                new Claim(ClaimRol, usuario.Rol.ToString()),
            }),
            SigningCredentials = new SigningCredentials(ClaveFirma(jwt), SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new TokenEmitido(token, expira.ToLocalTime());
    }

    /// <summary>Parámetros con los que la API valida el token. Se comparten con las pruebas para que no se desalineen.</summary>
    public static TokenValidationParameters ParametrosValidacion(JwtOptions jwt) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = ClaveFirma(jwt),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = ClaimNombre,
        RoleClaimType = ClaimRol,
    };

    private static SymmetricSecurityKey ClaveFirma(JwtOptions jwt) => new(Encoding.UTF8.GetBytes(jwt.Secret));
}
