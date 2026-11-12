using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// TA-13.2 · GET /api/tools/alertas. DADO el curso del Moodle falso con R2 en 1 (Luis sin entregar la Tarea 1 vencida), CUANDO el
/// agente pide las alertas, ENTONCES recibe solo las ACTIVAS con el objeto de GET /api/cursos/{id}/alertas; 403 en curso ajeno,
/// 401 sin token, 400 sin cursoId y 404 si el curso no existe. Cada llamada queda en el log sin el token.
/// </summary>
public class HerramientaAlertasApiTests
{
    private sealed class ApiConMoodleFalso : ApiConDatosFactory
    {
        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IMoodleClient>();
            services.AddSingleton<IMoodleClient>(MoodleClientFalso.ConCursoDeEjemplo());
        }
    }

    [Fact]
    public async Task Devuelve_las_alertas_activas_con_el_formato_del_contrato_y_no_las_revisadas()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/reglas", new object[] { new { codigo = "R2", umbral = 1.0, activa = true } })).EnsureSuccessStatusCode();
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();

        var cuerpo = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/alertas?cursoId={curso}");

        Assert.Equal("identificar_alertas", cuerpo.GetProperty("herramienta").GetString());
        Assert.Equal("AGIL-4B", cuerpo.GetProperty("curso").GetProperty("nombreCorto").GetString());
        var alerta = Assert.Single(cuerpo.GetProperty("alertas").EnumerateArray());
        Assert.Equal("R2", alerta.GetProperty("regla").GetString());
        Assert.Equal("Atrasos", alerta.GetProperty("nombreRegla").GetString());
        Assert.False(alerta.TryGetProperty("titulo", out JsonElement _)); // forma corta para el contexto del modelo
        Assert.False(alerta.TryGetProperty("generada", out JsonElement _));
        Assert.Equal("Luis", alerta.GetProperty("estudiante").GetProperty("nombre").GetString());
        Assert.Equal("Tarea 1", alerta.GetProperty("evidencia").GetProperty("actividades")[0].GetString()); // sin fecha límite
        Assert.Equal(1m, alerta.GetProperty("evidencia").GetProperty("umbral").GetDecimal());
        Assert.Contains(factory.Logs.Mensajes, m => m.Contains("Herramienta identificar_alertas") && m.Contains("resultado OK"));
        Assert.DoesNotContain(factory.Logs.Mensajes, m => m.Contains("eyJ"));

        (await doc01.PatchAsJsonAsync($"/api/cursos/{curso}/alertas/{alerta.GetProperty("id").GetInt32()}", new { estado = "REVISADA" })).EnsureSuccessStatusCode();
        var despues = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/alertas?cursoId={curso}");
        Assert.Empty(despues.GetProperty("alertas").EnumerateArray());
    }

    [Fact]
    public async Task Curso_ajeno_403_sin_token_401_sin_cursoId_400_e_inexistente_404()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var admin = await factory.ClienteConSesionAsync("admin");
        var bdd = factory.IdCurso("BDD-4B");

        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync($"/api/tools/alertas?cursoId={bdd}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/tools/alertas?cursoId={bdd}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.GetAsync("/api/tools/alertas")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/tools/alertas?cursoId=9999")).StatusCode);
    }
}
