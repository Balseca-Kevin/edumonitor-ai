using EduMonitor.Application.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Errores;

/// <summary>Traduce las excepciones conocidas a ProblemDetails. Las demás quedan como 500 sin detalles internos.</summary>
public sealed class ManejadorExcepciones(IProblemDetailsService problemDetails, ILogger<ManejadorExcepciones> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, detalle) = exception switch
        {
            MoodleNoDisponibleException e => (StatusCodes.Status502BadGateway, $"Moodle no respondió en {e.Funcion}: {e.Message}"),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado."),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error no controlado en {Ruta}", httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("{Tipo} en {Ruta}: {Mensaje}", exception.GetType().Name, httpContext.Request.Path, exception.Message);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Detail = detalle },
        });
    }
}
