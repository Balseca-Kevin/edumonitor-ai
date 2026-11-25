namespace EduMonitor.Domain.Entities;

/// <summary>
/// Registro de auditoría de acciones de los usuarios (AUDITORIA en el Diagrama 5, HU-17). Sin secretos: nunca guarda contraseñas,
/// tokens ni la clave del modelo externo.
/// </summary>
public class Auditoria
{
    public int Id { get; set; }

    /// <summary>Usuario que hizo la acción. Null en un inicio de sesión fallido o una petición sin token.</summary>
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    /// <summary>Acción realizada, p. ej. <c>SINCRONIZAR</c>, <c>CONSULTAR_ASISTENTE</c> o <c>ACCESO_DENEGADO</c>.</summary>
    public string Accion { get; set; } = string.Empty;

    /// <summary>Recurso afectado: la ruta de la API sin la consulta, p. ej. <c>/api/cursos/7/sync</c>.</summary>
    public string Entidad { get; set; } = string.Empty;

    /// <summary>HU-17 · Curso de la acción, si la tiene.</summary>
    public int? CursoId { get; set; }

    /// <summary>HU-17 · Detalle en una línea (estado de la sincronización, modelo que respondió, herramienta…), sin secretos.</summary>
    public string Detalle { get; set; } = string.Empty;

    /// <summary>HU-17 · Código HTTP de la respuesta.</summary>
    public string Resultado { get; set; } = string.Empty;

    public DateTimeOffset Fecha { get; set; }
}
