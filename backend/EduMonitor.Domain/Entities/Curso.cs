namespace EduMonitor.Domain.Entities;

/// <summary>Curso importado de Moodle (CURSO en el Diagrama 5).</summary>
public class Curso
{
    public int Id { get; set; }

    /// <summary>Id del curso en Moodle. Único: la sincronización hace upsert por este valor.</summary>
    public int MoodleCursoId { get; set; }

    public string NombreCorto { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Periodo académico, p. ej. <c>2026-2</c>.</summary>
    public string Periodo { get; set; } = string.Empty;

    /// <summary>TA-22.A4 · Inicio del curso en Moodle (<c>startdate</c>). Null si la sincronización todavía no lo leyó.</summary>
    public DateTimeOffset? Inicio { get; set; }

    /// <summary>TA-22.A4 · Fin del curso en Moodle (<c>enddate</c>). Null si Moodle lo tiene en 0: el período termina con la última fecha límite.</summary>
    public DateTimeOffset? Fin { get; set; }

    public ICollection<CursoDocente> Docentes { get; set; } = new List<CursoDocente>();
}
