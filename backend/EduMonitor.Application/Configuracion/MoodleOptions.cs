namespace EduMonitor.Application.Configuracion;

/// <summary>Conexión con el Moodle de laboratorio. Sección <c>Moodle</c> (variables <c>Moodle__Url</c>, <c>Moodle__Servicio</c>, <c>Moodle__Token</c>).</summary>
public sealed class MoodleOptions
{
    public const string Seccion = "Moodle";

    public string Url { get; set; } = string.Empty;
    public string Servicio { get; set; } = "edumonitor_ws";
    public string Token { get; set; } = string.Empty;
    public int TiempoEsperaSegundos { get; set; } = 30;
}
