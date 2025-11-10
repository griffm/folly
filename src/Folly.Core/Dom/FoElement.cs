namespace Folly.Dom;

/// <summary>
/// Base class for all XSL-FO elements.
/// </summary>
public abstract class FoElement
{
    /// <summary>
    /// Gets the element name (e.g., "root", "block", "page-sequence").
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the properties defined on this element.
    /// </summary>
    public FoProperties Properties { get; init; } = new();

    /// <summary>
    /// Gets the child elements.
    /// </summary>
    public IReadOnlyList<FoElement> Children { get; init; } = Array.Empty<FoElement>();

    /// <summary>
    /// Gets text content if this is a text node.
    /// </summary>
    public string? TextContent { get; init; }

    /// <summary>
    /// Gets or sets the parent element. Null for root element.
    /// </summary>
    public FoElement? Parent { get; set; }

    /// <summary>
    /// Gets the writing mode for this element, with inheritance from parent elements.
    /// The writing mode affects how relative directional properties (before/after/start/end)
    /// map to absolute properties (top/bottom/left/right).
    /// </summary>
    public string WritingMode => GetComputedProperty("writing-mode", "lr-tb") ?? "lr-tb";

    /// <summary>
    /// Gets the computed value of a property, taking inheritance into account.
    /// </summary>
    public string? GetComputedProperty(string name, string? defaultValue = null)
    {
        // First check if property is explicitly set on this element
        var value = Properties[name];
        if (value != null)
        {
            // Handle explicit "inherit" keyword
            if (value == "inherit" && Parent != null)
                return Parent.GetComputedProperty(name, defaultValue);
            return value;
        }

        // If property is inheritable and has a parent, get from parent
        if (PropertyMetadata.IsInheritable(name) && Parent != null)
            return Parent.GetComputedProperty(name, defaultValue);

        // Use default value
        return defaultValue;
    }

    /// <summary>
    /// Gets a length property with writing-mode-aware directional property mapping.
    /// This method first checks for the relative directional property (e.g., padding-before),
    /// then falls back to the absolute property (e.g., padding-top), taking the current
    /// writing-mode into account.
    /// </summary>
    /// <param name="relativeProperty">The relative property name</param>
    /// <param name="absoluteProperty">The absolute property name</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <param name="genericProperty">Optional generic property to check (e.g., "padding")</param>
    protected double GetDirectionalLength(string relativeProperty, string absoluteProperty, double defaultValue = 0, string? genericProperty = null)
    {
        return WritingModeHelper.GetDirectionalLength(Properties, relativeProperty, absoluteProperty, WritingMode, defaultValue, genericProperty);
    }

    /// <summary>
    /// Gets a string property with writing-mode-aware directional property mapping.
    /// </summary>
    protected string GetDirectionalString(string relativeProperty, string absoluteProperty, string defaultValue)
    {
        return WritingModeHelper.GetDirectionalString(Properties, relativeProperty, absoluteProperty, WritingMode, defaultValue);
    }
}
