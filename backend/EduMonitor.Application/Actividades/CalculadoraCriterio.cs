namespace EduMonitor.Application.Actividades;

/// <summary>
/// TA-22.A1 · Diagnóstico por criterio de la rúbrica, sin base de datos. Dice si un criterio separa a quienes van bien del resto y
/// si conviene revisar cómo se enseñó o cómo está escrito. Con un curso de 32 estudiantes la cifra es orientativa: la pantalla dice
/// «revisar», no un veredicto.
/// </summary>
public static class CalculadoraCriterio
{
    /// <summary>Con este dominio o menos, casi todo el curso falló el criterio: revisar cómo se enseñó.</summary>
    public const decimal DominioBajo = 0.30m;

    /// <summary>Con esta separación o menos, el criterio no distingue a quienes van bien del resto: revisar el enunciado.</summary>
    public const decimal SeparacionBaja = 0.20m;

    /// <summary>Con menos estudiantes calificados la separación no se calcula.</summary>
    public const int MinimoEstudiantes = 8;

    public const string RevisarEnsenanza = "REVISAR_ENSENANZA";
    public const string RevisarEnunciado = "REVISAR_ENUNCIADO";

    /// <summary>Coeficiente de Pearson entre dos series del mismo largo, redondeado a 4 decimales. Null si alguna no varía.</summary>
    public static decimal? Pearson(IReadOnlyList<decimal> x, IReadOnlyList<decimal> y)
    {
        if (x.Count != y.Count || x.Count < 2)
        {
            return null;
        }

        var mx = x.Average(v => (double)v);
        var my = y.Average(v => (double)v);
        double sxy = 0, sxx = 0, syy = 0;
        for (var i = 0; i < x.Count; i++)
        {
            var dx = (double)x[i] - mx;
            var dy = (double)y[i] - my;
            sxy += dx * dy;
            sxx += dx * dx;
            syy += dy * dy;
        }

        if (sxx < 1e-12 || syy < 1e-12)
        {
            return null;
        }

        var r = Math.Clamp(sxy / Math.Sqrt(sxx * syy), -1, 1);
        return Math.Round((decimal)r, 4);
    }

    /// <summary>
    /// Separación de un criterio: Pearson entre el puntaje del criterio y la nota de la rúbrica sin ese criterio (la suma de los
    /// demás), sobre los estudiantes calificados. Null con un solo criterio, con menos de 8 estudiantes o si un lado no varía.
    /// </summary>
    public static decimal? Separacion(IReadOnlyList<(decimal Criterio, decimal Resto)> estudiantes, int criteriosEnRubrica)
    {
        if (criteriosEnRubrica < 2 || estudiantes.Count < MinimoEstudiantes)
        {
            return null;
        }

        return Pearson(estudiantes.Select(e => e.Criterio).ToList(), estudiantes.Select(e => e.Resto).ToList());
    }

    /// <summary>
    /// <see cref="RevisarEnsenanza"/> si el dominio es menor o igual a <see cref="DominioBajo"/>; si no,
    /// <see cref="RevisarEnunciado"/> si la separación existe y es menor o igual a <see cref="SeparacionBaja"/>; si no, null.
    /// </summary>
    public static string? Aviso(decimal? domina, decimal? separacion)
    {
        if (domina is null)
        {
            return null;
        }

        if (domina <= DominioBajo)
        {
            return RevisarEnsenanza;
        }

        return separacion is decimal s && s <= SeparacionBaja ? RevisarEnunciado : null;
    }
}
