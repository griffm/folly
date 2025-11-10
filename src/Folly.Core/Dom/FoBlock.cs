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
    /// Gets the font weight (normal, bold, 100-900), with inheritance from parent elements.
    /// </summary>
    public string FontWeight => GetComputedProperty("font-weight", "normal") ?? "normal";

    /// <summary>
    /// Gets the font style (normal, italic, oblique), with inheritance from parent elements.
    /// </summary>
    public string FontStyle => GetComputedProperty("font-style", "normal") ?? "normal";

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
    /// Gets the margin-top in points.
    /// Maps from margin-before in XSL-FO based on writing-mode.
    /// </summary>
    public double MarginTop => GetDirectionalLength("margin-before", "margin-top");

    /// <summary>
    /// Gets the margin-bottom in points.
    /// Maps from margin-after in XSL-FO based on writing-mode.
    /// </summary>
    public double MarginBottom => GetDirectionalLength("margin-after", "margin-bottom");

    /// <summary>
    /// Gets the margin-left in points.
    /// Maps from margin-start in XSL-FO based on writing-mode.
    /// </summary>
    public double MarginLeft => GetDirectionalLength("margin-start", "margin-left");

    /// <summary>
    /// Gets the margin-right in points.
    /// Maps from margin-end in XSL-FO based on writing-mode.
    /// </summary>
    public double MarginRight => GetDirectionalLength("margin-end", "margin-right");

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
    /// Gets the padding-top in points.
    /// Maps from padding-before in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingTop => GetDirectionalLength("padding-before", "padding-top");

    /// <summary>
    /// Gets the padding-bottom in points.
    /// Maps from padding-after in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingBottom => GetDirectionalLength("padding-after", "padding-bottom");

    /// <summary>
    /// Gets the padding-left in points.
    /// Maps from padding-start in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingLeft => GetDirectionalLength("padding-start", "padding-left");

    /// <summary>
    /// Gets the padding-right in points.
    /// Maps from padding-end in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingRight => GetDirectionalLength("padding-end", "padding-right");

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
    /// Gets the top border width in points.
    /// Maps from border-before-width in XSL-FO based on writing-mode.
    /// </summary>
    public double BorderTopWidth => GetDirectionalLength("border-before-width", "border-top-width", BorderWidth);

    /// <summary>
    /// Gets the bottom border width in points.
    /// Maps from border-after-width in XSL-FO based on writing-mode.
    /// </summary>
    public double BorderBottomWidth => GetDirectionalLength("border-after-width", "border-bottom-width", BorderWidth);

    /// <summary>
    /// Gets the left border width in points.
    /// Maps from border-start-width in XSL-FO based on writing-mode.
    /// </summary>
    public double BorderLeftWidth => GetDirectionalLength("border-start-width", "border-left-width", BorderWidth);

    /// <summary>
    /// Gets the right border width in points.
    /// Maps from border-end-width in XSL-FO based on writing-mode.
    /// </summary>
    public double BorderRightWidth => GetDirectionalLength("border-end-width", "border-right-width", BorderWidth);

    /// <summary>
    /// Gets the top border style.
    /// Maps from border-before-style in XSL-FO based on writing-mode.
    /// </summary>
    public string BorderTopStyle => GetDirectionalString("border-before-style", "border-top-style", BorderStyle);

    /// <summary>
    /// Gets the bottom border style.
    /// Maps from border-after-style in XSL-FO based on writing-mode.
    /// </summary>
    public string BorderBottomStyle => GetDirectionalString("border-after-style", "border-bottom-style", BorderStyle);

    /// <summary>
    /// Gets the left border style.
    /// Maps from border-start-style in XSL-FO based on writing-mode.
    /// </summary>
    public string BorderLeftStyle => GetDirectionalString("border-start-style", "border-left-style", BorderStyle);

    /// <summary>
    /// Gets the right border style.
    /// Maps from border-end-style in XSL-FO based on writing-mode.
    /// </summary>
    public string BorderRightStyle => GetDirectionalString("border-end-style", "border-right-style", BorderStyle);

    /// <summary>
    /// Gets the top border color.
    /// Maps from border-before-color in XSL-FO based on writing-mode.
    /// </summary>
    public string BorderTopColor => GetDirectionalString("border-before-color", "border-top-color", BorderColor);

    /// <summary>
    /// Gets the bottom border color.
    /// Maps from border-after-color in XSL-FO based on writing-mode.
    /// </summary>
    public string BorderBottomColor => GetDirectionalString("border-after-color", "border-bottom-color", BorderColor);

    /// <summary>
    /// Gets the left border color.
    /// Maps from border-start-color in XSL-FO based on writing-mode.
    /// </summary>
    public string BorderLeftColor => GetDirectionalString("border-start-color", "border-left-color", BorderColor);

    /// <summary>
    /// Gets the right border color.
    /// Maps from border-end-color in XSL-FO based on writing-mode.
    /// </summary>
    public string BorderRightColor => GetDirectionalString("border-end-color", "border-right-color", BorderColor);

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
