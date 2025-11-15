using System.Text;

namespace Folly.Svg;

/// <summary>
/// Converts SVG documents to PDF graphics commands (content stream).
/// This is the core converter that translates SVG vector graphics to PDF primitives.
/// </summary>
public sealed class SvgToPdfConverter
{
    private readonly StringBuilder _contentStream = new();
    private readonly SvgDocument _document;
    private readonly Stack<SvgTransform> _transformStack = new();
    private readonly Stack<SvgStyle> _styleStack = new();

    // Resource collections
    private readonly Dictionary<string, string> _shadings = new();
    private readonly Dictionary<string, string> _patterns = new();
    private readonly Dictionary<string, byte[]> _xObjects = new();
    private readonly Dictionary<string, string> _graphicsStates = new();

    // Resource counters for naming
    private int _shadingCounter = 0;
    private int _graphicsStateCounter = 0;
    private int _patternCounter = 0;
    private int _xObjectCounter = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="SvgToPdfConverter"/> class.
    /// </summary>
    /// <param name="document">The SVG document to convert.</param>
    public SvgToPdfConverter(SvgDocument document)
    {
        _document = document;
    }

    /// <summary>
    /// Converts the SVG document to PDF content stream commands and resource definitions.
    /// Returns a result containing both the content stream and resources.
    /// </summary>
    public SvgToPdfResult Convert()
    {
        _contentStream.Clear();
        _transformStack.Clear();
        _styleStack.Clear();

        // Clear resource collections
        _shadings.Clear();
        _patterns.Clear();
        _xObjects.Clear();
        _graphicsStates.Clear();

        // Reset resource counters
        _shadingCounter = 0;
        _patternCounter = 0;
        _xObjectCounter = 0;
        _graphicsStateCounter = 0;

        // Save graphics state
        _contentStream.AppendLine("q");

        // Set up coordinate system (SVG uses top-left origin, PDF uses bottom-left)
        // We'll flip the Y axis and translate
        if (_document.ViewBox != null)
        {
            var vb = _document.ViewBox;

            // Scale from viewBox to effective size
            var scaleX = _document.EffectiveWidthPt / vb.Width;
            var scaleY = _document.EffectiveHeightPt / vb.Height;

            // Apply viewBox transformation
            _contentStream.AppendLine($"1 0 0 -1 0 {_document.EffectiveHeightPt} cm"); // Flip Y
            _contentStream.AppendLine($"{scaleX} 0 0 {scaleY} {-vb.MinX * scaleX} {-vb.MinY * scaleY} cm");
        }
        else
        {
            // No viewBox, just flip Y axis
            _contentStream.AppendLine($"1 0 0 -1 0 {_document.EffectiveHeightPt} cm");
        }

        // Render the SVG tree
        RenderElement(_document.Root);

        // Restore graphics state
        _contentStream.AppendLine("Q");

        // Return result with content stream and collected resources
        return new SvgToPdfResult
        {
            ContentStream = _contentStream.ToString(),
            Shadings = new Dictionary<string, string>(_shadings),
            Patterns = new Dictionary<string, string>(_patterns),
            XObjects = new Dictionary<string, byte[]>(_xObjects),
            GraphicsStates = new Dictionary<string, string>(_graphicsStates)
        };
    }

    private void RenderElement(SvgElement element)
    {
        // Skip if not visible
        if (!element.ShouldRender())
            return;

        // Save state for this element
        _contentStream.AppendLine("q");

        // Apply transform
        if (element.Transform != null && !element.Transform.IsIdentity())
        {
            var t = element.Transform;
            _contentStream.AppendLine($"{t.A} {t.B} {t.C} {t.D} {t.E} {t.F} cm");
        }

        // Apply clipping path if specified
        if (!string.IsNullOrWhiteSpace(element.Style.ClipPath))
        {
            ApplyClippingPath(element.Style.ClipPath);
        }

        // Apply drop shadow if filter specified
        // NOTE: This is a simplified implementation without blur - just offset + opacity
        if (!string.IsNullOrWhiteSpace(element.Style.Filter) && IsUrlReference(element.Style.Filter))
        {
            var filterId = ExtractUrlId(element.Style.Filter);
            if (!string.IsNullOrWhiteSpace(filterId) && _document.Filters.TryGetValue(filterId, out var filter))
            {
                ApplySimpleDropShadow(element, filter);
            }
        }

        // Apply element opacity if needed
        if (element.Style.Opacity < 1.0)
        {
            // TODO: For proper opacity groups, we should:
            // 1. Create a transparency group (XObject with /Group dictionary)
            // 2. Render element and children into the group
            // 3. Paint the group with the specified opacity
            // For now, we'll apply opacity to fills and strokes individually
        }

        // Render based on element type
        switch (element.ElementType)
        {
            case "rect":
                RenderRect(element);
                break;
            case "circle":
                RenderCircle(element);
                break;
            case "ellipse":
                RenderEllipse(element);
                break;
            case "line":
                RenderLine(element);
                break;
            case "polyline":
                RenderPolyline(element);
                break;
            case "polygon":
                RenderPolygon(element);
                break;
            case "path":
                RenderPath(element);
                break;
            case "g":
                // Group: just render children
                break;
            case "symbol":
                // Symbol: container for reusable elements, render children
                break;
            case "text":
                RenderText(element);
                break;
            case "svg":
                // Nested SVG: render children
                break;
            case "use":
                RenderUse(element);
                break;
            case "image":
                RenderImage(element);
                break;
            case "defs":
                // Definitions: skip rendering (elements are referenced via <use>)
                return;
        }

        // Render children
        foreach (var child in element.Children)
        {
            RenderElement(child);
        }

        // Restore state
        _contentStream.AppendLine("Q");
    }

    private void RenderRect(SvgElement element)
    {
        var x = element.GetDoubleAttribute("x", 0);
        var y = element.GetDoubleAttribute("y", 0);
        var width = element.GetDoubleAttribute("width", 0);
        var height = element.GetDoubleAttribute("height", 0);
        var rx = element.GetDoubleAttribute("rx", 0);
        var ry = element.GetDoubleAttribute("ry", 0);

        if (width <= 0 || height <= 0) return;

        // SVG spec: if one of rx/ry is specified but the other isn't, use the specified value for both
        if (rx > 0 && ry == 0) ry = rx;
        if (ry > 0 && rx == 0) rx = ry;

        // Clamp to half the width/height (SVG spec)
        rx = Math.Min(rx, width / 2);
        ry = Math.Min(ry, height / 2);

        // Build the path
        if (rx > 0 && ry > 0)
        {
            DrawRoundedRectangle(x, y, width, height, rx, ry);
        }
        else
        {
            // Regular rectangle
            _contentStream.AppendLine($"{x} {y} {width} {height} re");
        }

        // Apply fill and stroke - now supports gradients!
        ApplyFillAndStroke(element.Style, (x, y, width, height));
    }

