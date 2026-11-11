using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Agente;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EduMonitor.Tests.Api;

/// <summary>
/// HU-14 · POST /api/agente/chat con un agente falso. DADO un docente con su curso, CUANDO pregunta desde la aplicación,
/// ENTONCES la API llama al agente con el token de servicio y guarda la conversación con sus trazas.
/// </summary>
public class AgenteApiTests
{
    private sealed class ApiConAgenteFalso : ApiConDatosFactory
    {
        public AgenteClientFalso Agente { get; } = new();

        protected override void ConfigurarServicios(IServiceCollection services)
        {
            services.RemoveAll<IAgenteClient>();
            services.AddSingleton<IAgenteClient>(Agente);
        }
    }

    [Fact]
    public async Task Pregunta_en_su_curso_responde_200_llama_al_agente_con_el_token_de_servicio_y_guarda_la_conversacion()
    {
        await using var factory = new ApiConAgenteFalso();
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var curso = factory.IdCurso("AGIL-4B");

        var respuesta = await doc01.PostAsJsonAsync("/api/agente/chat", new { cursoId = curso, mensaje = "¿Qué estudiantes requieren atención?" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(cuerpo.GetProperty("conversacionId").GetInt32() > 0);
        Assert.StartsWith("Hechos:", cuerpo.GetProperty("respuesta").GetString());
        Assert.Equal("qwen2.5:7b", cuerpo.GetProperty("modelo").GetString());
        var herramienta = cuerpo.GetProperty("herramientas").EnumerateArray().Single();
        Assert.Equal("obtener_calificaciones", herramienta.GetProperty("nombre").GetString());
        Assert.Equal("OK", herramienta.GetProperty("estado").GetString());
        Assert.Equal(curso, herramienta.GetProperty("parametros").GetProperty("cursoId").GetInt32());

        var peticion = factory.Agente.Peticiones.Single();
        Assert.Equal($"Curso: AGIL-4B (cursoId {curso})\n¿Qué estudiantes requieren atención?", peticion.Mensaje);
        Assert.Equal(curso, peticion.CursoId);
        var token = new JsonWebToken(peticion.Token);
        Assert.Equal("AGENTE", token.GetClaim("role").Value);
        Assert.Equal(curso.ToString(), token.GetClaim("curso").Value);
        Assert.Equal(2, (token.ValidTo - token.IssuedAt).TotalMinutes, precision: 0);

        using var alcance = factory.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
        var guardada = await db.Conversaciones.Include(c => c.Trazas).SingleAsync();
        Assert.Equal("¿Qué estudiantes requieren atención?", guardada.Pregunta);
        Assert.Equal("qwen2.5:7b", guardada.Modelo);
        Assert.Equal(curso, guardada.CursoId);
        var traza = guardada.Trazas.Single();
        Assert.Equal("obtener_calificaciones", traza.Herramienta);
        Assert.Equal("OK", traza.Estado);
        Assert.Equal(84, traza.DuracionMs);
        Assert.DoesNotContain("eyJ", traza.Parametros); // ningún token en la traza
    }
}
