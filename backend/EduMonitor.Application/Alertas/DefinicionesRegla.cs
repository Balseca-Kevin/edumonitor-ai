namespace EduMonitor.Application.Alertas;

/// <summary>Definición fija de una regla de alerta (HU-12): código, nombre, umbral por defecto, rango y condición.</summary>
public sealed record DefinicionRegla(
    string Codigo,
    string Nombre,
    decimal UmbralPorDefecto,
    decimal UmbralMinimo,
    decimal UmbralMaximo,
    string Condicion);

/// <summary>
/// TA-12.1 · Reglas del Sprint 2 con sus umbrales por defecto y sus rangos, tal como los define docs/contratos-api.md.
/// Es la única fuente de verdad: la usan las clases de regla, el servicio de reglas y el validador del PUT.
/// </summary>
public static class DefinicionesRegla
{
    public const string CodigoR1 = "R1";
    public const string CodigoR2 = "R2";
    public const string CodigoR4 = "R4";

    public static readonly DefinicionRegla R1 = new(
        CodigoR1,
        "Descenso reciente",
        UmbralPorDefecto: 1.5m,
        UmbralMinimo: 0.5m,
        UmbralMaximo: 5m,
        Condicion: "Tendencia menor o igual a −umbral con las últimas 3 notas");

    public static readonly DefinicionRegla R2 = new(
        CodigoR2,
        "Atrasos",
        UmbralPorDefecto: 2m,
        UmbralMinimo: 1m,
        UmbralMaximo: 10m,
        Condicion: "Actividades vencidas sin entrega mayor o igual a umbral");

    public static readonly DefinicionRegla R4 = new(
        CodigoR4,
        "Promedio bajo",
        UmbralPorDefecto: 7.0m,
        UmbralMinimo: 1m,
        UmbralMaximo: 10m,
        Condicion: "Promedio ponderado menor a umbral con al menos 4 notas");

    /// <summary>Las tres reglas, en el orden en que se muestran al docente.</summary>
    public static IReadOnlyList<DefinicionRegla> Todas { get; } = new[] { R1, R2, R4 };

    /// <summary>La definición de un código, o null si el código no existe (p. ej. R3).</summary>
    public static DefinicionRegla? PorCodigo(string? codigo) =>
        Todas.FirstOrDefault(d => string.Equals(d.Codigo, codigo, StringComparison.OrdinalIgnoreCase));

    /// <summary>True si el umbral queda dentro del rango permitido de esa regla.</summary>
    public static bool UmbralEnRango(DefinicionRegla definicion, decimal umbral) =>
        umbral >= definicion.UmbralMinimo && umbral <= definicion.UmbralMaximo;
}
