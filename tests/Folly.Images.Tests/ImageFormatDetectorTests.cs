using Folly.Images;
using Xunit;

namespace Folly.Images.Tests;

public class ImageFormatDetectorTests
{
    [Fact]
    public void Detect_JpegSignature_ReturnsJPEG()
    {
        // Arrange
        var jpegData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x00, 0x00 };

        // Act
        string format = ImageFormatDetector.Detect(jpegData);

        // Assert
        Assert.Equal("JPEG", format);
    }

    [Fact]
    public void Detect_PngSignature_ReturnsPNG()
    {
        // Arrange
        var pngData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // Act
        string format = ImageFormatDetector.Detect(pngData);

        // Assert
        Assert.Equal("PNG", format);
    }

    [Fact]
    public void Detect_BmpSignature_ReturnsBMP()
    {
        // Arrange
        var bmpData = new byte[] { 0x42, 0x4D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // Act
        string format = ImageFormatDetector.Detect(bmpData);

        // Assert
        Assert.Equal("BMP", format);
    }

    [Fact]
    public void Detect_Gif87aSignature_ReturnsGIF()
    {
        // Arrange
        var gifData = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61, 0x00, 0x00 };

        // Act
        string format = ImageFormatDetector.Detect(gifData);

        // Assert
        Assert.Equal("GIF", format);
    }

    [Fact]
    public void Detect_Gif89aSignature_ReturnsGIF()
    {
        // Arrange
        var gifData = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00, 0x00 };

        // Act
        string format = ImageFormatDetector.Detect(gifData);

        // Assert
        Assert.Equal("GIF", format);
    }

    [Fact]
    public void Detect_TiffLittleEndianSignature_ReturnsTIFF()
    {
        // Arrange
        var tiffData = new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // Act
        string format = ImageFormatDetector.Detect(tiffData);

        // Assert
        Assert.Equal("TIFF", format);
    }

    [Fact]
    public void Detect_TiffBigEndianSignature_ReturnsTIFF()
    {
        // Arrange
        var tiffData = new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x00 };

        // Act
        string format = ImageFormatDetector.Detect(tiffData);

        // Assert
        Assert.Equal("TIFF", format);
    }

    [Fact]
    public void Detect_SvgWithXmlDeclaration_ReturnsSVG()
    {
        // Arrange
        var svgData = System.Text.Encoding.UTF8.GetBytes(
            "<?xml version=\"1.0\"?><svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");

        // Act
        string format = ImageFormatDetector.Detect(svgData);

        // Assert
        Assert.Equal("SVG", format);
    }

    [Fact]
    public void Detect_SvgWithoutXmlDeclaration_ReturnsSVG()
    {
        // Arrange
        var svgData = System.Text.Encoding.UTF8.GetBytes(
            "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect width=\"100\" height=\"100\"/></svg>");

        // Act
        string format = ImageFormatDetector.Detect(svgData);

        // Assert
        Assert.Equal("SVG", format);
    }

    [Fact]
    public void Detect_SvgWithLeadingWhitespace_ReturnsSVG()
    {
        // Arrange
        var svgData = System.Text.Encoding.UTF8.GetBytes(
            "  \n\t<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");

        // Act
        string format = ImageFormatDetector.Detect(svgData);

        // Assert
        Assert.Equal("SVG", format);
    }

    [Fact]
    public void Detect_SvgWithUtf8Bom_ReturnsSVG()
    {
        // Arrange
        var svgData = new List<byte>();
        svgData.AddRange(new byte[] { 0xEF, 0xBB, 0xBF }); // UTF-8 BOM
        svgData.AddRange(System.Text.Encoding.UTF8.GetBytes(
            "<?xml version=\"1.0\"?><svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"));

        // Act
        string format = ImageFormatDetector.Detect(svgData.ToArray());

        // Assert
        Assert.Equal("SVG", format);
    }

    [Fact]
    public void Detect_XmlWithoutSvg_ReturnsUnknown()
    {
        // Arrange
        var xmlData = System.Text.Encoding.UTF8.GetBytes(
            "<?xml version=\"1.0\"?><root><element>data</element></root>");

        // Act
        string format = ImageFormatDetector.Detect(xmlData);

        // Assert
        Assert.Equal("UNKNOWN", format);
    }

    [Fact]
    public void Detect_EmptyData_ReturnsUnknown()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act
        string format = ImageFormatDetector.Detect(emptyData);

        // Assert
        Assert.Equal("UNKNOWN", format);
    }

    [Fact]
    public void Detect_NullData_ReturnsUnknown()
    {
        // Act
        string format = ImageFormatDetector.Detect(null!);

        // Assert
        Assert.Equal("UNKNOWN", format);
    }

    [Fact]
    public void Detect_TooSmallData_ReturnsUnknown()
    {
        // Arrange
        var smallData = new byte[] { 0xFF, 0xD8 }; // Only 2 bytes

        // Act
        string format = ImageFormatDetector.Detect(smallData);

        // Assert
        Assert.Equal("UNKNOWN", format);
    }

    [Fact]
    public void Detect_UnknownSignature_ReturnsUnknown()
    {
        // Arrange
        var unknownData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // Act
        string format = ImageFormatDetector.Detect(unknownData);

        // Assert
        Assert.Equal("UNKNOWN", format);
    }

    [Fact]
    public void Detect_CorruptedJpegSignature_ReturnsUnknown()
    {
        // Arrange
        var corruptedData = new byte[] { 0xFF, 0xD8, 0x00, 0x00 }; // Missing third 0xFF

        // Act
        string format = ImageFormatDetector.Detect(corruptedData);

        // Assert
        Assert.Equal("UNKNOWN", format);
    }

    [Fact]
    public void Detect_PartialPngSignature_ReturnsUnknown()
    {
        // Arrange
        var partialData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A }; // Only 6 bytes

        // Act
        string format = ImageFormatDetector.Detect(partialData);

        // Assert
        Assert.Equal("UNKNOWN", format);
    }

    [Fact]
    public void Detect_InvalidGifVersion_ReturnsUnknown()
    {
        // Arrange
        var invalidGif = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x35, 0x61, 0x00, 0x00 }; // GIF85a (invalid)

        // Act
        string format = ImageFormatDetector.Detect(invalidGif);

        // Assert
        Assert.Equal("UNKNOWN", format);
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00 }, "JPEG")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "PNG")]
    [InlineData(new byte[] { 0x42, 0x4D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, "BMP")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61, 0x00, 0x00 }, "GIF")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00, 0x00 }, "GIF")]
    [InlineData(new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x00, 0x00, 0x00, 0x00 }, "TIFF")]
    [InlineData(new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x00 }, "TIFF")]
    public void Detect_VariousFormats_ReturnsCorrectFormat(byte[] data, string expectedFormat)
    {
        // Act
        string format = ImageFormatDetector.Detect(data);

        // Assert
        Assert.Equal(expectedFormat, format);
    }

    [Fact]
    public void Detect_JpegWithExtraData_ReturnsJPEG()
    {
        // Arrange
        var jpegData = new List<byte>();
        jpegData.AddRange(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        jpegData.AddRange(new byte[100]); // Extra data

        // Act
        string format = ImageFormatDetector.Detect(jpegData.ToArray());

        // Assert
        Assert.Equal("JPEG", format);
    }

    [Fact]
    public void Detect_PngWithExtraData_ReturnsPNG()
    {
        // Arrange
        var pngData = new List<byte>();
        pngData.AddRange(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        pngData.AddRange(new byte[100]); // Extra data

        // Act
        string format = ImageFormatDetector.Detect(pngData.ToArray());

        // Assert
        Assert.Equal("PNG", format);
    }

    [Fact]
    public void Detect_SvgWithComplexStructure_ReturnsSVG()
    {
        // Arrange
        var svgData = System.Text.Encoding.UTF8.GetBytes(@"
            <?xml version=""1.0"" encoding=""UTF-8""?>
            <svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""100"">
                <circle cx=""50"" cy=""50"" r=""40"" fill=""red""/>
            </svg>");

        // Act
        string format = ImageFormatDetector.Detect(svgData);

        // Assert
        Assert.Equal("SVG", format);
    }

    [Fact]
    public void Detect_MinimalData_HandlesGracefully()
    {
        // Arrange
        var minimalData = new byte[] { 0x00 };

        // Act
        string format = ImageFormatDetector.Detect(minimalData);

        // Assert
        Assert.Equal("UNKNOWN", format);
    }

    [Fact]
    public void Detect_LargeUnknownFile_ReturnsUnknown()
    {
        // Arrange
        var largeData = new byte[10000];
        Array.Fill<byte>(largeData, 0x00);

        // Act
        string format = ImageFormatDetector.Detect(largeData);

        // Assert
        Assert.Equal("UNKNOWN", format);
    }
}
