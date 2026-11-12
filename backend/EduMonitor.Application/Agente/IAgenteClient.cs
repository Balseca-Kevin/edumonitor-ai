using System.Text.Json;

namespace EduMonitor.Application.Agente;

/// <summary>
/// Cliente del servicio de agente (<c>POST /chat</c> en la red interna). La implementación (HttpClient tipado) vive en Infrastructure.
/// Lanza <see cref="AgenteNoDisponibleException"/> si el agente no responde a tiempo o responde con error.
/// </summary>
public interface IAgenteClient
{
    Task<RespuestaAgente> ConsultarAsync(PeticionAgente peticion, CancellationToken ct = default);

    /// <summary>HU-20 · <c>GET /salud</c> del agente: su estado, el modelo y si Ollama responde.</summary>
    Task<SaludAgente> SaludAsync(CancellationToken ct = default);
}

/// <summary>Respuesta de <c>GET /salud</c> del agente. <c>Ollama</c>: OK o NO_DISPONIBLE.</summary>
public sealed record SaludAgente(string Estado, string Modelo, string Ollama);

/// <summary>Cuerpo de <c>POST /chat</c> del agente. El token es el de servicio (rol AGENTE); nunca se registra.</summary>
public sealed record PeticionAgente(string Mensaje, int CursoId, string Token)
{
    // Si alguien registra la petición completa, el token no sale.
    public override string ToString() => $"PeticionAgente {{ CursoId = {CursoId}, Mensaje = {Mensaje.Length} caracteres }}";
}

/// <summary>Respuesta de <c>POST /chat</c> del agente: texto, herramientas usadas con su traza, modelo y duración.</summary>
public sealed record RespuestaAgente(string Respuesta, IReadOnlyList<TrazaAgente> Herramientas, string Modelo, int DuracionMs);

/// <summary>Llamada del agente a una herramienta del backend: nombre, parámetros validados, OK o ERROR y duración.</summary>
public sealed record TrazaAgente(string Nombre, JsonElement Parametros, string Estado, int DuracionMs);

/// <summary>El agente no respondió dentro del tiempo máximo, no está levantado o respondió con error.</summary>
public sealed class AgenteNoDisponibleException(string mensaje, Exception? interna = null) : Exception(mensaje, interna);
