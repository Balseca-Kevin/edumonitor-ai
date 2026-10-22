using EduMonitor.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Application;

public static class DependencyInjection
{
    /// <summary>Servicios de casos de uso. Se llenan con cada historia (AuthService, CursoService, SincronizacionService).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        return services;
    }
}
