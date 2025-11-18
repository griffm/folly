namespace Folly.Layout.Testing;

/// <summary>
/// Provides a fluent API for querying and asserting on area trees.
/// This enables precise, readable tests for layout verification.
/// </summary>
public class AreaTreeQuery
{
    private readonly AreaTree _areaTree;

    internal AreaTreeQuery(AreaTree areaTree)
    {
        _areaTree = areaTree ?? throw new ArgumentNullException(nameof(areaTree));
    }

    /// <summary>
    /// Gets the number of pages in the area tree.
    /// </summary>
    public int PageCount => _areaTree.Pages.Count;

    /// <summary>
    /// Selects a specific page by index (0-based).
    /// </summary>
    public PageQuery Page(int index)
    {
        if (index < 0 || index >= _areaTree.Pages.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Page index {index} is out of range. Tree has {_areaTree.Pages.Count} pages.");

        return new PageQuery(_areaTree.Pages[index]);
    }

    /// <summary>
    /// Selects the first page.
    /// </summary>
    public PageQuery FirstPage() => Page(0);

    /// <summary>
    /// Selects the last page.
    /// </summary>
    public PageQuery LastPage() => Page(_areaTree.Pages.Count - 1);

    /// <summary>
    /// Gets all pages.
    /// </summary>
    public IEnumerable<PageQuery> AllPages()
    {
        return _areaTree.Pages.Select(p => new PageQuery(p));
    }

    /// <summary>
    /// Extracts all text content from the area tree.
    /// </summary>
    public string ExtractText()
    {
        var sb = new System.Text.StringBuilder();

        foreach (var page in _areaTree.Pages)
        {
            ExtractTextFromAreas(page.Areas, sb);
            ExtractTextFromAbsoluteAreas(page.AbsoluteAreas, sb);
        }

        return sb.ToString();
    }

    private static void ExtractTextFromAreas(IReadOnlyList<Area> areas, System.Text.StringBuilder sb)
    {
        foreach (var area in areas)
        {
            if (area is BlockArea block)
            {
                ExtractTextFromBlock(block, sb);
            }
            else if (area is TableArea table)
            {
                foreach (var row in table.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        ExtractTextFromAreas(cell.Children, sb);
                    }
                }
            }
        }
    }

    private static void ExtractTextFromBlock(BlockArea block, System.Text.StringBuilder sb)
    {
        foreach (var child in block.Children)
        {
            if (child is LineArea line)
            {
                foreach (var inline in line.Inlines)
                {
                    if (!string.IsNullOrEmpty(inline.Text))
                    {
                        sb.Append(inline.Text);
                    }
                }
            }
        }
    }

    private static void ExtractTextFromAbsoluteAreas(IReadOnlyList<AbsolutePositionedArea> areas, System.Text.StringBuilder sb)
    {
        foreach (var area in areas)
        {
            ExtractTextFromAreas(area.Children, sb);
        }
    }
}

/// <summary>
/// Query interface for a specific page.
/// </summary>
public class PageQuery
{
    private readonly PageViewport _page;

    internal PageQuery(PageViewport page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
    }

    /// <summary>
    /// Gets the page width in points.
    /// </summary>
    public double Width => _page.Width;

    /// <summary>
    /// Gets the page height in points.
    /// </summary>
    public double Height => _page.Height;

    /// <summary>
    /// Gets the page number.
    /// </summary>
    public int PageNumber => _page.PageNumber;

    /// <summary>
    /// Gets the number of normal flow areas.
    /// </summary>
    public int AreaCount => _page.Areas.Count;

    /// <summary>
    /// Gets the number of absolutely positioned areas.
    /// </summary>
    public int AbsoluteAreaCount => _page.AbsoluteAreas.Count;

    /// <summary>
    /// Selects all block areas on the page.
    /// </summary>
    public IEnumerable<BlockQuery> Blocks()
    {
        return _page.Areas
            .OfType<BlockArea>()
            .Select(b => new BlockQuery(b));
    }

