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

/// <summary>
/// Respuesta de <c>GET /salud</c> del agente. <c>Ollama</c>: OK o NO_DISPONIBLE. HU-23: <c>Externo</c> dice si el modelo externo
/// está configurado o en pausa, y <c>Activo</c> cuál responde ahora (EXTERNO o LOCAL).
/// </summary>
public sealed record SaludAgente(string Estado, string Modelo, string Ollama, SaludModeloExterno? Externo = null, string? Activo = null);

/// <summary>HU-23 · Modelo externo según el agente: <c>Estado</c> OK, NO_CONFIGURADO o EN_PAUSA (3 fallos en 5 minutos).</summary>
public sealed record SaludModeloExterno(string? Proveedor, string? Modelo, string Estado, int PausaSegundos);

/// <summary>HU-23 · Un turno anterior de la conversación, ya seudonimizado.</summary>
public sealed record TurnoAgente(string Pregunta, string Respuesta);

/// <summary>
/// Cuerpo de <c>POST /chat</c> del agente. El token es el de servicio (rol AGENTE); nunca se registra. HU-23: <c>Historial</c> lleva
/// los últimos 4 turnos y <c>Modo</c> es <c>auto</c> (externo con respaldo), <c>local</c> o <c>externo</c> (solo la evaluación).
/// </summary>
public sealed record PeticionAgente(string Mensaje, int CursoId, string Token, IReadOnlyList<TurnoAgente>? Historial = null, string Modo = "auto", string Tramo = "todo")
{
    // Si alguien registra la petición completa, el token no sale.
    public override string ToString() => $"PeticionAgente {{ CursoId = {CursoId}, Mensaje = {Mensaje.Length} caracteres }}";
}

/// <summary>
/// Respuesta de <c>POST /chat</c> del agente: texto, herramientas usadas con su traza, modelo y duración. HU-23: quién respondió
/// (EXTERNO, LOCAL o NINGUNO), si fue el respaldo y las cifras con su origen.
/// </summary>
public sealed record RespuestaAgente(string Respuesta, IReadOnlyList<TrazaAgente> Herramientas, string Modelo, int DuracionMs)
{
    public string? Proveedor { get; init; }

    public bool Respaldo { get; init; }

    public IReadOnlyList<CifraDto>? Cifras { get; init; }
}

/// <summary>TA-23.A1 · Una cifra de la respuesta: herramienta y campo de donde sale, su estudiante y actividad, y si quedó verificada.</summary>
public sealed record CifraDto(string Valor, string? Herramienta, string? Campo, int? EstudianteId, int? ActividadId, bool Verificada);

/// <summary>Llamada del agente a una herramienta del backend: nombre, parámetros validados, OK o ERROR y duración.</summary>
public sealed record TrazaAgente(string Nombre, JsonElement Parametros, string Estado, int DuracionMs);

/// <summary>El agente no respondió dentro del tiempo máximo, no está levantado o respondió con error.</summary>
public sealed class AgenteNoDisponibleException(string mensaje, Exception? interna = null) : Exception(mensaje, interna);
