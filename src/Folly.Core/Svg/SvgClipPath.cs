namespace Folly.Svg;

/// <summary>
/// Represents an SVG clipping path.
/// Clipping paths define a region that restricts rendering.
/// </summary>
public sealed class SvgClipPath
{
    /// <summary>
    /// Gets the clip path ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the clip path units: "userSpaceOnUse" or "objectBoundingBox".
    /// Default: "userSpaceOnUse".
    /// </summary>
    public string ClipPathUnits { get; init; } = "userSpaceOnUse";

    /// <summary>
    /// Gets the elements that define the clipping region.
    /// </summary>
    public List<SvgElement> ClipElements { get; init; } = new();

    /// <summary>
    /// Gets the clip rule: "nonzero" or "evenodd".
    /// Default: "nonzero".
    /// </summary>
    public string ClipRule { get; init; } = "nonzero";
}
