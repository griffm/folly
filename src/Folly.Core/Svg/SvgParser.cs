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

        // Parse gradients
        var gradients = new Dictionary<string, Gradients.SvgGradient>();
        CollectGradients(root, gradients);

        return new SvgDocument
        {
            Root = rootElement,
            ViewBox = viewBox,
            Width = width,
            Height = height,
            EffectiveWidthPt = effectiveWidthPt,
            EffectiveHeightPt = effectiveHeightPt,
            PreserveAspectRatio = preserveAspectRatio,
            Definitions = definitions,
            Gradients = gradients
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

    private static void CollectGradients(XElement root, Dictionary<string, Gradients.SvgGradient> gradients)
    {
        // Find all linearGradient and radialGradient elements
        var linearGradients = root.Descendants().Where(e => e.Name.LocalName == "linearGradient");
        var radialGradients = root.Descendants().Where(e => e.Name.LocalName == "radialGradient");

        foreach (var elem in linearGradients)
        {
            var gradient = ParseLinearGradient(elem);
            if (gradient != null && !string.IsNullOrWhiteSpace(gradient.Id))
            {
                gradients[gradient.Id] = gradient;
            }
        }

        foreach (var elem in radialGradients)
        {
            var gradient = ParseRadialGradient(elem);
            if (gradient != null && !string.IsNullOrWhiteSpace(gradient.Id))
            {
                gradients[gradient.Id] = gradient;
            }
        }
    }

    private static Gradients.SvgLinearGradient? ParseLinearGradient(XElement elem)
    {
        var id = elem.Attribute("id")?.Value;
        if (string.IsNullOrWhiteSpace(id)) return null;

        var x1 = ParseDoubleAttr(elem, "x1", 0);
        var y1 = ParseDoubleAttr(elem, "y1", 0);
        var x2 = ParseDoubleAttr(elem, "x2", 1);
        var y2 = ParseDoubleAttr(elem, "y2", 0);

        var gradient = new Gradients.SvgLinearGradient
        {
            Id = id,
            Type = "linearGradient",
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            GradientUnits = elem.Attribute("gradientUnits")?.Value ?? "objectBoundingBox",
            SpreadMethod = elem.Attribute("spreadMethod")?.Value ?? "pad",
            Href = elem.Attribute(XName.Get("href", "http://www.w3.org/1999/xlink"))?.Value
        };

        // Parse gradientTransform
        var transformAttr = elem.Attribute("gradientTransform")?.Value;
        if (!string.IsNullOrWhiteSpace(transformAttr))
        {
            gradient.GradientTransform = SvgTransformParser.Parse(transformAttr);
        }

        // Parse stops
        var stops = elem.Elements().Where(e => e.Name.LocalName == "stop");
        foreach (var stop in stops)
        {
            var gradientStop = ParseGradientStop(stop);
            if (gradientStop != null)
            {
                gradient.Stops.Add(gradientStop);
            }
        }

        return gradient;
    }

    private static Gradients.SvgRadialGradient? ParseRadialGradient(XElement elem)
    {
        var id = elem.Attribute("id")?.Value;
        if (string.IsNullOrWhiteSpace(id)) return null;

        var cx = ParseDoubleAttr(elem, "cx", 0.5);
        var cy = ParseDoubleAttr(elem, "cy", 0.5);
        var r = ParseDoubleAttr(elem, "r", 0.5);
        var fx = ParseDoubleAttr(elem, "fx", cx);
        var fy = ParseDoubleAttr(elem, "fy", cy);
        var fr = ParseDoubleAttr(elem, "fr", 0);

        var gradient = new Gradients.SvgRadialGradient
        {
            Id = id,
            Type = "radialGradient",
            Cx = cx,
            Cy = cy,
            R = r,
            Fx = fx,
            Fy = fy,
            Fr = fr,
            GradientUnits = elem.Attribute("gradientUnits")?.Value ?? "objectBoundingBox",
            SpreadMethod = elem.Attribute("spreadMethod")?.Value ?? "pad",
            Href = elem.Attribute(XName.Get("href", "http://www.w3.org/1999/xlink"))?.Value
        };

        // Parse gradientTransform
        var transformAttr = elem.Attribute("gradientTransform")?.Value;
        if (!string.IsNullOrWhiteSpace(transformAttr))
        {
            gradient.GradientTransform = SvgTransformParser.Parse(transformAttr);
        }

        // Parse stops
        var stops = elem.Elements().Where(e => e.Name.LocalName == "stop");
        foreach (var stop in stops)
        {
            var gradientStop = ParseGradientStop(stop);
            if (gradientStop != null)
            {
                gradient.Stops.Add(gradientStop);
            }
        }

        return gradient;
    }

    private static Gradients.SvgGradientStop? ParseGradientStop(XElement elem)
    {
        var offsetStr = elem.Attribute("offset")?.Value;
        if (string.IsNullOrWhiteSpace(offsetStr)) return null;

        // Parse offset (can be percentage like "50%" or decimal like "0.5")
        double offset;
        if (offsetStr.EndsWith('%'))
        {
            if (double.TryParse(offsetStr.TrimEnd('%'), out var pct))
                offset = pct / 100.0;
            else
                return null;
        }
        else
        {
            if (!double.TryParse(offsetStr, out offset))
                return null;
        }

        // Parse stop-color and stop-opacity
        var color = elem.Attribute("stop-color")?.Value ?? "black";
        var opacity = 1.0;

        var opacityStr = elem.Attribute("stop-opacity")?.Value;
        if (!string.IsNullOrWhiteSpace(opacityStr))
        {
            double.TryParse(opacityStr, out opacity);
        }

        // Also check style attribute for stop-color and stop-opacity
        var styleAttr = elem.Attribute("style")?.Value;
        if (!string.IsNullOrWhiteSpace(styleAttr))
        {
            var declarations = styleAttr.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var declaration in declarations)
            {
                var parts = declaration.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    var property = parts[0].Trim();
                    var value = parts[1].Trim();

                    if (property == "stop-color")
                        color = value;
                    else if (property == "stop-opacity")
                        double.TryParse(value, out opacity);
                }
            }
        }

        return new Gradients.SvgGradientStop
        {
            Offset = offset,
            Color = color,
            Opacity = opacity
        };
    }

    private static double ParseDoubleAttr(XElement elem, string attrName, double defaultValue)
    {
        var value = elem.Attribute(attrName)?.Value;
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;

        // Handle percentages
        if (value.EndsWith('%'))
        {
            if (double.TryParse(value.TrimEnd('%'), out var pct))
                return pct / 100.0;
        }

        if (double.TryParse(value, out var result))
            return result;

        return defaultValue;
    }
}
