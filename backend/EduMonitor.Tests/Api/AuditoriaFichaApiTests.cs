using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Application.Agente;
using EduMonitor.Application.Auditorias;
using EduMonitor.Application.Fichas;
using EduMonitor.Application.Indicadores;
using EduMonitor.Domain.Entities;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// HU-17 · Auditoría sin secretos: sesiones, sincronizaciones, preguntas al asistente con el modelo que respondió, herramientas y
/// accesos no permitidos, visibles solo para ADMIN. HU-10 · Ficha del estudiante con las mismas notas y estados del libro.
/// </summary>
public class AuditoriaFichaApiTests
{
    private sealed class Api : ApiConDatosFactory
    {
        public AgenteClientFalso Agente { get; } = new();

        public MoodleClientFalso Moodle { get; } = MoodleClientFalso.ConCursoDeEjemplo();

        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IAgenteClient>();
            services.AddSingleton<IAgenteClient>(Agente);
            services.RemoveAll<IMoodleClient>();
            services.AddSingleton<IMoodleClient>(Moodle);
        }

        public int IdEstudiante(string apellido)
        {
            using var alcance = Services.CreateScope();
            return alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>().Estudiantes.Single(e => e.Apellido == apellido).Id;
        }
    }

    private static async Task<JsonElement> AuditoriaAsync(Api api, string consulta = "")
    {
        var admin = await api.ClienteConSesionAsync("admin");
        return await admin.GetFromJsonAsync<JsonElement>($"/api/auditoria{consulta}");
    }

    private static IEnumerable<JsonElement> Registros(JsonElement respuesta, string accion) =>
        respuesta.GetProperty("registros").EnumerateArray().Where(r => r.GetProperty("accion").GetString() == accion);

    // --- HU-17 ---

    [Fact]
    public void SinSecretos_enmascara_tokens_y_claves()
    {
        var limpio = AuditoriaService.SinSecretos("token=abc123 Bearer eyJhb.eyJz.firma clave: sk-ant-12345678 contrasena=x1");

        Assert.DoesNotContain("abc123", limpio);
        Assert.DoesNotContain("eyJ", limpio);
        Assert.DoesNotContain("sk-ant", limpio);
        Assert.DoesNotContain("x1", limpio);
    }

    [Fact]
    public async Task Inicio_de_sesion_correcto_y_fallido_quedan_sin_la_contrasena()
    {
        await using var api = new Api();
        await api.ClienteConSesionAsync("doc01");
        await api.CreateClient().PostAsJsonAsync("/api/auth/login", new { usuario = "doc01", contrasena = "una-clave-equivocada" });

        var registros = Registros(await AuditoriaAsync(api, "?accion=INICIAR_SESION"), "INICIAR_SESION").ToList();

        var fallido = registros.First(r => r.GetProperty("resultado").GetString() == "401");
        Assert.Equal(JsonValueKind.Null, fallido.GetProperty("usuario").ValueKind);
        Assert.Equal("usuario doc01", fallido.GetProperty("detalle").GetString());
        var correcto = registros.First(r => r.GetProperty("resultado").GetString() == "200" && r.GetProperty("usuario").ValueKind == JsonValueKind.Object
            && r.GetProperty("usuario").GetProperty("usuario").GetString() == "doc01");
        Assert.Equal("/api/auth/login", correcto.GetProperty("entidad").GetString());
        Assert.DoesNotContain(registros, r => r.ToString().Contains("una-clave-equivocada") || r.ToString().Contains(ApiConDatosFactory.ContrasenaPrueba));
    }

    [Fact]
    public async Task Sincronizacion_pregunta_al_asistente_herramienta_y_acceso_denegado_quedan_registrados()
    {
        await using var api = new Api();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var doc02 = await api.ClienteConSesionAsync("doc02");
        var curso = api.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        api.Agente.Respaldo = true;
        await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "¿Cómo va el curso?" });
        await doc02.GetAsync($"/api/cursos/{curso}/libro");
        using (var alcance = api.Services.CreateScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
            var docente = db.Usuarios.Single(u => u.NombreUsuario == "doc01");
            var token = alcance.ServiceProvider.GetRequiredService<IJwtService>().EmitirTokenAgente(docente.Id, curso);
            var agente = api.CreateClient();
            agente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            await agente.GetAsync($"/api/tools/alertas?cursoId={curso}");
        }

        var todo = await AuditoriaAsync(api);

        var sync = Registros(todo, "SINCRONIZAR").Single();
        Assert.StartsWith("COMPLETADA", sync.GetProperty("detalle").GetString());
        Assert.Equal(curso, sync.GetProperty("cursoId").GetInt32());
        var chat = Registros(todo, "CONSULTAR_ASISTENTE").Single();
        Assert.Contains("Respondió: modelo local (respaldo)", chat.GetProperty("detalle").GetString());
        var herramienta = Registros(todo, "HERRAMIENTA").Single();
        Assert.Equal("identificar_alertas · pedida por el asistente", herramienta.GetProperty("detalle").GetString());
        Assert.Equal("doc01", herramienta.GetProperty("usuario").GetProperty("usuario").GetString());
        var denegado = Registros(todo, "ACCESO_DENEGADO").Single();
        Assert.Equal("doc02", denegado.GetProperty("usuario").GetProperty("usuario").GetString());
        Assert.Equal("403", denegado.GetProperty("resultado").GetString());
        Assert.DoesNotContain("eyJ", todo.ToString());
    }

    [Fact]
    public async Task Auditoria_es_solo_para_admin_y_filtra_por_accion_y_limite()
    {
        await using var api = new Api();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var admin = await api.ClienteConSesionAsync("admin");

        Assert.Equal(HttpStatusCode.Forbidden, (await doc01.GetAsync("/api/auditoria")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/auditoria?limite=0")).StatusCode);
        var uno = await admin.GetFromJsonAsync<JsonElement>("/api/auditoria?accion=INICIAR_SESION&limite=1");
        Assert.Single(uno.GetProperty("registros").EnumerateArray());
        Assert.True(uno.GetProperty("total").GetInt32() >= 2);
        Assert.Contains("INICIAR_SESION", uno.GetProperty("acciones").EnumerateArray().Select(a => a.GetString()));
    }

    // --- HU-10 ---

    [Fact]
    public void Evolucion_da_el_promedio_ponderado_con_las_notas_hasta_cada_actividad()
    {
        var examen = new Actividad { Id = 1, Nombre = "Examen", Categoria = "Exámenes" };
        var tarea = new Actividad { Id = 2, Nombre = "Tarea 1", Categoria = "Tareas" };

        var puntos = CalculadoraFicha.Evolucion(new[] { new NotaCalculo(examen, 6m), new NotaCalculo(tarea, 8m) });

        Assert.Equal(new decimal?[] { 6.00m, 6.86m }, puntos.Select(p => p.Promedio)); // (0,4·6 + 0,3·8) / 0,7
        Assert.Equal(new[] { 6m, 8m }, puntos.Select(p => p.Nota));
    }

    [Fact]
    public async Task Ficha_muestra_las_mismas_notas_y_estados_que_el_libro()
    {
        await using var api = new Api();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var curso = api.IdCurso("AGIL-4B");
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        var luis = api.IdEstudiante("Mora");

        var ficha = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/estudiantes/{luis}");
        var libro = await doc01.GetFromJsonAsync<JsonElement>($"/api/cursos/{curso}/libro");

        var fila = libro.GetProperty("estudiantes").EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == luis);
        var delLibro = fila.GetProperty("notas").EnumerateArray().Select(n => (n.GetProperty("actividadId").GetInt32(), n.GetProperty("estado").ToString(), n.GetProperty("nota").ToString())).ToList();
        var deLaFicha = ficha.GetProperty("actividades").EnumerateArray().Select(a => (a.GetProperty("id").GetInt32(), a.GetProperty("estado").ToString(), a.GetProperty("nota").ToString())).ToList();
        Assert.Equal(delLibro, deLaFicha);
        Assert.Equal("Mora", ficha.GetProperty("estudiante").GetProperty("apellido").GetString());
        Assert.Equal(fila.GetProperty("promedioPonderado").ToString(), ficha.GetProperty("promedioPonderado").ToString());
        Assert.Equal(7.0m, ficha.GetProperty("aprobacion").GetDecimal());
    }

    [Fact]
    public async Task Ficha_responde_404_403_y_409_como_el_resto_del_curso()
    {
        await using var api = new Api();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var doc02 = await api.ClienteConSesionAsync("doc02");
        var curso = api.IdCurso("AGIL-4B");

        Assert.Equal(HttpStatusCode.Conflict, (await doc01.GetAsync($"/api/cursos/{curso}/estudiantes/1")).StatusCode);
        (await doc01.PostAsync($"/api/cursos/{curso}/sync", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await doc01.GetAsync($"/api/cursos/{curso}/estudiantes/99999")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc02.GetAsync($"/api/cursos/{curso}/estudiantes/{api.IdEstudiante("Mora")}")).StatusCode);
    }
}
