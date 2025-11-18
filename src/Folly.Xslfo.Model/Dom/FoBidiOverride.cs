namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:bidi-override element.
/// Used to override the default Unicode bidirectional algorithm,
/// forcing directionality for its content (e.g., for right-to-left text).
/// </summary>
public sealed class FoBidiOverride : FoElement
{
    /// <inheritdoc/>
    public override string Name => "bidi-override";

    /// <summary>
    /// Gets the direction (ltr, rtl).
    /// Specifies the inline-progression direction.
    /// Default is "ltr".
    /// </summary>
    public string Direction => Properties.GetString("direction", "ltr");

    /// <summary>
    /// Gets the Unicode bidi property (normal, embed, bidi-override).
    /// Default is "bidi-override" for this element.
    /// </summary>
    public string UnicodeBidi => Properties.GetString("unicode-bidi", "bidi-override");

    /// <summary>
    /// Gets the font family.
    /// </summary>
    public string FontFamily => GetComputedProperty("font-family", "Helvetica") ?? "Helvetica";

    /// <summary>
    /// Gets the font size in points.
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
    /// Gets the font weight (normal, bold, 100-900).
    /// </summary>
    public string FontWeight => GetComputedProperty("font-weight", "normal") ?? "normal";

    /// <summary>
    /// Gets the font style (normal, italic, oblique).
    /// </summary>
    public string FontStyle => GetComputedProperty("font-style", "normal") ?? "normal";

    /// <summary>
    /// Gets the text color in CSS format.
    /// </summary>
    public string Color => GetComputedProperty("color", "black") ?? "black";
}
