using EduMonitor.Api.Autorizacion;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Cursos;
using EduMonitor.Application.Herramientas;
using EduMonitor.Application.Periodo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMonitor.Api.Controllers;

/// <summary>
/// Herramientas que llama el agente de IA, con el JWT del docente o con el token de servicio (rol AGENTE) que emite el chat (TA-14.2).
/// Cada llamada queda registrada (herramienta, docente, fecha, resultado), sin el token.
/// </summary>
[ApiController]
[Route("api/tools")]
[Authorize(Roles = "DOCENTE,ADMIN," + TokenAgente.Rol)]
[TypeFilter(typeof(SeudonimizarParaAgenteFilter))]
[Auditar("HERRAMIENTA")]
public sealed class ToolsController(
    CursoService cursos,
    HerramientasService herramientas,
    PeriodoService periodo,
    EduMonitor.Application.NotaNecesaria.NotaNecesariaService notaNecesaria,
    EduMonitor.Application.Tematicas.TematicaService tematicas,
    EduMonitor.Application.Refuerzos.PlanRefuerzoService planes,
    EduMonitor.Application.Agente.Seudonimizador seudonimizador,
    EduMonitor.Application.Actividades.ActividadService actividades,
    TimeProvider reloj,
    ILogger<ToolsController> logger) : ControllerBase
{
    [HttpGet("cursos-autorizados")]
    [ProducesResponseType<RespuestaCursosAutorizados>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CursosAutorizados(CancellationToken ct)
    {
        var usuario = await User.ResolverUsuarioAsync(cursos, ct);
        if (usuario is null)
        {
            return Unauthorized();
        }

        try
        {
            var respuesta = await cursos.CursosAutorizadosAsync(usuario, ct);
            Registrar(CursoService.HerramientaCursosAutorizados, usuario.Id, null, "OK", respuesta.Cursos.Count);
            return Ok(respuesta);
        }
        catch (Exception)
        {
            Registrar(CursoService.HerramientaCursosAutorizados, usuario.Id, null, "ERROR", 0);
            throw;
        }
    }

    /// <summary>
    /// TA-13.1 · Resumen por estudiante de un curso; detalle por actividad de un estudiante con <c>estudianteId</c> o con <c>nombre</c>
    /// (nombre o apellido, sin tildes ni mayúsculas). Varias coincidencias de nombre devuelven la lista corta para elegir.
    /// HU-23: el resumen acepta <c>promedioMenorA</c> (0 a 10) y <c>orden</c> (<c>promedio</c> o <c>sinEntrega</c>).
    /// 400 sin <c>cursoId</c> o con un filtro fuera de rango; 403 si el curso no es del docente; 404 si el curso no existe o ningún
    /// estudiante coincide.
    /// </summary>
    [HttpGet("calificaciones")]
    [Authorize(Policy = CursoAutorizadoRequirement.PoliticaHerramientas)]
    [ProducesResponseType<RespuestaCalificacionesHerramienta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Calificaciones(
        [FromQuery] int? cursoId, [FromQuery] int? estudianteId, [FromQuery] string? nombre,
        [FromQuery] decimal? promedioMenorA, [FromQuery] string? orden, CancellationToken ct)
    {
        var usuario = await User.ResolverUsuarioAsync(cursos, ct);
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (cursoId is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Falta el parámetro cursoId.");
        }

        // HU-23 · Filtro y orden del resumen hechos en el backend.
        if (promedioMenorA is < 0 or > 10)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "promedioMenorA tiene que estar entre 0 y 10.");
        }

        OrdenCalificaciones ordenResumen;
        switch (orden?.Trim().ToLowerInvariant())
        {
            case null or "" or "promedio":
                ordenResumen = OrdenCalificaciones.Promedio;
                break;
            case "sinentrega":
                ordenResumen = OrdenCalificaciones.SinEntrega;
                break;
            default:
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "El parámetro orden solo acepta «promedio» o «sinEntrega».");
        }

        // HU-23 · El agente nombra a los estudiantes por su código (E-005): se busca por ese código, no por nombre.
        if (nombre is not null && System.Text.RegularExpressions.Regex.IsMatch(nombre.Trim(), @"^E-\d{3}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            var porCodigo = (await seudonimizador.MapaAsync(cursoId.Value, ct)).PorCodigo(nombre);
            if (porCodigo is null)
            {
                Registrar(HerramientasService.HerramientaCalificaciones, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Ningún estudiante del curso {cursoId} tiene el código {nombre.Trim()}.");
            }

            estudianteId = porCodigo.EstudianteId;
            nombre = null;
        }

        try
        {
            var resultado = await herramientas.CalificacionesAsync(
                cursoId.Value, estudianteId, nombre, new FiltroCalificaciones(promedioMenorA, ordenResumen), ct);
            if (resultado is null)
            {
                Registrar(HerramientasService.HerramientaCalificaciones, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {cursoId} no existe.");
            }

            if (resultado.EstudianteNoMatriculado)
            {
                Registrar(HerramientasService.HerramientaCalificaciones, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                var detalle = estudianteId is not null
                    ? $"El estudiante {estudianteId} no está matriculado en el curso {cursoId}."
                    : $"Ningún estudiante del curso {cursoId} se llama «{nombre?.Trim()}».";
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: detalle);
            }

            Registrar(HerramientasService.HerramientaCalificaciones, usuario.Id, cursoId, "OK", resultado.Respuesta!.Estudiantes.Count);
            return Ok(resultado.Respuesta);
        }
        catch (Exception)
        {
            Registrar(HerramientasService.HerramientaCalificaciones, usuario.Id, cursoId, "ERROR", 0);
            throw;
        }
    }

    /// <summary>
    /// TA-13.2 · Alertas activas del curso para la herramienta identificar_alertas. 400 sin <c>cursoId</c>; 403 si el curso no es
    /// del docente; 404 si no existe.
    /// </summary>
    [HttpGet("alertas")]
    [Authorize(Policy = CursoAutorizadoRequirement.PoliticaHerramientas)]
    [ProducesResponseType<RespuestaAlertasHerramienta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Alertas([FromQuery] int? cursoId, CancellationToken ct)
    {
        var usuario = await User.ResolverUsuarioAsync(cursos, ct);
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (cursoId is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Falta el parámetro cursoId.");
        }

        try
        {
            var respuesta = await herramientas.AlertasAsync(cursoId.Value, ct);
            if (respuesta is null)
            {
                Registrar(HerramientasService.HerramientaAlertas, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {cursoId} no existe.");
            }

            Registrar(HerramientasService.HerramientaAlertas, usuario.Id, cursoId, "OK", respuesta.Alertas.Count);
            return Ok(respuesta);
        }
        catch (Exception)
        {
            Registrar(HerramientasService.HerramientaAlertas, usuario.Id, cursoId, "ERROR", 0);
            throw;
        }
    }

    /// <summary>
    /// TA-22.A4 · Estado del período para la herramienta consultar_estado_periodo: semana, unidad actual, vencidas con entregas que
    /// faltan, lo que vence esta semana y la siguiente evaluación. 400 sin <c>cursoId</c>; 403 en un curso ajeno; 404 si no existe.
    /// </summary>
    [HttpGet("estado-periodo")]
    [Authorize(Policy = CursoAutorizadoRequirement.PoliticaHerramientas)]
    [ProducesResponseType<RespuestaEstadoPeriodoHerramienta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EstadoPeriodo([FromQuery] int? cursoId, CancellationToken ct)
    {
        var usuario = await User.ResolverUsuarioAsync(cursos, ct);
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (cursoId is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Falta el parámetro cursoId.");
        }

        try
        {
            var respuesta = await periodo.HerramientaAsync(cursoId.Value, ct);
            if (respuesta is null)
            {
                Registrar(PeriodoService.Herramienta, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El curso {cursoId} no existe.");
            }

            Registrar(PeriodoService.Herramienta, usuario.Id, cursoId, "OK", respuesta.Vencidas.Count + respuesta.VenceEstaSemana.Count);
            return Ok(respuesta);
        }
        catch (Exception)
        {
            Registrar(PeriodoService.Herramienta, usuario.Id, cursoId, "ERROR", 0);
            throw;
        }
    }

    /// <summary>TA-10.A1 · Herramienta calcular_nota_necesaria del asistente. 400 sin cursoId o estudianteId; 404 si no existe o no está matriculado.</summary>
    [HttpGet("nota-necesaria")]
    [Authorize(Policy = CursoAutorizadoRequirement.PoliticaHerramientas)]
    [ProducesResponseType<EduMonitor.Application.NotaNecesaria.RespuestaNotaNecesariaHerramienta>(StatusCodes.Status200OK)]
    public async Task<IActionResult> NotaNecesaria([FromQuery] int? cursoId, [FromQuery] int? estudianteId, CancellationToken ct)
    {
        var usuario = await User.ResolverUsuarioAsync(cursos, ct);
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (cursoId is null || estudianteId is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Faltan los parámetros cursoId y estudianteId.");
        }

        const string herramienta = EduMonitor.Application.NotaNecesaria.NotaNecesariaService.Herramienta;
        var consulta = await notaNecesaria.ConsultarAsync(cursoId.Value, estudianteId.Value, ct);
        switch (consulta.Estado)
        {
            case EduMonitor.Application.NotaNecesaria.EstadoConsultaNotaNecesaria.Calculada:
                Registrar(herramienta, usuario.Id, cursoId, "OK", consulta.Resultado!.Pendientes);
                return Ok(new EduMonitor.Application.NotaNecesaria.RespuestaNotaNecesariaHerramienta(
                    herramienta, EduMonitor.Application.Periodo.CalculadoraPeriodo.EnEcuador(reloj.GetUtcNow()), consulta.Resultado));
            case EduMonitor.Application.NotaNecesaria.EstadoConsultaNotaNecesaria.SinSincronizar:
                Registrar(herramienta, usuario.Id, cursoId, "SIN_SINCRONIZAR", 0);
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar");
            default:
                Registrar(herramienta, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El estudiante {estudianteId} no está en el curso {cursoId}.");
        }
    }

    /// <summary>HU-08 · Herramienta obtener_desempeno_tematico del asistente: desempeño por tema en forma corta.</summary>
    [HttpGet("desempeno-tematico")]
    [Authorize(Policy = CursoAutorizadoRequirement.PoliticaHerramientas)]
    [ProducesResponseType<EduMonitor.Application.Tematicas.RespuestaDesempenoHerramienta>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DesempenoTematico([FromQuery] int? cursoId, [FromQuery] int? actividadId, CancellationToken ct)
    {
        var usuario = await User.ResolverUsuarioAsync(cursos, ct);
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (cursoId is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Falta el parámetro cursoId.");
        }

        const string herramienta = EduMonitor.Application.Tematicas.RespuestaDesempenoHerramienta.Nombre;
        var resultado = await tematicas.DesempenoAsync(cursoId.Value, actividadId, ct);
        switch (resultado.Estado)
        {
            case EduMonitor.Application.Tematicas.EstadoConsultaTematica.Correcta:
                Registrar(herramienta, usuario.Id, cursoId, "OK", resultado.Valor!.Temas.Count);
                return Ok(EduMonitor.Application.Tematicas.RespuestaDesempenoHerramienta.Desde(
                    resultado.Valor, EduMonitor.Application.Periodo.CalculadoraPeriodo.EnEcuador(reloj.GetUtcNow())));
            case EduMonitor.Application.Tematicas.EstadoConsultaTematica.SinSincronizar:
                Registrar(herramienta, usuario.Id, cursoId, "SIN_SINCRONIZAR", 0);
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar");
            default:
                Registrar(herramienta, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: resultado.Detalle);
        }
    }

    /// <summary>TA-15.A4 · Herramienta obtener_plan_refuerzo: el plan con las notas, los criterios fallados y la nota necesaria de cada estudiante.</summary>
    [HttpGet("plan-refuerzo")]
    [Authorize(Policy = CursoAutorizadoRequirement.PoliticaHerramientas)]
    [ProducesResponseType<EduMonitor.Application.Refuerzos.RespuestaPlanHerramienta>(StatusCodes.Status200OK)]
    public async Task<IActionResult> PlanRefuerzo([FromQuery] int? cursoId, [FromQuery] int? refuerzoId, CancellationToken ct)
    {
        var usuario = await User.ResolverUsuarioAsync(cursos, ct);
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (cursoId is null || refuerzoId is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Faltan los parámetros cursoId y refuerzoId.");
        }

        const string herramienta = EduMonitor.Application.Refuerzos.RespuestaPlanHerramienta.Nombre;
        var respuesta = await planes.HerramientaAsync(cursoId.Value, refuerzoId.Value, ct);
        if (respuesta is null)
        {
            Registrar(herramienta, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"El plan {refuerzoId} no es del curso {cursoId}.");
        }

        Registrar(herramienta, usuario.Id, cursoId, "OK", respuesta.Estudiantes.Count);
        return Ok(respuesta);
    }

    /// <summary>HU-22 · Herramienta obtener_detalle_actividad: el panel de actividad en forma corta. 400 sin parámetros; 404 si no es del curso.</summary>
    [HttpGet("actividad")]
    [Authorize(Policy = CursoAutorizadoRequirement.PoliticaHerramientas)]
    [ProducesResponseType<EduMonitor.Application.Actividades.RespuestaActividadHerramienta>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Actividad([FromQuery] int? cursoId, [FromQuery] int? actividadId, CancellationToken ct)
    {
        var usuario = await User.ResolverUsuarioAsync(cursos, ct);
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (cursoId is null || actividadId is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Faltan los parámetros cursoId y actividadId.");
        }

        const string herramienta = EduMonitor.Application.Actividades.ActividadService.Herramienta;
        var (estado, respuesta) = await actividades.HerramientaAsync(cursoId.Value, actividadId.Value, EduMonitor.Application.Periodo.CalculadoraPeriodo.EnEcuador(reloj.GetUtcNow()), ct);
        switch (estado)
        {
            case EduMonitor.Application.Actividades.EstadoConsultaActividad.Encontrada:
                Registrar(herramienta, usuario.Id, cursoId, "OK", respuesta!.RequierenAtencion.Count);
                return Ok(respuesta);
            case EduMonitor.Application.Actividades.EstadoConsultaActividad.SinSincronizar:
                Registrar(herramienta, usuario.Id, cursoId, "SIN_SINCRONIZAR", 0);
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Curso sin sincronizar");
            default:
                Registrar(herramienta, usuario.Id, cursoId, "NO_ENCONTRADO", 0);
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"La actividad {actividadId} no es del curso {cursoId}.");
        }
    }

    /// <summary>TA-15.A2 · Herramienta consultar_refuerzos_aplicados: refuerzos aprobados con la nota del tema antes y después.</summary>
    [HttpGet("refuerzos-aplicados")]
    [Authorize(Policy = CursoAutorizadoRequirement.PoliticaHerramientas)]
    [ProducesResponseType<EduMonitor.Application.Refuerzos.RespuestaRefuerzosAplicadosHerramienta>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RefuerzosAplicados([FromQuery] int? cursoId, CancellationToken ct)
    {
        var usuario = await User.ResolverUsuarioAsync(cursos, ct);
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (cursoId is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Falta el parámetro cursoId.");
        }

        var aplicados = await planes.AplicadosAsync(cursoId.Value, ct);
        Registrar(EduMonitor.Application.Refuerzos.RespuestaRefuerzosAplicadosHerramienta.Nombre, usuario.Id, cursoId, "OK", aplicados.Filas.Count);
        return Ok(new EduMonitor.Application.Refuerzos.RespuestaRefuerzosAplicadosHerramienta(
            EduMonitor.Application.Refuerzos.RespuestaRefuerzosAplicadosHerramienta.Nombre,
            EduMonitor.Application.Periodo.CalculadoraPeriodo.EnEcuador(reloj.GetUtcNow()),
            aplicados.Conteos,
            aplicados.Filas));
    }

    // Traza de cada llamada en el log de la API, sin el token. Las trazas que ve el docente se guardan con el chat (TRAZA_HERRAMIENTA, HU-14).
    private void Registrar(string herramienta, int docenteId, int? cursoId, string resultado, int cantidad) =>
        logger.LogInformation(
            "Herramienta {Herramienta} docente {DocenteId} curso {CursoId} fecha {Fecha:O} resultado {Resultado} registros {Cantidad}",
            herramienta, docenteId, cursoId, reloj.GetLocalNow(), resultado, cantidad);
}
