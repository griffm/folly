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
        Assert.Contains("Tm", pdfContent); // Text matrix (absolute positioning)
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

    [Fact]
    public void LongDocument_CreatesMultiplePages()
    {
        // Arrange - Create a document with many blocks that will overflow one page
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
                  <fo:block font-size="18pt" margin-bottom="12pt">Chapter 1: Introduction</fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    This is the first paragraph of our document. It contains some introductory text
                    that explains what this document is about and why it exists.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Here is another paragraph with more detailed information about the topic at hand.
                    We need to add enough content to ensure this spans multiple pages.
                  </fo:block>
                  <fo:block font-size="18pt" margin-bottom="12pt" margin-top="24pt">Chapter 2: Main Content</fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
                    incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud
                    exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu
                    fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in
                    culpa qui officia deserunt mollit anim id est laborum.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque
                    laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi
                    architecto beatae vitae dicta sunt explicabo.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia
                    consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci
                    velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam
                    aliquam quaerat voluptatem.
                  </fo:block>
                  <fo:block font-size="18pt" margin-bottom="12pt" margin-top="24pt">Chapter 3: More Content</fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium
                    voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint
                    occaecati cupiditate non provident.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Similique sunt in culpa qui officia deserunt mollitia animi, id est laborum et dolorum
                    fuga. Et harum quidem rerum facilis est et expedita distinctio.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Nam libero tempore, cum soluta nobis est eligendi optio cumque nihil impedit quo minus
                    id quod maxime placeat facere possimus, omnis voluptas assumenda est, omnis dolor
                    repellendus.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus saepe
                    eveniet ut et voluptates repudiandae sint et molestiae non recusandae.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Itaque earum rerum hic tenetur a sapiente delectus, ut aut reiciendis voluptatibus
                    maiores alias consequatur aut perferendis doloribus asperiores repellat.
                  </fo:block>
                  <fo:block font-size="18pt" margin-bottom="12pt" margin-top="24pt">Chapter 4: Even More Content</fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
                    incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud
                    exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu
                    fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in
                    culpa qui officia deserunt mollit anim id est laborum.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque
                    laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi
                    architecto beatae vitae dicta sunt explicabo.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia
                    consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci
                    velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam
                    aliquam quaerat voluptatem.
                  </fo:block>
                  <fo:block font-size="18pt" margin-bottom="12pt" margin-top="24pt">Chapter 5: Additional Content</fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium
                    voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint
                    occaecati cupiditate non provident.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Similique sunt in culpa qui officia deserunt mollitia animi, id est laborum et dolorum
                    fuga. Et harum quidem rerum facilis est et expedita distinctio.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Nam libero tempore, cum soluta nobis est eligendi optio cumque nihil impedit quo minus
                    id quod maxime placeat facere possimus, omnis voluptas assumenda est, omnis dolor
                    repellendus.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus saepe
                    eveniet ut et voluptates repudiandae sint et molestiae non recusandae.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Itaque earum rerum hic tenetur a sapiente delectus, ut aut reiciendis voluptatibus
                    maiores alias consequatur aut perferendis doloribus asperiores repellat.
                  </fo:block>
                  <fo:block font-size="18pt" margin-bottom="12pt" margin-top="24pt">Chapter 6: Final Section</fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
                    incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud
                    exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu
                    fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in
                    culpa qui officia deserunt mollit anim id est laborum.
                  </fo:block>
                  <fo:block font-size="12pt" margin-bottom="12pt">
                    Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque
                    laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi
                    architecto beatae vitae dicta sunt explicabo.
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

        // Assert - Build area tree to check page count
        var areaTree = doc.BuildAreaTree();
        Assert.True(areaTree.Pages.Count > 1, $"Document should span multiple pages, but only has {areaTree.Pages.Count} page(s)");

        // Verify PDF contains multiple pages
        outputStream.Position = 0;
        var pdfContent = System.Text.Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.StartsWith("%PDF-1.7", pdfContent);
        Assert.Contains("/Type /Page", pdfContent);

        // Check that content from different sections appears in the PDF
        Assert.Contains("Chapter 1: Introduction", pdfContent);
        Assert.Contains("Chapter 2: Main Content", pdfContent);
        Assert.Contains("Chapter 3: More Content", pdfContent);
    }

    [Fact]
    public void BordersAndBackgrounds_RenderCorrectly()
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
                  <fo:block background-color="yellow" padding="12pt" margin-bottom="12pt">
                    Block with yellow background
                  </fo:block>
                  <fo:block border-width="2pt" border-style="solid" border-color="red" padding="12pt" margin-bottom="12pt">
                    Block with red border
                  </fo:block>
                  <fo:block background-color="#0000FF" border-width="3pt" border-style="dashed" border-color="black" padding="12pt">
                    Block with blue background and dashed border
                  </fo:block>
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

        // Check PDF structure
        Assert.StartsWith("%PDF-1.7", pdfContent);

        // Check for graphics state operators
        Assert.Contains("q", pdfContent); // Save state
        Assert.Contains("Q", pdfContent); // Restore state

        // Check for color operators (rg = fill color, RG = stroke color)
        Assert.Contains("rg", pdfContent); // Fill color for backgrounds
        Assert.Contains("RG", pdfContent); // Stroke color for borders

        // Check for rectangle operators
        Assert.Contains("re", pdfContent); // Rectangle path
        Assert.Contains("f", pdfContent);  // Fill
        Assert.Contains("S", pdfContent);  // Stroke

        // Check for line width and dash pattern operators
        Assert.Contains("w", pdfContent);  // Line width
        Assert.Contains("d", pdfContent);  // Dash pattern

        // Verify text content still appears
        Assert.Contains("Block with yellow background", pdfContent);
        Assert.Contains("Block with red border", pdfContent);
        Assert.Contains("Block with blue background and dashed border", pdfContent);
    }
}
