namespace EduMonitor.Application.Refuerzos;

/// <summary>TA-15.B3 · Un plan aprobado del docente: lo que redactó el asistente y lo que el docente dejó al aprobar.</summary>
public sealed record EjemploEstilo(int PlanId, int CursoId, string Generado, string Aprobado);

/// <summary>
/// TA-15.B3 · Elige los ejemplos de estilo del docente. Solo sirven los planes donde el docente cambió el borrador: si lo aprobó tal cual,
/// no enseña nada de cómo escribe. Se usan los 2 más recientes, y solo si hay 2, para que el asistente no copie un único texto.
/// </summary>
public static class EjemplosEstilo
{
    public const int Maximo = 2;

    /// <summary>Largo máximo de cada texto del ejemplo que va al asistente, para no llenar el contexto del modelo.</summary>
    public const int LargoMaximo = 1500;

    public static IReadOnlyList<EjemploEstilo> Elegir(IEnumerable<EjemploEstilo> candidatos)
    {
        var elegidos = candidatos
            .Where(c => !string.IsNullOrWhiteSpace(c.Generado) && !string.IsNullOrWhiteSpace(c.Aprobado))
            .Where(c => !string.Equals(Normalizar(c.Generado), Normalizar(c.Aprobado), StringComparison.Ordinal))
            .OrderByDescending(c => c.PlanId)
            .Take(Maximo)
            .ToList();
        return elegidos.Count < Maximo ? Array.Empty<EjemploEstilo>() : elegidos;
    }

    /// <summary>Corta un texto al largo máximo, en el último salto de línea o espacio antes del límite.</summary>
    public static string Recortar(string texto)
    {
        texto = texto.Trim();
        if (texto.Length <= LargoMaximo)
        {
            return texto;
        }

        var corte = texto.LastIndexOfAny(new[] { '\n', ' ' }, LargoMaximo - 1);
        return (corte > LargoMaximo / 2 ? texto[..corte] : texto[..LargoMaximo]).TrimEnd() + " …";
    }

    private static string Normalizar(string texto) => string.Join(' ', texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
