using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Indicadores;
using EduMonitor.Application.Libro;
using EduMonitor.Domain.Entities;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// TA-21.1 · GET /api/cursos/{id}/libro. DADO un curso sincronizado con el Moodle falso (Ana: 8,5 en Tarea 1 y 0 en Examen P1;
/// Luis: sin entrega en Tarea 1 y 7 en Examen P1), CUANDO el docente abre el libro, ENTONCES ve cada nota de Moodle en su
/// actividad, agrupada por parcial y unidad, con el cero calificado distinto de sin entrega y el mismo promedio del panel;
/// 401 sin token, 403 en un curso ajeno, 404 si no existe, 409 sin sincronizar y 400 con un orden desconocido.
/// </summary>
public class LibroApiTests
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

    [Fact]
    public async Task Curso_sincronizado_muestra_cada_nota_por_parcial_y_unidad_con_cero_y_sin_entrega_distintos()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;

        var respuesta = await doc01.GetAsync($"/api/cursos/{curso}/libro");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var libro = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(curso, libro.GetProperty("cursoId").GetInt32());
        Assert.Equal(10, libro.GetProperty("escala").GetInt32());
        Assert.Equal(7.0m, libro.GetProperty("aprobacion").GetDecimal());
        Assert.Equal("COMPLETADA", libro.GetProperty("sincronizacion").GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.String, libro.GetProperty("sincronizacion").GetProperty("origen").ValueKind); // el Moodle de donde salen las notas
        Assert.Empty(libro.GetProperty("noDisponibles").EnumerateArray());

        // Parcial 1 con la Unidad 1 (Tarea 1) y la Unidad 2 (Examen P1); las unidades 3 y 4 no tienen actividades y no aparecen.
        var parcial = Assert.Single(libro.GetProperty("parciales").EnumerateArray());
        Assert.Equal(1, parcial.GetProperty("numero").GetInt32());
        var unidades = parcial.GetProperty("unidades").EnumerateArray().ToList();
        Assert.Equal(new[] { "Unidad 1", "Unidad 2" }, unidades.Select(u => u.GetProperty("nombre").GetString()));
        var tarea = Assert.Single(unidades[0].GetProperty("actividades").EnumerateArray());
        var examen = Assert.Single(unidades[1].GetProperty("actividades").EnumerateArray());
        Assert.Equal("Tarea 1", tarea.GetProperty("nombre").GetString());
        Assert.Equal("Tareas", tarea.GetProperty("categoria").GetString());
        Assert.Equal(new DateTimeOffset(2026, 9, 8, 23, 59, 0, TimeSpan.FromHours(-5)), tarea.GetProperty("fechaLimite").GetDateTimeOffset());
        Assert.Equal("Exámenes", examen.GetProperty("categoria").GetString());
        var idTarea = tarea.GetProperty("id").GetInt32();
        var idExamen = examen.GetProperty("id").GetInt32();

        var estudiantes = libro.GetProperty("estudiantes").EnumerateArray().ToList();
        Assert.Equal(new[] { "Mora", "Pérez" }, estudiantes.Select(e => e.GetProperty("apellido").GetString())); // por apellido
        var ana = estudiantes.Single(e => e.GetProperty("nombre").GetString() == "Ana");
        var luis = estudiantes.Single(e => e.GetProperty("nombre").GetString() == "Luis");

        // Una nota por actividad, en el orden de las columnas.
        Assert.Equal(new[] { idTarea, idExamen }, ana.GetProperty("notas").EnumerateArray().Select(n => n.GetProperty("actividadId").GetInt32()));
        Assert.Equal((8.5m, "CALIFICADO"), Nota(ana, idTarea));
        Assert.Equal((0m, "CALIFICADO"), Nota(ana, idExamen)); // cero calificado
        var sinEntrega = luis.GetProperty("notas")[0];
        Assert.Equal(JsonValueKind.Null, sinEntrega.GetProperty("nota").ValueKind); // sin entrega no es 0
        Assert.Equal("SIN_ENTREGA", sinEntrega.GetProperty("estado").GetString());
        Assert.Equal((7m, "CALIFICADO"), Nota(luis, idExamen));

        // El mismo promedio y la misma marca de parcial que el panel del curso.
        var panel = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/indicadores");
        foreach (var delPanel in panel.GetProperty("estudiantes").EnumerateArray())
        {
            var delLibro = estudiantes.Single(e => e.GetProperty("id").GetInt32() == delPanel.GetProperty("id").GetInt32());
            Assert.Equal(delPanel.GetProperty("promedioPonderado").ToString(), delLibro.GetProperty("promedioPonderado").ToString());
            Assert.Equal(delPanel.GetProperty("parcial").GetBoolean(), delLibro.GetProperty("parcial").GetBoolean());
        }
    }

    [Fact]
    public async Task Busqueda_y_orden_por_promedio_filtran_y_ordenan_sin_cambiar_las_columnas()
    {
        var (factory, doc01, curso) = await CursoSincronizadoAsync();
        await using var _ = factory;

        var porPromedio = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/libro?orden=promedio");
        Assert.Equal(new[] { "Ana", "Luis" }, porPromedio.GetProperty("estudiantes").EnumerateArray().Select(e => e.GetProperty("nombre").GetString())); // 3,64 antes de 7,00

        var busqueda = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/libro?busqueda=perez");
        var unico = Assert.Single(busqueda.GetProperty("estudiantes").EnumerateArray());
        Assert.Equal("Ana", unico.GetProperty("nombre").GetString());
        Assert.Single(busqueda.GetProperty("parciales").EnumerateArray());

        var nadie = await doc01.GetAsync($"/api/cursos/{curso}/libro?busqueda=zoila");
        Assert.Equal(HttpStatusCode.OK, nadie.StatusCode);
        Assert.Empty((await nadie.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estudiantes").EnumerateArray());

        var invalido = await doc01.GetAsync($"/api/cursos/{curso}/libro?orden=nota");
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    [Fact]
    public async Task Sin_entregas_de_Moodle_avisa_que_los_estados_no_estan_disponibles()
    {
        await using var factory = new ApiConMoodleFalso();
        factory.Moodle.Fallar.Add("mod_assign_get_submissions");
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();

        var libro = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/libro");

        Assert.Equal("PARCIAL", libro.GetProperty("sincronizacion").GetProperty("estado").GetString());
        Assert.Equal(new[] { "estadosEntrega" }, libro.GetProperty("noDisponibles").EnumerateArray().Select(n => n.GetString()));
    }

    [Fact]
    public async Task Responde_401_403_404_y_409_segun_el_contrato()
    {
        await using var factory = new ApiConMoodleFalso();
        var agil = factory.IdCurso("AGIL-4B");
        var bdd = factory.IdCurso("BDD-4B");
        var doc01 = await factory.ClienteConSesionAsync("doc01");

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/cursos/{agil}/libro")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync($"/api/cursos/{bdd}/libro")).StatusCode);
        var admin = await factory.ClienteConSesionAsync("admin");
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/cursos/9999/libro")).StatusCode);

        var sinSincronizar = await doc01.GetAsync($"/api/cursos/{agil}/libro");
        Assert.Equal(HttpStatusCode.Conflict, sinSincronizar.StatusCode);
        Assert.Equal("Curso sin sincronizar", (await sinSincronizar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
    }

    [Fact]
    public void Actividades_sin_unidad_van_al_final_en_el_parcial_0()
    {
        var unidades = new[]
        {
            new Unidad { Id = 1, Numero = 3, Parcial = 2, Nombre = "Unidad 3" },
            new Unidad { Id = 2, Numero = 1, Parcial = 1, Nombre = "Unidad 1" },
        };
        var actividades = new[]
        {
            new Actividad { Id = 10, Nombre = "Suelta", MoodleItemId = 1 },
            new Actividad { Id = 11, Nombre = "Tarea 5", UnidadId = 1, MoodleItemId = 2 },
            new Actividad { Id = 12, Nombre = "Tarea 1", UnidadId = 2, MoodleItemId = 3 },
        };

        var columnas = LibroService.Columnas(new DatosCurso(unidades, actividades, Array.Empty<Estudiante>(), Array.Empty<Calificacion>()));

        Assert.Equal(new[] { 1, 2, 0 }, columnas.Select(p => p.Numero));
        Assert.Equal("Tarea 1", columnas[0].Unidades.Single().Actividades.Single().Nombre);
        Assert.Equal("Tarea 5", columnas[1].Unidades.Single().Actividades.Single().Nombre);
        Assert.Equal(LibroService.NombreSinUnidad, columnas[2].Unidades.Single().Nombre);
        Assert.Equal("Suelta", columnas[2].Unidades.Single().Actividades.Single().Nombre);
    }

    private static (decimal?, string?) Nota(JsonElement estudiante, int actividadId)
    {
        var nota = estudiante.GetProperty("notas").EnumerateArray().Single(n => n.GetProperty("actividadId").GetInt32() == actividadId);
        return (nota.GetProperty("nota").ValueKind == JsonValueKind.Null ? null : nota.GetProperty("nota").GetDecimal(), nota.GetProperty("estado").GetString());
    }
}
