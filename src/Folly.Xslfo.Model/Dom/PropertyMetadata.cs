namespace Folly.Xslfo;

/// <summary>
/// Provides metadata about XSL-FO properties, including inheritance rules.
/// Based on XSL-FO 1.1 specification.
/// </summary>
public static class PropertyMetadata
{
    /// <summary>
    /// Set of properties that inherit from parent to child according to XSL-FO 1.1 spec.
    /// </summary>
    private static readonly HashSet<string> InheritableProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        // Font properties
        "font-family",
        "font-size",
        "font-style",
        "font-variant",
        "font-weight",
        "font-stretch",
        "font-size-adjust",

        // Text properties
        "color",
        "line-height",
        "text-align",
        "text-align-last",
        "text-indent",
        "text-transform",
        "white-space",
        "white-space-collapse",
        "white-space-treatment",
        "word-spacing",
        "letter-spacing",
        "text-decoration",

        // Writing mode properties
        "writing-mode",
        "direction",
        "unicode-bidi",
        "glyph-orientation-horizontal",
        "glyph-orientation-vertical",

        // Visibility and rendering
        "visibility",

        // Hyphenation properties
        "hyphenate",
        "hyphenation-character",
        "hyphenation-push-character-count",
        "hyphenation-remain-character-count",
        "country",
        "language",

        // Leaders
        "leader-alignment",
        "leader-pattern",
        "leader-pattern-width",
        "leader-length",

        // Table properties
        "border-collapse",
        "border-spacing",
        "caption-side",
        "empty-cells",

        // List properties
        "provisional-distance-between-starts",
        "provisional-label-separation",

        // Page break properties (inherit in some contexts)
        "orphans",
        "widows",

        // Accessibility
        "role",
        "source-document",

        // Score spaces
        "score-spaces",

        // Reference orientation
        "reference-orientation",

        // Span
        "span",
    };

    /// <summary>
    /// Checks if a property value should be inherited from parent to child.
    /// </summary>
    /// <param name="propertyName">The name of the property (case-insensitive).</param>
    /// <returns>True if the property inherits by default, false otherwise.</returns>
    public static bool IsInheritable(string propertyName)
    {
        return InheritableProperties.Contains(propertyName);
    }

    /// <summary>
    /// Gets the default value for a property. Returns null if no default is defined.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The default value, or null if none is defined.</returns>
    public static string? GetDefaultValue(string propertyName)
    {
        return propertyName.ToLowerInvariant() switch
        {
            // Font defaults
            "font-family" => "serif",
            "font-size" => "12pt",
            "font-style" => "normal",
            "font-variant" => "normal",
            "font-weight" => "normal",
            "font-stretch" => "normal",

            // Text defaults
            "color" => "black",
            "line-height" => "normal",
            "text-align" => "start",
            "text-indent" => "0pt",
            "text-transform" => "none",
            "white-space" => "normal",
            "word-spacing" => "normal",
            "letter-spacing" => "normal",
            "text-decoration" => "none",

            // Writing mode defaults
            "writing-mode" => "lr-tb",
            "direction" => "ltr",

            // Visibility
            "visibility" => "visible",

            // Margins and padding (non-inheritable)
            "margin-top" => "0pt",
            "margin-bottom" => "0pt",
            "margin-left" => "0pt",
            "margin-right" => "0pt",
            "padding-top" => "0pt",
            "padding-bottom" => "0pt",
            "padding-left" => "0pt",
            "padding-right" => "0pt",

            // Borders (non-inheritable)
            "border-width" => "0pt",
            "border-style" => "none",
            "border-color" => "black",

            // Background (non-inheritable)
            "background-color" => "transparent",

            _ => null
        };
    }
}
