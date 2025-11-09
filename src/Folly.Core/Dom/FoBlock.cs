namespace Folly.Dom;

/// <summary>
/// Represents the fo:block element.
/// </summary>
public sealed class FoBlock : FoElement
{
    /// <inheritdoc/>
    public override string Name => "block";

    /// <summary>
    /// Gets the font family.
    /// </summary>
    public string FontFamily => Properties.GetString("font-family", "Helvetica");

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public double FontSize => Properties.GetLength("font-size", 12);

    /// <summary>
    /// Gets the line height in points.
    /// </summary>
    public double LineHeight => Properties.GetLength("line-height", FontSize * 1.2);

    /// <summary>
    /// Gets the text alignment.
    /// </summary>
    public string TextAlign => Properties.GetString("text-align", "start");

    /// <summary>
    /// Gets the margin-top in points.
    /// </summary>
    public double MarginTop => Properties.GetLength("margin-top", 0);

    /// <summary>
    /// Gets the margin-bottom in points.
    /// </summary>
    public double MarginBottom => Properties.GetLength("margin-bottom", 0);

    /// <summary>
    /// Gets the margin-left in points.
    /// </summary>
    public double MarginLeft => Properties.GetLength("margin-left", 0);

    /// <summary>
    /// Gets the margin-right in points.
    /// </summary>
    public double MarginRight => Properties.GetLength("margin-right", 0);

    /// <summary>
    /// Gets the padding-top in points.
    /// </summary>
    public double PaddingTop => Properties.GetLength("padding-top", 0);

    /// <summary>
    /// Gets the padding-bottom in points.
    /// </summary>
    public double PaddingBottom => Properties.GetLength("padding-bottom", 0);

    /// <summary>
    /// Gets the padding-left in points.
    /// </summary>
    public double PaddingLeft => Properties.GetLength("padding-left", 0);

    /// <summary>
    /// Gets the padding-right in points.
    /// </summary>
    public double PaddingRight => Properties.GetLength("padding-right", 0);

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
