using System.Text.Json;
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
        var ahora = reloj.GetLocalNow();
        var conversacion = new Conversacion
        {
            UsuarioId = usuario.Id,
            CursoId = cursoId,
            Fecha = ahora,
            Pregunta = pregunta,
            Respuesta = respuesta.Respuesta,
            Modelo = respuesta.Modelo,
            DuracionMs = duracion,
            Trazas = respuesta.Herramientas.Select(h => new TrazaHerramienta
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

        return new ResultadoChat(EstadoChat.Respondido, new RespuestaChatDto(
            conversacion.Id,
            respuesta.Respuesta,
            respuesta.Herramientas.Select(h => new HerramientaUsadaDto(h.Nombre, h.Parametros, h.Estado, h.DuracionMs)).ToList(),
            respuesta.Modelo,
            duracion));
    }
}

/// <summary>Cuerpo de <c>POST /api/agente/chat</c>.</summary>
public sealed record PreguntaChatDto(int CursoId, string? Mensaje);

/// <summary>Respuesta de <c>POST /api/agente/chat</c> (docs/contratos-api.md, HU-14).</summary>
public sealed record RespuestaChatDto(int ConversacionId, string Respuesta, IReadOnlyList<HerramientaUsadaDto> Herramientas, string Modelo, int DuracionMs);

public sealed record HerramientaUsadaDto(string Nombre, JsonElement Parametros, string Estado, int DuracionMs);

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
