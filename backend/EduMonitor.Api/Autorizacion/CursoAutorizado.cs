using EduMonitor.Application.Cursos;
using Microsoft.AspNetCore.Authorization;

namespace EduMonitor.Api.Autorizacion;

/// <summary>
/// Política para todos los endpoints <c>/api/cursos/{id}/*</c>: un docente solo entra a sus cursos; un ADMIN entra a todos.
/// El recurso es el id del curso tomado de la ruta.
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

        if (!int.TryParse(http.GetRouteValue("id")?.ToString(), out var cursoId))
        {
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
