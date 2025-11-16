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

    // Cache of loaded TrueType fonts for kerning support
    private readonly Dictionary<string, Fonts.Models.FontFile?> _loadedFonts = new();

    /// <summary>
    /// Initializes a new PDF renderer.
    /// </summary>
    /// <param name="output">Stream to write PDF output to.</param>
    /// <param name="options">PDF rendering options.</param>
    public PdfRenderer(Stream output, PdfOptions options)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _writer = new PdfWriter(_output, _options);
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

        // Create structure tree if tagged PDF is enabled
        PdfStructureTree? structureTree = null;
        if (_options.EnableTaggedPdf)
        {
            structureTree = new PdfStructureTree();
        }

        // Collect fonts used in the document and track character usage
        var (fonts, characterUsage) = CollectFonts(areaTree);

        // Load TrueType fonts for kerning support
        LoadFontsForKerning(fonts);

        // Collect images used in the document
        var images = CollectImages(areaTree);

        // Write font resources with subsetting if enabled
        var fontIds = _writer.WriteFonts(fonts, characterUsage, _options.SubsetFonts, _options.TrueTypeFonts, _options.EnableFontFallback);

        // Write image resources
        var imageIds = _writer.WriteImages(images);

        // Render pages and build structure tree
        var pageIds = new List<int>();
        for (int pageIndex = 0; pageIndex < areaTree.Pages.Count; pageIndex++)
        {
            var page = areaTree.Pages[pageIndex];
            var pageId = RenderPage(page, fontIds, imageIds, structureTree, pageIndex);
            pageIds.Add(pageId);
        }

        // Write structure tree if enabled (must be written before catalog)
        int structTreeRootId = 0;
        if (structureTree != null)
        {
            structTreeRootId = structureTree.WriteToPdf(_writer, pageIds.ToArray());
        }

        // Write catalog with structure tree reference
        var catalogId = _writer.WriteCatalog(areaTree.Pages.Count, bookmarkTree, structTreeRootId);

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

            // Collect fonts from absolutely positioned areas
            foreach (var absoluteArea in page.AbsoluteAreas)
            {
                CollectFontsFromAbsoluteArea(absoluteArea, fonts, characterUsage);
            }
        }

        return (fonts, characterUsage);
    }

    private Dictionary<string, (byte[] Data, string Format, int Width, int Height)> CollectImages(AreaTree areaTree)
    {
        var images = new Dictionary<string, (byte[], string, int, int)>();
        foreach (var page in areaTree.Pages)
        {
            CollectImagesFromAreas(page.Areas, images);

            // Collect images from absolutely positioned areas
            foreach (var absoluteArea in page.AbsoluteAreas)
            {
                CollectImagesFromAbsoluteArea(absoluteArea, images);
            }
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
                // Collect background image if present
                if (blockArea.BackgroundImageData != null && !string.IsNullOrEmpty(blockArea.BackgroundImage))
                {
                    var imageKey = $"{blockArea.BackgroundImage}_{blockArea.BackgroundImageData.GetHashCode()}";
                    if (!images.ContainsKey(imageKey))
                    {
                        images[imageKey] = (
                            blockArea.BackgroundImageData,
                            blockArea.BackgroundImageFormat ?? "UNKNOWN",
                            (int)blockArea.BackgroundImageWidth,
                            (int)blockArea.BackgroundImageHeight
                        );
                    }
                }

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

    private void CollectFontsFromAbsoluteArea(AbsolutePositionedArea absoluteArea,
        HashSet<string> fonts, Dictionary<string, HashSet<char>> characterUsage)
    {
        // Recursively collect fonts from child areas
        CollectFontsFromAreas(absoluteArea.Children, fonts, characterUsage);
    }

    private void CollectImagesFromAbsoluteArea(AbsolutePositionedArea absoluteArea,
        Dictionary<string, (byte[], string, int, int)> images)
    {
        // Collect background image if present
        if (absoluteArea.BackgroundImageData != null && !string.IsNullOrEmpty(absoluteArea.BackgroundImage))
        {
            var imageKey = $"{absoluteArea.BackgroundImage}_{absoluteArea.BackgroundImageData.GetHashCode()}";
            if (!images.ContainsKey(imageKey))
            {
                images[imageKey] = (
                    absoluteArea.BackgroundImageData,
                    absoluteArea.BackgroundImageFormat ?? "UNKNOWN",
                    (int)absoluteArea.BackgroundImageWidth,
                    (int)absoluteArea.BackgroundImageHeight
                );
            }
        }

        // Recursively collect images from child areas
        CollectImagesFromAreas(absoluteArea.Children, images);
    }

    /// <summary>
    /// Loads TrueType fonts for kerning support.
    /// Fonts are cached for the lifetime of the renderer.
    /// </summary>
    private void LoadFontsForKerning(HashSet<string> fontNames)
    {
        foreach (var fontName in fontNames)
        {
            // Skip if already loaded
            if (_loadedFonts.ContainsKey(fontName))
                continue;

            // Try to load TrueType font from options
            if (_options.TrueTypeFonts.TryGetValue(fontName, out var fontPath))
            {
                try
                {
                    var font = Fonts.FontParser.Parse(fontPath);
                    _loadedFonts[fontName] = font;
                }
                catch
                {
                    // If loading fails, store null (will fall back to Standard fonts without kerning)
                    _loadedFonts[fontName] = null;
                }
            }
            else if (_options.EnableFontFallback)
            {
                // Try to resolve font via system font discovery
                var resolver = new Fonts.FontResolver(_options.TrueTypeFonts, _options.FontCacheOptions);
                var resolvedPath = resolver.ResolveFontFamily(fontName);
                if (resolvedPath != null)
                {
                    try
                    {
                        var font = Fonts.FontParser.Parse(resolvedPath);
                        _loadedFonts[fontName] = font;
                    }
                    catch
                    {
                        _loadedFonts[fontName] = null;
                    }
                }
                else
                {
                    _loadedFonts[fontName] = null;
                }
            }
            else
            {
                // No TrueType font available, use Standard fonts without kerning
                _loadedFonts[fontName] = null;
            }
        }
    }

    private int RenderPage(PageViewport page, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, PdfStructureTree? structureTree, int pageIndex)
    {
        // Create Document structure element if this is the first page and tagged PDF is enabled
        StructureElement? documentElement = null;
        if (structureTree != null && pageIndex == 0)
        {
            documentElement = structureTree.CreateElement(StructureRole.Document);
        }

        // Build content stream
        var content = new StringBuilder();

        // Render all areas on the page (normal flow)
        // Pass page height for coordinate conversion (PDF uses bottom-up coordinates)
        foreach (var area in page.Areas)
        {
            RenderArea(area, content, fontIds, imageIds, page.Height, structureTree, documentElement, pageIndex);
        }

        // Render absolutely positioned areas (after normal flow, sorted by z-index)
        // Lower z-index values are rendered first (appear behind higher z-index)
        var sortedAbsoluteAreas = page.AbsoluteAreas
            .OrderBy(a => a.ZIndex)
            .ToList();

        foreach (var absoluteArea in sortedAbsoluteAreas)
        {
            RenderAbsoluteArea(absoluteArea, content, fontIds, imageIds, page.Height, structureTree, documentElement, pageIndex);
        }

        return _writer.WritePage(page, content.ToString(), fontIds, imageIds, _options.CompressStreams);
    }

    private void RenderArea(Area area, StringBuilder content, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, double pageHeight, PdfStructureTree? structureTree, StructureElement? parentElement, int pageIndex, double offsetX = 0, double offsetY = 0)
    {
        if (area is ImageArea imageArea)
        {
            RenderImage(imageArea, content, imageIds, pageHeight, offsetX, offsetY, structureTree, parentElement, pageIndex);
        }
        else if (area is SvgArea svgArea)
        {
            RenderSvg(svgArea, content, pageHeight, offsetX, offsetY);
        }
        else if (area is TableArea tableArea)
        {
            RenderTable(tableArea, content, fontIds, imageIds, pageHeight, structureTree, parentElement, pageIndex);
        }
        else if (area is LeaderArea leaderArea)
        {
            RenderLeader(leaderArea, content, fontIds, pageHeight, offsetX, offsetY);
        }
        else if (area is BlockArea blockArea)
        {
            // Create structure element for this block if tagging is enabled
            StructureElement? blockElement = null;
            int mcid = -1;
            StructureRole role = StructureRole.Paragraph; // Default role

            if (structureTree != null && parentElement != null)
            {
                // Detect structure role based on block properties
                role = DetectStructureRole(blockArea);

                blockElement = structureTree.CreateElement(role, parentElement);
                mcid = structureTree.RegisterMarkedContent(blockElement, pageIndex);

                // Begin marked content with appropriate tag
                var roleTag = GetRoleTag(role);
                content.AppendLine($"/{roleTag} <</MCID {mcid}>> BDC");
            }

            // Render background color
            if (blockArea.BackgroundColor != "transparent" && !string.IsNullOrWhiteSpace(blockArea.BackgroundColor))
            {
                RenderBackground(blockArea, content, pageHeight, offsetX, offsetY);
            }

            // Render background image
            if (blockArea.BackgroundImageData != null && !string.IsNullOrWhiteSpace(blockArea.BackgroundImageFormat))
            {
                RenderBackgroundImage(blockArea, content, imageIds, pageHeight, offsetX, offsetY);
            }

            // Render borders (check both generic and directional borders, plus border-radius)
            var hasGenericBorder = blockArea.BorderStyle != "none" && blockArea.BorderWidth > 0;
            var hasDirectionalBorder = (blockArea.BorderTopStyle != "none" && blockArea.BorderTopWidth > 0) ||
                                        (blockArea.BorderBottomStyle != "none" && blockArea.BorderBottomWidth > 0) ||
                                        (blockArea.BorderLeftStyle != "none" && blockArea.BorderLeftWidth > 0) ||
                                        (blockArea.BorderRightStyle != "none" && blockArea.BorderRightWidth > 0);
            var hasBorderRadius = blockArea.BorderTopLeftRadius > 0 || blockArea.BorderTopRightRadius > 0 ||
                                  blockArea.BorderBottomLeftRadius > 0 || blockArea.BorderBottomRightRadius > 0;

            if (hasGenericBorder || hasDirectionalBorder || hasBorderRadius)
            {
                RenderBorder(blockArea, content, pageHeight, offsetX, offsetY);
            }

            // Render child areas (lines and images) with offset
            // Children have coordinates relative to the block, so we offset them by the block's absolute position
            // For list structures (List, ListItem, ListLabel, ListBody), pass the structure element to children
            // so they can be nested in the structure tree. For regular blocks, pass null so child lines
            // are part of the parent block's content.
            var isListStructure = role == StructureRole.List ||
                                  role == StructureRole.ListItem ||
                                  role == StructureRole.ListLabel ||
                                  role == StructureRole.ListBody;
            var childParent = isListStructure ? blockElement : null;

            foreach (var child in blockArea.Children)
            {
                RenderArea(child, content, fontIds, imageIds, pageHeight, structureTree, childParent, pageIndex, offsetX + blockArea.X, offsetY + blockArea.Y);
            }

            // End marked content
            if (mcid >= 0)
            {
                content.AppendLine("EMC");
            }
        }
        else if (area is LineArea lineArea)
        {
            // Render inline areas (text)
            // Lines are not tagged separately - they're part of the parent block's marked content
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

        // Always set word spacing (Tw) to prevent it from persisting from previous text
        // In PDF, Tw remains active until explicitly changed, so we must set it even when 0
        content.AppendLine($"{inline.WordSpacing:F3} Tw"); // Set word spacing

        // Get character to glyph ID mapping for TrueType fonts (Type 0/Identity-H)
        // Fall back to character remapping for Type1 fonts
        var glyphIdMapping = _writer.GetCharacterToGlyphId(inline.FontFamily);
        var remapping = _writer.GetCharacterRemapping(inline.FontFamily);

        // Check if we have kerning data available for this font
        var hasKerning = _loadedFonts.TryGetValue(inline.FontFamily, out var font) &&
                         font != null &&
                         font.KerningPairs.Count > 0;

        // For TrueType fonts with glyph ID mapping, use hex string format
        // For Type1 fonts, use literal string format
        if (glyphIdMapping != null)
        {
            if (hasKerning)
            {
                // Use TJ operator with kerning adjustments
                content.AppendLine(BuildKernedTextArrayHex(inline.Text, font!, glyphIdMapping));
            }
            else
            {
                // Use Tj operator without kerning
                content.AppendLine($"<{ConvertToGlyphIdHexString(inline.Text, glyphIdMapping)}> Tj"); // Show text (hex format)
            }
        }
        else
        {
            if (hasKerning)
            {
                // Use TJ operator with kerning adjustments
                content.AppendLine(BuildKernedTextArrayLiteral(inline.Text, font!, remapping));
            }
            else
            {
                // Use Tj operator without kerning
                content.AppendLine($"({EscapeAndRemapString(inline.Text, remapping)}) Tj"); // Show text (literal format)
            }
        }

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

    private void RenderAbsoluteArea(AbsolutePositionedArea absoluteArea, StringBuilder content,
        Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, double pageHeight,
        PdfStructureTree? structureTree, StructureElement? parentElement, int pageIndex)
    {
        // Apply rotation transformation if specified
        bool hasRotation = absoluteArea.ReferenceOrientation != 0;
        if (hasRotation)
        {
            content.AppendLine("q"); // Save graphics state
            ApplyRotationTransformation(content, absoluteArea, pageHeight);
        }

        // Render background color if specified
        if (absoluteArea.BackgroundColor != "transparent" && !string.IsNullOrWhiteSpace(absoluteArea.BackgroundColor))
        {
            var (r, g, b) = ParseColor(absoluteArea.BackgroundColor);

            // Convert from top-left origin to bottom-left origin
            var pdfY = pageHeight - absoluteArea.Y - absoluteArea.Height;

            content.AppendLine($"{r:F3} {g:F3} {b:F3} rg");
            content.AppendLine($"{absoluteArea.X:F2} {pdfY:F2} {absoluteArea.Width:F2} {absoluteArea.Height:F2} re f");
        }

        // Render background image if specified
        if (absoluteArea.BackgroundImageData != null && !string.IsNullOrWhiteSpace(absoluteArea.BackgroundImageFormat))
        {
            RenderBackgroundImageForAbsoluteArea(absoluteArea, content, imageIds, pageHeight);
        }

        // Render borders if specified
        var hasRadius = absoluteArea.BorderTopLeftRadius > 0 || absoluteArea.BorderTopRightRadius > 0 ||
                        absoluteArea.BorderBottomLeftRadius > 0 || absoluteArea.BorderBottomRightRadius > 0;

        var hasDirectionalBorder = (absoluteArea.BorderTopStyle != "none" && absoluteArea.BorderTopWidth > 0) ||
                                    (absoluteArea.BorderBottomStyle != "none" && absoluteArea.BorderBottomWidth > 0) ||
                                    (absoluteArea.BorderLeftStyle != "none" && absoluteArea.BorderLeftWidth > 0) ||
                                    (absoluteArea.BorderRightStyle != "none" && absoluteArea.BorderRightWidth > 0);

        if (hasRadius)
        {
            // Render rounded border (takes precedence over directional borders)
            var pdfY = pageHeight - absoluteArea.Y - absoluteArea.Height;
            RenderRoundedBorderForAbsoluteArea(absoluteArea, content, absoluteArea.X, pdfY);
        }
        else if (hasDirectionalBorder)
        {
            // Convert from top-left origin to bottom-left origin
            var pdfY = pageHeight - absoluteArea.Y - absoluteArea.Height;

            // Top border
            if (absoluteArea.BorderTopStyle != "none" && absoluteArea.BorderTopWidth > 0)
            {
                RenderBorderSide(content, absoluteArea.BorderTopWidth, absoluteArea.BorderTopColor, absoluteArea.BorderTopStyle,
                    absoluteArea.X, pdfY + absoluteArea.Height,
                    absoluteArea.X + absoluteArea.Width, pdfY + absoluteArea.Height);
            }

            // Bottom border
            if (absoluteArea.BorderBottomStyle != "none" && absoluteArea.BorderBottomWidth > 0)
            {
                RenderBorderSide(content, absoluteArea.BorderBottomWidth, absoluteArea.BorderBottomColor, absoluteArea.BorderBottomStyle,
                    absoluteArea.X, pdfY,
                    absoluteArea.X + absoluteArea.Width, pdfY);
            }

            // Left border
            if (absoluteArea.BorderLeftStyle != "none" && absoluteArea.BorderLeftWidth > 0)
            {
                RenderBorderSide(content, absoluteArea.BorderLeftWidth, absoluteArea.BorderLeftColor, absoluteArea.BorderLeftStyle,
                    absoluteArea.X, pdfY,
                    absoluteArea.X, pdfY + absoluteArea.Height);
            }

            // Right border
            if (absoluteArea.BorderRightStyle != "none" && absoluteArea.BorderRightWidth > 0)
            {
                RenderBorderSide(content, absoluteArea.BorderRightWidth, absoluteArea.BorderRightColor, absoluteArea.BorderRightStyle,
                    absoluteArea.X + absoluteArea.Width, pdfY,
                    absoluteArea.X + absoluteArea.Width, pdfY + absoluteArea.Height);
            }
        }

        // Render child areas within the absolute container
        foreach (var child in absoluteArea.Children)
        {
            RenderArea(child, content, fontIds, imageIds, pageHeight, structureTree, parentElement, pageIndex);
        }

        // Restore graphics state if rotation was applied
        if (hasRotation)
        {
            content.AppendLine("Q"); // Restore graphics state
        }
    }

    /// <summary>
    /// Applies rotation transformation matrix to the PDF content stream.
    /// Rotates content around the center of the absolute area.
    /// </summary>
    private void ApplyRotationTransformation(StringBuilder content, AbsolutePositionedArea area, double pageHeight)
    {
        var rotation = area.ReferenceOrientation;

        // Convert from top-left origin to bottom-left origin (PDF coordinate system)
        var pdfY = pageHeight - area.Y - area.Height;

        // Calculate rotation center (center of the area)
        var centerX = area.X + area.Width / 2.0;
        var centerY = pdfY + area.Height / 2.0;

        // Get transformation matrix components based on rotation angle
        double a, b, c, d;
        switch (rotation)
        {
            case 90:
                a = 0; b = 1; c = -1; d = 0;
                break;
            case 180:
                a = -1; b = 0; c = 0; d = -1;
                break;
            case 270:
                a = 0; b = -1; c = 1; d = 0;
                break;
            default: // 0 or invalid
                a = 1; b = 0; c = 0; d = 1;
                break;
        }

        // Calculate translation to rotate around center point
        // Formula: Translate to center, rotate, translate back
        // T(cx, cy) * R(θ) * T(-cx, -cy)
        var tx = centerX - (a * centerX + c * centerY);
        var ty = centerY - (b * centerX + d * centerY);

        // Apply transformation matrix: a b c d tx ty cm
        content.AppendLine($"{a:F6} {b:F6} {c:F6} {d:F6} {tx:F2} {ty:F2} cm");
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

    private void RenderBackgroundImage(BlockArea block, StringBuilder content, Dictionary<string, int> imageIds, double pageHeight, double offsetX, double offsetY)
    {
        if (block.BackgroundImageData == null || string.IsNullOrEmpty(block.BackgroundImageFormat))
            return;

        // Register image and get image ID
        var imageKey = $"{block.BackgroundImage}_{block.BackgroundImageData.GetHashCode()}";
        if (!imageIds.ContainsKey(imageKey))
        {
            imageIds[imageKey] = imageIds.Count + 1;
        }
        var imageId = imageIds[imageKey];

        // Calculate absolute position
        var blockX = offsetX + block.X;
        var blockY = offsetY + block.Y;

        // Calculate background position
        var (bgX, bgY) = CalculateBackgroundPosition(
            block.BackgroundPositionHorizontal,
            block.BackgroundPositionVertical,
            blockX, blockY,
            block.Width, block.Height,
            block.BackgroundImageWidth, block.BackgroundImageHeight);

        // Render based on repeat mode
        RenderBackgroundImageTiles(
            content, imageId, pageHeight,
            blockX, blockY, block.Width, block.Height,
            bgX, bgY,
            block.BackgroundImageWidth, block.BackgroundImageHeight,
            block.BackgroundRepeat);
    }

    private void RenderBackgroundImageForAbsoluteArea(AbsolutePositionedArea area, StringBuilder content, Dictionary<string, int> imageIds, double pageHeight)
    {
        if (area.BackgroundImageData == null || string.IsNullOrEmpty(area.BackgroundImageFormat))
            return;

        // Register image and get image ID
        var imageKey = $"{area.BackgroundImage}_{area.BackgroundImageData.GetHashCode()}";
        if (!imageIds.ContainsKey(imageKey))
        {
            imageIds[imageKey] = imageIds.Count + 1;
        }
        var imageId = imageIds[imageKey];

        // Calculate background position
        var (bgX, bgY) = CalculateBackgroundPosition(
            area.BackgroundPositionHorizontal,
            area.BackgroundPositionVertical,
            area.X, area.Y,
            area.Width, area.Height,
            area.BackgroundImageWidth, area.BackgroundImageHeight);

        // Render based on repeat mode
        RenderBackgroundImageTiles(
            content, imageId, pageHeight,
            area.X, area.Y, area.Width, area.Height,
            bgX, bgY,
            area.BackgroundImageWidth, area.BackgroundImageHeight,
            area.BackgroundRepeat);
    }

    private (double X, double Y) CalculateBackgroundPosition(
        string positionHorizontal, string positionVertical,
        double containerX, double containerY,
        double containerWidth, double containerHeight,
        double imageWidth, double imageHeight)
    {
        // Calculate horizontal position
        double bgX = containerX;
        if (positionHorizontal.EndsWith("%"))
        {
            var percent = double.Parse(positionHorizontal.TrimEnd('%')) / 100.0;
            bgX = containerX + ((containerWidth - imageWidth) * percent);
        }
        else if (positionHorizontal == "left")
        {
            bgX = containerX;
        }
        else if (positionHorizontal == "center")
        {
            bgX = containerX + (containerWidth - imageWidth) / 2.0;
        }
        else if (positionHorizontal == "right")
        {
            bgX = containerX + containerWidth - imageWidth;
        }
        else
        {
            // Try to parse as length
            bgX = containerX + ParseSimpleLength(positionHorizontal);
        }

        // Calculate vertical position
        double bgY = containerY;
        if (positionVertical.EndsWith("%"))
        {
            var percent = double.Parse(positionVertical.TrimEnd('%')) / 100.0;
            bgY = containerY + ((containerHeight - imageHeight) * percent);
        }
        else if (positionVertical == "top")
        {
            bgY = containerY;
        }
        else if (positionVertical == "center")
        {
            bgY = containerY + (containerHeight - imageHeight) / 2.0;
        }
        else if (positionVertical == "bottom")
        {
            bgY = containerY + containerHeight - imageHeight;
        }
        else
        {
            // Try to parse as length
            bgY = containerY + ParseSimpleLength(positionVertical);
        }

        return (bgX, bgY);
    }

    /// <summary>
    /// Parses simple length values like "10pt", "5mm", "2in".
    /// </summary>
    private static double ParseSimpleLength(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        value = value.Trim().ToLowerInvariant();

        // Try to extract number and unit
        var numberPart = "";
        var unitPart = "";

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsDigit(c) || c == '.' || c == '-')
            {
                numberPart += c;
            }
            else
            {
                unitPart = value.Substring(i);
                break;
            }
        }

        if (!double.TryParse(numberPart, out var number))
            return 0;

        // Convert to points based on unit
        return unitPart switch
        {
            "pt" => number,
            "px" => number, // Assume 72 DPI (1 px = 1 pt)
            "in" => number * 72,
            "cm" => number * 72 / 2.54,
            "mm" => number * 72 / 25.4,
            "pc" => number * 12, // pica = 12 points
            "em" => number * 12, // Assume 12pt font (simplified)
            _ => number // Default to points if no unit
        };
    }

    private void RenderBackgroundImageTiles(
        StringBuilder content, int imageId, double pageHeight,
        double containerX, double containerY, double containerWidth, double containerHeight,
        double startX, double startY,
        double imageWidth, double imageHeight,
        string repeatMode)
    {
        // Save graphics state
        content.AppendLine("q");

        // Set clipping rectangle to container bounds
        var clipPdfY = pageHeight - containerY - containerHeight;
        content.AppendLine($"{containerX:F2} {clipPdfY:F2} {containerWidth:F2} {containerHeight:F2} re W n");

        // Determine tiling dimensions based on repeat mode
        bool repeatX = repeatMode == "repeat" || repeatMode == "repeat-x";
        bool repeatY = repeatMode == "repeat" || repeatMode == "repeat-y";

        // Calculate tiling bounds
        double minX = repeatX ? containerX : startX;
        double maxX = repeatX ? containerX + containerWidth : startX + imageWidth;
        double minY = repeatY ? containerY : startY;
        double maxY = repeatY ? containerY + containerHeight : startY + imageHeight;

        // Adjust start positions for repeat modes
        if (repeatX && startX > containerX)
        {
            // Move startX back to before container start
            var offset = (int)Math.Ceiling((startX - containerX) / imageWidth);
            startX -= offset * imageWidth;
        }
        if (repeatY && startY > containerY)
        {
            // Move startY back to before container start
            var offset = (int)Math.Ceiling((startY - containerY) / imageHeight);
            startY -= offset * imageHeight;
        }

        // Render tiles
        for (double y = startY; y < maxY; y += imageHeight)
        {
            for (double x = startX; x < maxX; x += imageWidth)
            {
                // Convert Y coordinate from top-down to PDF's bottom-up
                var pdfY = pageHeight - y - imageHeight;

                // Render image using Do operator
                content.AppendLine($"q");  // Save state
                content.AppendLine($"{imageWidth:F2} 0 0 {imageHeight:F2} {x:F2} {pdfY:F2} cm");  // Transform matrix
                content.AppendLine($"/Im{imageId} Do");  // Draw image
                content.AppendLine($"Q");  // Restore state

                if (!repeatX) break;  // Only one tile horizontally
            }
            if (!repeatY) break;  // Only one tile vertically
        }

        // Restore graphics state
        content.AppendLine("Q");
    }

    private void RenderBorder(BlockArea block, StringBuilder content, double pageHeight, double offsetX, double offsetY)
    {
        // Calculate absolute position
        var x = offsetX + block.X;
        var y = offsetY + block.Y;
        var pdfY = pageHeight - y - block.Height;

        // Check if any border radius is present
        var hasRadius = block.BorderTopLeftRadius > 0 || block.BorderTopRightRadius > 0 ||
                        block.BorderBottomLeftRadius > 0 || block.BorderBottomRightRadius > 0;

        if (hasRadius)
        {
            // Render rounded border
            RenderRoundedBorder(block, content, x, pdfY);
            return;
        }

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

    /// <summary>
    /// Renders a border with rounded corners using Bezier curves.
    /// </summary>
    private void RenderRoundedBorder(BlockArea block, StringBuilder content, double x, double pdfY)
    {
        // Clamp radii to ensure they don't exceed half the box dimensions
        var maxRadiusX = block.Width / 2;
        var maxRadiusY = block.Height / 2;

        var topLeftRadius = Math.Min(block.BorderTopLeftRadius, Math.Min(maxRadiusX, maxRadiusY));
        var topRightRadius = Math.Min(block.BorderTopRightRadius, Math.Min(maxRadiusX, maxRadiusY));
        var bottomRightRadius = Math.Min(block.BorderBottomRightRadius, Math.Min(maxRadiusX, maxRadiusY));
        var bottomLeftRadius = Math.Min(block.BorderBottomLeftRadius, Math.Min(maxRadiusX, maxRadiusY));

        // Bezier curve constant for approximating a quarter circle
        // Magic number: 4/3 * (sqrt(2) - 1) ≈ 0.552284749831
        const double kappa = 0.552284749831;

        // TODO: For now, use a uniform border style. In the future, we could support
        // different styles per side with rounded corners, but that's complex.
        var borderWidth = Math.Max(Math.Max(block.BorderTopWidth, block.BorderBottomWidth),
                                   Math.Max(block.BorderLeftWidth, block.BorderRightWidth));
        var borderColor = block.BorderTopColor != "black" ? block.BorderTopColor :
                         block.BorderBottomColor != "black" ? block.BorderBottomColor :
                         block.BorderLeftColor != "black" ? block.BorderLeftColor :
                         block.BorderRightColor;
        var borderStyle = block.BorderTopStyle != "none" ? block.BorderTopStyle :
                         block.BorderBottomStyle != "none" ? block.BorderBottomStyle :
                         block.BorderLeftStyle != "none" ? block.BorderLeftStyle :
                         block.BorderRightStyle;

        if (borderStyle == "none" || borderWidth <= 0)
            return;

        var (r, g, b) = ParseColor(borderColor);

        content.AppendLine("q"); // Save graphics state
        content.AppendLine($"{r:F3} {g:F3} {b:F3} RG");
        content.AppendLine($"{borderWidth:F2} w");

        // Set dash pattern based on style
        switch (borderStyle.ToLowerInvariant())
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

        // Adjust for border width (stroke is centered on path)
        var halfWidth = borderWidth / 2;
        var left = x + halfWidth;
        var right = x + block.Width - halfWidth;
        var bottom = pdfY + halfWidth;
        var top = pdfY + block.Height - halfWidth;

        // Adjust radii for the stroke offset
        var tlr = Math.Max(0, topLeftRadius - halfWidth);
        var trr = Math.Max(0, topRightRadius - halfWidth);
        var brr = Math.Max(0, bottomRightRadius - halfWidth);
        var blr = Math.Max(0, bottomLeftRadius - halfWidth);

        // Build the rounded rectangle path
        // Start at the top-left corner, after the radius
        content.AppendLine($"{left + tlr:F2} {top:F2} m");

        // Top edge + top-right corner
        content.AppendLine($"{right - trr:F2} {top:F2} l");
        if (trr > 0)
        {
            var cp1x = right - trr + trr * kappa;
            var cp1y = top;
            var cp2x = right;
            var cp2y = top - trr + trr * kappa;
            content.AppendLine($"{cp1x:F2} {cp1y:F2} {cp2x:F2} {cp2y:F2} {right:F2} {top - trr:F2} c");
        }

        // Right edge + bottom-right corner
        content.AppendLine($"{right:F2} {bottom + brr:F2} l");
        if (brr > 0)
        {
            var cp1x = right;
            var cp1y = bottom + brr - brr * kappa;
            var cp2x = right - brr + brr * kappa;
            var cp2y = bottom;
            content.AppendLine($"{cp1x:F2} {cp1y:F2} {cp2x:F2} {cp2y:F2} {right - brr:F2} {bottom:F2} c");
        }

        // Bottom edge + bottom-left corner
        content.AppendLine($"{left + blr:F2} {bottom:F2} l");
        if (blr > 0)
        {
            var cp1x = left + blr - blr * kappa;
            var cp1y = bottom;
            var cp2x = left;
            var cp2y = bottom + blr - blr * kappa;
            content.AppendLine($"{cp1x:F2} {cp1y:F2} {cp2x:F2} {cp2y:F2} {left:F2} {bottom + blr:F2} c");
        }

        // Left edge + top-left corner
        content.AppendLine($"{left:F2} {top - tlr:F2} l");
        if (tlr > 0)
        {
            var cp1x = left;
            var cp1y = top - tlr + tlr * kappa;
            var cp2x = left + tlr - tlr * kappa;
            var cp2y = top;
            content.AppendLine($"{cp1x:F2} {cp1y:F2} {cp2x:F2} {cp2y:F2} {left + tlr:F2} {top:F2} c");
        }

        content.AppendLine("S"); // Stroke the path
        content.AppendLine("Q"); // Restore graphics state
    }

    /// <summary>
    /// Renders a border with rounded corners for AbsolutePositionedArea using Bezier curves.
    /// </summary>
    private void RenderRoundedBorderForAbsoluteArea(AbsolutePositionedArea area, StringBuilder content, double x, double pdfY)
    {
        // Clamp radii to ensure they don't exceed half the box dimensions
        var maxRadiusX = area.Width / 2;
        var maxRadiusY = area.Height / 2;

        var topLeftRadius = Math.Min(area.BorderTopLeftRadius, Math.Min(maxRadiusX, maxRadiusY));
        var topRightRadius = Math.Min(area.BorderTopRightRadius, Math.Min(maxRadiusX, maxRadiusY));
        var bottomRightRadius = Math.Min(area.BorderBottomRightRadius, Math.Min(maxRadiusX, maxRadiusY));
        var bottomLeftRadius = Math.Min(area.BorderBottomLeftRadius, Math.Min(maxRadiusX, maxRadiusY));

        // Bezier curve constant for approximating a quarter circle
        const double kappa = 0.552284749831;

        // TODO: For now, use a uniform border style
        var borderWidth = Math.Max(Math.Max(area.BorderTopWidth, area.BorderBottomWidth),
                                   Math.Max(area.BorderLeftWidth, area.BorderRightWidth));
        var borderColor = area.BorderTopColor != "black" ? area.BorderTopColor :
                         area.BorderBottomColor != "black" ? area.BorderBottomColor :
                         area.BorderLeftColor != "black" ? area.BorderLeftColor :
                         area.BorderRightColor;
        var borderStyle = area.BorderTopStyle != "none" ? area.BorderTopStyle :
                         area.BorderBottomStyle != "none" ? area.BorderBottomStyle :
                         area.BorderLeftStyle != "none" ? area.BorderLeftStyle :
                         area.BorderRightStyle;

        if (borderStyle == "none" || borderWidth <= 0)
            return;

        var (r, g, b) = ParseColor(borderColor);

        content.AppendLine("q"); // Save graphics state
        content.AppendLine($"{r:F3} {g:F3} {b:F3} RG");
        content.AppendLine($"{borderWidth:F2} w");

        // Set dash pattern based on style
        switch (borderStyle.ToLowerInvariant())
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

        // Adjust for border width (stroke is centered on path)
        var halfWidth = borderWidth / 2;
        var left = x + halfWidth;
        var right = x + area.Width - halfWidth;
        var bottom = pdfY + halfWidth;
        var top = pdfY + area.Height - halfWidth;

        // Adjust radii for the stroke offset
        var tlr = Math.Max(0, topLeftRadius - halfWidth);
        var trr = Math.Max(0, topRightRadius - halfWidth);
        var brr = Math.Max(0, bottomRightRadius - halfWidth);
        var blr = Math.Max(0, bottomLeftRadius - halfWidth);

        // Build the rounded rectangle path (same as BlockArea)
        content.AppendLine($"{left + tlr:F2} {top:F2} m");

        // Top edge + top-right corner
        content.AppendLine($"{right - trr:F2} {top:F2} l");
        if (trr > 0)
        {
            var cp1x = right - trr + trr * kappa;
            var cp1y = top;
            var cp2x = right;
            var cp2y = top - trr + trr * kappa;
            content.AppendLine($"{cp1x:F2} {cp1y:F2} {cp2x:F2} {cp2y:F2} {right:F2} {top - trr:F2} c");
        }

        // Right edge + bottom-right corner
        content.AppendLine($"{right:F2} {bottom + brr:F2} l");
        if (brr > 0)
        {
            var cp1x = right;
            var cp1y = bottom + brr - brr * kappa;
            var cp2x = right - brr + brr * kappa;
            var cp2y = bottom;
            content.AppendLine($"{cp1x:F2} {cp1y:F2} {cp2x:F2} {cp2y:F2} {right - brr:F2} {bottom:F2} c");
        }

        // Bottom edge + bottom-left corner
        content.AppendLine($"{left + blr:F2} {bottom:F2} l");
        if (blr > 0)
        {
            var cp1x = left + blr - blr * kappa;
            var cp1y = bottom;
            var cp2x = left;
            var cp2y = bottom + blr - blr * kappa;
            content.AppendLine($"{cp1x:F2} {cp1y:F2} {cp2x:F2} {cp2y:F2} {left:F2} {bottom + blr:F2} c");
        }

        // Left edge + top-left corner
        content.AppendLine($"{left:F2} {top - tlr:F2} l");
        if (tlr > 0)
        {
            var cp1x = left;
            var cp1y = top - tlr + tlr * kappa;
            var cp2x = left + tlr - tlr * kappa;
            var cp2y = top;
            content.AppendLine($"{cp1x:F2} {cp1y:F2} {cp2x:F2} {cp2y:F2} {left + tlr:F2} {top:F2} c");
        }

        content.AppendLine("S"); // Stroke the path
        content.AppendLine("Q"); // Restore graphics state
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

    private void RenderImage(ImageArea image, StringBuilder content, Dictionary<string, int> imageIds, double pageHeight, double offsetX, double offsetY, PdfStructureTree? structureTree, StructureElement? parentElement, int pageIndex)
    {
        // Look up the image ID
        if (!imageIds.TryGetValue(image.Source, out var imageId))
            return; // Image not found

        // Create Figure structure element if tagging is enabled
        StructureElement? figureElement = null;
        int mcid = -1;
        if (structureTree != null && parentElement != null)
        {
            figureElement = structureTree.CreateElement(StructureRole.Figure, parentElement);
            mcid = structureTree.RegisterMarkedContent(figureElement, pageIndex);

            // Generate alt text from filename (extract filename from path)
            var altText = Path.GetFileNameWithoutExtension(image.Source);
            if (!string.IsNullOrEmpty(altText))
            {
                figureElement.AltText = altText;
            }

            // Begin marked content for figure
            content.AppendLine($"/Figure <</MCID {mcid}>> BDC");
        }

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

        // End marked content for figure
        if (mcid >= 0)
        {
            content.AppendLine("EMC");
        }
    }

    private void RenderSvg(SvgArea svg, StringBuilder content, double pageHeight, double offsetX, double offsetY)
    {
        if (svg.SvgDocument == null)
            return;

        // Save graphics state
        content.AppendLine("q");

        // Calculate absolute position
        var x = offsetX + svg.X;
        var y = offsetY + svg.Y;

        // Convert Y coordinate from top-down to PDF's bottom-up coordinate system
        var pdfY = pageHeight - y - svg.Height;

        // Translate to SVG position
        content.AppendLine($"1 0 0 1 {x:F2} {pdfY:F2} cm");

        // Calculate scaling factor based on display size vs intrinsic size
        double scaleX = svg.Width / svg.IntrinsicWidth;
        double scaleY = svg.Height / svg.IntrinsicHeight;

        // Apply scaling to match display dimensions
        content.AppendLine($"{scaleX:F6} 0 0 {scaleY:F6} 0 0 cm");

        // Convert SVG to PDF content stream
        var svgConverter = new Svg.SvgToPdfConverter(svg.SvgDocument);
        var svgResult = svgConverter.Convert();

        // Append the SVG content directly
        content.Append(svgResult.ContentStream);

        // Restore graphics state
        content.AppendLine("Q");
    }

    private void RenderTable(TableArea table, StringBuilder content, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, double pageHeight, PdfStructureTree? structureTree, StructureElement? parentElement, int pageIndex)
    {
        // Create Table structure element if tagging is enabled
        StructureElement? tableElement = null;
        if (structureTree != null && parentElement != null)
        {
            tableElement = structureTree.CreateElement(StructureRole.Table, parentElement);
        }

        // Render each row in the table
        int rowIndex = 0;
        foreach (var row in table.Rows)
        {
            RenderTableRow(row, table, content, fontIds, imageIds, pageHeight, structureTree, tableElement, pageIndex, rowIndex);
            rowIndex++;
        }
    }

    private void RenderTableRow(TableRowArea row, TableArea table, StringBuilder content, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, double pageHeight, PdfStructureTree? structureTree, StructureElement? tableElement, int pageIndex, int rowIndex)
    {
        // Create TR (table row) structure element if tagging is enabled
        StructureElement? rowElement = null;
        if (structureTree != null && tableElement != null)
        {
            rowElement = structureTree.CreateElement(StructureRole.TableRow, tableElement);
        }

        // Detect if this is a header row (first row of table)
        bool isHeaderRow = rowIndex == 0;

        // Render each cell in the row
        foreach (var cell in row.Cells)
        {
            RenderTableCell(cell, table, row, content, fontIds, imageIds, pageHeight, structureTree, rowElement, pageIndex, isHeaderRow);
        }
    }

    private void RenderTableCell(TableCellArea cell, TableArea table, TableRowArea row, StringBuilder content, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, double pageHeight, PdfStructureTree? structureTree, StructureElement? rowElement, int pageIndex, bool isHeaderRow)
    {
        // Create TH (table header) or TD (table data) structure element if tagging is enabled
        StructureElement? cellElement = null;
        int mcid = -1;
        if (structureTree != null && rowElement != null)
        {
            // Use TH for header cells, TD for data cells
            var cellRole = isHeaderRow ? StructureRole.TableHeader : StructureRole.TableData;
            cellElement = structureTree.CreateElement(cellRole, rowElement);
            mcid = structureTree.RegisterMarkedContent(cellElement, pageIndex);

            // Begin marked content for table cell
            var cellTag = isHeaderRow ? "TH" : "TD";
            content.AppendLine($"/{cellTag} <</MCID {mcid}>> BDC");
        }

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
        // Don't pass structure tree to cell children - they're part of the cell's marked content
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

                RenderArea(child, content, fontIds, imageIds, pageHeight, null, null, pageIndex);

                // Restore original position
                blockArea.X = originalX;
                blockArea.Y = originalY;
            }
            else
            {
                // Pass the absolute position offsets for non-BlockArea children
                RenderArea(child, content, fontIds, imageIds, pageHeight, null, null, pageIndex, absoluteX, absoluteY);
            }
        }

        // End marked content for table cell
        if (mcid >= 0)
        {
            content.AppendLine("EMC");
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
    // Unicode to Adobe Standard Encoding mappings for PDF rendering
    // Must match the mappings in StandardFonts.cs
    private static readonly Dictionary<char, char> UnicodeToAdobeEncoding = new()
    {
        { '\u2014', (char)208 },  // Em dash (—) → emdash
        { '\u2013', (char)177 },  // En dash (–) → endash
        { '\u2018', (char)193 },  // Left single quotation mark (') → quoteleft
        { '\u2019', (char)194 },  // Right single quotation mark (') → quoteright
        { '\u201C', (char)195 },  // Left double quotation mark (") → quotedblleft
        { '\u201D', (char)196 },  // Right double quotation mark (") → quotedblright
        { '\u2022', (char)183 },  // Bullet (•) → bullet
        { '\u2026', (char)188 },  // Horizontal ellipsis (…) → ellipsis
        { '\u2020', (char)178 },  // Dagger (†) → dagger
        { '\u2021', (char)179 },  // Double dagger (‡) → daggerdbl
        { '\u2030', (char)189 },  // Per mille sign (‰) → perthousand
        { '\u0192', (char)166 },  // Latin small letter f with hook (ƒ) → florin
    };

    /// <summary>
    /// Converts text to a hex string of 2-byte glyph IDs for Type 0 fonts with Identity-H encoding.
    /// Each character is mapped to its glyph ID and written as a 4-digit hex value.
    /// Example: "AB" with glyph IDs 65,66 becomes "00410042"
    /// </summary>
    private static string ConvertToGlyphIdHexString(string str, Dictionary<char, ushort> glyphIdMapping)
    {
        var result = new StringBuilder(str.Length * 4); // 4 hex digits per character

        foreach (var ch in str)
        {
            // Look up glyph ID for this character
            if (glyphIdMapping.TryGetValue(ch, out var glyphId))
            {
                // Write glyph ID as 4-digit hex (2 bytes)
                result.Append($"{glyphId:X4}");
            }
            else
            {
                // Character not in font - use glyph ID 0 (missing glyph / .notdef)
                result.Append("0000");
            }
        }

        return result.ToString();
    }

    private static string EscapeAndRemapString(string str, Dictionary<char, byte>? remapping)
    {
        var result = new StringBuilder(str.Length);
        foreach (var ch in str)
        {
            char outputChar = ch;

            // First, apply font subsetting remapping if available
            if (remapping != null && remapping.TryGetValue(ch, out var remappedByte))
            {
                outputChar = (char)remappedByte;
            }
            // Otherwise, apply Unicode to Adobe Standard Encoding conversion
            else if (UnicodeToAdobeEncoding.TryGetValue(ch, out var adobeChar))
            {
                outputChar = adobeChar;
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
    /// Builds a kerned text array for the TJ operator using hex string format (for TrueType fonts).
    /// Returns a PDF array with strings and kerning adjustments, e.g., "[(A) -100 (V)] TJ"
    /// </summary>
    private static string BuildKernedTextArrayHex(string text, Fonts.Models.FontFile font, Dictionary<char, ushort> glyphIdMapping)
    {
        if (string.IsNullOrEmpty(text))
            return "[] TJ";

        var array = new StringBuilder();
        array.Append('[');

        char? previousChar = null;

        foreach (var ch in text)
        {
            // Get kerning adjustment if we have a previous character
            if (previousChar.HasValue)
            {
                var kerning = font.GetKerning(previousChar.Value, ch);
                if (kerning != 0)
                {
                    // Convert font units to PDF units (1000ths of an em)
                    // Font: negative kerning = characters closer, positive = further apart
                    // PDF TJ: positive adjustment = move left (closer), negative = move right (further)
                    // Therefore: negate the font kerning value
                    var pdfKerning = -kerning * 1000.0 / font.UnitsPerEm;
                    array.Append($" {pdfKerning:F0}");
                }
            }

            // Add character as hex glyph ID
            if (glyphIdMapping.TryGetValue(ch, out var glyphId))
            {
                array.Append($" <{glyphId:X4}>");
            }
            else
            {
                // Fallback to glyph 0 (.notdef)
                array.Append(" <0000>");
            }

            previousChar = ch;
        }

        array.Append("] TJ");
        return array.ToString();
    }

    /// <summary>
    /// Builds a kerned text array for the TJ operator using literal string format (for Type1 fonts).
    /// Returns a PDF array with strings and kerning adjustments, e.g., "[(A) -100 (V)] TJ"
    /// </summary>
    private static string BuildKernedTextArrayLiteral(string text, Fonts.Models.FontFile font, Dictionary<char, byte>? remapping)
    {
        if (string.IsNullOrEmpty(text))
            return "[] TJ";

        var array = new StringBuilder();
        array.Append('[');

        char? previousChar = null;

        foreach (var ch in text)
        {
            // Get kerning adjustment if we have a previous character
            if (previousChar.HasValue)
            {
                var kerning = font.GetKerning(previousChar.Value, ch);
                if (kerning != 0)
                {
                    // Convert font units to PDF units (1000ths of an em)
                    // Font: negative kerning = characters closer, positive = further apart
                    // PDF TJ: positive adjustment = move left (closer), negative = move right (further)
                    // Therefore: negate the font kerning value
                    var pdfKerning = -kerning * 1000.0 / font.UnitsPerEm;
                    array.Append($" {pdfKerning:F0}");
                }
            }

            // Add character as escaped literal string
            var escapedChar = EscapeSingleChar(ch, remapping);
            array.Append($" ({escapedChar})");

            previousChar = ch;
        }

        array.Append("] TJ");
        return array.ToString();
    }

    /// <summary>
    /// Escapes a single character for PDF literal strings, applying remapping if available.
    /// </summary>
    private static string EscapeSingleChar(char ch, Dictionary<char, byte>? remapping)
    {
        char outputChar = ch;

        // First, apply font subsetting remapping if available
        if (remapping != null && remapping.TryGetValue(ch, out var remappedByte))
        {
            outputChar = (char)remappedByte;
        }
        // Otherwise, apply Unicode to Adobe Standard Encoding conversion
        else if (UnicodeToAdobeEncoding.TryGetValue(ch, out var adobeChar))
        {
            outputChar = adobeChar;
        }

        // Escape special PDF characters
        return outputChar switch
        {
            '\\' => "\\\\",
            '(' => "\\(",
            ')' => "\\)",
            '\r' => "\\r",
            '\n' => "\\n",
            '\t' => "\\t",
            _ => outputChar.ToString()
        };
    }

    /// <summary>
    /// Detects the appropriate structure role for a block area based on its properties.
    /// First checks for explicit structure hints, then uses font size and font weight heuristics.
    /// </summary>
    private static StructureRole DetectStructureRole(BlockArea blockArea)
    {
        // Check for explicit structure hint first (used for lists, etc.)
        if (!string.IsNullOrEmpty(blockArea.StructureHint))
        {
            return blockArea.StructureHint switch
            {
                "List" => StructureRole.List,
                "ListItem" => StructureRole.ListItem,
                "ListLabel" => StructureRole.ListLabel,
                "ListBody" => StructureRole.ListBody,
                _ => StructureRole.Paragraph // Unknown hint, default to paragraph
            };
        }

        var fontSize = blockArea.FontSize;
        var fontWeight = blockArea.FontWeight?.ToLowerInvariant();
        var isBold = fontWeight == "bold" || fontWeight == "700" || fontWeight == "800" || fontWeight == "900";

        // Heading detection based on font size and weight
        // H1: Large text (24pt+) or bold large text (20pt+)
        if (fontSize >= 24 || (isBold && fontSize >= 20))
        {
            return StructureRole.Heading1;
        }
        // H2: Medium-large text (18pt+) or bold medium text (16pt+)
        else if (fontSize >= 18 || (isBold && fontSize >= 16))
        {
            return StructureRole.Heading2;
        }
        // H3: Medium text (14pt+) or bold small-medium text (13pt+)
        else if (fontSize >= 14 || (isBold && fontSize >= 13))
        {
            return StructureRole.Heading3;
        }
        // H4: Small-medium text (12pt) if bold
        else if (isBold && fontSize >= 12)
        {
            return StructureRole.Heading4;
        }

        // Default to paragraph for regular text
        return StructureRole.Paragraph;
    }

    /// <summary>
    /// Gets the PDF marked content tag string for a structure role.
    /// </summary>
    private static string GetRoleTag(StructureRole role)
    {
        return role switch
        {
            StructureRole.Document => "Document",
            StructureRole.Part => "Part",
            StructureRole.Section => "Sect",
            StructureRole.Division => "Div",
            StructureRole.Paragraph => "P",
            StructureRole.Heading => "H",
            StructureRole.Heading1 => "H1",
            StructureRole.Heading2 => "H2",
            StructureRole.Heading3 => "H3",
            StructureRole.Heading4 => "H4",
            StructureRole.Heading5 => "H5",
            StructureRole.Heading6 => "H6",
            StructureRole.List => "L",
            StructureRole.ListItem => "LI",
            StructureRole.ListLabel => "Lbl",
            StructureRole.ListBody => "LBody",
            StructureRole.Table => "Table",
            StructureRole.TableRow => "TR",
            StructureRole.TableHeader => "TH",
            StructureRole.TableData => "TD",
            StructureRole.TableBody => "TBody",
            StructureRole.TableHead => "THead",
            StructureRole.TableFoot => "TFoot",
            StructureRole.Figure => "Figure",
            StructureRole.Formula => "Formula",
            StructureRole.Form => "Form",
            StructureRole.Link => "Link",
            StructureRole.Annotation => "Annot",
            StructureRole.Quote => "BlockQuote",
            StructureRole.Note => "Note",
            StructureRole.Reference => "Reference",
            StructureRole.BibliographyEntry => "BibEntry",
            StructureRole.Code => "Code",
            StructureRole.Span => "Span",
            _ => "P" // Default to paragraph
        };
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
