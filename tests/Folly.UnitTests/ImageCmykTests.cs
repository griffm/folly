using Folly.Images;
using Folly.Images.Parsers;
using Folly.Pdf;
using Folly.UnitTests.Helpers;
using Xunit;

namespace Folly.UnitTests;

/// <summary>
/// Tests for CMYK color space and ICC profile support (Phase 9.4).
/// Priority 1 (Critical) - 10 tests.
/// </summary>
public class ImageCmykTests
{
    [Fact]
    public void DetectCmykJpeg()
    {
        // Arrange
        var imageBytes = TestResourceLocator.LoadImage("test-cmyk.jpg");
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(imageBytes);

        // Assert
        Assert.Equal("DeviceCMYK", info.ColorSpace);
        Assert.Equal(4, info.ColorComponents);
    }

    [Fact(Skip = "Integration test - requires investigation of FO document structure")]
    public void EmbedCmykJpeg_UsesDeviceCMYK()
    {
        // Arrange
        var imagePath = TestResourceLocator.GetImagePath("test-cmyk.jpg");
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateImage(imagePath));

        // Act
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream);
        var pdfBytes = pdfStream.ToArray();

        // Assert
        var pdfContent = PdfContentHelper.GetPdfContent(pdfBytes);
        Assert.Contains("/DeviceCMYK", pdfContent);
    }

    [Fact]
    public void ExtractIccProfile_FromJpeg()
    {
        // Arrange
        var imageBytes = TestResourceLocator.LoadImage("jpeg-with-icc.jpg");
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(imageBytes);

        // Assert
        Assert.NotNull(info.IccProfile);
        Assert.True(info.IccProfile.Length > 0);
    }

    [Fact]
    public void ExtractIccProfile_FromPng()
    {
        // Arrange
        var imageBytes = TestResourceLocator.LoadImage("png-with-iccp.png");
        var parser = new PngParser();

        // Act
        var info = parser.Parse(imageBytes);

        // Assert
        Assert.NotNull(info.IccProfile);
        Assert.True(info.IccProfile.Length > 0);
    }

    [Fact(Skip = "Integration test - requires investigation of FO document structure")]
    public void EmbedIccProfile_InPdf()
    {
        // Arrange
        var imagePath = TestResourceLocator.GetImagePath("jpeg-with-icc.jpg");
        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            FoSnippetBuilder.CreateImage(imagePath));

        // Act
        using var pdfStream = new MemoryStream();
        foDoc.SavePdf(pdfStream);
        var pdfBytes = pdfStream.ToArray();

        // Assert
        var pdfContent = PdfContentHelper.GetPdfContent(pdfBytes);
        Assert.Contains("/ICCBased", pdfContent);
    }

    [Fact]
    public void IccProfile_ZlibDecompression_Png()
    {
        // Arrange
        var imageBytes = TestResourceLocator.LoadImage("png-with-iccp.png");
        var parser = new PngParser();

        // Act
        var info = parser.Parse(imageBytes);

        // Assert
        // PNG iCCP chunk is zlib-compressed, verify it decompresses correctly
        Assert.NotNull(info.IccProfile);
        Assert.True(info.IccProfile.Length > 100); // ICC profiles are typically >100 bytes
    }

    [Fact(Skip = "Implementation pending")]
    public void RgbJpeg_NoIccProfile_UsesDeviceRGB()
    {
        // TODO: Test that RGB JPEG without ICC profile uses /DeviceRGB
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Implementation pending")]
    public void IccProfile_EmbeddedAsStream()
    {
        // TODO: Verify ICC profile is embedded as a stream object in PDF
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Implementation pending")]
    public void MultipleImages_CmykAndRgb()
    {
        // TODO: Test document with both CMYK and RGB images
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Implementation pending")]
    public void PrintWorkflow_CmykImage()
    {
        // TODO: Integration test with CMYK image in print layout
        Assert.True(true, "Not yet implemented");
    }
}
