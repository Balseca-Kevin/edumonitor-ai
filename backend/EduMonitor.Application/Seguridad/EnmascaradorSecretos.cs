namespace EduMonitor.Application.Seguridad;

/// <summary>Enmascara secretos (token de Moodle) para logs y respuestas: «••••» más los últimos 4 caracteres.</summary>
public static class EnmascaradorSecretos
{
    public const string Mascara = "••••";
    private const int CaracteresVisibles = 4;

    /// <summary>
    /// <c>"0123456789abcdef3f9a"</c> → <c>"••••3f9a"</c>. Un secreto vacío devuelve una cadena vacía.
    /// Si el secreto tiene 8 caracteres o menos no se muestra ninguno, para no revelar la mitad o más.
    /// </summary>
    public static string Enmascarar(string? secreto)
    {
        if (string.IsNullOrEmpty(secreto))
        {
            return string.Empty;
        }

        return secreto.Length <= CaracteresVisibles * 2
            ? Mascara
            : Mascara + secreto[^CaracteresVisibles..];
    }
}
