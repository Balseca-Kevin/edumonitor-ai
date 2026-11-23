using System.Net;
using System.Text.RegularExpressions;

namespace EduMonitor.Application.Sincronizaciones;

/// <summary>
/// TA-08.A1 · Temas del sílabo que el aula virtual guarda en el resumen de cada sección, como lista HTML
/// (<c>&lt;ul&gt;&lt;li&gt;1.1 Evolución del software&lt;/li&gt;…&lt;/ul&gt;</c>). Sin lista, cada línea o párrafo es un tema.
/// </summary>
public static partial class TemasSilabo
{
    public const int LargoMaximo = 250;

    public static IReadOnlyList<string> Leer(string? resumenHtml)
    {
        if (string.IsNullOrWhiteSpace(resumenHtml))
        {
            return Array.Empty<string>();
        }

        var elementos = ElementoLista().Matches(resumenHtml).Select(m => m.Groups[1].Value).ToList();
        if (elementos.Count == 0)
        {
            elementos = SaltoDeBloque().Split(resumenHtml).ToList();
        }

        return elementos
            .Select(e => WebUtility.HtmlDecode(Etiqueta().Replace(e, " ")))
            .Select(t => Espacios().Replace(t, " ").Trim())
            .Where(t => t.Length > 0)
            .Select(t => t.Length > LargoMaximo ? t[..LargoMaximo] : t)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    [GeneratedRegex(@"<li[^>]*>(.*?)</li>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ElementoLista();

    [GeneratedRegex(@"<br\s*/?>|</p>|\r?\n", RegexOptions.IgnoreCase)]
    private static partial Regex SaltoDeBloque();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Etiqueta();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}
