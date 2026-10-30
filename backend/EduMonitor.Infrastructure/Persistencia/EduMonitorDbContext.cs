using EduMonitor.Application.Abstractions;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Infrastructure.Persistencia;

public class EduMonitorDbContext(DbContextOptions<EduMonitorDbContext> options) : DbContext(options), IEduMonitorDbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Unidad> Unidades => Set<Unidad>();
    public DbSet<CursoDocente> CursosDocentes => Set<CursoDocente>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<Calificacion> Calificaciones => Set<Calificacion>();
    public DbSet<Sincronizacion> Sincronizaciones => Set<Sincronizacion>();

    // Tablas del Diagrama 5 que se usan desde el Sprint 2 (mapeo temático, alertas, agente y auditoría).
    public DbSet<Tema> Temas => Set<Tema>();
    public DbSet<ContenidoMinimo> ContenidosMinimos => Set<ContenidoMinimo>();
    public DbSet<ActividadTema> ActividadesTemas => Set<ActividadTema>();
    public DbSet<ReglaAlerta> ReglasAlerta => Set<ReglaAlerta>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<TareaRefuerzo> TareasRefuerzo => Set<TareaRefuerzo>();
    public DbSet<Conversacion> Conversaciones => Set<Conversacion>();
    public DbSet<TrazaHerramienta> TrazasHerramienta => Set<TrazaHerramienta>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EduMonitorDbContext).Assembly);
    }
}
