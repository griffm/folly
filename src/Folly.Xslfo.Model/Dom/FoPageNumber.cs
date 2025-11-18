namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:page-number element.
/// Generates the current page number at layout time.
/// </summary>
public sealed class FoPageNumber : FoElement
{
    /// <inheritdoc/>
    public override string Name => "page-number";

    /// <summary>
    /// Gets the font family.
    /// </summary>
    public string FontFamily => Properties.GetString("font-family", "Helvetica");

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public double FontSize => Properties.GetLength("font-size", 12);
}
