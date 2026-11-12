using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Agente;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EduMonitor.Tests.Api;

/// <summary>
/// HU-14 · POST /api/agente/chat con un agente falso. DADO un docente con su curso, CUANDO pregunta desde la aplicación,
/// ENTONCES la API llama al agente con el token de servicio y guarda la conversación con sus trazas.
/// </summary>
public class AgenteApiTests
{
    private sealed class ApiConAgenteFalso : ApiConDatosFactory
    {
        public AgenteClientFalso Agente { get; } = new();

        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IAgenteClient>();
            services.AddSingleton<IAgenteClient>(Agente);
        }
    }

    [Fact]
    public async Task Pregunta_en_su_curso_responde_200_llama_al_agente_con_el_token_de_servicio_y_guarda_la_conversacion()
    {
        await using var factory = new ApiConAgenteFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");

        var respuesta = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "¿Qué estudiantes requieren atención?" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(cuerpo.GetProperty("conversacionId").GetInt32() > 0);
        Assert.StartsWith("Hechos:", cuerpo.GetProperty("respuesta").GetString());
        Assert.Equal("qwen2.5:7b", cuerpo.GetProperty("modelo").GetString());
        var herramienta = cuerpo.GetProperty("herramientas").EnumerateArray().Single();
        Assert.Equal("obtener_calificaciones", herramienta.GetProperty("nombre").GetString());
        Assert.Equal("OK", herramienta.GetProperty("estado").GetString());
        Assert.Equal(curso, herramienta.GetProperty("parametros").GetProperty("cursoId").GetInt32());

        var peticion = factory.Agente.Peticiones.Single();
        Assert.Equal($"Curso: AGIL-4B (cursoId {curso})\n¿Qué estudiantes requieren atención?", peticion.Mensaje);
        Assert.Equal(curso, peticion.CursoId);
        var token = new JsonWebToken(peticion.Token);
        Assert.Equal("AGENTE", token.GetClaim("role").Value);
        Assert.Equal(curso.ToString(), token.GetClaim("curso").Value);
        Assert.Equal(2, (token.ValidTo - token.IssuedAt).TotalMinutes, precision: 0);

        using var alcance = factory.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
        var guardada = await db.Conversaciones.Include(c => c.Trazas).SingleAsync();
        Assert.Equal("¿Qué estudiantes requieren atención?", guardada.Pregunta);
        Assert.Equal("qwen2.5:7b", guardada.Modelo);
        Assert.Equal(curso, guardada.CursoId);
        var traza = guardada.Trazas.Single();
        Assert.Equal("obtener_calificaciones", traza.Herramienta);
        Assert.Equal("OK", traza.Estado);
        Assert.Equal(84, traza.DuracionMs);
        Assert.DoesNotContain("eyJ", traza.Parametros); // ningún token en la traza
    }

    // --- TA-14.4 · Pruebas del proxy: 401, 403 sin llamar al agente, 503 con el agente caído ---

    [Fact]
    public async Task Sin_token_responde_401_y_no_llama_al_agente()
    {
        await using var factory = new ApiConAgenteFalso();

        var respuesta = await factory.CreateClient().PostAsJsonAsync("/api/agente/chat", new { cursoId = factory.IdCurso("AGIL-4B"), mensaje = "Hola" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Empty(factory.Agente.Peticiones);
    }

    [Fact]
    public async Task Curso_ajeno_responde_403_y_no_llama_al_agente()
    {
        await using var factory = new ApiConAgenteFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var bdd = factory.IdCurso("BDD-4B");

        var respuesta = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = bdd, mensaje = "¿Qué estudiantes requieren atención?" });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Acceso denegado", problema.GetProperty("title").GetString());
        Assert.Equal($"El curso {bdd} no está asignado a este docente.", problema.GetProperty("detail").GetString());
        Assert.Empty(factory.Agente.Peticiones);
    }

    [Fact]
    public async Task Agente_caido_responde_503_asistente_no_disponible_y_no_guarda_conversacion()
    {
        await using var factory = new ApiConAgenteFalso();
        factory.Agente.Caido = true;
        var doc01 = await factory.ClienteConSesionAsync("doc01");

        var respuesta = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = factory.IdCurso("AGIL-4B"), mensaje = "¿Qué estudiantes requieren atención?" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Asistente no disponible", problema.GetProperty("title").GetString());
        Assert.Equal(503, problema.GetProperty("status").GetInt32());
        using var alcance = factory.Services.CreateScope();
        Assert.Empty(alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>().Conversaciones);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Mensaje_vacio_responde_400_sin_llamar_al_agente(string mensaje)
    {
        await using var factory = new ApiConAgenteFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");

        var respuesta = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = factory.IdCurso("AGIL-4B"), mensaje });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(factory.Agente.Peticiones);
    }

    [Fact]
    public async Task Mensaje_de_mas_de_500_caracteres_responde_400_y_curso_inexistente_404()
    {
        await using var factory = new ApiConAgenteFalso();
        var admin = await factory.ClienteConSesionAsync("admin");

        var largo = await admin.PostAsJsonAsync("/api/agente/chat", new { cursoId = factory.IdCurso("AGIL-4B"), mensaje = new string('a', 501) });
        var inexistente = await admin.PostAsJsonAsync("/api/agente/chat", new { cursoId = 9999, mensaje = "Hola" });

        Assert.Equal(HttpStatusCode.BadRequest, largo.StatusCode);
        Assert.Contains("500 caracteres", (await largo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Empty(factory.Agente.Peticiones);
    }

    // --- HU-14 · Un código de otro curso en la pregunta se responde sin llamar al agente ---

    [Fact]
    public async Task Pregunta_por_otro_curso_del_docente_responde_que_abra_ese_curso_y_no_llama_al_agente()
    {
        await using var factory = new ApiConAgenteFalso();
        var admin = await factory.ClienteConSesionAsync("admin");
        var agil = factory.IdCurso("AGIL-4B");

        var respuesta = await admin.PostAsJsonAsync("/api/agente/chat", new { cursoId = agil, mensaje = "¿Qué estudiantes de bdd-4b tienen promedio bajo 7?" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Esta conversación es de AGIL-4B. Abre BDD-4B para preguntar sobre ese curso.", cuerpo.GetProperty("respuesta").GetString());
        Assert.Empty(cuerpo.GetProperty("herramientas").EnumerateArray());
        Assert.Equal(AgenteService.ModeloSinAgente, cuerpo.GetProperty("modelo").GetString());
        Assert.Empty(factory.Agente.Peticiones);
    }

    [Fact]
    public async Task Pregunta_por_un_curso_ajeno_responde_sin_acceso_y_no_llama_al_agente()
    {
        await using var factory = new ApiConAgenteFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");

        var respuesta = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = factory.IdCurso("AGIL-4B"), mensaje = "¿Cómo va BDD-4B?" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("No tienes acceso a BDD-4B.", cuerpo.GetProperty("respuesta").GetString());
        Assert.Empty(factory.Agente.Peticiones);
    }

    [Fact]
    public async Task Nombrar_el_curso_abierto_si_llama_al_agente()
    {
        await using var factory = new ApiConAgenteFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");

        var respuesta = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = factory.IdCurso("AGIL-4B"), mensaje = "¿Cómo va AGIL-4B?" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Single(factory.Agente.Peticiones);
    }

    // --- HU-19 · Historial de conversaciones para la barra lateral del asistente ---

    [Fact]
    public async Task El_historial_lista_las_conversaciones_del_docente_de_la_mas_reciente_a_la_mas_antigua_y_el_detalle_trae_las_herramientas()
    {
        await using var factory = new ApiConAgenteFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "Primera pregunta" })).EnsureSuccessStatusCode();
        (await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "Segunda pregunta" })).EnsureSuccessStatusCode();

        var lista = await doc01.GetFromJsonAsync<JsonElement>($"/api/agente/conversaciones?cursoId={curso}");

        var filas = lista.EnumerateArray().ToList();
        Assert.Equal(2, filas.Count);
        Assert.Equal("Segunda pregunta", filas[0].GetProperty("pregunta").GetString());
        Assert.Equal("Primera pregunta", filas[1].GetProperty("pregunta").GetString());
        Assert.Equal(1, filas[0].GetProperty("herramientas").GetInt32());
        Assert.Equal("qwen2.5:7b", filas[0].GetProperty("modelo").GetString());

        var detalle = await doc01.GetFromJsonAsync<JsonElement>($"/api/agente/conversaciones/{filas[0].GetProperty("id").GetInt32()}");
        Assert.Equal("Segunda pregunta", detalle.GetProperty("pregunta").GetString());
        Assert.StartsWith("Hechos:", detalle.GetProperty("respuesta").GetString());
        Assert.Equal(curso, detalle.GetProperty("cursoId").GetInt32());
        var herramienta = detalle.GetProperty("herramientas").EnumerateArray().Single();
        Assert.Equal("obtener_calificaciones", herramienta.GetProperty("nombre").GetString());
        Assert.Equal(curso, herramienta.GetProperty("parametros").GetProperty("cursoId").GetInt32());
        Assert.Equal("OK", herramienta.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task El_historial_responde_403_en_curso_ajeno_400_sin_cursoId_y_404_si_la_conversacion_no_existe()
    {
        await using var factory = new ApiConAgenteFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var doc02 = await factory.ClienteConSesionAsync("doc02");
        var agil = factory.IdCurso("AGIL-4B");
        var creada = await (await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = agil, mensaje = "Hola" })).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync($"/api/agente/conversaciones?cursoId={factory.IdCurso("BDD-4B")}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.GetAsync("/api/agente/conversaciones")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync("/api/agente/conversaciones?cursoId=9999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync("/api/agente/conversaciones/9999")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc02.GetAsync($"/api/agente/conversaciones/{creada.GetProperty("conversacionId").GetInt32()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/agente/conversaciones?cursoId={agil}")).StatusCode);
    }
}
