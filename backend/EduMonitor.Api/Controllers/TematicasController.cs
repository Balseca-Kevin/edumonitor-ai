using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Tematicas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>
/// HU-08 · Mapeo de actividades y criterios de rúbrica a los temas de cada unidad, y desempeño por tema (HU-22). Contrato en
/// docs/contratos-api.md, «Mapeo temático» y «Desempeño por tema».
/// </summary>
[ApiController]
[Route("api/cursos/{id:int}")]
[Authorize(Policy = CursoAutorizadoRequirement.Politica)]
public sealed class TematicasController(TematicaService tematicas) : ControllerBase
{
    [HttpGet("mapeo")]
    [ProducesResponseType<MapeoDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Mapeo(int id, CancellationToken ct) => Responder(await tematicas.MapeoAsync(id, ct));

    [HttpGet("unidades/{uid:int}/temas")]
    [ProducesResponseType<IReadOnlyList<TemaDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Temas(int id, int uid, CancellationToken ct) => Responder(await tematicas.TemasDeUnidadAsync(id, uid, ct));

    [HttpPost("unidades/{uid:int}/temas")]
    [ProducesResponseType<TemaDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CrearTema(int id, int uid, [FromBody] NuevoTemaDto nuevo, CancellationToken ct)
    {
        var resultado = await tematicas.CrearTemaAsync(id, uid, nuevo, ct);
        return resultado.Estado == EstadoConsultaTematica.Correcta
            ? Created($"/api/cursos/{id}/unidades/{uid}/temas", resultado.Valor)
            : Responder(resultado);
    }

    [HttpPut("actividades/{aid:int}/temas")]
    [ProducesResponseType<ActividadMapeoDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Asociar(int id, int aid, [FromBody] TemasActividadDto pedido, CancellationToken ct) =>
        Responder(await tematicas.AsociarAsync(id, aid, pedido, ct));

    [HttpGet("tematicas")]
    [ProducesResponseType<TematicasDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Desempeno(int id, [FromQuery] int? actividadId, CancellationToken ct) =>
        Responder(await tematicas.DesempenoAsync(id, actividadId, ct));

    private IActionResult Responder<T>(ResultadoTematica<T> resultado) => resultado.Estado switch
    {
        EstadoConsultaTematica.Correcta => Ok(resultado.Valor),
        EstadoConsultaTematica.CursoNoExiste or EstadoConsultaTematica.NoEncontrado => Problem(statusCode: StatusCodes.Status404NotFound, detail: resultado.Detalle),
        EstadoConsultaTematica.SinSincronizar => Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar", detail: resultado.Detalle),
        _ => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Datos inválidos", detail: resultado.Detalle),
    };
}
