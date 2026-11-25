using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Application.Agente;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// HU-15 · TA-15.A3, TA-15.A4 y TA-15.A1 · DADO el curso de ejemplo con la rúbrica de la Tarea 1 y su criterio asociado al tema
/// «Historias de usuario», CUANDO el docente arma el plan para Ana, ENTONCES se guarda, la herramienta del asistente trae el criterio
/// fallado con su nivel, el docente edita y aprueba el borrador, y el historial compara el tema antes y después.
/// </summary>
public class RefuerzosApiTests
{
    private sealed class ApiConMoodleFalso : ApiConDatosFactory
    {
        public MoodleClientFalso Moodle { get; } = ConRubrica();
        public AgenteClientFalso Agente { get; } = new();

        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IMoodleClient>();
            services.AddSingleton<IMoodleClient>(Moodle);
            services.RemoveAll<IAgenteClient>();
            services.AddSingleton<IAgenteClient>(Agente);
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
        moodle.EstadosEntrega[(7, 101)] = new EstadoEntregaMoodle(501, "Revisa:\n• Redacción de historias de usuario: falta el beneficio.");
        return moodle;
    }

    private sealed record Escenario(ApiConMoodleFalso Factory, HttpClient Doc01, int Curso, int Tema, int Tarea, int Examen, int Ana, int Luis);

