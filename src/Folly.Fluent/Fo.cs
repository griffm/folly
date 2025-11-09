namespace Folly.Fluent;

/// <summary>
/// Fluent API entry point for building XSL-FO documents programmatically.
/// </summary>
public static class Fo
{
    /// <summary>
    /// Creates a new XSL-FO document.
    /// </summary>
    /// <param name="configure">Action to configure the document.</param>
    /// <returns>A fluent document builder.</returns>
    public static DocumentBuilder Document(Action<DocumentBuilder>? configure = null)
    {
        var builder = new DocumentBuilder();
        configure?.Invoke(builder);
        return builder;
    }
}

/// <summary>
/// Builder for constructing an XSL-FO document.
/// </summary>
public sealed class DocumentBuilder
{
    private PdfMetadata? _metadata;
    private readonly List<FoSimplePageMaster> _simplePageMasters = new();
    private readonly List<FoPageSequence> _pageSequences = new();

    /// <summary>
    /// Configures document metadata.
    /// </summary>
    public DocumentBuilder Metadata(Action<MetadataBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _metadata = new PdfMetadata();
        var builder = new MetadataBuilder(_metadata);
        configure(builder);
        return this;
    }

    /// <summary>
    /// Configures layout masters.
    /// </summary>
    public DocumentBuilder LayoutMasters(Action<LayoutMasterBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new LayoutMasterBuilder(_simplePageMasters);
        configure(builder);
        return this;
    }

    /// <summary>
    /// Adds a page sequence.
    /// </summary>
    public DocumentBuilder PageSequence(string masterReference, Action<PageSequenceBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(masterReference);
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new PageSequenceBuilder(masterReference);
        configure(builder);
        _pageSequences.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Builds the FoRoot from configured builders.
    /// </summary>
    private FoRoot BuildRoot()
    {
        var layoutMasterSet = new FoLayoutMasterSet
        {
            SimplePageMasters = _simplePageMasters.ToArray()
        };

        return new FoRoot
        {
            LayoutMasterSet = layoutMasterSet,
            PageSequences = _pageSequences.ToArray()
        };
    }

    /// <summary>
    /// Saves the document as PDF.
    /// </summary>
    public void SavePdf(string path, PdfOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        options ??= new PdfOptions();
        if (_metadata != null)
        {
            options.Metadata = _metadata;
        }

        var root = BuildRoot();
        var doc = CreateDocument(root);
        doc.SavePdf(path, options);
    }

    /// <summary>
    /// Saves the document as PDF to a stream.
    /// </summary>
    public void SavePdf(Stream output, PdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(output);

        options ??= new PdfOptions();
        if (_metadata != null)
        {
            options.Metadata = _metadata;
        }

        var root = BuildRoot();
        var doc = CreateDocument(root);
        doc.SavePdf(output, options);
    }

    /// <summary>
    /// Creates an FoDocument from the root element using reflection.
    /// This bypasses the XML loading path since we're building programmatically.
    /// </summary>
    private static FoDocument CreateDocument(FoRoot root)
    {
        // Create a minimal XDocument for the constructor
        var xDoc = new XDocument(new XElement("root"));

        // Use reflection to create FoDocument with our programmatically built root
        var ctor = typeof(FoDocument).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(XDocument), typeof(FoRoot) },
            null);

        if (ctor == null)
        {
            throw new InvalidOperationException("Could not find FoDocument constructor");
        }

        return (FoDocument)ctor.Invoke(new object[] { xDoc, root });
    }
}

/// <summary>
/// Builder for document metadata.
/// </summary>
public sealed class MetadataBuilder
{
    private readonly PdfMetadata _metadata;

    internal MetadataBuilder(PdfMetadata metadata)
    {
        _metadata = metadata;
    }

    /// <summary>
    /// Sets the document title.
    /// </summary>
    public MetadataBuilder Title(string title)
    {
        _metadata.Title = title;
        return this;
    }

    /// <summary>
    /// Sets the document author.
    /// </summary>
    public MetadataBuilder Author(string author)
    {
        _metadata.Author = author;
        return this;
    }

