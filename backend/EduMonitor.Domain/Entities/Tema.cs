namespace EduMonitor.Domain.Entities;

/// <summary>Tema de una unidad (TEMA en el Diagrama 5). Lo registra el docente en el mapeo temático (HU-08, Sprint 2).</summary>
public class Tema
{
    public int Id { get; set; }

    public int UnidadId { get; set; }
    public Unidad Unidad { get; set; } = null!;

    public string Nombre { get; set; } = string.Empty;

    public ICollection<ContenidoMinimo> ContenidosMinimos { get; set; } = new List<ContenidoMinimo>();
}
