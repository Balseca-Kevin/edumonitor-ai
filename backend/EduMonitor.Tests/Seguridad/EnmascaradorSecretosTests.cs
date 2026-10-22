using EduMonitor.Application.Configuracion;
using EduMonitor.Application.Seguridad;

namespace EduMonitor.Tests.Seguridad;

public class EnmascaradorSecretosTests
{
    private const string TokenPrueba = "0f1e2d3c4b5a69788796a5b4c3d23f9a";

    [Fact]
    public void Muestra_solo_los_ultimos_4_caracteres()
    {
        Assert.Equal("••••3f9a", EnmascaradorSecretos.Enmascarar(TokenPrueba));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("abc", "••••")]
    [InlineData("12345678", "••••")]
    [InlineData("123456789", "••••6789")]
    public void Secretos_vacios_o_cortos_no_se_revelan(string? secreto, string esperado)
    {
        Assert.Equal(esperado, EnmascaradorSecretos.Enmascarar(secreto));
    }

    [Fact]
    public void MoodleOptions_ToString_no_contiene_el_token_completo()
    {
        var opciones = new MoodleOptions { Url = "http://localhost:8080", Token = TokenPrueba };

        var texto = opciones.ToString();

        Assert.DoesNotContain(TokenPrueba, texto);
        Assert.Contains("••••3f9a", texto);
        Assert.Equal("••••3f9a", opciones.TokenEnmascarado);
    }
}
