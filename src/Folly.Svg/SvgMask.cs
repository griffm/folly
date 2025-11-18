namespace Folly.Svg;

/// <summary>
/// Represents an SVG mask (alpha/luminance masking).
/// Masks use the luminance or alpha values of graphics to control opacity.
/// </summary>
public sealed class SvgMask
{
    /// <summary>
    /// Gets the mask ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the X coordinate of the mask region.
    /// </summary>
    public double X { get; init; } = -0.1;

    /// <summary>
    /// Gets the Y coordinate of the mask region.
    /// </summary>
    public double Y { get; init; } = -0.1;

    /// <summary>
    /// Gets the width of the mask region.
    /// </summary>
    public double Width { get; init; } = 1.2;

    /// <summary>
    /// Gets the height of the mask region.
    /// </summary>
    public double Height { get; init; } = 1.2;

    /// <summary>
    /// Gets the mask units: "userSpaceOnUse" or "objectBoundingBox".
    /// Default: "objectBoundingBox".
    /// </summary>
    public string MaskUnits { get; init; } = "objectBoundingBox";

    /// <summary>
    /// Gets the mask content units: "userSpaceOnUse" or "objectBoundingBox".
    /// Default: "userSpaceOnUse".
    /// </summary>
    public string MaskContentUnits { get; init; } = "userSpaceOnUse";

    /// <summary>
    /// Gets the mask type: "luminance" or "alpha".
    /// Default: "luminance" (uses luminance of mask content).
    /// </summary>
    public string MaskType { get; init; } = "luminance";

    /// <summary>
    /// Gets the elements that define the mask content.
    /// </summary>
    public List<SvgElement> MaskElements { get; init; } = new();
}
