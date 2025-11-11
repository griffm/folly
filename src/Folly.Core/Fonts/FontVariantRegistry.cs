namespace Folly.Fonts;

/// <summary>
/// Registry for font variant names. Maps base font families combined with
/// bold and italic flags to specific font variant names.
///
/// This provides a centralized, data-driven approach to font variant
/// resolution, making it easy to add new font families and maintain
/// consistent naming.
/// </summary>
internal static class FontVariantRegistry
{
    /// <summary>
    /// Font variant key combining family name with style flags.
    /// </summary>
    private record FontVariantKey(string Family, bool Bold, bool Italic);

    /// <summary>
    /// Lookup table mapping font variant keys to specific font names.
    /// </summary>
    private static readonly Dictionary<FontVariantKey, string> _variants = new()
    {
        // Helvetica family (uses "Oblique" for italic)
        [new FontVariantKey("Helvetica", false, false)] = "Helvetica",
        [new FontVariantKey("Helvetica", true, false)] = "Helvetica-Bold",
        [new FontVariantKey("Helvetica", false, true)] = "Helvetica-Oblique",
        [new FontVariantKey("Helvetica", true, true)] = "Helvetica-BoldOblique",

        // Times family (uses "Italic" for italic)
        [new FontVariantKey("Times-Roman", false, false)] = "Times-Roman",
        [new FontVariantKey("Times-Roman", true, false)] = "Times-Bold",
        [new FontVariantKey("Times-Roman", false, true)] = "Times-Italic",
        [new FontVariantKey("Times-Roman", true, true)] = "Times-BoldItalic",

        // Courier family (uses "Oblique" for italic)
        [new FontVariantKey("Courier", false, false)] = "Courier",
        [new FontVariantKey("Courier", true, false)] = "Courier-Bold",
        [new FontVariantKey("Courier", false, true)] = "Courier-Oblique",
        [new FontVariantKey("Courier", true, true)] = "Courier-BoldOblique",

        // Symbol fonts (no variants)
        [new FontVariantKey("Symbol", false, false)] = "Symbol",
        [new FontVariantKey("Symbol", true, false)] = "Symbol",
        [new FontVariantKey("Symbol", false, true)] = "Symbol",
        [new FontVariantKey("Symbol", true, true)] = "Symbol",

        [new FontVariantKey("ZapfDingbats", false, false)] = "ZapfDingbats",
        [new FontVariantKey("ZapfDingbats", true, false)] = "ZapfDingbats",
        [new FontVariantKey("ZapfDingbats", false, true)] = "ZapfDingbats",
        [new FontVariantKey("ZapfDingbats", true, true)] = "ZapfDingbats",
    };

    /// <summary>
    /// Gets the specific font variant name for a base family with style flags.
    /// </summary>
    /// <param name="baseFamily">The base font family (e.g., "Helvetica", "Times-Roman", "Courier")</param>
    /// <param name="bold">Whether to apply bold weight</param>
    /// <param name="italic">Whether to apply italic/oblique style</param>
    /// <returns>The specific font variant name (e.g., "Helvetica-BoldOblique")</returns>
    public static string GetVariant(string baseFamily, bool bold, bool italic)
    {
        var key = new FontVariantKey(baseFamily, bold, italic);

        // Try exact match first
        if (_variants.TryGetValue(key, out var variant))
            return variant;

        // Fallback: try to construct variant name using common patterns
        // This allows for future extensibility with custom fonts
        return ConstructVariantName(baseFamily, bold, italic);
    }

    /// <summary>
    /// Constructs a variant name using common naming patterns when not found in registry.
    /// This provides a fallback for custom fonts that follow standard naming conventions.
    /// </summary>
    private static string ConstructVariantName(string baseFamily, bool bold, bool italic)
    {
        // If neither bold nor italic, return base name
        if (!bold && !italic)
            return baseFamily;

        // Determine the suffix based on base family naming patterns
        var suffix = DetermineSuffix(baseFamily, bold, italic);

        // Check if base family already has a variant suffix and remove it
        var cleanFamily = RemoveVariantSuffix(baseFamily);

        return $"{cleanFamily}{suffix}";
    }

    /// <summary>
    /// Determines the appropriate suffix for a font variant.
    /// </summary>
    private static string DetermineSuffix(string baseFamily, bool bold, bool italic)
    {
        // Determine if this family uses "Oblique" or "Italic" for slanted text
        var usesOblique = baseFamily.Contains("Helvetica", StringComparison.OrdinalIgnoreCase) ||
                          baseFamily.Contains("Courier", StringComparison.OrdinalIgnoreCase);

        var usesItalic = baseFamily.Contains("Times", StringComparison.OrdinalIgnoreCase);

        if (bold && italic)
        {
            if (usesOblique)
                return "-BoldOblique";
            if (usesItalic)
                return "-BoldItalic";
            return "-BoldItalic"; // Default to Italic for unknown fonts
        }

        if (bold)
            return "-Bold";

        if (italic)
        {
            if (usesOblique)
                return "-Oblique";
            if (usesItalic)
                return "-Italic";
            return "-Italic"; // Default to Italic for unknown fonts
        }

        return "";
    }

    /// <summary>
    /// Removes variant suffixes from a font name to get the base family.
    /// </summary>
    private static string RemoveVariantSuffix(string fontName)
    {
        // Remove common variant suffixes
        var suffixes = new[]
        {
            "-BoldOblique",
            "-BoldItalic",
            "-Bold",
            "-Oblique",
            "-Italic"
        };

        foreach (var suffix in suffixes)
        {
            if (fontName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return fontName[..^suffix.Length];
            }
        }

        return fontName;
    }

    /// <summary>
    /// Registers a custom font family with its variants.
    /// This can be used to extend the registry with additional fonts.
    /// </summary>
    /// <param name="baseFamily">The base font family name</param>
    /// <param name="regular">Regular variant name</param>
    /// <param name="bold">Bold variant name (null if not available)</param>
    /// <param name="italic">Italic/Oblique variant name (null if not available)</param>
    /// <param name="boldItalic">Bold Italic/Oblique variant name (null if not available)</param>
    public static void RegisterFamily(
        string baseFamily,
        string regular,
        string? bold = null,
        string? italic = null,
        string? boldItalic = null)
    {
        _variants[new FontVariantKey(baseFamily, false, false)] = regular;
        _variants[new FontVariantKey(baseFamily, true, false)] = bold ?? regular;
        _variants[new FontVariantKey(baseFamily, false, true)] = italic ?? regular;
        _variants[new FontVariantKey(baseFamily, true, true)] = boldItalic ?? bold ?? italic ?? regular;
    }
}
