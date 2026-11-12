namespace EduMonitor.Domain.Entities;

/// <summary>
/// Ítem calificable del libro de calificaciones de Moodle (ACTIVIDAD en el Diagrama 5).
/// La unidad (UNIDAD) la asigna TA-05.1; la fecha límite, TA-05.4.
/// </summary>
public class Actividad
{
    public int Id { get; set; }

    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    /// <summary>Unidad (sección de Moodle) donde está la actividad. Null si la actividad no está en una sección, o si la sincronización no pudo leer las secciones.</summary>
    public int? UnidadId { get; set; }
    public Unidad? Unidad { get; set; }

    /// <summary>Id del ítem de calificación en Moodle. Único: la sincronización hace upsert por este valor.</summary>
    public int MoodleItemId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    /// <summary>Categoría del libro de calificaciones (Exámenes, Tareas, APE). Null si el ítem no está en una categoría con nombre.</summary>
    public string? Categoria { get; set; }

    /// <summary>Peso del ítem dentro de su categoría, tal como lo da Moodle.</summary>
    public decimal? Peso { get; set; }

    public decimal? NotaMaxima { get; set; }

    /// <summary>Tipo de módulo de Moodle (assign, quiz...) e id de su instancia; TA-05.4 los usa para enlazar tareas y entregas.</summary>
    public string? Modulo { get; set; }
    public int? Instancia { get; set; }

    /// <summary>Fecha límite de la tarea. La llena TA-05.4 desde mod_assign_get_assignments.</summary>
    public DateTimeOffset? FechaLimite { get; set; }
}
