using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Configuracion;
using EduMonitor.Infrastructure.Moodle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MoodleOptions>(configuration.GetSection(MoodleOptions.Seccion));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Seccion));

        // TA-02.2 (Sebastián) reemplaza este registro por el HttpClient tipado: services.AddHttpClient<IMoodleClient, MoodleClient>(...)
        services.AddScoped<IMoodleClient, MoodleClientPendiente>();
        services.AddHostedService<RegistroConfiguracionMoodle>();

        return services;
    }
}
