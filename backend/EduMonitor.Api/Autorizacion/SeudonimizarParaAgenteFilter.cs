using System.Text.Json;
using System.Text.Json.Nodes;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Agente;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EduMonitor.Api.Autorizacion;

/// <summary>
/// HU-23 · Las respuestas de <c>/api/tools/*</c> que pide el agente (rol AGENTE) llevan códigos E-001… en lugar de nombres, sin
/// correos ni ids de Moodle. Con el JWT del docente la respuesta no cambia. El curso es el del token de servicio.
/// </summary>
public sealed class SeudonimizarParaAgenteFilter(Seudonimizador seudonimizador) : IAsyncResultFilter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var usuario = context.HttpContext.User;
        if (usuario.IsInRole(TokenAgente.Rol)
            && context.Result is ObjectResult { Value: not null and not ProblemDetails, StatusCode: null or (>= 200 and < 300) } resultado
            && int.TryParse(usuario.FindFirst(TokenAgente.ClaimCurso)?.Value, out var cursoId))
        {
            var mapa = await seudonimizador.MapaAsync(cursoId, context.HttpContext.RequestAborted);
            var nodo = JsonSerializer.SerializeToNode(resultado.Value, resultado.Value.GetType(), Json);
            resultado.Value = Seudonimizador.OcultarJson(nodo, mapa);
            resultado.DeclaredType = typeof(JsonNode);
        }

        await next();
    }
}
