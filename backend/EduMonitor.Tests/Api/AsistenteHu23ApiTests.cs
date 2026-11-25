using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Application.Agente;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// HU-23 · Asistente con memoria, foco, seudonimización, respaldo local e interruptor «Usar solo el modelo local».
/// DADO AGIL-4B sincronizado con Ana Pérez y Luis Mora, CUANDO el docente pregunta, ENTONCES el agente recibe códigos (E-001…) y no
/// nombres, la pantalla recibe nombres, y una pregunta de seguimiento lleva los turnos anteriores.
/// </summary>
public class AsistenteHu23ApiTests
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

        public HttpClient ClienteAgente(int cursoId)
        {
            using var alcance = Services.CreateScope();
            var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
            var docente = db.Usuarios.Single(u => u.NombreUsuario == "doc01");
            var token = alcance.ServiceProvider.GetRequiredService<IJwtService>().EmitirTokenAgente(docente.Id, cursoId);
            var cliente = CreateClient();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            return cliente;
        }

        public int IdEstudiante(string apellido)
        {
            using var alcance = Services.CreateScope();
            return alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>().Estudiantes.Single(e => e.Apellido == apellido).Id;
        }
    }

    private static async Task<(Api Api, HttpClient Doc01, int Curso)> CursoSincronizadoAsync()
    {
        var api = new Api();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var curso = api.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        return (api, doc01, curso);
    }

    // --- Seudonimizador ---

    private static readonly MapaSeudonimos Mapa = new(new[]
    {
        new Seudonimo(5, "E-001", "Ana", "Pérez"),
        new Seudonimo(8, "E-002", "Luis Andrés", "Mora"),
    });

    [Fact]
    public void Ocultar_cambia_nombre_completo_apellido_unico_y_sin_tildes_por_el_codigo()
    {
        Assert.Equal("¿Cómo va E-001? ¿Y E-002?", Seudonimizador.Ocultar("¿Cómo va Ana Perez? ¿Y Mora?", Mapa));
        Assert.Equal("Notas de E-002", Seudonimizador.Ocultar("Notas de mora, luis andrés", Mapa));
        Assert.Equal("Moralidad y Anabel", Seudonimizador.Ocultar("Moralidad y Anabel", Mapa)); // solo palabras completas
    }

    [Fact]
    public void Mostrar_cambia_cada_codigo_por_el_nombre_y_deja_los_desconocidos()
    {
        Assert.Equal("Ana Pérez y Luis Andrés Mora; E-099 no existe.", Seudonimizador.Mostrar("E-001 y E-002; E-099 no existe.", Mapa));
    }

    [Fact]
    public void OcultarJson_cambia_los_estudiantes_por_su_codigo_y_quita_ids_de_moodle()
    {
        var nodo = JsonNode.Parse("""{"estudiantes":[{"id":8,"nombre":"Luis Andrés","apellido":"Mora","moodleId":102,"nota":7.5}],"texto":"Revisar a Ana Pérez"}""");

        var oculto = Seudonimizador.OcultarJson(nodo, Mapa)!.ToJsonString();

        Assert.Contains("\"codigo\":\"E-002\"", oculto);
        Assert.DoesNotContain("Mora", oculto);
        Assert.DoesNotContain("moodleId", oculto);
        Assert.Contains("Revisar a E-001", oculto);
        Assert.Contains("\"nota\":7.5", oculto);
    }

    // --- Herramientas para el agente ---

    [Fact]
    public async Task Con_el_token_del_agente_las_herramientas_dan_codigos_y_con_el_del_docente_nombres()
    {
        var (api, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = api;

        var paraAgente = await api.ClienteAgente(curso).GetStringAsync($"/api/tools/calificaciones?cursoId={curso}");
        var paraDocente = await doc01.GetStringAsync($"/api/tools/calificaciones?cursoId={curso}");

        Assert.DoesNotContain("Pérez", paraAgente);
        Assert.Contains("\"codigo\":\"E-001\"", paraAgente);
        Assert.Contains("Pérez", paraDocente);
    }

    [Fact]
    public async Task El_agente_busca_por_codigo_y_recibe_el_detalle_con_el_id_de_cada_actividad()
    {
        var (api, _, curso) = await CursoSincronizadoAsync();
        await using var __ = api;

        var detalle = await api.ClienteAgente(curso).GetFromJsonAsync<JsonElement>($"/api/tools/calificaciones?cursoId={curso}&nombre=E-002");
        var noExiste = await api.ClienteAgente(curso).GetAsync($"/api/tools/calificaciones?cursoId={curso}&nombre=E-099");

        var estudiante = detalle.GetProperty("estudiantes").EnumerateArray().Single();
        Assert.Equal("E-002", estudiante.GetProperty("codigo").GetString());
        Assert.All(estudiante.GetProperty("calificaciones").EnumerateArray(), c => Assert.True(c.GetProperty("actividadId").GetInt32() > 0));
        Assert.Equal(HttpStatusCode.NotFound, noExiste.StatusCode);
    }

    // --- Chat ---

    [Fact]
    public async Task La_pregunta_llega_al_agente_con_codigos_y_la_respuesta_vuelve_con_nombres()
    {
        var (api, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = api;
        api.Agente.Texto = "E-002 tiene 2 actividades sin entrega.";
        api.Agente.Proveedor = "EXTERNO";
        api.Agente.Cifras.Add(new CifraDto("2", "obtener_calificaciones", "estudiantes[0].notas.sinEntrega", api.IdEstudiante("Mora"), null, true));

        var respuesta = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "¿Cómo va Luis Mora?" });

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Luis Mora tiene 2 actividades sin entrega.", cuerpo.GetProperty("respuesta").GetString());
        Assert.Equal("EXTERNO", cuerpo.GetProperty("proveedor").GetString());
        Assert.False(cuerpo.GetProperty("respaldo").GetBoolean());
        Assert.True(cuerpo.GetProperty("cifras")[0].GetProperty("verificada").GetBoolean());
        var mensaje = api.Agente.Peticiones.Single().Mensaje;
        Assert.EndsWith("¿Cómo va E-002?", mensaje);
        Assert.DoesNotContain("Mora", mensaje);
        Assert.Equal("auto", api.Agente.Peticiones.Single().Modo);
    }

    [Fact]
    public async Task Una_pregunta_de_seguimiento_lleva_los_turnos_anteriores_y_queda_en_el_mismo_hilo()
    {
        var (api, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = api;

        var primera = await (await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "¿Cómo va Ana Pérez?" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var hilo = primera.GetProperty("conversacionId").GetInt32();
        Assert.Equal(hilo, primera.GetProperty("hiloId").GetInt32());

        var segunda = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "¿Y en qué actividad le fue peor?", conversacionId = hilo });

        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Equal(hilo, (await segunda.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("hiloId").GetInt32());
        var historial = api.Agente.Peticiones[1].Historial!;
        Assert.Equal("¿Cómo va E-001?", Assert.Single(historial).Pregunta);
        Assert.Empty(api.Agente.Peticiones[0].Historial!);

        var lista = await doc01.GetFromJsonAsync<JsonElement>($"/api/agente/conversaciones?cursoId={curso}");
        var unica = Assert.Single(lista.EnumerateArray());
        Assert.Equal(2, unica.GetProperty("turnos").GetInt32());

        var abierta = await doc01.GetFromJsonAsync<JsonElement>($"/api/agente/conversaciones/{hilo}");
        Assert.Equal(2, abierta.GetProperty("turnos").GetArrayLength());
    }

    [Fact]
    public async Task La_memoria_lleva_como_maximo_los_ultimos_cuatro_turnos()
    {
        var (api, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = api;
        int? hilo = null;
        for (var i = 1; i <= 6; i++)
        {
            var r = await (await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = $"Pregunta {i}", conversacionId = hilo }))
                .Content.ReadFromJsonAsync<JsonElement>();
            hilo ??= r.GetProperty("conversacionId").GetInt32();
        }

        var ultima = api.Agente.Peticiones.Last().Historial!;
        Assert.Equal(new[] { "Pregunta 2", "Pregunta 3", "Pregunta 4", "Pregunta 5" }, ultima.Select(t => t.Pregunta));
    }

    [Fact]
    public async Task Seguir_la_conversacion_de_otro_docente_o_de_otro_curso_responde_400()
    {
        var (api, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = api;
        var primera = await (await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "Hola" })).Content.ReadFromJsonAsync<JsonElement>();
        var admin = await api.ClienteConSesionAsync("admin");

        var ajena = await admin.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "Sigo", conversacionId = primera.GetProperty("conversacionId").GetInt32() });

        Assert.Equal(HttpStatusCode.BadRequest, ajena.StatusCode);
    }

    [Fact]
    public async Task El_foco_del_estudiante_llega_con_su_codigo_y_un_estudiante_ajeno_responde_400()
    {
        var (api, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = api;
        var ana = api.IdEstudiante("Pérez");

        var bien = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "¿Cómo va?", foco = new { estudianteId = ana } });
        var mal = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "¿Cómo va?", foco = new { estudianteId = 99999 } });

        Assert.Equal(HttpStatusCode.OK, bien.StatusCode);
        Assert.Contains($"Foco: estudiante E-001 (estudianteId {ana}).", api.Agente.Peticiones.Single().Mensaje);
        Assert.Equal(HttpStatusCode.BadRequest, mal.StatusCode);
    }

    // --- Modelo e interruptor ---

    [Fact]
    public async Task Modelo_solo_para_admin_y_el_interruptor_manda_al_agente_el_modo_local()
    {
        var (api, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = api;
        api.Agente.Externo = new SaludModeloExterno("anthropic", "modelo-externo", "OK", 0);
        var admin = await api.ClienteConSesionAsync("admin");

        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync("/api/admin/modelo")).StatusCode);
        var antes = await admin.GetFromJsonAsync<JsonElement>("/api/admin/modelo");
        Assert.Equal("EXTERNO", antes.GetProperty("activo").GetString());
        Assert.False(antes.GetProperty("soloLocal").GetBoolean());

        var cambio = await admin.PutAsJsonAsync("/api/admin/modelo", new { soloLocal = true });
        var despues = await cambio.Content.ReadFromJsonAsync<JsonElement>();
        await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "Hola" });

        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        Assert.Equal("LOCAL", despues.GetProperty("activo").GetString());
        Assert.True(despues.GetProperty("soloLocal").GetBoolean());
        Assert.Equal("local", api.Agente.Peticiones.Single().Modo);
    }

    [Fact]
    public async Task Modelo_con_el_agente_caido_responde_200_con_los_dos_no_disponibles()
    {
        await using var api = new Api();
        api.Agente.Caido = true;
        var admin = await api.ClienteConSesionAsync("admin");

        var modelo = await admin.GetFromJsonAsync<JsonElement>("/api/admin/modelo");

        Assert.Equal("NINGUNO", modelo.GetProperty("activo").GetString());
        Assert.Equal("NO_DISPONIBLE", modelo.GetProperty("externo").GetProperty("estado").GetString());
        Assert.Equal("NO_DISPONIBLE", modelo.GetProperty("local").GetProperty("estado").GetString());
    }

    [Fact]
    public async Task La_conversacion_guardada_conserva_quien_respondio_y_las_cifras()
    {
        var (api, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = api;
        api.Agente.Respaldo = true;
        api.Agente.Cifras.Add(new CifraDto("9,9", null, null, null, null, false));

        var r = await (await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "Hola" })).Content.ReadFromJsonAsync<JsonElement>();
        var guardada = await doc01.GetFromJsonAsync<JsonElement>($"/api/agente/conversaciones/{r.GetProperty("conversacionId").GetInt32()}");

        Assert.True(guardada.GetProperty("respaldo").GetBoolean());
        Assert.Equal("LOCAL", guardada.GetProperty("proveedor").GetString());
        Assert.False(guardada.GetProperty("cifras")[0].GetProperty("verificada").GetBoolean());
    }
}
