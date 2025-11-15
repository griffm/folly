namespace Folly.Svg;

/// <summary>
/// Represents a parsed SVG document ready for conversion to PDF primitives.
/// Unlike raster images which are embedded as-is, SVG must be converted to PDF vector graphics.
/// </summary>
public sealed class SvgDocument
{
    /// <summary>
    /// Gets the root SVG element.
    /// </summary>
    public required SvgElement Root { get; init; }

    /// <summary>
    /// Gets the viewBox (user coordinate system).
    /// Format: minX minY width height
    /// </summary>
    public SvgViewBox? ViewBox { get; init; }

    /// <summary>
    /// Gets the declared width (may include units like px, pt, cm, %, etc.).
    /// </summary>
    public string? Width { get; init; }

    /// <summary>
    /// Gets the declared height (may include units).
    /// </summary>
    public string? Height { get; init; }

    /// <summary>
    /// Gets the effective width in points for PDF rendering.
    /// Falls back to ViewBox width if declared width is not specified.
    /// </summary>
    public double EffectiveWidthPt { get; init; }

    /// <summary>
    /// Gets the effective height in points for PDF rendering.
    /// Falls back to ViewBox height if declared height is not specified.
    /// </summary>
    public double EffectiveHeightPt { get; init; }

    /// <summary>
    /// Gets the preserveAspectRatio setting.
    /// Controls how the SVG scales and aligns within its viewport.
    /// </summary>
    public string? PreserveAspectRatio { get; init; }

    /// <summary>
    /// Gets the definitions (defs) dictionary for reusable elements.
    /// Keys are element IDs, values are the defined elements (gradients, patterns, symbols, etc.).
    /// </summary>
    public Dictionary<string, SvgElement> Definitions { get; init; } = new();

    /// <summary>
    /// Gets the gradients dictionary (linear and radial gradients).
    /// Keys are gradient IDs, values are gradient objects.
    /// </summary>
    public Dictionary<string, Gradients.SvgGradient> Gradients { get; init; } = new();

    /// <summary>
    /// Gets the clipping paths dictionary.
    /// Keys are clipPath IDs, values are clipPath objects.
    /// </summary>
    public Dictionary<string, SvgClipPath> ClipPaths { get; init; } = new();

    /// <summary>
    /// Gets the patterns dictionary (repeating fills/strokes).
    /// Keys are pattern IDs, values are pattern objects.
    /// </summary>
    public Dictionary<string, SvgPattern> Patterns { get; init; } = new();

    /// <summary>
    /// Gets the masks dictionary (alpha/luminance masking).
    /// Keys are mask IDs, values are mask objects.
    /// </summary>
    public Dictionary<string, SvgMask> Masks { get; init; } = new();

    /// <summary>
    /// Parses an SVG document from a byte array.
    /// </summary>
    public static SvgDocument Parse(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return Parse(stream);
    }

    /// <summary>
    /// Parses an SVG document from a stream.
    /// </summary>
    public static SvgDocument Parse(Stream stream)
    {
        return SvgParser.Parse(stream);
    }

    /// <summary>
    /// Parses an SVG document from a string.
    /// </summary>
    public static SvgDocument Parse(string svgContent)
    {
        return SvgParser.Parse(svgContent);
    }
}

/// <summary>
/// Represents the SVG viewBox coordinate system.
/// </summary>
public sealed class SvgViewBox
{
    /// <summary>
    /// Gets the minimum X coordinate of the viewBox.
    /// </summary>
    public required double MinX { get; init; }

    /// <summary>
    /// Gets the minimum Y coordinate of the viewBox.
    /// </summary>
    public required double MinY { get; init; }

    /// <summary>
    /// Gets the width of the viewBox.
    /// </summary>
    public required double Width { get; init; }

    /// <summary>
    /// Gets the height of the viewBox.
    /// </summary>
    public required double Height { get; init; }

    /// <summary>
    /// Returns a string representation of the viewBox.
    /// </summary>
    public override string ToString() => $"{MinX} {MinY} {Width} {Height}";
}
