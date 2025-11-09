namespace Folly.Fonts;

/// <summary>
/// Font metrics for measuring text.
/// </summary>
public sealed class FontMetrics
{
    /// <summary>
    /// Gets the font family name.
    /// </summary>
    public string FamilyName { get; init; } = "Helvetica";

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public double Size { get; init; } = 12;

    /// <summary>
    /// Gets whether the font is bold.
    /// </summary>
    public bool IsBold { get; init; }

    /// <summary>
    /// Gets whether the font is italic.
    /// </summary>
    public bool IsItalic { get; init; }

    /// <summary>
    /// Measures the width of a text string in points.
    /// </summary>
    public double MeasureWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Get the base font
        var baseFont = StandardFonts.GetFont(FamilyName, IsBold, IsItalic);

        // Calculate width based on character widths
        var width = 0.0;
        foreach (var ch in text)
        {
            width += baseFont.GetCharWidth(ch);
        }

        // Scale by font size (base metrics are for 1000 units per em)
        return width * Size / 1000.0;
    }

    /// <summary>
    /// Gets the height of a line of text in points.
    /// </summary>
    public double GetLineHeight()
    {
        // Typical line height is 120% of font size
        return Size * 1.2;
    }

    /// <summary>
    /// Gets the ascent (height above baseline) in points.
    /// </summary>
    public double GetAscent()
    {
        var baseFont = StandardFonts.GetFont(FamilyName, IsBold, IsItalic);
        return baseFont.Ascent * Size / 1000.0;
    }

    /// <summary>
    /// Gets the descent (depth below baseline) in points.
    /// </summary>
    public double GetDescent()
    {
        var baseFont = StandardFonts.GetFont(FamilyName, IsBold, IsItalic);
        return baseFont.Descent * Size / 1000.0;
    }
}
