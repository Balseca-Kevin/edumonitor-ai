using EduMonitor.Application.Cursos;
using Microsoft.AspNetCore.Authorization;

namespace EduMonitor.Api.Autorizacion;

/// <summary>
/// Política para todos los endpoints <c>/api/cursos/{id}/*</c> y las herramientas del agente: un docente solo entra a sus cursos;
/// un ADMIN entra a todos. El recurso es el id del curso tomado de la ruta (<c>id</c>) o de la consulta (<c>cursoId</c>).
/// </summary>
public sealed class CursoAutorizadoRequirement : IAuthorizationRequirement
{
    public const string Politica = "CursoAutorizado";
}

public sealed class CursoAutorizadoHandler(CursoService cursos) : AuthorizationHandler<CursoAutorizadoRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CursoAutorizadoRequirement requirement)
    {
        var usuario = context.User.ComoUsuarioActual();
        if (usuario is null || context.Resource is not HttpContext http)
        {
            return;
        }

        var enRuta = http.GetRouteValue("id")?.ToString();
        if (!int.TryParse(enRuta ?? http.Request.Query["cursoId"].FirstOrDefault(), out var cursoId))
        {
            // Una herramienta sin cursoId pasa para que el controlador responda 400; las rutas /api/cursos/{id:int} siempre lo traen.
            if (enRuta is null)
            {
                context.Succeed(requirement);
            }

            return;
        }

        var ct = http.RequestAborted;

        // Un curso que no existe pasa la política para que el controlador responda 404, como pide el contrato.
        if (await cursos.TieneAccesoAsync(usuario, cursoId, ct) || !await cursos.ExisteAsync(cursoId, ct))
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(new AuthorizationFailureReason(this, $"El curso {cursoId} no está asignado a este docente."));
    }
}
