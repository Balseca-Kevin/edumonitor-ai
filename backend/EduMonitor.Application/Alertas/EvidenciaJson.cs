using System.Text.Json;

namespace EduMonitor.Application.Alertas;

/// <summary>Serializa la evidencia de una alerta en JSON camelCase, igual que el resto de la API (HU-12).</summary>
internal static class EvidenciaJson
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);

    public static string Serializar(object valor) => JsonSerializer.Serialize(valor, Opciones);
}