    /// <summary>
    /// Selects a specific block area by index.
    /// </summary>
    public BlockQuery Block(int index)
    {
        var blocks = _page.Areas.OfType<BlockArea>().ToList();
        if (index < 0 || index >= blocks.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Block index {index} is out of range. Page has {blocks.Count} blocks.");

        return new BlockQuery(blocks[index]);
    }

    /// <summary>
    /// Selects all table areas on the page.
    /// </summary>
    public IEnumerable<TableQuery> Tables()
    {
        return _page.Areas
            .OfType<TableArea>()
            .Select(t => new TableQuery(t));
    }

    /// <summary>
    /// Selects the first table area on the page.
    /// </summary>
    public TableQuery FirstTable()
    {
        var table = _page.Areas.OfType<TableArea>().FirstOrDefault();
        if (table == null)
            throw new InvalidOperationException("No tables found on page.");

        return new TableQuery(table);
    }

    /// <summary>
    /// Extracts all text from the page.
    /// </summary>
    public string ExtractText()
    {
        var sb = new System.Text.StringBuilder();
        ExtractTextFromAreas(_page.Areas, sb);
        return sb.ToString();
    }

    private static void ExtractTextFromAreas(IReadOnlyList<Area> areas, System.Text.StringBuilder sb)
    {
        foreach (var area in areas)
        {
            if (area is BlockArea block)
            {
                foreach (var child in block.Children)
                {
                    if (child is LineArea line)
                    {
                        foreach (var inline in line.Inlines)
                        {
                            sb.Append(inline.Text ?? "");
                        }
                    }
                }
            }
        }
    }
}

/// <summary>
/// Query interface for a block area.
/// </summary>
public class BlockQuery
{
    private readonly BlockArea _block;

    internal BlockQuery(BlockArea block)
    {
        _block = block ?? throw new ArgumentNullException(nameof(block));
    }

    /// <summary>
    /// Gets the X position.
    /// </summary>
    public double X => _block.X;

    /// <summary>
    /// Gets the Y position.
    /// </summary>
    public double Y => _block.Y;

    /// <summary>
    /// Gets the width.
    /// </summary>
    public double Width => _block.Width;

    /// <summary>
    /// Gets the height.
    /// </summary>
    public double Height => _block.Height;

    /// <summary>
    /// Gets the font family.
    /// </summary>
    public string? FontFamily => _block.FontFamily;

    /// <summary>
    /// Gets the font size.
    /// </summary>
    public double? FontSize => _block.FontSize;

    /// <summary>
    /// Gets the text alignment.
    /// </summary>
    public string? TextAlign => _block.TextAlign;

    /// <summary>
    /// Gets all line areas in the block.
    /// </summary>
    public IEnumerable<LineQuery> Lines()
    {
        return _block.Children
            .OfType<LineArea>()
            .Select(l => new LineQuery(l));
    }

