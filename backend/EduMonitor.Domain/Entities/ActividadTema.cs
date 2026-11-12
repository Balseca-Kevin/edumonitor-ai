namespace EduMonitor.Domain.Entities;

/// <summary>Temas que evalúa una actividad (ACTIVIDAD_TEMA en el Diagrama 5). Clave compuesta por actividad y tema.</summary>
public class ActividadTema
{
    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    public int TemaId { get; set; }
    public Tema Tema { get; set; } = null!;

    /// <summary>Peso del tema dentro de la actividad; el desempeño por tema pondera con este valor.</summary>
    public decimal PesoRelativo { get; set; }
}
