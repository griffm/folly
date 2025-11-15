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

    /// <summary>
    /// Initializes a new instance of the <see cref="SvgToPdfConverter"/> class.
    /// </summary>
    /// <param name="document">The SVG document to convert.</param>
    public SvgToPdfConverter(SvgDocument document)
    {
        _document = document;
    }

    /// <summary>
    /// Converts the SVG document to PDF content stream commands.
    /// Returns the PDF stream content as a string.
    /// </summary>
    public string Convert()
    {
        _contentStream.Clear();
        _transformStack.Clear();
        _styleStack.Clear();

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

        return _contentStream.ToString();
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
            case "text":
                RenderText(element);
                break;
            case "svg":
                // Nested SVG: render children
                break;
            case "use":
                RenderUse(element);
                break;
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

        // If rx or ry are specified, we need rounded corners
        if (rx > 0 || ry > 0)
        {
            // TODO: Implement rounded rectangles using Bézier curves
            // For now, fall back to regular rectangle
            _contentStream.AppendLine($"{x} {y} {width} {height} re");
        }
        else
        {
            _contentStream.AppendLine($"{x} {y} {width} {height} re");
        }

        ApplyFillAndStroke(element.Style);
    }

    private void RenderCircle(SvgElement element)
    {
        var cx = element.GetDoubleAttribute("cx", 0);
        var cy = element.GetDoubleAttribute("cy", 0);
        var r = element.GetDoubleAttribute("r", 0);

        if (r <= 0) return;

        // Draw circle using Bézier curve approximation
        DrawEllipse(cx, cy, r, r);
        ApplyFillAndStroke(element.Style);
    }

    private void RenderEllipse(SvgElement element)
    {
        var cx = element.GetDoubleAttribute("cx", 0);
        var cy = element.GetDoubleAttribute("cy", 0);
        var rx = element.GetDoubleAttribute("rx", 0);
        var ry = element.GetDoubleAttribute("ry", 0);

        if (rx <= 0 || ry <= 0) return;

        DrawEllipse(cx, cy, rx, ry);
        ApplyFillAndStroke(element.Style);
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
    }

    private void RenderText(SvgElement element)
    {
        // TODO: Implement text rendering
        // This is complex - needs font selection, positioning, etc.
    }

    private void RenderUse(SvgElement element)
    {
        // TODO: Implement <use> element (references to other elements)
    }

    private void ApplyFillAndStroke(SvgStyle style)
    {
        var hasFill = style.Fill != null && style.Fill != "none";
        var hasStroke = style.Stroke != null && style.Stroke != "none";

        if (!hasFill && !hasStroke) return;

        // Set fill color
        if (hasFill)
        {
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

        // Set stroke color and width
        if (hasStroke)
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
}
