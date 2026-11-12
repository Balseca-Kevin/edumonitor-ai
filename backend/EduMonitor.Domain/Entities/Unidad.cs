namespace EduMonitor.Domain.Entities;

/// <summary>Unidad del curso: una sección de Moodle (UNIDAD en el Diagrama 5). La sección 0 (General) no es unidad.</summary>
public class Unidad
{
    public int Id { get; set; }

    public int CursoId { get; set; }

    public Curso Curso { get; set; } = null!;

    /// <summary>Id de la sección en Moodle. Único: la sincronización hace upsert por este valor.</summary>
    public int MoodleSeccionId { get; set; }

    /// <summary>Número de la sección en el curso (1 a 4).</summary>
    public int Numero { get; set; }

    /// <summary>Parcial UTA al que pertenece: 1 (unidades 1 y 2) o 2 (unidades 3 y 4).</summary>
    public int Parcial { get; set; }

    public string Nombre { get; set; } = string.Empty;
}
