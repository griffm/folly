namespace Folly.Svg.Gradients;

/// <summary>
/// Base class for SVG gradients (linear and radial).
/// Gradients are reusable fill/stroke patterns stored in the defs section.
/// </summary>
public abstract class SvgGradient
{
    /// <summary>
    /// Gets the gradient ID (from the 'id' attribute).
    /// Used to reference the gradient via url(#id).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the gradient type ("linearGradient" or "radialGradient").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the gradient color stops.
    /// </summary>
    public List<SvgGradientStop> Stops { get; init; } = new();

    /// <summary>
    /// Gets the gradient units: "objectBoundingBox" or "userSpaceOnUse".
    /// Default: "objectBoundingBox" (coordinates are percentages 0-1).
    /// </summary>
    public string GradientUnits { get; init; } = "objectBoundingBox";

    /// <summary>
    /// Gets or sets the gradient transformation.
    /// </summary>
    public SvgTransform? GradientTransform { get; set; }

    /// <summary>
    /// Gets the spread method: "pad", "reflect", or "repeat".
    /// Default: "pad" (extend the edge colors).
    /// </summary>
    public string SpreadMethod { get; init; } = "pad";

    /// <summary>
    /// Gets the reference to another gradient (via xlink:href).
    /// Used for inheriting gradient properties.
    /// </summary>
    public string? Href { get; init; }
}

/// <summary>
/// Represents a linear gradient (fills from point 1 to point 2).
/// </summary>
public sealed class SvgLinearGradient : SvgGradient
{
    /// <summary>
    /// Gets the X coordinate of the start point.
    /// Default: 0 (left edge in objectBoundingBox).
    /// </summary>
    public double X1 { get; init; } = 0;

    /// <summary>
    /// Gets the Y coordinate of the start point.
    /// Default: 0 (top edge in objectBoundingBox).
    /// </summary>
    public double Y1 { get; init; } = 0;

    /// <summary>
    /// Gets the X coordinate of the end point.
    /// Default: 1 (right edge in objectBoundingBox).
    /// </summary>
    public double X2 { get; init; } = 1;

    /// <summary>
    /// Gets the Y coordinate of the end point.
    /// Default: 0 (creates horizontal gradient).
    /// </summary>
    public double Y2 { get; init; } = 0;
}

/// <summary>
/// Represents a radial gradient (fills from focal point to outer circle).
/// </summary>
public sealed class SvgRadialGradient : SvgGradient
{
    /// <summary>
    /// Gets the X coordinate of the circle center.
    /// Default: 0.5 (center in objectBoundingBox).
    /// </summary>
    public double Cx { get; init; } = 0.5;

    /// <summary>
    /// Gets the Y coordinate of the circle center.
    /// Default: 0.5 (center in objectBoundingBox).
    /// </summary>
    public double Cy { get; init; } = 0.5;

    /// <summary>
    /// Gets the radius of the gradient circle.
    /// Default: 0.5 (fills to edge in objectBoundingBox).
    /// </summary>
    public double R { get; init; } = 0.5;

    /// <summary>
    /// Gets the X coordinate of the focal point.
    /// Default: same as Cx.
    /// </summary>
    public double Fx { get; init; }

    /// <summary>
    /// Gets the Y coordinate of the focal point.
    /// Default: same as Cy.
    /// </summary>
    public double Fy { get; init; }

    /// <summary>
    /// Gets the focal point radius (SVG 2.0).
    /// Default: 0.
    /// </summary>
    public double Fr { get; init; } = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="SvgRadialGradient"/> class.
    /// Sets focal point to center if not specified.
    /// </summary>
    public SvgRadialGradient()
    {
        Fx = Cx;
        Fy = Cy;
    }
}

/// <summary>
/// Represents a color stop in a gradient.
/// </summary>
public sealed class SvgGradientStop
{
    /// <summary>
    /// Gets the offset position (0.0 to 1.0).
    /// 0.0 = start, 1.0 = end.
    /// </summary>
    public required double Offset { get; init; }

    /// <summary>
    /// Gets the stop color.
    /// </summary>
    public required string Color { get; init; }

    /// <summary>
    /// Gets the stop opacity (0.0 to 1.0).
    /// Default: 1.0 (fully opaque).
    /// </summary>
    public double Opacity { get; init; } = 1.0;
}
