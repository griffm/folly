namespace Folly.Dom;

/// <summary>
/// Represents the fo:block element.
/// </summary>
public sealed class FoBlock : FoElement
{
    /// <inheritdoc/>
    public override string Name => "block";

    /// <summary>
    /// Gets the font family, with inheritance from parent elements.
    /// </summary>
    public string FontFamily => GetComputedProperty("font-family", "Helvetica") ?? "Helvetica";

    /// <summary>
    /// Gets the font size in points, with inheritance from parent elements.
    /// </summary>
    public double FontSize
    {
        get
        {
            var value = GetComputedProperty("font-size", "12pt");
            if (string.IsNullOrEmpty(value)) return 12;
            return LengthParser.Parse(value);
        }
    }

    /// <summary>
    /// Gets the line height in points, with inheritance from parent elements.
    /// </summary>
    public double LineHeight
    {
        get
        {
            var value = GetComputedProperty("line-height");
            if (string.IsNullOrEmpty(value) || value == "normal")
                return FontSize * 1.2;

            // Check if the value is unitless (a multiplier)
            value = value.Trim();
            if (double.TryParse(value, out var multiplier))
            {
                // Unitless value is a multiplier of font-size
                return FontSize * multiplier;
            }

            // Parse as absolute length
            return LengthParser.Parse(value);
        }
    }

    /// <summary>
    /// Gets the text alignment, with inheritance from parent elements.
    /// </summary>
    public string TextAlign => GetComputedProperty("text-align", "start") ?? "start";

    /// <summary>
    /// Gets the margin-top in points (margin-before in XSL-FO).
    /// </summary>
    public double MarginTop => Properties.GetLength("margin-before", Properties.GetLength("margin-top", 0));

    /// <summary>
    /// Gets the margin-bottom in points (margin-after in XSL-FO).
    /// </summary>
    public double MarginBottom => Properties.GetLength("margin-after", Properties.GetLength("margin-bottom", 0));

    /// <summary>
    /// Gets the margin-left in points (margin-start in XSL-FO).
    /// </summary>
    public double MarginLeft => Properties.GetLength("margin-start", Properties.GetLength("margin-left", 0));

    /// <summary>
    /// Gets the margin-right in points (margin-end in XSL-FO).
    /// </summary>
    public double MarginRight => Properties.GetLength("margin-end", Properties.GetLength("margin-right", 0));

    /// <summary>
    /// Gets the space before in points (space before this block).
    /// In XSL-FO, space-before defines the minimum space between this block and the preceding block.
    /// </summary>
    public double SpaceBefore => Properties.GetLength("space-before", 0);

    /// <summary>
    /// Gets the space after in points (space after this block).
    /// In XSL-FO, space-after defines the minimum space between this block and the following block.
    /// </summary>
    public double SpaceAfter => Properties.GetLength("space-after", 0);

    /// <summary>
    /// Gets the padding-top in points (padding-before in XSL-FO).
    /// </summary>
    public double PaddingTop => Properties.GetLength("padding-before", Properties.GetLength("padding-top", 0));

    /// <summary>
    /// Gets the padding-bottom in points (padding-after in XSL-FO).
    /// </summary>
    public double PaddingBottom => Properties.GetLength("padding-after", Properties.GetLength("padding-bottom", 0));

    /// <summary>
    /// Gets the padding-left in points (padding-start in XSL-FO).
    /// </summary>
    public double PaddingLeft => Properties.GetLength("padding-start", Properties.GetLength("padding-left", 0));

    /// <summary>
    /// Gets the padding-right in points (padding-end in XSL-FO).
    /// </summary>
    public double PaddingRight => Properties.GetLength("padding-end", Properties.GetLength("padding-right", 0));

    /// <summary>
    /// Gets the background color in CSS format (e.g., "#FF0000", "red", "transparent").
    /// </summary>
    public string BackgroundColor => Properties.GetString("background-color", "transparent");

    /// <summary>
    /// Gets the border width in points.
    /// </summary>
    public double BorderWidth => Properties.GetLength("border-width", 0);

    /// <summary>
    /// Gets the border color in CSS format.
    /// </summary>
    public string BorderColor => Properties.GetString("border-color", "black");

