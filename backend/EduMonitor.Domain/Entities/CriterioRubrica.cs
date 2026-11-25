namespace EduMonitor.Domain.Entities;

/// <summary>
/// HU-08 · Criterio de la rúbrica de una tarea, tal como está en Moodle (core_grading_get_definitions). El docente lo asocia a un
/// tema en EduMonitor; la sincronización actualiza el texto y el puntaje sin tocar ese tema.
/// </summary>
public class CriterioRubrica
{
    public int Id { get; set; }

    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    /// <summary>Id del criterio en Moodle. Único por actividad: la sincronización hace upsert por este valor.</summary>
    public int MoodleCriterioId { get; set; }

    public int Orden { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Puntaje del nivel más alto del criterio.</summary>
    public decimal PuntajeMaximo { get; set; }

    /// <summary>Tema que evalúa el criterio según el docente (HU-08). Null si todavía no se asoció.</summary>
    public int? TemaId { get; set; }
    public Tema? Tema { get; set; }
}

/// <summary>HU-08 · Nivel que obtuvo un estudiante en un criterio de rúbrica (core_grading_get_gradingform_instances).</summary>
public class CalificacionCriterio
{
    public int Id { get; set; }

    public int CriterioRubricaId { get; set; }
    public CriterioRubrica CriterioRubrica { get; set; } = null!;

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    public decimal Puntaje { get; set; }

    /// <summary>Nombre del nivel en la rúbrica (Insuficiente, En proceso, Logrado, Destacado).</summary>
    public string Nivel { get; set; } = string.Empty;
}
