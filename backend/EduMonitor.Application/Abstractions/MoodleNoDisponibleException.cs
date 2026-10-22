namespace EduMonitor.Application.Abstractions;

/// <summary>Moodle no respondió o devolvió un error. La API la traduce a 502 con ProblemDetails.</summary>
public sealed class MoodleNoDisponibleException : Exception
{
    public MoodleNoDisponibleException(string funcion, string mensaje, Exception? causa = null)
        : base(mensaje, causa)
    {
        Funcion = funcion;
    }

    /// <summary>Función de Moodle que falló, por ejemplo <c>core_enrol_get_enrolled_users</c>.</summary>
    public string Funcion { get; }
}
