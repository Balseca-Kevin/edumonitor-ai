namespace EduMonitor.Domain.Entities;

/// <summary>Regla de alerta configurable por curso (REGLA_ALERTA en el Diagrama 5): R1 a R6 y G1 de la planificación (HU-12, Sprint 2).</summary>
public class ReglaAlerta
{
    public int Id { get; set; }

    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    /// <summary>Código de la regla, p. ej. <c>R1</c> (descenso reciente) o <c>G1</c> (alerta grupal por tema).</summary>
    public string Codigo { get; set; } = string.Empty;

    public decimal Umbral { get; set; }

    public bool Activa { get; set; }
}
