using EduMonitor.Application.Configuracion;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;
using EduMonitor.Infrastructure.Seguridad;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EduMonitor.Tests.Seguridad;

public class JwtServiceTests
{
    private static readonly JwtOptions Opciones = new()
    {
        Secret = ApiFactory.SecretoJwtPrueba,
        Issuer = "EduMonitor",
        Audience = "EduMonitor",
        ExpiraMinutos = 120,
    };

    private static readonly Usuario Docente = new() { Id = 2, NombreUsuario = "doc01", Nombre = "Docente Uno", Rol = RolSistema.DOCENTE };

    [Fact]
    public async Task El_token_lleva_sub_name_role_y_vence_segun_la_configuracion()
    {
        var ahora = DateTimeOffset.UtcNow;
        var servicio = new JwtService(Options.Create(Opciones), new RelojFijo(ahora));

        var emitido = servicio.Emitir(Docente);

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(emitido.Token, JwtService.ParametrosValidacion(Opciones));
        Assert.True(resultado.IsValid, resultado.Exception?.Message);
        Assert.Equal("2", resultado.Claims["sub"]);
        Assert.Equal("doc01", resultado.Claims["name"]);
        Assert.Equal("DOCENTE", resultado.Claims["role"]);
        Assert.True(new System.Security.Claims.ClaimsPrincipal(resultado.ClaimsIdentity).IsInRole("DOCENTE"));

        var esperado = ahora.AddMinutes(120);
        Assert.Equal(esperado.ToUnixTimeSeconds(), emitido.ExpiraEn.ToUnixTimeSeconds());
        var vencimientoEnToken = new DateTimeOffset(((JsonWebToken)resultado.SecurityToken).ValidTo, TimeSpan.Zero);
        Assert.Equal(esperado.ToUnixTimeSeconds(), vencimientoEnToken.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task Un_token_vencido_no_es_valido()
    {
        var hace3Horas = DateTimeOffset.UtcNow.AddHours(-3);
        var servicio = new JwtService(Options.Create(Opciones), new RelojFijo(hace3Horas));

        var emitido = servicio.Emitir(Docente);

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(emitido.Token, JwtService.ParametrosValidacion(Opciones));
        Assert.False(resultado.IsValid);
    }

    [Fact]
    public async Task Un_token_firmado_con_otro_secreto_no_es_valido()
    {
        var otro = new JwtOptions { Secret = "otro-secreto-distinto-de-al-menos-32-caracteres", Issuer = "EduMonitor", Audience = "EduMonitor", ExpiraMinutos = 120 };
        var emitido = new JwtService(Options.Create(otro), TimeProvider.System).Emitir(Docente);

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(emitido.Token, JwtService.ParametrosValidacion(Opciones));
        Assert.False(resultado.IsValid);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
