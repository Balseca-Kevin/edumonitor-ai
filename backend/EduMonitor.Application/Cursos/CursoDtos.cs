using EduMonitor.Domain.Enums;

namespace EduMonitor.Application.Cursos;

/// <summary>Usuario autenticado que hace la petición (claims <c>sub</c> y <c>role</c> del JWT).</summary>
public sealed record UsuarioActual(int Id, RolSistema Rol)
{
    public bool EsAdmin => Rol == RolSistema.ADMIN;
}

/// <summary>Elemento de <c>GET /api/cursos</c> y respuesta de <c>GET /api/cursos/{id}</c>.</summary>
public sealed record CursoDto(
    int Id,
    int MoodleId,
    string NombreCorto,
    string Nombre,
    string Periodo,
    ResumenSincronizacion? UltimaSincronizacion);

/// <summary>Última sincronización del curso; null si nunca se sincronizó.</summary>
public sealed record ResumenSincronizacion(int Id, DateTimeOffset Fecha, string Estado);
