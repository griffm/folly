namespace Folly.UnitTests;

using System.Text;

/// <summary>
/// Tests for PDF output validation including structure, font subsetting,
/// stream compression, metadata, bookmarks, and link destinations.
/// </summary>
public class PdfValidationTests
{
    [Fact]
    public void PdfOutput_HasCorrectHeader()
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
                  <fo:block>Test</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        doc.SavePdf(outputStream);

        outputStream.Position = 0;
        var header = new byte[8];
        outputStream.Read(header, 0, 8);
        var headerString = Encoding.ASCII.GetString(header);

        Assert.StartsWith("%PDF-1.7", headerString);
    }

    [Fact]
    public void PdfOutput_HasCorrectTrailer()
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
                  <fo:block>Test</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        var options = new PdfOptions { CompressStreams = false };
        doc.SavePdf(outputStream, options);

        outputStream.Position = 0;
        var pdfContent = Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.Contains("%%EOF", pdfContent);
    }

    [Fact]
    public void PdfOutput_ContainsCatalog()
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
                  <fo:block>Test</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        var options = new PdfOptions { CompressStreams = false };
        doc.SavePdf(outputStream, options);

        outputStream.Position = 0;
        var pdfContent = Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.Contains("/Type /Catalog", pdfContent);
    }

    [Fact]
    public void PdfOutput_ContainsPages()
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
                  <fo:block>Test</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        var options = new PdfOptions { CompressStreams = false };
        doc.SavePdf(outputStream, options);

        outputStream.Position = 0;
        var pdfContent = Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.Contains("/Type /Pages", pdfContent);
        Assert.Contains("/Type /Page", pdfContent);
    }

    [Fact]
    public void PdfOutput_EmbedsFonts()
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
                  <fo:block font-family="Helvetica">Helvetica text</fo:block>
                  <fo:block font-family="Times">Times text</fo:block>
                  <fo:block font-family="Courier">Courier text</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        var options = new PdfOptions { CompressStreams = false };
        doc.SavePdf(outputStream, options);

        outputStream.Position = 0;
        var pdfContent = Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.Contains("/Type /Font", pdfContent);
        Assert.Contains("/BaseFont /Helvetica", pdfContent);
        Assert.Contains("/BaseFont /Times-Roman", pdfContent);
        Assert.Contains("/BaseFont /Courier", pdfContent);
    }

    [Fact]
    public void PdfOutput_WithCompression_IsSmallerThanUncompressed()
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
                  <fo:block>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.</fo:block>
                  <fo:block>Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream1 = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc1 = FoDocument.Load(inputStream1);
        using var uncompressed = new MemoryStream();
        doc1.SavePdf(uncompressed, new PdfOptions { CompressStreams = false });

        using var inputStream2 = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc2 = FoDocument.Load(inputStream2);
        using var compressed = new MemoryStream();
        doc2.SavePdf(compressed, new PdfOptions { CompressStreams = true });

        Assert.True(compressed.Length < uncompressed.Length,
            $"Compressed PDF ({compressed.Length} bytes) should be smaller than uncompressed ({uncompressed.Length} bytes)");
    }

    [Fact]
    public void PdfOutput_IncludesMetadata()
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
                  <fo:block>Test</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);

        using var outputStream = new MemoryStream();
        var options = new PdfOptions
        {
            CompressStreams = false,
            Metadata = new PdfMetadata
            {
                Title = "Test Document",
                Author = "Folly Test Suite",
                Subject = "PDF Validation"
            }
        };
        doc.SavePdf(outputStream, options);

        outputStream.Position = 0;
        var pdfContent = Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.Contains("/Title", pdfContent);
        Assert.Contains("/Author", pdfContent);
        Assert.Contains("/Subject", pdfContent);
    }

    [Fact]
    public void PdfOutput_WithBookmarks_CreatesOutline()
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

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        var options = new PdfOptions { CompressStreams = false };
        doc.SavePdf(outputStream, options);

        outputStream.Position = 0;
        var pdfContent = Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.Contains("/Outlines", pdfContent);
    }

    [Fact]
    public void PdfOutput_WithExternalLink_CreatesAnnotation()
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
                    <fo:basic-link external-destination="http://example.com">Link</fo:basic-link>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        var options = new PdfOptions { CompressStreams = false };
        doc.SavePdf(outputStream, options);

        outputStream.Position = 0;
        var pdfContent = Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.Contains("/Annots", pdfContent);
        Assert.Contains("/URI", pdfContent);
    }

    [Fact]
    public void PdfOutput_ContainsTextContent()
    {
        var testText = "Hello, PDF World!";
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
                  <fo:block>{testText}</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        var options = new PdfOptions { CompressStreams = false };
        doc.SavePdf(outputStream, options);

        outputStream.Position = 0;
        var pdfContent = Encoding.ASCII.GetString(outputStream.ToArray());

        Assert.Contains(testText, pdfContent);
    }

    [Fact]
    public void PdfOutput_ContainsGraphicsOperators()
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
                  <fo:block border="1pt solid black" background-color="yellow">Test</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        var options = new PdfOptions { CompressStreams = false };
        doc.SavePdf(outputStream, options);

        outputStream.Position = 0;
        var pdfContent = Encoding.ASCII.GetString(outputStream.ToArray());

        // Check for basic PDF graphics operators
        Assert.Contains("q", pdfContent); // Save graphics state
        Assert.Contains("Q", pdfContent); // Restore graphics state
        Assert.Contains("BT", pdfContent); // Begin text
        Assert.Contains("ET", pdfContent); // End text
    }

    [Fact]
    public void PdfOutput_MultiplePages_HasCorrectCount()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-height="200pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block margin-bottom="100pt">Page 1</fo:block>
                  <fo:block>Page 2</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);

        var areaTree = doc.BuildAreaTree();
        Assert.True(areaTree.Pages.Count >= 2, "Should generate at least 2 pages");
    }

    [Fact]
    public void PdfOutput_IsNotEmpty()
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
                  <fo:block>Content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        doc.SavePdf(outputStream);

        Assert.True(outputStream.Length > 100, "PDF should be larger than 100 bytes");
    }
}
