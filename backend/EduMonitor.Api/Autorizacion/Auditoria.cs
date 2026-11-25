using System.Security.Claims;
using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Auditorias;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace EduMonitor.Api.Autorizacion;

/// <summary>HU-17 · Marca una acción (o un controlador) para que quede en la auditoría con esta acción.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuditarAttribute(string accion) : Attribute
{
    public string Accion { get; } = accion;
}

/// <summary>
/// HU-17 · Filtro global: después de cada acción marcada con <see cref="AuditarAttribute"/> guarda quién la hizo, el recurso, el
/// curso, el código HTTP y un detalle sin secretos (estado de la sincronización, modelo que respondió, herramienta…). Si la auditoría
/// falla, la petición sigue: se registra en el log.
/// </summary>
public sealed class AuditoriaFilter(AuditoriaService auditoria, ILogger<AuditoriaFilter> logger) : IAsyncActionFilter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var marca = context.ActionDescriptor.EndpointMetadata.OfType<AuditarAttribute>().LastOrDefault();
        var ejecutado = await next();
        if (marca is null)
        {
            return;
        }

        try
        {
            var codigo = ejecutado.Exception is not null && !ejecutado.ExceptionHandled
                ? 500
                : ejecutado.Result switch
                {
                    IStatusCodeActionResult { StatusCode: int c } => c,
                    ObjectResult { Value: ProblemDetails { Status: int s } } => s,
                    _ => 200,
                };
            var valor = (ejecutado.Result as ObjectResult)?.Value;
            var usuarioId = UsuarioId(context.HttpContext.User) ?? UsuarioDeLaRespuesta(valor);
            await auditoria.RegistrarAsync(new RegistroAuditoria(
                usuarioId,
                marca.Accion,
                context.HttpContext.Request.Path.Value ?? string.Empty,
                CursoId(context),
                Detalle(marca.Accion, context, valor),
                codigo.ToString()), context.HttpContext.RequestAborted);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "No se pudo guardar la auditoría de {Accion}", marca.Accion);
        }
    }

    /// <summary>Docente del token; con el token de servicio del agente, el docente que preguntó.</summary>
    public static int? UsuarioId(ClaimsPrincipal usuario)
    {
        var claim = usuario.FindFirstValue("role") == TokenAgente.Rol ? usuario.FindFirstValue(TokenAgente.ClaimDocente) : usuario.FindFirstValue("sub");
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static int? UsuarioDeLaRespuesta(object? valor)
    {
        var json = AJson(valor);
        return json is { } j && j.TryGetProperty("usuario", out var u) && u.ValueKind == JsonValueKind.Object
            && u.TryGetProperty("id", out var id) && id.TryGetInt32(out var n) ? n : null;
    }

    private static int? CursoId(ActionExecutingContext context)
    {
        var http = context.HttpContext;
        if (context.RouteData.Values.TryGetValue("id", out var id) && int.TryParse(id?.ToString(), out var ruta)
            && (http.Request.Path.Value ?? string.Empty).StartsWith("/api/cursos/", StringComparison.OrdinalIgnoreCase))
        {
            return ruta;
        }

        if (int.TryParse(http.Request.Query["cursoId"], out var consulta))
        {
            return consulta;
        }

        return context.ActionArguments.Values.Select(v => AJson(v)).FirstOrDefault(j => j?.TryGetProperty("cursoId", out _) == true) is { } cuerpo
               && cuerpo.GetProperty("cursoId").TryGetInt32(out var c)
            ? c
            : int.TryParse(http.User.FindFirstValue(TokenAgente.ClaimCurso), out var claim) ? claim : null;
    }

    private static string Detalle(string accion, ActionExecutingContext context, object? valor)
    {
        var j = AJson(valor);
        string Texto(string campo) => j is { } e && e.TryGetProperty(campo, out var v) && v.ValueKind != JsonValueKind.Null ? v.ToString() : string.Empty;

        switch (accion)
        {
            case "INICIAR_SESION":
                var solicitud = context.ActionArguments.Values.Select(v => AJson(v)).FirstOrDefault(x => x?.TryGetProperty("usuario", out _) == true);
                return solicitud is { } s ? $"usuario {s.GetProperty("usuario").GetString()}" : string.Empty;
            case "SINCRONIZAR":
                if (j is not { } sync || !sync.TryGetProperty("registros", out var r))
                {
                    return string.Empty;
                }

                var errores = sync.TryGetProperty("errores", out var e2) && e2.ValueKind == JsonValueKind.Array ? e2.GetArrayLength() : 0;
                return $"{Texto("estado")} · {r.GetProperty("estudiantes")} estudiantes, {r.GetProperty("actividades")} actividades, "
                    + $"{r.GetProperty("calificaciones")} calificaciones · {errores} errores";
            case "CONSULTAR_ASISTENTE":
                if (j is not { } chat)
                {
                    return string.Empty;
                }

                var proveedor = Texto("proveedor") switch
                {
                    "EXTERNO" => "modelo externo",
                    "NINGUNO" => "sin modelo",
                    _ => Texto("respaldo") == "True" ? "modelo local (respaldo)" : "modelo local",
                };
                var herramientas = chat.TryGetProperty("herramientas", out var h) && h.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", h.EnumerateArray().Select(x => x.GetProperty("nombre").GetString()))
                    : string.Empty;
                return $"Respondió: {proveedor} · {Texto("modelo")} · {Texto("duracionMs")} ms · herramientas: {(herramientas.Length == 0 ? "ninguna" : herramientas)}";
            case "HERRAMIENTA":
                var nombre = Texto("herramienta");
                var rol = context.HttpContext.User.FindFirstValue("role") == TokenAgente.Rol ? "asistente" : "docente";
                return $"{(nombre.Length > 0 ? nombre : context.ActionDescriptor.RouteValues["action"])} · pedida por el {rol}";
            case "CAMBIAR_MODELO":
                return Texto("soloLocal") == "True" ? "Usar solo el modelo local: activado" : "Usar solo el modelo local: desactivado";
            case "EDITAR_REFUERZO":
            case "CREAR_REFUERZO":
                return j is null ? string.Empty : $"plan {Texto("id")} · estado {Texto("estado")}";
            default:
                return string.Empty;
        }
    }

    private static JsonElement? AJson(object? valor)
    {
        if (valor is null || valor is string || valor.GetType().IsPrimitive || valor is CancellationToken)
        {
            return null;
        }

        try
        {
            var e = JsonSerializer.SerializeToElement(valor, valor.GetType(), Json);
            return e.ValueKind == JsonValueKind.Object ? e : null;
        }
        catch (Exception ex) when (ex is NotSupportedException or JsonException or InvalidOperationException)
        {
            return null;
        }
    }
}
