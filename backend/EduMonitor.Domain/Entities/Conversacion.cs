namespace EduMonitor.Domain.Entities;

/// <summary>Conversación de un docente con el agente sobre un curso (CONVERSACION en el Diagrama 5).</summary>
public class Conversacion
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    public DateTimeOffset Fecha { get; set; }

    /// <summary>Pregunta del docente (hasta 500 caracteres).</summary>
    public string Pregunta { get; set; } = string.Empty;

    /// <summary>Respuesta del agente tal como la vio el docente.</summary>
    public string Respuesta { get; set; } = string.Empty;

    /// <summary>Modelo de lenguaje que respondió, p. ej. <c>qwen2.5:7b</c>.</summary>
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Tiempo total de la consulta medido en la API, en milisegundos.</summary>
    public int DuracionMs { get; set; }

    public ICollection<TrazaHerramienta> Trazas { get; set; } = new List<TrazaHerramienta>();
}
