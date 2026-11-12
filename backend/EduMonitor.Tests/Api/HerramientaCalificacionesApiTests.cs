using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// TA-13.1 · GET /api/tools/calificaciones. DADO un curso sincronizado, CUANDO el agente pide las calificaciones con el token del docente,
/// ENTONCES recibe notas en escala 0 a 10 y estados diferenciados (0 calificado, sin entrega); 403 en un curso ajeno, 401 sin token,
/// 400 sin cursoId y 404 si el estudiante no está en el curso. Cada llamada queda en el log sin el token.
/// </summary>
public class HerramientaCalificacionesApiTests
{
    private sealed class ApiConMoodleFalso : ApiConDatosFactory
    {
        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IMoodleClient>();
            services.AddSingleton<IMoodleClient>(MoodleClientFalso.ConCursoDeEjemplo());
        }
    }

    private static async Task<(ApiConMoodleFalso Factory, HttpClient Doc01, int Curso)> CursoSincronizadoAsync()
    {
        var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        return (factory, doc01, curso);
    }

    [Fact]
    public async Task Sin_estudianteId_devuelve_un_resumen_por_estudiante_igual_al_del_panel_y_sin_notas_por_actividad()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;

        var respuesta = await doc01.GetAsync($"/api/tools/calificaciones?cursoId={curso}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("obtener_calificaciones", cuerpo.GetProperty("herramienta").GetString());
        Assert.Equal("AGIL-4B", cuerpo.GetProperty("curso").GetProperty("nombreCorto").GetString());
        Assert.NotEqual(JsonValueKind.Null, cuerpo.GetProperty("curso").GetProperty("ultimaSincronizacion").ValueKind);

        var lista = cuerpo.GetProperty("estudiantes").EnumerateArray().ToList();
        var promedios = lista.Select(e => e.GetProperty("promedioPonderado").GetDecimal()).ToList();
        Assert.Equal(promedios.Order(), promedios); // de menor a mayor promedio
        var estudiantes = lista.ToDictionary(e => e.GetProperty("nombre").GetString()!);
        Assert.Equal(2, estudiantes.Count);
        Assert.All(estudiantes.Values, e => Assert.False(e.TryGetProperty("calificaciones", out JsonElement _)));
        Assert.Equal(1, estudiantes["Ana"].GetProperty("notas").GetProperty("ceros").GetInt32());
        Assert.Equal(1, estudiantes["Luis"].GetProperty("notas").GetProperty("sinEntrega").GetInt32());
        Assert.Equal(7.00m, estudiantes["Luis"].GetProperty("promedioPonderado").GetDecimal());

        // Mismos números que el panel del curso (GET /api/cursos/{id}/indicadores).
        var panel = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/indicadores");
        foreach (var delPanel in panel.GetProperty("estudiantes").EnumerateArray())
        {
            var resumen = estudiantes[delPanel.GetProperty("nombre").GetString()!];
            Assert.Equal(delPanel.GetProperty("id").GetInt32(), resumen.GetProperty("id").GetInt32());
            Assert.Equal(delPanel.GetProperty("apellido").GetString(), resumen.GetProperty("apellido").GetString());
            Assert.Equal(delPanel.GetProperty("promedioPonderado").ToString(), resumen.GetProperty("promedioPonderado").ToString());
            Assert.Equal(delPanel.GetProperty("parcial").GetBoolean(), resumen.GetProperty("parcial").GetBoolean());
            Assert.Equal(delPanel.GetProperty("notas").ToString(), resumen.GetProperty("notas").ToString());
        }

        Assert.Contains(factory.Logs.Mensajes, m => m.Contains("Herramienta obtener_calificaciones") && m.Contains("resultado OK"));
        Assert.DoesNotContain(factory.Logs.Mensajes, m => m.Contains("eyJ")); // ningún JWT en el log
    }

    [Fact]
    public async Task Con_estudianteId_devuelve_las_notas_y_estados_de_ese_estudiante_y_404_si_no_esta_matriculado()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;
        var resumen = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/calificaciones?cursoId={curso}");
        int Id(string nombre) => resumen.GetProperty("estudiantes").EnumerateArray().First(e => e.GetProperty("nombre").GetString() == nombre).GetProperty("id").GetInt32();

        var soloAna = (await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/calificaciones?cursoId={curso}&estudianteId={Id("Ana")}"))
            .GetProperty("estudiantes").EnumerateArray().Single();
        Assert.Equal("Ana", soloAna.GetProperty("nombre").GetString());
        var ana = soloAna.GetProperty("calificaciones").EnumerateArray().ToDictionary(c => c.GetProperty("actividad").GetString()!);
        Assert.Equal(0m, ana["Examen P1"].GetProperty("nota").GetDecimal());
        Assert.Equal("CALIFICADO", ana["Examen P1"].GetProperty("estado").GetString());
        Assert.Equal("Exámenes", ana["Examen P1"].GetProperty("categoria").GetString());
        Assert.Equal(2, ana["Examen P1"].GetProperty("unidad").GetInt32());

        var soloLuis = (await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/calificaciones?cursoId={curso}&estudianteId={Id("Luis")}"))
            .GetProperty("estudiantes").EnumerateArray().Single();
        var luis = soloLuis.GetProperty("calificaciones").EnumerateArray().ToDictionary(c => c.GetProperty("actividad").GetString()!);
        Assert.Equal(JsonValueKind.Null, luis["Tarea 1"].GetProperty("nota").ValueKind);
        Assert.Equal("SIN_ENTREGA", luis["Tarea 1"].GetProperty("estado").GetString());
        Assert.Equal(7.00m, soloLuis.GetProperty("promedioPonderado").GetDecimal());

        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync($"/api/tools/calificaciones?cursoId={curso}&estudianteId=9999")).StatusCode);
    }

    [Fact]
    public async Task Curso_ajeno_403_sin_token_401_y_sin_cursoId_400()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var bdd = factory.IdCurso("BDD-4B");

        var ajeno = await doc01.GetAsync($"/api/tools/calificaciones?cursoId={bdd}");
        Assert.Equal(HttpStatusCode.Forbidden, ajeno.StatusCode);
        Assert.Contains($"El curso {bdd} no está asignado", (await ajeno.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/tools/calificaciones?cursoId={bdd}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await doc01.GetAsync("/api/tools/calificaciones")).StatusCode);
    }

    [Fact]
    public async Task Curso_sin_sincronizar_responde_lista_vacia_sin_fecha()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");

        var cuerpo = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/calificaciones?cursoId={factory.IdCurso("AGIL-4B")}");

        Assert.Empty(cuerpo.GetProperty("estudiantes").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, cuerpo.GetProperty("curso").GetProperty("ultimaSincronizacion").ValueKind);
    }
}