    private static async Task<Escenario> PrepararAsync()
    {
        var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        var mapeo = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/mapeo");
        var unidad = mapeo.GetProperty("unidades")[0].GetProperty("id").GetInt32();
        var actividades = mapeo.GetProperty("actividades").EnumerateArray().ToDictionary(a => a.GetProperty("nombre").GetString()!);
        var tarea = actividades["Tarea 1"];
        var tema = (await (await doc01.PostAsJsonAsync($"/api/cursos/{curso}/unidades/{unidad}/temas", new { nombre = "Historias de usuario" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/actividades/{tarea.GetProperty("id").GetInt32()}/temas",
            new { criterios = new[] { new { id = tarea.GetProperty("criterios")[0].GetProperty("id").GetInt32(), temaId = (int?)tema } } })).EnsureSuccessStatusCode();
        var estudiantes = (await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/indicadores")).GetProperty("estudiantes").EnumerateArray()
            .ToDictionary(e => e.GetProperty("nombre").GetString()!, e => e.GetProperty("id").GetInt32());
        return new Escenario(factory, doc01, curso, tema, tarea.GetProperty("id").GetInt32(), actividades["Examen P1"].GetProperty("id").GetInt32(), estudiantes["Ana"], estudiantes["Luis"]);
    }

    [Fact]
    public async Task El_docente_arma_el_plan_el_asistente_lo_lee_y_el_docente_lo_edita_y_aprueba()
    {
        var e = await PrepararAsync();
        await using var _f = e.Factory;

        var creado = await e.Doc01.PostAsJsonAsync($"/api/cursos/{e.Curso}/refuerzos", new { actividadId = e.Tarea, temas = new[] { e.Tema }, estudiantes = new[] { e.Ana } });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var plan = await creado.Content.ReadFromJsonAsync<JsonElement>();
        var rid = plan.GetProperty("id").GetInt32();
        Assert.Equal("ENVIADO", plan.GetProperty("estado").GetString());
        Assert.Equal("Tarea 1", plan.GetProperty("actividad").GetProperty("nombre").GetString());
        Assert.Equal("Historias de usuario", plan.GetProperty("temas")[0].GetProperty("nombre").GetString());

        // TA-15.A4 · Lo que lee el asistente: el criterio fallado con su nivel real, el comentario del docente y la nota necesaria.
        var herramienta = await e.Doc01.GetFromJsonAsync<JsonElement>($"/api/tools/plan-refuerzo?cursoId={e.Curso}&refuerzoId={rid}");
        Assert.Equal("obtener_plan_refuerzo", herramienta.GetProperty("herramienta").GetString());
        var ana = Assert.Single(herramienta.GetProperty("estudiantes").EnumerateArray());
        var fallado = Assert.Single(ana.GetProperty("criteriosFallados").EnumerateArray());
        Assert.Equal(("Redacción de historias de usuario", "En proceso", 1m), (fallado.GetProperty("criterio").GetString(), fallado.GetProperty("nivel").GetString(), fallado.GetProperty("puntaje").GetDecimal()));
        Assert.Equal(4m, ana.GetProperty("notasPorTema")[0].GetProperty("nota").GetDecimal());
        Assert.StartsWith("Revisa:", ana.GetProperty("comentarioDocente").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await e.Doc01.PatchAsJsonAsync($"/api/cursos/{e.Curso}/refuerzos/{rid}", new { estado = "APROBADO" })).StatusCode); // sin borrador
        var editado = await e.Doc01.PatchAsJsonAsync($"/api/cursos/{e.Curso}/refuerzos/{rid}", new { texto = "Ana Pérez: repasa la redacción de historias con rol, acción y beneficio." });
        Assert.Equal("BORRADOR", (await editado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estado").GetString());

        var aprobado = await e.Doc01.PatchAsJsonAsync($"/api/cursos/{e.Curso}/refuerzos/{rid}", new { estado = "APROBADO" });
        var final = await aprobado.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("APROBADO", final.GetProperty("estado").GetString());
        Assert.NotEqual(JsonValueKind.Null, final.GetProperty("aprobadoEn").ValueKind);
        var resultado = Assert.Single(final.GetProperty("resultados").EnumerateArray());
        Assert.Equal(("SIN_NOTAS_NUEVAS", 4m), (resultado.GetProperty("estado").GetString(), resultado.GetProperty("antes").GetDecimal()));
        Assert.Equal(HttpStatusCode.Conflict, (await e.Doc01.PatchAsJsonAsync($"/api/cursos/{e.Curso}/refuerzos/{rid}", new { texto = "otro" })).StatusCode);

        using (var alcance = e.Factory.Services.CreateScope())
        {
            var tareas = await alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>().TareasRefuerzo.ToListAsync();
            Assert.Equal((e.Tema, e.Ana, "APROBADA"), (tareas.Single().TemaId, tareas.Single().EstudianteId!.Value, tareas.Single().Estado));
        }

        // TA-15.A1 · Una nota nueva en el tema después de aprobar: el examen se asocia al tema y Ana tiene 0 en él.
        (await e.Doc01.PutAsJsonAsync($"/api/cursos/{e.Curso}/actividades/{e.Examen}/temas", new { temas = new[] { e.Tema } })).EnsureSuccessStatusCode();
        var historial = await e.Doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{e.Curso}/refuerzos");
        var despues = Assert.Single(historial.EnumerateArray()).GetProperty("resultados")[0];
        Assert.Equal(("BAJO", 4m, 2m), (despues.GetProperty("estado").GetString(), despues.GetProperty("antes").GetDecimal(), despues.GetProperty("ahora").GetDecimal()));
    }

    [Fact]
    public async Task Rechaza_planes_sin_temas_con_temas_o_estudiantes_ajenos_y_responde_404_y_403()
    {
        var e = await PrepararAsync();
        await using var _f = e.Factory;
        var ruta = $"/api/cursos/{e.Curso}/refuerzos";

        Assert.Equal(HttpStatusCode.BadRequest, (await e.Doc01.PostAsJsonAsync(ruta, new { actividadId = e.Tarea, temas = Array.Empty<int>(), estudiantes = new[] { e.Ana } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.Doc01.PostAsJsonAsync(ruta, new { actividadId = e.Examen, temas = new[] { e.Tema }, estudiantes = new[] { e.Ana } })).StatusCode); // el examen no tiene el tema
        Assert.Equal(HttpStatusCode.BadRequest, (await e.Doc01.PostAsJsonAsync(ruta, new { actividadId = e.Tarea, temas = new[] { e.Tema }, estudiantes = new[] { 9999 } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Doc01.PostAsJsonAsync(ruta, new { actividadId = 9999, temas = new[] { e.Tema }, estudiantes = new[] { e.Ana } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Doc01.GetAsync($"{ruta}/9999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Doc01.GetAsync($"/api/tools/plan-refuerzo?cursoId={e.Curso}&refuerzoId=9999")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await e.Doc01.GetAsync($"/api/cursos/{e.Factory.IdCurso("BDD-4B")}/refuerzos")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await e.Factory.CreateClient().GetAsync(ruta)).StatusCode);
        Assert.Empty((await e.Doc01.GetFromJsonAsync<JsonElement>(ruta)).EnumerateArray());
    }

    [Fact]
    public async Task El_plan_como_foco_del_chat_guarda_el_borrador_y_el_documento_se_descarga_al_aprobar()
    {
        var e = await PrepararAsync();
        await using var _f = e.Factory;
        var plan = await (await e.Doc01.PostAsJsonAsync($"/api/cursos/{e.Curso}/refuerzos", new { actividadId = e.Tarea, temas = new[] { e.Tema }, estudiantes = new[] { e.Ana } }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var rid = plan.GetProperty("id").GetInt32();
        var documento = $"/api/cursos/{e.Curso}/refuerzos/{rid}/documento";
        Assert.Equal(HttpStatusCode.Conflict, (await e.Doc01.GetAsync($"{documento}?formato=pdf")).StatusCode);

        var chat = await e.Doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = e.Curso, mensaje = "", foco = new { refuerzoId = rid } });

        Assert.Equal(HttpStatusCode.OK, chat.StatusCode);
        var respuesta = await chat.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(rid, respuesta.GetProperty("refuerzoId").GetInt32());
        var mensaje = e.Factory.Agente.Peticiones.Single().Mensaje;
        Assert.Contains($"refuerzoId {rid}", mensaje);
        Assert.Contains("obtener_plan_refuerzo", mensaje);
        Assert.EndsWith("Redacta el borrador del plan de refuerzo.", mensaje);

        var conBorrador = await e.Doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{e.Curso}/refuerzos/{rid}");
        Assert.Equal("BORRADOR", conBorrador.GetProperty("estado").GetString());
        Assert.Equal(respuesta.GetProperty("respuesta").GetString(), conBorrador.GetProperty("texto").GetString());
        Assert.Equal(respuesta.GetProperty("conversacionId").GetInt32(), conBorrador.GetProperty("conversacionId").GetInt32());

        (await e.Doc01.PatchAsJsonAsync($"/api/cursos/{e.Curso}/refuerzos/{rid}", new { texto = "Ana Pérez: repasa historias de usuario.", estado = "APROBADO" })).EnsureSuccessStatusCode();

        var pdf = await e.Doc01.GetAsync($"{documento}?formato=pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
        Assert.Equal($"plan-refuerzo-AGIL-4B-{rid}.pdf", pdf.Content.Headers.ContentDisposition!.FileName?.Trim('"'));

        var docx = await e.Doc01.GetAsync($"{documento}?formato=docx");
        Assert.Equal(HttpStatusCode.OK, docx.StatusCode);
        var bytes = await docx.Content.ReadAsByteArrayAsync();
        Assert.Equal("PK", System.Text.Encoding.ASCII.GetString(bytes[..2]));
        using (var leido = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(new MemoryStream(bytes), false))
        {
            var texto = leido.MainDocumentPart!.Document.Body!.InnerText;
            Assert.Contains("Ana Pérez: repasa historias de usuario.", texto);
            Assert.Contains("Historias de usuario", texto);
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await e.Doc01.GetAsync($"{documento}?formato=odt")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.Doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = e.Curso, mensaje = "", foco = new { refuerzoId = rid } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.Doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = e.Curso, mensaje = "", foco = new { refuerzoId = 9999 } })).StatusCode);
    }
}
