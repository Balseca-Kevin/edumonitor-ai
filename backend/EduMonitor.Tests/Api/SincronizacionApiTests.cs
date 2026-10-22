using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>POST y GET /api/cursos/{id}/sync con la política CursoAutorizado y un Moodle falso.</summary>
public class SincronizacionApiTests
{
    private sealed class ApiConMoodleFalso : ApiConDatosFactory
    {
        public MoodleClientFalso Moodle { get; } = MoodleClientFalso.ConCursoDeEjemplo();

        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IMoodleClient>();
            services.AddSingleton<IMoodleClient>(Moodle);
        }
    }

    [Fact]
    public async Task Post_sync_responde_200_con_el_formato_del_contrato()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");

        var respuesta = await doc01.PostAsync($"/api/cursos/{curso}/sync", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(curso, cuerpo.GetProperty("cursoId").GetInt32());
        Assert.Equal("COMPLETADA", cuerpo.GetProperty("estado").GetString());
        Assert.Equal(2, cuerpo.GetProperty("registros").GetProperty("estudiantes").GetInt32());
        Assert.Equal(4, cuerpo.GetProperty("registros").GetProperty("calificaciones").GetInt32());
        Assert.Equal(0, cuerpo.GetProperty("errores").GetArrayLength());
        foreach (var campo in new[] { "id", "inicio", "fin", "origen" })
        {
            Assert.True(cuerpo.TryGetProperty(campo, out _), $"Falta {campo}");
        }
    }

    [Fact]
    public async Task Post_sync_de_un_curso_ajeno_responde_403_y_sin_token_401()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var cursoAjeno = factory.IdCurso("BDD-4B");

        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.PostAsync($"/api/cursos/{cursoAjeno}/sync", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().PostAsync($"/api/cursos/{cursoAjeno}/sync", null)).StatusCode);
        Assert.Empty(factory.Moodle.Llamadas); // no se llegó a consultar Moodle
    }

    [Fact]
    public async Task Get_sync_devuelve_el_historial_de_la_mas_reciente_a_la_mas_antigua_y_el_curso_muestra_la_ultima()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        await doc01.PostAsync($"/api/cursos/{curso}/sync", null);
        factory.Moodle.Fallar.Add("gradereport_user_get_grade_items");
        await doc01.PostAsync($"/api/cursos/{curso}/sync", null);

        var historial = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/sync");
        var detalle = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}");

        Assert.Equal(new[] { "PARCIAL", "COMPLETADA" }, historial.EnumerateArray().Select(s => s.GetProperty("estado").GetString()));
        var ultima = detalle.GetProperty("ultimaSincronizacion");
        Assert.Equal("PARCIAL", ultima.GetProperty("estado").GetString());
        Assert.Equal(historial[0].GetProperty("id").GetInt32(), ultima.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Si_Moodle_no_responde_en_ninguna_funcion_responde_502_y_queda_registrada_FALLIDA()
    {
        await using var factory = new ApiConMoodleFalso();
        factory.Moodle.Fallar.Add("core_enrol_get_enrolled_users");
        factory.Moodle.Fallar.Add("gradereport_user_get_grade_items");
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");

        var respuesta = await doc01.PostAsync($"/api/cursos/{curso}/sync", null);

        Assert.Equal(HttpStatusCode.BadGateway, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Moodle no disponible", problema.GetProperty("title").GetString());
        Assert.Equal("FALLIDA", problema.GetProperty("sincronizacion").GetProperty("estado").GetString());
        var historial = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/sync");
        Assert.Equal("FALLIDA", Assert.Single(historial.EnumerateArray()).GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Get_sync_de_curso_inexistente_responde_404()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");

        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync("/api/cursos/9999/sync")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.PostAsync("/api/cursos/9999/sync", null)).StatusCode);
    }
}
