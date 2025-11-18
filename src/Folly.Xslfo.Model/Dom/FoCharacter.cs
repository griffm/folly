namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:character element.
/// Used to explicitly map a Unicode character, which can be useful for
/// special characters, non-breaking spaces, and glyph mapping.
/// </summary>
public sealed class FoCharacter : FoElement
{
    /// <inheritdoc/>
    public override string Name => "character";

    /// <summary>
    /// Gets the character to be rendered.
    /// Specified using the "character" property (a Unicode code point).
    /// </summary>
    public string Character => Properties.GetString("character", "");

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

    /// <summary>
    /// Gets the baseline shift in points.
    /// </summary>
    public double BaselineShift => Properties.GetLength("baseline-shift", 0);

    /// <summary>
    /// Gets the glyph orientation horizontal (angle in degrees).
    /// Used for vertical text layout.
    /// </summary>
    public double GlyphOrientationHorizontal => Properties.GetLength("glyph-orientation-horizontal", 0);

    /// <summary>
    /// Gets the glyph orientation vertical (angle in degrees or "auto").
    /// Used for vertical text layout.
    /// </summary>
    public string GlyphOrientationVertical => Properties.GetString("glyph-orientation-vertical", "auto");
}
