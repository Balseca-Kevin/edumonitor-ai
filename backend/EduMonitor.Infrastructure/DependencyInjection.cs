using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Configuracion;
using EduMonitor.Infrastructure.Moodle;
using System.Text;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MoodleOptions>(configuration.GetSection(MoodleOptions.Seccion));
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Seccion))
            .Validate(o => Encoding.UTF8.GetByteCount(o.Secret) >= 32, "Jwt__Secret debe tener al menos 32 caracteres.")
            .Validate(o => o.ExpiraMinutos > 0, "Jwt__ExpiraMinutos debe ser mayor que 0.")
            .ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IJwtService, JwtService>();

        services.AddDbContext<EduMonitorDbContext>(opciones =>
            opciones.UseSqlServer(configuration.GetConnectionString("EduMonitorDB")));
        services.AddScoped<IEduMonitorDbContext>(sp => sp.GetRequiredService<EduMonitorDbContext>());

        // TA-02.2 (Sebastián) reemplaza este registro por el HttpClient tipado: services.AddHttpClient<IMoodleClient, MoodleClient>(...)
        services.AddScoped<IMoodleClient, MoodleClientPendiente>();
        services.AddHostedService<RegistroConfiguracionMoodle>();

        return services;
    }
}
