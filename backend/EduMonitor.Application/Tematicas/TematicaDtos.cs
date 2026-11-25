namespace EduMonitor.Application.Tematicas;

// ---------- Mapeo (GET /api/cursos/{id}/mapeo) ----------

public sealed record TemaDto(int Id, string Nombre);

public sealed record UnidadMapeoDto(int Id, int Numero, string Nombre, int Parcial, IReadOnlyList<TemaDto> Temas);

public sealed record CriterioMapeoDto(int Id, int Orden, string Descripcion, decimal PuntajeMaximo, int? TemaId);

public sealed record ActividadMapeoDto(
    int Id,
    string Nombre,
    string? Categoria,
    int? Unidad,
    DateTimeOffset? FechaLimite,
    IReadOnlyList<int> Temas,
    IReadOnlyList<CriterioMapeoDto> Criterios);

public sealed record MapeoDto(int CursoId, IReadOnlyList<UnidadMapeoDto> Unidades, IReadOnlyList<ActividadMapeoDto> Actividades);

public sealed record NuevoTemaDto(string? Nombre);

public sealed record CriterioTemaDto(int Id, int? TemaId);

public sealed record TemasActividadDto(IReadOnlyList<int>? Temas, IReadOnlyList<CriterioTemaDto>? Criterios);

// ---------- Desempeño por tema (GET /api/cursos/{id}/tematicas) ----------

public sealed record UnidadTemaDto(int Numero, string Nombre);

public sealed record EvaluacionTemaDto(int ActividadId, string Actividad, string Fuente, IReadOnlyList<string> Criterios);

public sealed record NotaTemaActividadDto(int ActividadId, decimal Nota);

public sealed record EstudianteTemaDto(int Id, string Nombre, string Apellido, decimal Nota, IReadOnlyList<NotaTemaActividadDto> Notas);

public sealed record EstudianteBajoDto(int Id, string Nombre, string Apellido, decimal Nota);

public sealed record ProximaEvaluacionDto(int ActividadId, string Nombre, DateTimeOffset FechaLimite);

public sealed record TemaDesempenoDto(
    int Id,
    string Nombre,
    UnidadTemaDto Unidad,
    IReadOnlyList<EvaluacionTemaDto> Evaluaciones,
    int EstudiantesConNota,
    decimal? Promedio,
    decimal? Domina,
    IReadOnlyList<EstudianteBajoDto> Bajo6,
    IReadOnlyList<EstudianteTemaDto> Estudiantes,
    IReadOnlyList<ProximaEvaluacionDto> Proximas);

public sealed record ActividadSinTemaDto(int ActividadId, string Nombre);

public sealed record TematicasDto(int CursoId, int? ActividadId, IReadOnlyList<TemaDesempenoDto> Temas, IReadOnlyList<ActividadSinTemaDto> SinTemas);

public enum EstadoConsultaTematica
{
    Correcta,
    CursoNoExiste,
    SinSincronizar,
    NoEncontrado,
    Invalida,
}

/// <summary>Resultado de una operación del mapeo o del desempeño por tema, con el detalle del error para el ProblemDetails.</summary>
public sealed record ResultadoTematica<T>(EstadoConsultaTematica Estado, T? Valor = default, string? Detalle = null);

// ---------- Herramienta obtener_desempeno_tematico (forma corta para el asistente) ----------

public sealed record TemaHerramientaDto(
    int Id,
    string Nombre,
    string Unidad,
    decimal? Domina,
    decimal? Promedio,
    int EstudiantesConNota,
    IReadOnlyList<string> Evaluaciones,
    IReadOnlyList<EstudianteBajoDto> Bajo6,
    IReadOnlyList<string> Proximas);

public sealed record RespuestaDesempenoHerramienta(string Herramienta, DateTimeOffset Generado, int CursoId, int? ActividadId, IReadOnlyList<TemaHerramientaDto> Temas, IReadOnlyList<string> SinTemas)
{
    public const string Nombre = "obtener_desempeno_tematico";

    public static RespuestaDesempenoHerramienta Desde(TematicasDto t, DateTimeOffset generado) => new(
        Nombre,
        generado,
        t.CursoId,
        t.ActividadId,
        t.Temas.Select(x => new TemaHerramientaDto(
            x.Id,
            x.Nombre,
            $"U{x.Unidad.Numero} · {x.Unidad.Nombre}",
            x.Domina,
            x.Promedio,
            x.EstudiantesConNota,
            x.Evaluaciones.Select(e => e.Criterios.Count > 0 ? $"{e.Actividad} (criterio: {string.Join(", ", e.Criterios)})" : e.Actividad).ToList(),
            x.Bajo6,
            x.Proximas.Select(p => $"{p.Nombre} ({p.FechaLimite:yyyy-MM-dd})").ToList())).ToList(),
        t.SinTemas.Select(s => s.Nombre).ToList());
}
