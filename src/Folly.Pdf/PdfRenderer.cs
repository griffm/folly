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

        // Collect images used in the document
        var images = CollectImages(areaTree);

        var catalogId = _writer.WriteCatalog(areaTree.Pages.Count);

        // Write font resources
        var fontIds = _writer.WriteFonts(fonts);

        // Write image resources
        var imageIds = _writer.WriteImages(images);

        // Render pages
        var pageIds = new List<int>();
        foreach (var page in areaTree.Pages)
        {
            var pageId = RenderPage(page, fontIds, imageIds);
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

    private Dictionary<string, (byte[] Data, string Format, int Width, int Height)> CollectImages(AreaTree areaTree)
    {
        var images = new Dictionary<string, (byte[], string, int, int)>();
        foreach (var page in areaTree.Pages)
        {
            CollectImagesFromAreas(page.Areas, images);
        }
        return images;
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
            else if (area is TableArea tableArea)
            {
                foreach (var row in tableArea.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        CollectFontsFromAreas(cell.Children, fonts);
                    }
                }
            }
        }
    }

    private void CollectImagesFromAreas(IEnumerable<Area> areas, Dictionary<string, (byte[], string, int, int)> images)
    {
        foreach (var area in areas)
        {
            if (area is ImageArea imageArea)
            {
                if (imageArea.ImageData != null && !string.IsNullOrEmpty(imageArea.Source))
                {
                    if (!images.ContainsKey(imageArea.Source))
                    {
                        images[imageArea.Source] = (
                            imageArea.ImageData,
                            imageArea.Format,
                            (int)imageArea.IntrinsicWidth,
                            (int)imageArea.IntrinsicHeight
                        );
                    }
                }
            }
            else if (area is BlockArea blockArea)
            {
                CollectImagesFromAreas(blockArea.Children, images);
            }
            else if (area is TableArea tableArea)
            {
                foreach (var row in tableArea.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        CollectImagesFromAreas(cell.Children, images);
                    }
                }
            }
        }
    }

    private int RenderPage(PageViewport page, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds)
    {
        // Build content stream
        var content = new StringBuilder();

        // Render all areas on the page
        // Pass page height for coordinate conversion (PDF uses bottom-up coordinates)
        foreach (var area in page.Areas)
        {
            RenderArea(area, content, fontIds, imageIds, page.Height);
        }

        return _writer.WritePage(page, content.ToString(), fontIds, imageIds);
    }

    private void RenderArea(Area area, StringBuilder content, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, double pageHeight, double offsetX = 0, double offsetY = 0)
    {
        if (area is ImageArea imageArea)
        {
            RenderImage(imageArea, content, imageIds, pageHeight, offsetX, offsetY);
        }
        else if (area is TableArea tableArea)
        {
            RenderTable(tableArea, content, fontIds, imageIds, pageHeight);
        }
        else if (area is BlockArea blockArea)
        {
            // Render background
            if (blockArea.BackgroundColor != "transparent" && !string.IsNullOrWhiteSpace(blockArea.BackgroundColor))
            {
                RenderBackground(blockArea, content, pageHeight, offsetX, offsetY);
            }

            // Render borders
            if (blockArea.BorderStyle != "none" && blockArea.BorderWidth > 0)
            {
                RenderBorder(blockArea, content, pageHeight, offsetX, offsetY);
            }

            // Render child areas (lines and images) with offset
            // Children have coordinates relative to the block, so we offset them by the block's absolute position
            foreach (var child in blockArea.Children)
            {
                RenderArea(child, content, fontIds, imageIds, pageHeight, offsetX + blockArea.X, offsetY + blockArea.Y);
            }
        }
        else if (area is LineArea lineArea)
        {
            // Render inline areas (text)
            foreach (var inline in lineArea.Inlines)
            {
                RenderInline(inline, lineArea, content, fontIds, pageHeight, offsetX, offsetY);
            }
        }
    }

    private void RenderInline(InlineArea inline, LineArea line, StringBuilder content, Dictionary<string, int> fontIds, double pageHeight, double offsetX, double offsetY)
    {
        if (string.IsNullOrEmpty(inline.Text))
            return;

        // Get font resource name
        if (!fontIds.TryGetValue(inline.FontFamily, out var fontId))
            return;

        // Calculate absolute position (parent offset + line position + inline offset)
        var x = offsetX + line.X + inline.X;
        var y = offsetY + line.Y + inline.BaselineOffset;

        // Convert Y coordinate from top-down to PDF's bottom-up coordinate system
        var pdfY = pageHeight - y;

        // PDF text positioning and rendering
        // Use Tm (set text matrix) for absolute positioning instead of Td (relative positioning)
        content.AppendLine("BT"); // Begin text
        content.AppendLine($"/F{fontId} {inline.FontSize:F2} Tf"); // Set font and size
        content.AppendLine($"1 0 0 1 {x:F2} {pdfY:F2} Tm"); // Set text matrix (absolute position)
        content.AppendLine($"({EscapeString(inline.Text)}) Tj"); // Show text
        content.AppendLine("ET"); // End text
    }

    private void RenderBackground(BlockArea block, StringBuilder content, double pageHeight, double offsetX, double offsetY)
    {
        // Parse color and convert to RGB
        var (r, g, b) = ParseColor(block.BackgroundColor);

        // Save graphics state
        content.AppendLine("q");

        // Set fill color
        content.AppendLine($"{r:F3} {g:F3} {b:F3} rg");

        // Calculate absolute position
        var x = offsetX + block.X;
        var y = offsetY + block.Y;

        // Convert Y coordinate from top-down to PDF's bottom-up coordinate system
        var pdfY = pageHeight - y - block.Height;

        // Draw filled rectangle
        content.AppendLine($"{x:F2} {pdfY:F2} {block.Width:F2} {block.Height:F2} re");
        content.AppendLine("f");

        // Restore graphics state
        content.AppendLine("Q");
    }

    private void RenderBorder(BlockArea block, StringBuilder content, double pageHeight, double offsetX, double offsetY)
    {
        // Parse border color and convert to RGB
        var (r, g, b) = ParseColor(block.BorderColor);

        // Save graphics state
        content.AppendLine("q");

        // Set stroke color and width
        content.AppendLine($"{r:F3} {g:F3} {b:F3} RG");
        content.AppendLine($"{block.BorderWidth:F2} w");

        // Set line dash pattern based on border style
        switch (block.BorderStyle.ToLowerInvariant())
        {
            case "dashed":
                content.AppendLine("[3 2] 0 d"); // Dashed pattern
                break;
            case "dotted":
                content.AppendLine("[1 1] 0 d"); // Dotted pattern
                break;
            default: // solid
                content.AppendLine("[] 0 d"); // Solid line
                break;
        }

        // Calculate absolute position
        var x = offsetX + block.X;
        var y = offsetY + block.Y;

        // Convert Y coordinate from top-down to PDF's bottom-up coordinate system
        var pdfY = pageHeight - y - block.Height;

        // Draw rectangle border (accounting for half the line width)
        var halfWidth = block.BorderWidth / 2;
        content.AppendLine($"{x + halfWidth:F2} {pdfY + halfWidth:F2} {block.Width - block.BorderWidth:F2} {block.Height - block.BorderWidth:F2} re");
        content.AppendLine("S");

        // Restore graphics state
        content.AppendLine("Q");
    }

    private static (double r, double g, double b) ParseColor(string color)
    {
        // Handle common color names
        var normalizedColor = color.ToLowerInvariant().Trim();

        return normalizedColor switch
        {
            "black" => (0, 0, 0),
            "white" => (1, 1, 1),
            "red" => (1, 0, 0),
            "green" => (0, 1, 0),
            "blue" => (0, 0, 1),
            "yellow" => (1, 1, 0),
            "cyan" => (0, 1, 1),
            "magenta" => (1, 0, 1),
            "gray" or "grey" => (0.5, 0.5, 0.5),
            "silver" => (0.75, 0.75, 0.75),
            _ => ParseHexColor(normalizedColor)
        };
    }

    private static (double r, double g, double b) ParseHexColor(string color)
    {
        // Handle hex colors (#RGB or #RRGGBB)
        if (color.StartsWith('#'))
        {
            var hex = color[1..];

            if (hex.Length == 3)
            {
                // #RGB format - expand to #RRGGBB
                var r = Convert.ToInt32(hex[0].ToString() + hex[0].ToString(), 16) / 255.0;
                var g = Convert.ToInt32(hex[1].ToString() + hex[1].ToString(), 16) / 255.0;
                var b = Convert.ToInt32(hex[2].ToString() + hex[2].ToString(), 16) / 255.0;
                return (r, g, b);
            }
            else if (hex.Length == 6)
            {
                // #RRGGBB format
                var r = Convert.ToInt32(hex[0..2], 16) / 255.0;
                var g = Convert.ToInt32(hex[2..4], 16) / 255.0;
                var b = Convert.ToInt32(hex[4..6], 16) / 255.0;
                return (r, g, b);
            }
        }

        // Default to black if unable to parse
        return (0, 0, 0);
    }

    private void RenderImage(ImageArea image, StringBuilder content, Dictionary<string, int> imageIds, double pageHeight, double offsetX, double offsetY)
    {
        // Look up the image ID
        if (!imageIds.TryGetValue(image.Source, out var imageId))
            return; // Image not found

        // Save graphics state
        content.AppendLine("q");

        // Calculate absolute position
        var x = offsetX + image.X;
        var y = offsetY + image.Y;

        // Convert Y coordinate from top-down to PDF's bottom-up coordinate system
        var pdfY = pageHeight - y - image.Height;

        // Translate to image position and scale to image dimensions
        // PDF images are drawn in a 1x1 unit square, so we scale and translate
        content.AppendLine($"{image.Width:F2} 0 0 {image.Height:F2} {x:F2} {pdfY:F2} cm");

        // Draw the image using Do operator
        content.AppendLine($"/Im{imageId} Do");

        // Restore graphics state
        content.AppendLine("Q");
    }

    private void RenderTable(TableArea table, StringBuilder content, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, double pageHeight)
    {
        // Render each row in the table
        foreach (var row in table.Rows)
        {
            RenderTableRow(row, table, content, fontIds, imageIds, pageHeight);
        }
    }

    private void RenderTableRow(TableRowArea row, TableArea table, StringBuilder content, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, double pageHeight)
    {
        // Render each cell in the row
        foreach (var cell in row.Cells)
        {
            RenderTableCell(cell, table, row, content, fontIds, imageIds, pageHeight);
        }
    }

    private void RenderTableCell(TableCellArea cell, TableArea table, TableRowArea row, StringBuilder content, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, double pageHeight)
    {
        // Calculate absolute position
        var absoluteX = table.X + cell.X;
        var absoluteY = table.Y + row.Y + cell.Y;

        // Convert Y coordinate from top-down to PDF's bottom-up coordinate system
        var pdfY = pageHeight - absoluteY - cell.Height;

        // Render cell background
        if (cell.BackgroundColor != "transparent" && !string.IsNullOrWhiteSpace(cell.BackgroundColor))
        {
            var (r, g, b) = ParseColor(cell.BackgroundColor);

            content.AppendLine("q");
            content.AppendLine($"{r:F3} {g:F3} {b:F3} rg");
            content.AppendLine($"{absoluteX:F2} {pdfY:F2} {cell.Width:F2} {cell.Height:F2} re");
            content.AppendLine("f");
            content.AppendLine("Q");
        }

        // Render cell border
        if (cell.BorderStyle != "none" && cell.BorderWidth > 0)
        {
            var (r, g, b) = ParseColor(cell.BorderColor);

            content.AppendLine("q");
            content.AppendLine($"{r:F3} {g:F3} {b:F3} RG");
            content.AppendLine($"{cell.BorderWidth:F2} w");

            // Set line dash pattern
            switch (cell.BorderStyle.ToLowerInvariant())
            {
                case "dashed":
                    content.AppendLine("[3 2] 0 d");
                    break;
                case "dotted":
                    content.AppendLine("[1 1] 0 d");
                    break;
                default: // solid
                    content.AppendLine("[] 0 d");
                    break;
            }

            var halfWidth = cell.BorderWidth / 2;
            content.AppendLine($"{absoluteX + halfWidth:F2} {pdfY + halfWidth:F2} {cell.Width - cell.BorderWidth:F2} {cell.Height - cell.BorderWidth:F2} re");
            content.AppendLine("S");
            content.AppendLine("Q");
        }

        // Render cell content (blocks)
        // For table cells, we need to handle coordinate conversion differently
        // We'll render children with absolute coordinates, not using PDF transformation matrix
        foreach (var child in cell.Children)
        {
            // Create a temporary copy of the area with adjusted coordinates
            if (child is BlockArea blockArea)
            {
                var originalX = blockArea.X;
                var originalY = blockArea.Y;

                // Adjust to absolute position
                blockArea.X = absoluteX + originalX;
                blockArea.Y = absoluteY + originalY;

                RenderArea(child, content, fontIds, imageIds, pageHeight);

                // Restore original position
                blockArea.X = originalX;
                blockArea.Y = originalY;
            }
            else
            {
                RenderArea(child, content, fontIds, imageIds, pageHeight);
            }
        }
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
