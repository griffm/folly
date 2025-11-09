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
    /// Gets the writing mode (lr-tb, rl-tb, tb-rl, tb-lr, lr, rl, tb).
    /// Default is "lr-tb" (left-to-right, top-to-bottom).
    /// </summary>
    public string WritingMode => Properties.GetString("writing-mode", "lr-tb");

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
    /// </summary>
    public double MarginTop => Properties.GetLength("margin-top", 0);

    /// <summary>
    /// Gets the margin-bottom.
    /// </summary>
    public double MarginBottom => Properties.GetLength("margin-bottom", 0);

    /// <summary>
    /// Gets the margin-left.
    /// </summary>
    public double MarginLeft => Properties.GetLength("margin-left", 0);

    /// <summary>
    /// Gets the margin-right.
    /// </summary>
    public double MarginRight => Properties.GetLength("margin-right", 0);
}
