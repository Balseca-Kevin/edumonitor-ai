using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;

namespace EduMonitor.Tests.Sincronizaciones;

/// <summary>
/// HU-08 · Bloque 2 · La sincronización importa la rúbrica de cada tarea, el nivel de cada estudiante en cada criterio y el
/// comentario del docente, con upsert por id de Moodle; el tema que el docente asoció a un criterio se conserva.
/// </summary>
public class ImportadorRubricasTests
{
    private sealed class ApiConMoodleFalso : ApiConDatosFactory
    {
        public MoodleClientFalso Moodle { get; } = ConRubrica();

        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IMoodleClient>();
            services.AddSingleton<IMoodleClient>(Moodle);
        }
    }

    /// <summary>Tarea 1 del curso de ejemplo (instancia 7, cmid 70) con una rúbrica de 2 criterios; Ana (101) la tiene calificada.</summary>
    private static MoodleClientFalso ConRubrica()
    {
        var moodle = MoodleClientFalso.ConCursoDeEjemplo();
        moodle.Rubricas.Add(new RubricaMoodle(70, 900, new[]
        {
            new CriterioRubricaMoodle(11, 1, "Redacción de historias de usuario", new[] { new NivelRubricaMoodle(1, 0m, "Insuficiente"), new NivelRubricaMoodle(2, 2.5m, "Destacado") }),
            new CriterioRubricaMoodle(12, 2, "Formato y entrega", new[] { new NivelRubricaMoodle(3, 0m, "Insuficiente"), new NivelRubricaMoodle(4, 2.5m, "Destacado") }),
        }));
        moodle.InstanciasRubrica[900] = new()
        {
            new InstanciaRubricaMoodle(40, 501, 0, new[] { new NivelElegidoMoodle(11, 1), new NivelElegidoMoodle(12, 3) }), // archivada
            new InstanciaRubricaMoodle(41, 501, 1, new[] { new NivelElegidoMoodle(11, 2), new NivelElegidoMoodle(12, 3) }),
            new InstanciaRubricaMoodle(42, 999, 1, new[] { new NivelElegidoMoodle(11, 2) }), // nota de alguien que no está matriculado
        };
        moodle.EstadosEntrega[(7, 101)] = new EstadoEntregaMoodle(501, "Revisa estos puntos:\n• Formato y entrega: sigue la plantilla.");
        return moodle;
    }

    [Fact]
    public async Task Guarda_los_criterios_los_niveles_de_la_calificacion_activa_y_el_comentario_del_docente()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");

        var sync = await (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("COMPLETADA", sync.GetProperty("estado").GetString());
        using var alcance = factory.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
        var criterios = await db.CriteriosRubrica.OrderBy(c => c.Orden).ToListAsync();
        Assert.Equal(new[] { "Redacción de historias de usuario", "Formato y entrega" }, criterios.Select(c => c.Descripcion));
        Assert.All(criterios, c => Assert.Equal(2.5m, c.PuntajeMaximo));

        var niveles = await db.CalificacionesCriterio.Include(c => c.Estudiante).Include(c => c.CriterioRubrica).OrderBy(c => c.CriterioRubrica.Orden).ToListAsync();
        Assert.Equal(new[] { ("Ana", 2.5m, "Destacado"), ("Ana", 0m, "Insuficiente") }, niveles.Select(n => (n.Estudiante.Nombre, n.Puntaje, n.Nivel)));

        var tarea = await db.Calificaciones.Include(c => c.Estudiante).SingleAsync(c => c.Actividad.Nombre == "Tarea 1" && c.Estudiante.Nombre == "Ana");
        Assert.Equal("Revisa estos puntos:\n• Formato y entrega: sigue la plantilla.", tarea.Comentario);
        // Luis no tiene nota en la Tarea 1: no se pregunta su entrega.
        Assert.Single(factory.Moodle.Llamadas, l => l == "mod_assign_get_submission_status");
    }

    [Fact]
    public async Task Una_segunda_sincronizacion_no_duplica_y_conserva_el_tema_del_criterio()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();

        using (var alcance = factory.Services.CreateScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
            var unidad = await db.Unidades.FirstAsync(u => u.CursoId == curso && u.Numero == 1);
            var tema = new Domain.Entities.Tema { UnidadId = unidad.Id, Nombre = "Historias de usuario" };
            db.Temas.Add(tema);
            await db.SaveChangesAsync();
            (await db.CriteriosRubrica.SingleAsync(c => c.MoodleCriterioId == 11)).TemaId = tema.Id;
            await db.SaveChangesAsync();
        }

        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();

        using var despues = factory.Services.CreateScope();
        var db2 = despues.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
        Assert.Equal(2, await db2.CriteriosRubrica.CountAsync());
        Assert.Equal(2, await db2.CalificacionesCriterio.CountAsync());
        Assert.NotNull((await db2.CriteriosRubrica.SingleAsync(c => c.MoodleCriterioId == 11)).TemaId);
    }

    [Fact]
    public async Task Si_Moodle_no_da_las_rubricas_la_sincronizacion_queda_parcial_y_las_notas_siguen()
    {
        await using var factory = new ApiConMoodleFalso();
        factory.Moodle.Fallar.Add("core_grading_get_definitions");
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");

        var sync = await (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("PARCIAL", sync.GetProperty("estado").GetString());
        Assert.Equal("core_grading_get_definitions", sync.GetProperty("errores")[0].GetProperty("funcion").GetString());
        Assert.Equal(4, sync.GetProperty("registros").GetProperty("calificaciones").GetInt32());
        using var alcance = factory.Services.CreateScope();
        Assert.Equal(0, await alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>().CriteriosRubrica.CountAsync());
    }
}
