namespace Folly.UnitTests;

using Folly.Dom;

public class PropertyInheritanceTests
{
    [Fact]
    public void FontFamily_InheritsFromParentBlock()
    {
        // Arrange
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
                  <fo:block font-family="Times">
                    <fo:inline>This should inherit Times font</fo:inline>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var inline = block.Children[0] as FoInline;

        // Assert
        Assert.NotNull(inline);
        Assert.Equal("Times", block.FontFamily);
        Assert.Equal("Times", inline.FontFamily); // Should inherit from parent block
    }

    [Fact]
    public void FontSize_InheritsFromParentBlock()
    {
        // Arrange
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
                  <fo:block font-size="18pt">
                    <fo:inline>This should inherit 18pt font size</fo:inline>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var inline = block.Children[0] as FoInline;

        // Assert
        Assert.NotNull(inline);
        Assert.Equal(18, block.FontSize);
        Assert.Equal(18, inline.FontSize); // Should inherit from parent block
    }

    [Fact]
    public void Color_InheritsFromParentBlock()
    {
        // Arrange
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
                  <fo:block color="red">
                    <fo:inline>This should inherit red color</fo:inline>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var inline = block.Children[0] as FoInline;

        // Assert
        Assert.NotNull(inline);
        Assert.Equal("red", inline.Color); // Should inherit from parent block
    }

    [Fact]
    public void NestedInheritance_InheritsAcrossMultipleLevels()
    {
        // Arrange
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
                  <fo:block font-family="Courier" font-size="14pt" color="blue">
                    <fo:block>
                      <fo:inline>Nested inheritance test</fo:inline>
                    </fo:block>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var outerBlock = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var innerBlock = outerBlock.Children[0] as FoBlock;
        var inline = innerBlock!.Children[0] as FoInline;

        // Assert
        Assert.NotNull(innerBlock);
        Assert.NotNull(inline);

        // Inner block should inherit from outer block
        Assert.Equal("Courier", innerBlock.FontFamily);
        Assert.Equal(14, innerBlock.FontSize);

        // Inline should inherit from inner block (which inherited from outer)
        Assert.Equal("Courier", inline.FontFamily);
        Assert.Equal(14, inline.FontSize);
        Assert.Equal("blue", inline.Color);
    }

    [Fact]
    public void ExplicitInheritKeyword_InheritsFromParent()
    {
        // Arrange
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
                  <fo:block font-family="Helvetica" font-size="16pt">
                    <fo:inline font-family="inherit" font-size="inherit">Explicit inherit</fo:inline>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var inline = block.Children[0] as FoInline;

        // Assert
        Assert.NotNull(inline);
        Assert.Equal("Helvetica", inline.FontFamily); // Explicit inherit
        Assert.Equal(16, inline.FontSize); // Explicit inherit
    }

    [Fact]
    public void ChildPropertyOverridesParent()
    {
        // Arrange
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
                  <fo:block font-family="Times" font-size="12pt">
                    <fo:inline font-family="Helvetica" font-size="14pt">Override test</fo:inline>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var inline = block.Children[0] as FoInline;

        // Assert
        Assert.NotNull(inline);
        Assert.Equal("Times", block.FontFamily);
        Assert.Equal("Helvetica", inline.FontFamily); // Override parent
        Assert.Equal(12, block.FontSize);
        Assert.Equal(14, inline.FontSize); // Override parent
    }

    [Fact]
    public void NonInheritableProperties_DoNotInherit()
    {
        // Arrange
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
                  <fo:block margin-top="20pt" padding-left="10pt" background-color="yellow">
                    <fo:block>Nested block without explicit margins</fo:block>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var outerBlock = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var innerBlock = outerBlock.Children[0] as FoBlock;

        // Assert
        Assert.NotNull(innerBlock);

        // Parent has margins and padding
        Assert.Equal(20, outerBlock.MarginTop);
        Assert.Equal(10, outerBlock.PaddingLeft);
        Assert.Equal("yellow", outerBlock.BackgroundColor);

        // Child should NOT inherit non-inheritable properties
        Assert.Equal(0, innerBlock.MarginTop); // Should be default 0, not 20
        Assert.Equal(0, innerBlock.PaddingLeft); // Should be default 0, not 10
        Assert.Equal("transparent", innerBlock.BackgroundColor); // Should be default transparent, not yellow
    }

