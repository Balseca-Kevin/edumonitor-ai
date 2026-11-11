using EduMonitor.Application.Cursos;

namespace EduMonitor.Application.Indicadores;

/// <summary>Respuesta de <c>GET /api/cursos/{id}/indicadores</c> (docs/contratos-api.md, HU-09).</summary>
public sealed record IndicadoresCursoDto(
    int CursoId,
    int Escala,
    decimal Aprobacion,
    ResumenSincronizacion Sincronizacion,
    IReadOnlyList<UnidadIndicadoresDto> Unidades,
    ResumenIndicadoresCursoDto Curso,
    IReadOnlyList<TramoDistribucionDto> Distribucion,
    GruposRendimientoDto Grupos,
    IReadOnlyList<IndicadoresEstudianteDto> Estudiantes,
    IReadOnlyList<string> NoDisponibles);

/// <summary>Resumen de una unidad: actividades que tiene y cuántas ya tienen al menos una nota calificada.</summary>
public sealed record UnidadIndicadoresDto(int Numero, string Nombre, int Parcial, int Actividades, int Calificadas);

/// <summary>Indicadores del curso. Un valor null se muestra como «no disponible», nunca como 0.</summary>
public sealed record ResumenIndicadoresCursoDto(
    int Estudiantes,
    int ActividadesEvaluadas,
    decimal? Promedio,
    decimal? Mediana,
    decimal? Desviacion,
    decimal? TasaFinalizacion,
    decimal? Tendencia);

/// <summary>Tramo de 1 punto del histograma de promedios ponderados (0–1 … 9–10).</summary>
public sealed record TramoDistribucionDto(int Desde, int Hasta, int Cantidad);

public sealed record GruposRendimientoDto(int Alto, int Medio, int Bajo);

public sealed record IndicadoresEstudianteDto(
    int Id,
    string Nombre,
    string Apellido,
    decimal? PromedioPonderado,
    bool Parcial,
    decimal? Z,
    string? Grupo,
    ConteoNotasDto Notas,
    decimal? TasaFinalizacion,
    decimal? Tendencia);

/// <summary>Notas calificadas (incluidas las 0), cuántas de ellas son 0, actividades sin entrega y entregas pendientes de calificar.</summary>
public sealed record ConteoNotasDto(int Calificadas, int Ceros, int SinEntrega, int Pendientes);

/// <summary>Resultado de consultar los indicadores: el curso no existe, nunca se sincronizó con datos, o los indicadores.</summary>
public sealed record ConsultaIndicadores(EstadoConsultaIndicadores Estado, IndicadoresCursoDto? Indicadores = null);

public enum EstadoConsultaIndicadores
{
    Calculados,
    CursoNoExiste,
    SinSincronizar,
}
