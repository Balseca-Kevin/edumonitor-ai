namespace EduMonitor.Domain.Entities;

/// <summary>Tarea de refuerzo propuesta por el agente para un tema (TAREA_REFUERZO en el Diagrama 5). El docente la aprueba (HU-15).</summary>
public class TareaRefuerzo
{
    public int Id { get; set; }

    public int TemaId { get; set; }
    public Tema Tema { get; set; } = null!;

    /// <summary>Estudiante destinatario. Null en una tarea grupal.</summary>
    public int? EstudianteId { get; set; }
    public Estudiante? Estudiante { get; set; }

    public string Contenido { get; set; } = string.Empty;

    /// <summary>Estado del borrador, p. ej. <c>BORRADOR</c>, <c>APROBADA</c> o <c>DESCARTADA</c> (HU-15).</summary>
    public string Estado { get; set; } = string.Empty;
}
