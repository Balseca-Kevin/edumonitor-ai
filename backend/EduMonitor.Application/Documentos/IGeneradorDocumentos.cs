namespace EduMonitor.Application.Documentos;

/// <summary>Contenido de un documento de refuerzo aprobado: lo que el docente revisó y aprobó, con los datos reales del plan.</summary>
public sealed record DocumentoRefuerzo(
    string Curso,
    string Actividad,
    IReadOnlyList<string> Temas,
    IReadOnlyList<string> Estudiantes,
    string Texto,
    DateTimeOffset Aprobado);

/// <summary>
/// HU-15 · Generador de documentos (bloque 4, Kevin): PDF con QuestPDF y DOCX con DocumentFormat.OpenXml. La implementación vive en
/// Infrastructure; Application solo pide los bytes.
/// </summary>
public interface IGeneradorDocumentos
{
    byte[] Pdf(DocumentoRefuerzo documento);

    byte[] Docx(DocumentoRefuerzo documento);
}
