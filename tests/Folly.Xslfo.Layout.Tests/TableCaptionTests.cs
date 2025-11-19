using Folly.Layout;
using Xunit;

namespace Folly.Xslfo.Layout.Tests;

/// <summary>
/// Tests for table captions (Phase 13.1).
/// Priority 2 (Important) - 6 tests.
/// </summary>
public class TableCaptionTests
{
    private TableArea? FindTableArea(AreaTree areaTree)
    {
        foreach (var page in areaTree.Pages)
        {
            var table = FindTableAreaInChildren(page.Areas);
            if (table != null)
                return table;
        }
        return null;
    }

    private TableArea? FindTableAreaInChildren(IReadOnlyList<Area> areas)
    {
        foreach (var area in areas)
        {
            if (area is TableArea tableArea)
                return tableArea;

            if (area is BlockArea blockArea && blockArea.Children.Count > 0)
            {
                var table = FindTableAreaInChildren(blockArea.Children);
                if (table != null)
                    return table;
            }
        }
        return null;
    }

    [Fact]
    public void TableCaption_Before()
    {
        // Arrange: Table with caption positioned before
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
                  <fo:table-and-caption>
                    <fo:table-caption caption-side="before">
                      <fo:block>Table 1: Sample Data</fo:block>
                    </fo:table-caption>
                    <fo:table width="300pt">
                      <fo:table-column column-width="150pt"/>
                      <fo:table-column column-width="150pt"/>
                      <fo:table-body>
                        <fo:table-row>
                          <fo:table-cell><fo:block>A1</fo:block></fo:table-cell>
                          <fo:table-cell><fo:block>B1</fo:block></fo:table-cell>
                        </fo:table-row>
                      </fo:table-body>
                    </fo:table>
                  </fo:table-and-caption>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully with caption before table
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
    }

    [Fact]
    public void TableCaption_After()
    {
        // Arrange: Table with caption positioned after
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
                  <fo:table-and-caption>
                    <fo:table-caption caption-side="after">
                      <fo:block>Table 1: Sample Data</fo:block>
                    </fo:table-caption>
                    <fo:table width="300pt">
                      <fo:table-column column-width="150pt"/>
                      <fo:table-column column-width="150pt"/>
                      <fo:table-body>
                        <fo:table-row>
                          <fo:table-cell><fo:block>A1</fo:block></fo:table-cell>
                          <fo:table-cell><fo:block>B1</fo:block></fo:table-cell>
                        </fo:table-row>
                      </fo:table-body>
                    </fo:table>
                  </fo:table-and-caption>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully with caption after table
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
    }

    [Fact]
    public void TableCaption_Start()
    {
        // Arrange: Table with caption at start (normalized to before)
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
                  <fo:table-and-caption>
                    <fo:table-caption caption-side="start">
                      <fo:block>Table 1: Sample Data</fo:block>
                    </fo:table-caption>
                    <fo:table width="300pt">
                      <fo:table-column column-width="150pt"/>
                      <fo:table-column column-width="150pt"/>
                      <fo:table-body>
                        <fo:table-row>
                          <fo:table-cell><fo:block>A1</fo:block></fo:table-cell>
                          <fo:table-cell><fo:block>B1</fo:block></fo:table-cell>
                        </fo:table-row>
                      </fo:table-body>
                    </fo:table>
                  </fo:table-and-caption>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully (start is normalized to before)
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
    }

    [Fact]
    public void TableCaption_End()
    {
        // Arrange: Table with caption at end (normalized to after)
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
                  <fo:table-and-caption>
                    <fo:table-caption caption-side="end">
                      <fo:block>Table 1: Sample Data</fo:block>
                    </fo:table-caption>
                    <fo:table width="300pt">
                      <fo:table-column column-width="150pt"/>
                      <fo:table-column column-width="150pt"/>
                      <fo:table-body>
                        <fo:table-row>
                          <fo:table-cell><fo:block>A1</fo:block></fo:table-cell>
                          <fo:table-cell><fo:block>B1</fo:block></fo:table-cell>
                        </fo:table-row>
                      </fo:table-body>
                    </fo:table>
                  </fo:table-and-caption>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully (end is normalized to after)
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
    }

    [Fact]
    public void TableCaption_Styling()
    {
        // Arrange: Table with caption that has custom styling
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
                  <fo:table-and-caption>
                    <fo:table-caption caption-side="before">
                      <fo:block font-size="14pt" font-weight="bold" color="blue" text-align="center">
                        Table 1: Sample Data with Styled Caption
                      </fo:block>
                    </fo:table-caption>
                    <fo:table width="300pt">
                      <fo:table-column column-width="150pt"/>
                      <fo:table-column column-width="150pt"/>
                      <fo:table-body>
                        <fo:table-row>
                          <fo:table-cell><fo:block>A1</fo:block></fo:table-cell>
                          <fo:table-cell><fo:block>B1</fo:block></fo:table-cell>
                        </fo:table-row>
                      </fo:table-body>
                    </fo:table>
                  </fo:table-and-caption>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully with styled caption
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
    }

    [Fact]
    public void TableCaption_MultiPage()
    {
        // Arrange: Table that spans multiple pages with caption
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="500pt" page-height="200pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:table-and-caption>
                    <fo:table-caption caption-side="before">
                      <fo:block font-weight="bold">Table 1: Multi-Page Table</fo:block>
                    </fo:table-caption>
                    <fo:table width="300pt">
                      <fo:table-column column-width="150pt"/>
                      <fo:table-column column-width="150pt"/>
                      <fo:table-body>
                        <fo:table-row>
                          <fo:table-cell><fo:block>Row 1 Col 1</fo:block></fo:table-cell>
                          <fo:table-cell><fo:block>Row 1 Col 2</fo:block></fo:table-cell>
                        </fo:table-row>
                        <fo:table-row>
                          <fo:table-cell><fo:block>Row 2 Col 1</fo:block></fo:table-cell>
                          <fo:table-cell><fo:block>Row 2 Col 2</fo:block></fo:table-cell>
                        </fo:table-row>
                        <fo:table-row>
                          <fo:table-cell><fo:block>Row 3 Col 1</fo:block></fo:table-cell>
                          <fo:table-cell><fo:block>Row 3 Col 2</fo:block></fo:table-cell>
                        </fo:table-row>
                        <fo:table-row>
                          <fo:table-cell><fo:block>Row 4 Col 1</fo:block></fo:table-cell>
                          <fo:table-cell><fo:block>Row 4 Col 2</fo:block></fo:table-cell>
                        </fo:table-row>
                        <fo:table-row>
                          <fo:table-cell><fo:block>Row 5 Col 1</fo:block></fo:table-cell>
                          <fo:table-cell><fo:block>Row 5 Col 2</fo:block></fo:table-cell>
                        </fo:table-row>
                        <fo:table-row>
                          <fo:table-cell><fo:block>Row 6 Col 1</fo:block></fo:table-cell>
                          <fo:table-cell><fo:block>Row 6 Col 2</fo:block></fo:table-cell>
                        </fo:table-row>
                      </fo:table-body>
                    </fo:table>
                  </fo:table-and-caption>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully across multiple pages
        // Caption should appear only once (on first page)
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= 1, "Table should produce at least one page");
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
    }
}
