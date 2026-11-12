using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Agente;
using EduMonitor.Infrastructure.Agente;
using EduMonitor.Application.Configuracion;
using EduMonitor.Infrastructure.Moodle;
using System.Text;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Infrastructure.Seguridad;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddSingleton<IHasherContrasenas, HasherContrasenas>();

        services.AddDbContext<EduMonitorDbContext>(opciones =>
            opciones.UseSqlServer(configuration.GetConnectionString("EduMonitorDB")));
        services.AddScoped<IEduMonitorDbContext>(sp => sp.GetRequiredService<EduMonitorDbContext>());

        // HU-20 · La conexión con Moodle se guarda en la base con el token protegido. Las llaves viven en DataProtection__Llaves
        // (en Docker, el volumen api-keys en /keys); sin esa variable se usan las llaves por defecto del usuario.
        var proteccion = services.AddDataProtection().SetApplicationName("EduMonitor");
        var llaves = configuration["DataProtection:Llaves"];
        if (!string.IsNullOrWhiteSpace(llaves))
        {
            proteccion.PersistKeysToFileSystem(new DirectoryInfo(llaves));
        }

        services.AddSingleton<IConfiguracionMoodle, ConfiguracionMoodleEnBase>();
        services.AddHttpClient<IMoodleClient, MoodleClient>((sp, http) =>
            http.Timeout = TimeSpan.FromSeconds(sp.GetRequiredService<IOptions<MoodleOptions>>().Value.TiempoEsperaSegundos));
        services.AddHostedService<RegistroConfiguracionMoodle>();

        // HU-14 · Servicio de agente: solo la API lo llama, por la red interna en Docker (Agente__Url).
        services.Configure<AgenteOptions>(configuration.GetSection(AgenteOptions.Seccion));
        services.AddHttpClient<IAgenteClient, AgenteClient>((sp, http) =>
        {
            var agente = sp.GetRequiredService<IOptions<AgenteOptions>>().Value;
            http.BaseAddress = new Uri(agente.Url.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(agente.TiempoEsperaSegundos);
        });

        return services;
    }
}
