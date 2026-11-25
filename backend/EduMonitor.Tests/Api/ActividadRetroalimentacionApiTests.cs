using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Application.Actividades;
using EduMonitor.Application.Agente;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// HU-22 · Panel de actividad con el enunciado de Moodle, entregas, distribución y dominio por criterio. HU-15 · Retroalimentación
/// por estudiante con su borrador del asistente, aprobación y documento, y refuerzos aplicados (TA-15.A2).
/// </summary>
public class ActividadRetroalimentacionApiTests
{
    private sealed class Api : ApiConDatosFactory
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

        public HttpClient ClienteAgente(int cursoId)
        {
            using var alcance = Services.CreateScope();
            var docente = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>().Usuarios.Single(u => u.NombreUsuario == "doc01");
            var token = alcance.ServiceProvider.GetRequiredService<IJwtService>().EmitirTokenAgente(docente.Id, cursoId);
            var cliente = CreateClient();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            return cliente;
        }
    }

    private static MoodleClientFalso ConRubrica()
    {
        var moodle = MoodleClientFalso.ConCursoDeEjemplo();
        moodle.Tareas[0] = moodle.Tareas[0] with
        {
            Enunciado = "Redacta 5 historias de usuario.\n• Con criterios de aceptación",
            Archivos = new[] { new ArchivoMoodle("plantilla-historias.docx", 20480, "application/vnd.openxmlformats-officedocument.wordprocessingml.document") },
        };
        moodle.Rubricas.Add(new RubricaMoodle(70, 900, new[]
        {
            new CriterioRubricaMoodle(11, 1, "Redacción de historias de usuario", new[] { new NivelRubricaMoodle(1, 1m, "En proceso"), new NivelRubricaMoodle(2, 2.5m, "Destacado") }),
            new CriterioRubricaMoodle(12, 2, "Formato y entrega", new[] { new NivelRubricaMoodle(3, 0m, "Insuficiente"), new NivelRubricaMoodle(4, 2.5m, "Destacado") }),
        }));
        moodle.InstanciasRubrica[900] = new() { new InstanciaRubricaMoodle(41, 501, 1, new[] { new NivelElegidoMoodle(11, 1), new NivelElegidoMoodle(12, 4) }) };
        moodle.EstadosEntrega[(7, 101)] = new EstadoEntregaMoodle(501, "Revisa:\n• Redacción de historias de usuario: falta el beneficio.");
        return moodle;
    }

    private static async Task<(Api Api, HttpClient Doc01, int Curso, int Tarea, int Ana)> PrepararAsync()
    {
        var api = new Api();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var curso = api.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        using var alcance = api.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
        var tarea = db.Actividades.Single(a => a.CursoId == curso && a.Nombre == "Tarea 1").Id;
        var ana = db.Estudiantes.Single(e => e.Apellido == "Pérez").Id;
        return (api, doc01, curso, tarea, ana);
    }

    // --- Cálculos ---

    [Fact]
    public void Resumen_cuenta_entregas_y_promedia_solo_las_calificadas()
    {
        var resumen = CalculadoraActividad.Resumen(new (string?, decimal?)[]
        {
            ("CALIFICADO", 0m), ("CALIFICADO", 8m), ("CALIFICADO", 5.5m), ("SIN_ENTREGA", null), ("PENDIENTE", null), ("AUN_NO_VENCE", null), (null, null),
        });

        Assert.Equal(new ResumenActividadDto(7, 3, 1, 1, 1, 1, 1, 4.50m, 1, 2), resumen);
    }

    [Fact]
    public void Criterio_se_domina_con_el_70_por_ciento_del_puntaje_maximo()
    {
        var criterio = CalculadoraActividad.Criterio(1, 1, "Historias", 2.5m, null, new[] { 2.5m, 2m, 1m, 0m });
        var sinNiveles = CalculadoraActividad.Criterio(2, 2, "Formato", 2.5m, null, Array.Empty<decimal>());

        Assert.Equal(0.5m, criterio.Domina); // 2,5 y 2 llegan a 1,75
        Assert.Equal(1.38m, criterio.PuntajePromedio);
        Assert.Equal(4, criterio.ConNivel);
        Assert.Null(sinNiveles.Domina);
        Assert.Null(sinNiveles.PuntajePromedio);
    }

    // --- HU-22 ---

    [Fact]
    public async Task Panel_de_actividad_trae_el_enunciado_de_moodle_las_entregas_y_el_dominio_por_criterio()
    {
        var (api, doc01, curso, tarea, ana) = await PrepararAsync();
        await using var _ = api;

        var panel = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/actividades/{tarea}");

        var actividad = panel.GetProperty("actividad");
        Assert.Equal("Redacta 5 historias de usuario.\n• Con criterios de aceptación", actividad.GetProperty("enunciado").GetString());
        Assert.Equal("plantilla-historias.docx", actividad.GetProperty("archivos")[0].GetProperty("nombre").GetString());
        Assert.Equal(2, panel.GetProperty("resumen").GetProperty("matriculados").GetInt32());
        var criterios = panel.GetProperty("criterios").EnumerateArray().ToList();
        Assert.Equal(2, criterios.Count);
        Assert.Equal(0m, criterios[0].GetProperty("domina").GetDecimal()); // Ana: 1 de 2,5
        Assert.Equal(1m, criterios[1].GetProperty("domina").GetDecimal()); // Ana: 2,5 de 2,5
        // TA-22.A1: con menos de 8 calificados no hay separación; el dominio 0 basta para el aviso de enseñanza.
        Assert.Equal(JsonValueKind.Null, criterios[0].GetProperty("separacion").ValueKind);
        Assert.Equal("REVISAR_ENSENANZA", criterios[0].GetProperty("aviso").GetString());
        Assert.Equal(JsonValueKind.Null, criterios[1].GetProperty("aviso").ValueKind);
        var fila = panel.GetProperty("estudiantes").EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == ana);
        Assert.Contains("falta el beneficio", fila.GetProperty("comentario").GetString());
        Assert.Equal(10, panel.GetProperty("distribucion").GetArrayLength());
    }

    [Fact]
    public async Task Panel_de_actividad_responde_404_y_409_y_la_herramienta_da_codigos()
    {
        await using var api = new Api();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var curso = api.IdCurso("AGIL-4B");
        Assert.Equal(HttpStatusCode.Conflict, (await doc01.GetAsync($"/api/cursos/{curso}/actividades/1")).StatusCode);
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync($"/api/cursos/{curso}/actividades/99999")).StatusCode);

        int tarea;
        using (var alcance = api.Services.CreateScope())
        {
            tarea = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>().Actividades.Single(a => a.CursoId == curso && a.Nombre == "Tarea 1").Id;
        }

        var herramienta = await api.ClienteAgente(curso).GetStringAsync($"/api/tools/actividad?cursoId={curso}&actividadId={tarea}");
        Assert.Contains("obtener_detalle_actividad", herramienta);
        Assert.Contains("\"aviso\"", herramienta); // TA-22.A2: el asistente ve el aviso de cada criterio
        Assert.DoesNotContain("Pérez", herramienta);
        Assert.DoesNotContain("Mora", herramienta);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ClienteAgente(curso).GetAsync($"/api/tools/actividad?cursoId={curso}")).StatusCode);
    }

    // --- HU-15 ---

    [Fact]
    public async Task Retroalimentacion_sin_temas_se_redacta_aprueba_y_descarga_con_su_titulo()
    {
        var (api, doc01, curso, tarea, ana) = await PrepararAsync();
        await using var _ = api;

        var creado = await doc01.PostAsJsonAsync($"/api/cursos/{curso}/refuerzos", new { actividadId = tarea, tipo = "RETROALIMENTACION", estudiantes = new[] { ana } });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var rid = (await creado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var chat = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "", foco = new { refuerzoId = rid } });
        Assert.Equal(HttpStatusCode.OK, chat.StatusCode);
        Assert.Contains("Foco: retroalimentación", api.Agente.Peticiones.Single().Mensaje);

        var plan = await api.ClienteAgente(curso).GetFromJsonAsync<JsonElement>($"/api/tools/plan-refuerzo?cursoId={curso}&refuerzoId={rid}");
        Assert.Equal("RETROALIMENTACION", plan.GetProperty("tipo").GetString());
        Assert.Contains("Redacción de historias de usuario", plan.ToString()); // criterio fallado sin tema asociado

        var aprobado = await (await doc01.PatchAsJsonAsync($"/api/cursos/{curso}/refuerzos/{rid}", new { estado = "APROBADO" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("APROBADO", aprobado.GetProperty("estado").GetString());
        Assert.Empty(aprobado.GetProperty("resultados").EnumerateArray());

        var documento = await doc01.GetAsync($"/api/cursos/{curso}/refuerzos/{rid}/documento?formato=docx");
        Assert.Equal(HttpStatusCode.OK, documento.StatusCode);
        Assert.StartsWith("retroalimentacion-AGIL-4B-", documento.Content.Headers.ContentDisposition!.FileNameStar ?? documento.Content.Headers.ContentDisposition.FileName!.Trim('"'));
    }

    [Fact]
    public async Task Un_plan_de_refuerzo_sigue_pidiendo_temas_y_un_tipo_desconocido_es_400()
    {
        var (api, doc01, curso, tarea, ana) = await PrepararAsync();
        await using var _ = api;

        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.PostAsJsonAsync($"/api/cursos/{curso}/refuerzos", new { actividadId = tarea, estudiantes = new[] { ana } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.PostAsJsonAsync($"/api/cursos/{curso}/refuerzos", new { actividadId = tarea, tipo = "OTRO", estudiantes = new[] { ana } })).StatusCode);
    }

    [Fact]
    public async Task Refuerzos_aplicados_responde_con_conteos_y_la_herramienta_del_asistente()
    {
        var (api, doc01, curso, _, _) = await PrepararAsync();
        await using var __ = api;

        var aplicados = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/refuerzos/aplicados");
        var herramienta = await api.ClienteAgente(curso).GetFromJsonAsync<JsonElement>($"/api/tools/refuerzos-aplicados?cursoId={curso}");

        Assert.Equal(0, aplicados.GetProperty("conteos").GetProperty("subio").GetInt32());
        Assert.Empty(aplicados.GetProperty("filas").EnumerateArray());
        Assert.Equal("consultar_refuerzos_aplicados", herramienta.GetProperty("herramienta").GetString());
    }
}
