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

    static StandardFonts()
    {
        // Load all fonts from generated code (no runtime AFM parsing!)
        var allMetrics = Base14FontMetrics.GetAllFonts();

        foreach (var (name, metrics) in allMetrics)
        {
            var font = new StandardFont(metrics.Name, metrics.Ascender, metrics.Descender, metrics.DefaultWidth);

            // Load character widths
            foreach (var (charCode, width) in metrics.CharWidths)
            {
                if (charCode >= 0 && charCode <= 255)
                {
                    font.SetCharWidth((char)charCode, width);
                }
            }

            _fonts[name] = font;
        }
    }

    public static StandardFont GetFont(string familyName, bool bold, bool italic)
    {
        // First, check if the family name is already a complete font name (e.g., "Times-Italic")
        // This handles cases where the font variant is already in the name
        if (_fonts.TryGetValue(familyName, out var directFont))
        {
            return directFont;
        }

        // Otherwise, derive the font name from the base family and bold/italic flags
        var key = familyName.ToLowerInvariant() switch
        {
            "helvetica" or "arial" or "sans-serif" => bold && italic ? "Helvetica-BoldOblique" :
                                                       bold ? "Helvetica-Bold" :
                                                       italic ? "Helvetica-Oblique" :
                                                       "Helvetica",
            "times" or "times new roman" or "serif" or "times-roman" => bold && italic ? "Times-BoldItalic" :
                                                       bold ? "Times-Bold" :
                                                       italic ? "Times-Italic" :
                                                       "Times-Roman",
            "courier" or "courier new" or "monospace" => bold && italic ? "Courier-BoldOblique" :
                                                         bold ? "Courier-Bold" :
                                                         italic ? "Courier-Oblique" :
                                                         "Courier",
            _ => "Helvetica"
        };

        return _fonts.TryGetValue(key, out var font) ? font : _fonts["Helvetica"];
    }
}
