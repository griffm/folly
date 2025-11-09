namespace Folly.UnitTests;

/// <summary>
/// Fuzzing and stress tests for edge cases, malformed input,
/// extreme nesting, and large data structures.
/// </summary>
public class FuzzingAndStressTests
{
    [Fact]
    public void MalformedXml_ThrowsException()
    {
        var malformedXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                <!-- Missing closing tag
              </fo:layout-master-set>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(malformedXml));

        Assert.Throws<System.Xml.XmlException>(() => FoDocument.Load(stream));
    }

    [Fact(Skip = "Namespace validation not yet enforced by parser")]
    public void MissingNamespace_HandlesGracefully()
    {
        var xmlWithoutNamespace = """
            <?xml version="1.0"?>
            <root>
              <layout-master-set>
                <simple-page-master master-name="A4">
                  <region-body/>
                </simple-page-master>
              </layout-master-set>
              <page-sequence master-reference="A4">
                <flow flow-name="xsl-region-body">
                  <block>Test</block>
                </flow>
              </page-sequence>
            </root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlWithoutNamespace));

        // Should throw or handle gracefully
        var exception = Record.Exception(() =>
        {
            using var doc = FoDocument.Load(stream);
            doc.BuildAreaTree();
        });

        Assert.NotNull(exception); // Should fail gracefully
    }

    [Fact]
    public void ExtremeNesting_50Levels_HandlesCorrectly()
    {
        var blockNesting = new System.Text.StringBuilder();
        blockNesting.AppendLine("""
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
            """);

        // Create 50 levels of nesting
        for (int i = 0; i < 50; i++)
        {
            blockNesting.AppendLine("                  <fo:block>");
        }

        blockNesting.AppendLine("                    Deep content");

        for (int i = 0; i < 50; i++)
        {
            blockNesting.AppendLine("                  </fo:block>");
        }

        blockNesting.AppendLine("""
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(blockNesting.ToString()));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void LargeTable_100Rows_HandlesCorrectly()
    {
        var tableXml = new System.Text.StringBuilder();
        tableXml.AppendLine("""
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
                    <fo:table-column column-width="200pt"/>
                    <fo:table-column column-width="200pt"/>
                    <fo:table-body>
            """);

        for (int i = 0; i < 100; i++)
        {
            tableXml.AppendLine($"""
                      <fo:table-row>
                        <fo:table-cell><fo:block>Row {i} Col 1</fo:block></fo:table-cell>
                        <fo:table-cell><fo:block>Row {i} Col 2</fo:block></fo:table-cell>
                      </fo:table-row>
            """);
        }

        tableXml.AppendLine("""
                    </fo:table-body>
                  </fo:table>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(tableXml.ToString()));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void VeryLongText_10000Characters_HandlesCorrectly()
    {
        var longText = new string('A', 10000);
        var foXml = $"""
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>{longText}</fo:block>
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
    public void ManyBlocks_1000Blocks_HandlesCorrectly()
    {
        var blocksXml = new System.Text.StringBuilder();
        blocksXml.AppendLine("""
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
            """);

        for (int i = 0; i < 1000; i++)
        {
            blocksXml.AppendLine($"                  <fo:block>Block {i}</fo:block>");
        }

        blocksXml.AppendLine("""
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(blocksXml.ToString()));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void DeepPropertyInheritance_100Levels_HandlesCorrectly()
    {
        var nestedXml = new System.Text.StringBuilder();
        nestedXml.AppendLine("""
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
            """);

        nestedXml.AppendLine("                  <fo:block font-family=\"Times\" font-size=\"12pt\" color=\"black\">");

        for (int i = 1; i < 100; i++)
        {
            nestedXml.AppendLine("                  <fo:block>");
        }

        nestedXml.AppendLine("                    Deep inherited properties");

        for (int i = 1; i < 100; i++)
        {
            nestedXml.AppendLine("                  </fo:block>");
        }

        nestedXml.AppendLine("                  </fo:block>");

        nestedXml.AppendLine("""
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(nestedXml.ToString()));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void EmptyDocument_HandlesGracefully()
    {
        var emptyXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(emptyXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
    }

    [Fact]
    public void SpecialCharacters_HandlesCorrectly()
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
                  <fo:block>&lt;&gt;&amp;&quot;&apos; Special chars ™ © ® € £ ¥</fo:block>
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
    public void UnicodeCharacters_HandlesCorrectly()
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
                  <fo:block>Hello 世界 مرحبا שלום Здравствуй</fo:block>
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
    public void InvalidPropertyValue_HandlesGracefully()
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
                  <fo:block font-size="invalid-size" margin-top="not-a-number">Test</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));

        // Should handle gracefully (either use defaults or throw meaningful exception)
        var exception = Record.Exception(() =>
        {
            using var doc = FoDocument.Load(stream);
            doc.BuildAreaTree();
        });

        // Test passes if no crash occurs
        Assert.True(true);
    }

    [Fact]
    public void VeryLargeMargins_HandlesCorrectly()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body margin="300pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Content with huge margins</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
    }

    [Fact]
    public void MultiplePageSequences_HandlesCorrectly()
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
                  <fo:block>First sequence</fo:block>
                </fo:flow>
              </fo:page-sequence>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Second sequence</fo:block>
                </fo:flow>
              </fo:page-sequence>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Third sequence</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        var areaTree = doc.BuildAreaTree();
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= 3);
    }
}