    private void DrawRoundedRectangle(double x, double y, double width, double height, double rx, double ry)
    {
        // Magic number for Bézier approximation of a quarter circle
        const double kappa = 0.5522847498;
        var cpx = kappa * rx;
        var cpy = kappa * ry;

        // Start at top-left corner (after the rounded corner)
        _contentStream.AppendLine($"{x + rx} {y} m");

        // Top edge
        _contentStream.AppendLine($"{x + width - rx} {y} l");

        // Top-right corner
        _contentStream.AppendLine($"{x + width - rx + cpx} {y} {x + width} {y + ry - cpy} {x + width} {y + ry} c");

        // Right edge
        _contentStream.AppendLine($"{x + width} {y + height - ry} l");

        // Bottom-right corner
        _contentStream.AppendLine($"{x + width} {y + height - ry + cpy} {x + width - rx + cpx} {y + height} {x + width - rx} {y + height} c");

        // Bottom edge
        _contentStream.AppendLine($"{x + rx} {y + height} l");

        // Bottom-left corner
        _contentStream.AppendLine($"{x + rx - cpx} {y + height} {x} {y + height - ry + cpy} {x} {y + height - ry} c");

        // Left edge
        _contentStream.AppendLine($"{x} {y + ry} l");

        // Top-left corner
        _contentStream.AppendLine($"{x} {y + ry - cpy} {x + rx - cpx} {y} {x + rx} {y} c");
    }

    private void RenderCircle(SvgElement element)
    {
        var cx = element.GetDoubleAttribute("cx", 0);
        var cy = element.GetDoubleAttribute("cy", 0);
        var r = element.GetDoubleAttribute("r", 0);

        if (r <= 0) return;

        // Draw circle using Bézier curve approximation
        DrawEllipse(cx, cy, r, r);

        // Circle bounding box for gradient rendering
        var bbox = (cx - r, cy - r, r * 2, r * 2);
        ApplyFillAndStroke(element.Style, bbox);
    }

    private void RenderEllipse(SvgElement element)
    {
        var cx = element.GetDoubleAttribute("cx", 0);
        var cy = element.GetDoubleAttribute("cy", 0);
        var rx = element.GetDoubleAttribute("rx", 0);
        var ry = element.GetDoubleAttribute("ry", 0);

        if (rx <= 0 || ry <= 0) return;

        DrawEllipse(cx, cy, rx, ry);

        // Ellipse bounding box for gradient rendering
        var bbox = (cx - rx, cy - ry, rx * 2, ry * 2);
        ApplyFillAndStroke(element.Style, bbox);
    }

    private void DrawEllipse(double cx, double cy, double rx, double ry)
    {
        // Magic number for Bézier approximation of a circle
        const double kappa = 0.5522847498;
        var dx = kappa * rx;
        var dy = kappa * ry;

        // Start at right-most point
        _contentStream.AppendLine($"{cx + rx} {cy} m");

        // Top-right curve
        _contentStream.AppendLine($"{cx + rx} {cy + dy} {cx + dx} {cy + ry} {cx} {cy + ry} c");

        // Top-left curve
        _contentStream.AppendLine($"{cx - dx} {cy + ry} {cx - rx} {cy + dy} {cx - rx} {cy} c");

        // Bottom-left curve
        _contentStream.AppendLine($"{cx - rx} {cy - dy} {cx - dx} {cy - ry} {cx} {cy - ry} c");

        // Bottom-right curve
        _contentStream.AppendLine($"{cx + dx} {cy - ry} {cx + rx} {cy - dy} {cx + rx} {cy} c");
    }

    private void RenderLine(SvgElement element)
    {
        var x1 = element.GetDoubleAttribute("x1", 0);
        var y1 = element.GetDoubleAttribute("y1", 0);
        var x2 = element.GetDoubleAttribute("x2", 0);
        var y2 = element.GetDoubleAttribute("y2", 0);

        _contentStream.AppendLine($"{x1} {y1} m");
        _contentStream.AppendLine($"{x2} {y2} l");
        ApplyStroke(element.Style);
    }

