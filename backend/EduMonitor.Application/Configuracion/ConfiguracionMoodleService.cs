using EduMonitor.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace EduMonitor.Application.Configuracion;

/// <summary>
/// TA-02.3 · Leer, cambiar y probar la conexión con Moodle (<c>/api/admin/moodle-config</c>, solo ADMIN).
/// El token completo nunca sale del backend: las respuestas y los registros usan el token enmascarado.
/// </summary>
public sealed class ConfiguracionMoodleService(IConfiguracionMoodle configuracion, IMoodleClient moodle, ILogger<ConfiguracionMoodleService> logger)
{
    /// <summary>Las únicas funciones de Moodle que EduMonitor puede llamar.</summary>
    public static readonly IReadOnlyList<string> FuncionesPermitidas = new[]
    {
        "core_webservice_get_site_info",
        "core_enrol_get_users_courses",
        "core_course_get_contents",
        "core_enrol_get_enrolled_users",
        "gradereport_user_get_grade_items",
        "mod_assign_get_assignments",
        "mod_assign_get_submissions",
        "core_completion_get_activities_completion_status",
    };

    public ConfiguracionMoodleDto Obtener()
    {
        var actual = configuracion.Actual;
        return new ConfiguracionMoodleDto(actual.Url, actual.Servicio, actual.TokenEnmascarado, configuracion.Actualizado);
    }

    /// <summary>Devuelve false si la URL no es http(s) absoluta; en ese caso no cambia nada.</summary>
    public bool Actualizar(string? url, string? token)
    {
        if (!EsUrlValida(url))
        {
            return false;
        }

        configuracion.Actualizar(url!.TrimEnd('/'), token);
        var actual = configuracion.Actual;
        logger.LogInformation("Configuración de Moodle actualizada: {Url}, token {Token}", actual.Url, actual.TokenEnmascarado);
        return true;
    }

    /// <summary>Llama a core_webservice_get_site_info y marca cuáles de las 8 funciones permitidas tiene habilitadas el servicio.</summary>
    public async Task<PruebaConexionMoodleDto> ProbarAsync(CancellationToken ct = default)
    {
        try
        {
            var info = await moodle.ObtenerInfoSitioAsync(ct);
            var habilitadas = info.Funciones.ToHashSet(StringComparer.Ordinal);
            var funciones = FuncionesPermitidas.Select(f => new FuncionMoodleDto(f, habilitadas.Contains(f))).ToList();
            return new PruebaConexionMoodleDto(true, info.Sitio, info.Version, info.UsuarioTecnico, funciones, null);
        }
        catch (MoodleNoDisponibleException e)
        {
            logger.LogWarning("Prueba de conexión con Moodle fallida: {Error}", e.Message);
            return new PruebaConexionMoodleDto(false, null, null, null, Array.Empty<FuncionMoodleDto>(), e.Message);
        }
    }

    public static bool EsUrlValida(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

/// <summary>Respuesta de <c>GET /api/admin/moodle-config</c> (docs/contratos-api.md).</summary>
public sealed record ConfiguracionMoodleDto(string Url, string Servicio, string TokenEnmascarado, DateTimeOffset Actualizado);

/// <summary>Cuerpo de <c>PUT /api/admin/moodle-config</c>. Sin token se conserva el actual.</summary>
public sealed record ActualizarConfiguracionMoodle(string? Url, string? Token);

/// <summary>Respuesta de <c>POST /api/admin/moodle-config/test</c>. Si Moodle no responde: <c>conectado = false</c> y <c>error</c>.</summary>
public sealed record PruebaConexionMoodleDto(
    bool Conectado,
    string? Sitio,
    string? Version,
    string? UsuarioTecnico,
    IReadOnlyList<FuncionMoodleDto> Funciones,
    string? Error);

public sealed record FuncionMoodleDto(string Nombre, bool Disponible);
