namespace Folly.Dom;

/// <summary>
/// Represents the fo:block-container element.
/// A block-level formatting object that generates a reference area and establishes
/// a new coordinate system. Commonly used for absolute positioning, rotation,
/// and writing mode changes.
/// </summary>
public sealed class FoBlockContainer : FoElement
{
    /// <inheritdoc/>
    public override string Name => "block-container";

    /// <summary>
    /// Gets the absolute position (auto, absolute, fixed, relative).
    /// Default is "auto".
    /// </summary>
    public string AbsolutePosition => Properties.GetString("absolute-position", "auto");

    /// <summary>
    /// Gets the top position for absolutely positioned containers.
    /// Default is "auto".
    /// </summary>
    public string Top => Properties.GetString("top", "auto");

    /// <summary>
    /// Gets the left position for absolutely positioned containers.
    /// Default is "auto".
    /// </summary>
    public string Left => Properties.GetString("left", "auto");

    /// <summary>
    /// Gets the bottom position for absolutely positioned containers.
    /// Default is "auto".
    /// </summary>
    public string Bottom => Properties.GetString("bottom", "auto");

    /// <summary>
    /// Gets the right position for absolutely positioned containers.
    /// Default is "auto".
    /// </summary>
    public string Right => Properties.GetString("right", "auto");

    /// <summary>
    /// Gets the width of the block container.
    /// Default is "auto".
    /// </summary>
    public string Width => Properties.GetString("width", "auto");

    /// <summary>
    /// Gets the height of the block container.
    /// Default is "auto".
    /// </summary>
    public string Height => Properties.GetString("height", "auto");

    /// <summary>
    /// Gets the reference orientation (0, 90, 180, 270, -90, -180, -270).
    /// Specifies rotation in degrees.
    /// Default is "0".
    /// </summary>
    public int ReferenceOrientation => int.TryParse(Properties.GetString("reference-orientation", "0"), out var val) ? val : 0;

    /// <summary>
    /// Gets the display alignment (auto, before, center, after).
    /// Controls vertical alignment of content.
    /// Default is "auto".
    /// </summary>
    public string DisplayAlign => Properties.GetString("display-align", "auto");

    /// <summary>
    /// Gets the overflow behavior (visible, hidden, scroll, auto, error-if-overflow).
    /// Default is "auto".
    /// </summary>
    public string Overflow => Properties.GetString("overflow", "auto");

    /// <summary>
    /// Gets the clip rectangle for overflow clipping.
    /// Default is "auto".
    /// </summary>
    public string Clip => Properties.GetString("clip", "auto");

    /// <summary>
    /// Gets the z-index for stacking order.
    /// Default is "auto".
    /// </summary>
    public string ZIndex => Properties.GetString("z-index", "auto");

    /// <summary>
    /// Gets the background color.
    /// </summary>
    public string BackgroundColor => Properties.GetString("background-color", "");

    /// <summary>
    /// Gets the background image URI.
    /// </summary>
    public string BackgroundImage => Properties.GetString("background-image", "");

    /// <summary>
    /// Gets the background repeat mode (repeat, repeat-x, repeat-y, no-repeat).
    /// </summary>
    public string BackgroundRepeat => Properties.GetString("background-repeat", "repeat");

    /// <summary>
    /// Gets the background horizontal position (left, center, right, or length/percentage).
    /// </summary>
    public string BackgroundPositionHorizontal => Properties.GetString("background-position-horizontal", "0%");

    /// <summary>
    /// Gets the background vertical position (top, center, bottom, or length/percentage).
    /// </summary>
    public string BackgroundPositionVertical => Properties.GetString("background-position-vertical", "0%");

    /// <summary>
    /// Gets the border width (base value for all sides).
    /// </summary>
    public double BorderWidth => Properties.GetLength("border-width", 0);

    /// <summary>
    /// Gets the border color (base value for all sides).
    /// </summary>
    public string BorderColor => Properties.GetString("border-color", "black");

    /// <summary>
    /// Gets the border style (base value for all sides).
    /// </summary>
    public string BorderStyle => Properties.GetString("border-style", "none");

    /// <summary>
    /// Gets the border-before-width.
    /// </summary>
    public double BorderBeforeWidth => Properties.GetLength("border-before-width", 0);

    /// <summary>
    /// Gets the border-after-width.
    /// </summary>
    public double BorderAfterWidth => Properties.GetLength("border-after-width", 0);

    /// <summary>
    /// Gets the border-start-width.
    /// </summary>
    public double BorderStartWidth => Properties.GetLength("border-start-width", 0);

    /// <summary>
    /// Gets the border-end-width.
    /// </summary>
    public double BorderEndWidth => Properties.GetLength("border-end-width", 0);

    /// <summary>
    /// Gets the top border width.
    /// Maps from border-before-width in XSL-FO based on writing-mode.
    /// </summary>
    public double BorderTopWidth => GetDirectionalLength("border-before-width", "border-top-width", BorderWidth);

    /// <summary>
    /// Gets the bottom border width.
    /// Maps from border-after-width in XSL-FO based on writing-mode.
    /// </summary>
    public double BorderBottomWidth => GetDirectionalLength("border-after-width", "border-bottom-width", BorderWidth);

    /// <summary>
    /// Gets the left border width.
    /// Maps from border-start-width in XSL-FO based on writing-mode.
    /// </summary>
    public double BorderLeftWidth => GetDirectionalLength("border-start-width", "border-left-width", BorderWidth);

    /// <summary>
    /// Gets the right border width.
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
    /// Gets the padding-before.
    /// </summary>
    public double PaddingBefore => Properties.GetLength("padding-before", 0);

    /// <summary>
    /// Gets the padding-after.
    /// </summary>
    public double PaddingAfter => Properties.GetLength("padding-after", 0);

    /// <summary>
    /// Gets the padding-start.
    /// </summary>
    public double PaddingStart => Properties.GetLength("padding-start", 0);

    /// <summary>
    /// Gets the padding-end.
    /// </summary>
    public double PaddingEnd => Properties.GetLength("padding-end", 0);

    /// <summary>
    /// Gets the margin-top.
    /// Maps from margin-before in XSL-FO based on writing-mode.
    /// </summary>
    public double MarginTop => GetDirectionalLength("margin-before", "margin-top");

    /// <summary>
    /// Gets the margin-bottom.
    /// Maps from margin-after in XSL-FO based on writing-mode.
    /// </summary>
    public double MarginBottom => GetDirectionalLength("margin-after", "margin-bottom");

    /// <summary>
    /// Gets the margin-left.
    /// Maps from margin-start in XSL-FO based on writing-mode.
    /// </summary>
    public double MarginLeft => GetDirectionalLength("margin-start", "margin-left");

    /// <summary>
    /// Gets the margin-right.
    /// Maps from margin-end in XSL-FO based on writing-mode.
    /// </summary>
    public double MarginRight => GetDirectionalLength("margin-end", "margin-right");
}
