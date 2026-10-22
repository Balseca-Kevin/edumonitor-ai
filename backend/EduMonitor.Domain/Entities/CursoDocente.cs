namespace EduMonitor.Domain.Entities;

/// <summary>Cursos que tiene autorizados cada docente (DOCENTE_CURSO en el Diagrama 5).</summary>
public class CursoDocente
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;
}
