namespace Folly.Pdf;

/// <summary>
/// Extension methods for FoDocument to support PDF rendering.
/// </summary>
public static class FoDocumentExtensions
{
    /// <summary>
    /// Renders the document to PDF and writes to the specified stream.
    /// </summary>
    /// <param name="document">The FO document to render.</param>
    /// <param name="output">Stream to write the PDF to.</param>
    /// <param name="options">Optional PDF rendering options.</param>
    public static void SavePdf(this FoDocument document, Stream output, PdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);

        options ??= new PdfOptions();

        // Build the area tree from the FO document
        var areaTree = document.BuildAreaTree();

        // Render area tree to PDF
        using var renderer = new PdfRenderer(output, options);
        renderer.Render(areaTree);
    }

    /// <summary>
    /// Renders the document to PDF and writes to the specified file path.
    /// </summary>
    /// <param name="document">The FO document to render.</param>
    /// <param name="path">File path to write the PDF to.</param>
    /// <param name="options">Optional PDF rendering options.</param>
    public static void SavePdf(this FoDocument document, string path, PdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var stream = File.Create(path);
        document.SavePdf(stream, options);
    }
}
