using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Autorizacion;

/// <summary>Responde 401 y 403 con ProblemDetails; en 403 incluye el motivo que dio la política (p. ej. el curso ajeno).</summary>
public sealed class RespuestaAutorizacionProblemDetails(IProblemDetailsService problemDetails) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _porDefecto = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await Escribir(context, StatusCodes.Status401Unauthorized, "Falta el token, venció o no es válido.");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            var motivo = authorizeResult.AuthorizationFailure?.FailureReasons.Select(r => r.Message).FirstOrDefault()
                ?? "No tienes permiso para este recurso.";
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await Escribir(context, StatusCodes.Status403Forbidden, motivo);
            return;
        }

        await _porDefecto.HandleAsync(next, context, policy, authorizeResult);
    }

    private Task Escribir(HttpContext context, int status, string detalle) =>
        problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Detail = detalle },
        }).AsTask();
}
