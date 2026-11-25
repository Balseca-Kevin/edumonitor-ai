using EduMonitor.Application.Abstractions;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Agente;

/// <summary>
/// HU-23 · <c>GET/PUT /api/admin/modelo</c>: los dos modelos del asistente, cuál responde ahora y el interruptor «Usar solo el modelo
/// local». El interruptor se guarda en la base (una fila); el estado de cada modelo lo da <c>GET /salud</c> del agente en 3 s.
/// </summary>
public sealed class ModeloAsistenteService(IEduMonitorDbContext db, IAgenteClient agente, TimeProvider reloj)
{
    public static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(3);

    public async Task<bool> SoloLocalAsync(CancellationToken ct = default) =>
        await db.ConfiguracionesAsistente.AsNoTracking().Select(c => c.SoloModeloLocal).FirstOrDefaultAsync(ct);

    public async Task<ModeloAsistenteDto> ConsultarAsync(CancellationToken ct = default)
    {
        var configuracion = await db.ConfiguracionesAsistente.AsNoTracking().FirstOrDefaultAsync(ct);
        var soloLocal = configuracion?.SoloModeloLocal ?? false;

        SaludAgente? salud = null;
        string? motivo = null;
        using (var limite = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            limite.CancelAfter(TiempoMaximo);
            try
            {
                salud = await agente.SaludAsync(limite.Token);
            }
            catch (AgenteNoDisponibleException e)
            {
                motivo = e.Message;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                motivo = $"El agente no respondió en {TiempoMaximo.TotalSeconds:0} s";
            }
        }

        if (salud is null)
        {
            var sinDato = $"Sin dato: {motivo}";
            return new ModeloAsistenteDto(soloLocal, "NINGUNO",
                new ModeloDto("externo", null, null, EstadoSistemaNoDisponible, sinDato),
                new ModeloDto("local", null, null, EstadoSistemaNoDisponible, sinDato),
                configuracion?.Actualizado);
        }

        var externo = salud.Externo;
        var estadoExterno = externo?.Estado ?? "NO_CONFIGURADO";
        var detalleExterno = estadoExterno switch
        {
            "OK" => soloLocal ? "Configurado; no responde porque está activo «Usar solo el modelo local»" : "Configurado",
            "EN_PAUSA" => $"En pausa por 3 fallos en 5 minutos; vuelve en {Math.Max(1, (externo!.PausaSegundos + 59) / 60)} min",
            _ => "Sin configurar: responde siempre el modelo local",
        };
        var local = new ModeloDto("local", "ollama", salud.Modelo, salud.Ollama, salud.Ollama == "OK" ? "Ollama responde" : "Ollama no responde");
        var activo = !soloLocal && estadoExterno == "OK" ? "EXTERNO" : "LOCAL";
        return new ModeloAsistenteDto(
            soloLocal,
            activo,
            new ModeloDto("externo", externo?.Proveedor, externo?.Modelo, estadoExterno, detalleExterno),
            local,
            configuracion?.Actualizado);
    }

    public async Task GuardarAsync(bool soloLocal, CancellationToken ct = default)
    {
        var configuracion = await db.ConfiguracionesAsistente.FirstOrDefaultAsync(ct);
        if (configuracion is null)
        {
            configuracion = new ConfiguracionAsistente();
            db.ConfiguracionesAsistente.Add(configuracion);
        }

        configuracion.SoloModeloLocal = soloLocal;
        configuracion.Actualizado = reloj.GetLocalNow();
        await db.SaveChangesAsync(ct);
    }

    private const string EstadoSistemaNoDisponible = "NO_DISPONIBLE";
}

/// <summary>Respuesta de <c>GET /api/admin/modelo</c>. <c>Activo</c>: EXTERNO, LOCAL o NINGUNO (agente caído).</summary>
public sealed record ModeloAsistenteDto(bool SoloLocal, string Activo, ModeloDto Externo, ModeloDto Local, DateTimeOffset? Actualizado);

/// <summary>Un modelo: <c>Estado</c> OK, NO_CONFIGURADO, EN_PAUSA o NO_DISPONIBLE, con un detalle útil y sin secretos.</summary>
public sealed record ModeloDto(string Tipo, string? Proveedor, string? Modelo, string Estado, string Detalle);

/// <summary>Cuerpo de <c>PUT /api/admin/modelo</c>.</summary>
public sealed record CambioModeloDto(bool SoloLocal);
