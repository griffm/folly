namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:initial-property-set element.
/// Used within fo:block to specify formatting properties for the first line
/// or first letter of a block (similar to CSS ::first-line and ::first-letter).
/// </summary>
public sealed class FoInitialPropertySet : FoElement
{
    /// <inheritdoc/>
    public override string Name => "initial-property-set";

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
    /// Gets the text decoration (none, underline, overline, line-through).
    /// </summary>
    public string TextDecoration => GetComputedProperty("text-decoration", "none") ?? "none";

    /// <summary>
    /// Gets the text transform (none, capitalize, uppercase, lowercase).
    /// </summary>
    public string TextTransform => Properties.GetString("text-transform", "none");

    /// <summary>
    /// Gets the letter spacing in points.
    /// </summary>
    public double LetterSpacing => Properties.GetLength("letter-spacing", 0);

    /// <summary>
    /// Gets the word spacing in points.
    /// </summary>
    public double WordSpacing => Properties.GetLength("word-spacing", 0);

    /// <summary>
    /// Gets the line height (normal or a length value).
    /// </summary>
    public string LineHeight => Properties.GetString("line-height", "normal");

    /// <summary>
    /// Gets the background color.
    /// </summary>
    public string BackgroundColor => Properties.GetString("background-color", "");

    /// <summary>
    /// Gets the text shadow.
    /// </summary>
    public string TextShadow => Properties.GetString("text-shadow", "none");
}
