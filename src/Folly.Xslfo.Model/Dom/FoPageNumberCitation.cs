namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:page-number-citation element.
/// Used to reference the page number of a page containing the first normal area
/// returned by a formatting object with an id matching the ref-id property.
/// </summary>
public sealed class FoPageNumberCitation : FoElement
{
    /// <inheritdoc/>
    public override string Name => "page-number-citation";

    /// <summary>
    /// Gets the ref-id that specifies which formatting object to reference.
    /// This is a required property.
    /// </summary>
    public string RefId => Properties.GetString("ref-id", "");

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
