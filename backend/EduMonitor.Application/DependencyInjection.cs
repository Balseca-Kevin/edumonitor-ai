using EduMonitor.Application.Auth;
using EduMonitor.Application.Cursos;
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
        services.AddScoped<ImportadorMatriculados>();
        services.AddScoped<ImportadorCalificaciones>();
        services.AddScoped<SincronizacionService>();
        return services;
    }
}
