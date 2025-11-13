namespace Folly.Fonts;

/// <summary>
/// Font metrics for measuring text.
/// Provides methods to measure text width, line height, ascent, and descent
/// using the Standard PDF Type 1 fonts.
/// Note: Kerning is applied during PDF rendering, not during layout measurement.
/// </summary>
public sealed class FontMetrics
{
    /// <summary>
    /// Gets the font family name. This can be either a base family name
    /// (e.g., "Helvetica", "Times-Roman") or a complete font variant name
    /// (e.g., "Helvetica-Bold", "Times-Italic").
    /// </summary>
    public string FamilyName { get; init; } = "Helvetica";

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public double Size { get; init; } = 12;

    /// <summary>
    /// Gets whether the font is bold. Used in combination with FamilyName
    /// to resolve to a specific font variant.
    /// </summary>
    public bool IsBold { get; init; }

    /// <summary>
    /// Gets whether the font is italic. Used in combination with FamilyName
    /// to resolve to a specific font variant.
    /// </summary>
    public bool IsItalic { get; init; }

    /// <summary>
    /// Measures the width of a text string in points.
    /// Note: This measurement does not include kerning. Kerning is applied
    /// during PDF rendering for accurate character positioning.
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
