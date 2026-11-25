namespace EduMonitor.Domain.Entities;

/// <summary>
/// HU-15 · TA-15.A3 · Plan de refuerzo que arma el docente: una actividad evaluada, los temas que marcó y los estudiantes. El asistente
/// redacta el borrador (TA-15.A4); el docente lo edita y lo aprueba. Nada se envía al estudiante ni a Moodle.
/// </summary>
public class PlanRefuerzo
{
    public const string Enviado = "ENVIADO";
    public const string Borrador = "BORRADOR";
    public const string Aprobado = "APROBADO";

    /// <summary>HU-15 · Plan de refuerzo por temas (TA-15.A3).</summary>
    public const string TipoRefuerzo = "REFUERZO";

    /// <summary>HU-15 · Retroalimentación por estudiante de una actividad, con su rúbrica y el comentario del docente.</summary>
    public const string TipoRetroalimentacion = "RETROALIMENTACION";

    /// <summary>HU-15 · <c>REFUERZO</c> o <c>RETROALIMENTACION</c>. Solo el refuerzo guarda la foto del tema y su resultado.</summary>
    public string Tipo { get; set; } = TipoRefuerzo;

    public int Id { get; set; }

    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    /// <summary>Docente que armó el plan.</summary>
    public int UsuarioId { get; set; }

    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    public DateTimeOffset Creado { get; set; }

    /// <summary>ENVIADO (sin borrador todavía), BORRADOR (redactado por el asistente) o APROBADO.</summary>
    public string Estado { get; set; } = Enviado;

    /// <summary>Conversación del asistente donde se redactó el borrador.</summary>
    public int? ConversacionId { get; set; }

    /// <summary>Texto del borrador, editable por el docente hasta aprobarlo.</summary>
    public string? Texto { get; set; }

    public DateTimeOffset? AprobadoEn { get; set; }

    /// <summary>TA-15.B3 · Último texto que redactó el asistente, antes de que el docente lo edite.</summary>
    public string? TextoGenerado { get; set; }

    /// <summary>TA-15.B3 · Texto final que aprobó el docente. Con <see cref="TextoGenerado"/> muestra cómo escribe el docente.</summary>
    public string? TextoAprobado { get; set; }

    /// <summary>TA-15.B3 · El último borrador se redactó con ejemplos de estilo del mismo docente.</summary>
    public bool ConEstilo { get; set; }

    public List<PlanRefuerzoTema> Temas { get; set; } = new();
    public List<PlanRefuerzoEstudiante> Estudiantes { get; set; } = new();
}

/// <summary>Tema marcado en el plan.</summary>
public class PlanRefuerzoTema
{
    public int PlanRefuerzoId { get; set; }
    public PlanRefuerzo PlanRefuerzo { get; set; } = null!;

    public int TemaId { get; set; }
    public Tema Tema { get; set; } = null!;
}

/// <summary>
/// Estudiante del plan con la foto de cada tema al aprobar (TA-15.A1): la nota del tema y cuántas notas tenía en él, para comparar
/// después de la siguiente sincronización.
/// </summary>
public class PlanRefuerzoEstudiante
{
    public int PlanRefuerzoId { get; set; }
    public PlanRefuerzo PlanRefuerzo { get; set; } = null!;

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    /// <summary>JSON con la foto por tema al aprobar: [{ "temaId": 30, "nota": 4.0, "notas": 3 }]. Null antes de aprobar.</summary>
    public string? FotoAlAprobar { get; set; }
}
