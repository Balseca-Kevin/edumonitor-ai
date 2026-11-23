using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EduMonitor.Application.Documentos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EduMonitor.Infrastructure.Documentos;

/// <summary>
/// HU-15 · Documento del plan de refuerzo aprobado, en PDF (QuestPDF, licencia Community) y DOCX (DocumentFormat.OpenXml). Lleva el
/// curso, la actividad evaluada, los temas, los estudiantes, la fecha de aprobación y el texto que aprobó el docente.
/// </summary>
internal sealed class GeneradorDocumentos : IGeneradorDocumentos
{
    private static readonly CultureInfo Ecuador = CultureInfo.GetCultureInfo("es-EC");

    static GeneradorDocumentos()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Pdf(DocumentoRefuerzo d) =>
        QuestPDF.Fluent.Document.Create(contenedor => contenedor.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(2, Unit.Centimetre);
            pagina.DefaultTextStyle(t => t.FontSize(11));
            pagina.Header().Column(c =>
            {
                c.Item().Text("Plan de refuerzo").FontSize(20).Bold();
                c.Item().Text($"{d.Curso} · {d.Actividad}").FontSize(12).FontColor(Colors.Grey.Darken2);
            });
            pagina.Content().PaddingVertical(12).Column(c =>
            {
                c.Spacing(8);
                c.Item().Text(t =>
                {
                    t.Span("Temas: ").Bold();
                    t.Span(string.Join(", ", d.Temas));
                });
                c.Item().Text(t =>
                {
                    t.Span("Estudiantes: ").Bold();
                    t.Span(string.Join(", ", d.Estudiantes));
                });
                c.Item().Text(t =>
                {
                    t.Span("Aprobado por el docente: ").Bold();
                    t.Span(d.Aprobado.ToString("dd/MM/yyyy HH:mm", Ecuador));
                });
                c.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                foreach (var linea in Lineas(d.Texto))
                {
                    c.Item().Text(linea);
                }
            });
            pagina.Footer().AlignCenter().Text(t =>
            {
                t.Span("EduMonitor AI · página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        })).GeneratePdf();

    public byte[] Docx(DocumentoRefuerzo d)
    {
        using var memoria = new MemoryStream();
        using (var documento = WordprocessingDocument.Create(memoria, WordprocessingDocumentType.Document))
        {
            var principal = documento.AddMainDocumentPart();
            var cuerpo = new Body();
            cuerpo.Append(Parrafo("Plan de refuerzo", negrita: true, tamano: 40));
            cuerpo.Append(Parrafo($"{d.Curso} · {d.Actividad}", tamano: 24));
            cuerpo.Append(Parrafo($"Temas: {string.Join(", ", d.Temas)}"));
            cuerpo.Append(Parrafo($"Estudiantes: {string.Join(", ", d.Estudiantes)}"));
            cuerpo.Append(Parrafo($"Aprobado por el docente: {d.Aprobado.ToString("dd/MM/yyyy HH:mm", Ecuador)}"));
            foreach (var linea in Lineas(d.Texto))
            {
                cuerpo.Append(Parrafo(linea));
            }

            principal.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(cuerpo);
            principal.Document.Save();
        }

        return memoria.ToArray();
    }

    private static IEnumerable<string> Lineas(string texto) =>
        texto.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()).Select(l => l.Length == 0 ? " " : l);

    private static Paragraph Parrafo(string texto, bool negrita = false, int tamano = 22)
    {
        var propiedades = new RunProperties(new FontSize { Val = tamano.ToString(CultureInfo.InvariantCulture) });
        if (negrita)
        {
            propiedades.Append(new Bold());
        }

        return new Paragraph(new Run(propiedades, new Text(texto) { Space = SpaceProcessingModeValues.Preserve }));
    }
}
