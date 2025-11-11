namespace Folly.Fonts;

/// <summary>
/// Font weight values following CSS font-weight specification.
/// </summary>
public enum FontWeight
{
    /// <summary>
    /// Normal font weight (400).
    /// </summary>
    Normal = 400,

    /// <summary>
    /// Bold font weight (700).
    /// </summary>
    Bold = 700
}

/// <summary>
/// Font style values following CSS font-style specification.
/// </summary>
public enum FontStyle
{
    /// <summary>
    /// Normal (upright) font style.
    /// </summary>
    Normal,

    /// <summary>
    /// Italic font style (uses designed italic variants).
    /// </summary>
    Italic,

    /// <summary>
    /// Oblique font style (slanted version of normal font).
    /// </summary>
    Oblique
}

/// <summary>
/// Centralized font resolution service that handles mapping from
/// generic font families and style properties to specific font names.
/// </summary>
public static class FontResolver
{
    /// <summary>
    /// Resolves a font family name, weight, and style to a specific font name.
    /// </summary>
    /// <param name="familyName">The font family name (e.g., "serif", "Helvetica", "Times-Roman")</param>
    /// <param name="isBold">Whether the font should be bold</param>
    /// <param name="isItalic">Whether the font should be italic or oblique</param>
    /// <returns>The resolved font name (e.g., "Times-BoldItalic")</returns>
    public static string ResolveFont(string familyName, bool isBold, bool isItalic)
    {
        // Normalize the family name to a base font family
        var normalizedFamily = NormalizeFamily(familyName);

        // Apply weight and style variants
        return FontVariantRegistry.GetVariant(normalizedFamily, isBold, isItalic);
    }

    /// <summary>
    /// Resolves a font family name and CSS-style font-weight string to a specific font name.
    /// </summary>
    /// <param name="familyName">The font family name</param>
    /// <param name="fontWeight">CSS font-weight value (e.g., "bold", "700", "400")</param>
    /// <param name="fontStyle">CSS font-style value (e.g., "italic", "oblique", "normal")</param>
    /// <returns>The resolved font name</returns>
    public static string ResolveFont(string familyName, string? fontWeight, string? fontStyle)
    {
        var isBold = IsBoldWeight(fontWeight);
        var isItalic = IsItalicStyle(fontStyle);

        return ResolveFont(familyName, isBold, isItalic);
    }

    /// <summary>
    /// Normalizes generic font family names to specific base font families.
    /// </summary>
    /// <param name="familyName">The font family name to normalize</param>
    /// <returns>The normalized base font family name</returns>
    public static string NormalizeFamily(string familyName)
    {
        if (string.IsNullOrEmpty(familyName))
            return "Helvetica";

        return familyName.ToLowerInvariant() switch
        {
            // Serif family mappings
            "serif" => "Times-Roman",
            "times" => "Times-Roman",
            "times new roman" => "Times-Roman",
            "times-roman" => "Times-Roman",

            // Sans-serif family mappings
            "sans-serif" => "Helvetica",
            "helvetica" => "Helvetica",
            "arial" => "Helvetica",

            // Monospace family mappings
            "monospace" => "Courier",
            "courier" => "Courier",
            "courier new" => "Courier",

            // Already a specific font name (case-insensitive match)
            // Check if it's already a known variant font name
            _ when IsKnownFontName(familyName) => GetCanonicalFontName(familyName),

            // Unknown family, return as-is (will be handled by fallback logic)
            _ => familyName
        };
    }

    /// <summary>
    /// Determines if a font weight string represents a bold weight.
    /// </summary>
    /// <param name="fontWeight">CSS font-weight value</param>
    /// <returns>True if the weight is bold (≥700), false otherwise</returns>
    public static bool IsBoldWeight(string? fontWeight)
    {
        if (string.IsNullOrEmpty(fontWeight))
            return false;

        // Check for keyword "bold"
        if (fontWeight.Equals("bold", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check for numeric weight ≥ 700
        if (int.TryParse(fontWeight, out var weight))
            return weight >= 700;

        return false;
    }

    /// <summary>
    /// Determines if a font style string represents an italic or oblique style.
    /// </summary>
    /// <param name="fontStyle">CSS font-style value</param>
    /// <returns>True if the style is italic or oblique, false otherwise</returns>
    public static bool IsItalicStyle(string? fontStyle)
    {
        if (string.IsNullOrEmpty(fontStyle))
            return false;

        return fontStyle.Equals("italic", StringComparison.OrdinalIgnoreCase) ||
               fontStyle.Equals("oblique", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a font name is a known font variant name.
    /// </summary>
    private static bool IsKnownFontName(string fontName)
    {
        var normalized = fontName.ToLowerInvariant();

        return normalized switch
        {
            // Helvetica variants
            "helvetica" or "helvetica-bold" or "helvetica-oblique" or "helvetica-boldoblique" => true,

            // Times variants
            "times-roman" or "times-bold" or "times-italic" or "times-bolditalic" => true,

            // Courier variants
            "courier" or "courier-bold" or "courier-oblique" or "courier-boldoblique" => true,

            // Symbol fonts
            "symbol" or "zapfdingbats" => true,

            _ => false
        };
    }

    /// <summary>
    /// Gets the canonical (properly-cased) font name for a known font.
    /// </summary>
    private static string GetCanonicalFontName(string fontName)
    {
        var normalized = fontName.ToLowerInvariant();

        return normalized switch
        {
            // Helvetica variants
            "helvetica" => "Helvetica",
            "helvetica-bold" => "Helvetica-Bold",
            "helvetica-oblique" => "Helvetica-Oblique",
            "helvetica-boldoblique" => "Helvetica-BoldOblique",

            // Times variants
            "times-roman" => "Times-Roman",
            "times-bold" => "Times-Bold",
            "times-italic" => "Times-Italic",
            "times-bolditalic" => "Times-BoldItalic",

            // Courier variants
            "courier" => "Courier",
            "courier-bold" => "Courier-Bold",
            "courier-oblique" => "Courier-Oblique",
            "courier-boldoblique" => "Courier-BoldOblique",

            // Symbol fonts
            "symbol" => "Symbol",
            "zapfdingbats" => "ZapfDingbats",

            _ => fontName // Return as-is if not recognized
        };
    }
}
