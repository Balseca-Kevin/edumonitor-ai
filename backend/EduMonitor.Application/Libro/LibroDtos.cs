namespace EduMonitor.Application.Libro;

/// <summary>Respuesta de <c>GET /api/cursos/{id}/libro</c> (HU-21, docs/contratos-api.md).</summary>
public sealed record LibroCalificacionesDto(
    int CursoId,
    int Escala,
    decimal Aprobacion,
    SincronizacionLibroDto Sincronizacion,
    IReadOnlyList<ParcialLibroDto> Parciales,
    IReadOnlyList<EstudianteLibroDto> Estudiantes,
    IReadOnlyList<string> NoDisponibles,
    string Tramo = "todo");

/// <summary>Sincronización de la que salen las notas, con el Moodle de origen para compararlas con él.</summary>
public sealed record SincronizacionLibroDto(int Id, DateTimeOffset Fecha, string Estado, string Origen);

public sealed record ParcialLibroDto(int Numero, IReadOnlyList<UnidadLibroDto> Unidades);

public sealed record UnidadLibroDto(int Numero, string Nombre, IReadOnlyList<ActividadLibroDto> Actividades);

public sealed record ActividadLibroDto(int Id, string Nombre, string? Categoria, DateTimeOffset? FechaLimite);

/// <summary>Un estudiante con una nota por cada actividad de <see cref="LibroCalificacionesDto.Parciales"/>, en el mismo orden.</summary>
public sealed record EstudianteLibroDto(
    int Id,
    string Nombre,
    string Apellido,
    decimal? PromedioPonderado,
    bool Parcial,
    IReadOnlyList<NotaLibroDto> Notas);

/// <summary>Nota en escala 0 a 10 (null sin nota) y estado CALIFICADO, SIN_ENTREGA, PENDIENTE o null si no está disponible.</summary>
public sealed record NotaLibroDto(int ActividadId, decimal? Nota, string? Estado);

/// <summary>Resultado de consultar el libro: el curso no existe, nunca se sincronizó con datos, o el libro.</summary>
public sealed record ConsultaLibro(EstadoConsultaLibro Estado, LibroCalificacionesDto? Libro = null, string? Detalle = null);

public enum EstadoConsultaLibro
{
    Armado,
    CursoNoExiste,
    SinSincronizar,
    TramoInvalido,
}

/// <summary>Orden de los estudiantes: por apellido (por defecto) o por promedio, de menor a mayor.</summary>
public enum OrdenLibro
{
    Apellido,
    Promedio,
}
