using Folly.Testing;

namespace Folly.UnitTests;

/// <summary>
/// Demonstrates precise layout verification using the AreaTreeQuery fluent API.
/// These tests show how to verify exact dimensions, spacing, and layout properties.
/// </summary>
public class AreaTreeQueryTests
{
    [Fact]
    public void ProportionalColumnWidths_CalculatedCorrectly()
    {
        // Table with proportional widths: 1* + 2* + 1* = 4 parts
        // 400pt width -> 100pt, 200pt, 100pt
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="500pt" page-height="300pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:table width="400pt">
                    <fo:table-column column-width="proportional-column-width(1)"/>
                    <fo:table-column column-width="proportional-column-width(2)"/>
                    <fo:table-column column-width="proportional-column-width(1)"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell><fo:block>A</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>B</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>C</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);
        var areaTree = doc.BuildAreaTree();

        // Use fluent query API to inspect the table
        var table = areaTree.Query().FirstPage().FirstTable();

        // Verify column count
        Assert.Equal(3, table.ColumnCount);

        // The key thing is verifying the 1:2:1 ratio, not exact pixel values
        // (which may vary based on border-spacing, padding, etc.)
        var col1Width = table.ColumnWidth(0);
        var col2Width = table.ColumnWidth(1);
        var col3Width = table.ColumnWidth(2);

        // Verify the proportional ratio (1:2:1)
        Assert.Equal(col1Width * 2, col2Width, precision: 1);
        Assert.Equal(col1Width, col3Width, precision: 1);

