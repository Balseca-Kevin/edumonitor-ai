namespace EduMonitor.Domain.Entities;

/// <summary>
/// Estudiante importado de Moodle (ESTUDIANTE en el Diagrama 5). Solo datos mínimos: id de Moodle, nombre y apellido.
/// Sin correo ni datos de contacto.
/// </summary>
public class Estudiante
{
    public int Id { get; set; }

    /// <summary>Id del usuario en Moodle. Único: la sincronización hace upsert por este valor.</summary>
    public int MoodleUsuarioId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;

    public ICollection<Matricula> Matriculas { get; set; } = new List<Matricula>();
}
