namespace EduMonitor.Application.Alertas;

/// <summary>Estados de una alerta, tal como los guarda la tabla Alerta (HU-12).</summary>
public static class EstadosAlerta
{
    public const string Activa = "ACTIVA";
    public const string Revisada = "REVISADA";
}

/// <summary>Regla de un curso lista para evaluarse: su definición y el umbral que configuró el docente.</summary>
public sealed record ReglaEvaluada(DefinicionRegla Definicion, decimal Umbral);

/// <summary>Actividad con fecha límite en la que el estudiante no entregó (candidata a atraso para R2).</summary>
public sealed record ActividadSinEntrega(string Nombre, DateTimeOffset? FechaLimite);

/// <summary>Datos de un estudiante ya preparados para evaluar las reglas (TA-12.1).</summary>
/// <param name="EstudianteId">Id del estudiante en EduMonitorDB.</param>
/// <param name="NotasOrdenadas">Notas calificadas en escala 0 a 10, en el orden del curso.</param>
/// <param name="NotasConCategoria">Las mismas notas con su categoría, para el promedio ponderado UTA 40/30/30.</param>
/// <param name="SinEntregas">Actividades del curso en las que el estudiante no entregó, con su fecha límite.</param>
/// <param name="NotasCalificadas">Cantidad de notas calificadas, incluidas las 0.</param>
public sealed record ContextoEstudiante(
    int EstudianteId,
    IReadOnlyList<decimal> NotasOrdenadas,
    IReadOnlyList<(string? Categoria, decimal Nota)> NotasConCategoria,
    IReadOnlyList<ActividadSinEntrega> SinEntregas,
    int NotasCalificadas);

/// <summary>Alerta que una regla activó: la regla, el estudiante y la evidencia ya serializada en JSON camelCase.</summary>
public sealed record AlertaDetectada(string CodigoRegla, int EstudianteId, string EvidenciaJson);

/// <summary>Una regla de alerta: código determinista que evalúa a un estudiante y devuelve la evidencia que lo activó.</summary>
public interface IReglaAlerta
{
    DefinicionRegla Definicion { get; }

    /// <summary>Null si la regla no se activa para ese estudiante con ese umbral.</summary>
    AlertaDetectada? Evaluar(ContextoEstudiante contexto, decimal umbral, DateTimeOffset ahora);
}