    /// <summary>
    /// Gets a specific line by index.
    /// </summary>
    public LineQuery Line(int index)
    {
        var lines = _block.Children.OfType<LineArea>().ToList();
        if (index < 0 || index >= lines.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Line index {index} is out of range. Block has {lines.Count} lines.");

        return new LineQuery(lines[index]);
    }

    /// <summary>
    /// Gets the number of lines in the block.
    /// </summary>
    public int LineCount => _block.Children.OfType<LineArea>().Count();

    /// <summary>
    /// Extracts all text from the block.
    /// </summary>
    public string ExtractText()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var child in _block.Children)
        {
            if (child is LineArea line)
            {
                foreach (var inline in line.Inlines)
                {
                    sb.Append(inline.Text ?? "");
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets the margin values.
    /// </summary>
    public Spacing Margin => new Spacing(
        _block.MarginTop,
        _block.MarginRight,
        _block.MarginBottom,
        _block.MarginLeft
    );

    /// <summary>
    /// Gets the padding values.
    /// </summary>
    public Spacing Padding => new Spacing(
        _block.PaddingTop,
        _block.PaddingRight,
        _block.PaddingBottom,
        _block.PaddingLeft
    );
}

/// <summary>
/// Query interface for a line area.
/// </summary>
public class LineQuery
{
    private readonly LineArea _line;

    internal LineQuery(LineArea line)
    {
        _line = line ?? throw new ArgumentNullException(nameof(line));
    }

    /// <summary>
    /// Gets the X position.
    /// </summary>
    public double X => _line.X;

    /// <summary>
    /// Gets the Y position.
    /// </summary>
    public double Y => _line.Y;

    /// <summary>
    /// Gets the width.
    /// </summary>
    public double Width => _line.Width;

    /// <summary>
    /// Gets the height.
    /// </summary>
    public double Height => _line.Height;

    /// <summary>
    /// Gets all inline areas.
    /// </summary>
    public IEnumerable<InlineQuery> Inlines()
    {
        return _line.Inlines.Select(i => new InlineQuery(i));
    }

    /// <summary>
    /// Gets a specific inline by index.
    /// </summary>
    public InlineQuery Inline(int index)
    {
        if (index < 0 || index >= _line.Inlines.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Inline index {index} is out of range. Line has {_line.Inlines.Count} inlines.");

        return new InlineQuery(_line.Inlines[index]);
    }

    /// <summary>
    /// Gets the number of inlines.
    /// </summary>
    public int InlineCount => _line.Inlines.Count;

    /// <summary>
    /// Extracts text from the line.
    /// </summary>
    public string ExtractText()
    {
        return string.Join("", _line.Inlines.Select(i => i.Text ?? ""));
    }

    /// <summary>
    /// Checks if any inline has word spacing applied (for justified text).
    /// </summary>
    public bool HasWordSpacing => _line.Inlines.Any(i => i.WordSpacing != 0);

    /// <summary>
    /// Gets the maximum word spacing value in the line.
    /// </summary>
    public double MaxWordSpacing => _line.Inlines
        .Where(i => i.WordSpacing != 0)
        .Select(i => i.WordSpacing)
        .DefaultIfEmpty(0)
        .Max();
}

/// <summary>
/// Query interface for an inline area.
/// </summary>
public class InlineQuery
{
    private readonly InlineArea _inline;

    internal InlineQuery(InlineArea inline)
    {
        _inline = inline ?? throw new ArgumentNullException(nameof(inline));
    }

    /// <summary>
    /// Gets the text content.
    /// </summary>
    public string? Text => _inline.Text;

    /// <summary>
    /// Gets the X position.
    /// </summary>
    public double X => _inline.X;

    /// <summary>
    /// Gets the Y position.
    /// </summary>
    public double Y => _inline.Y;

    /// <summary>
    /// Gets the width.
    /// </summary>
    public double Width => _inline.Width;

    /// <summary>
    /// Gets the height.
    /// </summary>
    public double Height => _inline.Height;

    /// <summary>
    /// Gets the font family.
    /// </summary>
    public string? FontFamily => _inline.FontFamily;

    /// <summary>
    /// Gets the font size.
    /// </summary>
    public double? FontSize => _inline.FontSize;

    /// <summary>
    /// Gets the font weight.
    /// </summary>
    public string? FontWeight => _inline.FontWeight;

    /// <summary>
    /// Gets the font style.
    /// </summary>
    public string? FontStyle => _inline.FontStyle;

    /// <summary>
    /// Gets the word spacing adjustment (for justified text).
    /// </summary>
    public double? WordSpacing => _inline.WordSpacing;

    /// <summary>
    /// Gets the baseline offset.
    /// </summary>
    public double? BaselineOffset => _inline.BaselineOffset;

    /// <summary>
    /// Gets the color.
    /// </summary>
    public string? Color => _inline.Color;
}

/// <summary>
/// Query interface for a table area.
/// </summary>
public class TableQuery
{
    private readonly TableArea _table;

    internal TableQuery(TableArea table)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
    }

    /// <summary>
    /// Gets the X position.
    /// </summary>
    public double X => _table.X;

    /// <summary>
    /// Gets the Y position.
    /// </summary>
    public double Y => _table.Y;

    /// <summary>
    /// Gets the width.
    /// </summary>
    public double Width => _table.Width;

    /// <summary>
    /// Gets the height.
    /// </summary>
    public double Height => _table.Height;

    /// <summary>
    /// Gets the column widths.
    /// </summary>
    public IReadOnlyList<double> ColumnWidths => _table.ColumnWidths;

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int ColumnCount => _table.ColumnWidths.Count;

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    public int RowCount => _table.Rows.Count;

    /// <summary>
    /// Gets all rows.
    /// </summary>
    public IEnumerable<TableRowQuery> Rows()
    {
        return _table.Rows.Select(r => new TableRowQuery(r));
    }

    /// <summary>
    /// Gets a specific row by index.
    /// </summary>
    public TableRowQuery Row(int index)
    {
        if (index < 0 || index >= _table.Rows.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Row index {index} is out of range. Table has {_table.Rows.Count} rows.");

        return new TableRowQuery(_table.Rows[index]);
    }

    /// <summary>
    /// Gets the border collapse mode.
    /// </summary>
    public string? BorderCollapse => _table.BorderCollapse;

    /// <summary>
    /// Verifies that column widths sum to approximately the table width.
    /// </summary>
    public bool ColumnWidthsSumToTableWidth(double tolerance = 1.0)
    {
        var sum = _table.ColumnWidths.Sum();
        return Math.Abs(sum - _table.Width) <= tolerance;
    }

    /// <summary>
    /// Gets the column width at the specified index.
    /// </summary>
    public double ColumnWidth(int index)
    {
        if (index < 0 || index >= _table.ColumnWidths.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Column index {index} is out of range. Table has {_table.ColumnWidths.Count} columns.");

        return _table.ColumnWidths[index];
    }
}

/// <summary>
/// Query interface for a table row.
/// </summary>
public class TableRowQuery
{
    private readonly TableRowArea _row;

    internal TableRowQuery(TableRowArea row)
    {
        _row = row ?? throw new ArgumentNullException(nameof(row));
    }

    /// <summary>
    /// Gets the number of cells.
    /// </summary>
    public int CellCount => _row.Cells.Count;

    /// <summary>
    /// Gets all cells.
    /// </summary>
    public IEnumerable<TableCellQuery> Cells()
    {
        return _row.Cells.Select(c => new TableCellQuery(c));
    }

    /// <summary>
    /// Gets a specific cell by index.
    /// </summary>
    public TableCellQuery Cell(int index)
    {
        if (index < 0 || index >= _row.Cells.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Cell index {index} is out of range. Row has {_row.Cells.Count} cells.");

        return new TableCellQuery(_row.Cells[index]);
    }
}

/// <summary>
/// Query interface for a table cell.
/// </summary>
public class TableCellQuery
{
    private readonly TableCellArea _cell;

    internal TableCellQuery(TableCellArea cell)
    {
        _cell = cell ?? throw new ArgumentNullException(nameof(cell));
    }

    /// <summary>
    /// Gets the X position.
    /// </summary>
    public double X => _cell.X;

    /// <summary>
    /// Gets the Y position.
    /// </summary>
    public double Y => _cell.Y;

    /// <summary>
    /// Gets the width.
    /// </summary>
    public double Width => _cell.Width;

    /// <summary>
    /// Gets the height.
    /// </summary>
    public double Height => _cell.Height;

    /// <summary>
    /// Gets the column span.
    /// </summary>
    public int ColumnSpan => _cell.NumberColumnsSpanned;

    /// <summary>
    /// Gets the row span.
    /// </summary>
    public int RowSpan => _cell.NumberRowsSpanned;

    /// <summary>
    /// Gets the column index.
    /// </summary>
    public int ColumnIndex => _cell.ColumnIndex;

    /// <summary>
    /// Extracts text from the cell.
    /// </summary>
    public string ExtractText()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var child in _cell.Children)
        {
            if (child is BlockArea block)
            {
                foreach (var blockChild in block.Children)
                {
                    if (blockChild is LineArea line)
                    {
                        foreach (var inline in line.Inlines)
                        {
                            sb.Append(inline.Text ?? "");
                        }
                    }
                }
            }
        }
        return sb.ToString();
    }
}

/// <summary>
/// Represents spacing values (margin or padding).
/// </summary>
public readonly struct Spacing
{
    /// <summary>
    /// Gets the top spacing value.
    /// </summary>
    public double Top { get; }

    /// <summary>
    /// Gets the right spacing value.
    /// </summary>
    public double Right { get; }

    /// <summary>
    /// Gets the bottom spacing value.
    /// </summary>
    public double Bottom { get; }

    /// <summary>
    /// Gets the left spacing value.
    /// </summary>
    public double Left { get; }

    /// <summary>
    /// Initializes a new instance of the Spacing struct.
    /// </summary>
    /// <param name="top">The top spacing value.</param>
    /// <param name="right">The right spacing value.</param>
    /// <param name="bottom">The bottom spacing value.</param>
    /// <param name="left">The left spacing value.</param>
    public Spacing(double top, double right, double bottom, double left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    /// <summary>
    /// Gets the combined horizontal spacing (left + right).
    /// </summary>
    public double Horizontal => Left + Right;

    /// <summary>
    /// Gets the combined vertical spacing (top + bottom).
    /// </summary>
    public double Vertical => Top + Bottom;

    /// <summary>
    /// Returns a string representation of the spacing values.
    /// </summary>
    /// <returns>A formatted string showing top, right, bottom, and left values.</returns>
    public override string ToString() => $"T:{Top} R:{Right} B:{Bottom} L:{Left}";
}
