using EduMonitor.Application.Evaluacion;

namespace EduMonitor.Api.Servicios;

/// <summary>
/// HU-18 · Corre en segundo plano las evaluaciones del asistente, una a la vez, porque 36 consultas pasan del tiempo de una petición.
/// Un error de la corrida marca la evaluación como FALLIDA y no detiene la API.
/// </summary>
public sealed class EvaluacionWorker(ColaEvaluaciones cola, IServiceScopeFactory alcances, ILogger<EvaluacionWorker> logger) : BackgroundService
{
    public static readonly TimeSpan EsperaAntesDeEmpezar = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var id in cola.Lector.ReadAllAsync(stoppingToken))
            {
                cola.EnCurso = id;
                try
                {
                    // Deja terminar la petición que la inició (su auditoría) antes de empezar a escribir resultados.
                    await Task.Delay(EsperaAntesDeEmpezar, stoppingToken);
                    using var alcance = alcances.CreateScope();
                    await alcance.ServiceProvider.GetRequiredService<EvaluacionService>().EjecutarAsync(id, stoppingToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    logger.LogError(e, "La evaluación {Id} del asistente falló", id);
                    try
                    {
                        using var alcance = alcances.CreateScope();
                        await alcance.ServiceProvider.GetRequiredService<EvaluacionService>()
                            .MarcarFallidaAsync(id, $"La evaluación se detuvo: {e.GetType().Name}.", stoppingToken);
                    }
                    catch (Exception interna) when (interna is not OperationCanceledException)
                    {
                        logger.LogError(interna, "No se pudo marcar la evaluación {Id} como fallida", id);
                    }
                }
                finally
                {
                    cola.EnCurso = null;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // La API se está deteniendo.
        }
    }
}