    private void RenderPolyline(SvgElement element)
    {
        var points = element.GetAttribute("points");
        if (string.IsNullOrWhiteSpace(points)) return;

        var coords = SvgLengthParser.ParseList(points);
        if (coords.Length < 2) return;

        // Calculate bounding box for gradient support (in case fill is specified)
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        for (int i = 0; i < coords.Length; i += 2)
        {
            if (i + 1 < coords.Length)
            {
                var x = coords[i];
                var y = coords[i + 1];
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        // Move to first point
        _contentStream.AppendLine($"{coords[0]} {coords[1]} m");

        // Line to subsequent points
        for (int i = 2; i < coords.Length; i += 2)
        {
            if (i + 1 < coords.Length)
                _contentStream.AppendLine($"{coords[i]} {coords[i + 1]} l");
        }

        // Pass bounding box for gradient support (polyline can have fill too)
        var bbox = (minX, minY, maxX - minX, maxY - minY);
        ApplyFillAndStroke(element.Style, bbox);
    }

    private void RenderPolygon(SvgElement element)
    {
        var points = element.GetAttribute("points");
        if (string.IsNullOrWhiteSpace(points)) return;

        var coords = SvgLengthParser.ParseList(points);
        if (coords.Length < 2) return;

        // Calculate bounding box for gradient support
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        for (int i = 0; i < coords.Length; i += 2)
        {
            if (i + 1 < coords.Length)
            {
                var x = coords[i];
                var y = coords[i + 1];
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        // Move to first point
        _contentStream.AppendLine($"{coords[0]} {coords[1]} m");

        // Line to subsequent points
        for (int i = 2; i < coords.Length; i += 2)
        {
            if (i + 1 < coords.Length)
                _contentStream.AppendLine($"{coords[i]} {coords[i + 1]} l");
        }

        // Close path
        _contentStream.AppendLine("h");

        // Pass bounding box for gradient support
        var bbox = (minX, minY, maxX - minX, maxY - minY);
        ApplyFillAndStroke(element.Style, bbox);
    }

    private void RenderPath(SvgElement element)
    {
        var d = element.GetAttribute("d");
        if (string.IsNullOrWhiteSpace(d)) return;

        // Parse and render path commands
        var pathCommands = SvgPathParser.Parse(d);
        foreach (var command in pathCommands)
        {
            _contentStream.AppendLine(command);
        }

        // Calculate bounding box for gradient support
        var bbox = SvgPathParser.CalculateBoundingBox(d);
        ApplyFillAndStroke(element.Style, bbox);

        // Render markers if specified
        RenderMarkers(element, d);
    }

    private void RenderText(SvgElement element)
    {
        var x = element.GetDoubleAttribute("x", 0);
        var y = element.GetDoubleAttribute("y", 0);
        var style = element.Style;

        // Check if element has tspan children with dx/dy positioning
        var tspanChildren = element.Children.Where(c => c.ElementType == "tspan").ToList();
        bool hasTspansWithPositioning = tspanChildren.Any(t =>
            t.Attributes.ContainsKey("dx") || t.Attributes.ContainsKey("dy") ||
            t.Attributes.ContainsKey("x") || t.Attributes.ContainsKey("y"));

        // If we have tspans with positioning, render them individually
        if (hasTspansWithPositioning)
        {
            RenderTextWithTspans(element, x, y, style);
            return;
        }

        // Otherwise, render as simple text (original behavior)
        var textContent = GetTextContent(element);
        if (string.IsNullOrWhiteSpace(textContent)) return;

        // Map SVG font to PDF font
        var pdfFont = MapSvgFontToPdf(style.FontFamily, style.FontWeight, style.FontStyle);
        var fontSize = style.FontSize;

        // Calculate estimated text width
        var estimatedWidth = EstimateTextWidth(textContent, fontSize, pdfFont);

        // Handle textLength attribute (scale text to fit specific width)
        double horizontalScale = 100.0; // Default 100% (no scaling)
        var textLength = element.GetDoubleAttribute("textLength", 0);
        if (textLength > 0 && estimatedWidth > 0)
        {
            // Calculate scale factor to fit text to specified length
            horizontalScale = (textLength / estimatedWidth) * 100.0;
        }

        // Handle text-anchor alignment (start, middle, end)
        var textAnchor = element.GetAttribute("text-anchor") ?? "start";
        if (textAnchor == "middle" || textAnchor == "end")
        {
            // Use actual width (with scaling applied) for alignment
            var actualWidth = textLength > 0 ? textLength : estimatedWidth;

            if (textAnchor == "middle")
            {
                x -= actualWidth / 2.0;
            }
            else if (textAnchor == "end")
            {
                x -= actualWidth;
            }
        }

        // Begin text object
        _contentStream.AppendLine("BT");

        // Set font and size
        _contentStream.AppendLine($"/{pdfFont} {fontSize} Tf");

        // Apply horizontal scaling if textLength specified
        if (horizontalScale != 100.0)
        {
            _contentStream.AppendLine($"{horizontalScale} Tz");
        }

        // Set text position
        _contentStream.AppendLine($"{x} {y} Td");

        // Set text color (use fill color)
        if (style.Fill != null && style.Fill != "none")
        {
            var fillColor = ParseColor(style.Fill);
            if (fillColor != null)
            {
                var (r, g, b) = fillColor.Value;
                _contentStream.AppendLine($"{r} {g} {b} rg");
            }
        }

        // Set text opacity if needed
        if (style.FillOpacity < 1.0 || style.Opacity < 1.0)
        {
            var textOpacity = style.FillOpacity * style.Opacity;
            var gsName = AddOpacityGraphicsState(textOpacity, textOpacity);
            _contentStream.AppendLine($"/{gsName} gs");
        }

        // Render text
        var escapedText = EscapePdfString(textContent);
        _contentStream.AppendLine($"({escapedText}) Tj");

        // End text object
        _contentStream.AppendLine("ET");

        // Render text-decoration (underline, overline, line-through)
        var textDecoration = element.GetAttribute("text-decoration");
        if (!string.IsNullOrWhiteSpace(textDecoration) && textDecoration != "none")
        {
            // Use actual width (with textLength if specified)
            var textWidth = textLength > 0 ? textLength : estimatedWidth;
            var lineThickness = fontSize * 0.05; // 5% of font size

            // Set line color same as text color
            if (style.Fill != null && style.Fill != "none")
            {
                var fillColor = ParseColor(style.Fill);
                if (fillColor != null)
                {
                    var (r, g, b) = fillColor.Value;
                    _contentStream.AppendLine($"{r} {g} {b} RG");
                }
            }

            _contentStream.AppendLine($"{lineThickness} w");

            // Parse decoration types (can be space-separated: "underline overline")
            var decorations = textDecoration.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var decoration in decorations)
            {
                switch (decoration.Trim().ToLowerInvariant())
                {
                    case "underline":
                        // Draw line below baseline
                        var underlineY = y - fontSize * 0.1;
                        _contentStream.AppendLine($"{x} {underlineY} m");
                        _contentStream.AppendLine($"{x + textWidth} {underlineY} l");
                        _contentStream.AppendLine("S");
                        break;

                    case "overline":
                        // Draw line above text
                        var overlineY = y + fontSize * 0.9;
                        _contentStream.AppendLine($"{x} {overlineY} m");
                        _contentStream.AppendLine($"{x + textWidth} {overlineY} l");
                        _contentStream.AppendLine("S");
                        break;

                    case "line-through":
                        // Draw line through middle of text
                        var lineThroughY = y + fontSize * 0.3;
                        _contentStream.AppendLine($"{x} {lineThroughY} m");
                        _contentStream.AppendLine($"{x + textWidth} {lineThroughY} l");
                        _contentStream.AppendLine("S");
                        break;
                }
            }
        }

        // TODO: Support tspan elements for multi-line text
        // TODO: Support textPath for text on curves
    }

    private void RenderUse(SvgElement element)
    {
        // Get the href attribute (either href or xlink:href)
        var href = element.GetAttribute("href") ?? element.GetAttribute("xlink:href");
        if (string.IsNullOrWhiteSpace(href)) return;

        // Remove the '#' prefix
        var id = href.StartsWith('#') ? href[1..] : href;

        // Look up the referenced element
        if (!_document.Definitions.TryGetValue(id, out var referencedElement))
            return; // Referenced element not found

        // Get x, y transformations from <use> element
        var x = element.GetDoubleAttribute("x", 0);
        var y = element.GetDoubleAttribute("y", 0);

        // Save graphics state
        _contentStream.AppendLine("q");

        // Apply translation if x or y are non-zero
        if (x != 0 || y != 0)
        {
            _contentStream.AppendLine($"1 0 0 1 {x} {y} cm");
        }

        // Merge styles: use element's style overrides referenced element's style
        var mergedStyle = referencedElement.Style.Clone();
        // TODO: Merge element.Style properties into mergedStyle

        // Clone the referenced element and render it
        // We need to temporarily override its parent and style
        var originalParent = referencedElement.Parent;
        var originalStyle = referencedElement.Style;

        referencedElement.Parent = element;
        referencedElement.Style = mergedStyle;

        RenderElement(referencedElement);

        // Restore original parent and style
        referencedElement.Parent = originalParent;
        referencedElement.Style = originalStyle;

        // Restore graphics state
        _contentStream.AppendLine("Q");
    }

    private void RenderImage(SvgElement element)
    {
        // Get image href (either href or xlink:href)
        var href = element.GetAttribute("href") ?? element.GetAttribute("xlink:href");
        if (string.IsNullOrWhiteSpace(href)) return;

        var x = element.GetDoubleAttribute("x", 0);
        var y = element.GetDoubleAttribute("y", 0);
        var width = element.GetDoubleAttribute("width", 0);
        var height = element.GetDoubleAttribute("height", 0);

        if (width <= 0 || height <= 0) return;

        // Check if it's a data URI
        if (href.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            // Parse data URI: data:image/png;base64,iVBORw0KGgoAAAA...
            var parts = href.Split(',');
            if (parts.Length == 2)
            {
                var imageData = parts[1];
                var mimeType = parts[0];

                // Decode base64 if present
                byte[] decodedData;
                if (mimeType.Contains("base64", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        decodedData = System.Convert.FromBase64String(imageData);
                    }
                    catch
                    {
                        return; // Invalid base64
                    }
                }
                else
                {
                    // URL-encoded data (rare)
                    return; // TODO: Handle URL-encoded image data
                }

                // Add image to XObjects collection
                var imageName = $"Im{++_xObjectCounter}";
                _xObjects[imageName] = decodedData;

                // Render image using PDF Do operator
                _contentStream.AppendLine("q"); // Save state
                _contentStream.AppendLine($"{width} 0 0 {height} {x} {y} cm"); // Position and scale
                _contentStream.AppendLine($"/{imageName} Do"); // Draw image
                _contentStream.AppendLine("Q"); // Restore state
            }
        }
        else if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // External URL - cannot embed without fetching
            // TODO: Could add option to fetch external images
            // For now, we'll skip external images
        }
        else
        {
            // Local file reference - cannot resolve without file system access
            // TODO: Could add option to resolve local file paths
            // For now, we'll skip local file references
        }
    }

    private void ApplyFillAndStroke(SvgStyle style, (double x, double y, double width, double height)? boundingBox = null)
    {
        var hasFill = style.Fill != null && style.Fill != "none";
        var hasStroke = style.Stroke != null && style.Stroke != "none";

        if (!hasFill && !hasStroke) return;

        // Check if fill/stroke is a URL reference (gradient or pattern)
        var fillIsUrl = hasFill && IsUrlReference(style.Fill!);
        var strokeIsUrl = hasStroke && IsUrlReference(style.Stroke!);

        // Determine if URL references are gradients or patterns
        var fillIsGradient = fillIsUrl && IsGradientReference(style.Fill!);
        var strokeIsGradient = strokeIsUrl && IsGradientReference(style.Stroke!);
        var fillIsPattern = fillIsUrl && !fillIsGradient;
        var strokeIsPattern = strokeIsUrl && !strokeIsGradient;

        // Set fill color, gradient, or pattern
        if (hasFill && !fillIsPattern && !fillIsGradient)
        {
            // Solid color fill
            var fillColor = ParseColor(style.Fill!);
            if (fillColor != null)
            {
                var (r, g, b) = fillColor.Value;
                _contentStream.AppendLine($"{r} {g} {b} rg");
            }

            // Set fill opacity if needed
            if (style.FillOpacity < 1.0 || style.Opacity < 1.0)
            {
                var fillOpacity = style.FillOpacity * style.Opacity;
                var strokeOpacity = 1.0; // Not applying to stroke here
                var gsName = AddOpacityGraphicsState(fillOpacity, strokeOpacity);
                _contentStream.AppendLine($"/{gsName} gs");
            }
        }
        else if (fillIsGradient && boundingBox.HasValue)
        {
            // ✅ GRADIENT RENDERING NOW WORKS!
            var gradientId = ExtractUrlId(style.Fill!);
            if (!string.IsNullOrWhiteSpace(gradientId) && _document.Gradients.TryGetValue(gradientId, out var gradient))
            {
                // Generate and add shading to resources
                var shadingName = AddGradientShading(gradient, boundingBox.Value);

                // Render using PDF shading operator
                // Save state, clip to current path, paint with shading, restore
                _contentStream.AppendLine("q");
                _contentStream.AppendLine("W n"); // Clip to current path
                _contentStream.AppendLine($"/{shadingName} sh"); // Paint with shading
                _contentStream.AppendLine("Q");

                // Don't draw the path again
                hasFill = false;
            }
        }
        else if (fillIsGradient)
        {
            // Gradient specified but no bounding box provided
            // This happens for paths where we don't track bounds yet
            // TODO: Implement bounding box tracking for path elements
        }

        // Set stroke color and width
        if (hasStroke && !strokeIsPattern && !strokeIsGradient)
        {
            var strokeColor = ParseColor(style.Stroke!);
            if (strokeColor != null)
            {
                var (r, g, b) = strokeColor.Value;
                _contentStream.AppendLine($"{r} {g} {b} RG");
            }

            _contentStream.AppendLine($"{style.StrokeWidth} w");

            // Line cap
            var lineCap = style.StrokeLineCap switch
            {
                "round" => 1,
                "square" => 2,
                _ => 0 // butt
            };
            _contentStream.AppendLine($"{lineCap} J");

            // Line join
            var lineJoin = style.StrokeLineJoin switch
            {
                "round" => 1,
                "bevel" => 2,
                _ => 0 // miter
            };
            _contentStream.AppendLine($"{lineJoin} j");

            // Miter limit
            _contentStream.AppendLine($"{style.StrokeMiterLimit} M");

            // Dash pattern
            if (!string.IsNullOrWhiteSpace(style.StrokeDashArray) && style.StrokeDashArray != "none")
            {
                var dashPattern = ParseDashArray(style.StrokeDashArray);
                _contentStream.AppendLine($"[{string.Join(" ", dashPattern)}] {style.StrokeDashOffset} d");
            }

            // Set stroke opacity if needed
            if (style.StrokeOpacity < 1.0 || style.Opacity < 1.0)
            {
                var fillOpacity = 1.0; // Not applying to fill here
                var strokeOpacity = style.StrokeOpacity * style.Opacity;
                var gsName = AddOpacityGraphicsState(fillOpacity, strokeOpacity);
                _contentStream.AppendLine($"/{gsName} gs");
            }
        }

        // Handle pattern fills/strokes
        if (fillIsPattern)
        {
            var patternId = ExtractUrlId(style.Fill!);
            if (!string.IsNullOrWhiteSpace(patternId) && _document.Patterns.TryGetValue(patternId, out var pattern))
            {
                // Add pattern to resources
                var patternName = AddPattern(pattern, boundingBox);

                // Set pattern color space and use pattern
                _contentStream.AppendLine("/Pattern cs");
                _contentStream.AppendLine($"/{patternName} scn");
            }
            else
            {
                // Pattern not found, skip fill
                hasFill = false;
            }
        }

        if (strokeIsPattern)
        {
            var patternId = ExtractUrlId(style.Stroke!);
            if (!string.IsNullOrWhiteSpace(patternId) && _document.Patterns.TryGetValue(patternId, out var pattern))
            {
                // Add pattern to resources
                var patternName = AddPattern(pattern, boundingBox);

                // Set pattern color space for stroke and use pattern
                _contentStream.AppendLine("/Pattern CS");
                _contentStream.AppendLine($"/{patternName} SCN");
                _contentStream.AppendLine($"{style.StrokeWidth} w");
            }
            else
            {
                // Pattern not found, skip stroke
                hasStroke = false;
            }
        }

        if (hasFill && hasStroke)
        {
            var fillRule = style.FillRule == "evenodd" ? "*" : "";
            _contentStream.AppendLine($"B{fillRule}");
        }
        else if (hasFill)
        {
            var fillRule = style.FillRule == "evenodd" ? "*" : "";
            _contentStream.AppendLine($"f{fillRule}");
        }
        else if (hasStroke)
        {
            _contentStream.AppendLine("S");
        }
    }

    private void ApplyStroke(SvgStyle style)
    {
        if (style.Stroke == null || style.Stroke == "none") return;

        var strokeColor = ParseColor(style.Stroke);
        if (strokeColor != null)
        {
            var (r, g, b) = strokeColor.Value;
            _contentStream.AppendLine($"{r} {g} {b} RG");
        }

        _contentStream.AppendLine($"{style.StrokeWidth} w");
        _contentStream.AppendLine("S");
    }

    private (double r, double g, double b)? ParseColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color) || color == "none")
            return null;

