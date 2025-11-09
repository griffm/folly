namespace Folly.Dom;

/// <summary>
/// Represents the fo:inline-container element.
/// An inline-level formatting object that generates a reference area,
/// similar to block-container but participates in inline formatting.
/// Useful for inline blocks with specific dimensions or writing mode changes.
/// </summary>
public sealed class FoInlineContainer : FoElement
{
    /// <inheritdoc/>
    public override string Name => "inline-container";

    /// <summary>
    /// Gets the inline-progression-dimension (width in lr-tb writing mode).
    /// Default is "auto".
    /// </summary>
    public string InlineProgressionDimension => Properties.GetString("inline-progression-dimension", "auto");

    /// <summary>
    /// Gets the block-progression-dimension (height in lr-tb writing mode).
    /// Default is "auto".
    /// </summary>
    public string BlockProgressionDimension => Properties.GetString("block-progression-dimension", "auto");

    /// <summary>
    /// Gets the width of the inline container.
    /// Default is "auto".
    /// </summary>
    public string Width => Properties.GetString("width", "auto");

    /// <summary>
    /// Gets the height of the inline container.
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
    /// Gets the alignment adjust (auto, baseline, before-edge, text-before-edge, etc.).
    /// Default is "auto".
    /// </summary>
    public string AlignmentAdjust => Properties.GetString("alignment-adjust", "auto");

    /// <summary>
    /// Gets the alignment baseline (auto, baseline, before-edge, text-before-edge, etc.).
    /// Default is "auto".
    /// </summary>
    public string AlignmentBaseline => Properties.GetString("alignment-baseline", "auto");

    /// <summary>
    /// Gets the baseline shift in points.
    /// </summary>
    public double BaselineShift => Properties.GetLength("baseline-shift", 0);

    /// <summary>
    /// Gets the dominant baseline (auto, use-script, no-change, reset-size, ideographic, etc.).
    /// Default is "auto".
    /// </summary>
    public string DominantBaseline => Properties.GetString("dominant-baseline", "auto");

    /// <summary>
    /// Gets the background color.
    /// </summary>
    public string BackgroundColor => Properties.GetString("background-color", "");

    /// <summary>
    /// Gets the border-top width.
    /// </summary>
    public double BorderTopWidth => Properties.GetLength("border-top-width", 0);

    /// <summary>
    /// Gets the border-bottom width.
    /// </summary>
    public double BorderBottomWidth => Properties.GetLength("border-bottom-width", 0);

    /// <summary>
    /// Gets the border-left width.
    /// </summary>
    public double BorderLeftWidth => Properties.GetLength("border-left-width", 0);

    /// <summary>
    /// Gets the border-right width.
    /// </summary>
    public double BorderRightWidth => Properties.GetLength("border-right-width", 0);

    /// <summary>
    /// Gets the padding-top.
    /// </summary>
    public double PaddingTop => Properties.GetLength("padding-top", 0);

    /// <summary>
    /// Gets the padding-bottom.
    /// </summary>
    public double PaddingBottom => Properties.GetLength("padding-bottom", 0);

    /// <summary>
    /// Gets the padding-left.
    /// </summary>
    public double PaddingLeft => Properties.GetLength("padding-left", 0);

    /// <summary>
    /// Gets the padding-right.
    /// </summary>
    public double PaddingRight => Properties.GetLength("padding-right", 0);
}