    [Fact]
    public void TextAlign_InheritsFromParent()
    {
        // Arrange
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
                  <fo:block text-align="center">
                    <fo:block>This should inherit center alignment</fo:block>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var outerBlock = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var innerBlock = outerBlock.Children[0] as FoBlock;

        // Assert
        Assert.NotNull(innerBlock);
        Assert.Equal("center", outerBlock.TextAlign);
        Assert.Equal("center", innerBlock.TextAlign); // Should inherit
    }

    [Fact]
    public void LineHeight_InheritsFromParent()
    {
        // Arrange
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
                  <fo:block font-size="12pt" line-height="20pt">
                    <fo:block font-size="12pt">Nested block</fo:block>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var outerBlock = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var innerBlock = outerBlock.Children[0] as FoBlock;

        // Assert
        Assert.NotNull(innerBlock);
        Assert.Equal(20, outerBlock.LineHeight);
        Assert.Equal(20, innerBlock.LineHeight); // Should inherit
    }

    [Fact]
    public void FontWeight_InheritsFromParent()
    {
        // Arrange
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
                  <fo:block font-weight="bold">
                    <fo:inline>This should inherit bold weight</fo:inline>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var inline = block.Children[0] as FoInline;

        // Assert
        Assert.NotNull(inline);
        Assert.Equal("bold", inline.FontWeight); // Should inherit from parent
    }

    [Fact]
    public void FontStyle_InheritsFromParent()
    {
        // Arrange
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
                  <fo:block font-style="italic">
                    <fo:inline>This should inherit italic style</fo:inline>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var inline = block.Children[0] as FoInline;

        // Assert
        Assert.NotNull(inline);
        Assert.Equal("italic", inline.FontStyle); // Should inherit from parent
    }

    [Fact]
    public void TextDecoration_InheritsFromParent()
    {
        // Arrange
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
                  <fo:block text-decoration="underline">
                    <fo:inline>This should inherit underline</fo:inline>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var inline = block.Children[0] as FoInline;

        // Assert
        Assert.NotNull(inline);
        Assert.Equal("underline", inline.TextDecoration); // Should inherit from parent
    }

    [Fact]
    public void FontWeightAndStyle_InheritFromParent()
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
                  <fo:block font-weight="bold" font-style="italic">
                    <fo:inline>Bold italic text</fo:inline>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var inline = block.Children[0] as FoInline;

        Assert.NotNull(inline);
        Assert.Equal("bold", inline.FontWeight);
        Assert.Equal("italic", inline.FontStyle);
    }

    [Fact]
    public void MultiplePropertiesAtOnce_InheritCorrectly()
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
                  <fo:block font-family="Courier" font-size="14pt" color="red">
                    <fo:inline>Inherited properties</fo:inline>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var block = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var inline = block.Children[0] as FoInline;

        Assert.NotNull(inline);
        Assert.Equal("Courier", inline.FontFamily);
        Assert.Equal(14, inline.FontSize);
        Assert.Equal("red", inline.Color);
    }

    [Fact]
    public void DeepInheritanceChain_InheritsCorrectly()
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
                  <fo:block font-family="Times" font-size="14pt" color="blue" text-align="center">
                    <fo:block>
                      <fo:block>
                        <fo:block>
                          <fo:inline>Deep inheritance</fo:inline>
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

        var level1 = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var level2 = level1.Children[0] as FoBlock;
        var level3 = level2!.Children[0] as FoBlock;
        var level4 = level3!.Children[0] as FoBlock;
        var inline = level4!.Children[0] as FoInline;

        Assert.NotNull(inline);
        Assert.Equal("Times", inline.FontFamily);
        Assert.Equal(14, inline.FontSize);
        Assert.Equal("blue", inline.Color);
    }

    [Fact]
    public void MixedInheritanceAndOverride_WorksCorrectly()
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
                  <fo:block font-family="Times" font-size="12pt" color="black">
                    <fo:block font-size="14pt">
                      <fo:inline color="red">Mixed inheritance</fo:inline>
                    </fo:block>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var outerBlock = doc.Root.PageSequences[0].Flow!.Blocks[0];
        var middleBlock = outerBlock.Children[0] as FoBlock;
        var inline = middleBlock!.Children[0] as FoInline;

        Assert.NotNull(inline);
        Assert.Equal("Times", inline.FontFamily); // Inherited from outer block
        Assert.Equal(14, inline.FontSize); // Inherited from middle block (overrode outer)
        Assert.Equal("red", inline.Color); // Overrode both parent blocks
    }
}
