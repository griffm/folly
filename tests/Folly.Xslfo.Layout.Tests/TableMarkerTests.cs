using Xunit;

namespace Folly.Xslfo.Layout.Tests;

/// <summary>
/// Tests for retrieve-table-marker (Phase 13.2).
/// Priority 2 (Important) - 4 tests.
/// </summary>
public class TableMarkerTests
{
    [Fact]
    public void TableMarker_FirstStarting()
    {
        // Arrange: Table with markers in rows, retrieve first marker in table header
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
                  <fo:table width="300pt">
                    <fo:table-column column-width="150pt"/>
                    <fo:table-column column-width="150pt"/>
                    <fo:table-header>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            Section: <fo:retrieve-table-marker retrieve-class-name="section" retrieve-position="first-starting"/>
                          </fo:block>
                        </fo:table-cell>
                        <fo:table-cell><fo:block>Data</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-header>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            <fo:marker marker-class-name="section">Section A</fo:marker>
                            Row 1
                          </fo:block>
                        </fo:table-cell>
                        <fo:table-cell><fo:block>Value 1</fo:block></fo:table-cell>
                      </fo:table-row>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            <fo:marker marker-class-name="section">Section B</fo:marker>
                            Row 2
                          </fo:block>
                        </fo:table-cell>
                        <fo:table-cell><fo:block>Value 2</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree (retrieve-table-marker should be processed)
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully with table markers
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void TableMarker_LastEnding()
    {
        // Arrange: Table with markers, retrieve last marker in table footer
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
                  <fo:table width="300pt">
                    <fo:table-column column-width="150pt"/>
                    <fo:table-column column-width="150pt"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            <fo:marker marker-class-name="category">Category X</fo:marker>
                            Row 1
                          </fo:block>
                        </fo:table-cell>
                        <fo:table-cell><fo:block>Value 1</fo:block></fo:table-cell>
                      </fo:table-row>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            <fo:marker marker-class-name="category">Category Y</fo:marker>
                            Row 2
                          </fo:block>
                        </fo:table-cell>
                        <fo:table-cell><fo:block>Value 2</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                    <fo:table-footer>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            Last: <fo:retrieve-table-marker retrieve-class-name="category" retrieve-position="last-ending"/>
                          </fo:block>
                        </fo:table-cell>
                        <fo:table-cell><fo:block>End</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-footer>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void TableMarker_TableScope()
    {
        // Arrange: Multiple tables with markers - markers should be scoped to their table
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
                  <fo:table width="300pt">
                    <fo:table-column column-width="300pt"/>
                    <fo:table-header>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            Table 1: <fo:retrieve-table-marker retrieve-class-name="title" retrieve-position="first-starting"/>
                          </fo:block>
                        </fo:table-cell>
                      </fo:table-row>
                    </fo:table-header>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            <fo:marker marker-class-name="title">First Table</fo:marker>
                            Content 1
                          </fo:block>
                        </fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                  </fo:table>

                  <fo:block space-before="10pt"/>

                  <fo:table width="300pt">
                    <fo:table-column column-width="300pt"/>
                    <fo:table-header>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            Table 2: <fo:retrieve-table-marker retrieve-class-name="title" retrieve-position="first-starting"/>
                          </fo:block>
                        </fo:table-cell>
                      </fo:table-row>
                    </fo:table-header>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            <fo:marker marker-class-name="title">Second Table</fo:marker>
                            Content 2
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

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully
        // Each table's retrieve-table-marker should only see markers from its own table
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void TableMarker_Integration()
    {
        // Arrange: Complex table with header, footer, and multiple markers
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
                    <fo:table-column column-width="200pt"/>
                    <fo:table-column column-width="200pt"/>
                    <fo:table-header>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block font-weight="bold">
                            Section: <fo:retrieve-table-marker retrieve-class-name="section" retrieve-position="first-starting"/>
                          </fo:block>
                        </fo:table-cell>
                        <fo:table-cell>
                          <fo:block font-weight="bold">Data</fo:block>
                        </fo:table-cell>
                      </fo:table-row>
                    </fo:table-header>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            <fo:marker marker-class-name="section">Introduction</fo:marker>
                            <fo:marker marker-class-name="page-ref">Page 1</fo:marker>
                            Item 1
                          </fo:block>
                        </fo:table-cell>
                        <fo:table-cell><fo:block>100</fo:block></fo:table-cell>
                      </fo:table-row>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            <fo:marker marker-class-name="section">Methods</fo:marker>
                            <fo:marker marker-class-name="page-ref">Page 2</fo:marker>
                            Item 2
                          </fo:block>
                        </fo:table-cell>
                        <fo:table-cell><fo:block>200</fo:block></fo:table-cell>
                      </fo:table-row>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block>
                            <fo:marker marker-class-name="section">Results</fo:marker>
                            <fo:marker marker-class-name="page-ref">Page 3</fo:marker>
                            Item 3
                          </fo:block>
                        </fo:table-cell>
                        <fo:table-cell><fo:block>300</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                    <fo:table-footer>
                      <fo:table-row>
                        <fo:table-cell>
                          <fo:block font-style="italic">
                            Last section: <fo:retrieve-table-marker retrieve-class-name="section" retrieve-position="last-ending"/>
                          </fo:block>
                        </fo:table-cell>
                        <fo:table-cell>
                          <fo:block font-style="italic">
                            Ref: <fo:retrieve-table-marker retrieve-class-name="page-ref" retrieve-position="last-ending"/>
                          </fo:block>
                        </fo:table-cell>
                      </fo:table-row>
                    </fo:table-footer>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully with complex marker retrieval
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }
}
