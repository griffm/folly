namespace Folly.Svg;

/// <summary>
/// Represents an SVG pattern (repeating fill/stroke).
/// Patterns define a tile that repeats to fill a shape.
/// </summary>
public sealed class SvgPattern
{
    /// <summary>
    /// Gets the pattern ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the X coordinate of the pattern tile.
    /// </summary>
    public double X { get; init; } = 0;

    /// <summary>
    /// Gets the Y coordinate of the pattern tile.
    /// </summary>
    public double Y { get; init; } = 0;

    /// <summary>
    /// Gets the width of the pattern tile.
    /// </summary>
    public double Width { get; init; } = 0;

    /// <summary>
    /// Gets the height of the pattern tile.
    /// </summary>
    public double Height { get; init; } = 0;

    /// <summary>
    /// Gets the pattern units: "userSpaceOnUse" or "objectBoundingBox".
    /// Default: "objectBoundingBox".
    /// </summary>
    public string PatternUnits { get; init; } = "objectBoundingBox";

    /// <summary>
    /// Gets the pattern content units: "userSpaceOnUse" or "objectBoundingBox".
    /// Default: "userSpaceOnUse".
    /// </summary>
    public string PatternContentUnits { get; init; } = "userSpaceOnUse";

    /// <summary>
    /// Gets or sets the pattern transformation.
    /// </summary>
    public SvgTransform? PatternTransform { get; set; }

    /// <summary>
    /// Gets the viewBox for the pattern content.
    /// </summary>
    public SvgViewBox? ViewBox { get; init; }

    /// <summary>
    /// Gets the preserveAspectRatio for the pattern.
    /// </summary>
    public string? PreserveAspectRatio { get; init; }

    /// <summary>
    /// Gets the elements that define the pattern content.
    /// </summary>
    public List<SvgElement> PatternElements { get; init; } = new();

    /// <summary>
    /// Gets the reference to another pattern (via xlink:href).
    /// </summary>
    public string? Href { get; init; }
}
