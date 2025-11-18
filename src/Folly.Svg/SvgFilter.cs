namespace Folly.Svg;

/// <summary>
/// Represents an SVG filter (effects like blur, shadows, color transforms).
/// PDF has limited filter support - we focus on PDF-compatible effects.
/// </summary>
public sealed class SvgFilter
{
    /// <summary>
    /// Gets the filter ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the X coordinate of the filter region.
    /// Default: -10% (filter region extends beyond element bounds).
    /// </summary>
    public double X { get; init; } = -0.1;

    /// <summary>
    /// Gets the Y coordinate of the filter region.
    /// Default: -10%.
    /// </summary>
    public double Y { get; init; } = -0.1;

    /// <summary>
    /// Gets the width of the filter region.
    /// Default: 120% (filter region extends beyond element bounds).
    /// </summary>
    public double Width { get; init; } = 1.2;

    /// <summary>
    /// Gets the height of the filter region.
    /// Default: 120%.
    /// </summary>
    public double Height { get; init; } = 1.2;

    /// <summary>
    /// Gets the filter units: "userSpaceOnUse" or "objectBoundingBox".
    /// Default: "objectBoundingBox".
    /// </summary>
    public string FilterUnits { get; init; } = "objectBoundingBox";

    /// <summary>
    /// Gets the primitive units: "userSpaceOnUse" or "objectBoundingBox".
    /// Default: "userSpaceOnUse".
    /// </summary>
    public string PrimitiveUnits { get; init; } = "userSpaceOnUse";

    /// <summary>
    /// Gets the list of filter primitives (feGaussianBlur, feDropShadow, etc.).
    /// </summary>
    public List<SvgFilterPrimitive> Primitives { get; init; } = new();
}

/// <summary>
/// Base class for SVG filter primitives.
/// </summary>
public abstract class SvgFilterPrimitive
{
    /// <summary>
    /// Gets the filter primitive type (e.g., "feGaussianBlur", "feDropShadow").
    /// </summary>
    public required string PrimitiveType { get; init; }

    /// <summary>
    /// Gets the input source ("SourceGraphic", "SourceAlpha", or another primitive's result).
    /// </summary>
    public string? In { get; init; }

    /// <summary>
    /// Gets the result name (for chaining filter primitives).
    /// </summary>
    public string? Result { get; init; }
}

/// <summary>
/// Represents a Gaussian blur filter primitive (feGaussianBlur).
/// PDF-compatible via transparency groups with blur.
/// </summary>
public sealed class SvgGaussianBlur : SvgFilterPrimitive
{
    /// <summary>
    /// Gets the standard deviation for the blur (larger = more blur).
    /// Can be a single value (applied to both X and Y) or two values "stdDeviationX stdDeviationY".
    /// </summary>
    public required string StdDeviation { get; init; }

    /// <summary>
    /// Gets the edge mode: "duplicate", "wrap", or "none".
    /// Default: "duplicate".
    /// </summary>
    public string EdgeMode { get; init; } = "duplicate";
}

/// <summary>
/// Represents a drop shadow filter primitive (feDropShadow).
/// PDF-compatible via offset + blur + composite.
/// </summary>
public sealed class SvgDropShadow : SvgFilterPrimitive
{
    /// <summary>
    /// Gets the X offset of the shadow.
    /// Default: 2.
    /// </summary>
    public double Dx { get; init; } = 2;

    /// <summary>
    /// Gets the Y offset of the shadow.
    /// Default: 2.
    /// </summary>
    public double Dy { get; init; } = 2;

    /// <summary>
    /// Gets the blur amount (standard deviation).
    /// Default: 2.
    /// </summary>
    public double StdDeviation { get; init; } = 2;

    /// <summary>
    /// Gets the shadow color.
    /// Default: "black".
    /// </summary>
    public string FloodColor { get; init; } = "black";

    /// <summary>
    /// Gets the shadow opacity.
    /// Default: 1.0.
    /// </summary>
    public double FloodOpacity { get; init; } = 1.0;
}

/// <summary>
/// Represents a blend filter primitive (feBlend).
/// PDF-compatible via blend modes.
/// </summary>
public sealed class SvgBlend : SvgFilterPrimitive
{
    /// <summary>
    /// Gets the second input for blending.
    /// </summary>
    public string? In2 { get; init; }

    /// <summary>
    /// Gets the blend mode: "normal", "multiply", "screen", "overlay", "darken", "lighten", etc.
    /// Default: "normal".
    /// </summary>
    public string Mode { get; init; } = "normal";
}
