namespace EduMonitor.Domain.Entities;

/// <summary>Estudiante matriculado en un curso (MATRICULA en el Diagrama 5). Única por curso y estudiante.</summary>
public class Matricula
{
    public int Id { get; set; }

    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;
}
