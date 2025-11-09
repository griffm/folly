using System.Globalization;
using System.Reflection;

namespace Folly.Fonts;

/// <summary>
/// Parsed AFM (Adobe Font Metrics) data.
/// </summary>
internal sealed class AfmData
{
    public string FontName { get; init; } = "";
    public double Ascender { get; init; }
    public double Descender { get; init; }
    public Dictionary<int, double> CharWidths { get; init; } = new();
    public double DefaultWidth { get; init; }
}

/// <summary>
/// Parser for AFM (Adobe Font Metrics) files.
/// </summary>
internal static class AfmParser
{
    /// <summary>
    /// Parses an AFM file from embedded resources.
    /// </summary>
    /// <param name="resourcePath">Path to the AFM resource (e.g., "fonts.base14.Helvetica.afm")</param>
    /// <returns>Parsed AFM data</returns>
    public static AfmData Parse(string resourcePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullResourcePath = $"Folly.Fonts.{resourcePath}";

        using var stream = assembly.GetManifestResourceStream(fullResourcePath);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {fullResourcePath}");
        }

        using var reader = new StreamReader(stream);
        return ParseStream(reader);
    }

    /// <summary>
    /// Parses an AFM file from a stream.
    /// </summary>
    private static AfmData ParseStream(StreamReader reader)
    {
        var fontName = "";
        var ascender = 0.0;
        var descender = 0.0;
        var charWidths = new Dictionary<int, double>();
        var defaultWidth = 500.0; // Reasonable fallback
        var inCharMetrics = false;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Parse header fields
            if (line.StartsWith("FontName "))
            {
                fontName = line.Substring("FontName ".Length).Trim();
            }
            else if (line.StartsWith("Ascender "))
            {
                if (double.TryParse(line.Substring("Ascender ".Length).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    ascender = value;
            }
            else if (line.StartsWith("Descender "))
            {
                if (double.TryParse(line.Substring("Descender ".Length).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    descender = value;
            }
            else if (line.StartsWith("StartCharMetrics"))
            {
                inCharMetrics = true;
            }
            else if (line.StartsWith("EndCharMetrics"))
            {
                inCharMetrics = false;
            }
            else if (inCharMetrics && line.StartsWith("C "))
            {
                // Parse character metrics: C 32 ; WX 278 ; N space ; B 0 0 0 0 ;
                var parts = line.Split(';');
                if (parts.Length >= 2)
                {
                    var charCode = -1;
                    var width = 0.0;

                    foreach (var part in parts)
                    {
                        var trimmed = part.Trim();

                        // Parse character code
                        if (trimmed.StartsWith("C "))
                        {
                            var codeStr = trimmed.Substring(2).Trim();
                            int.TryParse(codeStr, out charCode);
                        }
                        // Parse width
                        else if (trimmed.StartsWith("WX "))
                        {
                            var widthStr = trimmed.Substring(3).Trim();
                            double.TryParse(widthStr, NumberStyles.Float, CultureInfo.InvariantCulture, out width);
                        }
                    }

                    if (charCode >= 0 && width > 0)
                    {
                        charWidths[charCode] = width;
                    }
                }
            }
        }

        // Calculate average width as default
        if (charWidths.Count > 0)
        {
            defaultWidth = charWidths.Values.Average();
        }

        return new AfmData
        {
            FontName = fontName,
            Ascender = ascender,
            Descender = descender,
            CharWidths = charWidths,
            DefaultWidth = defaultWidth
        };
    }

    /// <summary>
    /// Parses an AFM file and loads character widths into a StandardFont.
    /// </summary>
    /// <param name="resourcePath">Path to the AFM resource (e.g., "fonts.base14.Helvetica.afm")</param>
    /// <returns>A StandardFont with accurate metrics from the AFM file</returns>
    public static StandardFont LoadFont(string resourcePath)
    {
        try
        {
            var afmData = Parse(resourcePath);
            var font = new StandardFont(afmData.FontName, afmData.Ascender, afmData.Descender, afmData.DefaultWidth);

            // Load character widths
            foreach (var (charCode, width) in afmData.CharWidths)
            {
                // AFM files use ASCII codes (0-255 range typically)
                if (charCode >= 0 && charCode <= 255)
                {
                    font.SetCharWidth((char)charCode, width);
                }
            }

            return font;
        }
        catch (Exception ex)
        {
            // Log warning and return null - caller should fall back to hardcoded metrics
            Console.WriteLine($"Warning: Failed to load AFM file {resourcePath}: {ex.Message}");
            throw;
        }
    }
}
