namespace Folly.UnitTests;

/// <summary>
/// End-to-end tests that verify complete FO to PDF rendering.
/// </summary>
public class EndToEndTests
{
    [Fact]
    public void HelloWorld_ProducesValidPdf()
    {
        // Arrange - Create a simple "Hello World" FO document
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
                  <fo:block font-family="Helvetica" font-size="24pt" text-align="center">
                    Hello, Folly!
                  </fo:block>
                  <fo:block font-family="Times" font-size="12pt" margin-top="12pt">
                    This is a test document demonstrating the Folly XSL-FO processor.
                    It can parse XSL-FO XML, perform layout with line breaking,
                    and render beautiful PDFs with proper text positioning.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        // Act - Render to PDF
        doc.SavePdf(outputStream);

        // Assert - Verify PDF structure
        Assert.True(outputStream.Length > 0, "PDF output should not be empty");

        outputStream.Position = 0;
        var pdfContent = System.Text.Encoding.ASCII.GetString(outputStream.ToArray());

        // Check PDF header
        Assert.StartsWith("%PDF-1.7", pdfContent);

        // Check for required PDF structures
        Assert.Contains("/Type /Catalog", pdfContent);
        Assert.Contains("/Type /Pages", pdfContent);
        Assert.Contains("/Type /Page", pdfContent);
        Assert.Contains("/Type /Font", pdfContent);

        // Check for our text content
        Assert.Contains("Hello, Folly!", pdfContent);
        Assert.Contains("This is a test document", pdfContent);

        // Check for font references (Helvetica and Times)
        Assert.Contains("/BaseFont /Helvetica", pdfContent);
        Assert.Contains("/BaseFont /Times-Roman", pdfContent);

        // Check for text positioning operators
        Assert.Contains("BT", pdfContent); // Begin text
        Assert.Contains("ET", pdfContent); // End text
        Assert.Contains("Tf", pdfContent); // Set font
        Assert.Contains("Td", pdfContent); // Text position
        Assert.Contains("Tj", pdfContent); // Show text

        // Check PDF trailer
        Assert.Contains("%%EOF", pdfContent);
    }

    [Fact]
    public void MultipleBlocks_LayoutCorrectly()
    {
        // Arrange
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
                  <fo:block font-size="18pt" margin-bottom="12pt">First Block</fo:block>
                  <fo:block font-size="14pt" margin-bottom="12pt">Second Block</fo:block>
                  <fo:block font-size="12pt">Third Block</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        // Act
        doc.SavePdf(outputStream);

        // Assert
        Assert.True(outputStream.Length > 0);
        outputStream.Position = 0;
        var pdfContent = System.Text.Encoding.ASCII.GetString(outputStream.ToArray());

        // All three blocks should appear in the PDF
        Assert.Contains("First Block", pdfContent);
        Assert.Contains("Second Block", pdfContent);
        Assert.Contains("Third Block", pdfContent);
    }

    [Fact]
    public void TextAlignment_RendersCorrectly()
    {
        // Arrange
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
                  <fo:block text-align="start">Left aligned</fo:block>
                  <fo:block text-align="center">Center aligned</fo:block>
                  <fo:block text-align="end">Right aligned</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        // Act
        doc.SavePdf(outputStream);

        // Assert
        Assert.True(outputStream.Length > 0);
        outputStream.Position = 0;
        var pdfContent = System.Text.Encoding.ASCII.GetString(outputStream.ToArray());

        // Check all text appears
        Assert.Contains("Left aligned", pdfContent);
        Assert.Contains("Center aligned", pdfContent);
        Assert.Contains("Right aligned", pdfContent);
    }

    [Fact]
    public void LongText_BreaksAcrossLines()
    {
        // Arrange
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
                  <fo:block font-size="12pt">
                    This is a very long line of text that should be broken across multiple lines
                    when rendered in the PDF because it exceeds the available width of the content
                    area on the page. The layout engine should perform proper word wrapping.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);

        // Act - Build area tree to verify line breaking
        var areaTree = doc.BuildAreaTree();

        // Assert - Should have created multiple lines
        Assert.NotEmpty(areaTree.Pages);
        var page = areaTree.Pages[0];
        Assert.NotEmpty(page.Areas);

        var blockArea = page.Areas[0] as BlockArea;
        Assert.NotNull(blockArea);

        // Should have multiple line areas (more than 1 line)
        Assert.True(blockArea.Children.Count > 1, "Long text should break into multiple lines");
    }
}
