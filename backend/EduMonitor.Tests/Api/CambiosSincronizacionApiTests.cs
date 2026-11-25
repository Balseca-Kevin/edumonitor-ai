using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// TA-21.A1 · GET /api/cursos/{id}/sync/{sid}/cambios. DADO el curso de ejemplo ya sincronizado, CUANDO en Moodle cambia una nota,
/// llega una entrega y después se califica, ENTONCES cada sincronización guarda esas celdas con su tipo, su antes y su después.
/// </summary>
public class CambiosSincronizacionApiTests
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

    /// <summary>Cambia en el Moodle falso la nota de un ítem para un usuario.</summary>
    private static void CambiarNota(MoodleClientFalso moodle, int usuarioId, string item, decimal? nota)
    {
        var indice = moodle.Calificaciones.FindIndex(c => c.UsuarioId == usuarioId);
        var actual = moodle.Calificaciones[indice];
        moodle.Calificaciones[indice] = actual with { Items = actual.Items.Select(i => i.Nombre == item ? i with { Nota = nota } : i).ToList() };
    }

    private static async Task<int> SincronizarAsync(HttpClient cliente, int curso)
    {
        var respuesta = await cliente.PostAsync($"/api/cursos/{curso}/sync", null);
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task La_primera_sincronizacion_no_marca_celdas_y_las_siguientes_guardan_cada_cambio()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");

        var primera = await SincronizarAsync(doc01, curso);
        var p = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/sync/{primera}/cambios");
        Assert.Equal(JsonValueKind.Null, p.GetProperty("anteriorId").ValueKind);
        Assert.Equal(0, p.GetProperty("conteos").GetProperty("total").GetInt32());
        Assert.Empty(p.GetProperty("celdas").EnumerateArray());
        Assert.Empty(p.GetProperty("noDisponibles").EnumerateArray());

        // Ana sube de 0 a 6 en el examen y Luis entrega la Tarea 1, todavía sin calificar.
        CambiarNota(factory.Moodle, 101, "Examen P1", 6m);
        factory.Moodle.Entregas.Add(new EntregaMoodle(7, 102, "submitted", "notgraded"));
        var segunda = await SincronizarAsync(doc01, curso);

        var s = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/sync/{segunda}/cambios");
        Assert.Equal(segunda, s.GetProperty("sincronizacionId").GetInt32());
        Assert.Equal(primera, s.GetProperty("anteriorId").GetInt32());
        var conteos = s.GetProperty("conteos");
        Assert.Equal((2, 0, 1, 1, 0), (conteos.GetProperty("total").GetInt32(), conteos.GetProperty("notasNuevas").GetInt32(),
            conteos.GetProperty("notasCambiadas").GetInt32(), conteos.GetProperty("entregasNuevas").GetInt32(), conteos.GetProperty("otros").GetInt32()));

        // Orden del libro: Tarea 1 (unidad 1) antes que Examen P1 (unidad 2).
        var celdas = s.GetProperty("celdas").EnumerateArray().ToList();
        Assert.Equal(new[] { "Tarea 1", "Examen P1" }, celdas.Select(c => c.GetProperty("actividad").GetString()));
        var entrega = celdas[0];
        Assert.Equal(("ENTREGA_NUEVA", "Mora"), (entrega.GetProperty("tipo").GetString(), entrega.GetProperty("estudiante").GetProperty("apellido").GetString()));
        Assert.Equal("SIN_ENTREGA", entrega.GetProperty("antes").GetProperty("estado").GetString());
        Assert.Equal("PENDIENTE", entrega.GetProperty("ahora").GetProperty("estado").GetString());
        var nota = celdas[1];
        Assert.Equal(("NOTA_CAMBIADA", "Pérez"), (nota.GetProperty("tipo").GetString(), nota.GetProperty("estudiante").GetProperty("apellido").GetString()));
        Assert.Equal(0m, nota.GetProperty("antes").GetProperty("nota").GetDecimal());
        Assert.Equal(6m, nota.GetProperty("ahora").GetProperty("nota").GetDecimal());

        // El docente califica la entrega de Luis con 9: nota nueva. Una sincronización sin cambios queda en 0.
        CambiarNota(factory.Moodle, 102, "Tarea 1", 9m);
        var tercera = await SincronizarAsync(doc01, curso);
        var t = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/sync/{tercera}/cambios");
        Assert.Equal("NOTA_NUEVA", Assert.Single(t.GetProperty("celdas").EnumerateArray()).GetProperty("tipo").GetString());

        var cuarta = await SincronizarAsync(doc01, curso);
        var c = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/sync/{cuarta}/cambios");
        Assert.Equal(0, c.GetProperty("conteos").GetProperty("total").GetInt32());
        Assert.Empty(c.GetProperty("noDisponibles").EnumerateArray());
    }

    [Fact]
    public async Task Una_sincronizacion_anterior_al_registro_dice_que_sus_cambios_no_estan_disponibles()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");
        var sid = await SincronizarAsync(doc01, curso);

        using (var alcance = factory.Services.CreateScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
            var registro = await db.Sincronizaciones.SingleAsync(s => s.Id == sid);
            registro.CambiosRegistrados = false;
            await db.SaveChangesAsync();
        }

        var r = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/sync/{sid}/cambios");
        Assert.Equal(new[] { "cambios" }, r.GetProperty("noDisponibles").EnumerateArray().Select(n => n.GetString()));
        Assert.Empty(r.GetProperty("celdas").EnumerateArray());
    }

    [Fact]
    public async Task Responde_401_403_y_404_segun_el_contrato()
    {
        await using var factory = new ApiConMoodleFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var doc02 = await factory.ClienteConSesionAsync("doc02");
        var agil = factory.IdCurso("AGIL-4B");
        var bdd = factory.IdCurso("BDD-4B");
        var sid = await SincronizarAsync(doc01, agil);

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/cursos/{agil}/sync/{sid}/cambios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc02.GetAsync($"/api/cursos/{agil}/sync/{sid}/cambios")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync($"/api/cursos/{agil}/sync/9999/cambios")).StatusCode);
        // La sincronización de AGIL-4B no es de BDD-4B.
        Assert.Equal(HttpStatusCode.NotFound, (await doc02.GetAsync($"/api/cursos/{bdd}/sync/{sid}/cambios")).StatusCode);
        var admin = await factory.ClienteConSesionAsync("admin");
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/cursos/9999/sync/1/cambios")).StatusCode);
    }
}
