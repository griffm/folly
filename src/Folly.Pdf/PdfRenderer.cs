namespace Folly.Pdf;

/// <summary>
/// Renders an area tree to PDF 1.7 format.
/// </summary>
public sealed class PdfRenderer : IDisposable
{
    private readonly Stream _output;
    private readonly PdfOptions _options;
    private readonly PdfWriter _writer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new PDF renderer.
    /// </summary>
    /// <param name="output">Stream to write PDF output to.</param>
    /// <param name="options">PDF rendering options.</param>
    public PdfRenderer(Stream output, PdfOptions options)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _writer = new PdfWriter(_output);
    }

    /// <summary>
    /// Renders the area tree to PDF.
    /// </summary>
    /// <param name="areaTree">The area tree to render.</param>
    public void Render(AreaTree areaTree)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(areaTree);

        // TODO: Implement full PDF rendering
        // - Write PDF header
        // - Create document catalog
        // - Process fonts and create font descriptors
        // - Embed/subset fonts as needed
        // - Create pages
        // - Render each page from area tree
        // - Handle images (JPEG passthrough, PNG decode)
        // - Draw borders, backgrounds, graphics
        // - Place text with correct positioning
        // - Create bookmarks and links
        // - Write metadata
        // - Create cross-reference table
        // - Write trailer

        _writer.WriteHeader(_options.PdfVersion);

        // Collect fonts used in the document
        var fonts = CollectFonts(areaTree);

        var catalogId = _writer.WriteCatalog(areaTree.Pages.Count);

        // Write font resources
        var fontIds = _writer.WriteFonts(fonts);

        // Render pages
        var pageIds = new List<int>();
        foreach (var page in areaTree.Pages)
        {
            var pageId = RenderPage(page, fontIds);
            pageIds.Add(pageId);
        }

        // Update pages tree
        _writer.WritePages(catalogId + 1, pageIds, areaTree.Pages);

        _writer.WriteMetadata(_options.Metadata);
        _writer.WriteXRefAndTrailer(catalogId);
    }

    private HashSet<string> CollectFonts(AreaTree areaTree)
    {
        var fonts = new HashSet<string>();
        foreach (var page in areaTree.Pages)
        {
            CollectFontsFromAreas(page.Areas, fonts);
        }
        return fonts;
    }

    private void CollectFontsFromAreas(IEnumerable<Area> areas, HashSet<string> fonts)
    {
        foreach (var area in areas)
        {
            if (area is BlockArea blockArea)
            {
                fonts.Add(blockArea.FontFamily);
                CollectFontsFromAreas(blockArea.Children, fonts);
            }
            else if (area is LineArea lineArea)
            {
                foreach (var inline in lineArea.Inlines)
                {
                    fonts.Add(inline.FontFamily);
                }
            }
            else if (area is InlineArea inlineArea)
            {
                fonts.Add(inlineArea.FontFamily);
            }
        }
    }

    private int RenderPage(PageViewport page, Dictionary<string, int> fontIds)
    {
        // Build content stream
        var content = new StringBuilder();

        // Render all areas on the page
        foreach (var area in page.Areas)
        {
            RenderArea(area, content, fontIds);
        }

        return _writer.WritePage(page, content.ToString(), fontIds);
    }

    private void RenderArea(Area area, StringBuilder content, Dictionary<string, int> fontIds)
    {
        if (area is BlockArea blockArea)
        {
            // TODO: Render borders and backgrounds

            // Render child areas (lines)
            foreach (var child in blockArea.Children)
            {
                RenderArea(child, content, fontIds);
            }
        }
        else if (area is LineArea lineArea)
        {
            // Render inline areas (text)
            foreach (var inline in lineArea.Inlines)
            {
                RenderInline(inline, lineArea, content, fontIds);
            }
        }
    }

    private void RenderInline(InlineArea inline, LineArea line, StringBuilder content, Dictionary<string, int> fontIds)
    {
        if (string.IsNullOrEmpty(inline.Text))
            return;

        // Get font resource name
        if (!fontIds.TryGetValue(inline.FontFamily, out var fontId))
            return;

        // Calculate absolute position (line position + inline offset)
        var x = line.X + inline.X;
        var y = line.Y + inline.BaselineOffset;

        // PDF text positioning and rendering
        content.AppendLine("BT"); // Begin text
        content.AppendLine($"/F{fontId} {inline.FontSize:F2} Tf"); // Set font and size
        content.AppendLine($"{x:F2} {y:F2} Td"); // Position text
        content.AppendLine($"({EscapeString(inline.Text)}) Tj"); // Show text
        content.AppendLine("ET"); // End text
    }

    private static string EscapeString(string str)
    {
        return str
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// Disposes resources used by the PDF renderer.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _writer.Dispose();
            _disposed = true;
        }
    }
}
