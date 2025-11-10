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
    /// <param name="bookmarkTree">Optional bookmark tree for PDF outline.</param>
    public void Render(AreaTree areaTree, Dom.FoBookmarkTree? bookmarkTree = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(areaTree);

        // Write PDF header
        _writer.WriteHeader(_options.PdfVersion);

        // Collect fonts used in the document and track character usage
        var (fonts, characterUsage) = CollectFonts(areaTree);

        // Collect images used in the document
        var images = CollectImages(areaTree);

        var catalogId = _writer.WriteCatalog(areaTree.Pages.Count, bookmarkTree);

        // Write font resources with subsetting if enabled
        var fontIds = _writer.WriteFonts(fonts, characterUsage, _options.SubsetFonts);

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

    private (HashSet<string> Fonts, Dictionary<string, HashSet<char>> CharacterUsage) CollectFonts(AreaTree areaTree)
    {
        var fonts = new HashSet<string>();
        var characterUsage = new Dictionary<string, HashSet<char>>();

        foreach (var page in areaTree.Pages)
        {
            CollectFontsFromAreas(page.Areas, fonts, characterUsage);
        }

        return (fonts, characterUsage);
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

    private void CollectFontsFromAreas(IEnumerable<Area> areas, HashSet<string> fonts, Dictionary<string, HashSet<char>> characterUsage)
    {
        foreach (var area in areas)
        {
            if (area is BlockArea blockArea)
            {
                fonts.Add(blockArea.FontFamily);
                CollectFontsFromAreas(blockArea.Children, fonts, characterUsage);
            }
            else if (area is LineArea lineArea)
            {
                foreach (var inline in lineArea.Inlines)
                {
                    fonts.Add(inline.FontFamily);

                    // Track character usage
                    if (!string.IsNullOrEmpty(inline.Text))
                    {
                        if (!characterUsage.ContainsKey(inline.FontFamily))
                        {
                            characterUsage[inline.FontFamily] = new HashSet<char>();
                        }

                        foreach (var ch in inline.Text)
                        {
                            characterUsage[inline.FontFamily].Add(ch);
                        }
                    }
                }
            }
            else if (area is InlineArea inlineArea)
            {
                fonts.Add(inlineArea.FontFamily);

                // Track character usage
                if (!string.IsNullOrEmpty(inlineArea.Text))
                {
                    if (!characterUsage.ContainsKey(inlineArea.FontFamily))
                    {
                        characterUsage[inlineArea.FontFamily] = new HashSet<char>();
                    }

                    foreach (var ch in inlineArea.Text)
                    {
                        characterUsage[inlineArea.FontFamily].Add(ch);
                    }
                }
            }
            else if (area is LeaderArea leaderArea)
            {
                // Leaders may use fonts for dot patterns
                fonts.Add(leaderArea.FontFamily);

                // Track dot character usage
                if (leaderArea.LeaderPattern == "dots")
                {
                    if (!characterUsage.ContainsKey(leaderArea.FontFamily))
                    {
                        characterUsage[leaderArea.FontFamily] = new HashSet<char>();
                    }
                    characterUsage[leaderArea.FontFamily].Add('.');
                }
            }
            else if (area is TableArea tableArea)
            {
                foreach (var row in tableArea.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        CollectFontsFromAreas(cell.Children, fonts, characterUsage);
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

        return _writer.WritePage(page, content.ToString(), fontIds, imageIds, _options.CompressStreams);
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
        else if (area is LeaderArea leaderArea)
        {
            RenderLeader(leaderArea, content, fontIds, pageHeight, offsetX, offsetY);
        }
        else if (area is BlockArea blockArea)
        {
            // Render background
            if (blockArea.BackgroundColor != "transparent" && !string.IsNullOrWhiteSpace(blockArea.BackgroundColor))
            {
                RenderBackground(blockArea, content, pageHeight, offsetX, offsetY);
            }

            // Render borders (check both generic and directional borders)
            var hasGenericBorder = blockArea.BorderStyle != "none" && blockArea.BorderWidth > 0;
            var hasDirectionalBorder = (blockArea.BorderTopStyle != "none" && blockArea.BorderTopWidth > 0) ||
                                        (blockArea.BorderBottomStyle != "none" && blockArea.BorderBottomWidth > 0) ||
                                        (blockArea.BorderLeftStyle != "none" && blockArea.BorderLeftWidth > 0) ||
                                        (blockArea.BorderRightStyle != "none" && blockArea.BorderRightWidth > 0);

            if (hasGenericBorder || hasDirectionalBorder)
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

        // Render background color if specified
        if (!string.IsNullOrEmpty(inline.BackgroundColor) && inline.BackgroundColor != "transparent")
        {
            var (bgR, bgG, bgB) = ParseColor(inline.BackgroundColor);
            content.AppendLine("q");
            content.AppendLine($"{bgR:F3} {bgG:F3} {bgB:F3} rg");
            var bgY = pageHeight - (offsetY + line.Y);
            content.AppendLine($"{x:F2} {bgY - line.Height:F2} {inline.Width:F2} {line.Height:F2} re");
            content.AppendLine("f");
            content.AppendLine("Q");
        }

        // PDF text positioning and rendering
        // Use Tm (set text matrix) for absolute positioning instead of Td (relative positioning)
        content.AppendLine("BT"); // Begin text
        content.AppendLine($"/F{fontId} {inline.FontSize:F2} Tf"); // Set font and size

        // Set text color if specified
        if (!string.IsNullOrEmpty(inline.Color))
        {
            var (r, g, b) = ParseColor(inline.Color);
            content.AppendLine($"{r:F3} {g:F3} {b:F3} rg"); // Set text color
        }

        content.AppendLine($"1 0 0 1 {x:F2} {pdfY:F2} Tm"); // Set text matrix (absolute position)

        // Get character remapping for this font (if subsetting is enabled)
        var remapping = _writer.GetCharacterRemapping(inline.FontFamily);
        content.AppendLine($"({EscapeAndRemapString(inline.Text, remapping)}) Tj"); // Show text
        content.AppendLine("ET"); // End text

        // Render text decoration (underline, overline, line-through)
        if (!string.IsNullOrEmpty(inline.TextDecoration) && inline.TextDecoration != "none")
        {
            var (decorR, decorG, decorB) = !string.IsNullOrEmpty(inline.Color)
                ? ParseColor(inline.Color)
                : (0.0, 0.0, 0.0); // Default to black

            content.AppendLine("q");
            content.AppendLine($"{decorR:F3} {decorG:F3} {decorB:F3} RG"); // Set stroke color
            content.AppendLine("0.5 w"); // Set line width

            var decorationY = inline.TextDecoration switch
            {
                "underline" => pdfY - 2, // Below baseline
                "overline" => pdfY + inline.FontSize, // Above text
                "line-through" => pdfY + (inline.FontSize * 0.3), // Middle of text
                _ => pdfY - 2
            };

            content.AppendLine($"{x:F2} {decorationY:F2} m"); // Move to start
            content.AppendLine($"{x + inline.Width:F2} {decorationY:F2} l"); // Line to end
            content.AppendLine("S"); // Stroke
            content.AppendLine("Q");
        }
    }

    private void RenderLeader(LeaderArea leader, StringBuilder content, Dictionary<string, int> fontIds, double pageHeight, double offsetX, double offsetY)
    {
        // Calculate absolute position
        var x = offsetX + leader.X;
        var y = offsetY + leader.Y + leader.BaselineOffset;

        // Convert Y coordinate from top-down to PDF's bottom-up coordinate system
        var pdfY = pageHeight - y;

        // Parse leader color
        var (r, g, b) = ParseColor(leader.Color);

        // Save graphics state
        content.AppendLine("q");
        content.AppendLine($"{r:F3} {g:F3} {b:F3} RG"); // Set stroke color
        content.AppendLine($"{r:F3} {g:F3} {b:F3} rg"); // Set fill color

        switch (leader.LeaderPattern)
        {
            case "dots":
                // Render dots pattern using text
                if (fontIds.TryGetValue(leader.FontFamily, out var fontId))
                {
                    content.AppendLine("BT");
                    content.AppendLine($"/F{fontId} {leader.FontSize:F2} Tf");

                    // Calculate number of dots that fit
                    var patternWidth = leader.LeaderPatternWidth;
                    var numDots = (int)(leader.Width / patternWidth);

                    // Render dots with spacing
                    for (int i = 0; i < numDots; i++)
                    {
                        var dotX = x + (i * patternWidth);
                        content.AppendLine($"1 0 0 1 {dotX:F2} {pdfY:F2} Tm");
                        content.AppendLine("(.) Tj");
                    }

                    content.AppendLine("ET");
                }
                break;

            case "rule":
                // Render solid or styled line
                var lineY = pdfY - (leader.RuleThickness / 2);
                content.AppendLine($"{leader.RuleThickness:F2} w");

                // Set dash pattern based on rule style
                switch (leader.RuleStyle)
                {
                    case "dashed":
                        content.AppendLine("[3 2] 0 d");
                        break;
                    case "dotted":
                        content.AppendLine("[1 1] 0 d");
                        break;
                    case "solid":
                    default:
                        content.AppendLine("[] 0 d");
                        break;
                }

                content.AppendLine($"{x:F2} {lineY:F2} m");
                content.AppendLine($"{x + leader.Width:F2} {lineY:F2} l");
                content.AppendLine("S");
                break;

            case "space":
            default:
                // Space pattern - render nothing (just empty space)
                break;
        }

        // Restore graphics state
        content.AppendLine("Q");
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
        // Calculate absolute position
        var x = offsetX + block.X;
        var y = offsetY + block.Y;
        var pdfY = pageHeight - y - block.Height;

        // Check if we should use directional borders or generic border
        var hasDirectionalBorders = block.BorderTopStyle != "none" || block.BorderBottomStyle != "none" ||
                                     block.BorderLeftStyle != "none" || block.BorderRightStyle != "none";

        if (hasDirectionalBorders)
        {
            // Render each border side independently
            // Top border (border-before in XSL-FO)
            if (block.BorderTopStyle != "none" && block.BorderTopWidth > 0)
            {
                RenderBorderSide(content, block.BorderTopWidth, block.BorderTopColor, block.BorderTopStyle,
                    x, pdfY + block.Height, x + block.Width, pdfY + block.Height);
            }

            // Bottom border (border-after in XSL-FO)
            if (block.BorderBottomStyle != "none" && block.BorderBottomWidth > 0)
            {
                RenderBorderSide(content, block.BorderBottomWidth, block.BorderBottomColor, block.BorderBottomStyle,
                    x, pdfY, x + block.Width, pdfY);
            }

            // Left border (border-start in XSL-FO)
            if (block.BorderLeftStyle != "none" && block.BorderLeftWidth > 0)
            {
                RenderBorderSide(content, block.BorderLeftWidth, block.BorderLeftColor, block.BorderLeftStyle,
                    x, pdfY, x, pdfY + block.Height);
            }

            // Right border (border-end in XSL-FO)
            if (block.BorderRightStyle != "none" && block.BorderRightWidth > 0)
            {
                RenderBorderSide(content, block.BorderRightWidth, block.BorderRightColor, block.BorderRightStyle,
                    x + block.Width, pdfY, x + block.Width, pdfY + block.Height);
            }
        }
        else
        {
            // Fall back to generic border (draw complete rectangle)
            var (r, g, b) = ParseColor(block.BorderColor);

            content.AppendLine("q");
            content.AppendLine($"{r:F3} {g:F3} {b:F3} RG");
            content.AppendLine($"{block.BorderWidth:F2} w");

            switch (block.BorderStyle.ToLowerInvariant())
            {
                case "dashed":
                    content.AppendLine("[3 2] 0 d");
                    break;
                case "dotted":
                    content.AppendLine("[1 1] 0 d");
                    break;
                default:
                    content.AppendLine("[] 0 d");
                    break;
            }

            var halfWidth = block.BorderWidth / 2;
            content.AppendLine($"{x + halfWidth:F2} {pdfY + halfWidth:F2} {block.Width - block.BorderWidth:F2} {block.Height - block.BorderWidth:F2} re");
            content.AppendLine("S");
            content.AppendLine("Q");
        }
    }

    private void RenderBorderSide(StringBuilder content, double width, string color, string style, double x1, double y1, double x2, double y2)
    {
        var (r, g, b) = ParseColor(color);

        content.AppendLine("q");
        content.AppendLine($"{r:F3} {g:F3} {b:F3} RG");
        content.AppendLine($"{width:F2} w");

        switch (style.ToLowerInvariant())
        {
            case "dashed":
                content.AppendLine("[3 2] 0 d");
                break;
            case "dotted":
                content.AppendLine("[1 1] 0 d");
                break;
            default:
                content.AppendLine("[] 0 d");
                break;
        }

        content.AppendLine($"{x1:F2} {y1:F2} m");
        content.AppendLine($"{x2:F2} {y2:F2} l");
        content.AppendLine("S");
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
    /// Escapes a string for PDF and applies character remapping for high-Unicode characters.
    /// </summary>
    private static string EscapeAndRemapString(string str, Dictionary<char, byte>? remapping)
    {
        if (remapping == null || remapping.Count == 0)
        {
            return EscapeString(str);
        }

        var result = new StringBuilder(str.Length);
        foreach (var ch in str)
        {
            // Apply remapping if available
            char outputChar = ch;
            if (remapping.TryGetValue(ch, out var remappedByte))
            {
                outputChar = (char)remappedByte;
            }

            // Escape special PDF characters
            switch (outputChar)
            {
                case '\\':
                    result.Append("\\\\");
                    break;
                case '(':
                    result.Append("\\(");
                    break;
                case ')':
                    result.Append("\\)");
                    break;
                case '\r':
                    result.Append("\\r");
                    break;
                case '\n':
                    result.Append("\\n");
                    break;
                case '\t':
                    result.Append("\\t");
                    break;
                default:
                    result.Append(outputChar);
                    break;
            }
        }
        return result.ToString();
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
