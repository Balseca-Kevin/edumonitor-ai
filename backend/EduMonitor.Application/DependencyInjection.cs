using EduMonitor.Application.Agente;
using EduMonitor.Application.Alertas;
using EduMonitor.Application.Auth;
using EduMonitor.Application.Configuracion;
using EduMonitor.Application.Cursos;
using EduMonitor.Application.Estado;
using EduMonitor.Application.Herramientas;
using EduMonitor.Application.Indicadores;
using EduMonitor.Application.Libro;
using EduMonitor.Application.Periodo;
using EduMonitor.Application.Sincronizaciones;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Application;

public static class DependencyInjection
{
    /// <summary>Servicios de casos de uso. Se llenan con cada historia (AuthService, CursoService, SincronizacionService).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<CursoService>();
        services.AddScoped<IndicadorService>();
        services.AddScoped<LibroService>();
        services.AddScoped<PeriodoService>();
        services.AddScoped<EduMonitor.Application.Refuerzos.PlanRefuerzoService>();
        services.AddScoped<EduMonitor.Application.NotaNecesaria.NotaNecesariaService>();
        services.AddScoped<EduMonitor.Application.Tematicas.TematicaService>();
        services.AddScoped<AlertasService>();
        services.AddScoped<HerramientasService>();
        services.AddScoped<AgenteService>();
        services.AddScoped<EstadoSistemaService>();
        services.AddScoped<ImportadorUnidades>();
        services.AddScoped<ImportadorMatriculados>();
        services.AddScoped<ImportadorCalificaciones>();
        services.AddScoped<ImportadorEntregas>();
        services.AddScoped<SincronizacionService>();
        services.AddScoped<ImportadorRubricas>();
        services.AddScoped<CambiosSincronizacionService>();
        services.AddScoped<ConfiguracionMoodleService>();
        return services;
    }
}
