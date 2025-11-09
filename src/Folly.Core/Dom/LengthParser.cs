namespace Folly.Dom;

/// <summary>
/// Parses XSL-FO length values into points.
/// </summary>
internal static class LengthParser
{
    private const double InchToPoints = 72.0;
    private const double CmToPoints = 28.35;
    private const double MmToPoints = 2.835;
    private const double PicaToPoints = 12.0;

    /// <summary>
    /// Parses a length value and returns it in points.
    /// Supports: pt, px, in, cm, mm, pc
    /// </summary>
    public static double Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        value = value.Trim();

        // Try to extract number and unit
        var numEnd = 0;
        while (numEnd < value.Length && (char.IsDigit(value[numEnd]) || value[numEnd] == '.' || value[numEnd] == '-'))
            numEnd++;

        if (numEnd == 0)
            return 0;

        if (!double.TryParse(value.Substring(0, numEnd), out var number))
            return 0;

        var unit = value.Substring(numEnd).Trim().ToLowerInvariant();

        return unit switch
        {
            "pt" or "" => number, // Points or unitless (assume points)
            "px" => number, // Pixels (treat as points for now)
            "in" => number * InchToPoints,
            "cm" => number * CmToPoints,
            "mm" => number * MmToPoints,
            "pc" => number * PicaToPoints,
            _ => number // Default to points
        };
    }
}
