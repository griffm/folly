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
}
