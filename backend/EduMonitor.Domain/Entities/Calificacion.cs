using EduMonitor.Domain.Enums;

namespace EduMonitor.Domain.Entities;

/// <summary>Nota de un estudiante en una actividad (CALIFICACION en el Diagrama 5). Única por actividad y estudiante.</summary>
public class Calificacion
{
    public int Id { get; set; }

    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    /// <summary>Nota de Moodle. Null si Moodle no tiene nota; 0 es una nota real.</summary>
    public decimal? Nota { get; set; }

    /// <summary>
    /// CALIFICADO cuando hay nota (incluida la 0). Sin nota queda null hasta que TA-05.4 lo derive como SIN_ENTREGA o PENDIENTE
    /// con los estados de entrega; null se muestra como dato no disponible, nunca como 0.
    /// </summary>
    public EstadoCalificacion? Estado { get; set; }

    /// <summary>
    /// Estado de finalización de la actividad en Moodle (core_completion_get_activities_completion_status).
    /// Null si la actividad no tiene seguimiento o si Moodle no concedió el dato: se muestra como no disponible.
    /// </summary>
    public bool? Finalizada { get; set; }
}
