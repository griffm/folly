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

        // Extract metadata from FO declarations and merge with options
        // Options metadata takes precedence over declarations
        ExtractAndMergeMetadata(document.Root.Declarations, options.Metadata);

        // Create layout options from PDF options
        var layoutOptions = new LayoutOptions
        {
            DefaultImageDpi = options.DefaultImageDpi,
            AllowAbsoluteImagePaths = true  // Allow absolute paths for PDF generation
        };

        // Build the area tree from the FO document
        var areaTree = document.BuildAreaTree(layoutOptions);

        // Get the bookmark tree (if any)
        var bookmarkTree = document.Root.BookmarkTree;

        // Render area tree to PDF
        using var renderer = new PdfRenderer(output, options);
        renderer.Render(areaTree, bookmarkTree);
    }

    /// <summary>
    /// Extracts metadata from FO declarations and merges with existing metadata.
    /// Existing metadata values take precedence over declarations.
    /// </summary>
    private static void ExtractAndMergeMetadata(FoDeclarations? declarations, PdfMetadata metadata)
    {
        if (declarations?.Info == null)
            return;

        var info = declarations.Info;

        // Only set values that are not already specified in metadata
        if (string.IsNullOrWhiteSpace(metadata.Title) && !string.IsNullOrWhiteSpace(info.Title))
            metadata.Title = info.Title;

        if (string.IsNullOrWhiteSpace(metadata.Author) && !string.IsNullOrWhiteSpace(info.Author))
            metadata.Author = info.Author;

        if (string.IsNullOrWhiteSpace(metadata.Subject) && !string.IsNullOrWhiteSpace(info.Subject))
            metadata.Subject = info.Subject;

        if (string.IsNullOrWhiteSpace(metadata.Keywords) && !string.IsNullOrWhiteSpace(info.Keywords))
            metadata.Keywords = info.Keywords;

        if (metadata.Creator == "Folly XSL-FO Processor" && !string.IsNullOrWhiteSpace(info.Creator))
            metadata.Creator = info.Creator;
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
