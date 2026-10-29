namespace EduMonitor.Application.Configuracion;

/// <summary>
/// Conexión con Moodle vigente. Arranca con <c>Moodle__Url</c> y <c>Moodle__Token</c> del entorno; un ADMIN puede cambiarla con
/// <c>PUT /api/admin/moodle-config</c>. El cambio vive en memoria mientras corre la API (decisión del Sprint 1: el token
/// no se guarda en la base) y se pierde al reiniciar.
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
