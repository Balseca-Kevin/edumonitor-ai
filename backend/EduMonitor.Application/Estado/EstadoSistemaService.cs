using System.Reflection;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Agente;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Estado;

/// <summary>
/// TA-20.5 · Estado del sistema para el ADMIN: api, sqlserver, moodle, agente y modelo. Cada servicio se consulta en paralelo con
/// un tiempo máximo de 3 s; uno caído no detiene a los demás y se informa como NO_DISPONIBLE con un detalle útil, sin secretos.
/// </summary>
public sealed class EstadoSistemaService(IEduMonitorDbContext db, IMoodleClient moodle, IAgenteClient agente, TimeProvider reloj)
{
    public static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(3);

    public const string Ok = "OK";
    public const string NoDisponible = "NO_DISPONIBLE";

    public async Task<EstadoSistemaDto> ConsultarAsync(CancellationToken ct = default)
    {
        var baseDatos = ConLimiteAsync(BaseDatosAsync, "sqlserver", ct);
        var aulaVirtual = ConLimiteAsync(MoodleAsync, "moodle", ct);
        var saludAgente = SaludAgenteAsync(ct);
        await Task.WhenAll(baseDatos, aulaVirtual, saludAgente);

        var (servicioAgente, servicioModelo) = saludAgente.Result;
        return new EstadoSistemaDto(reloj.GetLocalNow(), new[]
        {
            new ServicioEstadoDto("api", Ok, $"versión {Version()}"),
            baseDatos.Result,
            aulaVirtual.Result,
            servicioAgente,
            servicioModelo,
        });
    }

    private async Task<ServicioEstadoDto> BaseDatosAsync(CancellationToken ct)
    {
        await db.Usuarios.AnyAsync(ct);
        return new ServicioEstadoDto("sqlserver", Ok, "EduMonitorDB");
    }

    private async Task<ServicioEstadoDto> MoodleAsync(CancellationToken ct)
    {
        try
        {
            var info = await moodle.ObtenerInfoSitioAsync(ct);
            return new ServicioEstadoDto("moodle", Ok, $"{info.Sitio} {info.Version}");
        }
        catch (MoodleNoDisponibleException e)
        {
            return new ServicioEstadoDto("moodle", NoDisponible, e.Message);
        }
    }

    private async Task<(ServicioEstadoDto Agente, ServicioEstadoDto Modelo)> SaludAgenteAsync(CancellationToken ct)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TiempoMaximo);
        try
        {
            var salud = await agente.SaludAsync(limite.Token);
            var modelo = salud.Ollama == Ok
                ? new ServicioEstadoDto("modelo", Ok, salud.Modelo)
                : new ServicioEstadoDto("modelo", NoDisponible, $"Ollama no responde ({salud.Modelo})");
            return (new ServicioEstadoDto("agente", Ok, "Responde en la red interna"), modelo);
        }
        catch (Exception e) when (e is AgenteNoDisponibleException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            var detalle = e is OperationCanceledException ? $"No respondió en {TiempoMaximo.TotalSeconds:0} s" : e.Message;
            return (new ServicioEstadoDto("agente", NoDisponible, detalle),
                new ServicioEstadoDto("modelo", NoDisponible, "Sin dato: el agente no respondió"));
        }
    }

    /// <summary>Ejecuta la consulta con el tiempo máximo; si se pasa o falla, el servicio queda NO_DISPONIBLE y los demás siguen.</summary>
    private static async Task<ServicioEstadoDto> ConLimiteAsync(Func<CancellationToken, Task<ServicioEstadoDto>> consulta, string nombre, CancellationToken ct)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TiempoMaximo);
        try
        {
            return await consulta(limite.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ServicioEstadoDto(nombre, NoDisponible, $"No respondió en {TiempoMaximo.TotalSeconds:0} s");
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return new ServicioEstadoDto(nombre, NoDisponible, $"Sin conexión ({e.GetType().Name})");
        }
    }

    private static string Version() =>
        (Assembly.GetEntryAssembly() ?? typeof(EstadoSistemaService).Assembly)
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "0.2.1";
}

/// <summary>Respuesta de <c>GET /api/admin/estado</c> (docs/contratos-api.md, HU-20).</summary>
public sealed record EstadoSistemaDto(DateTimeOffset Generado, IReadOnlyList<ServicioEstadoDto> Servicios);

/// <summary>Estado de un servicio: <c>OK</c> o <c>NO_DISPONIBLE</c>, con un detalle útil.</summary>
public sealed record ServicioEstadoDto(string Nombre, string Estado, string Detalle);
