namespace Folly.SpecTests;

using Folly.Dom;

/// <summary>
/// Conformance tests for XSL-FO 1.1 specification compliance.
/// Tests each formatting object and ensures proper parsing and structure.
/// </summary>
public class ConformanceTests
{
    [Fact]
    public void FoBlock_ParsesCorrectly()
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
                  <fo:block font-size="14pt" margin-top="10pt">Test Block</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        Assert.NotNull(block);
        Assert.Equal(14, block.FontSize);
        Assert.Equal(10, block.MarginTop);
    }

    [Fact]
    public void FoInline_ParsesCorrectly()
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
                    Normal text <fo:inline color="red" font-weight="bold">inline text</fo:inline> more text
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var inline = block.Children.OfType<FoInline>().FirstOrDefault();

        Assert.NotNull(inline);
        Assert.Equal("red", inline.Color);
        Assert.Equal("bold", inline.FontWeight);
    }

    [Fact]
    public void FoTable_ParsesCorrectly()
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
                        <fo:table-cell><fo:block>Cell 1</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>Cell 2</fo:block></fo:table-cell>
                      </fo:table-row>
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var table = doc.Root.PageSequences[0].Flow!.Tables.FirstOrDefault();
        Assert.NotNull(table);
        Assert.Equal(2, table.Columns.Count);
        Assert.NotNull(table.Body);
        Assert.Single(table.Body.Rows);
        Assert.Equal(2, table.Body.Rows[0].Cells.Count);
    }

    [Fact]
    public void FoListBlock_ParsesCorrectly()
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
                      <fo:list-item-label><fo:block>•</fo:block></fo:list-item-label>
                      <fo:list-item-body><fo:block>Item 1</fo:block></fo:list-item-body>
                    </fo:list-item>
                  </fo:list-block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var listBlock = doc.Root.PageSequences[0].Flow!.Lists.FirstOrDefault();
        Assert.NotNull(listBlock);
        Assert.Single(listBlock.Items);
        Assert.NotNull(listBlock.Items[0].Label);
        Assert.NotNull(listBlock.Items[0].Body);
    }

    [Fact]
    public void FoExternalGraphic_ParsesCorrectly()
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
                    <fo:external-graphic src="image.jpg" content-width="200pt" content-height="150pt"/>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var graphic = block.Children.OfType<FoExternalGraphic>().FirstOrDefault();

        Assert.NotNull(graphic);
        Assert.Equal("image.jpg", graphic.Src);
    }

    [Fact]
    public void FoBasicLink_ParsesCorrectly()
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
                    <fo:basic-link external-destination="http://example.com">Click here</fo:basic-link>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var link = block.Children.OfType<FoBasicLink>().FirstOrDefault();

        Assert.NotNull(link);
        Assert.Equal("http://example.com", link.ExternalDestination);
    }

    [Fact]
    public void FoPageNumber_ParsesCorrectly()
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
                  <fo:block>Page <fo:page-number/></fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var pageNumber = block.Children.OfType<FoPageNumber>().FirstOrDefault();

        Assert.NotNull(pageNumber);
    }

    [Fact]
    public void FoLeader_ParsesCorrectly()
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
                    Chapter 1<fo:leader leader-pattern="dots"/>42
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var leader = block.Children.OfType<FoLeader>().FirstOrDefault();

        Assert.NotNull(leader);
        Assert.Equal("dots", leader.LeaderPattern);
    }

    [Fact]
    public void FoFootnote_ParsesCorrectly()
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
                    Some text<fo:footnote>
                      <fo:inline>1</fo:inline>
                      <fo:footnote-body>
                        <fo:block>Footnote text</fo:block>
                      </fo:footnote-body>
                    </fo:footnote>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var footnote = block.Footnotes.FirstOrDefault();

        Assert.NotNull(footnote);
        Assert.NotNull(footnote.FootnoteBody);
    }

    [Fact]
    public void FoMarker_ParsesCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                  <fo:region-before extent="1in"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-before">
                  <fo:block>
                    <fo:retrieve-marker retrieve-class-name="chapter"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:marker marker-class-name="chapter">Chapter 1</fo:marker>
                    Content here
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var marker = block.Children.OfType<FoMarker>().FirstOrDefault();

        Assert.NotNull(marker);
        Assert.Equal("chapter", marker.MarkerClassName);
    }

    [Fact]
    public void FoFloat_ParsesCorrectly()
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
                    <fo:float float="start">
                      <fo:block>Floating content</fo:block>
                    </fo:float>
                    Main content wraps around the float
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var floatElement = block.Floats.FirstOrDefault();

        Assert.NotNull(floatElement);
        Assert.Equal("start", floatElement.Float);
    }

    [Fact(Skip = "FoBlockContainer as direct child of flow not yet supported by parser")]
    public void FoBlockContainer_ParsesCorrectly()
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
                  <fo:block-container reference-orientation="90">
                    <fo:block>Rotated content</fo:block>
                  </fo:block-container>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var container = doc.Root.PageSequences[0].Flow!.Children.OfType<FoBlockContainer>().FirstOrDefault();
        Assert.NotNull(container);
        Assert.Equal(90, container.ReferenceOrientation);
    }

    [Fact]
    public void FoInlineContainer_ParsesCorrectly()
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
                    Text before <fo:inline-container>
                      <fo:block>Container content</fo:block>
                    </fo:inline-container> text after
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var container = block.Children.OfType<FoInlineContainer>().FirstOrDefault();

        Assert.NotNull(container);
    }

    [Fact]
    public void FoBidiOverride_ParsesCorrectly()
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
                    <fo:bidi-override direction="rtl">Hebrew text</fo:bidi-override>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var bidi = block.Children.OfType<FoBidiOverride>().FirstOrDefault();

        Assert.NotNull(bidi);
        Assert.Equal("rtl", bidi.Direction);
    }

    [Fact]
    public void FoCharacter_ParsesCorrectly()
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
                    Text<fo:character character="&#x2022;"/>More text
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var character = block.Children.OfType<FoCharacter>().FirstOrDefault();

        Assert.NotNull(character);
        Assert.Equal("•", character.Character);
    }

    [Fact]
    public void FoWrapper_ParsesCorrectly()
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
                    <fo:wrapper color="blue" font-weight="bold">Wrapped text</fo:wrapper>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var wrapper = block.Children.OfType<FoWrapper>().FirstOrDefault();

        Assert.NotNull(wrapper);
        // FoWrapper passes properties to children, verify it exists
        Assert.Equal("wrapper", wrapper.Name);
    }

    [Fact]
    public void EmptyBlock_ParsesCorrectly()
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
                  <fo:block/>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        Assert.NotNull(block);
        Assert.Empty(block.Children);
    }

    [Fact]
    public void DeeplyNestedStructure_ParsesCorrectly()
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
                    <fo:block>
                      <fo:block>
                        <fo:block>
                          <fo:block>
                            Deep content
                          </fo:block>
                        </fo:block>
                      </fo:block>
                    </fo:block>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        Assert.NotNull(block);

        // Navigate to deepest block
        var level2 = block.Children[0] as FoBlock;
        var level3 = level2!.Children[0] as FoBlock;
        var level4 = level3!.Children[0] as FoBlock;
        var level5 = level4!.Children[0] as FoBlock;

        Assert.NotNull(level5);
    }

    [Fact]
    public void BookmarkTree_ParsesCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:bookmark-tree>
                <fo:bookmark internal-destination="ch1">
                  <fo:bookmark-title>Chapter 1</fo:bookmark-title>
                </fo:bookmark>
              </fo:bookmark-tree>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block id="ch1">Chapter 1 Content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        Assert.NotNull(doc.Root.BookmarkTree);
        Assert.Single(doc.Root.BookmarkTree.Bookmarks);
        Assert.Equal("ch1", doc.Root.BookmarkTree.Bookmarks[0].InternalDestination);
    }

    [Fact]
    public void ConditionalPageMaster_ParsesCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="first">
                  <fo:region-body/>
                </fo:simple-page-master>
                <fo:simple-page-master master-name="rest">
                  <fo:region-body/>
                </fo:simple-page-master>
                <fo:page-sequence-master master-name="pages">
                  <fo:repeatable-page-master-alternatives>
                    <fo:conditional-page-master-reference master-reference="first" page-position="first"/>
                    <fo:conditional-page-master-reference master-reference="rest" page-position="rest"/>
                  </fo:repeatable-page-master-alternatives>
                </fo:page-sequence-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="pages">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var masterSet = doc.Root.LayoutMasterSet;
        Assert.NotNull(masterSet);
        Assert.NotEmpty(masterSet.PageSequenceMasters);
    }
}
