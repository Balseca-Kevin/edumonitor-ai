using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>TA-10.A1 · GET /api/cursos/{id}/estudiantes/{eid}/nota-necesaria y la herramienta GET /api/tools/nota-necesaria.</summary>
public class NotaNecesariaApiTests
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
    public async Task Responde_la_nota_necesaria_del_estudiante_y_la_herramienta_la_da_al_asistente()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        var luis = (await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/indicadores"))
            .GetProperty("estudiantes").EnumerateArray().Single(e => e.GetProperty("nombre").GetString() == "Luis").GetProperty("id").GetInt32();

        var respuesta = await doc01.GetAsync($"/api/cursos/{curso}/estudiantes/{luis}/nota-necesaria");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var r = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        // Luis: 7 en el Examen P1 (sin fecha límite, calificado) y la Tarea 1 vencida sin entrega: no hay pendientes.
        Assert.Equal("SIN_PENDIENTES", r.GetProperty("estado").GetString());
        Assert.Equal(7m, r.GetProperty("aprobacion").GetDecimal());
        Assert.Equal("Luis", r.GetProperty("estudiante").GetProperty("nombre").GetString());
        Assert.Equal(3, r.GetProperty("categorias").GetArrayLength());

        var herramienta = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/nota-necesaria?cursoId={curso}&estudianteId={luis}");
        Assert.Equal("calcular_nota_necesaria", herramienta.GetProperty("herramienta").GetString());
        Assert.Equal("SIN_PENDIENTES", herramienta.GetProperty("resultado").GetProperty("estado").GetString());
        Assert.Contains(factory.Logs.Mensajes, m => m.Contains("Herramienta calcular_nota_necesaria") && m.Contains("resultado OK"));
    }

    [Fact]
    public async Task Responde_401_403_404_y_409_segun_el_contrato()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");

        Assert.Equal(HttpStatusCode.Conflict, (await doc01.GetAsync($"/api/cursos/{curso}/estudiantes/1/nota-necesaria")).StatusCode);
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync($"/api/cursos/{curso}/estudiantes/9999/nota-necesaria")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync($"/api/cursos/{factory.IdCurso("BDD-4B")}/estudiantes/1/nota-necesaria")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/cursos/{curso}/estudiantes/1/nota-necesaria")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.GetAsync($"/api/tools/nota-necesaria?cursoId={curso}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync($"/api/tools/nota-necesaria?cursoId={curso}&estudianteId=9999")).StatusCode);
    }
}
