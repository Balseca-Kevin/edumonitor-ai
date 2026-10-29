using EduMonitor.Application.Configuracion;
using EduMonitor.Infrastructure.Moodle;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EduMonitor.Tests.Infraestructura;

/// <summary>
/// Acceso al Moodle de laboratorio real para las pruebas de integración. Lee <c>Moodle__Url</c> y <c>Moodle__Token</c>
/// del .env de la raíz del repo sin copiarlos a variables de entorno, para no alterar las demás pruebas.
/// En CI no hay .env: esas pruebas se omiten.
/// </summary>
public static class MoodleReal
{
    private static readonly Lazy<MoodleOptions?> Opciones = new(Leer);

    public static bool Disponible => Opciones.Value is not null;

    public static MoodleOptions Configuracion => Opciones.Value ?? throw new InvalidOperationException("No hay .env con Moodle__Token.");

    internal static MoodleClient Cliente() =>
        new(new HttpClient { Timeout = TimeSpan.FromSeconds(30) }, new ConfiguracionMoodleEnMemoria(Options.Create(Configuracion), TimeProvider.System), NullLogger<MoodleClient>.Instance);

    private static MoodleOptions? Leer()
    {
        try
        {
            var valores = DotNetEnv.Env.NoEnvVars().TraversePath().Load().ToDictionary(v => v.Key, v => v.Value);
            return valores.TryGetValue("Moodle__Url", out var url) && valores.TryGetValue("Moodle__Token", out var token)
                && !string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(token)
                ? new MoodleOptions { Url = url, Token = token }
                : null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}

/// <summary>Prueba que necesita el Moodle de laboratorio levantado y el .env con su token; si no hay .env se omite.</summary>
public sealed class FactMoodleRealAttribute : FactAttribute
{
    public FactMoodleRealAttribute()
    {
        if (!MoodleReal.Disponible)
        {
            Skip = "Requiere el Moodle de laboratorio y Moodle__Token en el .env de la raíz (no disponible en CI).";
        }
    }
}
