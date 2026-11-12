using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Application.Agente;
using Microsoft.Extensions.Logging;

namespace EduMonitor.Infrastructure.Agente;

/// <summary>
/// TA-14.1 · Llama a <c>POST /chat</c> del servicio de agente (<c>Agente__Url</c>; en Docker, <c>http://agent:8000</c> por la red interna).
/// Si no responde en <c>Agente__TiempoEsperaSegundos</c> (60 s), no está levantado o responde con error, lanza
/// <see cref="AgenteNoDisponibleException"/>. El token de servicio nunca se registra.
/// </summary>
public sealed class AgenteClient(HttpClient http, ILogger<AgenteClient> logger) : IAgenteClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<SaludAgente> SaludAsync(CancellationToken ct = default)
    {
        try
        {
            var salud = await http.GetFromJsonAsync<SaludAgente>("salud", Json, ct);
            return salud ?? throw new AgenteNoDisponibleException("El agente devolvió una respuesta vacía en /salud.");
        }
        catch (HttpRequestException e)
        {
            throw new AgenteNoDisponibleException("No se pudo conectar con el agente.", e);
        }
        catch (JsonException e)
        {
            throw new AgenteNoDisponibleException("El agente respondió /salud con un formato inesperado.", e);
        }
    }

    public async Task<RespuestaAgente> ConsultarAsync(PeticionAgente peticion, CancellationToken ct = default)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.PostAsJsonAsync("chat", new { mensaje = peticion.Mensaje, cursoId = peticion.CursoId, token = peticion.Token }, Json, ct);
        }
        catch (TaskCanceledException e) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("El agente no respondió en {Segundos} s (curso {CursoId})", http.Timeout.TotalSeconds, peticion.CursoId);
            throw new AgenteNoDisponibleException($"El asistente no respondió en {http.Timeout.TotalSeconds:0} segundos.", e);
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning("No se pudo conectar con el agente (curso {CursoId}): {Mensaje}", peticion.CursoId, e.Message);
            throw new AgenteNoDisponibleException("El asistente no está disponible. Revisa que el servicio del agente esté encendido.", e);
        }

        using (respuesta)
        {
            if (!respuesta.IsSuccessStatusCode)
            {
                logger.LogWarning("El agente respondió HTTP {Codigo} (curso {CursoId})", (int)respuesta.StatusCode, peticion.CursoId);
                throw new AgenteNoDisponibleException($"El asistente respondió con un error (HTTP {(int)respuesta.StatusCode}).");
            }

            try
            {
                var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAgente>(Json, ct);
                return cuerpo is null || cuerpo.Respuesta is null
                    ? throw new AgenteNoDisponibleException("El asistente devolvió una respuesta vacía.")
                    : cuerpo with { Herramientas = cuerpo.Herramientas ?? Array.Empty<TrazaAgente>(), Modelo = cuerpo.Modelo ?? string.Empty };
            }
            catch (JsonException e)
            {
                throw new AgenteNoDisponibleException("El asistente devolvió una respuesta con un formato inesperado.", e);
            }
        }
    }
}
