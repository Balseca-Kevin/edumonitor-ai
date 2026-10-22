using EduMonitor.Application.Abstractions;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Infrastructure.Persistencia;

public class EduMonitorDbContext(DbContextOptions<EduMonitorDbContext> options) : DbContext(options), IEduMonitorDbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<CursoDocente> CursosDocentes => Set<CursoDocente>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EduMonitorDbContext).Assembly);
    }
}
