namespace Folly.Xslfo;

/// <summary>
/// Helper class for mapping relative directional properties to absolute properties
/// based on the writing-mode.
/// </summary>
public static class WritingModeHelper
{
    /// <summary>
    /// Maps a relative directional property to its absolute equivalent based on writing-mode.
    /// </summary>
    /// <param name="relativeProperty">The relative property name (e.g., "padding-before", "margin-start")</param>
    /// <param name="writingMode">The writing mode (lr-tb, rl-tb, tb-rl, tb-lr)</param>
    /// <returns>The absolute property name (e.g., "padding-top", "margin-left")</returns>
    public static string MapToAbsolute(string relativeProperty, string writingMode)
    {
        // Parse the property name to extract the property type and direction
        var parts = relativeProperty.Split('-');
        if (parts.Length < 2)
            return relativeProperty; // Not a directional property

        var direction = parts[^1]; // Last part is the direction (before, after, start, end)
        var propertyBase = string.Join("-", parts[..^1]); // Everything before the direction

        // Map direction to absolute based on writing-mode
        var absoluteDirection = MapDirection(direction, writingMode);

        return $"{propertyBase}-{absoluteDirection}";
    }

    /// <summary>
    /// Maps a relative direction to an absolute direction based on writing-mode.
    /// </summary>
    /// <param name="direction">Relative direction: before, after, start, or end</param>
    /// <param name="writingMode">The writing mode</param>
    /// <returns>Absolute direction: top, bottom, left, or right</returns>
    private static string MapDirection(string direction, string writingMode)
    {
        // Normalize writing-mode
        var mode = writingMode?.ToLowerInvariant() ?? "lr-tb";

        return mode switch
        {
            // Left-to-right, top-to-bottom (default, Western languages)
            "lr-tb" or "lr" => direction switch
            {
                "before" => "top",
                "after" => "bottom",
                "start" => "left",
                "end" => "right",
                _ => direction
            },

            // Right-to-left, top-to-bottom (Arabic, Hebrew)
            "rl-tb" or "rl" => direction switch
            {
                "before" => "top",
                "after" => "bottom",
                "start" => "right",  // Start is on the right in RTL
                "end" => "left",      // End is on the left in RTL
                _ => direction
            },

            // Top-to-bottom, right-to-left (Traditional Chinese, Japanese)
            "tb-rl" or "tb" => direction switch
            {
                "before" => "right",  // Before is on the right
                "after" => "left",    // After is on the left
                "start" => "top",     // Start is at the top
                "end" => "bottom",    // End is at the bottom
                _ => direction
            },

            // Top-to-bottom, left-to-right (Mongolian)
            "tb-lr" => direction switch
            {
                "before" => "left",   // Before is on the left
                "after" => "right",   // After is on the right
                "start" => "top",     // Start is at the top
                "end" => "bottom",    // End is at the bottom
                _ => direction
            },

            // Unknown writing mode, assume lr-tb
            _ => direction switch
            {
                "before" => "top",
                "after" => "bottom",
                "start" => "left",
                "end" => "right",
                _ => direction
            }
        };
    }

    /// <summary>
    /// Gets a length property value with writing-mode-aware directional property mapping.
    /// </summary>
    /// <param name="properties">The properties object</param>
    /// <param name="relativeProperty">The relative property name (e.g., "padding-before")</param>
    /// <param name="absoluteProperty">The absolute property name (e.g., "padding-top")</param>
    /// <param name="writingMode">The writing mode</param>
    /// <param name="defaultValue">Default value if property is not found</param>
    /// <param name="genericProperty">Optional generic property to check (e.g., "padding" for padding-before/padding-top)</param>
    /// <returns>The property value in points</returns>
    public static double GetDirectionalLength(
        FoProperties properties,
        string relativeProperty,
        string absoluteProperty,
        string writingMode,
        double defaultValue = 0,
        string? genericProperty = null)
    {
        // First, check if the relative property is explicitly set
        if (properties.HasProperty(relativeProperty))
            return properties.GetLength(relativeProperty, defaultValue);

        // Then check if the absolute property is explicitly set
        if (properties.HasProperty(absoluteProperty))
            return properties.GetLength(absoluteProperty, defaultValue);

        // If we're in a non-default writing mode, check if the property might be
        // specified using a different relative direction that maps to the same absolute direction
        if (writingMode != "lr-tb" && writingMode != "lr")
        {
            // Try to find any relative property that maps to our absolute property
            var relativeDirection = GetRelativeDirection(relativeProperty);
            if (relativeDirection != null)
            {
                // Find which relative direction maps to our target absolute in this writing mode
                var alternateRelative = FindAlternateRelativeProperty(relativeProperty, absoluteProperty, writingMode);
                if (alternateRelative != null && properties.HasProperty(alternateRelative))
                    return properties.GetLength(alternateRelative, defaultValue);
            }
        }

        // Check generic property if provided
        if (genericProperty != null && properties.HasProperty(genericProperty))
            return properties.GetLength(genericProperty, defaultValue);

        return defaultValue;
    }

    /// <summary>
    /// Gets a string property value with writing-mode-aware directional property mapping.
    /// </summary>
    public static string GetDirectionalString(
        FoProperties properties,
        string relativeProperty,
        string absoluteProperty,
        string writingMode,
        string defaultValue)
    {
        // First, check if the relative property is explicitly set
        if (properties.HasProperty(relativeProperty))
            return properties.GetString(relativeProperty, defaultValue);

        // Then check if the absolute property is explicitly set
        if (properties.HasProperty(absoluteProperty))
            return properties.GetString(absoluteProperty, defaultValue);

        // Check for alternate relative properties in non-default writing modes
        if (writingMode != "lr-tb" && writingMode != "lr")
        {
            var alternateRelative = FindAlternateRelativeProperty(relativeProperty, absoluteProperty, writingMode);
            if (alternateRelative != null && properties.HasProperty(alternateRelative))
                return properties.GetString(alternateRelative, defaultValue);
        }

        return defaultValue;
    }

    private static string? GetRelativeDirection(string propertyName)
    {
        var parts = propertyName.Split('-');
        if (parts.Length < 2)
            return null;

        var lastPart = parts[^1];
        return lastPart is "before" or "after" or "start" or "end" ? lastPart : null;
    }

    private static string? FindAlternateRelativeProperty(string relativeProperty, string absoluteProperty, string writingMode)
    {
        // Extract the property base (e.g., "padding" from "padding-before")
        var parts = relativeProperty.Split('-');
        if (parts.Length < 2)
            return null;

        var propertyBase = string.Join("-", parts[..^1]);

        // Extract the target absolute direction
        var absoluteParts = absoluteProperty.Split('-');
        var targetDirection = absoluteParts[^1];

        // Find which relative direction maps to our target in this writing mode
        foreach (var relDir in new[] { "before", "after", "start", "end" })
        {
            if (MapDirection(relDir, writingMode) == targetDirection)
            {
                return $"{propertyBase}-{relDir}";
            }
        }

        return null;
    }
}
