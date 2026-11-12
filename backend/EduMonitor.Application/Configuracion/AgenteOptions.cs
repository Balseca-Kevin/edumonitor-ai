namespace EduMonitor.Application.Configuracion;

/// <summary>
/// Servicio de agente. Sección <c>Agente</c> (variables <c>Agente__Url</c> y <c>Agente__TiempoEsperaSegundos</c>).
/// En local, <c>http://localhost:8000</c>; en Docker, <c>http://agent:8000</c> por la red interna (el agente no publica puerto).
/// </summary>
public sealed class AgenteOptions
{
    public const string Seccion = "Agente";

    public string Url { get; set; } = "http://localhost:8000";

    /// <summary>Tiempo máximo de espera del chat; pasado ese tiempo la API responde 503 «Asistente no disponible».</summary>
    public int TiempoEsperaSegundos { get; set; } = 60;
}
