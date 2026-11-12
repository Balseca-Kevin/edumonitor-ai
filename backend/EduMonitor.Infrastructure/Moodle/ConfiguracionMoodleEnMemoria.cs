using EduMonitor.Application.Configuracion;
using Microsoft.Extensions.Options;

namespace EduMonitor.Infrastructure.Moodle;

/// <summary>Conexión con Moodle en memoria (singleton). Cada cambio reemplaza la copia completa, así una llamada en curso no ve datos a medias.</summary>
internal sealed class ConfiguracionMoodleEnMemoria(IOptions<MoodleOptions> inicial, TimeProvider reloj) : IConfiguracionMoodle
{
    private volatile MoodleOptions _actual = Copiar(inicial.Value, inicial.Value.Url, inicial.Value.Token);
    private DateTimeOffset _actualizado = reloj.GetLocalNow();

    public MoodleOptions Actual => _actual;

    public DateTimeOffset Actualizado => _actualizado;

    public void Actualizar(string url, string? token)
    {
        var anterior = _actual;
        _actual = Copiar(anterior, url, string.IsNullOrWhiteSpace(token) ? anterior.Token : token.Trim());
        _actualizado = reloj.GetLocalNow();
    }

    private static MoodleOptions Copiar(MoodleOptions origen, string url, string token) => new()
    {
        Url = url,
        Servicio = origen.Servicio,
        Token = token,
        TiempoEsperaSegundos = origen.TiempoEsperaSegundos,
    };
}
