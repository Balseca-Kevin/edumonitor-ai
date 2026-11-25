using System.Text.RegularExpressions;

namespace EduMonitor.Application.Indicadores;

/// <summary>
/// R.2 · Tramo del período que elige el control segmentado (todo el ciclo, un parcial o una unidad). Los cálculos del tramo los hace
/// el backend con el mismo código del curso completo, sobre las actividades de ese tramo; el frontend solo elige el tramo.
/// Códigos: <c>todo</c>, <c>p1</c>, <c>p2</c>, <c>u1</c> … <c>un</c>.
/// </summary>
public sealed partial record Tramo(int? Parcial, int? Unidad)
{
    public static readonly Tramo Todo = new(null, null);

    public bool EsTodo => Parcial is null && Unidad is null;

    public string Codigo => Parcial is int p ? $"p{p}" : Unidad is int u ? $"u{u}" : "todo";

    /// <summary>Sin texto es <see cref="Todo"/>. False si el texto no es un código de tramo.</summary>
    public static bool TryParse(string? texto, out Tramo tramo)
    {
        tramo = Todo;
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Equals("todo", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var coincidencia = Codigo_().Match(texto.Trim());
        if (!coincidencia.Success || !int.TryParse(coincidencia.Groups[2].Value, out var numero) || numero < 1)
        {
            return false;
        }

        tramo = char.ToLowerInvariant(coincidencia.Groups[1].Value[0]) == 'p' ? new Tramo(numero, null) : new Tramo(null, numero);
        return true;
    }

    /// <summary>True si el curso tiene ese parcial o esa unidad.</summary>
    public bool ExisteEn(DatosCurso datos) =>
        EsTodo
        || (Parcial is int p && datos.Unidades.Any(u => u.Parcial == p))
        || (Unidad is int n && datos.Unidades.Any(u => u.Numero == n));

    /// <summary>Los datos del curso con solo las actividades del tramo y sus calificaciones. Las actividades sin unidad solo entran en «todo».</summary>
    public DatosCurso Filtrar(DatosCurso datos)
    {
        if (EsTodo)
        {
            return datos;
        }

        var unidades = datos.Unidades
            .Where(u => (Parcial is int p && u.Parcial == p) || (Unidad is int n && u.Numero == n))
            .Select(u => u.Id)
            .ToHashSet();
        var actividades = datos.Actividades.Where(a => a.UnidadId is int id && unidades.Contains(id)).ToList();
        var ids = actividades.Select(a => a.Id).ToHashSet();
        return datos with
        {
            Actividades = actividades,
            Calificaciones = datos.Calificaciones.Where(c => ids.Contains(c.ActividadId)).ToList(),
        };
    }

    public string NoExisteDetalle => Parcial is int p ? $"El curso no tiene el parcial {p}." : $"El curso no tiene la unidad {Unidad}.";

    [GeneratedRegex("^([pPuU])([0-9]{1,2})$")]
    private static partial Regex Codigo_();
}
