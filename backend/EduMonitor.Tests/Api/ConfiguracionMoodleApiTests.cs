using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Configuracion;
using EduMonitor.Tests.Infraestructura;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>TA-02.3 · GET/PUT /api/admin/moodle-config y POST /api/admin/moodle-config/test (docs/contratos-api.md).</summary>
public class ConfiguracionMoodleApiTests
{
    private const string TokenInicial = "token-inicial-de-pruebas-0000aaaa";

    private sealed class ApiConMoodleFalso : ApiConDatosFactory
    {
        public MoodleClientFalso Moodle { get; } = MoodleClientFalso.ConCursoDeEjemplo();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Moodle:Url", "http://localhost:8080");
            builder.UseSetting("Moodle:Token", TokenInicial);
        }

        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IMoodleClient>();
            services.AddSingleton<IMoodleClient>(Moodle);
        }
    }

    [Fact]
    public async Task Get_devuelve_url_servicio_y_token_enmascarado()
    {
        await using var api = new ApiConMoodleFalso();
        var admin = await api.ClienteConSesionAsync("admin");

        var respuesta = await admin.GetAsync("/api/admin/moodle-config");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var texto = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain(TokenInicial, texto);
        var cuerpo = JsonDocument.Parse(texto).RootElement;
        Assert.Equal("http://localhost:8080", cuerpo.GetProperty("url").GetString());
        Assert.Equal("edumonitor_ws", cuerpo.GetProperty("servicio").GetString());
        Assert.Equal("••••aaaa", cuerpo.GetProperty("tokenEnmascarado").GetString());
        Assert.True(cuerpo.TryGetProperty("actualizado", out _));
    }

    [Fact]
    public async Task Un_docente_recibe_403_y_sin_token_401()
    {
        await using var api = new ApiConMoodleFalso();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var anonimo = api.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync("/api/admin/moodle-config")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.PostAsync("/api/admin/moodle-config/test", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.PutAsJsonAsync("/api/admin/moodle-config", new { url = "http://x.test", token = "t" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/admin/moodle-config")).StatusCode);
    }

    [Fact]
    public async Task Put_cambia_url_y_token_y_responde_204()
    {
        await using var api = new ApiConMoodleFalso();
        var admin = await api.ClienteConSesionAsync("admin");

        var respuesta = await admin.PutAsJsonAsync("/api/admin/moodle-config", new { url = "http://moodle.lab.test/", token = "token-nuevo-de-pruebas-1111bbbb" });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var cuerpo = await admin.GetFromJsonAsync<JsonElement>("/api/admin/moodle-config");
        Assert.Equal("http://moodle.lab.test", cuerpo.GetProperty("url").GetString());
        Assert.Equal("••••bbbb", cuerpo.GetProperty("tokenEnmascarado").GetString());
        var actual = api.Services.GetRequiredService<IConfiguracionMoodle>().Actual;
        Assert.Equal("token-nuevo-de-pruebas-1111bbbb", actual.Token);
        Assert.DoesNotContain(api.Logs.Mensajes, m => m.Contains("token-nuevo-de-pruebas-1111bbbb", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Put_sin_token_conserva_el_actual()
    {
        await using var api = new ApiConMoodleFalso();
        var admin = await api.ClienteConSesionAsync("admin");

        await admin.PutAsJsonAsync("/api/admin/moodle-config", new { url = "https://moodle.lab.test" });

        Assert.Equal(TokenInicial, api.Services.GetRequiredService<IConfiguracionMoodle>().Actual.Token);
    }

    [Theory]
    [InlineData("no-es-una-url")]
    [InlineData("ftp://moodle.lab.test")]
    [InlineData("")]
    public async Task Put_con_url_invalida_responde_400_y_no_cambia_nada(string url)
    {
        await using var api = new ApiConMoodleFalso();
        var admin = await api.ClienteConSesionAsync("admin");

        var respuesta = await admin.PutAsJsonAsync("/api/admin/moodle-config", new { url, token = "otro-token-0000cccc" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("Datos de entrada inválidos", (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
        Assert.Equal("http://localhost:8080", api.Services.GetRequiredService<IConfiguracionMoodle>().Actual.Url);
    }

    [Fact]
    public async Task Test_informa_sitio_version_y_cada_funcion_permitida()
    {
        await using var api = new ApiConMoodleFalso();
        api.Moodle.FuncionesSitio.AddRange(ConfiguracionMoodleService.FuncionesPermitidas.Where(f => f != "core_completion_get_activities_completion_status"));
        var admin = await api.ClienteConSesionAsync("admin");

        var respuesta = await admin.PostAsync("/api/admin/moodle-config/test", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(cuerpo.GetProperty("conectado").GetBoolean());
        Assert.Equal("Moodle-Lab-UTA", cuerpo.GetProperty("sitio").GetString());
        Assert.Equal("4.5", cuerpo.GetProperty("version").GetString());
        Assert.Equal("edumonitor_ws_user", cuerpo.GetProperty("usuarioTecnico").GetString());
        var funciones = cuerpo.GetProperty("funciones").EnumerateArray()
            .ToDictionary(f => f.GetProperty("nombre").GetString()!, f => f.GetProperty("disponible").GetBoolean());
        Assert.Equal(8, funciones.Count);
        Assert.False(funciones["core_completion_get_activities_completion_status"]);
        Assert.Equal(7, funciones.Values.Count(d => d));
    }

    [Fact]
    public async Task Test_con_Moodle_caido_responde_200_con_conectado_false_y_error()
    {
        await using var api = new ApiConMoodleFalso();
        api.Moodle.Fallar.Add("core_webservice_get_site_info");
        var admin = await api.ClienteConSesionAsync("admin");

        var respuesta = await admin.PostAsync("/api/admin/moodle-config/test", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(cuerpo.GetProperty("conectado").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(cuerpo.GetProperty("error").GetString()));
    }
}
