using EduMonitor.Application.Seguridad;

namespace EduMonitor.Application.Configuracion;

/// <summary>
/// Conexión con el Moodle de laboratorio. Sección <c>Moodle</c> (variables <c>Moodle__Url</c>, <c>Moodle__Servicio</c>, <c>Moodle__Token</c>).
/// El token solo se lee de aquí; en logs y respuestas se usa <see cref="TokenEnmascarado"/>.
/// </summary>
public sealed class MoodleOptions
{
    public const string Seccion = "Moodle";

    public string Url { get; set; } = string.Empty;
    public string Servicio { get; set; } = "edumonitor_ws";
    public string Token { get; set; } = string.Empty;
    public int TiempoEsperaSegundos { get; set; } = 30;

    public string TokenEnmascarado => EnmascaradorSecretos.Enmascarar(Token);

    public bool TokenConfigurado => !string.IsNullOrWhiteSpace(Token);

    // Si alguien registra las opciones completas ("{@Opciones}" o interpolación), el token sale enmascarado.
    public override string ToString() => $"Moodle {{ Url = {Url}, Servicio = {Servicio}, Token = {TokenEnmascarado} }}";
}
