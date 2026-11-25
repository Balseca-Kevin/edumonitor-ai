using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Agente;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// HU-16 · DADO un curso sincronizado y una respuesta del asistente, CUANDO el docente descarga el informe, ENTONCES recibe un PDF con
/// los indicadores y ese resumen; 400, 403, 404 y 409 como el resto del curso.
/// </summary>
public class InformeApiTests
{
    private sealed class Api : ApiConDatosFactory
    {
        public AgenteClientFalso Agente { get; } = new();
        public MoodleClientFalso Moodle { get; } = MoodleClientFalso.ConCursoDeEjemplo();

        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IAgenteClient>();
            services.AddSingleton<IAgenteClient>(Agente);
            services.RemoveAll<IMoodleClient>();
            services.AddSingleton<IMoodleClient>(Moodle);
        }
    }

    private static async Task<int> PreguntarAsync(HttpClient cliente, int curso)
    {
        var r = await (await cliente.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "Resume el estado del curso" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        return r.GetProperty("conversacionId").GetInt32();
    }

    [Fact]
    public async Task Descarga_el_pdf_del_informe_con_el_resumen_del_asistente()
    {
        await using var api = new Api();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var curso = api.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        var cid = await PreguntarAsync(doc01, curso);

        var respuesta = await doc01.GetAsync($"/api/cursos/{curso}/informe?formato=pdf&conversacionId={cid}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/pdf", respuesta.Content.Headers.ContentType!.MediaType);
        Assert.Equal($"informe-AGIL-4B-{cid}.pdf", respuesta.Content.Headers.ContentDisposition!.FileName!.Trim('"'));
        var bytes = await respuesta.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task Responde_400_404_409_y_403()
    {
        await using var api = new Api();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var doc02 = await api.ClienteConSesionAsync("doc02");
        var curso = api.IdCurso("AGIL-4B");
        var cid = await PreguntarAsync(doc01, curso);

        Assert.Equal(HttpStatusCode.Conflict, (await doc01.GetAsync($"/api/cursos/{curso}/informe?conversacionId={cid}")).StatusCode);
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.GetAsync($"/api/cursos/{curso}/informe?formato=docx&conversacionId={cid}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.GetAsync($"/api/cursos/{curso}/informe")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync($"/api/cursos/{curso}/informe?conversacionId=99999")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc02.GetAsync($"/api/cursos/{curso}/informe?conversacionId={cid}")).StatusCode);
    }
}
