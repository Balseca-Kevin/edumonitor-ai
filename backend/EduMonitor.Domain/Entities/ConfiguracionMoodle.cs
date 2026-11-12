namespace EduMonitor.Domain.Entities;

/// <summary>
/// Conexión con Moodle que guarda el ADMIN desde la aplicación (HU-20, TA-20.4). Una sola fila.
/// El token se guarda protegido con ASP.NET Core Data Protection, nunca en texto plano.
/// </summary>
public class ConfiguracionMoodle
{
    public int Id { get; set; }

    public string Url { get; set; } = string.Empty;

    /// <summary>Token del servicio web protegido con Data Protection. Vacío si no hay token.</summary>
    public string TokenProtegido { get; set; } = string.Empty;

    public DateTimeOffset Actualizado { get; set; }
}
