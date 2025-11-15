using System.Xml;
using System.Xml.Linq;

namespace Folly.Svg;

/// <summary>
/// Parses SVG XML into an SVG document model.
/// Handles viewBox, units, coordinate systems, and builds the element tree.
/// </summary>
public static class SvgParser
{
    /// <summary>
    /// Parses an SVG document from a stream.
    /// </summary>
    public static SvgDocument Parse(Stream stream)
    {
        var doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        return ParseDocument(doc);
    }

    /// <summary>
    /// Parses an SVG document from a string.
    /// </summary>
    public static SvgDocument Parse(string svgContent)
    {
        var doc = XDocument.Parse(svgContent, LoadOptions.PreserveWhitespace);
        return ParseDocument(doc);
    }

    private static SvgDocument ParseDocument(XDocument doc)
    {
        var root = doc.Root;
        if (root == null || root.Name.LocalName != "svg")
            throw new InvalidDataException("Invalid SVG: missing <svg> root element");

        // Parse root attributes
        var width = root.Attribute("width")?.Value;
        var height = root.Attribute("height")?.Value;
        var viewBoxStr = root.Attribute("viewBox")?.Value;
        var preserveAspectRatio = root.Attribute("preserveAspectRatio")?.Value ?? "xMidYMid meet";

        // Parse viewBox
        SvgViewBox? viewBox = null;
        if (!string.IsNullOrWhiteSpace(viewBoxStr))
        {
            var parts = SvgLengthParser.ParseList(viewBoxStr, 4);
            if (parts.Length == 4)
            {
                viewBox = new SvgViewBox
                {
                    MinX = parts[0],
                    MinY = parts[1],
                    Width = parts[2],
                    Height = parts[3]
                };
            }
        }

        // Calculate effective dimensions in points
        double effectiveWidthPt = 0;
        double effectiveHeightPt = 0;

        if (!string.IsNullOrWhiteSpace(width))
        {
            effectiveWidthPt = SvgLengthParser.ParseToPt(width, 0);
        }
        else if (viewBox != null)
        {
            effectiveWidthPt = viewBox.Width * 72.0 / 96.0; // Convert user units to points
        }

        if (!string.IsNullOrWhiteSpace(height))
        {
            effectiveHeightPt = SvgLengthParser.ParseToPt(height, 0);
        }
        else if (viewBox != null)
        {
            effectiveHeightPt = viewBox.Height * 72.0 / 96.0;
        }

        // Parse the element tree
        var rootElement = ParseElement(root, null, new SvgStyle());

        // Collect definitions
        var definitions = new Dictionary<string, SvgElement>();
        CollectDefinitions(rootElement, definitions);

        return new SvgDocument
        {
            Root = rootElement,
            ViewBox = viewBox,
            Width = width,
            Height = height,
            EffectiveWidthPt = effectiveWidthPt,
            EffectiveHeightPt = effectiveHeightPt,
            PreserveAspectRatio = preserveAspectRatio,
            Definitions = definitions
        };
    }

    private static SvgElement ParseElement(XElement xmlElement, SvgElement? parent, SvgStyle parentStyle)
    {
        var elementType = xmlElement.Name.LocalName;

        var element = new SvgElement
        {
            ElementType = elementType,
            Parent = parent
        };

        // Parse attributes
        foreach (var attr in xmlElement.Attributes())
        {
            var name = attr.Name.LocalName;
            var value = attr.Value;

            if (name == "id")
            {
                element.Id = value;
            }
            else
            {
                element.Attributes[name] = value;
            }
        }

        // Parse text content (for text elements)
        if (elementType == "text" || elementType == "tspan")
        {
            element.TextContent = xmlElement.Value;
        }

        // Compute style (inherit from parent, apply own attributes/style)
        element.Style = ComputeStyle(element, parentStyle);

        // Parse transform
        var transformAttr = element.GetAttribute("transform");
        if (!string.IsNullOrWhiteSpace(transformAttr))
        {
            element.Transform = SvgTransformParser.Parse(transformAttr);
        }

        // Parse children recursively
        foreach (var child in xmlElement.Elements())
        {
            var childElement = ParseElement(child, element, element.Style);
            element.Children.Add(childElement);
        }

        return element;
    }