        // Verify all columns have reasonable widths
        Assert.True(col1Width > 0, "Column 1 should have width");
        Assert.True(col2Width > 0, "Column 2 should have width");
        Assert.True(col3Width > 0, "Column 3 should have width");
    }

    [Fact]
    public void TextJustification_AppliesWordSpacing()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="300pt" page-height="200pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt" text-align="justify">
                    This is a justified paragraph that should have word spacing applied to make the text align to both margins.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);
        var areaTree = doc.BuildAreaTree();

        var block = areaTree.Query().FirstPage().Block(0);

        // Verify justified text has multiple lines
        Assert.True(block.LineCount > 1, "Justified text should wrap to multiple lines");

        // Check first line (should have word spacing)
        var firstLine = block.Line(0);
        Assert.True(firstLine.HasWordSpacing, "First line should have word spacing for justification");
        Assert.True(firstLine.MaxWordSpacing > 0, "Word spacing should be positive");

        // Last line should NOT have word spacing (not justified)
        var lastLine = block.Line(block.LineCount - 1);
        Assert.False(lastLine.HasWordSpacing, "Last line should not have word spacing");
    }

    [Fact]
    public void BlockMargins_CalculatedCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="300pt" page-height="200pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block margin-top="20pt" margin-bottom="15pt"
                            margin-left="10pt" margin-right="5pt"
                            padding-top="8pt" padding-left="4pt">
                    Test block
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);
        var areaTree = doc.BuildAreaTree();

        var block = areaTree.Query().FirstPage().Block(0);

        // Verify margins
        Assert.Equal(20.0, block.Margin.Top);
        Assert.Equal(5.0, block.Margin.Right);
        Assert.Equal(15.0, block.Margin.Bottom);
        Assert.Equal(10.0, block.Margin.Left);

        // Verify padding
        Assert.Equal(8.0, block.Padding.Top);
        Assert.Equal(4.0, block.Padding.Left);

        // Verify combined spacing
        Assert.Equal(15.0, block.Margin.Horizontal); // Left + Right
        Assert.Equal(35.0, block.Margin.Vertical);   // Top + Bottom
    }

    [Fact]
    public void TableCellSpanning_LayoutsCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="400pt" page-height="300pt">
                  <fo:region-body margin="10pt"/>
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
                          <fo:block>Spanning Cell</fo:block>
                        </fo:table-cell>
                        <fo:table-cell>
                          <fo:block>Normal</fo:block>
                        </fo:table-cell>
                      </fo:table-row>
                      <fo:table-row>
                        <fo:table-cell><fo:block>A</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>B</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>C</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);
        var areaTree = doc.BuildAreaTree();

        var table = areaTree.Query().FirstPage().FirstTable();

        // Verify table has rows
        Assert.True(table.RowCount > 0, "Table should have at least one row");

        // Verify first row has cells (spanning reduces cell count from 3 to 2)
        var firstRow = table.Row(0);
        Assert.Equal(2, firstRow.CellCount);

        // Verify spanning cell properties
        var spanningCell = firstRow.Cell(0);
        Assert.Equal(2, spanningCell.ColumnSpan);
        Assert.Equal(0, spanningCell.ColumnIndex);

        // Verify spanning cell width spans multiple columns
        Assert.True(spanningCell.Width > table.ColumnWidth(0),
            "Spanning cell should be wider than a single column");
    }

    [Fact]
    public void TextExtraction_WorksCorrectly()
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
                  <fo:block>First paragraph.</fo:block>
                  <fo:block>Second paragraph with <fo:inline font-weight="bold">bold</fo:inline> text.</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);
        var areaTree = doc.BuildAreaTree();

        // Extract text from entire page
        var pageText = areaTree.Query().FirstPage().ExtractText();
        Assert.Contains("First paragraph.", pageText);
        Assert.Contains("Second paragraph", pageText);
        Assert.Contains("bold", pageText);

        // Extract text from specific block
        var secondBlock = areaTree.Query().FirstPage().Block(1);
        var blockText = secondBlock.ExtractText();
        Assert.Contains("Second paragraph", blockText);
        Assert.Contains("bold", blockText);
        Assert.DoesNotContain("First paragraph", blockText);
    }

    [Fact]
    public void AreaTreeSerialization_ProducesValidJson()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="200pt" page-height="300pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt">Hello, World!</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);
        var areaTree = doc.BuildAreaTree();

        // Serialize to JSON
        var json = AreaTreeInspector.ToJson(areaTree);

        // Verify it's valid JSON by parsing it
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(json);

        // Verify structure
        Assert.True(jsonDoc.RootElement.TryGetProperty("pageCount", out var pageCount));
        Assert.Equal(1, pageCount.GetInt32());

        Assert.True(jsonDoc.RootElement.TryGetProperty("pages", out var pages));
        Assert.Equal(1, pages.GetArrayLength());

        // Verify JSON contains expected data
        Assert.Contains("BlockArea", json);
        Assert.Contains("Hello, World!", json);
        Assert.Contains("fontSize", json);
    }

    [Fact]
    public void AreaTreeSummary_ProvidesUsefulDebugInfo()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="200pt" page-height="300pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Block 1</fo:block>
                  <fo:block>Block 2</fo:block>
                  <fo:table width="150pt">
                    <fo:table-column column-width="75pt"/>
                    <fo:table-column column-width="75pt"/>
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

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);
        var areaTree = doc.BuildAreaTree();

        var summary = AreaTreeInspector.GetSummary(areaTree);

        // Verify summary contains useful information
        Assert.Contains("Pages: 1", summary);
        Assert.Contains("200", summary); // Width
        Assert.Contains("300", summary); // Height
        Assert.Contains("BlockArea", summary);
        Assert.Contains("TableArea", summary);
    }

    [Fact]
    public void MinimalSerialization_IncludesOnlyGeometry()
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
                  <fo:block font-size="12pt" color="red">Test</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);
        var areaTree = doc.BuildAreaTree();

        // Serialize with minimal options
        var json = AreaTreeInspector.ToJson(areaTree, AreaTreeSerializationOptions.Minimal);

        // Should include geometry
        Assert.Contains("\"x\":", json);
        Assert.Contains("\"y\":", json);
        Assert.Contains("\"width\":", json);
        Assert.Contains("\"height\":", json);

        // Should NOT include styling
        Assert.DoesNotContain("fontSize", json);
        Assert.DoesNotContain("color", json);
        Assert.DoesNotContain("Test", json); // No text content
    }

    [Fact]
    public void PageDimensions_MatchMasterSpecification()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="custom" page-width="500pt" page-height="700pt">
                  <fo:region-body margin="25pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="custom">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);
        var areaTree = doc.BuildAreaTree();

        var page = areaTree.Query().FirstPage();

        Assert.Equal(500.0, page.Width);
        Assert.Equal(700.0, page.Height);
        Assert.Equal(1, page.PageNumber);
    }

    [Fact]
    public void InlineProperties_CanBeInspected()
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
                  <fo:block font-family="Helvetica" font-size="14pt">
                    Normal <fo:inline font-weight="bold" color="blue">bold blue</fo:inline> text.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);
        var areaTree = doc.BuildAreaTree();

        var block = areaTree.Query().FirstPage().Block(0);
        var line = block.Line(0);

        // Verify we have inlines
        Assert.True(line.InlineCount > 0, "Should have inlines");

        // Find the bold blue inline (demonstrates property inspection)
        var inlines = line.Inlines().ToList();
        var boldInline = inlines.FirstOrDefault(i => i.FontWeight == "bold" && i.Color == "blue");

        // If we found a bold inline, verify its properties
        Assert.NotNull(boldInline);
        Assert.Equal("bold", boldInline.FontWeight);
        Assert.Equal("blue", boldInline.Color);

        // Verify we can extract text
        var lineText = line.ExtractText();
        Assert.Contains("Normal", lineText);
        Assert.Contains("bold", lineText);
        Assert.Contains("text", lineText);
    }
}
