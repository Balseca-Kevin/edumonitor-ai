using System.Text.Json;
using System.Text.RegularExpressions;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Cursos;
using EduMonitor.Application.Refuerzos;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Agente;

/// <summary>
/// TA-14.1 · Proxy del chat del asistente. Valida la pregunta y el curso, emite el token de servicio (rol AGENTE, 2 minutos),
/// llama al agente y guarda la conversación con sus trazas. El agente solo se llama si el curso es del docente.
/// </summary>
public sealed class AgenteService(
    IEduMonitorDbContext db,
    CursoService cursos,
    IJwtService jwt,
    IAgenteClient agente,
    TimeProvider reloj,
    EduMonitor.Application.Refuerzos.PlanRefuerzoService planes,
    Seudonimizador seudonimizador,
    ModeloAsistenteService modelo,
    EduMonitor.Application.Periodo.PeriodoService periodo)
{
    /// <summary>HU-23 · Turnos anteriores del hilo que se envían al agente.</summary>
    public const int TurnosDeMemoria = 4;

    private static readonly JsonSerializerOptions JsonCifras = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// HU-23 · Pregunta con <c>conversacionId</c> (sigue el hilo con sus últimos 4 turnos) y <c>foco</c> (refuerzo, actividad o
    /// estudiante de la pantalla). <paramref name="modoForzado"/> (<c>local</c> o <c>externo</c>) solo lo usa la evaluación del asistente.
    /// </summary>
    public Task<ResultadoChat> PreguntarAsync(UsuarioActual usuario, PreguntaChatDto pregunta, CancellationToken ct = default, string? modoForzado = null) =>
        PreguntarAsync(usuario, pregunta.CursoId, pregunta.Mensaje, ct, pregunta.Foco?.RefuerzoId, pregunta.ConversacionId,
            pregunta.Foco?.ActividadId, pregunta.Foco?.EstudianteId, modoForzado);

    public const int LargoMaximoMensaje = 500;

    /// <summary>Pregunta que se guarda cuando el docente envía el plan sin escribir nada.</summary>
    public const string TextoBorrador = "Redacta el borrador del plan de refuerzo.";

    /// <summary>HU-15 · Pregunta que se guarda cuando el docente envía una retroalimentación sin escribir nada.</summary>
    public const string TextoBorradorRetro = "Redacta la retroalimentación de cada estudiante.";

    /// <summary>
    /// Con <paramref name="refuerzoId"/> (TA-15.A4) el plan de refuerzo es el foco: el agente lee el plan con obtener_plan_refuerzo,
    /// redacta el borrador por estudiante y el borrador queda guardado en el plan, en esta conversación.
    /// </summary>
    public async Task<ResultadoChat> PreguntarAsync(
        UsuarioActual usuario, int cursoId, string? mensaje, CancellationToken ct = default, int? refuerzoId = null,
        int? conversacionId = null, int? actividadId = null, int? estudianteId = null, string? modoForzado = null)
    {
        var pregunta = mensaje?.Trim() ?? string.Empty;
        if (pregunta.Length == 0 && refuerzoId is not null)
        {
            pregunta = TextoBorrador;
        }

        if (pregunta.Length == 0)
        {
            return ResultadoChat.Invalido("Escribe una pregunta.");
        }

        if (pregunta.Length > LargoMaximoMensaje)
        {
            return ResultadoChat.Invalido($"La pregunta pasa de {LargoMaximoMensaje} caracteres.");
        }

        var curso = await db.Cursos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cursoId, ct);
        if (curso is null)
        {
            return new ResultadoChat(EstadoChat.CursoNoExiste, Detalle: $"El curso {cursoId} no existe.");
        }

        if (!await cursos.TieneAccesoAsync(usuario, cursoId, ct))
        {
            return new ResultadoChat(EstadoChat.Prohibido, Detalle: $"El curso {cursoId} no está asignado a este docente.");
        }

        // Un código de otro curso en la pregunta («¿Qué estudiantes de BDD-4B…?») se responde aquí, sin llamar al agente:
        // el token de servicio está atado a este curso y el modelo respondería con los datos del curso abierto.
        var otroCurso = await OtroCursoMencionadoAsync(pregunta, curso, ct);
        if (otroCurso is not null)
        {
            var conAcceso = await cursos.TieneAccesoAsync(usuario, otroCurso.Id, ct);
            var aviso = conAcceso
                ? $"Esta conversación es de {curso.NombreCorto}. Abre {otroCurso.NombreCorto} para preguntar sobre ese curso."
                : $"No tienes acceso a {otroCurso.NombreCorto}.";
            return new ResultadoChat(EstadoChat.Respondido, await GuardarAsync(usuario, cursoId, pregunta, aviso, ModeloSinAgente, 0, Array.Empty<TrazaAgente>(), ct));
        }

        // HU-23 · Hilo de la conversación: los últimos 4 turnos van al agente como memoria.
        int? hiloId = null;
        var turnos = new List<Conversacion>();
        if (conversacionId is int cid)
        {
            var anterior = await db.Conversaciones.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cid, ct);
            if (anterior is null || anterior.CursoId != cursoId || anterior.UsuarioId != usuario.Id)
            {
                return ResultadoChat.Invalido($"La conversación {cid} no es tuya en este curso.");
            }

            hiloId = anterior.HiloId ?? anterior.Id;
            var hilo = hiloId.Value;
            turnos = (await db.Conversaciones.AsNoTracking()
                    .Where(c => c.Id == hilo || c.HiloId == hilo)
                    .OrderByDescending(c => c.Id)
                    .Take(TurnosDeMemoria)
                    .ToListAsync(ct))
                .OrderBy(c => c.Id)
                .ToList();
        }

        var mapa = await seudonimizador.MapaAsync(cursoId, ct);
        var foco = new List<string>();
        if (estudianteId is int eid)
        {
            var estudiante = mapa.PorId(eid);
            if (estudiante is null)
            {
                return ResultadoChat.Invalido($"El estudiante {eid} no está matriculado en el curso {cursoId}.");
            }

            foco.Add($"Foco: estudiante {estudiante.Codigo} (estudianteId {eid}).");
        }

        if (actividadId is int aid)
        {
            var actividad = await db.Actividades.AsNoTracking().Where(a => a.Id == aid && a.CursoId == cursoId).Select(a => a.Nombre).SingleOrDefaultAsync(ct);
            if (actividad is null)
            {
                return ResultadoChat.Invalido($"La actividad {aid} no es del curso {cursoId}.");
            }

            foco.Add($"Foco: actividad {actividad} (actividadId {aid}).");
        }

        var lineaPeriodo = await LineaPeriodoAsync(cursoId, ct);

        string? focoPlan = null;
        string? focoEstilo = null;
        if (refuerzoId is int rid)
        {
            var plan = (await planes.ObtenerAsync(cursoId, rid, ct)).Valor;
            if (plan is null)
            {
                return ResultadoChat.Invalido($"El plan de refuerzo {rid} no es del curso {cursoId}.");
            }

            if (plan.Estado == Domain.Entities.PlanRefuerzo.Aprobado)
            {
                return ResultadoChat.Invalido("El plan ya está aprobado: su borrador no cambia.");
            }

            if (pregunta == TextoBorrador && string.IsNullOrWhiteSpace(mensaje) && plan.Tipo == Domain.Entities.PlanRefuerzo.TipoRetroalimentacion)
            {
                pregunta = TextoBorradorRetro;
            }

            focoPlan = plan.Tipo == Domain.Entities.PlanRefuerzo.TipoRetroalimentacion
                ? $"Foco: retroalimentación (refuerzoId {rid}) de {plan.Actividad.Nombre}. "
                    + "Llama a obtener_plan_refuerzo con cursoId y refuerzoId y redacta, por estudiante, una retroalimentación dirigida a él o ella: "
                    + "qué logró y qué le faltó según los niveles de su rúbrica y el comentario del docente, y un paso concreto para mejorar. "
                    + "Tono respetuoso, sin juicios sobre la persona. Usa solo las notas y criterios que devuelve la herramienta."
                : $"Foco: plan de refuerzo (refuerzoId {rid}) de {plan.Actividad.Nombre}, temas {string.Join(", ", plan.Temas.Select(t => t.Nombre))}. "
                    + "Llama a obtener_plan_refuerzo con cursoId y refuerzoId y redacta un borrador por estudiante: qué repasar en cada tema según "
                    + "sus criterios fallados y el comentario del docente, una entrega sugerida antes de la próxima evaluación de esos temas y qué "
                    + "está en juego según su nota necesaria. Usa solo las notas y criterios que devuelve la herramienta.";
            focoEstilo = await EstiloDocenteAsync(usuario.Id, rid, ct);
        }

        var token = jwt.EmitirTokenAgente(usuario.Id, cursoId);
        // El agente recibe el curso, el período y el foco en el texto; cursoId va también en el cuerpo, como pide el contrato v2.
        // HU-23: los nombres de estudiantes salen como códigos (E-001…).
        var contexto = new List<string> { $"Curso: {curso.NombreCorto} (cursoId {curso.Id})" };
        if (lineaPeriodo is not null)
        {
            contexto.Add(lineaPeriodo);
        }

        contexto.AddRange(foco);
        if (focoPlan is not null)
        {
            contexto.Add(focoPlan);
        }

        if (focoEstilo is not null)
        {
            contexto.Add(focoEstilo);
        }

        contexto.Add(Seudonimizador.Ocultar(pregunta, mapa));
        var mensajeAgente = string.Join("\n", contexto);
        var historial = turnos
            .Select(t => new TurnoAgente(Seudonimizador.Ocultar(t.Pregunta, mapa), Seudonimizador.Ocultar(t.Respuesta, mapa)))
            .ToList();
        var modo = modoForzado ?? (await modelo.SoloLocalAsync(ct) ? "local" : "auto");

        var inicio = reloj.GetTimestamp();
        RespuestaAgente respuesta;
        try
        {
            respuesta = await agente.ConsultarAsync(new PeticionAgente(mensajeAgente, cursoId, token.Token, historial, modo), ct);
            respuesta = respuesta with { Respuesta = Seudonimizador.Mostrar(respuesta.Respuesta, mapa) };
        }
        catch (AgenteNoDisponibleException e)
        {
            return new ResultadoChat(EstadoChat.AgenteNoDisponible, Detalle: e.Message);
        }

        var duracion = (int)reloj.GetElapsedTime(inicio).TotalMilliseconds;
        var guardada = await GuardarAsync(usuario, cursoId, pregunta, respuesta.Respuesta, respuesta.Modelo, duracion, respuesta.Herramientas, ct,
            hiloId, respuesta.Proveedor ?? "LOCAL", respuesta.Respaldo, respuesta.Cifras ?? Array.Empty<CifraDto>());
        if (refuerzoId is int planId)
        {
            await planes.GuardarBorradorAsync(planId, guardada.ConversacionId, respuesta.Respuesta, focoEstilo is not null, ct);
            guardada = guardada with { RefuerzoId = planId };
        }

        return new ResultadoChat(EstadoChat.Respondido, guardada);
    }

    /// <summary>
    /// TA-15.B3 · Ejemplos de cómo el docente corrigió dos borradores anteriores, para que el nuevo imite su tono y su estructura. Cada
    /// ejemplo se seudonimiza con el mapa de su propio curso y los códigos se cambian por «[estudiante]», para que el modelo no confunda
    /// a los estudiantes de otro plan con los de este. Sin 2 ejemplos devuelve null y el borrador se redacta como siempre.
    /// </summary>
    private async Task<string?> EstiloDocenteAsync(int usuarioId, int planId, CancellationToken ct)
    {
        var ejemplos = await planes.EjemplosEstiloAsync(usuarioId, planId, ct);
        if (ejemplos.Count == 0)
        {
            return null;
        }

        var partes = new List<string>
        {
            "Estilo del docente: abajo van dos borradores anteriores tal como los redactaste y tal como el docente los aprobó. Imita el tono, "
                + "la extensión y la estructura de la versión aprobada. No copies sus estudiantes, notas, temas ni criterios: los datos de este "
                + "borrador salen solo de obtener_plan_refuerzo.",
        };
        var n = 1;
        foreach (var ejemplo in ejemplos)
        {
            var mapaEjemplo = await seudonimizador.MapaAsync(ejemplo.CursoId, ct);
            partes.Add($"Ejemplo {n} · redactado:\n{SinCodigos(EjemplosEstilo.Recortar(Seudonimizador.Ocultar(ejemplo.Generado, mapaEjemplo)))}");
            partes.Add($"Ejemplo {n} · aprobado por el docente:\n{SinCodigos(EjemplosEstilo.Recortar(Seudonimizador.Ocultar(ejemplo.Aprobado, mapaEjemplo)))}");
            n++;
        }

        return string.Join("\n", partes);
    }

    private static string SinCodigos(string texto) => CodigoEstudiante.Replace(texto, "[estudiante]");

    private static readonly Regex CodigoEstudiante = new(@"\bE-\d{3,}\b", RegexOptions.Compiled);

    /// <summary>
    /// HU-19 · Historial del docente en un curso, de la más reciente a la más antigua. Cada quien ve solo sus conversaciones.
    /// CursoNoExiste si el curso no existe; Prohibido si el curso es ajeno.
    /// </summary>
    public async Task<(EstadoChat Estado, IReadOnlyList<ConversacionResumenDto> Conversaciones)> ListarConversacionesAsync(UsuarioActual usuario, int cursoId, CancellationToken ct = default)
    {
        if (!await cursos.ExisteAsync(cursoId, ct))
        {
            return (EstadoChat.CursoNoExiste, Array.Empty<ConversacionResumenDto>());
        }

        if (!await cursos.TieneAccesoAsync(usuario, cursoId, ct))
        {
            return (EstadoChat.Prohibido, Array.Empty<ConversacionResumenDto>());
        }

        var lista = await db.Conversaciones.AsNoTracking()
            .Where(c => c.CursoId == cursoId && c.UsuarioId == usuario.Id && c.HiloId == null)
            .OrderByDescending(c => c.Id) // el id crece con la fecha; SQLite (pruebas) no ordena por DateTimeOffset
            .Select(c => new ConversacionResumenDto(c.Id, c.Fecha, c.Pregunta, c.Modelo, c.DuracionMs, c.Trazas.Count)
            {
                Turnos = 1 + db.Conversaciones.Count(t => t.HiloId == c.Id),
            })
            .ToListAsync(ct);
        return (EstadoChat.Respondido, lista);
    }

    /// <summary>Una conversación con su respuesta y sus herramientas. 404 si no existe; 403 si el curso es ajeno o la conversación es de otro docente.</summary>
    public async Task<(EstadoChat Estado, ConversacionDto? Conversacion)> ObtenerConversacionAsync(UsuarioActual usuario, int id, CancellationToken ct = default)
    {
        var conversacion = await db.Conversaciones.AsNoTracking().Include(c => c.Trazas).SingleOrDefaultAsync(c => c.Id == id, ct);
        if (conversacion is null)
        {
            return (EstadoChat.CursoNoExiste, null);
        }

        if (!await cursos.TieneAccesoAsync(usuario, conversacion.CursoId, ct) || (conversacion.UsuarioId != usuario.Id && !usuario.EsAdmin))
        {
            return (EstadoChat.Prohibido, null);
        }

        // HU-23 · Todos los turnos del hilo, del primero al último; la conversación pedida es uno de ellos.
        var hilo = conversacion.HiloId ?? conversacion.Id;
        var delHilo = await db.Conversaciones.AsNoTracking().Include(c => c.Trazas)
            .Where(c => c.Id == hilo || c.HiloId == hilo)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);
        var dto = ADto(conversacion) with { HiloId = hilo, Turnos = delHilo.Select(ADto).ToList() };
        return (EstadoChat.Respondido, dto);
    }

    private static ConversacionDto ADto(Conversacion c) =>
        new(c.Id, c.CursoId, c.Fecha, c.Pregunta, c.Respuesta, c.Modelo, c.DuracionMs,
            c.Trazas.OrderBy(t => t.Id).Select(t => new HerramientaUsadaDto(t.Herramienta, ParametrosDeTraza(t.Parametros), t.Estado, t.DuracionMs)).ToList())
        {
            Proveedor = Proveedor(c.Proveedor, c.Modelo),
            Respaldo = c.Respaldo,
            Cifras = LeerCifras(c.Cifras),
        };

    /// <summary>Las conversaciones anteriores a HU-23 no guardaron el proveedor: si hubo modelo, fue el local.</summary>
    private static string Proveedor(string guardado, string modelo) =>
        !string.IsNullOrEmpty(guardado) ? guardado : modelo == ModeloSinAgente ? "NINGUNO" : "LOCAL";

    private static IReadOnlyList<CifraDto> LeerCifras(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<CifraDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<CifraDto>>(json, JsonCifras) ?? new List<CifraDto>();
        }
        catch (JsonException)
        {
            return Array.Empty<CifraDto>();
        }
    }

    /// <summary>HU-23 · Semana y unidad actual del período para el contexto del agente; null si el curso no tiene período calculado.</summary>
    private async Task<string?> LineaPeriodoAsync(int cursoId, CancellationToken ct)
    {
        var consulta = await periodo.ConsultarAsync(cursoId, ct);
        var p = consulta.Periodo;
        if (p?.Semana is null)
        {
            return null;
        }

        var unidad = p.UnidadActual is null ? string.Empty : $", unidad actual U{p.UnidadActual.Numero} · {p.UnidadActual.Nombre}";
        return $"Período: semana {p.Semana.Actual} de {p.Semana.Total}{unidad}.";
    }

    private static JsonElement ParametrosDeTraza(string parametros)
    {
        try
        {
            using var documento = JsonDocument.Parse(string.IsNullOrWhiteSpace(parametros) ? "{}" : parametros);
            return documento.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var vacio = JsonDocument.Parse("{}");
            return vacio.RootElement.Clone();
        }
    }

    /// <summary>Modelo que se guarda cuando la API responde sola, sin llamar al agente.</summary>
    public const string ModeloSinAgente = "ninguno";

    // Códigos de curso como AGIL-4B o BDD-4B: letras, guion, un dígito y una letra. Se comparan con los cursos de la base.
    private static readonly Regex CodigoDeCurso = new(@"\b\p{L}{2,12}-\d\p{L}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Primer curso de la base, distinto del abierto, cuyo código aparece en la pregunta. Null si no hay ninguno.</summary>
    private async Task<Curso?> OtroCursoMencionadoAsync(string pregunta, Curso actual, CancellationToken ct)
    {
        var codigos = CodigoDeCurso.Matches(pregunta).Select(m => m.Value.ToUpperInvariant()).Distinct().ToList();
        if (codigos.Count == 0)
        {
            return null;
        }

        var cursosNombrados = await db.Cursos.AsNoTracking().Where(c => c.Id != actual.Id).ToListAsync(ct);
        return cursosNombrados.FirstOrDefault(c => codigos.Contains(c.NombreCorto.ToUpperInvariant()));
    }

    private async Task<RespuestaChatDto> GuardarAsync(
        UsuarioActual usuario, int cursoId, string pregunta, string respuesta, string modelo, int duracion, IReadOnlyList<TrazaAgente> herramientas, CancellationToken ct,
        int? hiloId = null, string proveedor = "NINGUNO", bool respaldo = false, IReadOnlyList<CifraDto>? cifras = null)
    {
        cifras ??= Array.Empty<CifraDto>();
        var ahora = reloj.GetLocalNow();
        var conversacion = new Conversacion
        {
            UsuarioId = usuario.Id,
            CursoId = cursoId,
            Fecha = ahora,
            Pregunta = pregunta,
            Respuesta = respuesta,
            Modelo = modelo,
            DuracionMs = duracion,
            HiloId = hiloId,
            Proveedor = proveedor,
            Respaldo = respaldo,
            Cifras = cifras.Count == 0 ? null : JsonSerializer.Serialize(cifras, JsonCifras),
            Trazas = herramientas.Select(h => new TrazaHerramienta
            {
                Herramienta = h.Nombre,
                Parametros = h.Parametros.ValueKind == JsonValueKind.Undefined ? "{}" : h.Parametros.GetRawText(),
                Estado = h.Estado,
                Resultado = h.Estado,
                DuracionMs = h.DuracionMs,
                Fecha = ahora,
            }).ToList(),
        };
        db.Conversaciones.Add(conversacion);
        await db.SaveChangesAsync(ct);

        return new RespuestaChatDto(
            conversacion.Id,
            respuesta,
            herramientas.Select(h => new HerramientaUsadaDto(h.Nombre, h.Parametros, h.Estado, h.DuracionMs)).ToList(),
            modelo,
            duracion)
        {
            Fecha = ahora,
            HiloId = hiloId ?? conversacion.Id,
            Proveedor = proveedor,
            Respaldo = respaldo,
            Cifras = cifras,
        };
    }
}

