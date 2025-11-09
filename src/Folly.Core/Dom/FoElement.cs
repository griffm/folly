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
}
