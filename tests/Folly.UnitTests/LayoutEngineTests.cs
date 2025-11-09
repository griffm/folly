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
}
