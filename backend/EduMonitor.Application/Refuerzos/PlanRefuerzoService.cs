using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Cursos;
using EduMonitor.Application.NotaNecesaria;
using EduMonitor.Application.Tematicas;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Refuerzos;

public sealed record PedidoPlanRefuerzo(int? ActividadId, IReadOnlyList<int>? Temas, IReadOnlyList<int>? Estudiantes, string? Tipo = null);

public sealed record PedidoCambioPlan(string? Texto, string? Estado);

public sealed record ActividadPlanDto(int Id, string Nombre);

public sealed record EstudiantePlanDto(int Id, string Nombre, string Apellido);

/// <summary>TA-15.A1 · Promedio del tema antes de aprobar el refuerzo y ahora. Estado: SUBIO, IGUAL, BAJO o SIN_NOTAS_NUEVAS.</summary>
public sealed record ResultadoRefuerzoDto(int EstudianteId, string Estudiante, int TemaId, string Tema, decimal? Antes, decimal? Ahora, string Estado);

/// <summary>Respuesta de las rutas de <c>/api/cursos/{id}/refuerzos</c> (docs/contratos-api.md, «Plan de refuerzo»).</summary>
public sealed record PlanRefuerzoDto(
    int Id,
    int CursoId,
    DateTimeOffset Creado,
    string Estado,
    ActividadPlanDto Actividad,
    IReadOnlyList<TemaDto> Temas,
    IReadOnlyList<EstudiantePlanDto> Estudiantes,
    int? ConversacionId,
    string? Texto,
    DateTimeOffset? AprobadoEn,
    IReadOnlyList<ResultadoRefuerzoDto> Resultados)
{
    /// <summary>HU-15 · <c>REFUERZO</c> o <c>RETROALIMENTACION</c>.</summary>
    public string Tipo { get; init; } = PlanRefuerzo.TipoRefuerzo;
}

/// <summary>TA-15.A2 · Respuesta de <c>GET /api/cursos/{id}/refuerzos/aplicados</c>: cuántos subieron, quedaron igual, bajaron o no tienen notas nuevas.</summary>
public sealed record RefuerzosAplicadosDto(int CursoId, ConteoAplicadosDto Conteos, IReadOnlyList<RefuerzoAplicadoDto> Filas);

public sealed record ConteoAplicadosDto(int Subio, int Igual, int Bajo, int SinNotasNuevas);

public sealed record RefuerzoAplicadoDto(
    int PlanId, DateTimeOffset AprobadoEn, string Actividad, int EstudianteId, string Estudiante, int TemaId, string Tema, decimal? Antes, decimal? Ahora, string Estado);

/// <summary>TA-15.A2 · Herramienta consultar_refuerzos_aplicados.</summary>
public sealed record RespuestaRefuerzosAplicadosHerramienta(string Herramienta, DateTimeOffset Generado, ConteoAplicadosDto Conteos, IReadOnlyList<RefuerzoAplicadoDto> Filas)
{
    public const string Nombre = "consultar_refuerzos_aplicados";
}

public enum EstadoOperacionPlan
{
    Correcta,
    NoEncontrado,
    SinSincronizar,
    Invalida,
    YaAprobado,
}

public sealed record ResultadoPlan<T>(EstadoOperacionPlan Estado, T? Valor = default, string? Detalle = null);

// ---------- Herramienta obtener_plan_refuerzo (TA-15.A4) ----------

public sealed record CriterioFalladoDto(string Actividad, string Criterio, string Nivel, decimal Puntaje, decimal PuntajeMaximo);

public sealed record EstudianteHerramientaPlanDto(
    int Id,
    string Nombre,
    string Apellido,
    IReadOnlyList<NotaTemaHerramientaDto> NotasPorTema,
    IReadOnlyList<CriterioFalladoDto> CriteriosFallados,
    string? ComentarioDocente,
    string NotaNecesariaEstado,
    decimal? NotaNecesaria,
    decimal? PromedioActual);

public sealed record NotaTemaHerramientaDto(string Tema, decimal? Nota);

