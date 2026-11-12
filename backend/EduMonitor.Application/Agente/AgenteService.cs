using System.Text.Json;
using System.Text.RegularExpressions;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Cursos;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Agente;

/// <summary>
/// TA-14.1 · Proxy del chat del asistente. Valida la pregunta y el curso, emite el token de servicio (rol AGENTE, 2 minutos),
/// llama al agente y guarda la conversación con sus trazas. El agente solo se llama si el curso es del docente.
/// </summary>
public sealed class AgenteService(IEduMonitorDbContext db, CursoService cursos, IJwtService jwt, IAgenteClient agente, TimeProvider reloj)
{
    public const int LargoMaximoMensaje = 500;

    public async Task<ResultadoChat> PreguntarAsync(UsuarioActual usuario, int cursoId, string? mensaje, CancellationToken ct = default)
    {
        var pregunta = mensaje?.Trim() ?? string.Empty;
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

        var token = jwt.EmitirTokenAgente(usuario.Id, cursoId);
        // El agente de develop recibe el curso en el texto; cursoId va también en el cuerpo, como pide el contrato v2.
        var mensajeAgente = $"Curso: {curso.NombreCorto} (cursoId {curso.Id})\n{pregunta}";

        var inicio = reloj.GetTimestamp();
        RespuestaAgente respuesta;
        try
        {
            respuesta = await agente.ConsultarAsync(new PeticionAgente(mensajeAgente, cursoId, token.Token), ct);
        }
        catch (AgenteNoDisponibleException e)
        {
            return new ResultadoChat(EstadoChat.AgenteNoDisponible, Detalle: e.Message);
        }

        var duracion = (int)reloj.GetElapsedTime(inicio).TotalMilliseconds;
        return new ResultadoChat(EstadoChat.Respondido, await GuardarAsync(usuario, cursoId, pregunta, respuesta.Respuesta, respuesta.Modelo, duracion, respuesta.Herramientas, ct));
    }

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
            .Where(c => c.CursoId == cursoId && c.UsuarioId == usuario.Id)
            .OrderByDescending(c => c.Id) // el id crece con la fecha; SQLite (pruebas) no ordena por DateTimeOffset
            .Select(c => new ConversacionResumenDto(c.Id, c.Fecha, c.Pregunta, c.Modelo, c.DuracionMs, c.Trazas.Count))
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

        var herramientas = conversacion.Trazas
            .OrderBy(t => t.Id)
            .Select(t => new HerramientaUsadaDto(t.Herramienta, ParametrosDeTraza(t.Parametros), t.Estado, t.DuracionMs))
            .ToList();
        return (EstadoChat.Respondido, new ConversacionDto(
            conversacion.Id, conversacion.CursoId, conversacion.Fecha, conversacion.Pregunta, conversacion.Respuesta,
            conversacion.Modelo, conversacion.DuracionMs, herramientas));
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
        UsuarioActual usuario, int cursoId, string pregunta, string respuesta, string modelo, int duracion, IReadOnlyList<TrazaAgente> herramientas, CancellationToken ct)
    {
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
            duracion);
    }
}

/// <summary>Cuerpo de <c>POST /api/agente/chat</c>.</summary>
public sealed record PreguntaChatDto(int CursoId, string? Mensaje);

/// <summary>Respuesta de <c>POST /api/agente/chat</c> (docs/contratos-api.md, HU-14).</summary>
public sealed record RespuestaChatDto(int ConversacionId, string Respuesta, IReadOnlyList<HerramientaUsadaDto> Herramientas, string Modelo, int DuracionMs);

public sealed record HerramientaUsadaDto(string Nombre, JsonElement Parametros, string Estado, int DuracionMs);

/// <summary>Fila de <c>GET /api/agente/conversaciones?cursoId=</c>: <c>herramientas</c> es cuántas se usaron.</summary>
public sealed record ConversacionResumenDto(int Id, DateTimeOffset Fecha, string Pregunta, string Modelo, int DuracionMs, int Herramientas);

/// <summary>Respuesta de <c>GET /api/agente/conversaciones/{id}</c>.</summary>
public sealed record ConversacionDto(
    int Id, int CursoId, DateTimeOffset Fecha, string Pregunta, string Respuesta, string Modelo, int DuracionMs, IReadOnlyList<HerramientaUsadaDto> Herramientas);

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
