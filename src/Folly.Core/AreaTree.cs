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
    private readonly List<LinkArea> _links = new();
    private readonly List<AbsolutePositionedArea> _absoluteAreas = new();

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
    /// Gets the areas on this page (normal flow).
    /// </summary>
    public IReadOnlyList<Area> Areas => _areas;

    /// <summary>
    /// Gets the absolutely positioned areas on this page.
    /// These are rendered after normal flow content, sorted by z-index.
    /// </summary>
    public IReadOnlyList<AbsolutePositionedArea> AbsoluteAreas => _absoluteAreas;

    /// <summary>
    /// Gets the link areas on this page for PDF annotations.
    /// </summary>
    public IReadOnlyList<LinkArea> Links => _links;

    /// <summary>
    /// Adds an area to the page.
    /// </summary>
    internal void AddArea(Area area)
    {
        ArgumentNullException.ThrowIfNull(area);
        _areas.Add(area);
    }

    /// <summary>
    /// Removes an area from the page.
    /// Used for keep-with-next/previous constraints when blocks need to move together.
    /// </summary>
    internal void RemoveArea(Area area)
    {
        ArgumentNullException.ThrowIfNull(area);
        _areas.Remove(area);
    }

    /// <summary>
    /// Adds an absolutely positioned area to the page.
    /// Absolutely positioned areas are rendered after normal flow content.
    /// </summary>
    internal void AddAbsoluteArea(AbsolutePositionedArea area)
    {
        ArgumentNullException.ThrowIfNull(area);
        _absoluteAreas.Add(area);
    }

    /// <summary>
    /// Adds a link area to the page for PDF annotation generation.
    /// </summary>
    internal void AddLink(LinkArea link)
    {
        ArgumentNullException.ThrowIfNull(link);
        _links.Add(link);
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
    /// Gets or sets space before (spacing before this block).
    /// </summary>
    public double SpaceBefore { get; set; }

    /// <summary>
    /// Gets or sets space after (spacing after this block).
    /// </summary>
    public double SpaceAfter { get; set; }

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
    /// Gets or sets the background image source path.
    /// </summary>
    public string? BackgroundImage { get; set; }

    /// <summary>
    /// Gets or sets the background image data.
    /// </summary>
    public byte[]? BackgroundImageData { get; set; }

    /// <summary>
    /// Gets or sets the background image format (JPEG, PNG, etc.).
    /// </summary>
    public string? BackgroundImageFormat { get; set; }

    /// <summary>
    /// Gets or sets the background repeat mode (repeat, repeat-x, repeat-y, no-repeat).
    /// </summary>
    public string BackgroundRepeat { get; set; } = "repeat";

    /// <summary>
    /// Gets or sets the background horizontal position.
    /// </summary>
    public string BackgroundPositionHorizontal { get; set; } = "0%";

    /// <summary>
    /// Gets or sets the background vertical position.
    /// </summary>
    public string BackgroundPositionVertical { get; set; } = "0%";

    /// <summary>
    /// Gets or sets the intrinsic width of the background image (for sizing calculations).
    /// </summary>
    public double BackgroundImageWidth { get; set; }

    /// <summary>
    /// Gets or sets the intrinsic height of the background image (for sizing calculations).
    /// </summary>
    public double BackgroundImageHeight { get; set; }

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
    /// Gets or sets the top border width.
    /// </summary>
    public double BorderTopWidth { get; set; }

    /// <summary>
    /// Gets or sets the bottom border width.
    /// </summary>
    public double BorderBottomWidth { get; set; }

    /// <summary>
    /// Gets or sets the left border width.
    /// </summary>
    public double BorderLeftWidth { get; set; }

    /// <summary>
    /// Gets or sets the right border width.
    /// </summary>
    public double BorderRightWidth { get; set; }

    /// <summary>
    /// Gets or sets the top border style.
    /// </summary>
    public string BorderTopStyle { get; set; } = "none";

    /// <summary>
    /// Gets or sets the bottom border style.
    /// </summary>
    public string BorderBottomStyle { get; set; } = "none";

    /// <summary>
    /// Gets or sets the left border style.
    /// </summary>
    public string BorderLeftStyle { get; set; } = "none";

    /// <summary>
    /// Gets or sets the right border style.
    /// </summary>
    public string BorderRightStyle { get; set; } = "none";

    /// <summary>
    /// Gets or sets the top border color.
    /// </summary>
    public string BorderTopColor { get; set; } = "black";

    /// <summary>
    /// Gets or sets the bottom border color.
    /// </summary>
    public string BorderBottomColor { get; set; } = "black";

    /// <summary>
    /// Gets or sets the left border color.
    /// </summary>
    public string BorderLeftColor { get; set; } = "black";

    /// <summary>
    /// Gets or sets the right border color.
    /// </summary>
    public string BorderRightColor { get; set; } = "black";

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
    /// Gets or sets the font weight (normal, bold, 100-900).
    /// </summary>
    public string? FontWeight { get; set; }

    /// <summary>
    /// Gets or sets the font style (normal, italic, oblique).
    /// </summary>
    public string? FontStyle { get; set; }

    /// <summary>
    /// Gets or sets the text color in CSS format.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the text decoration (none, underline, overline, line-through).
    /// </summary>
    public string? TextDecoration { get; set; }

    /// <summary>
    /// Gets or sets the background color in CSS format.
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the baseline offset from the line's baseline.
    /// </summary>
    public double BaselineOffset { get; set; }

    /// <summary>
    /// Gets or sets the text direction (ltr, rtl).
    /// Used for bidirectional text rendering.
    /// </summary>
    public string Direction { get; set; } = "ltr";

    /// <summary>
    /// Gets or sets the word spacing adjustment in points.
    /// Used for text justification - distributes extra space between words.
    /// </summary>
    public double WordSpacing { get; set; }
}

