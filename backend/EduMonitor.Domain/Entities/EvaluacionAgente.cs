namespace EduMonitor.Domain.Entities;

/// <summary>HU-18 · TA-18.A1 · Corrida de las 18 preguntas de evaluación del asistente con el modelo externo y con el local.</summary>
public class EvaluacionAgente
{
    public const string EnCurso = "EN_CURSO";
    public const string Terminada = "TERMINADA";
    public const string Fallida = "FALLIDA";

    public int Id { get; set; }

    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    /// <summary>ADMIN que la corrió; las preguntas se hacen con su sesión.</summary>
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    /// <summary><c>EN_CURSO</c>, <c>TERMINADA</c> o <c>FALLIDA</c>.</summary>
    public string Estado { get; set; } = EnCurso;

    public DateTimeOffset Inicio { get; set; }

    public DateTimeOffset? Fin { get; set; }

    /// <summary>Por qué falló la corrida completa (no una pregunta).</summary>
    public string? Detalle { get; set; }

    public List<ResultadoEvaluacion> Resultados { get; set; } = new();
}

/// <summary>Resultado de una pregunta de la evaluación con un modelo.</summary>
public class ResultadoEvaluacion
{
    public int Id { get; set; }

    public int EvaluacionAgenteId { get; set; }
    public EvaluacionAgente Evaluacion { get; set; } = null!;

    /// <summary>Número de la pregunta, de 1 a 18.</summary>
    public int Numero { get; set; }

    /// <summary><c>EXTERNO</c> o <c>LOCAL</c>.</summary>
    public string Modo { get; set; } = string.Empty;

    /// <summary>False si el asistente no respondió (modelo caído, sin configurar o tiempo agotado).</summary>
    public bool Respondio { get; set; }

    public bool Aprobada { get; set; }

    /// <summary>Por qué aprobó o no, en una línea.</summary>
    public string Motivo { get; set; } = string.Empty;

    public string Respuesta { get; set; } = string.Empty;

    /// <summary>Herramientas usadas, separadas por coma, con su estado (<c>obtener_calificaciones:OK</c>).</summary>
    public string Herramientas { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public string Proveedor { get; set; } = string.Empty;

    public int DuracionMs { get; set; }

    public int CifrasVerificadas { get; set; }

    public int CifrasTotal { get; set; }

    public int? ConversacionId { get; set; }
}