    /// <summary>
    /// Gets the border style (solid, dashed, dotted, etc.).
    /// </summary>
    public string BorderStyle => Properties.GetString("border-style", "none");

    /// <summary>
    /// Gets the top border width in points (border-before in XSL-FO).
    /// </summary>
    public double BorderTopWidth => Properties.GetLength("border-before-width", Properties.GetLength("border-top-width", BorderWidth));

    /// <summary>
    /// Gets the bottom border width in points (border-after in XSL-FO).
    /// </summary>
    public double BorderBottomWidth => Properties.GetLength("border-after-width", Properties.GetLength("border-bottom-width", BorderWidth));

    /// <summary>
    /// Gets the left border width in points (border-start in XSL-FO).
    /// </summary>
    public double BorderLeftWidth => Properties.GetLength("border-start-width", Properties.GetLength("border-left-width", BorderWidth));

    /// <summary>
    /// Gets the right border width in points (border-end in XSL-FO).
    /// </summary>
    public double BorderRightWidth => Properties.GetLength("border-end-width", Properties.GetLength("border-right-width", BorderWidth));

    /// <summary>
    /// Gets the top border style (border-before-style in XSL-FO).
    /// </summary>
    public string BorderTopStyle => Properties.GetString("border-before-style", Properties.GetString("border-top-style", BorderStyle));

    /// <summary>
    /// Gets the bottom border style (border-after-style in XSL-FO).
    /// </summary>
    public string BorderBottomStyle => Properties.GetString("border-after-style", Properties.GetString("border-bottom-style", BorderStyle));

    /// <summary>
    /// Gets the left border style (border-start-style in XSL-FO).
    /// </summary>
    public string BorderLeftStyle => Properties.GetString("border-start-style", Properties.GetString("border-left-style", BorderStyle));

    /// <summary>
    /// Gets the right border style (border-end-style in XSL-FO).
    /// </summary>
    public string BorderRightStyle => Properties.GetString("border-end-style", Properties.GetString("border-right-style", BorderStyle));

    /// <summary>
    /// Gets the top border color (border-before-color in XSL-FO).
    /// </summary>
    public string BorderTopColor => Properties.GetString("border-before-color", Properties.GetString("border-top-color", BorderColor));

    /// <summary>
    /// Gets the bottom border color (border-after-color in XSL-FO).
    /// </summary>
    public string BorderBottomColor => Properties.GetString("border-after-color", Properties.GetString("border-bottom-color", BorderColor));

    /// <summary>
    /// Gets the left border color (border-start-color in XSL-FO).
    /// </summary>
    public string BorderLeftColor => Properties.GetString("border-start-color", Properties.GetString("border-left-color", BorderColor));

    /// <summary>
    /// Gets the right border color (border-end-color in XSL-FO).
    /// </summary>
    public string BorderRightColor => Properties.GetString("border-end-color", Properties.GetString("border-right-color", BorderColor));

    /// <summary>
    /// Gets the break-before property (auto, always, page).
    /// </summary>
    public string BreakBefore => Properties.GetString("break-before", "auto");

    /// <summary>
    /// Gets the break-after property (auto, always, page).
    /// </summary>
    public string BreakAfter => Properties.GetString("break-after", "auto");

    /// <summary>
    /// Gets the keep-together property (auto, always).
    /// </summary>
    public string KeepTogether => Properties.GetString("keep-together", "auto");

    /// <summary>
    /// Gets the keep-with-next property (auto, always).
    /// </summary>
    public string KeepWithNext => Properties.GetString("keep-with-next", "auto");

    /// <summary>
    /// Gets the keep-with-previous property (auto, always).
    /// </summary>
    public string KeepWithPrevious => Properties.GetString("keep-with-previous", "auto");

    /// <summary>
    /// Gets the footnotes contained in this block.
    /// Footnotes appear inline but are rendered at the bottom of the page.
    /// </summary>
    public IReadOnlyList<FoFootnote> Footnotes { get; init; } = Array.Empty<FoFootnote>();

    /// <summary>
    /// Gets the floats contained in this block.
    /// Floats are positioned to the side with content flowing around them.
    /// </summary>
    public IReadOnlyList<FoFloat> Floats { get; init; } = Array.Empty<FoFloat>();
}