/// <summary>
/// Represents an image area (from fo:external-graphic).
/// </summary>
public sealed class ImageArea : Area
{
    /// <summary>
    /// Gets or sets the image source path.
    /// </summary>
    public string Source { get; set; } = "";

    /// <summary>
    /// Gets or sets the image format (JPEG, PNG, etc.).
    /// </summary>
    public string Format { get; set; } = "";

    /// <summary>
    /// Gets or sets the raw image data.
    /// </summary>
    public byte[]? ImageData { get; set; }

    /// <summary>
    /// Gets or sets the intrinsic width of the image.
    /// </summary>
    public double IntrinsicWidth { get; set; }

    /// <summary>
    /// Gets or sets the intrinsic height of the image.
    /// </summary>
    public double IntrinsicHeight { get; set; }

    /// <summary>
    /// Gets or sets the scaling method.
    /// </summary>
    public string Scaling { get; set; } = "uniform";
}

/// <summary>
/// Represents a table area (from fo:table).
/// </summary>
public sealed class TableArea : Area
{
    private readonly List<TableRowArea> _rows = new();

    /// <summary>
    /// Gets or sets the border collapse model.
    /// </summary>
    public string BorderCollapse { get; set; } = "separate";

    /// <summary>
    /// Gets or sets the border spacing.
    /// </summary>
    public double BorderSpacing { get; set; }

    /// <summary>
    /// Gets or sets the column widths.
    /// </summary>
    public List<double> ColumnWidths { get; set; } = new();

    /// <summary>
    /// Gets the table rows.
    /// </summary>
    public IReadOnlyList<TableRowArea> Rows => _rows;

    /// <summary>
    /// Adds a row to the table.
    /// </summary>
    internal void AddRow(TableRowArea row)
    {
        ArgumentNullException.ThrowIfNull(row);
        _rows.Add(row);
    }
}

/// <summary>
/// Represents a table row area.
/// </summary>
public sealed class TableRowArea : Area
{
    private readonly List<TableCellArea> _cells = new();

    /// <summary>
    /// Gets the cells in this row.
    /// </summary>
    public IReadOnlyList<TableCellArea> Cells => _cells;

    /// <summary>
    /// Adds a cell to the row.
    /// </summary>
    internal void AddCell(TableCellArea cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        _cells.Add(cell);
    }
}

/// <summary>
/// Represents a table cell area.
/// </summary>
public sealed class TableCellArea : Area
{
    private readonly List<Area> _children = new();

    /// <summary>
    /// Gets or sets the number of columns this cell spans.
    /// </summary>
    public int NumberColumnsSpanned { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of rows this cell spans.
    /// </summary>
    public int NumberRowsSpanned { get; set; } = 1;

    /// <summary>
    /// Gets or sets the column index (0-based).
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Gets or sets padding top.
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
    /// Gets or sets border width.
    /// </summary>
    public double BorderWidth { get; set; }

    /// <summary>
    /// Gets or sets border style.
    /// </summary>
    public string BorderStyle { get; set; } = "none";

    /// <summary>
    /// Gets or sets border color.
    /// </summary>
    public string BorderColor { get; set; } = "black";

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string BackgroundColor { get; set; } = "transparent";

    /// <summary>
    /// Gets or sets the text alignment.
    /// </summary>
    public string TextAlign { get; set; } = "start";

    /// <summary>
    /// Gets or sets the vertical alignment.
    /// </summary>
    public string VerticalAlign { get; set; } = "top";

    /// <summary>
    /// Gets the child areas (blocks within the cell).
    /// </summary>
    public IReadOnlyList<Area> Children => _children;

    /// <summary>
    /// Adds a child area to the cell.
    /// </summary>
    internal void AddChild(Area area)
    {
        ArgumentNullException.ThrowIfNull(area);
        _children.Add(area);
    }
}

/// <summary>
/// Represents a float area (from fo:float).
/// Floats are positioned to the side with content potentially flowing around them.
/// </summary>
public sealed class FloatArea : Area
{
    private readonly List<BlockArea> _blocks = new();

