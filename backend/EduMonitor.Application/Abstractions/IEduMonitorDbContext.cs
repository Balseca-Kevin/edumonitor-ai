using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Abstractions;

/// <summary>Acceso a EduMonitorDB para los servicios de Application. La implementación vive en Infrastructure.</summary>
public interface IEduMonitorDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Curso> Cursos { get; }
    DbSet<Unidad> Unidades { get; }
    DbSet<Tema> Temas { get; }
    DbSet<ActividadTema> ActividadesTemas { get; }
    DbSet<CursoDocente> CursosDocentes { get; }
    DbSet<Estudiante> Estudiantes { get; }
    DbSet<Matricula> Matriculas { get; }
    DbSet<Actividad> Actividades { get; }
    DbSet<Calificacion> Calificaciones { get; }
    DbSet<Domain.Entities.Sincronizacion> Sincronizaciones { get; }
    DbSet<CambioSincronizacion> CambiosSincronizacion { get; }
    DbSet<CriterioRubrica> CriteriosRubrica { get; }
    DbSet<CalificacionCriterio> CalificacionesCriterio { get; }
    DbSet<PlanRefuerzo> PlanesRefuerzo { get; }
    DbSet<TareaRefuerzo> TareasRefuerzo { get; }
    DbSet<ReglaAlerta> ReglasAlerta { get; }
    DbSet<Alerta> Alertas { get; }
    DbSet<Conversacion> Conversaciones { get; }
    DbSet<TrazaHerramienta> TrazasHerramienta { get; }
    DbSet<ConfiguracionAsistente> ConfiguracionesAsistente { get; }
    DbSet<Auditoria> Auditorias { get; }
    DbSet<EvaluacionAgente> EvaluacionesAgente { get; }
    DbSet<ResultadoEvaluacion> ResultadosEvaluacion { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