public sealed record RespuestaPlanHerramienta(
    string Herramienta,
    DateTimeOffset Generado,
    int RefuerzoId,
    string Actividad,
    IReadOnlyList<string> Temas,
    IReadOnlyList<string> SeVuelvenAEvaluar,
    IReadOnlyList<EstudianteHerramientaPlanDto> Estudiantes)
{
    public const string Nombre = "obtener_plan_refuerzo";

    /// <summary>HU-15 · <c>REFUERZO</c> o <c>RETROALIMENTACION</c>.</summary>
    public string Tipo { get; init; } = PlanRefuerzo.TipoRefuerzo;
}

/// <summary>
/// HU-15 · TA-15.A3 y TA-15.A4 · Plan de refuerzo: lo arma el docente con una actividad, sus temas y sus estudiantes; el asistente
/// redacta el borrador; el docente lo edita y lo aprueba. Al aprobar se guarda la foto de cada tema (TA-15.A1) y una tarea de
/// refuerzo por estudiante y tema (TAREA_REFUERZO, estado APROBADA). Nada se envía al estudiante ni a Moodle.
/// </summary>
public sealed class PlanRefuerzoService(
    IEduMonitorDbContext db,
    TematicaService tematicas,
    NotaNecesariaService notaNecesaria,
    EduMonitor.Application.Documentos.IGeneradorDocumentos documentos,
    TimeProvider reloj)
{
    public const int MaximoEstudiantes = 40;
    public const int LargoMaximoTexto = 20000;
    public const string Subio = "SUBIO";
    public const string Igual = "IGUAL";
    public const string Bajo = "BAJO";
    public const string SinNotasNuevas = "SIN_NOTAS_NUEVAS";

    public async Task<ResultadoPlan<PlanRefuerzoDto>> CrearAsync(UsuarioActual usuario, int cursoId, PedidoPlanRefuerzo pedido, CancellationToken ct = default)
    {
        if (pedido.ActividadId is not int actividadId)
        {
            return new(EstadoOperacionPlan.Invalida, Detalle: "Falta la actividad evaluada.");
        }

        var actividad = await db.Actividades.AsNoTracking().SingleOrDefaultAsync(a => a.Id == actividadId && a.CursoId == cursoId, ct);
        if (actividad is null)
        {
            return new(EstadoOperacionPlan.NoEncontrado, Detalle: $"La actividad {actividadId} no es del curso {cursoId}.");
        }

        var tipo = string.IsNullOrWhiteSpace(pedido.Tipo) ? PlanRefuerzo.TipoRefuerzo : pedido.Tipo.Trim().ToUpperInvariant();
        if (tipo is not (PlanRefuerzo.TipoRefuerzo or PlanRefuerzo.TipoRetroalimentacion))
        {
            return new(EstadoOperacionPlan.Invalida, Detalle: "El tipo es REFUERZO o RETROALIMENTACION.");
        }

        var temas = (pedido.Temas ?? Array.Empty<int>()).Distinct().ToList();
        var estudiantes = (pedido.Estudiantes ?? Array.Empty<int>()).Distinct().ToList();
        // HU-15 · La retroalimentación es de la actividad completa: los temas son opcionales.
        if ((temas.Count == 0 && tipo == PlanRefuerzo.TipoRefuerzo) || estudiantes.Count == 0)
        {
            return new(EstadoOperacionPlan.Invalida, Detalle: tipo == PlanRefuerzo.TipoRefuerzo
                ? "Marca al menos un tema y un estudiante."
                : "Marca al menos un estudiante.");
        }

        if (estudiantes.Count > MaximoEstudiantes)
        {
            return new(EstadoOperacionPlan.Invalida, Detalle: $"Un plan tiene hasta {MaximoEstudiantes} estudiantes.");
        }

        var temasDeActividad = await db.ActividadesTemas.Where(at => at.ActividadId == actividadId).Select(at => at.TemaId).ToListAsync(ct);
        if (temas.FirstOrDefault(t => !temasDeActividad.Contains(t)) is var ajeno && ajeno != 0)
        {
            return new(EstadoOperacionPlan.Invalida, Detalle: $"El tema {ajeno} no está asociado a {actividad.Nombre}.");
        }

        var matriculados = await db.Matriculas.Where(m => m.CursoId == cursoId).Select(m => m.EstudianteId).ToListAsync(ct);
        if (estudiantes.FirstOrDefault(e => !matriculados.Contains(e)) is var fuera && fuera != 0)
        {
            return new(EstadoOperacionPlan.Invalida, Detalle: $"El estudiante {fuera} no está matriculado en el curso.");
        }

        var plan = new PlanRefuerzo
        {
            CursoId = cursoId,
            UsuarioId = usuario.Id,
            ActividadId = actividadId,
            Creado = reloj.GetLocalNow(),
            Estado = PlanRefuerzo.Enviado,
            Tipo = tipo,
            Temas = temas.Select(t => new PlanRefuerzoTema { TemaId = t }).ToList(),
            Estudiantes = estudiantes.Select(e => new PlanRefuerzoEstudiante { EstudianteId = e }).ToList(),
        };
        db.PlanesRefuerzo.Add(plan);
        await db.SaveChangesAsync(ct);
        return new(EstadoOperacionPlan.Correcta, (await ObtenerAsync(cursoId, plan.Id, ct)).Valor);
    }

    public async Task<IReadOnlyList<PlanRefuerzoDto>> ListarAsync(int cursoId, CancellationToken ct = default)
    {
        var ids = await db.PlanesRefuerzo.AsNoTracking().Where(p => p.CursoId == cursoId).OrderByDescending(p => p.Id).Select(p => p.Id).ToListAsync(ct);
        var lista = new List<PlanRefuerzoDto>();
        foreach (var id in ids)
        {
            lista.Add((await ObtenerAsync(cursoId, id, ct)).Valor!);
        }

        return lista;
    }

    public async Task<ResultadoPlan<PlanRefuerzoDto>> ObtenerAsync(int cursoId, int planId, CancellationToken ct = default)
    {
        var plan = await CargarPlanAsync(cursoId, planId, ct);
        if (plan is null)
        {
            return new(EstadoOperacionPlan.NoEncontrado, Detalle: $"El plan {planId} no es del curso {cursoId}.");
        }

        var resultados = plan.Estado == PlanRefuerzo.Aprobado ? await ResultadosAsync(plan, ct) : Array.Empty<ResultadoRefuerzoDto>();
        return new(EstadoOperacionPlan.Correcta, ADto(plan, resultados));
    }

    /// <summary>TA-15.A4 · Guarda el borrador que redactó el asistente en su conversación. No cambia un plan aprobado.</summary>
    public async Task GuardarBorradorAsync(int planId, int conversacionId, string texto, CancellationToken ct = default)
    {
        var plan = await db.PlanesRefuerzo.SingleOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null || plan.Estado == PlanRefuerzo.Aprobado)
        {
            return;
        }

        plan.Texto = texto.Length > LargoMaximoTexto ? texto[..LargoMaximoTexto] : texto;
        plan.ConversacionId = conversacionId;
        plan.Estado = PlanRefuerzo.Borrador;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>TA-15.A4 · El docente edita el texto del borrador y, con estado APROBADO, lo aprueba.</summary>
    public async Task<ResultadoPlan<PlanRefuerzoDto>> CambiarAsync(int cursoId, int planId, PedidoCambioPlan cambio, CancellationToken ct = default)
    {
        var plan = await db.PlanesRefuerzo.Include(p => p.Temas).Include(p => p.Estudiantes).SingleOrDefaultAsync(p => p.Id == planId && p.CursoId == cursoId, ct);
        if (plan is null)
        {
            return new(EstadoOperacionPlan.NoEncontrado, Detalle: $"El plan {planId} no es del curso {cursoId}.");
        }

        if (plan.Estado == PlanRefuerzo.Aprobado)
        {
            return new(EstadoOperacionPlan.YaAprobado, Detalle: "El plan ya está aprobado; no se puede cambiar.");
        }

        if (cambio.Estado is not null && cambio.Estado != PlanRefuerzo.Aprobado)
        {
            return new(EstadoOperacionPlan.Invalida, Detalle: "El único cambio de estado posible es APROBADO.");
        }

        var texto = (cambio.Texto ?? plan.Texto ?? string.Empty).Trim();
        if (cambio.Texto is not null)
        {
            if (texto.Length == 0 || texto.Length > LargoMaximoTexto)
            {
                return new(EstadoOperacionPlan.Invalida, Detalle: $"El borrador tiene que tener texto, hasta {LargoMaximoTexto} caracteres.");
            }

            plan.Texto = texto;
            if (plan.Estado == PlanRefuerzo.Enviado)
            {
                plan.Estado = PlanRefuerzo.Borrador;
            }
        }

        if (cambio.Estado == PlanRefuerzo.Aprobado)
        {
            if (string.IsNullOrWhiteSpace(plan.Texto))
            {
                return new(EstadoOperacionPlan.Invalida, Detalle: "Sin borrador no hay nada que aprobar.");
            }

            await AprobarAsync(plan, ct);
        }

        await db.SaveChangesAsync(ct);
        return await ObtenerAsync(cursoId, planId, ct);
    }

    /// <summary>
    /// TA-15.A4 · Documento del plan aprobado en PDF o DOCX. NoEncontrado si el plan no es del curso; Invalida si el formato no es pdf
    /// ni docx; YaAprobado se usa al revés: el plan todavía no está aprobado.
    /// </summary>
    public async Task<ResultadoPlan<(byte[] Contenido, string Tipo, string Nombre)>> DocumentoAsync(int cursoId, int planId, string? formato, CancellationToken ct = default)
    {
        var tipo = formato?.ToLowerInvariant() switch
        {
            "pdf" => "application/pdf",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => null,
        };
        if (tipo is null)
        {
            return new(EstadoOperacionPlan.Invalida, Detalle: "El formato es pdf o docx.");
        }

        var plan = await CargarPlanAsync(cursoId, planId, ct);
        if (plan is null)
        {
            return new(EstadoOperacionPlan.NoEncontrado, Detalle: $"El plan {planId} no es del curso {cursoId}.");
        }

        if (plan.Estado != PlanRefuerzo.Aprobado || plan.AprobadoEn is null)
        {
            return new(EstadoOperacionPlan.YaAprobado, Detalle: "El documento se descarga cuando el docente aprueba el borrador.");
        }

        var curso = await db.Cursos.AsNoTracking().SingleAsync(c => c.Id == cursoId, ct);
        var retro = plan.Tipo == PlanRefuerzo.TipoRetroalimentacion;
        var documento = new EduMonitor.Application.Documentos.DocumentoRefuerzo(
            $"{curso.NombreCorto} · {curso.Nombre}",
            plan.Actividad.Nombre,
            plan.Temas.Select(t => t.Tema.Nombre).OrderBy(t => t).ToList(),
            plan.Estudiantes.Select(e => $"{e.Estudiante.Nombre} {e.Estudiante.Apellido}").OrderBy(e => e).ToList(),
            plan.Texto ?? string.Empty,
            Periodo.CalculadoraPeriodo.EnEcuador(plan.AprobadoEn.Value))
        {
            Titulo = retro ? "Retroalimentación" : "Plan de refuerzo",
        };
        var contenido = formato!.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? documentos.Pdf(documento) : documentos.Docx(documento);
        return new(EstadoOperacionPlan.Correcta, (contenido, tipo, $"{(retro ? "retroalimentacion" : "plan-refuerzo")}-{curso.NombreCorto}-{plan.Id}.{formato.ToLowerInvariant()}"));
    }

    /// <summary>TA-15.A4 · Datos del plan para que el asistente redacte el borrador por estudiante, con notas reales.</summary>
    public async Task<RespuestaPlanHerramienta?> HerramientaAsync(int cursoId, int planId, CancellationToken ct = default)
    {
        var plan = await CargarPlanAsync(cursoId, planId, ct);
        if (plan is null)
        {
            return null;
        }

        var temasIds = plan.Temas.Select(t => t.TemaId).ToHashSet();
        var desempeno = (await tematicas.DesempenoAsync(cursoId, plan.ActividadId, ct)).Valor;
        var curso = (await tematicas.DesempenoAsync(cursoId, null, ct)).Valor;
        // HU-15 · La retroalimentación mira todos los criterios de la actividad; el refuerzo, solo los de sus temas.
        var retro = plan.Tipo == PlanRefuerzo.TipoRetroalimentacion;
        var criterios = await db.CriteriosRubrica.AsNoTracking()
            .Where(c => c.ActividadId == plan.ActividadId && (retro || (c.TemaId != null && temasIds.Contains(c.TemaId.Value))))
            .ToListAsync(ct);
        var ids = criterios.Select(c => c.Id).ToList();
        var niveles = await db.CalificacionesCriterio.AsNoTracking().Where(n => ids.Contains(n.CriterioRubricaId)).ToListAsync(ct);
        var comentarios = await db.Calificaciones.AsNoTracking().Where(c => c.ActividadId == plan.ActividadId).ToDictionaryAsync(c => c.EstudianteId, c => c.Comentario, ct);

        var estudiantes = new List<EstudianteHerramientaPlanDto>();
        foreach (var e in plan.Estudiantes.Select(x => x.Estudiante).OrderBy(e => e.Apellido))
        {
            var necesaria = (await notaNecesaria.ConsultarAsync(cursoId, e.Id, ct)).Resultado;
            estudiantes.Add(new EstudianteHerramientaPlanDto(
                e.Id,
                e.Nombre,
                e.Apellido,
                plan.Temas.Select(t => new NotaTemaHerramientaDto(
                    t.Tema.Nombre,
                    desempeno?.Temas.FirstOrDefault(x => x.Id == t.TemaId)?.Estudiantes.FirstOrDefault(x => x.Id == e.Id)?.Nota)).ToList(),
                criterios.OrderBy(c => c.Orden)
                    .Select(c => (Criterio: c, Nivel: niveles.FirstOrDefault(n => n.CriterioRubricaId == c.Id && n.EstudianteId == e.Id)))
                    .Where(x => x.Nivel is not null && x.Nivel.Puntaje < x.Criterio.PuntajeMaximo * 0.7m)
                    .Select(x => new CriterioFalladoDto(plan.Actividad.Nombre, x.Criterio.Descripcion, x.Nivel!.Nivel, x.Nivel.Puntaje, x.Criterio.PuntajeMaximo))
                    .ToList(),
                comentarios.GetValueOrDefault(e.Id),
                necesaria?.Estado ?? CalculadoraNotaNecesaria.SinDatos,
                necesaria?.NotaNecesaria,
                necesaria?.PromedioActual));
        }

        var proximas = curso?.Temas.Where(t => temasIds.Contains(t.Id)).SelectMany(t => t.Proximas).DistinctBy(p => p.ActividadId)
            .Select(p => $"{p.Nombre} ({p.FechaLimite:yyyy-MM-dd})").ToList() ?? new List<string>();
        return new RespuestaPlanHerramienta(
            RespuestaPlanHerramienta.Nombre,
            Periodo.CalculadoraPeriodo.EnEcuador(reloj.GetUtcNow()),
            plan.Id,
            plan.Actividad.Nombre,
            plan.Temas.Select(t => t.Tema.Nombre).ToList(),
            proximas,
            estudiantes)
        {
            Tipo = plan.Tipo,
        };
    }

    private async Task AprobarAsync(PlanRefuerzo plan, CancellationToken ct)
    {
        if (plan.Tipo == PlanRefuerzo.TipoRetroalimentacion)
        {
            // La retroalimentación no es un refuerzo por tema: no guarda foto del tema ni tareas de refuerzo.
            plan.Estado = PlanRefuerzo.Aprobado;
            plan.AprobadoEn = reloj.GetLocalNow();
            return;
        }

        var curso = (await tematicas.DesempenoAsync(plan.CursoId, null, ct)).Valor;
        foreach (var estudiante in plan.Estudiantes)
        {
            var foto = plan.Temas.Select(t =>
            {
                var nota = curso?.Temas.FirstOrDefault(x => x.Id == t.TemaId)?.Estudiantes.FirstOrDefault(x => x.Id == estudiante.EstudianteId);
                return new FotoTema(t.TemaId, nota?.Nota, nota?.Notas.Count ?? 0);
            }).ToList();
            estudiante.FotoAlAprobar = JsonSerializer.Serialize(foto);
            foreach (var t in plan.Temas)
            {
                db.TareasRefuerzo.Add(new TareaRefuerzo { TemaId = t.TemaId, EstudianteId = estudiante.EstudianteId, Contenido = plan.Texto!, Estado = "APROBADA" });
            }
        }

        plan.Estado = PlanRefuerzo.Aprobado;
        plan.AprobadoEn = reloj.GetLocalNow();
    }

    private async Task<IReadOnlyList<ResultadoRefuerzoDto>> ResultadosAsync(PlanRefuerzo plan, CancellationToken ct)
    {
        var curso = (await tematicas.DesempenoAsync(plan.CursoId, null, ct)).Valor;
        var resultados = new List<ResultadoRefuerzoDto>();
        foreach (var estudiante in plan.Estudiantes.OrderBy(e => e.Estudiante.Apellido))
        {
            var fotos = string.IsNullOrEmpty(estudiante.FotoAlAprobar) ? new List<FotoTema>() : JsonSerializer.Deserialize<List<FotoTema>>(estudiante.FotoAlAprobar) ?? new();
            foreach (var foto in fotos)
            {
                var tema = plan.Temas.First(t => t.TemaId == foto.TemaId).Tema;
                var ahora = curso?.Temas.FirstOrDefault(x => x.Id == foto.TemaId)?.Estudiantes.FirstOrDefault(x => x.Id == estudiante.EstudianteId);
                var estado = ahora is null || ahora.Notas.Count <= foto.Notas ? SinNotasNuevas
                    : foto.Nota is null ? Subio
                    : ahora.Nota > foto.Nota ? Subio
                    : ahora.Nota < foto.Nota ? Bajo
                    : Igual;
                resultados.Add(new ResultadoRefuerzoDto(
                    estudiante.EstudianteId, $"{estudiante.Estudiante.Nombre} {estudiante.Estudiante.Apellido}", foto.TemaId, tema.Nombre, foto.Nota, ahora?.Nota, estado));
            }
        }

        return resultados;
    }

    private Task<PlanRefuerzo?> CargarPlanAsync(int cursoId, int planId, CancellationToken ct) =>
        db.PlanesRefuerzo.AsNoTracking()
            .Include(p => p.Actividad)
            .Include(p => p.Temas).ThenInclude(t => t.Tema)
            .Include(p => p.Estudiantes).ThenInclude(e => e.Estudiante)
            .SingleOrDefaultAsync(p => p.Id == planId && p.CursoId == cursoId, ct);

    private static PlanRefuerzoDto ADto(PlanRefuerzo p, IReadOnlyList<ResultadoRefuerzoDto> resultados) => new(
        p.Id,
        p.CursoId,
        p.Creado,
        p.Estado,
        new ActividadPlanDto(p.ActividadId, p.Actividad.Nombre),
        p.Temas.Select(t => new TemaDto(t.TemaId, t.Tema.Nombre)).OrderBy(t => t.Nombre).ToList(),
        p.Estudiantes.Select(e => new EstudiantePlanDto(e.EstudianteId, e.Estudiante.Nombre, e.Estudiante.Apellido)).OrderBy(e => e.Apellido).ToList(),
        p.ConversacionId,
        p.Texto,
        p.AprobadoEn,
        resultados)
    {
        Tipo = p.Tipo,
    };

    /// <summary>
    /// TA-15.A2 · Refuerzos aplicados del curso: una fila por estudiante y tema de cada plan de refuerzo aprobado, con la nota del tema
    /// al aprobar y la de hoy, del más reciente al más antiguo, y los conteos por resultado.
    /// </summary>
    public async Task<RefuerzosAplicadosDto> AplicadosAsync(int cursoId, CancellationToken ct = default)
    {
        var ids = await db.PlanesRefuerzo.AsNoTracking()
            .Where(p => p.CursoId == cursoId && p.Estado == PlanRefuerzo.Aprobado && p.Tipo == PlanRefuerzo.TipoRefuerzo)
            .OrderByDescending(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(ct);
        var filas = new List<RefuerzoAplicadoDto>();
        foreach (var id in ids)
        {
            var plan = (await ObtenerAsync(cursoId, id, ct)).Valor!;
            filas.AddRange(plan.Resultados.Select(r => new RefuerzoAplicadoDto(
                plan.Id, plan.AprobadoEn!.Value, plan.Actividad.Nombre, r.EstudianteId, r.Estudiante, r.TemaId, r.Tema, r.Antes, r.Ahora, r.Estado)));
        }

        return new RefuerzosAplicadosDto(cursoId, ContarAplicados(filas), filas);
    }

    public static ConteoAplicadosDto ContarAplicados(IReadOnlyList<RefuerzoAplicadoDto> filas) => new(
        filas.Count(f => f.Estado == Subio),
        filas.Count(f => f.Estado == Igual),
        filas.Count(f => f.Estado == Bajo),
        filas.Count(f => f.Estado == SinNotasNuevas));

    private sealed record FotoTema(int TemaId, decimal? Nota, int Notas);
}
