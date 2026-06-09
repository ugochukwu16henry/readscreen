using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Readscreen.Core.Models;
using UglyToad.PdfPig;

namespace Readscreen.Memory;

public static class DocumentIngestService
{
    public static string ExtractText(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => ExtractPdf(filePath),
            ".docx" => ExtractDocx(filePath),
            ".pptx" => ExtractPptx(filePath),
            ".txt" or ".md" => File.ReadAllText(filePath, Encoding.UTF8),
            _ => throw new NotSupportedException($"Unsupported file type: {ext}")
        };
    }

    private static string ExtractPdf(string path)
    {
        var sb = new StringBuilder();
        using var document = PdfDocument.Open(path);
        foreach (var page in document.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static string ExtractDocx(string path)
    {
        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        return body?.InnerText ?? string.Empty;
    }

    private static string ExtractPptx(string path)
    {
        var sb = new StringBuilder();
        using var doc = PresentationDocument.Open(path, false);
        var slides = doc.PresentationPart?.SlideParts;
        if (slides == null)
            return string.Empty;

        foreach (var slide in slides)
        {
            if (slide.Slide == null) continue;
            var texts = slide.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (var t in texts)
                sb.AppendLine(t.Text);
        }
        return sb.ToString();
    }
}
