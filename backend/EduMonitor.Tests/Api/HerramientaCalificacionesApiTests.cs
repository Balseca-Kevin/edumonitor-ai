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
    public async Task Devuelve_notas_y_estados_de_todo_el_curso_con_el_formato_del_contrato()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;

        var respuesta = await doc01.GetAsync($"/api/tools/calificaciones?cursoId={curso}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("obtener_calificaciones", cuerpo.GetProperty("herramienta").GetString());
        Assert.Equal("AGIL-4B", cuerpo.GetProperty("curso").GetProperty("nombreCorto").GetString());
        Assert.NotEqual(JsonValueKind.Null, cuerpo.GetProperty("curso").GetProperty("ultimaSincronizacion").ValueKind);

        var estudiantes = cuerpo.GetProperty("estudiantes").EnumerateArray().ToDictionary(e => e.GetProperty("nombre").GetString()!);
        Assert.Equal(2, estudiantes.Count);
        var ana = estudiantes["Ana"].GetProperty("calificaciones").EnumerateArray().ToDictionary(c => c.GetProperty("actividad").GetString()!);
        Assert.Equal(0m, ana["Examen P1"].GetProperty("nota").GetDecimal());
        Assert.Equal("CALIFICADO", ana["Examen P1"].GetProperty("estado").GetString());
        Assert.Equal("Exámenes", ana["Examen P1"].GetProperty("categoria").GetString());
        Assert.Equal(2, ana["Examen P1"].GetProperty("unidad").GetInt32());
        var luis = estudiantes["Luis"].GetProperty("calificaciones").EnumerateArray().ToDictionary(c => c.GetProperty("actividad").GetString()!);
        Assert.Equal(JsonValueKind.Null, luis["Tarea 1"].GetProperty("nota").ValueKind);
        Assert.Equal("SIN_ENTREGA", luis["Tarea 1"].GetProperty("estado").GetString());
        Assert.Equal(7.00m, estudiantes["Luis"].GetProperty("promedioPonderado").GetDecimal());

        Assert.Contains(factory.Logs.Mensajes, m => m.Contains("Herramienta obtener_calificaciones") && m.Contains("resultado OK"));
        Assert.DoesNotContain(factory.Logs.Mensajes, m => m.Contains("eyJ")); // ningún JWT en el log
    }

    [Fact]
    public async Task Con_estudianteId_devuelve_solo_ese_estudiante_y_404_si_no_esta_matriculado()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;
        var todos = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/calificaciones?cursoId={curso}");
        var idAna = todos.GetProperty("estudiantes").EnumerateArray().First(e => e.GetProperty("nombre").GetString() == "Ana").GetProperty("id").GetInt32();

        var soloAna = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/calificaciones?cursoId={curso}&estudianteId={idAna}");

        Assert.Equal("Ana", soloAna.GetProperty("estudiantes").EnumerateArray().Single().GetProperty("nombre").GetString());
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
