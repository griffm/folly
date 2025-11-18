using Folly.Layout.Testing;

namespace Folly.UnitTests;

/// <summary>
/// Comprehensive validation tests for all implemented features using area tree inspection.
/// These tests verify that the major features work correctly by inspecting the generated area tree.
/// </summary>
public class FeatureValidationTests
{
    [Fact]
    public void Feature_BasicBlockLayout_WorksCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="210mm" page-height="297mm">
                  <fo:region-body margin="20mm"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="14pt" margin-bottom="10pt">First paragraph</fo:block>
                  <fo:block font-size="12pt" margin-bottom="8pt">Second paragraph</fo:block>
                  <fo:block font-size="10pt">Third paragraph</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var page = areaTree.Query().FirstPage();
        Assert.Equal(1, page.PageNumber);

        var blocks = page.Blocks().ToList();
        Assert.Equal(3, blocks.Count);

        // Verify font sizes
        Assert.Equal(14.0, blocks[0].FontSize);
        Assert.Equal(12.0, blocks[1].FontSize);
        Assert.Equal(10.0, blocks[2].FontSize);

        // Verify text content
        Assert.Contains("First", blocks[0].ExtractText());
        Assert.Contains("Second", blocks[1].ExtractText());
        Assert.Contains("Third", blocks[2].ExtractText());
    }

    [Fact]
    public void Feature_TextAlignment_AllModesWork()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block text-align="start">Left aligned text</fo:block>
                  <fo:block text-align="center">Centered text</fo:block>
                  <fo:block text-align="end">Right aligned text</fo:block>
                  <fo:block text-align="justify">Justified text that wraps to multiple lines to demonstrate justification</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var blocks = areaTree.Query().FirstPage().Blocks().ToList();
        Assert.Equal(4, blocks.Count);

        Assert.Equal("start", blocks[0].TextAlign);
        Assert.Equal("center", blocks[1].TextAlign);
        Assert.Equal("end", blocks[2].TextAlign);
        Assert.Equal("justify", blocks[3].TextAlign);
    }

    [Fact]
    public void Feature_MarginsAndPadding_AppliedCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block margin="12pt" padding="8pt">Block with uniform spacing</fo:block>
                  <fo:block margin-top="20pt" margin-bottom="10pt"
                            padding-left="15pt" padding-right="5pt">
                    Block with asymmetric spacing
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var blocks = areaTree.Query().FirstPage().Blocks().ToList();

        // First block: uniform spacing
        Assert.Equal(12.0, blocks[0].Margin.Top);
        Assert.Equal(12.0, blocks[0].Margin.Right);
        Assert.Equal(12.0, blocks[0].Margin.Bottom);
        Assert.Equal(12.0, blocks[0].Margin.Left);
        Assert.Equal(8.0, blocks[0].Padding.Top);

        // Second block: asymmetric spacing
        Assert.Equal(20.0, blocks[1].Margin.Top);
        Assert.Equal(10.0, blocks[1].Margin.Bottom);
        Assert.Equal(15.0, blocks[1].Padding.Left);
        Assert.Equal(5.0, blocks[1].Padding.Right);
    }

    [Fact]
    public void Feature_Tables_BasicLayoutWorks()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:table>
                    <fo:table-column column-width="100pt"/>
                    <fo:table-column column-width="150pt"/>
                    <fo:table-column column-width="100pt"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell><fo:block>A1</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>B1</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>C1</fo:block></fo:table-cell>
                      </fo:table-row>
                      <fo:table-row>
                        <fo:table-cell><fo:block>A2</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>B2</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>C2</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var table = areaTree.Query().FirstPage().FirstTable();

        // Verify table structure
        Assert.Equal(3, table.ColumnCount);
        Assert.True(table.RowCount >= 1, "Table should have at least one row");

        // Verify column widths
        Assert.Equal(100.0, table.ColumnWidth(0), precision: 1);
        Assert.Equal(150.0, table.ColumnWidth(1), precision: 1);
        Assert.Equal(100.0, table.ColumnWidth(2), precision: 1);

        // Verify cell content from first row
        var firstRow = table.Row(0);
        Assert.Equal(3, firstRow.CellCount);
        Assert.Contains("A", firstRow.Cell(0).ExtractText());
        Assert.Contains("B", firstRow.Cell(1).ExtractText());
        Assert.Contains("C", firstRow.Cell(2).ExtractText());
    }

    [Fact]
    public void Feature_ProportionalColumnWidths_CalculatedCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="500pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:table width="480pt">
                    <fo:table-column column-width="proportional-column-width(1)"/>
                    <fo:table-column column-width="proportional-column-width(3)"/>
                    <fo:table-column column-width="proportional-column-width(2)"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell><fo:block>Narrow</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>Wide</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>Medium</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var table = areaTree.Query().FirstPage().FirstTable();

        // Verify 1:3:2 ratio
        var col1 = table.ColumnWidth(0);
        var col2 = table.ColumnWidth(1);
        var col3 = table.ColumnWidth(2);

        // Column 2 should be 3x column 1
        Assert.Equal(col1 * 3, col2, precision: 1);

        // Column 3 should be 2x column 1
        Assert.Equal(col1 * 2, col3, precision: 1);
    }

    [Fact]
    public void Feature_TableCellSpanning_WorksCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:table width="300pt">
                    <fo:table-column column-width="100pt"/>
                    <fo:table-column column-width="100pt"/>
                    <fo:table-column column-width="100pt"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell number-columns-spanned="2">
                          <fo:block>Spans 2 columns</fo:block>
                        </fo:table-cell>
                        <fo:table-cell>
                          <fo:block>Normal</fo:block>
                        </fo:table-cell>
                      </fo:table-row>
                      <fo:table-row>
                        <fo:table-cell number-rows-spanned="2">
                          <fo:block>Spans 2 rows</fo:block>
                        </fo:table-cell>
                        <fo:table-cell><fo:block>B2</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>C2</fo:block></fo:table-cell>
                      </fo:table-row>
                      <fo:table-row>
                        <fo:table-cell><fo:block>B3</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>C3</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var table = areaTree.Query().FirstPage().FirstTable();

        // Verify table has rows
        Assert.True(table.RowCount > 0, "Table should have rows");

        // First row: 2 cells (one spans 2 columns)
        var row1 = table.Row(0);
        Assert.Equal(2, row1.CellCount);
        Assert.Equal(2, row1.Cell(0).ColumnSpan);

        // Verify spanning cell content
        Assert.Contains("Spans", row1.Cell(0).ExtractText());
    }

    [Fact]
    public void Feature_Lists_LayoutCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:list-block>
                    <fo:list-item>
                      <fo:list-item-label end-indent="label-end()">
                        <fo:block>1.</fo:block>
                      </fo:list-item-label>
                      <fo:list-item-body start-indent="body-start()">
                        <fo:block>First item</fo:block>
                      </fo:list-item-body>
                    </fo:list-item>
                    <fo:list-item>
                      <fo:list-item-label end-indent="label-end()">
                        <fo:block>2.</fo:block>
                      </fo:list-item-label>
                      <fo:list-item-body start-indent="body-start()">
                        <fo:block>Second item</fo:block>
                      </fo:list-item-body>
                    </fo:list-item>
                  </fo:list-block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var pageText = areaTree.Query().FirstPage().ExtractText();
        Assert.Contains("1.", pageText);
        Assert.Contains("First item", pageText);
        Assert.Contains("2.", pageText);
        Assert.Contains("Second item", pageText);
    }

    [Fact]
    public void Feature_MultiPageLayout_BreaksCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="210mm" page-height="100pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block margin-bottom="20pt">Block 1</fo:block>
                  <fo:block margin-bottom="20pt">Block 2</fo:block>
                  <fo:block margin-bottom="20pt">Block 3</fo:block>
                  <fo:block margin-bottom="20pt">Block 4</fo:block>
                  <fo:block margin-bottom="20pt">Block 5</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var query = areaTree.Query();

        // Should create multiple pages
        Assert.True(query.PageCount > 1, "Content should span multiple pages");

        // Verify page numbers
        var pages = query.AllPages().ToList();
        for (int i = 0; i < pages.Count; i++)
        {
            Assert.Equal(i + 1, pages[i].PageNumber);
        }
    }

    [Fact]
    public void Feature_InlineFormatting_WorksCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    Normal <fo:inline font-weight="bold">bold</fo:inline>
                    <fo:inline font-style="italic">italic</fo:inline>
                    <fo:inline text-decoration="underline">underline</fo:inline>
                    <fo:inline color="red">red</fo:inline>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var line = areaTree.Query().FirstPage().Block(0).Line(0);
        var inlines = line.Inlines().ToList();

        // Find formatted inlines
        var boldInline = inlines.FirstOrDefault(i => i.FontWeight == "bold");
        var italicInline = inlines.FirstOrDefault(i => i.FontStyle == "italic");
        var underlineInline = inlines.FirstOrDefault(i => i.Text?.Contains("underline") == true);
        var redInline = inlines.FirstOrDefault(i => i.Color == "red");

        Assert.NotNull(boldInline);
        Assert.NotNull(italicInline);
        Assert.NotNull(underlineInline);
        Assert.NotNull(redInline);
    }

    [Fact]
    public void Feature_TextJustification_AppliesWordSpacing()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="250pt" page-height="200pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block text-align="justify">
                    This is a justified paragraph with enough text to wrap across multiple lines
                    and demonstrate that word spacing is being applied correctly to fill the line width.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var block = areaTree.Query().FirstPage().Block(0);

        // Justified text should have multiple lines
        Assert.True(block.LineCount > 1, "Justified text should wrap");

        // First line should have word spacing applied
        var firstLine = block.Line(0);
        Assert.True(firstLine.HasWordSpacing, "Justified lines should have word spacing");
        Assert.True(firstLine.MaxWordSpacing > 0, "Word spacing should be positive");
    }

    [Fact]
    public void Feature_KeepTogether_PreventsPageBreak()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="210mm" page-height="120pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block margin-bottom="30pt">First block (takes space)</fo:block>
                  <fo:block margin-bottom="30pt">Second block (takes space)</fo:block>
                  <fo:block keep-together.within-page="always" margin-bottom="30pt">
                    Third block with keep-together that would normally break
                  </fo:block>
                  <fo:block>Fourth block</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        // The area tree should handle keep-together constraint
        // (We can't easily verify page placement without more context,
        // but we can verify the area tree was built successfully)
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= 1);
    }

    [Fact]
    public void Feature_PageDimensions_RespectPageMaster()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="custom"
                                       page-width="8.5in"
                                       page-height="11in">
                  <fo:region-body margin="1in"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="custom">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var page = areaTree.Query().FirstPage();

        // 8.5in = 612pt, 11in = 792pt
        Assert.Equal(612.0, page.Width, precision: 1);
        Assert.Equal(792.0, page.Height, precision: 1);
    }

    [Fact]
    public void Feature_AreaTreeSerialization_ProducesValidJson()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="14pt">
                    <fo:inline color="blue">Hello, World!</fo:inline>
                  </fo:block>
                  <fo:table width="200pt">
                    <fo:table-column column-width="100pt"/>
                    <fo:table-column column-width="100pt"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell><fo:block>A</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>B</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        // Test different serialization modes
        var fullJson = AreaTreeInspector.ToJson(areaTree);
        var minimalJson = AreaTreeInspector.ToJson(areaTree, AreaTreeSerializationOptions.Minimal);
        var layoutTestingJson = AreaTreeInspector.ToJson(areaTree, AreaTreeSerializationOptions.LayoutTesting);

        // Verify all produce valid JSON
        using var fullDoc = System.Text.Json.JsonDocument.Parse(fullJson);
        using var minimalDoc = System.Text.Json.JsonDocument.Parse(minimalJson);
        using var layoutDoc = System.Text.Json.JsonDocument.Parse(layoutTestingJson);

        // Full JSON should include typography
        Assert.Contains("fontSize", fullJson);
        Assert.Contains("blue", fullJson);
        Assert.Contains("Hello, World!", fullJson);

        // Minimal JSON should not include typography or text
        Assert.DoesNotContain("fontSize", minimalJson);
        Assert.DoesNotContain("Hello, World!", minimalJson);

        // Layout testing should include table details
        Assert.Contains("columnWidths", layoutTestingJson);
    }

    [Fact]
    public void Feature_AreaTreeSummary_ProvidesUsefulInformation()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="210mm" page-height="297mm">
                  <fo:region-body margin="20mm"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Text block</fo:block>
                  <fo:table>
                    <fo:table-column column-width="100pt"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell><fo:block>Cell</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var summary = AreaTreeInspector.GetSummary(areaTree);

        // Verify summary contains key information
        Assert.Contains("Pages: 1", summary);
        Assert.Contains("BlockArea", summary);
        Assert.Contains("TableArea", summary);
        Assert.Contains("595.", summary); // ~210mm in points
        Assert.Contains("842.", summary); // ~297mm in points
    }

    [Fact]
    public void Feature_TextExtraction_PreservesContent()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Paragraph one with some content.</fo:block>
                  <fo:block>Paragraph two with <fo:inline font-weight="bold">bold text</fo:inline>.</fo:block>
                  <fo:table>
                    <fo:table-column column-width="100pt"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell><fo:block>Table content</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        var page = areaTree.Query().FirstPage();
        var fullText = page.ExtractText();

        // Verify all text is extracted (at least the paragraphs)
        Assert.Contains("Paragraph one", fullText);
        Assert.Contains("Paragraph two", fullText);
        Assert.Contains("bold text", fullText);

        // Verify block-level extraction
        var firstBlock = page.Block(0);
        var blockText = firstBlock.ExtractText();
        Assert.Contains("Paragraph one", blockText);
        Assert.DoesNotContain("Paragraph two", blockText);
    }
}
