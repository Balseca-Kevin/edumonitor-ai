using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Abstractions;

/// <summary>Acceso a EduMonitorDB para los servicios de Application. La implementación vive en Infrastructure.</summary>
public interface IEduMonitorDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Curso> Cursos { get; }
    DbSet<CursoDocente> CursosDocentes { get; }
    DbSet<Estudiante> Estudiantes { get; }
    DbSet<Matricula> Matriculas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
