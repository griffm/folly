namespace Folly.Xslfo;

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
    /// Maps from border-before-width in XSL-FO based on writing-mode.
    /// </summary>
    public double BorderTopWidth => GetDirectionalLength("border-before-width", "border-top-width");

    /// <summary>
    /// Gets the border-bottom width.
    /// Maps from border-after-width in XSL-FO based on writing-mode.
    /// </summary>
    public double BorderBottomWidth => GetDirectionalLength("border-after-width", "border-bottom-width");

    /// <summary>
    /// Gets the border-left width.
    /// Maps from border-start-width in XSL-FO based on writing-mode.
    /// </summary>
    public double BorderLeftWidth => GetDirectionalLength("border-start-width", "border-left-width");

    /// <summary>
    /// Gets the border-right width.
    /// Maps from border-end-width in XSL-FO based on writing-mode.
    /// </summary>
    public double BorderRightWidth => GetDirectionalLength("border-end-width", "border-right-width");

    /// <summary>
    /// Gets the padding-top.
    /// Maps from padding-before in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingTop => GetDirectionalLength("padding-before", "padding-top");

    /// <summary>
    /// Gets the padding-bottom.
    /// Maps from padding-after in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingBottom => GetDirectionalLength("padding-after", "padding-bottom");

    /// <summary>
    /// Gets the padding-left.
    /// Maps from padding-start in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingLeft => GetDirectionalLength("padding-start", "padding-left");

    /// <summary>
    /// Gets the padding-right.
    /// Maps from padding-end in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingRight => GetDirectionalLength("padding-end", "padding-right");
}
