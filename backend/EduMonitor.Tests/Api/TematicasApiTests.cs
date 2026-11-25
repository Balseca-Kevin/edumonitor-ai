using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// HU-08 · DADO el curso de ejemplo sincronizado con la rúbrica de la Tarea 1, CUANDO el docente crea un tema y le asocia el criterio
/// de la rúbrica y el Examen P1, ENTONCES el mapeo lo muestra y el desempeño por tema sale de esos criterios y notas.
/// </summary>
public class TematicasApiTests
{
    private sealed class ApiConMoodleFalso : ApiConDatosFactory
    {
        public MoodleClientFalso Moodle { get; } = ConRubrica();

        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IMoodleClient>();
            services.AddSingleton<IMoodleClient>(Moodle);
        }
    }

    private static MoodleClientFalso ConRubrica()
    {
        var moodle = MoodleClientFalso.ConCursoDeEjemplo();
        moodle.Rubricas.Add(new RubricaMoodle(70, 900, new[]
        {
            new CriterioRubricaMoodle(11, 1, "Redacción de historias de usuario", new[] { new NivelRubricaMoodle(1, 1m, "En proceso"), new NivelRubricaMoodle(2, 2.5m, "Destacado") }),
            new CriterioRubricaMoodle(12, 2, "Formato y entrega", new[] { new NivelRubricaMoodle(3, 0m, "Insuficiente"), new NivelRubricaMoodle(4, 2.5m, "Destacado") }),
        }));
        moodle.InstanciasRubrica[900] = new() { new InstanciaRubricaMoodle(41, 501, 1, new[] { new NivelElegidoMoodle(11, 1), new NivelElegidoMoodle(12, 4) }) };
        moodle.EstadosEntrega[(7, 101)] = new EstadoEntregaMoodle(501, null);
        return moodle;
    }

    private static async Task<(ApiConMoodleFalso Factory, HttpClient Doc01, int Curso)> SincronizadoAsync()
    {
        var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        return (factory, doc01, curso);
    }

    [Fact]
    public async Task El_docente_crea_un_tema_le_asocia_un_criterio_y_una_actividad_y_ve_el_desempeno()
    {
        var (factory, doc01, curso) = await SincronizadoAsync();
        await using var _f = factory;

        var mapeo = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/mapeo");
        var unidad1 = mapeo.GetProperty("unidades").EnumerateArray().First(u => u.GetProperty("numero").GetInt32() == 1).GetProperty("id").GetInt32();
        var actividades = mapeo.GetProperty("actividades").EnumerateArray().ToDictionary(a => a.GetProperty("nombre").GetString()!);
        var tarea = actividades["Tarea 1"];
        Assert.Equal(new[] { "Redacción de historias de usuario", "Formato y entrega" },
            tarea.GetProperty("criterios").EnumerateArray().Select(c => c.GetProperty("descripcion").GetString()));
        Assert.Empty(actividades["Examen P1"].GetProperty("criterios").EnumerateArray());

        var creado = await doc01.PostAsJsonAsync($"/api/cursos/{curso}/unidades/{unidad1}/temas", new { nombre = "  Historias   de usuario " });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var tema = await creado.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Historias de usuario", tema.GetProperty("nombre").GetString());
        var temaId = tema.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.PostAsJsonAsync($"/api/cursos/{curso}/unidades/{unidad1}/temas", new { nombre = "historias de USUARIO" })).StatusCode);

        var criterio = tarea.GetProperty("criterios")[0].GetProperty("id").GetInt32();
        var asociada = await doc01.PutAsJsonAsync($"/api/cursos/{curso}/actividades/{tarea.GetProperty("id").GetInt32()}/temas",
            new { temas = Array.Empty<int>(), criterios = new[] { new { id = criterio, temaId = (int?)temaId } } });
        Assert.Equal(HttpStatusCode.OK, asociada.StatusCode);
        var actividad = await asociada.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { temaId }, actividad.GetProperty("temas").EnumerateArray().Select(t => t.GetInt32())); // llega por el criterio
        (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/actividades/{actividades["Examen P1"].GetProperty("id").GetInt32()}/temas", new { temas = new[] { temaId } })).EnsureSuccessStatusCode();

        var desempeno = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/tematicas");
        var t = Assert.Single(desempeno.GetProperty("temas").EnumerateArray());
        // Ana: criterio 1 de 2,5 → 4 en la Tarea 1 y 0 en el Examen P1: 2. Luis: 7 en el examen.
        var notas = t.GetProperty("estudiantes").EnumerateArray().Select(e => (e.GetProperty("nombre").GetString()!, e.GetProperty("nota").GetDecimal()));
        Assert.Equal(new[] { ("Ana", 2m), ("Luis", 7m) }, notas);
        Assert.Equal(0.5m, t.GetProperty("domina").GetDecimal());
        Assert.Equal("Ana", Assert.Single(t.GetProperty("bajo6").EnumerateArray()).GetProperty("nombre").GetString());
        Assert.Equal("CRITERIOS", t.GetProperty("evaluaciones")[0].GetProperty("fuente").GetString());

        var soloTarea = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/tematicas?actividadId={tarea.GetProperty("id").GetInt32()}");
        Assert.Equal(4m, soloTarea.GetProperty("temas")[0].GetProperty("promedio").GetDecimal());

        // La herramienta del asistente da lo mismo en forma corta, sin la lista completa de estudiantes.
        var herramienta = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/desempeno-tematico?cursoId={curso}");
        Assert.Equal("obtener_desempeno_tematico", herramienta.GetProperty("herramienta").GetString());
        var corto = herramienta.GetProperty("temas")[0];
        Assert.Equal("Historias de usuario", corto.GetProperty("nombre").GetString());
        Assert.Equal("Tarea 1 (criterio: Redacción de historias de usuario)", corto.GetProperty("evaluaciones")[0].GetString());
        Assert.False(corto.TryGetProperty("estudiantes", out _));
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.GetAsync("/api/tools/desempeno-tematico")).StatusCode);
    }

    [Fact]
    public async Task Rechaza_temas_y_criterios_ajenos_y_responde_404_409_y_403()
    {
        var (factory, doc01, curso) = await SincronizadoAsync();
        await using var _f = factory;
        var mapeo = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/mapeo");
        var tarea = mapeo.GetProperty("actividades").EnumerateArray().First(a => a.GetProperty("nombre").GetString() == "Tarea 1").GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/actividades/{tarea}/temas", new { temas = new[] { 9999 } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/actividades/{tarea}/temas", new { criterios = new[] { new { id = 9999, temaId = (int?)null } } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/actividades/9999/temas", new { temas = Array.Empty<int>() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync($"/api/cursos/{curso}/unidades/9999/temas")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync($"/api/cursos/{curso}/tematicas?actividadId=9999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.PostAsJsonAsync($"/api/cursos/{curso}/unidades/{mapeo.GetProperty("unidades")[0].GetProperty("id").GetInt32()}/temas", new { nombre = " " })).StatusCode);

        var bdd = factory.IdCurso("BDD-4B");
        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync($"/api/cursos/{bdd}/mapeo")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/cursos/{curso}/tematicas")).StatusCode);
        var doc02 = await factory.ClienteConSesionAsync("doc02");
        Assert.Equal(HttpStatusCode.Conflict, (await doc02.GetAsync($"/api/cursos/{bdd}/tematicas")).StatusCode);
    }
}
