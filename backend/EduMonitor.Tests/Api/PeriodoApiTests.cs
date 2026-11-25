using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// TA-22.A4 · GET /api/cursos/{id}/periodo y la herramienta GET /api/tools/estado-periodo. DADO el curso de ejemplo sincronizado con
/// su startdate de Moodle (1 sep 2026), CUANDO el docente abre el curso, ENTONCES recibe la semana, las actividades con su estado y
/// los bloques de la cabecera; sin la fecha de inicio, los estados siguen y las semanas van en noDisponibles; 401, 403, 404 y 409.
/// </summary>
public class PeriodoApiTests
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

    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 2, 0, 0, TimeSpan.FromHours(-5));

    private static async Task<(ApiConMoodleFalso Factory, HttpClient Doc01, int Curso)> CursoSincronizadoAsync(bool conFechas = true)
    {
        var factory = new ApiConMoodleFalso();
        if (conFechas)
        {
            factory.Moodle.Cursos.Add(new CursoMoodle(2, "AGIL-4B", "Metodologías Ágiles (lab)", Inicio, null));
        }

        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        return (factory, doc01, curso);
    }

    [Fact]
    public async Task La_sincronizacion_guarda_el_inicio_y_el_fin_del_curso()
    {
        var (factory, _, curso) = await CursoSincronizadoAsync();
        await using var _f = factory;

        using var alcance = factory.Services.CreateScope();
        var guardado = await alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>().Cursos.SingleAsync(c => c.Id == curso);
        Assert.Equal(Inicio, guardado.Inicio);
        Assert.Null(guardado.Fin); // enddate 0 en Moodle
        Assert.Contains("core_enrol_get_users_courses", factory.Moodle.Llamadas);
    }

    [Fact]
    public async Task El_periodo_trae_la_semana_las_actividades_con_su_estado_y_los_bloques()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _f = factory;

        var respuesta = await doc01.GetAsync($"/api/cursos/{curso}/periodo");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var p = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(curso, p.GetProperty("cursoId").GetInt32());
        Assert.Equal("ULTIMA_FECHA_LIMITE", p.GetProperty("finOrigen").GetString());
        Assert.Equal(TimeSpan.FromHours(-5), p.GetProperty("hoy").GetDateTimeOffset().Offset);
        Assert.Empty(p.GetProperty("noDisponibles").EnumerateArray());

        // Tarea 1 venció el 8 sep (semana 2); Luis no la entregó. Examen P1 no tiene fecha límite en el curso de ejemplo.
        var actividades = p.GetProperty("actividades").EnumerateArray().ToDictionary(a => a.GetProperty("nombre").GetString()!);
        var tarea = actividades["Tarea 1"];
        Assert.Equal(("T1", "VENCIDA", 2, 1, 1), (tarea.GetProperty("sigla").GetString(), tarea.GetProperty("estado").GetString(),
            tarea.GetProperty("semana").GetInt32(), tarea.GetProperty("entregaron").GetInt32(), tarea.GetProperty("faltan").GetInt32()));
        Assert.Equal("SIN_FECHA", actividades["Examen P1"].GetProperty("estado").GetString());
        Assert.Equal(1, p.GetProperty("vencidas").GetProperty("actividades").GetInt32());
        Assert.Equal("Tarea 1", p.GetProperty("vencidas").GetProperty("conFaltas")[0].GetProperty("nombre").GetString());
        Assert.Equal(2, p.GetProperty("semana").GetProperty("total").GetInt32()); // fin = última fecha límite (Tarea 1)
        Assert.Equal("TERMINADO", p.GetProperty("estado").GetString());
        Assert.Equal(4, p.GetProperty("unidades").GetArrayLength());
    }

    [Fact]
    public async Task Sin_fecha_de_inicio_no_hay_semanas_y_lo_dice_noDisponibles()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync(conFechas: false);
        await using var _f = factory;

        var p = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/periodo");

        Assert.Equal(new[] { "inicio" }, p.GetProperty("noDisponibles").EnumerateArray().Select(n => n.GetString()));
        Assert.Equal(JsonValueKind.Null, p.GetProperty("semana").ValueKind);
        Assert.Equal("VENCIDA", p.GetProperty("actividades")[0].GetProperty("estado").GetString());
    }

    [Fact]
    public async Task La_herramienta_estado_periodo_da_la_forma_corta_para_el_asistente()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _f = factory;

        var respuesta = await doc01.GetAsync($"/api/tools/estado-periodo?cursoId={curso}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var h = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("consultar_estado_periodo", h.GetProperty("herramienta").GetString());
        Assert.Equal("AGIL-4B", h.GetProperty("curso").GetProperty("nombreCorto").GetString());
        Assert.Equal(2, h.GetProperty("totalSemanas").GetInt32());
        Assert.Equal(("Tarea 1", 1), (h.GetProperty("vencidas")[0].GetProperty("nombre").GetString(), h.GetProperty("vencidas")[0].GetProperty("faltan").GetInt32()));
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", h.GetProperty("hoy").GetString());
        Assert.Contains(factory.Logs.Mensajes, m => m.Contains("Herramienta consultar_estado_periodo") && m.Contains("resultado OK"));

        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.GetAsync("/api/tools/estado-periodo")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync($"/api/tools/estado-periodo?cursoId={factory.IdCurso("BDD-4B")}")).StatusCode);
    }

    [Fact]
    public async Task Responde_401_403_404_y_409_segun_el_contrato()
    {
        await using var factory = new ApiConMoodleFalso();
        var agil = factory.IdCurso("AGIL-4B");
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var admin = await factory.ClienteConSesionAsync("admin");

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/cursos/{agil}/periodo")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync($"/api/cursos/{factory.IdCurso("BDD-4B")}/periodo")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/cursos/9999/periodo")).StatusCode);
        var sinSincronizar = await doc01.GetAsync($"/api/cursos/{agil}/periodo");
        Assert.Equal(HttpStatusCode.Conflict, sinSincronizar.StatusCode);
        Assert.Equal("Curso sin sincronizar", (await sinSincronizar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
    }
}
