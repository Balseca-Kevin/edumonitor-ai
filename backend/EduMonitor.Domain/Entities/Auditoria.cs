namespace EduMonitor.Domain.Entities;

/// <summary>Registro de auditoría de acciones de los usuarios (AUDITORIA en el Diagrama 5, HU-17). Sin secretos.</summary>
public class Auditoria
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    /// <summary>Acción realizada, p. ej. <c>SINCRONIZAR</c> o <c>ACCESO_DENEGADO</c>.</summary>
    public string Accion { get; set; } = string.Empty;

    /// <summary>Entidad afectada, p. ej. <c>Curso 7</c>.</summary>
    public string Entidad { get; set; } = string.Empty;

    public DateTimeOffset Fecha { get; set; }
}
