namespace Folly.Svg;

/// <summary>
/// Represents the computed style properties for an SVG element.
/// Handles CSS cascading, inheritance, and presentation attributes.
/// </summary>
public sealed class SvgStyle
{
    // Fill properties

    /// <summary>
    /// Gets or sets the fill color (default: black).
    /// </summary>
    public string? Fill { get; set; } = "black";

    /// <summary>
    /// Gets or sets the fill opacity (0.0 to 1.0, default: 1.0).
    /// </summary>
    public double FillOpacity { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the fill rule: "nonzero" or "evenodd" (default: "nonzero").
    /// </summary>
    public string FillRule { get; set; } = "nonzero";

    // Stroke properties

    /// <summary>
    /// Gets or sets the stroke color (default: none).
    /// </summary>
    public string? Stroke { get; set; }

    /// <summary>
    /// Gets or sets the stroke width (default: 1.0).
    /// </summary>
    public double StrokeWidth { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the stroke opacity (0.0 to 1.0, default: 1.0).
    /// </summary>
    public double StrokeOpacity { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the stroke line cap: "butt", "round", or "square" (default: "butt").
    /// </summary>
    public string StrokeLineCap { get; set; } = "butt";

    /// <summary>
    /// Gets or sets the stroke line join: "miter", "round", or "bevel" (default: "miter").
    /// </summary>
    public string StrokeLineJoin { get; set; } = "miter";

    /// <summary>
    /// Gets or sets the stroke miter limit (default: 4.0).
    /// </summary>
    public double StrokeMiterLimit { get; set; } = 4.0;

    /// <summary>
    /// Gets or sets the stroke dash array pattern (e.g., "5,5" for dashed lines).
    /// </summary>
    public string? StrokeDashArray { get; set; }

    /// <summary>
    /// Gets or sets the stroke dash offset (default: 0).
    /// </summary>
    public double StrokeDashOffset { get; set; } = 0;

    // Opacity

    /// <summary>
    /// Gets or sets the global opacity (0.0 to 1.0, default: 1.0).
    /// </summary>
    public double Opacity { get; set; } = 1.0;

    // Display and visibility

    /// <summary>
    /// Gets or sets the display property (default: "inline").
    /// </summary>
    public string Display { get; set; } = "inline";

    /// <summary>
    /// Gets or sets the visibility: "visible", "hidden", or "collapse" (default: "visible").
    /// </summary>
    public string Visibility { get; set; } = "visible";

    // Text properties

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public string? FontFamily { get; set; }

    /// <summary>
    /// Gets or sets the font size in pixels (default: 16.0).
    /// </summary>
    public double FontSize { get; set; } = 16.0;

    /// <summary>
    /// Gets or sets the font weight: "normal", "bold", or 100-900 (default: "normal").
    /// </summary>
    public string FontWeight { get; set; } = "normal";

    /// <summary>
    /// Gets or sets the font style: "normal", "italic", or "oblique" (default: "normal").
    /// </summary>
    public string FontStyle { get; set; } = "normal";

    /// <summary>
    /// Gets or sets the text anchor: "start", "middle", or "end" (default: "start").
    /// </summary>
    public string TextAnchor { get; set; } = "start";

    /// <summary>
    /// Gets or sets the text decoration (underline, overline, line-through).
    /// </summary>
    public string? TextDecoration { get; set; }

    // Color (for currentColor references)

    /// <summary>
    /// Gets or sets the current color for currentColor references (default: "black").
    /// </summary>
    public string Color { get; set; } = "black";

    /// <summary>
    /// Creates a copy of this style.
    /// </summary>
    public SvgStyle Clone()
    {
        return new SvgStyle
        {
            Fill = Fill,
            FillOpacity = FillOpacity,
            FillRule = FillRule,
            Stroke = Stroke,
            StrokeWidth = StrokeWidth,
            StrokeOpacity = StrokeOpacity,
            StrokeLineCap = StrokeLineCap,
            StrokeLineJoin = StrokeLineJoin,
            StrokeMiterLimit = StrokeMiterLimit,
            StrokeDashArray = StrokeDashArray,
            StrokeDashOffset = StrokeDashOffset,
            Opacity = Opacity,
            Display = Display,
            Visibility = Visibility,
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontWeight = FontWeight,
            FontStyle = FontStyle,
            TextAnchor = TextAnchor,
            TextDecoration = TextDecoration,
            Color = Color
        };
    }

    /// <summary>
    /// Inherits properties from a parent style (for inheritable properties only).
    /// </summary>
    public void InheritFrom(SvgStyle parent)
    {
        // Inheritable properties
        if (Fill == null) Fill = parent.Fill;
        if (Stroke == null) Stroke = parent.Stroke;

        FontFamily ??= parent.FontFamily;
        if (FontSize == 16.0) FontSize = parent.FontSize; // Only inherit if not explicitly set
        if (FontWeight == "normal") FontWeight = parent.FontWeight;
        if (FontStyle == "normal") FontStyle = parent.FontStyle;
        if (TextAnchor == "start") TextAnchor = parent.TextAnchor;

        Color = parent.Color; // Color is always inherited
    }
}
