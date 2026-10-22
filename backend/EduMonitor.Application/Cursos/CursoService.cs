using EduMonitor.Application.Abstractions;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Cursos;

public sealed class CursoService(IEduMonitorDbContext db, TimeProvider reloj)
{
    public const string HerramientaCursosAutorizados = "listar_cursos_autorizados";

    /// <summary>Cursos asignados al docente; un ADMIN recibe todos. Filtros opcionales por texto y periodo.</summary>
    public async Task<IReadOnlyList<CursoDto>> ListarAsync(UsuarioActual usuario, string? busqueda, string? periodo, CancellationToken ct = default)
    {
        var consulta = CursosVisibles(usuario);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(c => c.Nombre.Contains(texto) || c.NombreCorto.Contains(texto));
        }

        if (!string.IsNullOrWhiteSpace(periodo))
        {
            consulta = consulta.Where(c => c.Periodo == periodo.Trim());
        }

        var cursos = await consulta.OrderBy(c => c.NombreCorto).ToListAsync(ct);
        return cursos.Select(ADto).ToList();
    }

    /// <summary>El curso por id, o null si no existe. La autorización por curso ya la resolvió la política CursoAutorizado.</summary>
    public async Task<CursoDto?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var curso = await db.Cursos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        return curso is null ? null : ADto(curso);
    }

    public Task<bool> ExisteAsync(int cursoId, CancellationToken ct = default) =>
        db.Cursos.AnyAsync(c => c.Id == cursoId, ct);

    /// <summary>True si el usuario puede ver el curso: es ADMIN o el curso está asignado a ese docente.</summary>
    public async Task<bool> TieneAccesoAsync(UsuarioActual usuario, int cursoId, CancellationToken ct = default) =>
        usuario.EsAdmin || await db.CursosDocentes.AnyAsync(cd => cd.UsuarioId == usuario.Id && cd.CursoId == cursoId, ct);

    /// <summary>Datos de la herramienta <c>listar_cursos_autorizados</c> del agente.</summary>
    public async Task<RespuestaCursosAutorizados> CursosAutorizadosAsync(UsuarioActual usuario, CancellationToken ct = default)
    {
        var cursos = await ListarAsync(usuario, null, null, ct);
        return new RespuestaCursosAutorizados(
            HerramientaCursosAutorizados,
            reloj.GetLocalNow(),
            cursos.Select(c => new CursoAutorizadoHerramienta(c.Id, c.NombreCorto, c.Nombre, c.UltimaSincronizacion?.Fecha)).ToList());
    }

    private IQueryable<Curso> CursosVisibles(UsuarioActual usuario) =>
        usuario.EsAdmin
            ? db.Cursos.AsNoTracking()
            : db.Cursos.AsNoTracking().Where(c => c.Docentes.Any(d => d.UsuarioId == usuario.Id));

    // ultimaSincronizacion queda en null hasta que HU-05 agregue la entidad Sincronizacion.
    private static CursoDto ADto(Curso curso) =>
        new(curso.Id, curso.MoodleCursoId, curso.NombreCorto, curso.Nombre, curso.Periodo, null);
}
