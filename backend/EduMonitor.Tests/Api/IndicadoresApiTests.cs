using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// TA-09.3 · GET /api/cursos/{id}/indicadores. DADO un curso sincronizado con el Moodle falso (Ana: 8,5 en la tarea y 0 en el examen;
/// Luis: sin entrega en la tarea y 7 en el examen), CUANDO el docente pide los indicadores, ENTONCES recibe el formato del contrato
/// con la nota cero y la falta de entrega diferenciadas; 401 sin token, 403 en un curso ajeno, 404 si no existe y 409 sin sincronizar.
/// </summary>
public class IndicadoresApiTests
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

    [Fact]
    public async Task Curso_sincronizado_responde_200_con_el_formato_del_contrato()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();

        var respuesta = await doc01.GetAsync($"/api/cursos/{curso}/indicadores");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(curso, cuerpo.GetProperty("cursoId").GetInt32());
        Assert.Equal(10, cuerpo.GetProperty("escala").GetInt32());
        Assert.Equal(7.0m, cuerpo.GetProperty("aprobacion").GetDecimal());
        Assert.Equal("COMPLETADA", cuerpo.GetProperty("sincronizacion").GetProperty("estado").GetString());
        Assert.Equal(4, cuerpo.GetProperty("unidades").GetArrayLength());
        Assert.Equal(10, cuerpo.GetProperty("distribucion").GetArrayLength());
        Assert.Empty(cuerpo.GetProperty("noDisponibles").EnumerateArray());

        var resumen = cuerpo.GetProperty("curso");
        Assert.Equal(2, resumen.GetProperty("estudiantes").GetInt32());
        Assert.Equal(2, resumen.GetProperty("actividadesEvaluadas").GetInt32());
        Assert.Equal(5.32m, resumen.GetProperty("promedio").GetDecimal()); // (3,64 + 7,00) / 2
        Assert.Equal(0.5m, resumen.GetProperty("tasaFinalizacion").GetDecimal());

        var estudiantes = cuerpo.GetProperty("estudiantes").EnumerateArray().ToDictionary(e => e.GetProperty("nombre").GetString()!);
        var ana = estudiantes["Ana"];
        Assert.Equal(3.64m, ana.GetProperty("promedioPonderado").GetDecimal()); // 0,4·0 + 0,3·8,5 normalizado sin APE
        Assert.True(ana.GetProperty("parcial").GetBoolean());
        Assert.Equal(1, ana.GetProperty("notas").GetProperty("ceros").GetInt32());
        Assert.Equal(0, ana.GetProperty("notas").GetProperty("sinEntrega").GetInt32());
        var luis = estudiantes["Luis"];
        Assert.Equal(1, luis.GetProperty("notas").GetProperty("sinEntrega").GetInt32());
        Assert.Equal(0, luis.GetProperty("notas").GetProperty("ceros").GetInt32());
        Assert.Equal(7.00m, luis.GetProperty("promedioPonderado").GetDecimal());
        foreach (var campo in new[] { "id", "apellido", "z", "grupo", "tasaFinalizacion", "tendencia" })
        {
            Assert.True(ana.TryGetProperty(campo, out _), $"Falta {campo}");
        }
    }

    [Fact]
    public async Task Si_la_finalizacion_fallo_la_tasa_va_en_null_y_en_noDisponibles()
    {
        await using var factory = new ApiConMoodleFalso();
        factory.Moodle.Fallar.Add("core_completion_get_activities_completion_status");
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();

        var cuerpo = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/indicadores");

        Assert.Equal("PARCIAL", cuerpo.GetProperty("sincronizacion").GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.Null, cuerpo.GetProperty("curso").GetProperty("tasaFinalizacion").ValueKind);
        Assert.Equal(new[] { "tasaFinalizacion" }, cuerpo.GetProperty("noDisponibles").EnumerateArray().Select(v => v.GetString()));
    }

    [Fact]
    public async Task Sin_token_401_curso_ajeno_403_inexistente_404_y_sin_sincronizar_409()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var admin = await factory.ClienteConSesionAsync("admin");
        var bdd = factory.IdCurso("BDD-4B");

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/cursos/{bdd}/indicadores")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync($"/api/cursos/{bdd}/indicadores")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/cursos/9999/indicadores")).StatusCode);

        var sinSincronizar = await admin.GetAsync($"/api/cursos/{bdd}/indicadores");
        Assert.Equal(HttpStatusCode.Conflict, sinSincronizar.StatusCode);
        var problema = await sinSincronizar.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Curso sin sincronizar", problema.GetProperty("title").GetString());
        Assert.Equal(409, problema.GetProperty("status").GetInt32());
    }
}
