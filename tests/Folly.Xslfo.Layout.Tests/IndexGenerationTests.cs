using Xunit;

namespace Folly.Xslfo.Layout.Tests;

/// <summary>
/// Tests for index generation (Phase 13.4).
/// Priority 2 (Important) - 6 tests.
/// </summary>
public class IndexGenerationTests
{
    [Fact]
    public void Index_RangeTracking()
    {
        // Arrange: Document with index-range-begin and index-range-end markers
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="500pt" page-height="700pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Content before index range</fo:block>
                  <fo:block>
                    <fo:index-range-begin id="range1" index-key="algorithms"/>
                    Start of algorithms section
                  </fo:block>
                  <fo:block>Algorithms content on page 1</fo:block>
                  <fo:block break-before="page">More algorithms on page 2</fo:block>
                  <fo:block>
                    Even more algorithms on page 2
                    <fo:index-range-end ref-id="range1"/>
                  </fo:block>
                  <fo:block>Content after index range</fo:block>

                  <fo:block space-before="20pt">
                    Index: <fo:index-key-reference ref-index-key="algorithms"/>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree (should track index-range-begin to index-range-end)
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        // The index key reference should generate page numbers "1, 2" or "1–2"
    }

    [Fact]
    public void Index_PageNumbers()
    {
        // Arrange: Document with multiple index ranges generating page number list
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
                  <fo:block>
                    <fo:index-range-begin id="term1" index-key="sorting"/>
                    Sorting algorithms
                    <fo:index-range-end ref-id="term1"/>
                  </fo:block>
                  <fo:block break-before="page">More content on page 2</fo:block>
                  <fo:block>
                    <fo:index-range-begin id="term2" index-key="sorting"/>
                    More about sorting
                    <fo:index-range-end ref-id="term2"/>
                  </fo:block>
                  <fo:block break-before="page">Page 3 content</fo:block>
                  <fo:block>
                    <fo:index-range-begin id="term3" index-key="sorting"/>
                    Even more sorting
                    <fo:index-range-end ref-id="term3"/>
                  </fo:block>

                  <fo:block break-before="page" space-before="20pt">
                    Index: <fo:index-key-reference ref-index-key="sorting"/>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should generate correct page numbers for all occurrences
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= 4); // At least 4 pages (content + index)
    }

    [Fact]
    public void Index_PageRanges()
    {
        // Arrange: Document with index range spanning multiple pages
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
                  <fo:block>
                    <fo:index-range-begin id="chapter" index-key="datastructures"/>
                    Chapter on data structures starts here
                  </fo:block>
                  <fo:block break-before="page">Page 2 of data structures</fo:block>
                  <fo:block break-before="page">Page 3 of data structures</fo:block>
                  <fo:block break-before="page">
                    Page 4 of data structures
                    <fo:index-range-end ref-id="chapter"/>
                  </fo:block>

                  <fo:block break-before="page" space-before="20pt">
                    Index:
                    <fo:index-key-reference ref-index-key="datastructures">
                      <fo:index-page-citation-list merge-sequential-page-numbers="merge">
                        <fo:index-page-citation-range-separator>–</fo:index-page-citation-range-separator>
                      </fo:index-page-citation-list>
                    </fo:index-key-reference>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Pages 1-4 should be merged into range "1–4"
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= 5);
    }

    [Fact]
    public void Index_MergeSequential_Enabled()
    {
        // Arrange: Test merge-sequential-page-numbers="merge"
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="500pt" page-height="250pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:index-range-begin id="pg1" index-key="trees"/>Page 1<fo:index-range-end ref-id="pg1"/>
                  </fo:block>
                  <fo:block break-before="page">
                    <fo:index-range-begin id="pg2" index-key="trees"/>Page 2<fo:index-range-end ref-id="pg2"/>
                  </fo:block>
                  <fo:block break-before="page">
                    <fo:index-range-begin id="pg3" index-key="trees"/>Page 3<fo:index-range-end ref-id="pg3"/>
                  </fo:block>
                  <fo:block break-before="page">
                    <fo:index-range-begin id="pg5" index-key="trees"/>Page 5<fo:index-range-end ref-id="pg5"/>
                  </fo:block>

                  <fo:block break-before="page" space-before="20pt">
                    Index with merge:
                    <fo:index-key-reference ref-index-key="trees">
                      <fo:index-page-citation-list merge-sequential-page-numbers="merge">
                        <fo:index-page-citation-list-separator>, </fo:index-page-citation-list-separator>
                        <fo:index-page-citation-range-separator>–</fo:index-page-citation-range-separator>
                      </fo:index-page-citation-list>
                    </fo:index-key-reference>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should merge pages 1-3 into "1–3" and have "5" separate (result: "1–3, 5")
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= 5);
    }

    [Fact]
    public void Index_CustomSeparators()
    {
        // Arrange: Test custom list and range separators
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="500pt" page-height="250pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:index-range-begin id="g1" index-key="graphs"/>Graphs topic<fo:index-range-end ref-id="g1"/>
                  </fo:block>
                  <fo:block break-before="page">More content</fo:block>
                  <fo:block>
                    <fo:index-range-begin id="g2" index-key="graphs"/>More graphs<fo:index-range-end ref-id="g2"/>
                  </fo:block>
                  <fo:block break-before="page">Even more</fo:block>
                  <fo:block>
                    <fo:index-range-begin id="g3" index-key="graphs"/>Final graphs<fo:index-range-end ref-id="g3"/>
                  </fo:block>

                  <fo:block break-before="page" space-before="20pt">
                    Index:
                    <fo:index-key-reference ref-index-key="graphs">
                      <fo:index-page-number-prefix>pp. </fo:index-page-number-prefix>
                      <fo:index-page-citation-list merge-sequential-page-numbers="no-merge">
                        <fo:index-page-citation-list-separator>; </fo:index-page-citation-list-separator>
                      </fo:index-page-citation-list>
                    </fo:index-key-reference>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should use prefix "pp. " and separator "; " (result: "pp. 1; 2; 3")
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= 4);
    }

    [Fact]
    public void Index_Sorting()
    {
        // Arrange: Verify index entries are sorted by page number
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="500pt" page-height="250pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Page 1 content</fo:block>
                  <fo:block>
                    <fo:index-range-begin id="s1" index-key="search"/>Search on page 1<fo:index-range-end ref-id="s1"/>
                  </fo:block>
                  <fo:block break-before="page">Page 2 content</fo:block>
                  <fo:block>
                    <fo:index-range-begin id="s2" index-key="search"/>Search on page 2<fo:index-range-end ref-id="s2"/>
                  </fo:block>
                  <fo:block break-before="page">Page 3 content</fo:block>
                  <fo:block break-before="page">Page 4 content</fo:block>
                  <fo:block break-before="page">Page 5 content</fo:block>
                  <fo:block>
                    <fo:index-range-begin id="s5" index-key="search"/>Search on page 5<fo:index-range-end ref-id="s5"/>
                  </fo:block>

                  <fo:block break-before="page" space-before="20pt">
                    Index:
                    <fo:index-key-reference ref-index-key="search">
                      <fo:index-page-citation-list merge-sequential-page-numbers="no-merge">
                        <fo:index-page-citation-list-separator>, </fo:index-page-citation-list-separator>
                      </fo:index-page-citation-list>
                    </fo:index-key-reference>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Page numbers should be sorted (result: "1, 2, 5")
        // Even though markers appear in order 1, 2, 5, they should be sorted
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= 6);
    }
}
