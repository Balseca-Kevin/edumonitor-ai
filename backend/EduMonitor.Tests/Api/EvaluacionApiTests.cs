using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Agente;
using EduMonitor.Application.Evaluacion;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// HU-18 · TA-18.A1 · DADO un ADMIN, CUANDO corre la evaluación del asistente, ENTONCES se hacen las 18 preguntas con el modelo externo y
/// con el local, cada una con su criterio de aprobación, y el resultado se ve en «Calidad del asistente».
/// </summary>
public class EvaluacionApiTests
{
    private sealed class Api : ApiConDatosFactory
    {
        public AgenteClientFalso Agente { get; } = new();

        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IAgenteClient>();
            services.AddSingleton<IAgenteClient>(Agente);
        }
    }

    private static RespuestaChatDto Respuesta(string proveedor, params (string Nombre, string Estado)[] herramientas) =>
        new(1, "Respuesta", herramientas.Select(h => new HerramientaUsadaDto(h.Nombre, JsonSerializer.SerializeToElement(new { }), h.Estado, 10)).ToList(), "modelo", 100)
        {
            Proveedor = proveedor,
        };

    // --- Criterio de aprobación ---

    [Fact]
    public void Aprueba_con_la_herramienta_esperada_en_OK_y_sin_cifras_sin_verificar()
    {
        var pregunta = PreguntasEvaluacion.Todas.Single(p => p.Numero == 2);

        Assert.True(CalculadoraEvaluacion.Calificar(pregunta, Respuesta("LOCAL", ("obtener_calificaciones", "OK"))).Aprobada);
        Assert.False(CalculadoraEvaluacion.Calificar(pregunta, Respuesta("LOCAL", ("obtener_calificaciones", "ERROR"))).Aprobada);
        Assert.False(CalculadoraEvaluacion.Calificar(pregunta, Respuesta("LOCAL", ("identificar_alertas", "OK"))).Aprobada);
        var conCifraSuelta = Respuesta("LOCAL", ("obtener_calificaciones", "OK")) with { };
        conCifraSuelta = conCifraSuelta with { Cifras = new[] { new CifraDto("9,9", null, null, null, null, false) } };
        var (aprobada, motivo) = CalculadoraEvaluacion.Calificar(pregunta, conCifraSuelta);
        Assert.False(aprobada);
        Assert.Contains("9,9", motivo);
    }

    [Fact]
    public void Fuera_de_alcance_y_otro_curso_aprueban_solo_sin_modelo()
    {
        var fuera = PreguntasEvaluacion.Todas.Single(p => p.Numero == 16);

        Assert.True(CalculadoraEvaluacion.Calificar(fuera, Respuesta("NINGUNO")).Aprobada);
        Assert.False(CalculadoraEvaluacion.Calificar(fuera, Respuesta("EXTERNO", ("obtener_calificaciones", "OK"))).Aprobada);
    }

    [Fact]
    public void Son_18_preguntas_10_de_TA_13_7_5_de_seguimiento_y_3_fuera_de_alcance()
    {
        var todas = PreguntasEvaluacion.Todas;

        Assert.Equal(Enumerable.Range(1, 18), todas.Select(p => p.Numero));
        Assert.Equal(5, todas.Count(p => p.SeguimientoDe is not null));
        Assert.Equal(3, todas.Count(p => p.Tipo == "Fuera de alcance"));
        Assert.All(todas.Where(p => p.SeguimientoDe is int b), p => Assert.True(p.SeguimientoDe < p.Numero));
    }

    // --- Rutas ---

    // La base de prueba es una sola conexión SQLite en memoria: mientras corre la evaluación no se consulta la API, se espera en la cola
    // (en memoria) y se lee el resultado al terminar. En SQL Server cada consulta usa su propia conexión.
    private static async Task<JsonElement> EsperarAsync(Api api, HttpClient admin, int id)
    {
        var cola = api.Services.GetRequiredService<ColaEvaluaciones>();
        for (var i = 0; i < 400 && cola.EnCurso == id; i++)
        {
            await Task.Delay(50);
        }

        Assert.Null(cola.EnCurso);
        return await admin.GetFromJsonAsync<JsonElement>($"/api/admin/evaluacion-agente/{id}");
    }

    [Fact]
    public async Task Corre_las_18_preguntas_con_los_dos_modelos_y_sigue_el_hilo_en_las_de_seguimiento()
    {
        await using var api = new Api();
        var admin = await api.ClienteConSesionAsync("admin");
        var curso = api.IdCurso("AGIL-4B");

        var inicio = await admin.PostAsJsonAsync("/api/admin/evaluacion-agente", new { cursoId = curso });
        Assert.Equal(HttpStatusCode.Accepted, inicio.StatusCode);
        var id = (await inicio.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var evaluacion = await EsperarAsync(api, admin, id);

        Assert.Equal("TERMINADA", evaluacion.GetProperty("estado").GetString());
        Assert.Equal(18, evaluacion.GetProperty("preguntas").GetArrayLength());
        Assert.Equal(36, evaluacion.GetProperty("resultados").GetArrayLength());
        // El agente falso responde con obtener_calificaciones: las preguntas que la esperan aprueban; las fuera de alcance no llegan al agente.
        var externo = evaluacion.GetProperty("resumen").EnumerateArray().Single(r => r.GetProperty("modo").GetString() == "EXTERNO");
        Assert.Equal(18, externo.GetProperty("respondidas").GetInt32());
        Assert.Equal(new[] { "externo", "local" }, api.Agente.Peticiones.Select(p => p.Modo).Distinct());
        // Pregunta 11 sigue el hilo de la 2: lleva su turno como memoria.
        var seguimiento = api.Agente.Peticiones.First(p => p.Mensaje.EndsWith("¿En cuál de esas actividades le fue peor?"));
        Assert.Single(seguimiento.Historial!);

        var lista = await admin.GetFromJsonAsync<JsonElement>("/api/admin/evaluacion-agente");
        Assert.Equal(id, lista[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Un_modelo_que_no_responde_queda_como_no_respondio_sin_detener_la_evaluacion()
    {
        await using var api = new Api();
        api.Agente.Caido = true;
        var admin = await api.ClienteConSesionAsync("admin");

        var id = (await (await admin.PostAsJsonAsync("/api/admin/evaluacion-agente", new { cursoId = api.IdCurso("AGIL-4B") }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var evaluacion = await EsperarAsync(api, admin, id);

        Assert.Equal("TERMINADA", evaluacion.GetProperty("estado").GetString());
        var caida = evaluacion.GetProperty("resultados").EnumerateArray().First(r => r.GetProperty("numero").GetInt32() == 2);
        Assert.False(caida.GetProperty("respondio").GetBoolean());
        Assert.StartsWith("No respondió", caida.GetProperty("motivo").GetString());
    }

    [Fact]
    public async Task Es_solo_para_admin_y_valida_el_curso()
    {
        await using var api = new Api();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var admin = await api.ClienteConSesionAsync("admin");

        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.PostAsJsonAsync("/api/admin/evaluacion-agente", new { cursoId = api.IdCurso("AGIL-4B") })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync("/api/admin/evaluacion-agente")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/evaluacion-agente", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync("/api/admin/evaluacion-agente", new { cursoId = 99999 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/admin/evaluacion-agente/99999")).StatusCode);
    }
}
