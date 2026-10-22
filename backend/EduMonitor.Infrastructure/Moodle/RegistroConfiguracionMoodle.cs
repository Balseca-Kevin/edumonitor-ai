using EduMonitor.Application.Configuracion;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduMonitor.Infrastructure.Moodle;

/// <summary>Al iniciar, deja en el log a qué Moodle se conecta la API, con el token enmascarado.</summary>
internal sealed class RegistroConfiguracionMoodle(IOptions<MoodleOptions> opciones, ILogger<RegistroConfiguracionMoodle> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var moodle = opciones.Value;
        if (moodle.TokenConfigurado)
        {
            logger.LogInformation("Moodle configurado: {Url}, servicio {Servicio}, token {Token}", moodle.Url, moodle.Servicio, moodle.TokenEnmascarado);
        }
        else
        {
            logger.LogWarning("Moodle__Token no está configurado; las llamadas a Moodle fallarán hasta definirlo en .env");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
