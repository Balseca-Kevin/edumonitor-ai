namespace EduMonitor.Domain.Entities;

/// <summary>
/// Alerta generada por una regla para un estudiante (ALERTA en el Diagrama 5). Se muestra como «Requiere atención»,
/// nunca como «reprobará» ni «en riesgo».
/// </summary>
public class Alerta
{
    public int Id { get; set; }

    public int ReglaId { get; set; }
    public ReglaAlerta Regla { get; set; } = null!;

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    /// <summary>Datos que justifican la alerta (notas, promedios, umbral), para mostrarlos al docente.</summary>
    public string Evidencia { get; set; } = string.Empty;

    /// <summary>Estado de revisión, p. ej. <c>ACTIVA</c> o <c>REVISADA</c> (HU-12).</summary>
    public string Estado { get; set; } = string.Empty;
}
