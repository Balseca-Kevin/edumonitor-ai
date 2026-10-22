using Microsoft.AspNetCore.Http;

namespace EduMonitor.Api.Errores;

/// <summary>Formato común de ProblemDetails (docs/contratos-api.md): type https://httpstatuses.io/{código} y título en español.</summary>
public static class TitulosProblemDetails
{
    private static readonly Dictionary<int, string> Titulos = new()
    {
        [StatusCodes.Status400BadRequest] = "Datos de entrada inválidos",
        [StatusCodes.Status401Unauthorized] = "No autenticado",
        [StatusCodes.Status403Forbidden] = "Acceso denegado",
        [StatusCodes.Status404NotFound] = "Recurso no encontrado",
        [StatusCodes.Status500InternalServerError] = "Error interno",
        [StatusCodes.Status502BadGateway] = "Moodle no disponible",
    };

    public static void Aplicar(ProblemDetailsContext contexto)
    {
        var problema = contexto.ProblemDetails;
        var status = problema.Status ?? contexto.HttpContext.Response.StatusCode;
        problema.Status = status;
        problema.Type = $"https://httpstatuses.io/{status}";

        // Un título puesto a propósito por el controlador (p. ej. "Credenciales incorrectas") se respeta.
        if (Titulos.TryGetValue(status, out var titulo) && (string.IsNullOrEmpty(problema.Title) || EsTituloPorDefecto(problema.Title)))
        {
            problema.Title = titulo;
        }

        problema.Extensions.Remove("traceId");
    }

    // Títulos en inglés que pone ASP.NET Core por defecto.
    private static bool EsTituloPorDefecto(string titulo) =>
        titulo is "Bad Request" or "Unauthorized" or "Forbidden" or "Not Found" or "Bad Gateway"
            or "An error occurred while processing your request."
            or "One or more validation errors occurred.";
}
