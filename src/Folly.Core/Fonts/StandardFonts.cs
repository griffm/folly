namespace Folly.Fonts;

/// <summary>
/// Represents a standard PDF font with character metrics.
/// </summary>
internal sealed class StandardFont
{
    public string Name { get; init; } = "";
    public double Ascent { get; init; }
    public double Descent { get; init; }

    private readonly Dictionary<char, double> _charWidths = new();
    private readonly double _defaultWidth;

    public StandardFont(string name, double ascent, double descent, double defaultWidth)
    {
        Name = name;
        Ascent = ascent;
        Descent = descent;
        _defaultWidth = defaultWidth;
    }

    public void SetCharWidth(char ch, double width)
    {
        _charWidths[ch] = width;
    }

    public double GetCharWidth(char ch)
    {
        if (_charWidths.TryGetValue(ch, out var width))
            return width;
        return _defaultWidth;
    }
}

/// <summary>
/// Standard PDF Type 1 fonts (built into PDF readers).
/// </summary>
internal static class StandardFonts
{
    private static readonly Dictionary<string, StandardFont> _fonts = new();

    // Unicode to Adobe Standard Encoding mappings for common special characters
    private static readonly Dictionary<char, int> UnicodeToAdobeEncoding = new()
    {
        { '\u2014', 208 },  // Em dash (—) → emdash
        { '\u2013', 177 },  // En dash (–) → endash
        { '\u2018', 193 },  // Left single quotation mark (') → quoteleft
        { '\u2019', 194 },  // Right single quotation mark (') → quoteright
        { '\u201C', 195 },  // Left double quotation mark (") → quotedblleft
        { '\u201D', 196 },  // Right double quotation mark (") → quotedblright
        { '\u2022', 183 },  // Bullet (•) → bullet
        { '\u2026', 188 },  // Horizontal ellipsis (…) → ellipsis
        { '\u2020', 178 },  // Dagger (†) → dagger
        { '\u2021', 179 },  // Double dagger (‡) → daggerdbl
        { '\u2030', 189 },  // Per mille sign (‰) → perthousand
        { '\u0192', 166 },  // Latin small letter f with hook (ƒ) → florin
    };

    static StandardFonts()
    {
        // Load all fonts from generated code (no runtime AFM parsing!)
        var allMetrics = Base14FontMetrics.GetAllFonts();

        foreach (var (name, metrics) in allMetrics)
        {
            var font = new StandardFont(metrics.Name, metrics.Ascender, metrics.Descender, metrics.DefaultWidth);

            // Load character widths for Adobe Standard Encoding (0-255)
            foreach (var (charCode, width) in metrics.CharWidths)
            {
                if (charCode >= 0 && charCode <= 255)
                {
                    font.SetCharWidth((char)charCode, width);
                }
            }

            // Map common Unicode characters to their Adobe Standard Encoding equivalents
            foreach (var (unicodeChar, adobeCode) in UnicodeToAdobeEncoding)
            {
                if (metrics.CharWidths.TryGetValue(adobeCode, out var width))
                {
                    font.SetCharWidth(unicodeChar, width);
                }
            }

            _fonts[name] = font;
        }
    }

    /// <summary>
    /// Gets a font by family name with bold and italic flags.
    /// Uses FontResolver to determine the specific font variant.
    /// </summary>
    public static StandardFont GetFont(string familyName, bool bold, bool italic)
    {
        // Use FontResolver to determine the specific font name
        var fontName = FontResolver.ResolveFont(familyName, bold, italic);

        // Look up the font by resolved name
        return GetFont(fontName);
    }

    /// <summary>
    /// Gets a font by its exact name (e.g., "Times-BoldItalic", "Helvetica").
    /// </summary>
    public static StandardFont GetFont(string fontName)
    {
        // Try direct lookup
        if (_fonts.TryGetValue(fontName, out var font))
            return font;

        // Fallback to Helvetica if font not found
        return _fonts["Helvetica"];
    }
}