    /// <summary>
    /// Sets the document subject.
    /// </summary>
    public MetadataBuilder Subject(string subject)
    {
        _metadata.Subject = subject;
        return this;
    }

    /// <summary>
    /// Sets the document keywords.
    /// </summary>
    public MetadataBuilder Keywords(string keywords)
    {
        _metadata.Keywords = keywords;
        return this;
    }

    /// <summary>
    /// Sets the application that created the document.
    /// </summary>
    public MetadataBuilder Creator(string creator)
    {
        _metadata.Creator = creator;
        return this;
    }

    /// <summary>
    /// Sets the producer (library name and version).
    /// </summary>
    public MetadataBuilder Producer(string producer)
    {
        _metadata.Producer = producer;
        return this;
    }
}

/// <summary>
/// Builder for layout masters.
/// </summary>
public sealed class LayoutMasterBuilder
{
    private readonly List<FoSimplePageMaster> _simplePageMasters;

    internal LayoutMasterBuilder(List<FoSimplePageMaster> simplePageMasters)
    {
        _simplePageMasters = simplePageMasters;
    }

    /// <summary>
    /// Adds a simple page master.
    /// </summary>
    public LayoutMasterBuilder SimplePageMaster(string masterName, string pageWidth, string pageHeight, Action<SimplePageMasterBuilder>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(masterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(pageWidth);
        ArgumentException.ThrowIfNullOrWhiteSpace(pageHeight);

        var builder = new SimplePageMasterBuilder(masterName, pageWidth, pageHeight);
        configure?.Invoke(builder);
        _simplePageMasters.Add(builder.Build());
        return this;
    }
}

/// <summary>
/// Builder for simple page masters.
/// </summary>
public sealed class SimplePageMasterBuilder
{
    private readonly string _masterName;
    private readonly string _pageWidth;
    private readonly string _pageHeight;
    private FoRegionBody? _regionBody;
    private FoRegion? _regionBefore;
    private FoRegion? _regionAfter;
    private FoRegion? _regionStart;
    private FoRegion? _regionEnd;

    internal SimplePageMasterBuilder(string masterName, string pageWidth, string pageHeight)
    {
        _masterName = masterName;
        _pageWidth = pageWidth;
        _pageHeight = pageHeight;
    }

    /// <summary>
    /// Configures the region body.
    /// </summary>
    public SimplePageMasterBuilder RegionBody(Action<RegionBodyBuilder>? configure = null)
    {
        var builder = new RegionBodyBuilder();
        configure?.Invoke(builder);
        _regionBody = builder.Build();
        return this;
    }

    /// <summary>
    /// Configures the region before (header).
    /// </summary>
    public SimplePageMasterBuilder RegionBefore(string extent, Action<RegionBuilder>? configure = null)
    {
        var builder = new RegionBuilder("region-before", extent);
        configure?.Invoke(builder);
        _regionBefore = builder.Build();
        return this;
    }

    /// <summary>
    /// Configures the region after (footer).
    /// </summary>
    public SimplePageMasterBuilder RegionAfter(string extent, Action<RegionBuilder>? configure = null)
    {
        var builder = new RegionBuilder("region-after", extent);
        configure?.Invoke(builder);
        _regionAfter = builder.Build();
        return this;
    }

    /// <summary>
    /// Configures the region start (left sidebar).
    /// </summary>
    public SimplePageMasterBuilder RegionStart(string extent, Action<RegionBuilder>? configure = null)
    {
        var builder = new RegionBuilder("region-start", extent);
        configure?.Invoke(builder);
        _regionStart = builder.Build();
        return this;
    }

    /// <summary>
    /// Configures the region end (right sidebar).
    /// </summary>
    public SimplePageMasterBuilder RegionEnd(string extent, Action<RegionBuilder>? configure = null)
    {
        var builder = new RegionBuilder("region-end", extent);
        configure?.Invoke(builder);
        _regionEnd = builder.Build();
        return this;
    }

