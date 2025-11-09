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
    private readonly List<Area> _areas = new();

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

    /// <summary>
    /// Gets the areas on this page.
    /// </summary>
    public IReadOnlyList<Area> Areas => _areas;

    /// <summary>
    /// Adds an area to the page.
    /// </summary>
    internal void AddArea(Area area)
    {
        ArgumentNullException.ThrowIfNull(area);
        _areas.Add(area);
    }
}

/// <summary>
/// Base class for all areas in the area tree.
/// </summary>
public abstract class Area
{
    /// <summary>
    /// Gets or sets the X position in points.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y position in points.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width in points.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height in points.
    /// </summary>
    public double Height { get; set; }
}

/// <summary>
/// Represents a block-level area (from fo:block).
/// </summary>
public sealed class BlockArea : Area
{
    private readonly List<Area> _children = new();

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public string FontFamily { get; set; } = "Helvetica";

    /// <summary>
    /// Gets or sets the font size in points.
    /// </summary>
    public double FontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the text alignment.
    /// </summary>
    public string TextAlign { get; set; } = "start";

    /// <summary>
    /// Gets or sets margins.
    /// </summary>
    public double MarginTop { get; set; }

    /// <summary>
    /// Gets or sets margin bottom.
    /// </summary>
    public double MarginBottom { get; set; }

    /// <summary>
    /// Gets or sets margin left.
    /// </summary>
    public double MarginLeft { get; set; }

    /// <summary>
    /// Gets or sets margin right.
    /// </summary>
    public double MarginRight { get; set; }

    /// <summary>
    /// Gets or sets padding.
    /// </summary>
    public double PaddingTop { get; set; }

    /// <summary>
    /// Gets or sets padding bottom.
    /// </summary>
    public double PaddingBottom { get; set; }

    /// <summary>
    /// Gets or sets padding left.
    /// </summary>
    public double PaddingLeft { get; set; }

    /// <summary>
    /// Gets or sets padding right.
    /// </summary>
    public double PaddingRight { get; set; }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string BackgroundColor { get; set; } = "transparent";

    /// <summary>
    /// Gets or sets the border width.
    /// </summary>
    public double BorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public string BorderColor { get; set; } = "black";

    /// <summary>
    /// Gets or sets the border style.
    /// </summary>
    public string BorderStyle { get; set; } = "none";

    /// <summary>
    /// Gets the child areas (typically line areas).
    /// </summary>
    public IReadOnlyList<Area> Children => _children;

    /// <summary>
    /// Adds a child area.
    /// </summary>
    internal void AddChild(Area area)
    {
        ArgumentNullException.ThrowIfNull(area);
        _children.Add(area);
    }
}

/// <summary>
/// Represents a line area containing inline content.
/// </summary>
public sealed class LineArea : Area
{
    private readonly List<InlineArea> _inlines = new();

    /// <summary>
    /// Gets the inline areas in this line.
    /// </summary>
    public IReadOnlyList<InlineArea> Inlines => _inlines;

    /// <summary>
    /// Adds an inline area to the line.
    /// </summary>
    internal void AddInline(InlineArea inline)
    {
        ArgumentNullException.ThrowIfNull(inline);
        _inlines.Add(inline);
    }
}

/// <summary>
/// Represents inline content (text, images, etc.).
/// </summary>
public sealed class InlineArea : Area
{
    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public string FontFamily { get; set; } = "Helvetica";

    /// <summary>
    /// Gets or sets the font size in points.
    /// </summary>
    public double FontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the baseline offset from the line's baseline.
    /// </summary>
    public double BaselineOffset { get; set; }
}