    /// <summary>
    /// Gets or sets the float position ("start" for left, "end" for right).
    /// </summary>
    public string Float { get; set; } = "start";

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
    /// Gets the blocks that make up the float content.
    /// </summary>
    public IReadOnlyList<BlockArea> Blocks => _blocks;

    /// <summary>
    /// Adds a block to the float.
    /// </summary>
    internal void AddBlock(BlockArea block)
    {
        ArgumentNullException.ThrowIfNull(block);
        _blocks.Add(block);
    }
}

/// <summary>
/// Represents a leader area (from fo:leader).
/// Leaders generate repeating patterns (dots, rules, spaces) to fill space,
/// commonly used in tables of contents to connect entries with page numbers.
/// </summary>
public sealed class LeaderArea : Area
{
    /// <summary>
    /// Gets or sets the leader pattern type (space, dots, rule, use-content).
    /// </summary>
    public string LeaderPattern { get; set; } = "space";

    /// <summary>
    /// Gets or sets the pattern width for repeating patterns.
    /// </summary>
    public double LeaderPatternWidth { get; set; } = 5.0;

    /// <summary>
    /// Gets or sets the rule thickness (for leader-pattern="rule").
    /// </summary>
    public double RuleThickness { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the rule style (solid, dotted, dashed, etc.).
    /// </summary>
    public string RuleStyle { get; set; } = "solid";

    /// <summary>
    /// Gets or sets the color for the leader pattern.
    /// </summary>
    public string Color { get; set; } = "black";

    /// <summary>
    /// Gets or sets the font family (for dot patterns).
    /// </summary>
    public string FontFamily { get; set; } = "Helvetica";

    /// <summary>
    /// Gets or sets the font size (for dot patterns).
    /// </summary>
    public double FontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the baseline offset for inline positioning.
    /// </summary>
    public double BaselineOffset { get; set; }
}

/// <summary>
/// Represents a link area (from fo:basic-link).
/// Links create clickable regions in the PDF that navigate to internal or external destinations.
/// </summary>
public sealed class LinkArea : Area
{
    /// <summary>
    /// Gets or sets the internal destination ID.
    /// Used for links within the document (e.g., table of contents, cross-references).
    /// Null if this is an external link.
    /// </summary>
    public string? InternalDestination { get; set; }

    /// <summary>
    /// Gets or sets the external destination URI.
    /// Used for external links (http://, mailto:, file://, etc.).
    /// Null if this is an internal link.
    /// </summary>
    public string? ExternalDestination { get; set; }

    /// <summary>
    /// Gets or sets the show-destination property.
    /// "replace" opens in same window, "new" opens in new window.
    /// </summary>
    public string ShowDestination { get; set; } = "replace";

    /// <summary>
    /// Gets or sets the text content of the link.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the font family for the link text.
    /// </summary>
    public string FontFamily { get; set; } = "Helvetica";

    /// <summary>
    /// Gets or sets the font size in points.
    /// </summary>
    public double FontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the text color (typically blue for links).
    /// </summary>
    public string Color { get; set; } = "blue";

    /// <summary>
    /// Gets or sets the text decoration (typically underline for links).
    /// </summary>
    public string TextDecoration { get; set; } = "underline";

    /// <summary>
    /// Gets or sets the baseline offset for inline positioning.
    /// </summary>
    public double BaselineOffset { get; set; }
}

/// <summary>
/// Represents an absolutely positioned area (from fo:block-container with absolute-position="absolute").
/// Absolute positioned areas are positioned relative to the page, not the flow.
/// They are rendered after normal flow content to appear on top.
/// </summary>
public sealed class AbsolutePositionedArea : Area
{
    private readonly List<Area> _children = new();

