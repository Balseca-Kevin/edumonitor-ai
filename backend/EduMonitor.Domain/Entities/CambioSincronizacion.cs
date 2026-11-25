using EduMonitor.Domain.Enums;

namespace EduMonitor.Domain.Entities;

/// <summary>
/// TA-21.A1 · Celda del libro (actividad × estudiante) que cambió en una sincronización frente a lo que había en la base:
/// nota nueva, nota cambiada, entrega nueva u otro cambio de estado. Guarda el estado de antes y el de después.
/// </summary>
public class CambioSincronizacion
{
    public int Id { get; set; }

    public int SincronizacionId { get; set; }
    public Sincronizacion Sincronizacion { get; set; } = null!;

    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    /// <summary>NOTA_NUEVA, NOTA_CAMBIADA, ENTREGA_NUEVA u OTRO (docs/contratos-api.md, «Cambios entre sincronizaciones»).</summary>
    public string Tipo { get; set; } = string.Empty;

    public decimal? NotaAntes { get; set; }
    public EstadoCalificacion? EstadoAntes { get; set; }
    public decimal? NotaAhora { get; set; }
    public EstadoCalificacion? EstadoAhora { get; set; }
}
