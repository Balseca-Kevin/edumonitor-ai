namespace EduMonitor.Domain.Entities;

/// <summary>Contenido mínimo de un tema (CONTENIDO_MINIMO en el Diagrama 5).</summary>
public class ContenidoMinimo
{
    public int Id { get; set; }

    public int TemaId { get; set; }
    public Tema Tema { get; set; } = null!;

    public string Descripcion { get; set; } = string.Empty;
}
