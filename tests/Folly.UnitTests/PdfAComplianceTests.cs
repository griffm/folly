using Folly;
using Folly.Pdf;
using Folly.UnitTests.Helpers;
using Xunit;

namespace Folly.UnitTests;

/// <summary>
/// Tests for PDF/A compliance (Phase 12.1).
/// Priority 1 (Critical) - 12 tests.
/// </summary>
public class PdfAComplianceTests
{
    [Fact]
    public void PdfA_Disabled_ByDefault()
    {
        // Arrange
        var options = new PdfOptions();

        // Assert
        Assert.Equal(PdfALevel.None, options.PdfACompliance);
    }

    [Fact]
    public void PdfA2b_XmpMetadata_Included()
    {
        // Arrange: Create simple document with PDF/A-2b enabled
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateBlock("Test PDF/A-2b"));

        var options = new PdfOptions
        {
            PdfACompliance = PdfALevel.PdfA2b,
            EmbedFonts = true
        };

        // Act: Generate PDF with PDF/A-2b compliance
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream, options);
        var pdfBytes = pdfStream.ToArray();

        // Assert: PDF should contain XMP packet wrapper
        var pdfContent = PdfContentHelper.GetPdfContent(pdfBytes);
        Assert.Contains("<?xpacket begin=", pdfContent);
        Assert.Contains("<?xpacket end=", pdfContent);
    }

    [Fact]
    public void PdfA2b_XmpMetadata_Part()
    {
        // Arrange
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateBlock("Test"));

        var options = new PdfOptions
        {
            PdfACompliance = PdfALevel.PdfA2b,
            EmbedFonts = true
        };

        // Act
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream, options);
        var pdfBytes = pdfStream.ToArray();

        // Assert: XMP should contain pdfaid:part = 2
        var pdfContent = PdfContentHelper.GetPdfContent(pdfBytes);
        Assert.Contains("<pdfaid:part", pdfContent);
        Assert.Contains(">2</pdfaid:part>", pdfContent);
    }

    [Fact]
    public void PdfA2b_XmpMetadata_Conformance()
    {
        // Arrange
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateBlock("Test"));

        var options = new PdfOptions
        {
            PdfACompliance = PdfALevel.PdfA2b,
            EmbedFonts = true
        };

        // Act
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream, options);
        var pdfBytes = pdfStream.ToArray();

        // Assert: XMP should contain pdfaid:conformance = B
        var pdfContent = PdfContentHelper.GetPdfContent(pdfBytes);
        Assert.Contains("<pdfaid:conformance", pdfContent);
        Assert.Contains(">B</pdfaid:conformance>", pdfContent);
    }

    [Fact]
    public void PdfA2b_OutputIntent_Included()
    {
        // Arrange
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateBlock("Test"));

        var options = new PdfOptions
        {
            PdfACompliance = PdfALevel.PdfA2b,
            EmbedFonts = true
        };

        // Act
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream, options);
        var pdfBytes = pdfStream.ToArray();

        // Assert: PDF catalog should contain /OutputIntents array
        var pdfContent = PdfContentHelper.GetPdfContent(pdfBytes);
        Assert.Contains("/OutputIntents", pdfContent);
    }

    [Fact]
    public void PdfA2b_OutputIntent_IccProfile()
    {
        // Arrange
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateBlock("Test"));

        var options = new PdfOptions
        {
            PdfACompliance = PdfALevel.PdfA2b,
            EmbedFonts = true
        };

        // Act
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream, options);
        var pdfBytes = pdfStream.ToArray();

        // Assert: OutputIntent should reference ICC profile
        var pdfContent = PdfContentHelper.GetPdfContent(pdfBytes);
        Assert.Contains("/OutputIntent", pdfContent);
        Assert.Contains("/DestOutputProfile", pdfContent);
        // ICC profile stream should have /N 3 (RGB color space)
        Assert.Contains("/N 3", pdfContent);
    }

    [Fact]
    public void PdfA2b_Version_Correct()
    {
        // Arrange
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateBlock("Test"));

        var options = new PdfOptions
        {
            PdfACompliance = PdfALevel.PdfA2b,
            EmbedFonts = true
        };

        // Act
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream, options);
        var pdfBytes = pdfStream.ToArray();

        // Assert: PDF version should be 1.7 for PDF/A-2b
        var header = PdfContentHelper.GetPdfHeader(pdfBytes);
        Assert.Equal("%PDF-1.7", header);
    }

    [Fact]
    public void PdfA2b_Validation_FontsEmbedded()
    {
        // Arrange: Create document with PDF/A enabled but EmbedFonts=false
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateBlock("Test"));

        var options = new PdfOptions
        {
            PdfACompliance = PdfALevel.PdfA2b,
            EmbedFonts = false  // This should trigger validation error
        };

        // Act & Assert: Should throw InvalidOperationException
        using var pdfStream = new MemoryStream();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            foDoc.SavePdf(pdfStream, options));

        Assert.Contains("PDF/A compliance requires all fonts to be embedded", exception.Message);
    }

    [Fact]
    public void PdfA1b_Metadata()
    {
        // Arrange
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateBlock("Test"));

        var options = new PdfOptions
        {
            PdfACompliance = PdfALevel.PdfA1b,
            EmbedFonts = true
        };

        // Act
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream, options);
        var pdfBytes = pdfStream.ToArray();

        // Assert: XMP should contain pdfaid:part = 1 for PDF/A-1b
        var pdfContent = PdfContentHelper.GetPdfContent(pdfBytes);
        Assert.Contains("<pdfaid:part", pdfContent);
        Assert.Contains(">1</pdfaid:part>", pdfContent);
        Assert.Contains(">B</pdfaid:conformance>", pdfContent);
    }

    [Fact]
    public void PdfA3b_Metadata()
    {
        // Arrange
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateBlock("Test"));

        var options = new PdfOptions
        {
            PdfACompliance = PdfALevel.PdfA3b,
            EmbedFonts = true
        };

        // Act
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream, options);
        var pdfBytes = pdfStream.ToArray();

        // Assert: XMP should contain pdfaid:part = 3 for PDF/A-3b
        var pdfContent = PdfContentHelper.GetPdfContent(pdfBytes);
        Assert.Contains("<pdfaid:part", pdfContent);
        Assert.Contains(">3</pdfaid:part>", pdfContent);
        Assert.Contains(">B</pdfaid:conformance>", pdfContent);
    }

    [Fact]
    public void PdfA_DublinCore_Metadata()
    {
        // Arrange: Create document with Dublin Core metadata
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateBlock("Test"));

        var options = new PdfOptions
        {
            PdfACompliance = PdfALevel.PdfA2b,
            EmbedFonts = true,
            Metadata = new PdfMetadata
            {
                Title = "Test Document",
                Author = "Test Author",
                Subject = "Test Subject",
                Keywords = "testing, pdf/a, compliance"
            }
        };

        // Act
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream, options);
        var pdfBytes = pdfStream.ToArray();

        // Assert: XMP should contain Dublin Core metadata
        var pdfContent = PdfContentHelper.GetPdfContent(pdfBytes);

        // Dublin Core namespace and elements
        Assert.Contains("http://purl.org/dc/elements/1.1/", pdfContent);
        Assert.Contains("<dc:title", pdfContent);
        Assert.Contains("Test Document", pdfContent);
        Assert.Contains("<dc:creator", pdfContent);
        Assert.Contains("Test Author", pdfContent);
        Assert.Contains("<dc:description", pdfContent);
        Assert.Contains("Test Subject", pdfContent);
        Assert.Contains("<dc:subject", pdfContent);
        Assert.Contains("testing", pdfContent);
    }

    [Fact]
    public void PdfA_Integration()
    {
        // Arrange: Full integration test with realistic document
        var content = new System.Xml.Linq.XElement(
            System.Xml.Linq.XName.Get("block", "http://www.w3.org/1999/XSL/Format"),
            new System.Xml.Linq.XElement(
                System.Xml.Linq.XName.Get("block", "http://www.w3.org/1999/XSL/Format"),
                new System.Xml.Linq.XAttribute("font-weight", "bold"),
                "PDF/A Compliance Test"),
            new System.Xml.Linq.XElement(
                System.Xml.Linq.XName.Get("block", "http://www.w3.org/1999/XSL/Format"),
                "This is a test document for PDF/A-2b compliance."),
            new System.Xml.Linq.XElement(
                System.Xml.Linq.XName.Get("block", "http://www.w3.org/1999/XSL/Format"),
                "It should contain XMP metadata and an OutputIntent.")
        );

        var foDoc = FoSnippetBuilder.CreateSimpleDocument(content);

        var options = new PdfOptions
        {
            PdfACompliance = PdfALevel.PdfA2b,
            EmbedFonts = true,
            Metadata = new PdfMetadata
            {
                Title = "PDF/A Integration Test",
                Author = "Folly Test Suite",
                Subject = "Testing PDF/A compliance",
                Keywords = "pdf/a, archival, long-term preservation"
            }
        };

        // Act
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream, options);
        var pdfBytes = pdfStream.ToArray();

        // Assert: Comprehensive structural validation
        Assert.True(PdfContentHelper.IsValidPdf(pdfBytes), "Should be a valid PDF");

        var pdfContent = PdfContentHelper.GetPdfContent(pdfBytes);

        // 1. PDF version
        var header = PdfContentHelper.GetPdfHeader(pdfBytes);
        Assert.Equal("%PDF-1.7", header);

        // 2. XMP metadata with PDF/A identification
        Assert.Contains("<?xpacket", pdfContent);
        Assert.Contains("<pdfaid:part>2</pdfaid:part>", pdfContent);
        Assert.Contains("<pdfaid:conformance>B</pdfaid:conformance>", pdfContent);

        // 3. Dublin Core metadata in XMP
        Assert.Contains("PDF/A Integration Test", pdfContent);
        Assert.Contains("Folly Test Suite", pdfContent);
        Assert.Contains("long-term preservation", pdfContent);

        // 4. OutputIntent with ICC profile
        Assert.Contains("/OutputIntents", pdfContent);
        Assert.Contains("/DestOutputProfile", pdfContent);
        Assert.Contains("/N 3", pdfContent); // RGB color space

        // 5. Verify PDF contains necessary structural elements
        Assert.Contains("/Type /Catalog", pdfContent);
        Assert.Contains("/Type /Pages", pdfContent);
        Assert.Contains("/Type /Page", pdfContent);
    }
}
