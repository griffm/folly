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
        InitializeHelvetica();
        InitializeTimes();
        InitializeCourier();
    }

    public static StandardFont GetFont(string familyName, bool bold, bool italic)
    {
        var key = familyName.ToLowerInvariant() switch
        {
            "helvetica" or "arial" or "sans-serif" => bold && italic ? "Helvetica-BoldOblique" :
                                                       bold ? "Helvetica-Bold" :
                                                       italic ? "Helvetica-Oblique" :
                                                       "Helvetica",
            "times" or "times new roman" or "serif" => bold && italic ? "Times-BoldItalic" :
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

    private static void InitializeHelvetica()
    {
        // Load Helvetica fonts from AFM files
        try
        {
            _fonts["Helvetica"] = AfmParser.LoadFont("base14.Helvetica.afm");
            _fonts["Helvetica-Bold"] = AfmParser.LoadFont("base14.Helvetica-Bold.afm");
            _fonts["Helvetica-Oblique"] = AfmParser.LoadFont("base14.Helvetica-Oblique.afm");
            _fonts["Helvetica-BoldOblique"] = AfmParser.LoadFont("base14.Helvetica-BoldOblique.afm");
        }
        catch (Exception ex)
        {
            // Fallback to approximate metrics if AFM loading fails
            Console.WriteLine($"Warning: Failed to load Helvetica AFM files, using fallback metrics: {ex.Message}");

            var helvetica = new StandardFont("Helvetica", 718, -207, 556);
            SetCommonCharWidths(helvetica, 556, 278);
            _fonts["Helvetica"] = helvetica;

            var helveticaBold = new StandardFont("Helvetica-Bold", 718, -207, 556);
            SetCommonCharWidths(helveticaBold, 556, 278);
            _fonts["Helvetica-Bold"] = helveticaBold;

            var helveticaOblique = new StandardFont("Helvetica-Oblique", 718, -207, 556);
            SetCommonCharWidths(helveticaOblique, 556, 278);
            _fonts["Helvetica-Oblique"] = helveticaOblique;

            var helveticaBoldOblique = new StandardFont("Helvetica-BoldOblique", 718, -207, 556);
            SetCommonCharWidths(helveticaBoldOblique, 556, 278);
            _fonts["Helvetica-BoldOblique"] = helveticaBoldOblique;
        }
    }

    private static void InitializeTimes()
    {
        // Load Times fonts from AFM files
        try
        {
            _fonts["Times-Roman"] = AfmParser.LoadFont("base14.Times-Roman.afm");
            _fonts["Times-Bold"] = AfmParser.LoadFont("base14.Times-Bold.afm");
            _fonts["Times-Italic"] = AfmParser.LoadFont("base14.Times-Italic.afm");
            _fonts["Times-BoldItalic"] = AfmParser.LoadFont("base14.Times-BoldItalic.afm");
        }
        catch (Exception ex)
        {
            // Fallback to approximate metrics if AFM loading fails
            Console.WriteLine($"Warning: Failed to load Times AFM files, using fallback metrics: {ex.Message}");

            var times = new StandardFont("Times-Roman", 683, -217, 500);
            SetCommonCharWidths(times, 500, 250);
            _fonts["Times-Roman"] = times;

            var timesBold = new StandardFont("Times-Bold", 683, -217, 500);
            SetCommonCharWidths(timesBold, 500, 250);
            _fonts["Times-Bold"] = timesBold;

            var timesItalic = new StandardFont("Times-Italic", 683, -217, 500);
            SetCommonCharWidths(timesItalic, 500, 250);
            _fonts["Times-Italic"] = timesItalic;

            var timesBoldItalic = new StandardFont("Times-BoldItalic", 683, -217, 500);
            SetCommonCharWidths(timesBoldItalic, 500, 250);
            _fonts["Times-BoldItalic"] = timesBoldItalic;
        }
    }

    private static void InitializeCourier()
    {
        // Load Courier fonts from AFM files
        try
        {
            _fonts["Courier"] = AfmParser.LoadFont("base14.Courier.afm");
            _fonts["Courier-Bold"] = AfmParser.LoadFont("base14.Courier-Bold.afm");
            _fonts["Courier-Oblique"] = AfmParser.LoadFont("base14.Courier-Oblique.afm");
            _fonts["Courier-BoldOblique"] = AfmParser.LoadFont("base14.Courier-BoldOblique.afm");
        }
        catch (Exception ex)
        {
            // Fallback to approximate metrics if AFM loading fails
            Console.WriteLine($"Warning: Failed to load Courier AFM files, using fallback metrics: {ex.Message}");

            var courier = new StandardFont("Courier", 629, -157, 600);
            SetCommonCharWidths(courier, 600, 600);
            _fonts["Courier"] = courier;

            var courierBold = new StandardFont("Courier-Bold", 629, -157, 600);
            SetCommonCharWidths(courierBold, 600, 600);
            _fonts["Courier-Bold"] = courierBold;

            var courierOblique = new StandardFont("Courier-Oblique", 629, -157, 600);
            SetCommonCharWidths(courierOblique, 600, 600);
            _fonts["Courier-Oblique"] = courierOblique;

            var courierBoldOblique = new StandardFont("Courier-BoldOblique", 629, -157, 600);
            SetCommonCharWidths(courierBoldOblique, 600, 600);
            _fonts["Courier-BoldOblique"] = courierBoldOblique;
        }
    }

    private static void SetCommonCharWidths(StandardFont font, double averageWidth, double spaceWidth)
    {
        // Set widths for common characters (simplified - using average for most)
        font.SetCharWidth(' ', spaceWidth);

        // Lowercase letters
        for (char c = 'a'; c <= 'z'; c++)
            font.SetCharWidth(c, averageWidth * 0.9);

        // Uppercase letters
        for (char c = 'A'; c <= 'Z'; c++)
            font.SetCharWidth(c, averageWidth * 1.1);

        // Digits
        for (char c = '0'; c <= '9'; c++)
            font.SetCharWidth(c, averageWidth);

        // Common punctuation
        font.SetCharWidth('.', averageWidth * 0.4);
        font.SetCharWidth(',', averageWidth * 0.4);
        font.SetCharWidth('!', averageWidth * 0.5);
        font.SetCharWidth('?', averageWidth * 0.8);
        font.SetCharWidth(':', averageWidth * 0.4);
        font.SetCharWidth(';', averageWidth * 0.4);
        font.SetCharWidth('-', averageWidth * 0.6);
        font.SetCharWidth('(', averageWidth * 0.5);
        font.SetCharWidth(')', averageWidth * 0.5);
    }
}
