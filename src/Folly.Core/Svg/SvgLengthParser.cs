namespace Folly.Svg;

/// <summary>
/// Parses SVG length values with unit support.
/// Supports: px, pt, pc, mm, cm, in, em, rem, %, and unitless values.
/// </summary>
public static class SvgLengthParser
{
    private const double PxPerPt = 96.0 / 72.0; // 1 pt = 1/72 inch, 1 px = 1/96 inch
    private const double PxPerPc = 96.0 / 6.0;  // 1 pc = 12 pt = 1/6 inch
    private const double PxPerMm = 96.0 / 25.4; // 1 inch = 25.4 mm
    private const double PxPerCm = 96.0 / 2.54; // 1 inch = 2.54 cm
    private const double PxPerIn = 96.0;        // 1 inch = 96 px (CSS reference)

    /// <summary>
    /// Parses a length value to pixels.
    /// </summary>
    /// <param name="value">The length string (e.g., "10px", "5cm", "100%").</param>
    /// <param name="defaultValue">Default value if parsing fails.</param>
    /// <param name="fontSize">Font size for em/rem units (default 16px).</param>
    /// <param name="referenceLength">Reference length for percentage values.</param>
    /// <returns>The length in pixels.</returns>
    public static double Parse(string? value, double defaultValue = 0, double fontSize = 16, double referenceLength = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        value = value.Trim();

        // Try to parse as a plain number (unitless = pixels in SVG)
        if (double.TryParse(value, out double numericValue))
            return numericValue;

        // Extract number and unit
        int unitStart = 0;
        for (int i = value.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(value[i]) || value[i] == '.' || value[i] == '-' || value[i] == '+')
            {
                unitStart = i + 1;
                break;
            }
        }

        if (unitStart == 0)
            return defaultValue;

        var numberPart = value[..unitStart].Trim();
        var unitPart = value[unitStart..].Trim().ToLowerInvariant();

        if (!double.TryParse(numberPart, out double number))
            return defaultValue;

        // Convert to pixels based on unit
        return unitPart switch
        {
            "px" => number,
            "pt" => number * PxPerPt,
            "pc" => number * PxPerPc,
            "mm" => number * PxPerMm,
            "cm" => number * PxPerCm,
            "in" => number * PxPerIn,
            "em" => number * fontSize,
            "rem" => number * fontSize, // TODO: Use root font size, not current
            "%" => number * referenceLength / 100.0,
            "" => number, // Unitless = pixels
            _ => defaultValue
        };
    }

    /// <summary>
    /// Parses a length value to points (1/72 inch) for PDF.
    /// </summary>
    public static double ParseToPt(string? value, double defaultValue = 0, double fontSize = 16, double referenceLength = 0)
    {
        var px = Parse(value, defaultValue, fontSize, referenceLength);
        return px / PxPerPt; // Convert px to pt
    }

    /// <summary>
    /// Parses a list of length values (e.g., for viewBox: "0 0 100 100").
    /// </summary>
    public static double[] ParseList(string? value, int expectedCount = -1)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<double>();

        var parts = value.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<double>();

        foreach (var part in parts)
        {
            if (double.TryParse(part.Trim(), out double num))
                result.Add(num);
        }

        if (expectedCount > 0 && result.Count != expectedCount)
            return Array.Empty<double>();

        return result.ToArray();
    }
}