    /// <summary>
    /// Gets or sets the absolute position type ("absolute", "fixed").
    /// "absolute" positions relative to the nearest positioned ancestor or page.
    /// "fixed" positions relative to the page viewport.
    /// </summary>
    public string Position { get; set; } = "absolute";

    /// <summary>
    /// Gets or sets the z-index for stacking order.
    /// Higher values are rendered on top of lower values.
    /// Default is 0.
    /// </summary>
    public int ZIndex { get; set; } = 0;

    /// <summary>
    /// Gets or sets the reference orientation in degrees (0, 90, 180, 270, -90, -180, -270).
    /// Specifies the rotation of the block container.
    /// Default is 0.
    /// </summary>
    public int ReferenceOrientation { get; set; } = 0;

    /// <summary>
    /// Gets or sets the display alignment (auto, before, center, after).
    /// Controls vertical alignment of content within the block container.
    /// Default is "auto" (align to top).
    /// </summary>
    public string DisplayAlign { get; set; } = "auto";

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string BackgroundColor { get; set; } = "transparent";

    /// <summary>
    /// Gets or sets the background image source path.
    /// </summary>
    public string? BackgroundImage { get; set; }

    /// <summary>
    /// Gets or sets the background image data.
    /// </summary>
    public byte[]? BackgroundImageData { get; set; }

    /// <summary>
    /// Gets or sets the background image format (JPEG, PNG, etc.).
    /// </summary>
    public string? BackgroundImageFormat { get; set; }

    /// <summary>
    /// Gets or sets the background repeat mode (repeat, repeat-x, repeat-y, no-repeat).
    /// </summary>
    public string BackgroundRepeat { get; set; } = "repeat";

    /// <summary>
    /// Gets or sets the background horizontal position.
    /// </summary>
    public string BackgroundPositionHorizontal { get; set; } = "0%";

    /// <summary>
    /// Gets or sets the background vertical position.
    /// </summary>
    public string BackgroundPositionVertical { get; set; } = "0%";

    /// <summary>
    /// Gets or sets the intrinsic width of the background image (for sizing calculations).
    /// </summary>
    public double BackgroundImageWidth { get; set; }

    /// <summary>
    /// Gets or sets the intrinsic height of the background image (for sizing calculations).
    /// </summary>
    public double BackgroundImageHeight { get; set; }

    /// <summary>
    /// Gets or sets padding top.
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
    /// Gets or sets border width.
    /// </summary>
    public double BorderWidth { get; set; }

    /// <summary>
    /// Gets or sets border color.
    /// </summary>
    public string BorderColor { get; set; } = "black";

    /// <summary>
    /// Gets or sets border style.
    /// </summary>
    public string BorderStyle { get; set; } = "none";

    /// <summary>
    /// Gets or sets top border width.
    /// </summary>
    public double BorderTopWidth { get; set; }

    /// <summary>
    /// Gets or sets bottom border width.
    /// </summary>
    public double BorderBottomWidth { get; set; }

    /// <summary>
    /// Gets or sets left border width.
    /// </summary>
    public double BorderLeftWidth { get; set; }

    /// <summary>
    /// Gets or sets right border width.
    /// </summary>
    public double BorderRightWidth { get; set; }

    /// <summary>
    /// Gets or sets top border style.
    /// </summary>
    public string BorderTopStyle { get; set; } = "none";

    /// <summary>
    /// Gets or sets bottom border style.
    /// </summary>
    public string BorderBottomStyle { get; set; } = "none";

    /// <summary>
    /// Gets or sets left border style.
    /// </summary>
    public string BorderLeftStyle { get; set; } = "none";

    /// <summary>
    /// Gets or sets right border style.
    /// </summary>
    public string BorderRightStyle { get; set; } = "none";

    /// <summary>
    /// Gets or sets top border color.
    /// </summary>
    public string BorderTopColor { get; set; } = "black";

    /// <summary>
    /// Gets or sets bottom border color.
    /// </summary>
    public string BorderBottomColor { get; set; } = "black";

    /// <summary>
    /// Gets or sets left border color.
    /// </summary>
    public string BorderLeftColor { get; set; } = "black";

    /// <summary>
    /// Gets or sets right border color.
    /// </summary>
    public string BorderRightColor { get; set; } = "black";

    /// <summary>
    /// Gets the child areas (blocks within the absolutely positioned container).
    /// </summary>
    public IReadOnlyList<Area> Children => _children;

    /// <summary>
    /// Adds a child area to the absolutely positioned container.
    /// </summary>
    internal void AddChild(Area area)
    {
        ArgumentNullException.ThrowIfNull(area);
        _children.Add(area);
    }
}
