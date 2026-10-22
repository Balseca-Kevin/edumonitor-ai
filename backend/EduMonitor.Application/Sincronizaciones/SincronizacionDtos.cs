using EduMonitor.Domain.Entities;

namespace EduMonitor.Application.Sincronizaciones;

/// <summary>Respuesta de <c>POST /api/cursos/{id}/sync</c> y elemento del historial (docs/contratos-api.md).</summary>
public sealed record SincronizacionDto(
    int Id,
    int CursoId,
    string Estado,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    string Origen,
    RegistrosSincronizacion Registros,
    IReadOnlyList<ErrorSincronizacionDto> Errores)
{
    public static SincronizacionDto Desde(Sincronizacion s) => new(
        s.Id,
        s.CursoId,
        s.Estado.ToString(),
        s.Inicio,
        s.Fin,
        s.Origen,
        new RegistrosSincronizacion(s.RegistrosUnidades, s.RegistrosActividades, s.RegistrosEstudiantes, s.RegistrosCalificaciones),
        s.Errores.Select(e => new ErrorSincronizacionDto(e.Funcion, e.Mensaje)).ToList());
}

public sealed record RegistrosSincronizacion(int Unidades, int Actividades, int Estudiantes, int Calificaciones);

public sealed record ErrorSincronizacionDto(string Funcion, string Mensaje);
