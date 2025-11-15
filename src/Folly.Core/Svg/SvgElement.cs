namespace Folly.Svg;

/// <summary>
/// Represents an SVG element (shapes, paths, groups, etc.).
/// This is the base class for all SVG elements in the document tree.
/// </summary>
public class SvgElement
{
    /// <summary>
    /// Gets the element type (rect, circle, path, g, etc.).
    /// </summary>
    public required string ElementType { get; init; }

    /// <summary>
    /// Gets the element ID (from the 'id' attribute).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets the element's attributes.
    /// </summary>
    public Dictionary<string, string> Attributes { get; init; } = new();

    /// <summary>
    /// Gets the element's child elements.
    /// </summary>
    public List<SvgElement> Children { get; init; } = new();

    /// <summary>
    /// Gets the parent element (null for root).
    /// </summary>
    public SvgElement? Parent { get; set; }

    /// <summary>
    /// Gets the computed style for this element (including inherited styles).
    /// </summary>
    public SvgStyle Style { get; set; } = new();

    /// <summary>
    /// Gets the transform applied to this element.
    /// </summary>
    public SvgTransform? Transform { get; set; }

    /// <summary>
    /// Gets or sets the text content (for text elements).
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// Gets an attribute value by name.
    /// </summary>
    public string? GetAttribute(string name)
    {
        return Attributes.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// Gets an attribute value as a double, with a default fallback.
    /// </summary>
    public double GetDoubleAttribute(string name, double defaultValue = 0)
    {
        var value = GetAttribute(name);
        if (value == null) return defaultValue;

        // Parse with unit support (px, pt, etc.)
        return SvgLengthParser.Parse(value, defaultValue);
    }

    /// <summary>
    /// Checks if this element should be rendered (not in defs, not display:none, etc.).
    /// </summary>
    public bool ShouldRender()
    {
        // Check display property
        if (Style.Display == "none") return false;

        // Check visibility
        if (Style.Visibility == "hidden" || Style.Visibility == "collapse") return false;

        // Check if parent is a defs element
        var current = Parent;
        while (current != null)
        {
            if (current.ElementType == "defs") return false;
            current = current.Parent;
        }

        return true;
    }
}
