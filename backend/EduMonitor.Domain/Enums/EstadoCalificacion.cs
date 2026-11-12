namespace EduMonitor.Domain.Enums;

/// <summary>
/// Estado de una calificación. Una nota 0 calificada es CALIFICADO y es distinta de SIN_ENTREGA.
/// </summary>
public enum EstadoCalificacion
{
    SIN_ENTREGA = 1,
    PENDIENTE = 2,
    CALIFICADO = 3,
}
