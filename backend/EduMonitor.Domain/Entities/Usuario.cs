using EduMonitor.Domain.Enums;

namespace EduMonitor.Domain.Entities;

/// <summary>Usuario del sistema (USUARIO en el Diagrama 5). El rol es una columna, no una tabla aparte.</summary>
public class Usuario
{
    public int Id { get; set; }

    /// <summary>Nombre de inicio de sesión (p. ej. <c>doc01</c>). Único.</summary>
    public string NombreUsuario { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string HashContrasena { get; set; } = string.Empty;
    public RolSistema Rol { get; set; }

    public ICollection<CursoDocente> Cursos { get; set; } = new List<CursoDocente>();
}
