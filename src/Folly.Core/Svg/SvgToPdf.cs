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
#pragma warning disable CS0414 // Field is assigned but never used (infrastructure for future features)
    private int _patternCounter = 0;
    private int _xObjectCounter = 0;
    private int _graphicsStateCounter = 0;
#pragma warning restore CS0414

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
            case "defs":
                // Definitions: skip rendering (elements are referenced via <use>)
                return;
            // TODO: More element types (image, tspan, etc.)
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

        // Move to first point
        _contentStream.AppendLine($"{coords[0]} {coords[1]} m");

        // Line to subsequent points
        for (int i = 2; i < coords.Length; i += 2)
        {
            if (i + 1 < coords.Length)
                _contentStream.AppendLine($"{coords[i]} {coords[i + 1]} l");
        }

        ApplyStroke(element.Style);
    }

    private void RenderPolygon(SvgElement element)
    {
        var points = element.GetAttribute("points");
        if (string.IsNullOrWhiteSpace(points)) return;

        var coords = SvgLengthParser.ParseList(points);
        if (coords.Length < 2) return;

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

        ApplyFillAndStroke(element.Style);
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

        ApplyFillAndStroke(element.Style);

        // Render markers if specified
        RenderMarkers(element, d);
    }

    private void RenderText(SvgElement element)
    {
        // Get text content (either from text content or tspan children)
        var textContent = GetTextContent(element);
        if (string.IsNullOrWhiteSpace(textContent)) return;

        var x = element.GetDoubleAttribute("x", 0);
        var y = element.GetDoubleAttribute("y", 0);
        var style = element.Style;

        // Begin text object
        _contentStream.AppendLine("BT");

        // Map SVG font to PDF font
        var pdfFont = MapSvgFontToPdf(style.FontFamily, style.FontWeight, style.FontStyle);
        var fontSize = style.FontSize;

        // Set font and size
        _contentStream.AppendLine($"/{pdfFont} {fontSize} Tf");

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
            // TODO: Set text opacity in graphics state
        }

        // Render text
        // TODO: Escape special characters in PDF strings (parentheses, backslash)
        var escapedText = EscapePdfString(textContent);
        _contentStream.AppendLine($"({escapedText}) Tj");

        // End text object
        _contentStream.AppendLine("ET");

        // TODO: Support text-anchor (start, middle, end)
        // TODO: Support tspan elements for multi-line text
        // TODO: Support text-decoration (underline, overline, line-through)
        // TODO: Support text transforms (rotate, etc.)
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
                var opacity = style.FillOpacity * style.Opacity;
                // TODO: Set CA (fill opacity) in graphics state
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
        }

        // Draw path
        // TODO: Pattern fills/strokes require creating PDF pattern resources and using /Pattern color space
        // For now, we'll render the path with solid colors only
        if (fillIsPattern || strokeIsPattern)
        {
            // TODO: Implement pattern fill/stroke using PDF tiling patterns (Type 1)
            // This requires: 1) Creating XObject Form for pattern tile
            //                2) Setting /Pattern color space
            //                3) Using scn/SCN operators with pattern name
            // For now, fall back to basic rendering
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

        // TODO: Implement marker rendering - this is complex and requires:
        // 1. Parse path data to extract all vertices (start, mid-points, end)
        // 2. Calculate the angle/direction at each vertex
        // 3. For each marker position:
        //    a. Save graphics state (q)
        //    b. Translate to vertex position
        //    c. Rotate to match path direction (if orient="auto")
        //    d. Scale based on markerUnits (strokeWidth vs userSpaceOnUse)
        //    e. Translate by -refX, -refY
        //    f. Render marker content
        //    g. Restore graphics state (Q)
        //
        // Key challenges:
        // - Path vertices extraction (need to track M, L, C, Q, A endpoints)
        // - Angle calculation (use atan2 of incoming/outgoing tangents)
        // - Orient="auto" vs orient="auto-start-reverse" vs fixed angle
        // - MarkerUnits="strokeWidth" requires multiplying by current stroke width
        //
        // For now, markers are parsed and stored but not yet rendered
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
}
