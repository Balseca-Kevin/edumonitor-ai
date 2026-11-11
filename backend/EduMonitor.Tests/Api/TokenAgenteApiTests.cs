using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Tests.Api;

/// <summary>
/// TA-14.2 · Token de servicio del agente. DADO el token AGENTE atado a doc01, CUANDO el agente llama a las herramientas,
/// ENTONCES se autoriza con los cursos de doc01 (AGIL-4B sí, BDD-4B no) y el token no sirve fuera de /api/tools.
/// </summary>
public class TokenAgenteApiTests(ApiConDatosFactory factory) : IClassFixture<ApiConDatosFactory>
{
    private HttpClient ClienteAgente(string usuario, string nombreCortoCurso)
    {
        using var alcance = factory.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
        var docente = db.Usuarios.Single(u => u.NombreUsuario == usuario);
        var token = alcance.ServiceProvider.GetRequiredService<IJwtService>().EmitirTokenAgente(docente.Id, factory.IdCurso(nombreCortoCurso));
        var cliente = factory.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return cliente;
    }

    [Fact]
    public async Task Herramientas_aceptan_el_token_del_agente_con_los_cursos_del_docente()
    {
        var agente = ClienteAgente("doc01", "AGIL-4B");

        var calificaciones = await agente.GetAsync($"/api/tools/calificaciones?cursoId={factory.IdCurso("AGIL-4B")}");
        var cursos = await agente.GetFromJsonAsync<JsonElement>("/api/tools/cursos-autorizados");

        Assert.Equal(HttpStatusCode.OK, calificaciones.StatusCode);
        Assert.Equal(new[] { "AGIL-4B" }, cursos.GetProperty("cursos").EnumerateArray().Select(c => c.GetProperty("nombreCorto").GetString()));
    }

    [Fact]
    public async Task Token_del_agente_responde_403_en_un_curso_ajeno_y_fuera_de_las_herramientas()
    {
        var agente = ClienteAgente("doc01", "AGIL-4B");

        var ajeno = await agente.GetAsync($"/api/tools/calificaciones?cursoId={factory.IdCurso("BDD-4B")}");

        Assert.Equal(HttpStatusCode.Forbidden, ajeno.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await agente.GetAsync("/api/cursos")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await agente.GetAsync($"/api/cursos/{factory.IdCurso("AGIL-4B")}")).StatusCode);
    }
}