    private static SvgStyle ComputeStyle(SvgElement element, SvgStyle parentStyle)
    {
        var style = parentStyle.Clone();

        // Apply presentation attributes (lower priority)
        ApplyPresentationAttribute(element, style, "fill");
        ApplyPresentationAttribute(element, style, "stroke");
        ApplyPresentationAttribute(element, style, "stroke-width");
        ApplyPresentationAttribute(element, style, "stroke-linecap");
        ApplyPresentationAttribute(element, style, "stroke-linejoin");
        ApplyPresentationAttribute(element, style, "stroke-miterlimit");
        ApplyPresentationAttribute(element, style, "stroke-dasharray");
        ApplyPresentationAttribute(element, style, "stroke-dashoffset");
        ApplyPresentationAttribute(element, style, "fill-opacity");
        ApplyPresentationAttribute(element, style, "stroke-opacity");
        ApplyPresentationAttribute(element, style, "opacity");
        ApplyPresentationAttribute(element, style, "fill-rule");
        ApplyPresentationAttribute(element, style, "display");
        ApplyPresentationAttribute(element, style, "visibility");
        ApplyPresentationAttribute(element, style, "font-family");
        ApplyPresentationAttribute(element, style, "font-size");
        ApplyPresentationAttribute(element, style, "font-weight");
        ApplyPresentationAttribute(element, style, "font-style");
        ApplyPresentationAttribute(element, style, "text-anchor");
        ApplyPresentationAttribute(element, style, "text-decoration");
        ApplyPresentationAttribute(element, style, "color");

        // Apply style attribute (higher priority)
        var styleAttr = element.GetAttribute("style");
        if (!string.IsNullOrWhiteSpace(styleAttr))
        {
            ParseInlineStyle(styleAttr, style);
        }

        return style;
    }

    private static void ApplyPresentationAttribute(SvgElement element, SvgStyle style, string attrName)
    {
        var value = element.GetAttribute(attrName);
        if (value == null) return;

        switch (attrName)
        {
            case "fill":
                style.Fill = value;
                break;
            case "stroke":
                style.Stroke = value;
                break;
            case "stroke-width":
                style.StrokeWidth = SvgLengthParser.Parse(value, 1.0);
                break;
            case "stroke-linecap":
                style.StrokeLineCap = value;
                break;
            case "stroke-linejoin":
                style.StrokeLineJoin = value;
                break;
            case "stroke-miterlimit":
                if (double.TryParse(value, out var miterLimit))
                    style.StrokeMiterLimit = miterLimit;
                break;
            case "stroke-dasharray":
                style.StrokeDashArray = value;
                break;
            case "stroke-dashoffset":
                if (double.TryParse(value, out var dashOffset))
                    style.StrokeDashOffset = dashOffset;
                break;
            case "fill-opacity":
                if (double.TryParse(value, out var fillOpacity))
                    style.FillOpacity = fillOpacity;
                break;
            case "stroke-opacity":
                if (double.TryParse(value, out var strokeOpacity))
                    style.StrokeOpacity = strokeOpacity;
                break;
            case "opacity":
                if (double.TryParse(value, out var opacity))
                    style.Opacity = opacity;
                break;
            case "fill-rule":
                style.FillRule = value;
                break;
            case "display":
                style.Display = value;
                break;
            case "visibility":
                style.Visibility = value;
                break;
            case "font-family":
                style.FontFamily = value;
                break;
            case "font-size":
                style.FontSize = SvgLengthParser.Parse(value, 16.0);
                break;
            case "font-weight":
                style.FontWeight = value;
                break;
            case "font-style":
                style.FontStyle = value;
                break;
            case "text-anchor":
                style.TextAnchor = value;
                break;
            case "text-decoration":
                style.TextDecoration = value;
                break;
            case "color":
                style.Color = value;
                break;
        }
    }

    private static void ParseInlineStyle(string styleAttr, SvgStyle style)
    {
        // Parse CSS inline style: "fill: red; stroke: blue; stroke-width: 2"
        var declarations = styleAttr.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var declaration in declarations)
        {
            var parts = declaration.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2) continue;

            var property = parts[0].Trim();
            var value = parts[1].Trim();

            // Apply to style (same logic as presentation attributes)
            var fakeElement = new SvgElement { ElementType = "temp" };
            fakeElement.Attributes[property] = value;
            ApplyPresentationAttribute(fakeElement, style, property);
        }
    }

    private static void CollectDefinitions(SvgElement element, Dictionary<string, SvgElement> definitions)
    {
        // Add element to definitions if it has an ID
        if (!string.IsNullOrWhiteSpace(element.Id))
        {
            definitions[element.Id] = element;
        }

        // Recursively collect from children
        foreach (var child in element.Children)
        {
            CollectDefinitions(child, definitions);
        }
    }
}
