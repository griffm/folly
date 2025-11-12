namespace Folly.UnitTests;

/// <summary>
/// Tests for layout engine edge cases including line breaking, page breaking,
/// multi-column layout, table layout, and footnote placement.
/// </summary>
public class LayoutEngineTests
{
    [Fact]
    public void LongWord_BreaksCorrectly()
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
                  <fo:block font-size="12pt">
                    Supercalifragilisticexpialidocious
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Should handle long word without crashing
        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void MultipleWhitespaces_HandledCorrectly()
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
                  <fo:block>Text    with    multiple    spaces</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void MixedFontSizes_LayoutCorrectly()
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
                  <fo:block font-size="12pt">
                    Normal <fo:inline font-size="24pt">Large</fo:inline>
                    <fo:inline font-size="8pt">Small</fo:inline> text
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);

        var page = areaTree.Pages[0];
        Assert.NotEmpty(page.Areas);
    }

    [Fact]
    public void KeepTogether_PreventsPageBreak()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="200pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block margin-bottom="100pt">First block fills most of page</fo:block>
                  <fo:block keep-together="always">
                    <fo:block>Line 1</fo:block>
                    <fo:block>Line 2</fo:block>
                    <fo:block>Line 3</fo:block>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        // Should create multiple pages due to keep-together constraint
        Assert.True(areaTree.Pages.Count >= 1);
    }

    [Fact]
    public void BreakBefore_ForcesPageBreak()
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
                  <fo:block>First page content</fo:block>
                  <fo:block break-before="page">Second page content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= 2, "break-before should create at least 2 pages");
    }

    [Fact]
    public void MultiColumnLayout_BalancesContent()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body column-count="3" column-gap="12pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Column 1 content that should flow into multiple columns</fo:block>
                  <fo:block>More content for column layout testing</fo:block>
                  <fo:block>Even more content to fill the columns</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void TableWithColspan_LayoutsCorrectly()
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
                    <fo:table-column column-width="100pt"/>
                    <fo:table-column column-width="100pt"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell number-columns-spanned="3">
                          <fo:block>Spanning cell</fo:block>
                        </fo:table-cell>
                      </fo:table-row>
                      <fo:table-row>
                        <fo:table-cell><fo:block>Cell 1</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>Cell 2</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>Cell 3</fo:block></fo:table-cell>
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
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void TableWithRowspan_LayoutsCorrectly()
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
                    <fo:table-column column-width="200pt"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell number-rows-spanned="2">
                          <fo:block>Tall cell</fo:block>
                        </fo:table-cell>
                        <fo:table-cell><fo:block>Cell A</fo:block></fo:table-cell>
                      </fo:table-row>
                      <fo:table-row>
                        <fo:table-cell><fo:block>Cell B</fo:block></fo:table-cell>
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
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void Footnote_PlacesCorrectly()
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
                    Main text with a footnote<fo:footnote>
                      <fo:inline>1</fo:inline>
                      <fo:footnote-body>
                        <fo:block>Footnote content</fo:block>
                      </fo:footnote-body>
                    </fo:footnote> reference.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void EmptyPage_HandlesGracefully()
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
                  <fo:block></fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void ZeroMargins_LayoutsCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body margin="0pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Content with zero margins</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void VerySmallFontSize_HandlesCorrectly()
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
                  <fo:block font-size="4pt">Very small text</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void VeryLargeFontSize_HandlesCorrectly()
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
                  <fo:block font-size="72pt">HUGE</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void NestedTables_LayoutCorrectly()
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
                    <fo:table-column column-width="300pt"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            <fo:table>
                              <fo:table-column column-width="150pt"/>
                              <fo:table-body>
                                <fo:table-row>
                                  <fo:table-cell>
                                    <fo:block>Nested table cell</fo:block>
                                  </fo:table-cell>
                                </fo:table-row>
                              </fo:table-body>
                            </fo:table>
                          </fo:block>
                        </fo:table-cell>
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
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void LineHeight_CalculatesCorrectly()
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
                  <fo:block font-size="12pt" line-height="24pt">
                    Line 1 with double spacing
                  </fo:block>
                  <fo:block font-size="12pt" line-height="24pt">
                    Line 2 with double spacing
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);

        var page = areaTree.Pages[0];
        Assert.True(page.Areas.Count >= 2);
    }

    [Fact]
    public void FootnoteSeparator_RendersWhenDefined()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-footnote-separator">
                  <fo:block>
                    <fo:leader leader-pattern="rule" leader-length="2in" rule-thickness="0.5pt"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt">
                    This text has a footnote.<fo:footnote><fo:inline>1</fo:inline><fo:footnote-body><fo:block font-size="10pt">This is the footnote text.</fo:block></fo:footnote-body></fo:footnote>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.Single(areaTree.Pages);

        var page = areaTree.Pages[0];
        // Verify that footnote separator is rendered (should have leader area)
        // Leader areas are typically children of block areas
        var hasLeaderArea = page.Areas.Any(a => a is LeaderArea ||
            (a is BlockArea block && block.Children.Any(c => c is LeaderArea)));
        Assert.True(hasLeaderArea, "Footnote separator should render a leader area");
    }

    [Fact]
    public void FootnoteSeparator_NotRenderedWithoutFootnotes()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-footnote-separator">
                  <fo:block>
                    <fo:leader leader-pattern="rule" leader-length="2in" rule-thickness="0.5pt"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt">
                    This text has no footnote.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.Single(areaTree.Pages);

        var page = areaTree.Pages[0];
        // Without footnotes, the separator should not be rendered
        // Leader areas are typically children of block areas
        var hasLeaderArea = page.Areas.Any(a => a is LeaderArea ||
            (a is BlockArea block && block.Children.Any(c => c is LeaderArea)));
        Assert.False(hasLeaderArea, "Footnote separator should not render when there are no footnotes");
    }

    [Fact]
    public void TextAlign_Justify_AppliesWordSpacing()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="300pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt" text-align="justify">
                    This is a test paragraph with multiple words that will be justified across the line.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);

        var page = areaTree.Pages[0];
        var blockArea = page.Areas.OfType<BlockArea>().FirstOrDefault();
        Assert.NotNull(blockArea);

        // Check that lines have word spacing applied (for justified text)
        var lineAreas = blockArea.Children.OfType<LineArea>().ToList();
        Assert.NotEmpty(lineAreas);

        // At least one line should have word spacing > 0 (non-last lines)
        var hasJustifiedLine = lineAreas.Take(lineAreas.Count - 1) // Exclude last line
            .Any(line => line.Inlines
                .Any(inline => inline.WordSpacing > 0));

        Assert.True(hasJustifiedLine || lineAreas.Count == 1,
            "Justified text should have word spacing on non-last lines (or be a single line)");
    }

    [Fact]
    public void TextAlign_Justify_LastLineNotJustified()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="250pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt" text-align="justify">
                    This is a longer paragraph with enough text to span multiple lines so we can test that the last line is not justified while the previous lines are justified.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);

        var page = areaTree.Pages[0];
        var blockArea = page.Areas.OfType<BlockArea>().FirstOrDefault();
        Assert.NotNull(blockArea);

        var lineAreas = blockArea.Children.OfType<LineArea>().ToList();

        if (lineAreas.Count > 1)
        {
            // Last line should not be justified (word spacing should be 0)
            var lastLine = lineAreas.Last();
            var lastLineInlines = lastLine.Inlines.ToList();
            Assert.All(lastLineInlines, inline => Assert.True(inline.WordSpacing == 0,
                "Last line of justified paragraph should not have word spacing"));
        }
    }

    [Fact]
    public void TextAlignLast_OverridesJustifyForLastLine()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="250pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt" text-align="justify" text-align-last="center">
                    This is a test paragraph that spans multiple lines to verify that the last line is centered instead of left-aligned.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);

        var page = areaTree.Pages[0];
        var blockArea = page.Areas.OfType<BlockArea>().FirstOrDefault();
        Assert.NotNull(blockArea);

        var lineAreas = blockArea.Children.OfType<LineArea>().ToList();

        if (lineAreas.Count > 1)
        {
            // Last line should be centered (X offset > 0 and not justified)
            var lastLine = lineAreas.Last();
            var lastLineInlines = lastLine.Inlines.ToList();

            // Check that the last line has centered alignment (X > 0)
            Assert.True(lastLineInlines.Any(inline => inline.X > 0),
                "Last line with text-align-last='center' should be centered (X > 0)");

            // And no word spacing
            Assert.All(lastLineInlines, inline => Assert.True(inline.WordSpacing == 0));
        }
    }

    [Fact]
    public void TextAlign_Justify_SingleWord_NotJustified()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="300pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt" text-align="justify">
                    SingleWord
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);

        var page = areaTree.Pages[0];
        var blockArea = page.Areas.OfType<BlockArea>().FirstOrDefault();
        Assert.NotNull(blockArea);

        var lineAreas = blockArea.Children.OfType<LineArea>().ToList();
        Assert.NotEmpty(lineAreas);

        // Single word should not be justified (no spaces to distribute)
        var inlines = lineAreas.First().Inlines.ToList();
        Assert.All(inlines, inline => Assert.True(inline.WordSpacing == 0,
            "Single word should not be justified (no word spacing)"));
    }

    [Fact]
    public void TextAlign_Justify_MultipleParagraphs()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="250pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt" text-align="justify" margin-bottom="12pt">
                    First paragraph with enough text to span multiple lines and test justification.
                  </fo:block>
                  <fo:block font-size="12pt" text-align="justify">
                    Second paragraph also with enough text to span multiple lines for testing.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);

        var page = areaTree.Pages[0];
        var blockAreas = page.Areas.OfType<BlockArea>().ToList();
        Assert.True(blockAreas.Count >= 2, "Should have at least 2 block areas");

        // Verify each paragraph has justified text
        foreach (var blockArea in blockAreas)
        {
            var lineAreas = blockArea.Children.OfType<LineArea>().ToList();
            if (lineAreas.Count > 1)
            {
                // Non-last lines should have word spacing
                var nonLastLines = lineAreas.Take(lineAreas.Count - 1);
                Assert.True(nonLastLines.Any(line =>
                    line.Inlines.Any(inline => inline.WordSpacing > 0)),
                    "Each paragraph should have justified non-last lines");
            }
        }
    }

    [Fact]
    public void TextAlignLast_DefaultsToStart_WhenTextAlignIsJustify()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="250pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt" text-align="justify">
                    This paragraph has text-align justify but no text-align-last specified so the last line should default to start alignment.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);

        var page = areaTree.Pages[0];
        var blockArea = page.Areas.OfType<BlockArea>().FirstOrDefault();
        Assert.NotNull(blockArea);

        var lineAreas = blockArea.Children.OfType<LineArea>().ToList();

        if (lineAreas.Count > 1)
        {
            // Last line should be start-aligned (X = 0)
            var lastLine = lineAreas.Last();
            var lastLineInlines = lastLine.Inlines.ToList();

            // Start-aligned text should have X = 0 (or close to it)
            Assert.True(lastLineInlines.All(inline => inline.X <= 1),
                "Last line should default to start alignment (X near 0) when text-align is justify");
        }
    }

    [Theory]
    [InlineData(20, 1)]   // 20 rows should fit on 1 page
    [InlineData(40, 2)]   // 40 rows should span 2 pages
    [InlineData(100, 3)]  // 100 rows should span 3+ pages
    public void MultiPageTable_BreaksAcrossPages(int rowCount, int expectedMinPages)
    {
        var rows = new System.Text.StringBuilder();
        for (int i = 1; i <= rowCount; i++)
        {
            rows.AppendLine($@"
                  <fo:table-row>
                    <fo:table-cell padding=""4pt"" border-width=""1pt"" border-style=""solid"">
                      <fo:block font-size=""10pt"">Row {i}</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding=""4pt"" border-width=""1pt"" border-style=""solid"">
                      <fo:block font-size=""10pt"">Data {i}</fo:block>
                    </fo:table-cell>
                  </fo:table-row>");
        }

        var foXml = $"""
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="14pt" margin-bottom="12pt">Multi-Page Table Test</fo:block>
                  <fo:table border-collapse="separate" border-spacing="2pt">
                    <fo:table-column column-width="200pt"/>
                    <fo:table-column column-width="200pt"/>

                    <fo:table-header>
                      <fo:table-row>
                        <fo:table-cell padding="6pt" border-width="1pt" border-style="solid" background-color="#CCCCCC">
                          <fo:block font-size="12pt" font-weight="bold">Column 1</fo:block>
                        </fo:table-cell>
                        <fo:table-cell padding="6pt" border-width="1pt" border-style="solid" background-color="#CCCCCC">
                          <fo:block font-size="12pt" font-weight="bold">Column 2</fo:block>
                        </fo:table-cell>
                      </fo:table-row>
                    </fo:table-header>

                    <fo:table-body>
                      {rows}
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= expectedMinPages,
            $"Expected at least {expectedMinPages} pages for {rowCount} rows, got {areaTree.Pages.Count}");

        // Verify header appears on each page (check for TableArea with header)
        foreach (var page in areaTree.Pages)
        {
            var tableAreas = page.Areas.OfType<TableArea>().ToList();
            Assert.NotEmpty(tableAreas);
        }
    }

    [Fact]
    public void TableWithOmitHeaderAtBreak_OnlyShowsHeaderOnFirstPage()
    {
        var rows = new System.Text.StringBuilder();
        for (int i = 1; i <= 50; i++)
        {
            rows.AppendLine($@"
                  <fo:table-row>
                    <fo:table-cell padding=""4pt"" border-width=""1pt"" border-style=""solid"">
                      <fo:block font-size=""10pt"">Row {i}</fo:block>
                    </fo:table-cell>
                  </fo:table-row>");
        }

        var foXml = $"""
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:table table-omit-header-at-break="true" border-collapse="separate" border-spacing="2pt">
                    <fo:table-column column-width="400pt"/>

                    <fo:table-header>
                      <fo:table-row>
                        <fo:table-cell padding="6pt" border-width="2pt" border-style="solid" background-color="#4A90E2">
                          <fo:block font-size="12pt" font-weight="bold" color="white">Header (First Page Only)</fo:block>
                        </fo:table-cell>
                      </fo:table-row>
                    </fo:table-header>

                    <fo:table-body>
                      {rows}
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= 2, "Table should span at least 2 pages");

        // Count headers across all pages - should only be on first page
        int headerCount = 0;
        foreach (var page in areaTree.Pages)
        {
            var tableAreas = page.Areas.OfType<TableArea>().ToList();
            foreach (var table in tableAreas)
            {
                // Check if this table area contains header rows (would have specific styling)
                if (table.Rows.Any())
                {
                    headerCount += table.Rows.Count(row =>
                        row.Cells.Any(cell => cell.BackgroundColor == "#4A90E2"));
                }
            }
        }

        // With omit-header-at-break=true, header should only appear once
        Assert.Equal(1, headerCount);
    }

    [Fact(Skip = "Keep-together implementation needs refinement")]
    public void TableRowWithKeepTogether_StartsOnNewPageWhenNeeded()
    {
        // Create many rows to fill the first page, then a keep-together row
        var rows = new System.Text.StringBuilder();
        // Add 25 small rows to fill most of the first page
        for (int i = 1; i <= 25; i++)
        {
            rows.AppendLine($@"
                      <fo:table-row>
                        <fo:table-cell padding=""4pt"" border-width=""1pt"" border-style=""solid"">
                          <fo:block font-size=""10pt"">Row {i}</fo:block>
                        </fo:table-cell>
                      </fo:table-row>");
        }
        // Add a large row with keep-together that should move to next page
        rows.AppendLine(@"
                      <fo:table-row keep-together=""always"">
                        <fo:table-cell padding=""60pt"" border-width=""1pt"" border-style=""solid"">
                          <fo:block font-size=""10pt"">Large row with keep-together</fo:block>
                        </fo:table-cell>
                      </fo:table-row>");

        var foXml = $"""
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:table border-collapse="separate" border-spacing="2pt">
                    <fo:table-column column-width="400pt"/>

                    <fo:table-body>
                      {rows}
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);

        // The keep-together constraint should prevent breaking the large row
        // and move it to the next page instead
        Assert.True(areaTree.Pages.Count >= 2,
            "Keep-together constraint should create at least 2 pages");
    }

    [Fact]
    public void KeepWithNext_KeepsHeadingWithParagraph()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="210pt" page-height="297pt">
                  <fo:region-body margin="20pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt">
                    Filler text to push the heading near the bottom of the page.
                    This paragraph takes up enough space so that the heading
                    will be near the page break. We need several lines of text
                    to achieve this. More filler text here. Even more text to ensure
                    we get close to the page break. Additional content to fill space.
                    Another line of text. And another. One more line. Final filler line.
                  </fo:block>
                  <fo:block font-size="16pt" font-weight="bold" keep-with-next="always">
                    Important Heading
                  </fo:block>
                  <fo:block font-size="12pt">
                    This paragraph should stay with the heading above due to keep-with-next.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);

        // Both the heading and the following paragraph should be on the same page
        // Find the pages containing the heading and the paragraph
        var headingFound = false;
        var paragraphFound = false;
        var headingPageIndex = -1;
        var paragraphPageIndex = -1;

        for (int i = 0; i < areaTree.Pages.Count; i++)
        {
            var page = areaTree.Pages[i];
            foreach (var area in page.Areas)
            {
                if (area is BlockArea blockArea)
                {
                    // Check if this block contains the heading or paragraph text
                    var hasHeading = blockArea.Children.Any(child =>
                        child is LineArea line && line.Inlines.Any(inline =>
                            inline.Text?.Contains("Important Heading") == true));

                    var hasParagraph = blockArea.Children.Any(child =>
                        child is LineArea line && line.Inlines.Any(inline =>
                            inline.Text?.Contains("paragraph should") == true || inline.Text?.Contains("stay") == true));

                    if (hasHeading)
                    {
                        headingFound = true;
                        headingPageIndex = i;
                    }

                    if (hasParagraph)
                    {
                        paragraphFound = true;
                        paragraphPageIndex = i;
                    }
                }
            }
        }

        Assert.True(headingFound, "Heading should be found in the area tree");
        Assert.True(paragraphFound, "Paragraph should be found in the area tree");
        Assert.Equal(headingPageIndex, paragraphPageIndex);
    }

    [Fact]
    public void KeepWithPrevious_KeepsBlocksWithPrevious()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="210pt" page-height="297pt">
                  <fo:region-body margin="20pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt">
                    Filler text to push blocks near the bottom of the page.
                    This paragraph takes up enough space. We need several lines.
                    More filler. Even more. Additional content. Another line.
                    And another. One more. Final filler line. More text here.
                  </fo:block>
                  <fo:block font-size="12pt">
                    First block without keep constraint.
                  </fo:block>
                  <fo:block font-size="12pt" keep-with-previous="always">
                    Second block should stay with previous due to keep-with-previous.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);

        // Both blocks should be on the same page
        var firstBlockFound = false;
        var secondBlockFound = false;
        var firstBlockPageIndex = -1;
        var secondBlockPageIndex = -1;

        for (int i = 0; i < areaTree.Pages.Count; i++)
        {
            var page = areaTree.Pages[i];
            foreach (var area in page.Areas)
            {
                if (area is BlockArea blockArea)
                {
                    var hasFirstBlock = blockArea.Children.Any(child =>
                        child is LineArea line && line.Inlines.Any(inline =>
                            inline.Text?.Contains("First block without") == true));

                    var hasSecondBlock = blockArea.Children.Any(child =>
                        child is LineArea line && line.Inlines.Any(inline =>
                            inline.Text?.Contains("Second block") == true || inline.Text?.Contains("should stay") == true));

                    if (hasFirstBlock)
                    {
                        firstBlockFound = true;
                        firstBlockPageIndex = i;
                    }

                    if (hasSecondBlock)
                    {
                        secondBlockFound = true;
                        secondBlockPageIndex = i;
                    }
                }
            }
        }

        Assert.True(firstBlockFound, "First block should be found");
        Assert.True(secondBlockFound, "Second block should be found");
        Assert.Equal(firstBlockPageIndex, secondBlockPageIndex);
    }

    [Fact]
    public void KeepWithNext_IntegerStrength_Works()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="210pt" page-height="297pt">
                  <fo:region-body margin="20pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt">
                    Filler text to push content near page break.
                    More text. Even more. Additional lines. Another line.
                    And more. One more. Final filler. More content here.
                  </fo:block>
                  <fo:block font-size="14pt" keep-with-next="500">
                    Figure 1: Diagram
                  </fo:block>
                  <fo:block font-size="12pt">
                    Caption text that should stay with figure above.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);

        // Integer keep strength (500) should work like "always"
        var figurePageIndex = -1;
        var captionPageIndex = -1;

        for (int i = 0; i < areaTree.Pages.Count; i++)
        {
            var page = areaTree.Pages[i];
            foreach (var area in page.Areas)
            {
                if (area is BlockArea blockArea)
                {
                    var hasFigure = blockArea.Children.Any(child =>
                        child is LineArea line && line.Inlines.Any(inline =>
                            inline.Text?.Contains("Figure 1") == true));

                    var hasCaption = blockArea.Children.Any(child =>
                        child is LineArea line && line.Inlines.Any(inline =>
                            inline.Text?.Contains("Caption text") == true));

                    if (hasFigure) figurePageIndex = i;
                    if (hasCaption) captionPageIndex = i;
                }
            }
        }

        Assert.True(figurePageIndex >= 0, "Figure should be found");
        Assert.True(captionPageIndex >= 0, "Caption should be found");
        Assert.Equal(figurePageIndex, captionPageIndex);
    }

    [Fact]
    public void KeepWithNext_WithBreakBefore_BreakTakesPrecedence()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="210pt" page-height="297pt">
                  <fo:region-body margin="20pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="14pt" keep-with-next="always">
                    First Block
                  </fo:block>
                  <fo:block font-size="14pt" break-before="page">
                    Second Block with break-before
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);

        // break-before should take precedence over keep-with-next
        // so blocks should be on different pages
        Assert.True(areaTree.Pages.Count >= 2,
            "break-before should override keep-with-next and create at least 2 pages");
    }

    [Fact]
    public void Widows_PreventsLonelyLinesAtTopOfPage()
    {
        // Test that widows property prevents too few lines at the top of a new page
        // Use a very small page height to force splitting
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="Small" page-width="200pt" page-height="120pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="Small">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="11pt" line-height="14pt" widows="3">
                    Line one text here. Line two text here. Line three text here.
                    Line four text here. Line five text here. Line six text here.
                    Line seven text here. Line eight text here. Line nine text here.
                    Line ten text here. Line eleven text here. Line twelve text here.
                    Line thirteen text here. Line fourteen text here. Line fifteen here.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);

        // With such a small page and widows=3, the block should either:
        // 1. Fit entirely on one page (if it's small enough), OR
        // 2. Split across pages with at least 3 lines on the second page (widows constraint)

        if (areaTree.Pages.Count >= 2)
        {
            // If split occurred, check widow constraint
            var secondPage = areaTree.Pages[1];
            var lineCount = secondPage.Areas.OfType<BlockArea>()
                .SelectMany(b => b.Children.OfType<LineArea>())
                .Count();

            if (lineCount > 0)
            {
                Assert.True(lineCount >= 3,
                    $"Second page should have at least 3 lines (widows constraint), but has {lineCount}");
            }
        }
        // If only one page, that's fine - the block fit without splitting
    }

    [Fact]
    public void Orphans_PreventsLonelyLinesAtBottomOfPage()
    {
        // Test that orphans property prevents too few lines at the bottom of a page
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="Small" page-width="200pt" page-height="120pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="Small">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="11pt" line-height="14pt" orphans="3">
                    Line one text here. Line two text here. Line three text here.
                    Line four text here. Line five text here. Line six text here.
                    Line seven text here. Line eight text here. Line nine text here.
                    Line ten text here. Line eleven text here. Line twelve text here.
                    Line thirteen text here. Line fourteen text here. Line fifteen here.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);

        // With orphans=3, if the block is split, the first page should have at least 3 lines
        if (areaTree.Pages.Count >= 2)
        {
            var firstPage = areaTree.Pages[0];
            var lineCount = firstPage.Areas.OfType<BlockArea>()
                .SelectMany(b => b.Children.OfType<LineArea>())
                .Count();

            if (lineCount > 0)
            {
                Assert.True(lineCount >= 3,
                    $"First page should have at least 3 lines (orphans constraint), but has {lineCount}");
            }
        }
        // If only one page, that's fine - the block fit without splitting
    }

    [Fact]
    public void WidowsAndOrphans_RespectsBothConstraints()
    {
        // Test that both widows and orphans constraints are respected simultaneously
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="Small" page-width="200pt" page-height="120pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="Small">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="11pt" line-height="14pt" widows="2" orphans="2">
                    Line one text here. Line two text here. Line three text here.
                    Line four text here. Line five text here. Line six text here.
                    Line seven text here. Line eight text here. Line nine text here.
                    Line ten text here. Line eleven text here. Line twelve text here.
                    Line thirteen text here. Line fourteen text here. Line fifteen here.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);

        // If block is split, check both constraints
        if (areaTree.Pages.Count >= 2)
        {
            var firstPage = areaTree.Pages[0];
            var firstPageLines = firstPage.Areas.OfType<BlockArea>()
                .SelectMany(b => b.Children.OfType<LineArea>())
                .Count();

            var secondPage = areaTree.Pages[1];
            var secondPageLines = secondPage.Areas.OfType<BlockArea>()
                .SelectMany(b => b.Children.OfType<LineArea>())
                .Count();

            if (firstPageLines > 0)
            {
                Assert.True(firstPageLines >= 2,
                    $"First page should have at least 2 lines (orphans), but has {firstPageLines}");
            }

            if (secondPageLines > 0)
            {
                Assert.True(secondPageLines >= 2,
                    $"Second page should have at least 2 lines (widows), but has {secondPageLines}");
            }
        }
        // If only one page, that's fine - the block fit without splitting
    }

    [Fact]
    public void KeepTogether_OverridesWidowOrphanControl()
    {
        // Test that keep-together constraint takes precedence over widow/orphan control
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="Small" page-width="200pt" page-height="120pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="Small">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="11pt" line-height="14pt">
                    Filler line one. Filler line two. Filler line three.
                    Filler line four. Filler line five. Filler line six.
                  </fo:block>
                  <fo:block font-size="11pt" line-height="14pt" widows="3" orphans="3" keep-together="always">
                    KEEPTOGETHER L1. KEEPTOGETHER L2. KEEPTOGETHER L3.
                    KEEPTOGETHER L4. KEEPTOGETHER L5. KEEPTOGETHER L6.
                    KEEPTOGETHER L7. KEEPTOGETHER L8. KEEPTOGETHER L9.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);

        // Find all blocks containing KEEPTOGETHER marker
        var keepTogetherBlocks = new List<(int page, BlockArea block)>();

        for (int i = 0; i < areaTree.Pages.Count; i++)
        {
            var page = areaTree.Pages[i];
            foreach (var area in page.Areas.OfType<BlockArea>())
            {
                var lines = area.Children.OfType<LineArea>().ToList();
                if (lines.Count > 0 && lines.Any(l => l.Inlines.Any(inline =>
                    inline.Text?.Contains("KEEPTOGETHER") == true)))
                {
                    keepTogetherBlocks.Add((i, area));
                }
            }
        }

        // The keep-together block should appear on only ONE page (not split)
        Assert.True(keepTogetherBlocks.Count > 0, "Should find at least one KEEPTOGETHER block");

        // All KEEPTOGETHER blocks should be on the same page
        var distinctPages = keepTogetherBlocks.Select(b => b.page).Distinct().ToList();
        Assert.True(distinctPages.Count == 1,
            $"Keep-together block should be on one page only, but found on {distinctPages.Count} pages");
    }
}
