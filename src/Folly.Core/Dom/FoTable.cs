namespace Folly.Dom;

/// <summary>
/// Represents an fo:table element.
/// </summary>
public sealed class FoTable : FoElement
{
    /// <inheritdoc/>
    public override string Name => "table";

    /// <summary>
    /// Gets the table column specifications.
    /// </summary>
    public IReadOnlyList<FoTableColumn> Columns { get; init; } = Array.Empty<FoTableColumn>();

    /// <summary>
    /// Gets the table header (optional).
    /// </summary>
    public FoTableHeader? Header { get; init; }

    /// <summary>
    /// Gets the table footer (optional).
    /// </summary>
    public FoTableFooter? Footer { get; init; }

    /// <summary>
    /// Gets the table body.
    /// </summary>
    public FoTableBody? Body { get; init; }

    /// <summary>
    /// Gets the table width.
    /// </summary>
    public double Width => LengthParser.Parse(Properties.GetString("width", "100%"));

    /// <summary>
    /// Gets the border collapse model.
    /// </summary>
    public string BorderCollapse => Properties.GetString("border-collapse", "separate");

    /// <summary>
    /// Gets the border spacing.
    /// </summary>
    public double BorderSpacing => LengthParser.Parse(Properties.GetString("border-spacing", "0pt"));

    /// <summary>
    /// Gets the table layout algorithm.
    /// </summary>
    public string TableLayout => Properties.GetString("table-layout", "auto");
}

/// <summary>
/// Represents an fo:table-column element.
/// </summary>
public sealed class FoTableColumn : FoElement
{
    /// <inheritdoc/>
    public override string Name => "table-column";

    /// <summary>
    /// Gets the column width.
    /// </summary>
    public double ColumnWidth => LengthParser.Parse(Properties.GetString("column-width", "auto"));

    /// <summary>
    /// Gets the column number.
    /// </summary>
    public int ColumnNumber => int.TryParse(Properties.GetString("column-number", "0"), out var n) ? n : 0;

    /// <summary>
    /// Gets the number of columns this specification represents.
    /// </summary>
    public int NumberColumnsRepeated => int.TryParse(Properties.GetString("number-columns-repeated", "1"), out var n) ? n : 1;
}

/// <summary>
/// Represents an fo:table-header element.
/// </summary>
public sealed class FoTableHeader : FoElement
{
    /// <inheritdoc/>
    public override string Name => "table-header";

    /// <summary>
    /// Gets the header rows.
    /// </summary>
    public IReadOnlyList<FoTableRow> Rows { get; init; } = Array.Empty<FoTableRow>();
}

/// <summary>
/// Represents an fo:table-footer element.
/// </summary>
public sealed class FoTableFooter : FoElement
{
    /// <inheritdoc/>
    public override string Name => "table-footer";

    /// <summary>
    /// Gets the footer rows.
    /// </summary>
    public IReadOnlyList<FoTableRow> Rows { get; init; } = Array.Empty<FoTableRow>();
}

/// <summary>
/// Represents an fo:table-body element.
/// </summary>
public sealed class FoTableBody : FoElement
{
    /// <inheritdoc/>
    public override string Name => "table-body";

    /// <summary>
    /// Gets the body rows.
    /// </summary>
    public IReadOnlyList<FoTableRow> Rows { get; init; } = Array.Empty<FoTableRow>();
}

/// <summary>
/// Represents an fo:table-row element.
/// </summary>
public sealed class FoTableRow : FoElement
{
    /// <inheritdoc/>
    public override string Name => "table-row";

    /// <summary>
    /// Gets the cells in this row.
    /// </summary>
    public IReadOnlyList<FoTableCell> Cells { get; init; } = Array.Empty<FoTableCell>();

    /// <summary>
    /// Gets the row height.
    /// </summary>
    public double Height => LengthParser.Parse(Properties.GetString("height", "auto"));
}

/// <summary>
/// Represents an fo:table-cell element.
/// </summary>
public sealed class FoTableCell : FoElement
{
    /// <inheritdoc/>
    public override string Name => "table-cell";

    /// <summary>
    /// Gets the cell's block content.
    /// </summary>
    public IReadOnlyList<FoBlock> Blocks { get; init; } = Array.Empty<FoBlock>();

    /// <summary>
    /// Gets the number of columns this cell spans.
    /// </summary>
    public int NumberColumnsSpanned => int.TryParse(Properties.GetString("number-columns-spanned", "1"), out var n) ? n : 1;

    /// <summary>
    /// Gets the number of rows this cell spans.
    /// </summary>
    public int NumberRowsSpanned => int.TryParse(Properties.GetString("number-rows-spanned", "1"), out var n) ? n : 1;

    /// <summary>
    /// Gets the column number (1-based).
    /// </summary>
    public int ColumnNumber => int.TryParse(Properties.GetString("column-number", "0"), out var n) ? n : 0;

    /// <summary>
    /// Gets the padding top.
    /// Maps from padding-before in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingTop => GetDirectionalLength("padding-before", "padding-top", 0, "padding");

    /// <summary>
    /// Gets the padding bottom.
    /// Maps from padding-after in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingBottom => GetDirectionalLength("padding-after", "padding-bottom", 0, "padding");

    /// <summary>
    /// Gets the padding left.
    /// Maps from padding-start in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingLeft => GetDirectionalLength("padding-start", "padding-left", 0, "padding");

    /// <summary>
    /// Gets the padding right.
    /// Maps from padding-end in XSL-FO based on writing-mode.
    /// </summary>
    public double PaddingRight => GetDirectionalLength("padding-end", "padding-right", 0, "padding");

    /// <summary>
    /// Gets the border width.
    /// </summary>
    public double BorderWidth => LengthParser.Parse(Properties.GetString("border-width", "0pt"));

    /// <summary>
    /// Gets the border style.
    /// </summary>
    public string BorderStyle => Properties.GetString("border-style", "none");

    /// <summary>
    /// Gets the border color.
    /// </summary>
    public string BorderColor => Properties.GetString("border-color", "black");

    /// <summary>
    /// Gets the background color.
    /// </summary>
    public string? BackgroundColor => Properties["background-color"];

    /// <summary>
    /// Gets the text alignment.
    /// </summary>
    public string TextAlign => Properties.GetString("text-align", "start");

    /// <summary>
    /// Gets the vertical alignment.
    /// </summary>
    public string VerticalAlign => Properties.GetString("vertical-align", "top");
}
