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

    public ICollection<TrazaHerramienta> Trazas { get; set; } = new List<TrazaHerramienta>();
}
