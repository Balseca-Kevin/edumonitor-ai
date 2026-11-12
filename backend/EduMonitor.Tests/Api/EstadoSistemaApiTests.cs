using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Agente;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// TA-20.5 · GET /api/admin/estado. DADO el sistema encendido, CUANDO el ADMIN abre «Estado del sistema», ENTONCES ve api, sqlserver,
/// moodle, agente y modelo con OK o NO_DISPONIBLE; un servicio caído no detiene a los demás y cada consulta tarda como máximo 3 s.
/// </summary>
public class EstadoSistemaApiTests
{
    private sealed class ApiConServiciosFalsos : ApiConDatosFactory
    {
        public MoodleClientFalso Moodle { get; } = MoodleClientFalso.ConCursoDeEjemplo();
        public AgenteClientFalso Agente { get; } = new();

        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IMoodleClient>();
            services.AddSingleton<IMoodleClient>(Moodle);
            services.RemoveAll<IAgenteClient>();
            services.AddSingleton<IAgenteClient>(Agente);
        }
    }

    private static Dictionary<string, (string Estado, string Detalle)> Servicios(JsonElement cuerpo) =>
        cuerpo.GetProperty("servicios").EnumerateArray().ToDictionary(
            s => s.GetProperty("nombre").GetString()!,
            s => (s.GetProperty("estado").GetString()!, s.GetProperty("detalle").GetString()!));

    [Fact]
    public async Task Admin_ve_los_cinco_servicios_en_verde()
    {
        await using var api = new ApiConServiciosFalsos();
        var admin = await api.ClienteConSesionAsync("admin");

        var cuerpo = await admin.GetFromJsonAsync<JsonElement>("/api/admin/estado");

        Assert.True(cuerpo.TryGetProperty("generado", out _));
        Assert.Equal(new[] { "api", "sqlserver", "moodle", "agente", "modelo" },
            cuerpo.GetProperty("servicios").EnumerateArray().Select(s => s.GetProperty("nombre").GetString()));
        var servicios = Servicios(cuerpo);
        Assert.All(servicios.Values, s => Assert.Equal("OK", s.Estado));
        Assert.Equal("Moodle-Lab-UTA 4.5", servicios["moodle"].Detalle);
        Assert.Equal("qwen2.5:7b", servicios["modelo"].Detalle);
        Assert.Equal("EduMonitorDB", servicios["sqlserver"].Detalle);
    }

    [Fact]
    public async Task Agente_caido_y_Moodle_caido_se_informan_sin_detener_a_los_demas()
    {
        await using var api = new ApiConServiciosFalsos();
        api.Agente.Caido = true;
        api.Moodle.Fallar.Add("core_webservice_get_site_info");
        var admin = await api.ClienteConSesionAsync("admin");

        var servicios = Servicios(await admin.GetFromJsonAsync<JsonElement>("/api/admin/estado"));

        Assert.Equal("NO_DISPONIBLE", servicios["agente"].Estado);
        Assert.Equal("NO_DISPONIBLE", servicios["modelo"].Estado);
        Assert.Equal("Sin dato: el agente no respondió", servicios["modelo"].Detalle);
        Assert.Equal("NO_DISPONIBLE", servicios["moodle"].Estado);
        Assert.Equal("OK", servicios["api"].Estado);
        Assert.Equal("OK", servicios["sqlserver"].Estado);
    }

    [Fact]
    public async Task Agente_que_no_contesta_se_corta_a_los_3_segundos()
    {
        await using var api = new ApiConServiciosFalsos();
        api.Agente.Lento = true;
        var admin = await api.ClienteConSesionAsync("admin");
        var reloj = Stopwatch.StartNew();

        var servicios = Servicios(await admin.GetFromJsonAsync<JsonElement>("/api/admin/estado"));

        Assert.True(reloj.Elapsed < TimeSpan.FromSeconds(10), $"Tardó {reloj.Elapsed}");
        Assert.Equal("NO_DISPONIBLE", servicios["agente"].Estado);
        Assert.Equal("No respondió en 3 s", servicios["agente"].Detalle);
    }

    [Fact]
    public async Task Ollama_caido_marca_el_modelo_no_disponible_con_el_agente_en_verde()
    {
        await using var api = new ApiConServiciosFalsos();
        api.Agente.Ollama = "NO_DISPONIBLE";
        var admin = await api.ClienteConSesionAsync("admin");

        var servicios = Servicios(await admin.GetFromJsonAsync<JsonElement>("/api/admin/estado"));

        Assert.Equal("OK", servicios["agente"].Estado);
        Assert.Equal("NO_DISPONIBLE", servicios["modelo"].Estado);
    }

    [Fact]
    public async Task Docente_recibe_403_y_sin_token_401()
    {
        await using var api = new ApiConServiciosFalsos();
        var doc01 = await api.ClienteConSesionAsync("doc01");

        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync("/api/admin/estado")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("/api/admin/estado")).StatusCode);
    }
}