    internal FoSimplePageMaster Build()
    {
        var properties = new FoProperties();
        properties["master-name"] = _masterName;
        properties["page-width"] = _pageWidth;
        properties["page-height"] = _pageHeight;

        return new FoSimplePageMaster
        {
            Properties = properties,
            RegionBody = _regionBody,
            RegionBefore = _regionBefore,
            RegionAfter = _regionAfter,
            RegionStart = _regionStart,
            RegionEnd = _regionEnd
        };
    }
}

/// <summary>
/// Builder for region body.
/// </summary>
public sealed class RegionBodyBuilder
{
    private readonly FoProperties _properties = new();

    /// <summary>
    /// Sets margin.
    /// </summary>
    public RegionBodyBuilder Margin(string top, string right, string bottom, string left)
    {
        _properties["margin-top"] = top;
        _properties["margin-right"] = right;
        _properties["margin-bottom"] = bottom;
        _properties["margin-left"] = left;
        return this;
    }

    /// <summary>
    /// Sets all margins to the same value.
    /// </summary>
    public RegionBodyBuilder Margin(string all)
    {
        return Margin(all, all, all, all);
    }

    /// <summary>
    /// Sets the number of columns.
    /// </summary>
    public RegionBodyBuilder ColumnCount(int count)
    {
        _properties["column-count"] = count.ToString();
        return this;
    }

    /// <summary>
    /// Sets the gap between columns.
    /// </summary>
    public RegionBodyBuilder ColumnGap(string gap)
    {
        _properties["column-gap"] = gap;
        return this;
    }

    internal FoRegionBody Build()
    {
        return new FoRegionBody { Properties = _properties };
    }
}

/// <summary>
/// Builder for regions (before, after, start, end).
/// </summary>
public sealed class RegionBuilder
{
    private readonly string _regionName;
    private readonly FoProperties _properties = new();

    internal RegionBuilder(string regionName, string extent)
    {
        _regionName = regionName;
        _properties["extent"] = extent;
    }

    /// <summary>
    /// Sets margin.
    /// </summary>
    public RegionBuilder Margin(string top, string right, string bottom, string left)
    {
        _properties["margin-top"] = top;
        _properties["margin-right"] = right;
        _properties["margin-bottom"] = bottom;
        _properties["margin-left"] = left;
        return this;
    }

    /// <summary>
    /// Sets all margins to the same value.
    /// </summary>
    public RegionBuilder Margin(string all)
    {
        return Margin(all, all, all, all);
    }

    internal FoRegion Build()
    {
        return _regionName switch
        {
            "region-before" => new FoRegionBefore { Properties = _properties },
            "region-after" => new FoRegionAfter { Properties = _properties },
            "region-start" => new FoRegionStart { Properties = _properties },
            "region-end" => new FoRegionEnd { Properties = _properties },
            _ => throw new InvalidOperationException($"Unknown region: {_regionName}")
        };
    }
}

/// <summary>
/// Builder for page sequences.
/// </summary>
public sealed class PageSequenceBuilder
{
    private readonly string _masterReference;
    private readonly List<FoStaticContent> _staticContents = new();
    private FoFlow? _flow;

    internal PageSequenceBuilder(string masterReference)
    {
        _masterReference = masterReference;
    }

    /// <summary>
    /// Adds static content (headers, footers).
    /// </summary>
    public PageSequenceBuilder StaticContent(string flowName, Action<StaticContentBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flowName);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new StaticContentBuilder(flowName);
        configure(builder);
        _staticContents.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Configures the main flow.
    /// </summary>
    public PageSequenceBuilder Flow(Action<FlowBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new FlowBuilder("xsl-region-body");
        configure(builder);
        _flow = builder.Build();
        return this;
    }

    internal FoPageSequence Build()
    {
        var properties = new FoProperties();
        properties["master-reference"] = _masterReference;

        return new FoPageSequence
        {
            Properties = properties,
            StaticContents = _staticContents.ToArray(),
            Flow = _flow
        };
    }
}

/// <summary>
/// Builder for static content.
/// </summary>
public sealed class StaticContentBuilder
{
    private readonly string _flowName;
    private readonly List<FoBlock> _blocks = new();

    internal StaticContentBuilder(string flowName)
    {
        _flowName = flowName;
    }

