namespace EduMonitor.Domain.Entities;

/// <summary>Llamada del agente a una herramienta del backend (TRAZA_HERRAMIENTA en el Diagrama 5). Nunca guarda tokens.</summary>
public class TrazaHerramienta
{
    public int Id { get; set; }

    public int ConversacionId { get; set; }
    public Conversacion Conversacion { get; set; } = null!;

    public string Herramienta { get; set; } = string.Empty;

    /// <summary>Parámetros de la llamada en JSON.</summary>
    public string Parametros { get; set; } = string.Empty;

    /// <summary>Resultado de la llamada (OK, ERROR y resumen).</summary>
    public string Resultado { get; set; } = string.Empty;
}
