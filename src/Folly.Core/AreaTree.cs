namespace Folly;

/// <summary>
/// Represents the area tree generated from an XSL-FO document.
/// This is the intermediate representation between FO parsing and PDF rendering.
/// </summary>
public sealed class AreaTree
{
    private readonly List<PageViewport> _pages = new();

    /// <summary>
    /// Gets the collection of page viewports in the area tree.
    /// </summary>
    public IReadOnlyList<PageViewport> Pages => _pages;

    /// <summary>
    /// Adds a page viewport to the area tree.
    /// </summary>
    internal void AddPage(PageViewport page)
    {
        ArgumentNullException.ThrowIfNull(page);
        _pages.Add(page);
    }
}

/// <summary>
/// Represents a single page in the area tree.
/// </summary>
public sealed class PageViewport
{
    /// <summary>
    /// Gets or sets the page width in points.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the page height in points.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; }

    // TODO: Add regions (body, before, after, start, end)
    // TODO: Add areas (block areas, line areas, inline areas)
}
