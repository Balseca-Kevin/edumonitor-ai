using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// TA-12.2 y TA-12.3 · GET/PUT /api/cursos/{id}/reglas y GET/PATCH .../alertas (docs/contratos-api.md).
/// DADO el curso del Moodle falso (Luis sin entregar la Tarea 1 con fecha límite pasada), CUANDO el docente
/// consulta o cambia reglas y sincroniza, ENTONCES las alertas se recalculan, la evidencia tiene la forma del
/// contrato y marcar una alerta como revisada la saca de las activas.
/// </summary>
public class AlertasApiTests
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

    private static readonly object R2ConUmbral1 = new { codigo = "R2", umbral = 1.0, activa = true };
    private static readonly object R2ConUmbral3 = new { codigo = "R2", umbral = 3.0, activa = true };

    [Fact]
    public async Task GET_reglas_crea_las_tres_con_sus_umbrales_por_defecto_y_no_las_duplica()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");

        var respuesta = await doc01.GetAsync($"/api/cursos/{curso}/reglas");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var reglas = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "R1", "R2", "R4" }, reglas.EnumerateArray().Select(r => r.GetProperty("codigo").GetString()));

        var r1 = reglas[0];
        Assert.Equal("Descenso reciente", r1.GetProperty("nombre").GetString());
        Assert.Equal(1.5m, r1.GetProperty("umbral").GetDecimal());
        Assert.True(r1.GetProperty("activa").GetBoolean());
        Assert.Equal("Tendencia menor o igual a −umbral con las últimas 3 notas", r1.GetProperty("condicion").GetString());
        Assert.Equal("Atrasos", reglas[1].GetProperty("nombre").GetString());
        Assert.Equal(2m, reglas[1].GetProperty("umbral").GetDecimal());
        Assert.Equal("Promedio bajo", reglas[2].GetProperty("nombre").GetString());
        Assert.Equal(7m, reglas[2].GetProperty("umbral").GetDecimal());

        // La segunda consulta no duplica las reglas.
        var otraVez = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/reglas");
        Assert.Equal(3, otraVez.GetArrayLength());
    }

    [Fact]
    public async Task PUT_reglas_cambia_el_umbral_y_responde_400_si_no_existe_o_sale_de_rango()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");

        var cambio = await doc01.PutAsJsonAsync($"/api/cursos/{curso}/reglas",
            new object[] { new { codigo = "R4", umbral = 5.0, activa = true } });
        Assert.Equal(HttpStatusCode.NoContent, cambio.StatusCode);

        var reglas = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/reglas");
        Assert.Equal(5m, reglas[2].GetProperty("umbral").GetDecimal());

        var inexistente = await doc01.PutAsJsonAsync($"/api/cursos/{curso}/reglas",
            new object[] { new { codigo = "R3", umbral = 2.0, activa = true } });
        Assert.Equal(HttpStatusCode.BadRequest, inexistente.StatusCode);

        var fueraDeRango = await doc01.PutAsJsonAsync($"/api/cursos/{curso}/reglas",
            new object[] { new { codigo = "R1", umbral = 9.0, activa = true } });
        Assert.Equal(HttpStatusCode.BadRequest, fueraDeRango.StatusCode);

        // Nada de lo inválido se escribió: R1 sigue en su umbral por defecto.
        reglas = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/reglas");
        Assert.Equal(1.5m, reglas[0].GetProperty("umbral").GetDecimal());
        Assert.Equal(5m, reglas[2].GetProperty("umbral").GetDecimal());
    }

    [Fact]
    public async Task Tras_sincronizar_se_recalculan_las_alertas_y_marcar_revisada_las_saca_de_activas()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");

        // R2 en 1 antes de sincronizar: sin datos todavía no hay alertas (lista vacía 200, nunca 409).
        (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/reglas", new object[] { R2ConUmbral1 })).EnsureSuccessStatusCode();
        Assert.Empty((await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/alertas")).EnumerateArray());

        // Al terminar la sincronización el recálculo detecta el atraso de Luis (Tarea 1 vencida sin entrega).
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();

        var activas = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/alertas?estado=ACTIVA");
        var alerta = Assert.Single(activas.EnumerateArray());
        Assert.Equal("R2", alerta.GetProperty("regla").GetString());
        Assert.Equal("Requiere atención", alerta.GetProperty("titulo").GetString());
        Assert.Equal("ACTIVA", alerta.GetProperty("estado").GetString());
        Assert.Equal("Luis", alerta.GetProperty("estudiante").GetProperty("nombre").GetString());
        Assert.Equal("Mora", alerta.GetProperty("estudiante").GetProperty("apellido").GetString());
        Assert.Equal(JsonValueKind.Null, alerta.GetProperty("revisada").ValueKind);

        var evidencia = alerta.GetProperty("evidencia");
        Assert.Equal("Tarea 1", evidencia.GetProperty("actividades")[0].GetProperty("nombre").GetString());
        Assert.Equal("2026-09-08T23:59:00-05:00", evidencia.GetProperty("actividades")[0].GetProperty("fechaLimite").GetString());
        Assert.Equal(1m, evidencia.GetProperty("umbral").GetDecimal());
        var alertaId = alerta.GetProperty("id").GetInt32();

        // Un estado distinto de REVISADA no se acepta.
        var invalida = await doc01.PatchAsJsonAsync($"/api/cursos/{curso}/alertas/{alertaId}", new { estado = "ACTIVA" });
        Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);

        var marcada = await doc01.PatchAsJsonAsync($"/api/cursos/{curso}/alertas/{alertaId}", new { estado = "REVISADA" });
        Assert.Equal(HttpStatusCode.OK, marcada.StatusCode);
        var revisada = await marcada.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("REVISADA", revisada.GetProperty("estado").GetString());
        Assert.NotEqual(JsonValueKind.Null, revisada.GetProperty("revisada").ValueKind);

        Assert.Empty((await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/alertas?estado=ACTIVA")).EnumerateArray());
        Assert.Single((await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/alertas?estado=REVISADA")).EnumerateArray());
    }

    [Fact]
    public async Task Cambiar_el_umbral_de_una_regla_recalcula_la_lista()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();

        // Con R2 en 2, Luis solo tiene 1 atraso: no se activa.
        Assert.Empty((await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/alertas")).EnumerateArray());

        // Bajarlo a 1 crea la alerta al instante.
        (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/reglas", new object[] { R2ConUmbral1 })).EnsureSuccessStatusCode();
        Assert.Single((await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/alertas")).EnumerateArray());

        // Subirlo a 3 la retira: el recálculo borra las activas que ya no aplican.
        (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/reglas", new object[] { R2ConUmbral3 })).EnsureSuccessStatusCode();
        Assert.Empty((await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/alertas")).EnumerateArray());

        // R4 en 5 no enciende nada: el curso de ejemplo tiene solo 2 notas por estudiante (mínimo 4).
        (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/reglas",
            new object[] { new { codigo = "R4", umbral = 5.0, activa = true } })).EnsureSuccessStatusCode();
        Assert.Empty((await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/alertas")).EnumerateArray());
    }

    [Fact]
    public async Task Sin_token_401_curso_ajeno_403_inexistente_404_y_estado_invalido_400()
    {
        await using var factory = new ApiConMoodleFalso();
        var anonimo = factory.CreateClient();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var admin = await factory.ClienteConSesionAsync("admin");
        var bdd = factory.IdCurso("BDD-4B");

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync($"/api/cursos/{bdd}/reglas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync($"/api/cursos/{bdd}/reglas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync($"/api/cursos/{bdd}/alertas")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/cursos/9999/reglas")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/cursos/9999/alertas")).StatusCode);

        var curso = factory.IdCurso("AGIL-4B");
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.GetAsync($"/api/cursos/{curso}/alertas?estado=XYZ")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await doc01.PatchAsJsonAsync($"/api/cursos/{curso}/alertas/9999", new { estado = "REVISADA" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await doc01.PutAsJsonAsync("/api/cursos/9999/reglas", new object[] { R2ConUmbral1 })).StatusCode);
    }
}
