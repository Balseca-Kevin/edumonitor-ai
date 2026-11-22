namespace EduMonitor.Application.Periodo;

/// <summary>Respuesta de <c>GET /api/cursos/{id}/periodo</c> (TA-22.A4, docs/contratos-api.md).</summary>
public sealed record PeriodoDto(
    int CursoId,
    DateTimeOffset Hoy,
    DateTimeOffset? Inicio,
    DateTimeOffset? Fin,
    string FinOrigen,
    string? Estado,
    SemanaDto? Semana,
    UnidadActualDto? UnidadActual,
    IReadOnlyList<UnidadPeriodoDto> Unidades,
    IReadOnlyList<ActividadPeriodoDto> Actividades,
    VencidasDto Vencidas,
    IReadOnlyList<VenceEstaSemanaDto> VenceEstaSemana,
    SiguienteDto? Siguiente,
    IReadOnlyList<string> NoDisponibles);

public sealed record SemanaDto(int Actual, int Total);

public sealed record UnidadActualDto(int Numero, string Nombre, int Parcial);

public sealed record UnidadPeriodoDto(int Numero, string Nombre, int Parcial, int? SemanaDesde, int? SemanaHasta);

public sealed record ActividadPeriodoDto(
    int Id,
    string Nombre,
    string Sigla,
    string? Categoria,
    int? Unidad,
    DateTimeOffset? FechaLimite,
    int? Semana,
    string Estado,
    int Matriculados,
    int Entregaron,
    int Faltan);

public sealed record VencidasDto(int Actividades, int EntregasQueFaltan, IReadOnlyList<ActividadConFaltasDto> ConFaltas);

public sealed record ActividadConFaltasDto(int Id, string Nombre, int Faltan);

public sealed record VenceEstaSemanaDto(int Id, string Nombre, DateTimeOffset FechaLimite, int Entregaron, int Matriculados);

public sealed record SiguienteDto(int Id, string Nombre, string? Categoria, int? Semana, DateTimeOffset FechaLimite, IReadOnlyList<string> Temas);

/// <summary>Resultado de consultar el período: el curso no existe, nunca se sincronizó, o el período.</summary>
public sealed record ConsultaPeriodo(EstadoConsultaPeriodo Estado, PeriodoDto? Periodo = null, string? NombreCorto = null);

public enum EstadoConsultaPeriodo
{
    Calculado,
    CursoNoExiste,
    SinSincronizar,
}

/// <summary>Respuesta de <c>GET /api/tools/estado-periodo</c> (herramienta <c>consultar_estado_periodo</c>).</summary>
public sealed record RespuestaEstadoPeriodoHerramienta(
    string Herramienta,
    DateTimeOffset Generado,
    CursoPeriodoHerramientaDto Curso,
    string? Hoy,
    int? Semana,
    int? TotalSemanas,
    string? UnidadActual,
    IReadOnlyList<VencidaHerramientaDto> Vencidas,
    IReadOnlyList<EstaSemanaHerramientaDto> VenceEstaSemana,
    SiguienteHerramientaDto? Siguiente);

public sealed record CursoPeriodoHerramientaDto(int Id, string NombreCorto);

public sealed record VencidaHerramientaDto(string Nombre, int Faltan);

public sealed record EstaSemanaHerramientaDto(string Nombre, string FechaLimite, int Entregaron, int Matriculados);

public sealed record SiguienteHerramientaDto(string Nombre, int? Semana, string FechaLimite, IReadOnlyList<string> Temas);
