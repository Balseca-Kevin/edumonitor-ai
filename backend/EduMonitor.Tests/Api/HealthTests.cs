using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Tests.Infraestructura;

namespace EduMonitor.Tests.Api;

public class HealthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_sin_token_responde_200_con_estado_OK()
    {
        var cliente = factory.CreateClient();

        var respuesta = await cliente.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("OK", cuerpo.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Ruta_inexistente_responde_404_con_ProblemDetails()
    {
        var cliente = factory.CreateClient();

        var respuesta = await cliente.GetAsync("/api/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://httpstatuses.io/404", cuerpo.GetProperty("type").GetString());
        Assert.Equal("Recurso no encontrado", cuerpo.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Cors_permite_el_origen_del_frontend()
    {
        var cliente = factory.CreateClient();
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        peticion.Headers.Add("Origin", "http://localhost:5173");

        var respuesta = await cliente.SendAsync(peticion);

        Assert.Equal("http://localhost:5173", respuesta.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