        color = color.Trim().ToLowerInvariant();

        // Handle hex colors
        if (color.StartsWith('#'))
        {
            return SvgColorParser.ParseHex(color);
        }

        // Handle rgb() colors
        if (color.StartsWith("rgb"))
        {
            return SvgColorParser.ParseRgb(color);
        }

        // Handle named colors
        return SvgColorParser.ParseNamed(color);
    }

    private double[] ParseDashArray(string dashArray)
    {
        var parts = dashArray.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<double>();

        foreach (var part in parts)
        {
            if (double.TryParse(part.Trim(), out var value))
                result.Add(value);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Checks if a string is a URL reference like "url(#id)".
    /// </summary>
    private bool IsUrlReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        return trimmed.StartsWith("url(", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(")");
    }

    /// <summary>
    /// Extracts the ID from a URL reference like "url(#id)" -> "id".
    /// </summary>
    private string? ExtractUrlId(string urlReference)
    {
        if (!IsUrlReference(urlReference))
            return null;

        // Extract content between "url(" and ")"
        var start = urlReference.IndexOf('(') + 1;
        var end = urlReference.LastIndexOf(')');
        if (start >= end)
            return null;

        var id = urlReference.Substring(start, end - start).Trim();

        // Remove leading # if present
        if (id.StartsWith('#'))
            id = id.Substring(1);

        return id;
    }

    /// <summary>
    /// Checks if a URL reference points to a gradient definition.
    /// </summary>
    private bool IsGradientReference(string urlReference)
    {
        var id = ExtractUrlId(urlReference);
        if (string.IsNullOrWhiteSpace(id))
            return false;

        return _document.Gradients.ContainsKey(id);
    }

    /// <summary>
    /// Applies a clipping path from the ClipPaths dictionary.
    /// </summary>
    private void ApplyClippingPath(string clipPathReference)
    {
        var id = ExtractUrlId(clipPathReference);
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (!_document.ClipPaths.TryGetValue(id, out var clipPath))
            return; // Clipping path not found

        // TODO: Handle clipPathUnits (userSpaceOnUse vs objectBoundingBox)
        // For objectBoundingBox, we'd need to scale the clip path to the element's bounding box

        // Render all clipping path elements to create the clipping region
        foreach (var clipElement in clipPath.ClipElements)
        {
            // Build the clipping path geometry
            switch (clipElement.ElementType)
            {
                case "rect":
                    {
                        var x = clipElement.GetDoubleAttribute("x", 0);
                        var y = clipElement.GetDoubleAttribute("y", 0);
                        var width = clipElement.GetDoubleAttribute("width", 0);
                        var height = clipElement.GetDoubleAttribute("height", 0);
                        if (width > 0 && height > 0)
                            _contentStream.AppendLine($"{x} {y} {width} {height} re");
                    }
                    break;

                case "circle":
                    {
                        var cx = clipElement.GetDoubleAttribute("cx", 0);
                        var cy = clipElement.GetDoubleAttribute("cy", 0);
                        var r = clipElement.GetDoubleAttribute("r", 0);
                        if (r > 0)
                            DrawEllipse(cx, cy, r, r);
                    }
                    break;

                case "ellipse":
                    {
                        var cx = clipElement.GetDoubleAttribute("cx", 0);
                        var cy = clipElement.GetDoubleAttribute("cy", 0);
                        var rx = clipElement.GetDoubleAttribute("rx", 0);
                        var ry = clipElement.GetDoubleAttribute("ry", 0);
                        if (rx > 0 && ry > 0)
                            DrawEllipse(cx, cy, rx, ry);
                    }
                    break;

                case "path":
                    {
                        var d = clipElement.GetAttribute("d");
                        if (!string.IsNullOrWhiteSpace(d))
                        {
                            var pathCommands = SvgPathParser.Parse(d);
                            foreach (var command in pathCommands)
                            {
                                _contentStream.AppendLine(command);
                            }
                        }
                    }
                    break;

                // TODO: Support more clipping shapes (polygon, polyline, etc.)
            }
        }

        // Apply the clipping path using the appropriate fill rule
        var clipRule = clipPath.ClipRule == "evenodd" ? "W*" : "W";
        _contentStream.AppendLine(clipRule); // Set clipping path
        _contentStream.AppendLine("n"); // End path without filling/stroking
    }

    /// <summary>
    /// Applies a simple drop shadow effect to an element.
    /// NOTE: This is a simplified implementation - renders shadow as offset copy with opacity.
    /// Full SVG feGaussianBlur is not implemented (would require PDF transparency groups).
    /// </summary>
    /// <param name="element">The element to apply shadow to.</param>
    /// <param name="filter">The filter definition.</param>
    private void ApplySimpleDropShadow(SvgElement element, SvgFilter filter)
    {
        // Look for feDropShadow primitive
        var dropShadow = filter.Primitives.OfType<SvgDropShadow>().FirstOrDefault();
        if (dropShadow == null)
            return; // No drop shadow found

        // Save state for shadow rendering
        _contentStream.AppendLine("q");

        // Apply shadow offset
        _contentStream.AppendLine($"1 0 0 1 {dropShadow.Dx} {dropShadow.Dy} cm");

        // Apply shadow opacity
        if (dropShadow.FloodOpacity < 1.0)
        {
            var gsName = AddOpacityGraphicsState(dropShadow.FloodOpacity, dropShadow.FloodOpacity);
            _contentStream.AppendLine($"/{gsName} gs");
        }

        // Apply shadow color to fill
        var shadowColor = ParseColor(dropShadow.FloodColor);
        if (shadowColor != null)
        {
            var (r, g, b) = shadowColor.Value;
            _contentStream.AppendLine($"{r} {g} {b} rg"); // Fill color
            _contentStream.AppendLine($"{r} {g} {b} RG"); // Stroke color
        }

        // Render shadow copy of element (simplified - just render the element type)
        // NOTE: This doesn't handle all edge cases, but provides basic drop shadow
        switch (element.ElementType)
        {
            case "rect":
                RenderRect(element);
                break;
            case "circle":
                RenderCircle(element);
                break;
            case "ellipse":
                RenderEllipse(element);
                break;
            case "line":
                RenderLine(element);
                break;
            case "polyline":
                RenderPolyline(element);
                break;
            case "polygon":
                RenderPolygon(element);
                break;
            case "path":
                RenderPath(element);
                break;
            // NOTE: Text and other elements not supported for shadows yet
        }

        // Restore state after shadow
        _contentStream.AppendLine("Q");

        // Original element will be rendered normally after this method returns
    }

    /// <summary>
    /// Renders markers (arrow heads, endpoints) on a path.
    /// </summary>
    private void RenderMarkers(SvgElement element, string pathData)
    {
        var style = element.Style;
        var hasMarkerStart = !string.IsNullOrWhiteSpace(style.MarkerStart);
        var hasMarkerMid = !string.IsNullOrWhiteSpace(style.MarkerMid);
        var hasMarkerEnd = !string.IsNullOrWhiteSpace(style.MarkerEnd);

        if (!hasMarkerStart && !hasMarkerMid && !hasMarkerEnd)
            return; // No markers to render

        // Extract path vertices with angles
        var vertices = ExtractPathVertices(pathData);
        if (vertices.Count == 0)
            return;

        // Render marker-start
        if (hasMarkerStart && _document.Markers.TryGetValue(ExtractUrlId(style.MarkerStart!)!, out var markerStart))
        {
            var vertex = vertices[0];
            RenderMarker(markerStart, vertex.x, vertex.y, vertex.outgoingAngle, style.StrokeWidth, isStart: true);
        }

        // Render marker-mid
        if (hasMarkerMid && _document.Markers.TryGetValue(ExtractUrlId(style.MarkerMid!)!, out var markerMid))
        {
            for (int i = 1; i < vertices.Count - 1; i++)
            {
                var vertex = vertices[i];
                // Use average of incoming and outgoing angles for mid markers
                var avgAngle = (vertex.incomingAngle + vertex.outgoingAngle) / 2.0;
                RenderMarker(markerMid, vertex.x, vertex.y, avgAngle, style.StrokeWidth, isStart: false);
            }
        }

        // Render marker-end
        if (hasMarkerEnd && _document.Markers.TryGetValue(ExtractUrlId(style.MarkerEnd!)!, out var markerEnd))
        {
            var vertex = vertices[^1];
            RenderMarker(markerEnd, vertex.x, vertex.y, vertex.incomingAngle, style.StrokeWidth, isStart: false);
        }
    }

    private List<(double x, double y, double incomingAngle, double outgoingAngle)> ExtractPathVertices(string pathData)
    {
        var vertices = new List<(double x, double y, double incomingAngle, double outgoingAngle)>();
        var parser = new Svg.PathDataParser(pathData);

        double currentX = 0, currentY = 0;
        double startX = 0, startY = 0;
        double lastX = 0, lastY = 0;
        double prevX = 0, prevY = 0; // Track previous point for angle calculation

        void AddVertex(double x, double y)
        {
            // Calculate incoming angle from previous point
            var incomingAngle = 0.0;
            if (vertices.Count > 0 || (prevX != x || prevY != y))
            {
                incomingAngle = Math.Atan2(y - prevY, x - prevX) * 180.0 / Math.PI;
            }

            // For now, outgoing angle is same as incoming (will be updated on next point)
            vertices.Add((x, y, incomingAngle, incomingAngle));

            // Update previous outgoing angle
            if (vertices.Count > 1)
            {
                var prev = vertices[^2];
                vertices[^2] = (prev.x, prev.y, prev.incomingAngle, incomingAngle);
            }

            prevX = x;
            prevY = y;
        }

        while (parser.HasMore())
        {
            var command = parser.ReadCommand();
            if (command == ' ') break;

            var isRelative = char.IsLower(command);
            var commandUpper = char.ToUpper(command);

            switch (commandUpper)
            {
                case 'M': // Move to
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x += currentX;
                            y += currentY;
                        }

                        currentX = x;
                        currentY = y;
                        startX = x;
                        startY = y;

                        if (vertices.Count == 0)
                        {
                            AddVertex(x, y);
                        }

                        lastX = x;
                        lastY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'L': // Line to
                case 'H': // Horizontal line
                case 'V': // Vertical line
                {
                    while (true)
                    {
                        double x = currentX, y = currentY;

                        if (commandUpper == 'L')
                        {
                            if (!parser.TryReadNumber(out x)) break;
                            if (!parser.TryReadNumber(out y)) break;
                            if (isRelative) { x += currentX; y += currentY; }
                        }
                        else if (commandUpper == 'H')
                        {
                            if (!parser.TryReadNumber(out x)) break;
                            if (isRelative) x += currentX;
                            y = currentY;
                        }
                        else // 'V'
                        {
                            if (!parser.TryReadNumber(out y)) break;
                            if (isRelative) y += currentY;
                            x = currentX;
                        }

                        AddVertex(x, y);
                        currentX = x;
                        currentY = y;
                        lastX = x;
                        lastY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'C': // Cubic Bézier
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x1)) break;
                        if (!parser.TryReadNumber(out var y1)) break;
                        if (!parser.TryReadNumber(out var x2)) break;
                        if (!parser.TryReadNumber(out var y2)) break;
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x1 += currentX; y1 += currentY;
                            x2 += currentX; y2 += currentY;
                            x += currentX; y += currentY;
                        }

                        AddVertex(x, y);
                        currentX = x;
                        currentY = y;
                        lastX = x;
                        lastY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'S': // Smooth cubic Bézier
                case 'Q': // Quadratic Bézier
                case 'T': // Smooth quadratic Bézier
                {
                    while (true)
                    {
                        double x, y;

                        if (commandUpper == 'S')
                        {
                            if (!parser.TryReadNumber(out var x2)) break;
                            if (!parser.TryReadNumber(out var y2)) break;
                            if (!parser.TryReadNumber(out x)) break;
                            if (!parser.TryReadNumber(out y)) break;
                            if (isRelative) { x += currentX; y += currentY; }
                        }
                        else if (commandUpper == 'Q')
                        {
                            if (!parser.TryReadNumber(out var x1)) break;
                            if (!parser.TryReadNumber(out var y1)) break;
                            if (!parser.TryReadNumber(out x)) break;
                            if (!parser.TryReadNumber(out y)) break;
                            if (isRelative) { x += currentX; y += currentY; }
                        }
                        else // 'T'
                        {
                            if (!parser.TryReadNumber(out x)) break;
                            if (!parser.TryReadNumber(out y)) break;
                            if (isRelative) { x += currentX; y += currentY; }
                        }

                        AddVertex(x, y);
                        currentX = x;
                        currentY = y;
                        lastX = x;
                        lastY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'A': // Elliptical arc
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var rx)) break;
                        if (!parser.TryReadNumber(out var ry)) break;
                        if (!parser.TryReadNumber(out var angle)) break;
                        if (!parser.TryReadNumber(out var largeArcFlag)) break;
                        if (!parser.TryReadNumber(out var sweepFlag)) break;
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x += currentX;
                            y += currentY;
                        }

                        AddVertex(x, y);
                        currentX = x;
                        currentY = y;
                        lastX = x;
                        lastY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'Z': // Close path
                {
                    if (startX != currentX || startY != currentY)
                    {
                        AddVertex(startX, startY);
                    }
                    currentX = startX;
                    currentY = startY;
                    break;
                }
            }
        }

        return vertices;
    }

    private void RenderMarker(SvgMarker marker, double x, double y, double angle, double strokeWidth, bool isStart)
    {
        _contentStream.AppendLine("q"); // Save graphics state

        // 1. Translate to vertex position
        _contentStream.AppendLine($"1 0 0 1 {x} {y} cm");

        // 2. Rotate if orient="auto" or orient="auto-start-reverse"
        if (marker.Orient == "auto" || (marker.Orient == "auto-start-reverse" && isStart))
        {
            var rotationAngle = angle;
            if (marker.Orient == "auto-start-reverse" && isStart)
                rotationAngle += 180;

            var radians = rotationAngle * Math.PI / 180.0;
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);
            _contentStream.AppendLine($"{cos} {sin} {-sin} {cos} 0 0 cm");
        }
        else if (double.TryParse(marker.Orient, out var fixedAngle))
        {
            // Fixed angle
            var radians = fixedAngle * Math.PI / 180.0;
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);
            _contentStream.AppendLine($"{cos} {sin} {-sin} {cos} 0 0 cm");
        }

        // 3. Scale if markerUnits="strokeWidth"
        if (marker.MarkerUnits == "strokeWidth")
        {
            var scale = strokeWidth;
            _contentStream.AppendLine($"{scale} 0 0 {scale} 0 0 cm");
        }

        // 4. Apply viewBox transform if present
        if (marker.ViewBox != null)
        {
            var scaleX = marker.MarkerWidth / marker.ViewBox.Width;
            var scaleY = marker.MarkerHeight / marker.ViewBox.Height;
            _contentStream.AppendLine($"{scaleX} 0 0 {scaleY} {-marker.ViewBox.MinX * scaleX} {-marker.ViewBox.MinY * scaleY} cm");
        }

        // 5. Translate by -refX, -refY
        _contentStream.AppendLine($"1 0 0 1 {-marker.RefX} {-marker.RefY} cm");

        // 6. Render marker content
        foreach (var markerElement in marker.MarkerElements)
        {
            RenderElement(markerElement);
        }

        _contentStream.AppendLine("Q"); // Restore graphics state
    }

    /// <summary>
    /// Gets the text content from a text element (including tspan children).
    /// </summary>
    private string GetTextContent(SvgElement element)
    {
        // Get direct text content if present
        if (!string.IsNullOrWhiteSpace(element.TextContent))
            return element.TextContent;

        // Recursively get text from children (for tspan support)
        var sb = new StringBuilder();
        foreach (var child in element.Children)
        {
            if (child.ElementType == "tspan" || child.ElementType == "text")
            {
                sb.Append(GetTextContent(child));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Maps SVG font family, weight, and style to a PDF base font.
    /// </summary>
    private string MapSvgFontToPdf(string? fontFamily, string fontWeight, string fontStyle)
    {
        // Normalize font family
        var family = fontFamily?.ToLowerInvariant().Trim() ?? "sans-serif";

        // Remove quotes
        family = family.Trim('"', '\'');

        // Determine if bold
        var isBold = fontWeight == "bold" || fontWeight == "700" || fontWeight == "800" || fontWeight == "900";

        // Determine if italic
        var isItalic = fontStyle == "italic" || fontStyle == "oblique";

        // Map to PDF standard fonts (Helvetica, Times, Courier)
        // TODO: This is a simplified mapping. A full implementation would use FontMetrics and PdfBaseFontMapper
        if (family.Contains("serif") && !family.Contains("sans"))
        {
            // Times Roman family
            if (isBold && isItalic) return "Times-BoldItalic";
            if (isBold) return "Times-Bold";
            if (isItalic) return "Times-Italic";
            return "Times-Roman";
        }
        else if (family.Contains("mono") || family.Contains("courier"))
        {
            // Courier family
            if (isBold && isItalic) return "Courier-BoldOblique";
            if (isBold) return "Courier-Bold";
            if (isItalic) return "Courier-Oblique";
            return "Courier";
        }
        else
        {
            // Helvetica family (default for sans-serif)
            if (isBold && isItalic) return "Helvetica-BoldOblique";
            if (isBold) return "Helvetica-Bold";
            if (isItalic) return "Helvetica-Oblique";
            return "Helvetica";
        }
    }

    /// <summary>
    /// Renders text with tspan children that have dx/dy positioning.
    /// Each tspan is rendered separately with its own offset.
    /// </summary>
    private void RenderTextWithTspans(SvgElement element, double baseX, double baseY, SvgStyle style)
    {
        // Map SVG font to PDF font
        var pdfFont = MapSvgFontToPdf(style.FontFamily, style.FontWeight, style.FontStyle);
        var fontSize = style.FontSize;

        // Begin text object
        _contentStream.AppendLine("BT");

        // Set font and size
        _contentStream.AppendLine($"/{pdfFont} {fontSize} Tf");

        // Set text color (use fill color)
        if (style.Fill != null && style.Fill != "none")
        {
            var fillColor = ParseColor(style.Fill);
            if (fillColor != null)
            {
                var (r, g, b) = fillColor.Value;
                _contentStream.AppendLine($"{r} {g} {b} rg");
            }
        }

        // Set text opacity if needed
        if (style.FillOpacity < 1.0 || style.Opacity < 1.0)
        {
            var textOpacity = style.FillOpacity * style.Opacity;
            var gsName = AddOpacityGraphicsState(textOpacity, textOpacity);
            _contentStream.AppendLine($"/{gsName} gs");
        }

        // Track current position
        double currentX = baseX;
        double currentY = baseY;

        // Render direct text content if present
        if (!string.IsNullOrWhiteSpace(element.TextContent))
        {
            _contentStream.AppendLine($"{currentX} {currentY} Td");
            var escapedText = EscapePdfString(element.TextContent);
            _contentStream.AppendLine($"({escapedText}) Tj");

            // Update position (estimate width)
            currentX += EstimateTextWidth(element.TextContent, fontSize, pdfFont);
        }

        // Render each tspan with positioning
        foreach (var tspan in element.Children.Where(c => c.ElementType == "tspan"))
        {
            var tspanText = GetTextContent(tspan);
            if (string.IsNullOrWhiteSpace(tspanText))
                continue;

            // Get tspan positioning
            var dx = tspan.GetDoubleAttribute("dx", 0);
            var dy = tspan.GetDoubleAttribute("dy", 0);

            // Check for absolute positioning
            if (tspan.Attributes.ContainsKey("x"))
            {
                currentX = tspan.GetDoubleAttribute("x", currentX);
            }
            if (tspan.Attributes.ContainsKey("y"))
            {
                currentY = tspan.GetDoubleAttribute("y", currentY);
            }

            // Apply dx/dy offsets
            currentX += dx;
            currentY += dy;

            // Position and render this tspan
            _contentStream.AppendLine($"{currentX} {currentY} Td");
            var escapedText = EscapePdfString(tspanText);
            _contentStream.AppendLine($"({escapedText}) Tj");

            // Update current position for next tspan
            currentX += EstimateTextWidth(tspanText, fontSize, pdfFont);
        }

        // End text object
        _contentStream.AppendLine("ET");
    }

    /// <summary>
    /// Estimates the width of text in PDF points for text-anchor alignment.
    /// Uses font-specific character width estimates for common PDF standard fonts.
    /// </summary>
    private double EstimateTextWidth(string text, double fontSize, string pdfFont)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Average character width as a fraction of fontSize for standard PDF fonts
        // These are empirical values based on typical character widths
        double avgCharWidth = pdfFont switch
        {
            "Courier" or "Courier-Bold" or "Courier-Oblique" or "Courier-BoldOblique"
                => 0.6,  // Monospace fonts have consistent width
            "Times-Roman" or "Times-Bold" or "Times-Italic" or "Times-BoldItalic"
                => 0.45, // Times is relatively narrow
            _ => 0.5     // Helvetica and default: medium width
        };

        // Estimate total width
        return text.Length * fontSize * avgCharWidth;
    }

    /// <summary>
    /// Escapes special characters in a PDF string (parentheses and backslash).
    /// </summary>
    private string EscapePdfString(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            switch (c)
            {
                case '(':
                case ')':
                case '\\':
                    sb.Append('\\');
                    sb.Append(c);
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Adds a gradient shading to the resource collection and returns its name.
    /// </summary>
    /// <param name="gradient">The gradient to add.</param>
    /// <param name="boundingBox">The bounding box of the shape being filled.</param>
    /// <returns>The shading resource name (e.g., "Sh1").</returns>
    private string AddGradientShading(Gradients.SvgGradient gradient, (double x, double y, double width, double height) boundingBox)
    {
        // Generate shading dictionary using SvgGradientToPdf
        var shadingDict = Gradients.SvgGradientToPdf.GenerateShadingDictionary(gradient, boundingBox);

        // Create unique shading name
        var shadingName = $"Sh{++_shadingCounter}";

        // Add to resources collection
        _shadings[shadingName] = shadingDict;

        return shadingName;
    }

    /// <summary>
    /// Adds a graphics state with opacity to the resource collection and returns its name.
    /// </summary>
    /// <param name="fillOpacity">The fill opacity (ca operator), 0.0-1.0.</param>
    /// <param name="strokeOpacity">The stroke opacity (CA operator), 0.0-1.0.</param>
    /// <returns>The graphics state resource name (e.g., "GS1").</returns>
    private string AddOpacityGraphicsState(double fillOpacity, double strokeOpacity)
    {
        // Build graphics state dictionary
        var gsDict = $@"<<
  /Type /ExtGState
  /ca {fillOpacity}
  /CA {strokeOpacity}
>>";

        // Create unique graphics state name
        var gsName = $"GS{++_graphicsStateCounter}";

        // Add to resources collection
        _graphicsStates[gsName] = gsDict;

        return gsName;
    }

    /// <summary>
    /// Adds a pattern to the resource collection and returns its name.
    /// Creates a PDF Type 1 tiling pattern from an SVG pattern definition.
    /// </summary>
    /// <param name="pattern">The SVG pattern to convert.</param>
    /// <param name="boundingBox">The bounding box of the shape being filled (for objectBoundingBox units).</param>
    /// <returns>The pattern resource name (e.g., "P1").</returns>
    private string AddPattern(SvgPattern pattern, (double x, double y, double width, double height)? boundingBox)
    {
        // Calculate pattern tile dimensions
        double tileWidth = pattern.Width;
        double tileHeight = pattern.Height;
        double tileX = pattern.X;
        double tileY = pattern.Y;

        // Handle objectBoundingBox units (default)
        if (pattern.PatternUnits == "objectBoundingBox" && boundingBox.HasValue)
        {
            var bbox = boundingBox.Value;
            tileX = bbox.x + pattern.X * bbox.width;
            tileY = bbox.y + pattern.Y * bbox.height;
            tileWidth = pattern.Width * bbox.width;
            tileHeight = pattern.Height * bbox.height;
        }

        // Render pattern content elements into a content stream
        var patternContent = new StringBuilder();

        // Save original content stream
        var originalStream = _contentStream.ToString();
        var originalLength = _contentStream.Length;

        // Clear content stream to render pattern content
        _contentStream.Clear();

        // Render each pattern element
        foreach (var element in pattern.PatternElements)
        {
            RenderElement(element);
        }

        // Get pattern content
        var patternContentStr = _contentStream.ToString();

        // Restore original content stream
        _contentStream.Clear();
        _contentStream.Append(originalStream);

        // Create Form XObject for pattern content
        var formXObjectName = $"FXO{++_xObjectCounter}";

        // Build Form XObject dictionary
        var formXObject = $@"<<
  /Type /XObject
  /Subtype /Form
  /BBox [0 0 {tileWidth} {tileHeight}]
  /Matrix [1 0 0 1 0 0]
  /Resources << >>
  /Length {Encoding.UTF8.GetByteCount(patternContentStr)}
>>
stream
{patternContentStr}endstream";

        // Add Form XObject to XObjects collection
        // TODO: This should be bytes, but for now we'll store as string and convert later
        _xObjects[formXObjectName] = Encoding.UTF8.GetBytes(formXObject);

        // Create PDF Type 1 tiling pattern dictionary
        var patternDict = $@"<<
  /Type /Pattern
  /PatternType 1
  /PaintType 1
  /TilingType 1
  /BBox [0 0 {tileWidth} {tileHeight}]
  /XStep {tileWidth}
  /YStep {tileHeight}
  /Resources <<
    /XObject << /{formXObjectName} {++_xObjectCounter} 0 R >>
  >>
  /Matrix [1 0 0 1 {tileX} {tileY}]
  /Length {patternContentStr.Length}
>>
stream
{patternContentStr}endstream";

        // Create unique pattern name
        var patternName = $"P{++_patternCounter}";

        // Add to patterns collection
        _patterns[patternName] = patternDict;

        return patternName;
    }
}
