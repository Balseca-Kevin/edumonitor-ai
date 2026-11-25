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
                c.Item().Text(d.Titulo).FontSize(20).Bold();
                c.Item().Text($"{d.Curso} · {d.Actividad}").FontSize(12).FontColor(Colors.Grey.Darken2);
            });
            pagina.Content().PaddingVertical(12).Column(c =>
            {
                c.Spacing(8);
                c.Item().Text(t =>
                {
                    t.Span("Temas: ").Bold();
                    t.Span(d.Temas.Count == 0 ? "todos los de la actividad" : string.Join(", ", d.Temas));
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
            cuerpo.Append(Parrafo(d.Titulo, negrita: true, tamano: 40));
            cuerpo.Append(Parrafo($"{d.Curso} · {d.Actividad}", tamano: 24));
            cuerpo.Append(Parrafo($"Temas: {(d.Temas.Count == 0 ? "todos los de la actividad" : string.Join(", ", d.Temas))}"));
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

    public byte[] Informe(DocumentoInforme d) =>
        QuestPDF.Fluent.Document.Create(contenedor => contenedor.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(2, Unit.Centimetre);
            pagina.DefaultTextStyle(t => t.FontSize(11));
            pagina.Header().Column(c =>
            {
                c.Item().Text("Informe del curso").FontSize(20).Bold();
                c.Item().Text(d.Curso).FontSize(12).FontColor(Colors.Grey.Darken2);
                c.Item().Text($"{d.Periodo} · datos de la sincronización del {d.Sincronizacion.ToString("dd/MM/yyyy HH:mm", Ecuador)} ({d.EstadoSincronizacion})")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);
            });
            pagina.Content().PaddingVertical(12).Column(c =>
            {
                c.Spacing(8);
                c.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(columnas =>
                    {
                        columnas.RelativeColumn(3);
                        columnas.RelativeColumn(2);
                    });
                    foreach (var (etiqueta, valor) in d.Cifras)
                    {
                        tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).Text(etiqueta);
                        tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).AlignRight().Text(valor).Bold();
                    }
                });
                c.Item().PaddingTop(8).Text("Resumen del asistente").FontSize(14).Bold();
                c.Item().Text(t =>
                {
                    t.Span("Pregunta: ").Bold();
                    t.Span(d.Pregunta);
                });
                c.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                foreach (var linea in Lineas(d.Resumen))
                {
                    c.Item().Text(linea);
                }

                c.Item().PaddingTop(6).Text(d.Respondio).FontSize(9).FontColor(Colors.Grey.Darken1);
            });
            pagina.Footer().AlignCenter().Text(t =>
            {
                t.Span($"EduMonitor AI · generado el {d.Generado.ToString("dd/MM/yyyy HH:mm", Ecuador)} · página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        })).GeneratePdf();

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
