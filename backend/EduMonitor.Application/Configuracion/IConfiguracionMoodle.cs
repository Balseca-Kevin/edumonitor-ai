namespace EduMonitor.Application.Configuracion;

/// <summary>
/// Conexión con Moodle vigente. Desde el Sprint 2 (TA-20.4) se guarda en la tabla <c>ConfiguracionMoodle</c> con el token protegido
/// por Data Protection y sobrevive al reinicio; si no hay fila, se usan <c>Moodle__Url</c> y <c>Moodle__Token</c> del entorno.
/// Un ADMIN la cambia con <c>PUT /api/admin/moodle-config</c>.
/// </summary>
public interface IConfiguracionMoodle
{
    /// <summary>Copia de la conexión actual. <see cref="MoodleOptions.ToString"/> y <see cref="MoodleOptions.TokenEnmascarado"/> nunca muestran el token.</summary>
    MoodleOptions Actual { get; }

    /// <summary>Cuándo se cargó o se cambió por última vez la conexión.</summary>
    DateTimeOffset Actualizado { get; }

    /// <summary>Cambia la URL y, si <paramref name="token"/> no está vacío, el token.</summary>
    void Actualizar(string url, string? token);
}
