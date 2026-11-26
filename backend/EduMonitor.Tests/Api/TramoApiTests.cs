using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Indicadores;
using EduMonitor.Domain.Entities;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// R.2 · Control segmentado del curso. DADO el curso de ejemplo (Tarea 1 en la unidad 1: Ana 8,5 y Luis sin entrega; Examen P1 en
/// la unidad 2: Ana 0 y Luis 7), CUANDO el docente elige un tramo, ENTONCES el backend calcula indicadores, libro y alertas solo con
/// las actividades de ese tramo; un tramo que el curso no tiene responde 400.
/// </summary>
public class TramoApiTests
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

    private static async Task<(ApiConMoodleFalso Factory, HttpClient Doc01, int Curso)> CursoSincronizadoAsync()
    {
        var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        return (factory, doc01, curso);
    }

    private static Dictionary<string, JsonElement> PorNombre(JsonElement cuerpo) =>
        cuerpo.GetProperty("estudiantes").EnumerateArray().ToDictionary(e => e.GetProperty("nombre").GetString()!);

    [Theory]
    [InlineData(null, null, null, "todo")]
    [InlineData("todo", null, null, "todo")]
    [InlineData("P2", 2, null, "p2")]
    [InlineData("u3", null, 3, "u3")]
    public void Lee_los_codigos_de_tramo(string? texto, int? parcial, int? unidad, string codigo)
    {
        Assert.True(Tramo.TryParse(texto, out var tramo));
        Assert.Equal((parcial, unidad, codigo), (tramo.Parcial, tramo.Unidad, tramo.Codigo));
    }

    [Theory]
    [InlineData("u0")]
    [InlineData("x1")]
    [InlineData("unidad1")]
    [InlineData("p")]
    public void Rechaza_lo_que_no_es_un_tramo(string texto) => Assert.False(Tramo.TryParse(texto, out _));

    [Fact]
    public void Filtrar_deja_solo_las_actividades_del_tramo_y_sus_calificaciones()
    {
        var unidades = new[] { new Unidad { Id = 1, Numero = 1, Parcial = 1 }, new Unidad { Id = 2, Numero = 3, Parcial = 2 } };
        var actividades = new[] { new Actividad { Id = 10, UnidadId = 1 }, new Actividad { Id = 11, UnidadId = 2 }, new Actividad { Id = 12 } };
        var calificaciones = actividades.Select(a => new Calificacion { ActividadId = a.Id, EstudianteId = 1, Nota = 5 }).ToList();
        var datos = new DatosCurso(unidades, actividades, Array.Empty<Estudiante>(), calificaciones);

        var parcial2 = new Tramo(2, null).Filtrar(datos);

        Assert.Equal(new[] { 11 }, parcial2.Actividades.Select(a => a.Id));
        Assert.Equal(new[] { 11 }, parcial2.Calificaciones.Select(c => c.ActividadId));
        Assert.Equal(3, Tramo.Todo.Filtrar(datos).Actividades.Count); // en «todo» entran también las que no tienen unidad
        Assert.True(new Tramo(null, 3).ExisteEn(datos));
        Assert.False(new Tramo(null, 2).ExisteEn(datos));
    }

    [Fact]
    public async Task Indicadores_del_tramo_usan_solo_sus_actividades_y_conservan_todas_las_unidades()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;

        var u1 = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/indicadores?tramo=u1");
        var u2 = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/indicadores?tramo=u2");
        var todo = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/indicadores");

        Assert.Equal("u1", u1.GetProperty("tramo").GetString());
        Assert.Equal("todo", todo.GetProperty("tramo").GetString());
        Assert.Equal(8.5m, PorNombre(u1)["Ana"].GetProperty("promedioPonderado").GetDecimal()); // solo Tarea 1
        Assert.Equal(JsonValueKind.Null, PorNombre(u1)["Luis"].GetProperty("promedioPonderado").ValueKind); // sin entrega no es 0
        Assert.Equal(1, PorNombre(u1)["Luis"].GetProperty("notas").GetProperty("sinEntrega").GetInt32());
        Assert.Equal(0m, PorNombre(u2)["Ana"].GetProperty("promedioPonderado").GetDecimal()); // solo Examen P1
        Assert.Equal(7m, PorNombre(u2)["Luis"].GetProperty("promedioPonderado").GetDecimal());
        Assert.Equal(1, u1.GetProperty("curso").GetProperty("actividadesEvaluadas").GetInt32());
        Assert.Equal(2, todo.GetProperty("curso").GetProperty("actividadesEvaluadas").GetInt32());
        Assert.Equal(4, u1.GetProperty("unidades").GetArrayLength()); // el control segmentado necesita todas las unidades

        var p1 = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/indicadores?tramo=p1");
        Assert.Equal(todo.GetProperty("curso").GetProperty("promedio").ToString(), p1.GetProperty("curso").GetProperty("promedio").ToString());
    }

    [Fact]
    public async Task Libro_del_tramo_trae_solo_sus_columnas_con_el_promedio_de_esas_notas()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;

        var libro = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/libro?tramo=u2");

        Assert.Equal("u2", libro.GetProperty("tramo").GetString());
        var unidad = Assert.Single(Assert.Single(libro.GetProperty("parciales").EnumerateArray()).GetProperty("unidades").EnumerateArray());
        Assert.Equal("Examen P1", Assert.Single(unidad.GetProperty("actividades").EnumerateArray()).GetProperty("nombre").GetString());
        var ana = PorNombre(libro)["Ana"];
        Assert.Single(ana.GetProperty("notas").EnumerateArray());
        Assert.Equal(0m, ana.GetProperty("promedioPonderado").GetDecimal());
    }

    [Fact]
    public async Task Alertas_del_tramo_son_las_del_curso_que_tambien_se_cumplen_en_ese_tramo()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;
        // Atrasos con umbral 1: Luis no entregó Tarea 1 (unidad 1), que ya venció.
        (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/reglas", new[] { new { codigo = "R2", umbral = 1m, activa = true } })).EnsureSuccessStatusCode();

        var todo = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/alertas");
        var u1 = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/alertas?tramo=u1");
        var u2 = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/alertas?tramo=u2");

        var atraso = Assert.Single(todo.EnumerateArray(), a => a.GetProperty("regla").GetString() == "R2");
        var delTramo = Assert.Single(u1.EnumerateArray());
        Assert.Equal(atraso.GetProperty("id").GetInt32(), delTramo.GetProperty("id").GetInt32()); // la misma alerta, se puede marcar revisada
        Assert.Equal("Tarea 1", delTramo.GetProperty("evidencia").GetProperty("actividades")[0].GetProperty("nombre").GetString());
        Assert.DoesNotContain(u2.EnumerateArray(), a => a.GetProperty("regla").GetString() == "R2"); // en la unidad 2 Luis entregó
    }

    [Theory]
    [InlineData("indicadores?tramo=u9")]
    [InlineData("indicadores?tramo=p3")]
    [InlineData("indicadores?tramo=semana")]
    [InlineData("libro?tramo=u9")]
    [InlineData("alertas?tramo=u9")]
    [InlineData("alertas?tramo=x")]
    public async Task Un_tramo_que_el_curso_no_tiene_responde_400(string ruta)
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;

        var respuesta = await doc01.GetAsync($"/api/cursos/{curso}/{ruta}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    // --- C7 · El mismo tramo en las herramientas del asistente ---

    [Fact]
    public async Task La_herramienta_de_calificaciones_usa_el_tramo_y_lo_dice()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;

        var u2 = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/calificaciones?cursoId={curso}&tramo=u2");
        var todo = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/calificaciones?cursoId={curso}");

        Assert.Equal("u2", u2.GetProperty("tramo").GetString());
        Assert.Equal("todo", todo.GetProperty("tramo").GetString());
        Assert.Equal(0m, PorNombre(u2)["Ana"].GetProperty("promedioPonderado").GetDecimal()); // solo Examen P1, igual que el panel
        Assert.Equal(7m, PorNombre(u2)["Luis"].GetProperty("promedioPonderado").GetDecimal());
    }

    [Fact]
    public async Task La_herramienta_de_alertas_usa_el_tramo_y_lo_dice()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;
        (await doc01.PutAsJsonAsync($"/api/cursos/{curso}/reglas", new[] { new { codigo = "R2", umbral = 1m, activa = true } })).EnsureSuccessStatusCode();

        var u1 = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/alertas?cursoId={curso}&tramo=u1");
        var u2 = await doc01.GetFromJsonAsync<JsonElement>($"/api/tools/alertas?cursoId={curso}&tramo=u2");

        Assert.Equal("u1", u1.GetProperty("tramo").GetString());
        Assert.Contains(u1.GetProperty("alertas").EnumerateArray(), a => a.GetProperty("regla").GetString() == "R2");
        Assert.DoesNotContain(u2.GetProperty("alertas").EnumerateArray(), a => a.GetProperty("regla").GetString() == "R2");
    }

    [Theory]
    [InlineData("calificaciones", "x1")]
    [InlineData("calificaciones", "u9")]
    [InlineData("alertas", "x1")]
    [InlineData("alertas", "u9")]
    public async Task Las_herramientas_responden_400_con_un_tramo_invalido_o_que_el_curso_no_tiene(string herramienta, string tramo)
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;

        var respuesta = await doc01.GetAsync($"/api/tools/{herramienta}?cursoId={curso}&tramo={tramo}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