/// <summary>Cuerpo de <c>POST /api/agente/chat</c>. HU-23: <c>ConversacionId</c> sigue un hilo con su memoria.</summary>
public sealed record PreguntaChatDto(int CursoId, string? Mensaje, FocoChatDto? Foco = null, int? ConversacionId = null);

/// <summary>Foco de la pantalla: el plan de refuerzo que el asistente redacta (TA-15.A4), o la actividad y el estudiante abiertos (HU-23).</summary>
public sealed record FocoChatDto(int? RefuerzoId, int? ActividadId = null, int? EstudianteId = null);

/// <summary>Respuesta de <c>POST /api/agente/chat</c> (docs/contratos-api.md, HU-14).</summary>
public sealed record RespuestaChatDto(int ConversacionId, string Respuesta, IReadOnlyList<HerramientaUsadaDto> Herramientas, string Modelo, int DuracionMs)
{
    /// <summary>TA-15.A4 · Plan de refuerzo cuyo borrador se redactó en esta respuesta; null en una pregunta común.</summary>
    public int? RefuerzoId { get; init; }

    public DateTimeOffset? Fecha { get; init; }

    /// <summary>HU-23 · Id de la primera pregunta del hilo, para seguir la conversación.</summary>
    public int? HiloId { get; init; }