    /// <summary>
    /// Adds a block.
    /// </summary>
    public StaticContentBuilder Block(Action<BlockBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new BlockBuilder();
        configure(builder);
        _blocks.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a text block.
    /// </summary>
    public StaticContentBuilder Block(string text)
    {
        var builder = new BlockBuilder();
        builder.Text(text);
        _blocks.Add(builder.Build());
        return this;
    }

    internal FoStaticContent Build()
    {
        var properties = new FoProperties();
        properties["flow-name"] = _flowName;

        return new FoStaticContent
        {
            Properties = properties,
            Blocks = _blocks.ToArray()
        };
    }
}

/// <summary>
/// Builder for flows.
/// </summary>
public sealed class FlowBuilder
{
    private readonly string _flowName;
    private readonly List<FoBlock> _blocks = new();
    private readonly List<FoTable> _tables = new();

    internal FlowBuilder(string flowName)
    {
        _flowName = flowName;
    }

    /// <summary>
    /// Adds a block.
    /// </summary>
    public FlowBuilder Block(Action<BlockBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new BlockBuilder();
        configure(builder);
        _blocks.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a text block.
    /// </summary>
    public FlowBuilder Block(string text)
    {
        var builder = new BlockBuilder();
        builder.Text(text);
        _blocks.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a table.
    /// </summary>
    public FlowBuilder Table(Action<TableBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TableBuilder();
        configure(builder);
        _tables.Add(builder.Build());
        return this;
    }

    internal FoFlow Build()
    {
        var properties = new FoProperties();
        properties["flow-name"] = _flowName;

        return new FoFlow
        {
            Properties = properties,
            Blocks = _blocks.ToArray(),
            Tables = _tables.ToArray()
        };
    }
}

/// <summary>
/// Builder for blocks.
/// </summary>
public sealed class BlockBuilder
{
    private readonly FoProperties _properties = new();
    private readonly List<FoElement> _children = new();
    private string? _textContent;

    /// <summary>
    /// Sets text content.
    /// </summary>
    public BlockBuilder Text(string text)
    {
        _textContent = text;
        return this;
    }

    /// <summary>
    /// Sets font family.
    /// </summary>
    public BlockBuilder FontFamily(string fontFamily)
    {
        _properties["font-family"] = fontFamily;
        return this;
    }

    /// <summary>
    /// Sets font size.
    /// </summary>
    public BlockBuilder FontSize(string fontSize)
    {
        _properties["font-size"] = fontSize;
        return this;
    }

    /// <summary>
    /// Sets text alignment.
    /// </summary>
    public BlockBuilder TextAlign(string textAlign)
    {
        _properties["text-align"] = textAlign;
        return this;
    }

    /// <summary>
    /// Sets margin.
    /// </summary>
    public BlockBuilder Margin(string top, string right, string bottom, string left)
    {
        _properties["margin-top"] = top;
        _properties["margin-right"] = right;
        _properties["margin-bottom"] = bottom;
        _properties["margin-left"] = left;
        return this;
    }

    /// <summary>
    /// Sets all margins to the same value.
    /// </summary>
    public BlockBuilder Margin(string all)
    {
        return Margin(all, all, all, all);
    }

    /// <summary>
    /// Sets padding.
    /// </summary>
    public BlockBuilder Padding(string top, string right, string bottom, string left)
    {
        _properties["padding-top"] = top;
        _properties["padding-right"] = right;
        _properties["padding-bottom"] = bottom;
        _properties["padding-left"] = left;
        return this;
    }

    /// <summary>
    /// Sets all padding to the same value.
    /// </summary>
    public BlockBuilder Padding(string all)
    {
        return Padding(all, all, all, all);
    }

    /// <summary>
    /// Sets background color.
    /// </summary>
    public BlockBuilder BackgroundColor(string color)
    {
        _properties["background-color"] = color;
        return this;
    }

    /// <summary>
    /// Sets border.
    /// </summary>
    public BlockBuilder Border(string width, string style, string color)
    {
        _properties["border-width"] = width;
        _properties["border-style"] = style;
        _properties["border-color"] = color;
        return this;
    }

    /// <summary>
    /// Sets line height.
    /// </summary>
    public BlockBuilder LineHeight(string lineHeight)
    {
        _properties["line-height"] = lineHeight;
        return this;
    }

    /// <summary>
    /// Sets break before.
    /// </summary>
    public BlockBuilder BreakBefore(string breakBefore)
    {
        _properties["break-before"] = breakBefore;
        return this;
    }

    /// <summary>
    /// Sets break after.
    /// </summary>
    public BlockBuilder BreakAfter(string breakAfter)
    {
        _properties["break-after"] = breakAfter;
        return this;
    }

    /// <summary>
    /// Sets keep together.
    /// </summary>
    public BlockBuilder KeepTogether(string keepTogether)
    {
        _properties["keep-together"] = keepTogether;
        return this;
    }

    /// <summary>
    /// Adds an inline element.
    /// </summary>
    public BlockBuilder Inline(Action<InlineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new InlineBuilder();
        configure(builder);
        _children.Add(builder.Build());
        return this;
    }

    internal FoBlock Build()
    {
        return new FoBlock
        {
            Properties = _properties,
            Children = _children.ToArray(),
            TextContent = _textContent
        };
    }
}

/// <summary>
/// Builder for inline elements.
/// </summary>
public sealed class InlineBuilder
{
    private readonly FoProperties _properties = new();
    private string? _textContent;

    /// <summary>
    /// Sets text content.
    /// </summary>
    public InlineBuilder Text(string text)
    {
        _textContent = text;
        return this;
    }

    /// <summary>
    /// Sets font family.
    /// </summary>
    public InlineBuilder FontFamily(string fontFamily)
    {
        _properties["font-family"] = fontFamily;
        return this;
    }

    /// <summary>
    /// Sets font size.
    /// </summary>
    public InlineBuilder FontSize(string fontSize)
    {
        _properties["font-size"] = fontSize;
        return this;
    }

    /// <summary>
    /// Sets font weight.
    /// </summary>
    public InlineBuilder FontWeight(string fontWeight)
    {
        _properties["font-weight"] = fontWeight;
        return this;
    }

    /// <summary>
    /// Sets font style.
    /// </summary>
    public InlineBuilder FontStyle(string fontStyle)
    {
        _properties["font-style"] = fontStyle;
        return this;
    }

    /// <summary>
    /// Sets color.
    /// </summary>
    public InlineBuilder Color(string color)
    {
        _properties["color"] = color;
        return this;
    }

    /// <summary>
    /// Sets text decoration.
    /// </summary>
    public InlineBuilder TextDecoration(string textDecoration)
    {
        _properties["text-decoration"] = textDecoration;
        return this;
    }

    /// <summary>
    /// Sets background color.
    /// </summary>
    public InlineBuilder BackgroundColor(string color)
    {
        _properties["background-color"] = color;
        return this;
    }

    internal FoInline Build()
    {
        return new FoInline
        {
            Properties = _properties,
            TextContent = _textContent
        };
    }
}

/// <summary>
/// Builder for tables.
/// </summary>
public sealed class TableBuilder
{
    private readonly FoProperties _properties = new();
    private readonly List<FoTableColumn> _columns = new();
    private FoTableHeader? _header;
    private FoTableBody? _body;
    private FoTableFooter? _footer;

    /// <summary>
    /// Sets table width.
    /// </summary>
    public TableBuilder Width(string width)
    {
        _properties["width"] = width;
        return this;
    }

    /// <summary>
    /// Sets border collapse.
    /// </summary>
    public TableBuilder BorderCollapse(string borderCollapse)
    {
        _properties["border-collapse"] = borderCollapse;
        return this;
    }

    /// <summary>
    /// Adds a table column.
    /// </summary>
    public TableBuilder Column(string columnWidth)
    {
        var properties = new FoProperties();
        properties["column-width"] = columnWidth;
        _columns.Add(new FoTableColumn { Properties = properties });
        return this;
    }

    /// <summary>
    /// Configures the table header.
    /// </summary>
    public TableBuilder Header(Action<TableSectionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TableSectionBuilder();
        configure(builder);
        _header = new FoTableHeader { Rows = builder.BuildRows().ToArray() };
        return this;
    }

    /// <summary>
    /// Configures the table body.
    /// </summary>
    public TableBuilder Body(Action<TableSectionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TableSectionBuilder();
        configure(builder);
        _body = new FoTableBody { Rows = builder.BuildRows().ToArray() };
        return this;
    }

    /// <summary>
    /// Configures the table footer.
    /// </summary>
    public TableBuilder Footer(Action<TableSectionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TableSectionBuilder();
        configure(builder);
        _footer = new FoTableFooter { Rows = builder.BuildRows().ToArray() };
        return this;
    }

    internal FoTable Build()
    {
        return new FoTable
        {
            Properties = _properties,
            Columns = _columns.ToArray(),
            Header = _header,
            Body = _body,
            Footer = _footer
        };
    }
}

/// <summary>
/// Builder for table sections (header, body, footer).
/// </summary>
public sealed class TableSectionBuilder
{
    private readonly List<FoTableRow> _rows = new();

    /// <summary>
    /// Adds a table row.
    /// </summary>
    public TableSectionBuilder Row(Action<TableRowBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TableRowBuilder();
        configure(builder);
        _rows.Add(builder.Build());
        return this;
    }

    internal List<FoTableRow> BuildRows()
    {
        return _rows;
    }
}

/// <summary>
/// Builder for table rows.
/// </summary>
public sealed class TableRowBuilder
{
    private readonly FoProperties _properties = new();
    private readonly List<FoTableCell> _cells = new();

    /// <summary>
    /// Sets row height.
    /// </summary>
    public TableRowBuilder Height(string height)
    {
        _properties["height"] = height;
        return this;
    }

    /// <summary>
    /// Adds a table cell.
    /// </summary>
    public TableRowBuilder Cell(Action<TableCellBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TableCellBuilder();
        configure(builder);
        _cells.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a simple text cell.
    /// </summary>
    public TableRowBuilder Cell(string text)
    {
        var builder = new TableCellBuilder();
        builder.Block(b => b.Text(text));
        _cells.Add(builder.Build());
        return this;
    }

    internal FoTableRow Build()
    {
        return new FoTableRow
        {
            Properties = _properties,
            Cells = _cells.ToArray()
        };
    }
}

/// <summary>
/// Builder for table cells.
/// </summary>
public sealed class TableCellBuilder
{
    private readonly FoProperties _properties = new();
    private readonly List<FoBlock> _blocks = new();

    /// <summary>
    /// Sets padding.
    /// </summary>
    public TableCellBuilder Padding(string padding)
    {
        _properties["padding"] = padding;
        return this;
    }

    /// <summary>
    /// Sets border.
    /// </summary>
    public TableCellBuilder Border(string width, string style, string color)
    {
        _properties["border-width"] = width;
        _properties["border-style"] = style;
        _properties["border-color"] = color;
        return this;
    }

    /// <summary>
    /// Sets background color.
    /// </summary>
    public TableCellBuilder BackgroundColor(string color)
    {
        _properties["background-color"] = color;
        return this;
    }

    /// <summary>
    /// Sets text alignment.
    /// </summary>
    public TableCellBuilder TextAlign(string textAlign)
    {
        _properties["text-align"] = textAlign;
        return this;
    }

    /// <summary>
    /// Sets vertical alignment.
    /// </summary>
    public TableCellBuilder VerticalAlign(string verticalAlign)
    {
        _properties["vertical-align"] = verticalAlign;
        return this;
    }

    /// <summary>
    /// Sets column span.
    /// </summary>
    public TableCellBuilder ColumnSpan(int span)
    {
        _properties["number-columns-spanned"] = span.ToString();
        return this;
    }

    /// <summary>
    /// Sets row span.
    /// </summary>
    public TableCellBuilder RowSpan(int span)
    {
        _properties["number-rows-spanned"] = span.ToString();
        return this;
    }

    /// <summary>
    /// Adds a block.
    /// </summary>
    public TableCellBuilder Block(Action<BlockBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new BlockBuilder();
        configure(builder);
        _blocks.Add(builder.Build());
        return this;
    }

    internal FoTableCell Build()
    {
        return new FoTableCell
        {
            Properties = _properties,
            Blocks = _blocks.ToArray()
        };
    }
}
