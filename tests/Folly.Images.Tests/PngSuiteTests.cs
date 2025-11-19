using System.IO;
using Folly.Pdf;
using Xunit;

namespace Folly.Images.Tests;

/// <summary>
/// Tests PNG support using the PngSuite test collection by Willem van Schaik.
/// PngSuite is a comprehensive test suite for PNG decoders covering all PNG formats.
/// License: Freely licensed for any purpose without fee (see TestData/PngSuite/PngSuite.LICENSE)
/// </summary>
public class PngSuiteTests
{
    private readonly string _pngSuitePath = Path.Combine("TestData", "PngSuite");

    #region Basic Non-Interlaced Tests (Should All Work)

    [Theory]
    [InlineData("basn0g01.png", "1-bit grayscale")]
    [InlineData("basn0g02.png", "2-bit grayscale")]
    [InlineData("basn0g04.png", "4-bit grayscale")]
    [InlineData("basn0g08.png", "8-bit grayscale")]
    [InlineData("basn0g16.png", "16-bit grayscale")]
    public void PngSuite_BasicGrayscale_RendersSuccessfully(string filename, string description)
    {
        // Arrange
        var pngPath = Path.Combine(_pngSuitePath, filename);
        var foXml = CreateFoDocumentWithImage(pngPath);

        // Act & Assert
        var pdf = RenderToPdf(foXml, description);
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);
    }

    [Theory]
    [InlineData("basn2c08.png", "8-bit RGB")]
    [InlineData("basn2c16.png", "16-bit RGB")]
    public void PngSuite_BasicRgb_RendersSuccessfully(string filename, string description)
    {
        // Arrange
        var pngPath = Path.Combine(_pngSuitePath, filename);
        var foXml = CreateFoDocumentWithImage(pngPath);

        // Act & Assert
        var pdf = RenderToPdf(foXml, description);
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);
    }

    [Theory]
    [InlineData("basn3p01.png", "1-bit indexed")]
    [InlineData("basn3p02.png", "2-bit indexed")]
    [InlineData("basn3p04.png", "4-bit indexed")]
    [InlineData("basn3p08.png", "8-bit indexed")]
    public void PngSuite_BasicIndexed_RendersSuccessfully(string filename, string description)
    {
        // Arrange
        var pngPath = Path.Combine(_pngSuitePath, filename);
        var foXml = CreateFoDocumentWithImage(pngPath);

        // Act & Assert
        var pdf = RenderToPdf(foXml, description);
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);
    }

    [Theory]
    [InlineData("basn4a08.png", "8-bit grayscale + alpha")]
    [InlineData("basn4a16.png", "16-bit grayscale + alpha")]
    public void PngSuite_GrayscaleAlpha_RendersSuccessfully(string filename, string description)
    {
        // Arrange
        var pngPath = Path.Combine(_pngSuitePath, filename);
        var foXml = CreateFoDocumentWithImage(pngPath);

        // Act & Assert
        var pdf = RenderToPdf(foXml, description);
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);
    }

    [Theory]
    [InlineData("basn6a08.png", "8-bit RGBA")]
    [InlineData("basn6a16.png", "16-bit RGBA")]
    public void PngSuite_Rgba_RendersSuccessfully(string filename, string description)
    {
        // Arrange
        var pngPath = Path.Combine(_pngSuitePath, filename);
        var foXml = CreateFoDocumentWithImage(pngPath);

        // Act & Assert
        var pdf = RenderToPdf(foXml, description);
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);
    }

    #endregion

    #region Interlaced Tests (Adam7 Support)

    [Theory]
    [InlineData("basi0g01.png", "1-bit grayscale interlaced")]
    [InlineData("basi0g08.png", "8-bit grayscale interlaced")]
    [InlineData("basi2c08.png", "8-bit RGB interlaced")]
    [InlineData("basi3p04.png", "4-bit indexed interlaced")]
    [InlineData("basi4a08.png", "8-bit grayscale+alpha interlaced")]
    [InlineData("basi6a08.png", "8-bit RGBA interlaced")]
    public void PngSuite_InterlacedImages_RendersSuccessfully(string filename, string description)
    {
        // Arrange
        var pngPath = Path.Combine(_pngSuitePath, filename);
        var foXml = CreateFoDocumentWithImage(pngPath);

        // Act & Assert
        var pdf = RenderToPdf(foXml, description);
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);
    }

    #endregion

    #region Corrupted/Invalid Tests (Should Handle Gracefully)

    [Theory]
    [InlineData("xc1n0g08.png", "color type 1")]
    [InlineData("xc9n2c08.png", "color type 9")]
    [InlineData("xd0n2c08.png", "bit depth 0")]
    [InlineData("xd9n2c08.png", "bit depth 99")]
    public void PngSuite_CorruptedImages_HandlesGracefully(string filename, string description)
    {
        // Skip if file doesn't exist (some corrupted files may not be in all PngSuite versions)
        var pngPath = Path.Combine(_pngSuitePath, filename);
        if (!File.Exists(pngPath))
        {
            return; // Skip this test
        }

        // Arrange
        var foXml = CreateFoDocumentWithImage(pngPath);

        // Act & Assert - Should throw a clear exception (not crash)
        var exception = Assert.Throws<Exception>(() => RenderToPdf(foXml, description));

        // Check if it's InvalidDataException (might be wrapped)
        var innerException = exception.InnerException ?? exception;
        Assert.IsType<InvalidDataException>(innerException);
    }

    // Note: CRC/checksum errors, incorrect signatures, and missing IDAT are not validated
    // for performance and resilience. These would require full PNG validation which is expensive.
    // The basic validation above catches the most common structural issues (invalid color type, bit depth).

    #endregion

    #region Helper Methods

    private string CreateFoDocumentWithImage(string imagePath)
    {
        return $"""
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="page" page-width="8.5in" page-height="11in"
                                       margin-top="1in" margin-bottom="1in"
                                       margin-left="1in" margin-right="1in">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="page">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:external-graphic src="url('{imagePath}')"/>
                  </fo:block>
                  <fo:block>Test image: {Path.GetFileName(imagePath)}</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;
    }

    private byte[] RenderToPdf(string foXml, string description)
    {
        try
        {
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
            var doc = FoDocument.Load(stream);

            var layoutOptions = new LayoutOptions
            {
                AllowAbsoluteImagePaths = true, // Allow test data access
                AllowedImageBasePath = Directory.GetCurrentDirectory()
            };

            var areaTree = doc.BuildAreaTree(layoutOptions);

            using var pdfStream = new MemoryStream();
            using var renderer = new PdfRenderer(pdfStream, new PdfOptions());
            renderer.Render(areaTree);
            return pdfStream.ToArray();
        }
        catch (Exception ex)
        {
            // Re-throw with context for better test debugging
            throw new Exception($"Failed to render PNG ({description}): {ex.Message}", ex);
        }
    }

    #endregion
}
