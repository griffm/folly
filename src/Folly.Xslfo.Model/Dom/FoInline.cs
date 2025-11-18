namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:inline element.
/// An inline formatting object used to style inline text portions within a block.
/// It does not cause line breaks and flows with the surrounding content.
/// </summary>
public sealed class FoInline : FoElement
{
    /// <inheritdoc/>
    public override string Name => "inline";

    /// <summary>
    /// Gets the font family, with inheritance from parent elements.
    /// </summary>
    public string FontFamily => GetComputedProperty("font-family", "Helvetica") ?? "Helvetica";

    /// <summary>
    /// Gets the font size in points, with inheritance from parent elements.
    /// </summary>
    public double? FontSize
    {
        get
        {
            var value = GetComputedProperty("font-size", "12pt");
            if (string.IsNullOrEmpty(value)) return 12;
            return LengthParser.Parse(value);
        }
    }

    /// <summary>
    /// Gets the font weight (normal, bold, 100-900), with inheritance from parent elements.
    /// </summary>
    public string FontWeight => GetComputedProperty("font-weight", "normal") ?? "normal";

    /// <summary>
    /// Gets the font style (normal, italic, oblique), with inheritance from parent elements.
    /// </summary>
    public string FontStyle => GetComputedProperty("font-style", "normal") ?? "normal";

    /// <summary>
    /// Gets the text color in CSS format (e.g., "#FF0000", "red", "black"), with inheritance from parent elements.
    /// </summary>
    public string Color => GetComputedProperty("color", "black") ?? "black";

    /// <summary>
    /// Gets the text decoration (none, underline, overline, line-through), with inheritance from parent elements.
    /// </summary>
    public string TextDecoration => GetComputedProperty("text-decoration", "none") ?? "none";

    /// <summary>
    /// Gets the background color in CSS format.
    /// </summary>
    public string BackgroundColor => Properties.GetString("background-color", "");

    /// <summary>
    /// Gets the baseline shift in points.
    /// Positive values shift upward (superscript), negative downward (subscript).
    /// </summary>
    public double BaselineShift => Properties.GetLength("baseline-shift", 0);
}
