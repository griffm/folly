namespace Folly.UnitTests;

public class PdfMetadataTests
{
    [Fact]
    public void SavePdf_WithMetadataInOptions_IncludesMetadataInPdf()
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
                  <fo:block>Hello, Folly!</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        var options = new PdfOptions
        {
            Metadata = new PdfMetadata
            {
                Title = "Test Document",
                Author = "Test Author",
                Subject = "Test Subject",
                Keywords = "test, metadata, pdf",
                Creator = "Test Creator App",
                Producer = "Folly Test"
            }
        };

        // Act
        doc.SavePdf(outputStream, options);

        // Assert
        outputStream.Position = 0;
        var pdfContent = System.Text.Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.Contains("/Title (Test Document)", pdfContent);
        Assert.Contains("/Author (Test Author)", pdfContent);
        Assert.Contains("/Subject (Test Subject)", pdfContent);
        Assert.Contains("/Keywords (test, metadata, pdf)", pdfContent);
        Assert.Contains("/Creator (Test Creator App)", pdfContent);
        Assert.Contains("/Producer (Folly Test)", pdfContent);
        Assert.Contains("/CreationDate", pdfContent);
    }

    [Fact]
    public void SavePdf_WithMetadataInDeclarations_IncludesMetadataInPdf()
    {
        // Arrange
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:declarations>
                <fo:info>
                  <title>Document from XSL-FO</title>
                  <author>XSL-FO Author</author>
                  <subject>XSL-FO Subject</subject>
                  <keywords>xsl-fo, metadata</keywords>
                  <creator>XSL-FO Creator</creator>
                </fo:info>
              </fo:declarations>
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Hello, Folly!</fo:block>
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
        outputStream.Position = 0;
        var pdfContent = System.Text.Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.Contains("/Title (Document from XSL-FO)", pdfContent);
        Assert.Contains("/Author (XSL-FO Author)", pdfContent);
        Assert.Contains("/Subject (XSL-FO Subject)", pdfContent);
        Assert.Contains("/Keywords (xsl-fo, metadata)", pdfContent);
        Assert.Contains("/Creator (XSL-FO Creator)", pdfContent);
    }

    [Fact]
    public void SavePdf_WithBothDeclarationsAndOptions_OptionsMetadataTakesPrecedence()
    {
        // Arrange
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:declarations>
                <fo:info>
                  <title>Declaration Title</title>
                  <author>Declaration Author</author>
                </fo:info>
              </fo:declarations>
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Hello, Folly!</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        var options = new PdfOptions
        {
            Metadata = new PdfMetadata
            {
                Title = "Options Title",
                Author = "Options Author"
            }
        };

        // Act
        doc.SavePdf(outputStream, options);

        // Assert
        outputStream.Position = 0;
        var pdfContent = System.Text.Encoding.ASCII.GetString(outputStream.ToArray());

        // Options should take precedence
        Assert.Contains("/Title (Options Title)", pdfContent);
        Assert.Contains("/Author (Options Author)", pdfContent);

        // Declaration values should NOT appear
        Assert.DoesNotContain("/Title (Declaration Title)", pdfContent);
        Assert.DoesNotContain("/Author (Declaration Author)", pdfContent);
    }

    [Fact]
    public void SavePdf_WithDefaultMetadata_IncludesDefaultCreatorAndProducer()
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
                  <fo:block>Hello, Folly!</fo:block>
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
        outputStream.Position = 0;
        var pdfContent = System.Text.Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.Contains("/Creator (Folly XSL-FO Processor)", pdfContent);
        Assert.Contains("/Producer (Folly)", pdfContent);
        Assert.Contains("/CreationDate", pdfContent);
    }

    [Fact]
    public void SavePdf_WithSpecialCharactersInMetadata_EscapesCorrectly()
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
                  <fo:block>Hello, Folly!</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        var options = new PdfOptions
        {
            Metadata = new PdfMetadata
            {
                Title = "Test (with) parentheses",
                Author = "Author\\with\\backslash"
            }
        };

        // Act
        doc.SavePdf(outputStream, options);

        // Assert
        outputStream.Position = 0;
        var pdfContent = System.Text.Encoding.ASCII.GetString(outputStream.ToArray());

        // Parentheses should be escaped
        Assert.Contains("/Title (Test \\(with\\) parentheses)", pdfContent);
        // Backslashes should be escaped
        Assert.Contains("/Author (Author\\\\with\\\\backslash)", pdfContent);
    }

    [Fact]
    public void FoParser_ParsesDeclarations_Correctly()
    {
        // Arrange
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:declarations>
                <fo:info>
                  <title>Parser Test Title</title>
                  <author>Parser Test Author</author>
                  <subject>Parser Test Subject</subject>
                  <keywords>parser, test</keywords>
                  <creator>Parser Test Creator</creator>
                </fo:info>
              </fo:declarations>
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Hello, Folly!</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));

        // Act
        using var doc = FoDocument.Load(inputStream);

        // Assert
        Assert.NotNull(doc.Root.Declarations);
        Assert.NotNull(doc.Root.Declarations.Info);

        var info = doc.Root.Declarations.Info;
        Assert.Equal("Parser Test Title", info.Title);
        Assert.Equal("Parser Test Author", info.Author);
        Assert.Equal("Parser Test Subject", info.Subject);
        Assert.Equal("parser, test", info.Keywords);
        Assert.Equal("Parser Test Creator", info.Creator);
    }
}