    /// <summary>HU-23 · EXTERNO, LOCAL o NINGUNO.</summary>
    public string Proveedor { get; init; } = "NINGUNO";

    /// <summary>HU-23 · El modelo local respondió por una falla del externo.</summary>
    public bool Respaldo { get; init; }

    /// <summary>TA-23.A1 · Cifras de la respuesta con su origen.</summary>
    public IReadOnlyList<CifraDto> Cifras { get; init; } = Array.Empty<CifraDto>();
}

public sealed record HerramientaUsadaDto(string Nombre, JsonElement Parametros, string Estado, int DuracionMs);

/// <summary>Fila de <c>GET /api/agente/conversaciones?cursoId=</c>: <c>herramientas</c> es cuántas se usaron.</summary>
public sealed record ConversacionResumenDto(int Id, DateTimeOffset Fecha, string Pregunta, string Modelo, int DuracionMs, int Herramientas)
{
    /// <summary>HU-23 · Preguntas del hilo, incluida la primera.</summary>
    public int Turnos { get; init; } = 1;
}

/// <summary>Respuesta de <c>GET /api/agente/conversaciones/{id}</c>.</summary>
public sealed record ConversacionDto(
    int Id, int CursoId, DateTimeOffset Fecha, string Pregunta, string Respuesta, string Modelo, int DuracionMs, IReadOnlyList<HerramientaUsadaDto> Herramientas)
{
    public string Proveedor { get; init; } = "LOCAL";

    public bool Respaldo { get; init; }

    public IReadOnlyList<CifraDto> Cifras { get; init; } = Array.Empty<CifraDto>();

    /// <summary>HU-23 · Primera pregunta del hilo.</summary>
    public int? HiloId { get; init; }

    /// <summary>HU-23 · Todos los turnos del hilo, del primero al último. Vacío dentro de cada turno.</summary>
    public IReadOnlyList<ConversacionDto> Turnos { get; init; } = Array.Empty<ConversacionDto>();
}

public enum EstadoChat
{
    Respondido,
    Invalido,
    CursoNoExiste,
    Prohibido,
    AgenteNoDisponible,
}

public sealed record ResultadoChat(EstadoChat Estado, RespuestaChatDto? Respuesta = null, string? Detalle = null)
{
    public static ResultadoChat Invalido(string detalle) => new(EstadoChat.Invalido, Detalle: detalle);
}
