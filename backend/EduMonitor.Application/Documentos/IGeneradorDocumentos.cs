namespace EduMonitor.Application.Documentos;

/// <summary>Contenido de un documento de refuerzo aprobado: lo que el docente revisó y aprobó, con los datos reales del plan.</summary>
public sealed record DocumentoRefuerzo(
    string Curso,
    string Actividad,
    IReadOnlyList<string> Temas,
    IReadOnlyList<string> Estudiantes,
    string Texto,
    DateTimeOffset Aprobado)
{
    /// <summary>HU-15 · «Plan de refuerzo» o «Retroalimentación».</summary>
    public string Titulo { get; init; } = "Plan de refuerzo";
}

/// <summary>
/// HU-15 · Generador de documentos (bloque 4, Kevin): PDF con QuestPDF y DOCX con DocumentFormat.OpenXml. La implementación vive en
/// Infrastructure; Application solo pide los bytes.
/// </summary>
public interface IGeneradorDocumentos
{
    byte[] Pdf(DocumentoRefuerzo documento);

    byte[] Docx(DocumentoRefuerzo documento);

    /// <summary>HU-16 · Informe del curso en PDF con los indicadores y el resumen del asistente.</summary>
    byte[] Informe(DocumentoInforme documento);
}

/// <summary>
/// HU-16 · Contenido del informe del curso: cifras de los indicadores (las mismas del panel) y el resumen que redactó el asistente en
/// una conversación del docente. Cada cifra es real; las que no están disponibles van como «no disponible».
/// </summary>
public sealed record DocumentoInforme(
    string Curso,
    string Periodo,
    DateTimeOffset Sincronizacion,
    string EstadoSincronizacion,
    IReadOnlyList<(string Etiqueta, string Valor)> Cifras,
    string Pregunta,
    string Resumen,
    string Respondio,
    DateTimeOffset Generado)
{
    /// <summary>
    /// TA-16.A1 · Refuerzos aplicados del curso: una fila por estudiante y tema de cada plan aprobado. Vacía si todavía no hay; el
    /// informe dice entonces «Aún no hay refuerzos aplicados».
    /// </summary>
    public IReadOnlyList<FilaRefuerzoInforme> Refuerzos { get; init; } = [];
}

/// <summary>
/// TA-16.A1 · Una fila de «Refuerzos aplicados» en el informe. <see cref="Cambio"/> dice el cambio del tema, no que el refuerzo lo
/// causó («subió de 5,20 a 7,10 en el tema después del refuerzo»).
/// </summary>
public sealed record FilaRefuerzoInforme(string Tema, string Estudiante, string Antes, string Ahora, string Estado, string Cambio);
