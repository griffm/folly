namespace Folly.Dom;

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
    /// Gets the font family.
    /// </summary>
    public string FontFamily => Properties.GetString("font-family", "");

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public double? FontSize
    {
        get
        {
            var value = Properties.GetString("font-size", "");
            if (string.IsNullOrEmpty(value)) return null;
            return Properties.GetLength("font-size", 0);
        }
    }

    /// <summary>
    /// Gets the font weight (normal, bold, 100-900).
    /// </summary>
    public string FontWeight => Properties.GetString("font-weight", "");

    /// <summary>
    /// Gets the font style (normal, italic, oblique).
    /// </summary>
    public string FontStyle => Properties.GetString("font-style", "");

    /// <summary>
    /// Gets the text color in CSS format (e.g., "#FF0000", "red", "black").
    /// </summary>
    public string Color => Properties.GetString("color", "");

    /// <summary>
    /// Gets the text decoration (none, underline, overline, line-through).
    /// </summary>
    public string TextDecoration => Properties.GetString("text-decoration", "");

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
