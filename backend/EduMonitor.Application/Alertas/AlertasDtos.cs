using System.Text.Json;

namespace EduMonitor.Application.Alertas;

/// <summary>Regla de un curso con su umbral y condición (docs/contratos-api.md, HU-12).</summary>
public sealed record ReglaDto(string Codigo, string Nombre, decimal Umbral, bool Activa, string Condicion);

/// <summary>Cambio de una regla en el PUT: { "codigo": "R4", "umbral": 5.0, "activa": true }.</summary>
public sealed record CambioRegla(string Codigo, decimal Umbral, bool Activa);

/// <summary>Resultado del PUT de reglas: 404 si el curso no existe, o 400 con el detalle de la validación.</summary>
public sealed record ResultadoReglas(bool CursoExiste, string? Detalle = null);

/// <summary>Estudiante al que pertenece la alerta.</summary>
public sealed record EstudianteAlertaDto(int Id, string Nombre, string Apellido);

/// <summary>Alerta de GET y PATCH .../alertas (docs/contratos-api.md). El título es siempre «Requiere atención».</summary>
public sealed record AlertaDto(
    int Id,
    string Regla,
    string Titulo,
    EstudianteAlertaDto Estudiante,
    string Estado,
    DateTimeOffset Generada,
    DateTimeOffset? Revisada,
    JsonElement Evidencia);

/// <summary>Cuerpo del PATCH: { "estado": "REVISADA" }.</summary>
public sealed record CambioAlerta(string Estado);

/// <summary>Resultado de GET alertas: el curso no existe, el estado del query no es válido, o la lista (vacía sin datos).</summary>
public sealed record ConsultaAlertas(EstadoConsultaAlertas Estado, IReadOnlyList<AlertaDto> Alertas, string? Detalle = null);

public enum EstadoConsultaAlertas
{
    Ok,
    CursoNoExiste,
    EstadoInvalido,
}

/// <summary>Resultado del PATCH: actualizada, la alerta no es de ese curso (404) o el estado no es REVISADA (400).</summary>
public sealed record ResultadoAlerta(EstadoResultadoAlerta Estado, AlertaDto? Alerta = null, string? Detalle = null);

public enum EstadoResultadoAlerta
{
    Actualizada,
    NoExiste,
    EstadoInvalido,
}
