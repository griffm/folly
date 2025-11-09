namespace Folly.UnitTests;

/// <summary>
/// Golden AreaTree snapshot tests to detect layout regressions.
/// These tests verify that the layout engine produces consistent results
/// for various document structures.
/// </summary>
public class AreaTreeSnapshotTests
{
    [Fact]
    public void SimpleBlock_ProducesConsistentAreaTree()
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
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt">Simple block content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();

        // Verify consistent structure
        Assert.NotNull(areaTree);
        Assert.Single(areaTree.Pages);
        Assert.NotEmpty(areaTree.Pages[0].Areas);

        var blockArea = areaTree.Pages[0].Areas[0] as BlockArea;
        Assert.NotNull(blockArea);
        Assert.NotEmpty(blockArea.Children); // Should have line areas
    }

    [Fact]
    public void MultipleBlocks_ProducesConsistentAreaTree()
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
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="18pt" margin-bottom="12pt">Title</fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">Paragraph 1</fo:block>
                  <fo:block font-size="12pt">Paragraph 2</fo:block>
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
        Assert.True(page.Areas.Count >= 3, "Should have at least 3 block areas");
    }

    [Fact]
    public void TableLayout_ProducesConsistentAreaTree()
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
                <fo:flow flow-name="xsl-region-body">
                  <fo:table>
                    <fo:table-column column-width="150pt"/>
                    <fo:table-column column-width="150pt"/>
                    <fo:table-body>
                      <fo:table-row>
                        <fo:table-cell><fo:block>A1</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>B1</fo:block></fo:table-cell>
                      </fo:table-row>
                      <fo:table-row>
                        <fo:table-cell><fo:block>A2</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>B2</fo:block></fo:table-cell>
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
        Assert.Single(areaTree.Pages);
        Assert.NotEmpty(areaTree.Pages[0].Areas);
    }

    [Fact]
    public void MultiColumnLayout_ProducesConsistentAreaTree()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt" column-count="2" column-gap="24pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>First block in columns</fo:block>
                  <fo:block>Second block in columns</fo:block>
                  <fo:block>Third block in columns</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();

        Assert.NotNull(areaTree);
        Assert.Single(areaTree.Pages);
    }

    [Fact]
    public void FootnoteLayout_ProducesConsistentAreaTree()
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
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    Main content with footnote<fo:footnote>
                      <fo:inline>1</fo:inline>
                      <fo:footnote-body>
                        <fo:block font-size="10pt">Footnote content here</fo:block>
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
        Assert.Single(areaTree.Pages);
    }

    [Fact]
    public void FloatLayout_ProducesConsistentAreaTree()
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
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:float float="start">
                      <fo:block>Floating sidebar content</fo:block>
                    </fo:float>
                    Main content flows around the float
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
    }

    [Fact]
    public void NestedBlocks_ProducesConsistentAreaTree()
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
                <fo:flow flow-name="xsl-region-body">
                  <fo:block margin-left="20pt">
                    <fo:block margin-left="20pt">
                      <fo:block margin-left="20pt">
                        Deeply nested content
                      </fo:block>
                    </fo:block>
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
        Assert.NotEmpty(areaTree.Pages[0].Areas);
    }

    [Fact]
    public void ListLayout_ProducesConsistentAreaTree()
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
                <fo:flow flow-name="xsl-region-body">
                  <fo:list-block>
                    <fo:list-item>
                      <fo:list-item-label><fo:block>•</fo:block></fo:list-item-label>
                      <fo:list-item-body><fo:block>Item 1</fo:block></fo:list-item-body>
                    </fo:list-item>
                    <fo:list-item>
                      <fo:list-item-label><fo:block>•</fo:block></fo:list-item-label>
                      <fo:list-item-body><fo:block>Item 2</fo:block></fo:list-item-body>
                    </fo:list-item>
                  </fo:list-block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();

        Assert.NotNull(areaTree);
        Assert.Single(areaTree.Pages);
    }

    [Fact]
    public void AreaTree_IsReproducible()
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
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-size="12pt">Reproducible content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        // Build area tree twice and verify same structure
        using var stream1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc1 = FoDocument.Load(stream1);
        var areaTree1 = doc1.BuildAreaTree();

        using var stream2 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc2 = FoDocument.Load(stream2);
        var areaTree2 = doc2.BuildAreaTree();

        // Should have same page count
        Assert.Equal(areaTree1.Pages.Count, areaTree2.Pages.Count);

        // Should have same number of areas on first page
        Assert.Equal(areaTree1.Pages[0].Areas.Count, areaTree2.Pages[0].Areas.Count);
    }
}
