using EduMonitor.Application.Configuracion;
using EduMonitor.Infrastructure.Persistencia;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduMonitor.Infrastructure.Moodle;

/// <summary>
/// TA-20.4 · Conexión con Moodle persistente (singleton). Al primer uso lee la fila de <c>ConfiguracionMoodle</c>; si no hay fila,
/// usa <c>Moodle__Url</c> y <c>Moodle__Token</c> del entorno. <see cref="Actualizar"/> guarda la fila con el token protegido por
/// Data Protection (llaves en <c>DataProtection__Llaves</c>, el volumen <c>api-keys</c> en Docker), así el cambio sobrevive al reinicio.
/// </summary>
internal sealed class ConfiguracionMoodleEnBase(
    IOptions<MoodleOptions> entorno,
    IServiceScopeFactory alcances,
    IDataProtectionProvider proteccion,
    TimeProvider reloj,
    ILogger<ConfiguracionMoodleEnBase> logger) : IConfiguracionMoodle
{
    public const string Proposito = "EduMonitor.Moodle.Token";

    private readonly object _candado = new();
    private volatile MoodleOptions? _actual;
    private DateTimeOffset _actualizado;

    public MoodleOptions Actual
    {
        get
        {
            Cargar();
            return _actual!;
        }
    }

    public DateTimeOffset Actualizado
    {
        get
        {
            Cargar();
            return _actualizado;
        }
    }

    public void Actualizar(string url, string? token)
    {
        Cargar();
        lock (_candado)
        {
            var anterior = _actual!;
            var nuevoToken = string.IsNullOrWhiteSpace(token) ? anterior.Token : token.Trim();
            var ahora = reloj.GetLocalNow();

            using var alcance = alcances.CreateScope();
            var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
            var fila = db.ConfiguracionesMoodle.OrderBy(c => c.Id).FirstOrDefault();
            if (fila is null)
            {
                fila = new Domain.Entities.ConfiguracionMoodle();
                db.ConfiguracionesMoodle.Add(fila);
            }

            fila.Url = url;
            fila.TokenProtegido = string.IsNullOrEmpty(nuevoToken) ? string.Empty : Protector().Protect(nuevoToken);
            fila.Actualizado = ahora;
            db.SaveChanges();

            _actualizado = ahora;
            _actual = Copiar(anterior, url, nuevoToken);
        }
    }

    private IDataProtector Protector() => proteccion.CreateProtector(Proposito);

    private void Cargar()
    {
        if (_actual is not null)
        {
            return;
        }

        lock (_candado)
        {
            if (_actual is not null)
            {
                return;
            }

            var inicial = entorno.Value;
            try
            {
                using var alcance = alcances.CreateScope();
                var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
                var fila = db.ConfiguracionesMoodle.AsNoTracking().OrderBy(c => c.Id).FirstOrDefault();
                if (fila is not null)
                {
                    var token = string.IsNullOrEmpty(fila.TokenProtegido) ? string.Empty : Protector().Unprotect(fila.TokenProtegido);
                    _actualizado = fila.Actualizado;
                    _actual = Copiar(inicial, fila.Url, token);
                    logger.LogInformation("Configuración de Moodle leída de la base: {Url}, token {Token}", fila.Url, _actual.TokenEnmascarado);
                    return;
                }
            }
            catch (Exception e)
            {
                // Base sin migrar o llaves perdidas: se usa la del entorno. El mensaje no lleva el token.
                logger.LogWarning("No se pudo leer la configuración de Moodle guardada ({Motivo}); se usa la del entorno.", e.GetType().Name);
            }

            _actualizado = reloj.GetLocalNow();
            _actual = Copiar(inicial, inicial.Url, inicial.Token);
        }
    }

    private static MoodleOptions Copiar(MoodleOptions origen, string url, string token) => new()
    {
        Url = url,
        Servicio = origen.Servicio,
        Token = token,
        TiempoEsperaSegundos = origen.TiempoEsperaSegundos,
    };
}
