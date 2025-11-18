namespace Folly.Svg;

/// <summary>
/// Represents an SVG marker (arrow heads, endpoints, decorative path markers).
/// Markers are small graphics that can be attached to the vertices of paths, lines, polylines, and polygons.
/// </summary>
public sealed class SvgMarker
{
    /// <summary>
    /// Gets the marker ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the reference X coordinate (where the marker attaches to the path).
    /// Default: 0.
    /// </summary>
    public double RefX { get; init; } = 0;

    /// <summary>
    /// Gets the reference Y coordinate (where the marker attaches to the path).
    /// Default: 0.
    /// </summary>
    public double RefY { get; init; } = 0;

    /// <summary>
    /// Gets the marker width.
    /// Default: 3.
    /// </summary>
    public double MarkerWidth { get; init; } = 3;

    /// <summary>
    /// Gets the marker height.
    /// Default: 3.
    /// </summary>
    public double MarkerHeight { get; init; } = 3;

    /// <summary>
    /// Gets the marker units: "strokeWidth" or "userSpaceOnUse".
    /// - "strokeWidth": marker scales with stroke width (default)
    /// - "userSpaceOnUse": marker uses absolute coordinates
    /// </summary>
    public string MarkerUnits { get; init; } = "strokeWidth";

    /// <summary>
    /// Gets the marker orientation: "auto", "auto-start-reverse", or angle in degrees.
    /// - "auto": marker rotates to match path direction (default)
    /// - "auto-start-reverse": like auto, but reversed for marker-start
    /// - angle: fixed rotation angle (e.g., "45", "90deg")
    /// </summary>
    public string Orient { get; init; } = "auto";

    /// <summary>
    /// Gets the viewBox for the marker coordinate system.
    /// </summary>
    public SvgViewBox? ViewBox { get; init; }

    /// <summary>
    /// Gets the preserveAspectRatio setting.
    /// </summary>
    public string? PreserveAspectRatio { get; init; }

    /// <summary>
    /// Gets the elements that define the marker graphics.
    /// </summary>
    public List<SvgElement> MarkerElements { get; init; } = new();
}
