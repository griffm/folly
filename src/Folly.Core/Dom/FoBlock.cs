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
    /// Gets the text alignment for the last line of a block, with inheritance from parent elements.
    /// If not specified, defaults to the value of text-align (or "start" if text-align is "justify").
    /// </summary>
    public string TextAlignLast
    {
        get
        {
            var value = GetComputedProperty("text-align-last");
            if (!string.IsNullOrEmpty(value))
                return value;

            // If not specified, use text-align value
            // Exception: if text-align is "justify", default to "start" for the last line
            var textAlign = TextAlign.ToLowerInvariant();
            return textAlign == "justify" ? "start" : textAlign;
        }
    }

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
    /// Gets the background image URI.
    /// </summary>
    public string BackgroundImage => Properties.GetString("background-image", "");

    /// <summary>
    /// Gets the background repeat mode (repeat, repeat-x, repeat-y, no-repeat).
    /// Default is "repeat" per XSL-FO 1.1 spec.
    /// </summary>
    public string BackgroundRepeat => Properties.GetString("background-repeat", "repeat");

    /// <summary>
    /// Gets the background horizontal position (left, center, right, or length/percentage).
    /// Default is "0%" per XSL-FO 1.1 spec.
    /// </summary>
    public string BackgroundPositionHorizontal => Properties.GetString("background-position-horizontal", "0%");

    /// <summary>
    /// Gets the background vertical position (top, center, bottom, or length/percentage).
    /// Default is "0%" per XSL-FO 1.1 spec.
    /// </summary>
    public string BackgroundPositionVertical => Properties.GetString("background-position-vertical", "0%");

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
    /// Gets the border radius (uniform for all corners) in points.
    /// Note: border-radius is not part of XSL-FO 1.1 spec but commonly used as an extension.
    /// </summary>
    public double BorderRadius => Properties.GetLength("border-radius", 0);

    /// <summary>
    /// Gets the top-left border radius in points.
    /// </summary>
    public double BorderTopLeftRadius => Properties.GetLength("border-top-left-radius", BorderRadius);

    /// <summary>
    /// Gets the top-right border radius in points.
    /// </summary>
    public double BorderTopRightRadius => Properties.GetLength("border-top-right-radius", BorderRadius);

    /// <summary>
    /// Gets the bottom-left border radius in points.
    /// </summary>
    public double BorderBottomLeftRadius => Properties.GetLength("border-bottom-left-radius", BorderRadius);

    /// <summary>
    /// Gets the bottom-right border radius in points.
    /// </summary>
    public double BorderBottomRightRadius => Properties.GetLength("border-bottom-right-radius", BorderRadius);

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
    /// Gets the widows property - minimum number of lines that must be left at the top of a page
    /// after a block is split across pages. Default is 2.
    /// </summary>
    public int Widows
    {
        get
        {
            var value = Properties.GetString("widows");
            if (string.IsNullOrEmpty(value))
                return 2; // Default value per XSL-FO spec

            if (int.TryParse(value, out var result) && result >= 0)
                return result;

            return 2;
        }
    }

    /// <summary>
    /// Gets the orphans property - minimum number of lines that must be left at the bottom of a page
    /// before a block is split across pages. Default is 2.
    /// </summary>
    public int Orphans
    {
        get
        {
            var value = Properties.GetString("orphans");
            if (string.IsNullOrEmpty(value))
                return 2; // Default value per XSL-FO spec

            if (int.TryParse(value, out var result) && result >= 0)
                return result;

            return 2;
        }
    }

    /// <summary>
    /// Gets the wrap-option property - controls line wrapping behavior.
    /// Values: "wrap" (default), "no-wrap"
    /// </summary>
    public string WrapOption => GetComputedProperty("wrap-option", "wrap") ?? "wrap";

    /// <summary>
    /// Gets the hyphenate property - controls whether hyphenation is enabled for this block.
    /// Values: "true" | "false" (default per XSL-FO 1.1 spec)
    /// This property allows per-paragraph control of hyphenation. When set to "false" (default),
    /// hyphenation is disabled for this block even if globally enabled via LayoutOptions.
    /// When set to "true", hyphenation is enabled if also globally enabled.
    /// </summary>
    public bool Hyphenate
    {
        get
        {
            var value = GetComputedProperty("hyphenate", "false");
            return value?.ToLowerInvariant() == "true";
        }
    }

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
