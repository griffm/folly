using Folly.Images;
using Folly.Images.Parsers;
using Xunit;

namespace Folly.Images.Tests;

public class JpegParserTests
{
    [Fact]
    public void JpegParser_CanParse_ValidJpegData_ReturnsTrue()
    {
        // Arrange
        var jpegData = CreateSimpleJpeg(2, 2);
        var parser = new JpegParser();

        // Act
        bool canParse = parser.CanParse(jpegData);

        // Assert
        Assert.True(canParse);
    }

    [Fact]
    public void JpegParser_CanParse_InvalidData_ReturnsFalse()
    {
        // Arrange
        var invalidData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG signature
        var parser = new JpegParser();

        // Act
        bool canParse = parser.CanParse(invalidData);

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void JpegParser_CanParse_EmptyData_ReturnsFalse()
    {
        // Arrange
        var parser = new JpegParser();

        // Act
        bool canParse = parser.CanParse(Array.Empty<byte>());

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void JpegParser_CanParse_NullData_ReturnsFalse()
    {
        // Arrange
        var parser = new JpegParser();

        // Act
        bool canParse = parser.CanParse(null!);

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void JpegParser_Parse_SimpleJpegRgb_ReturnsCorrectInfo()
    {
        // Arrange
        var jpegData = CreateSimpleJpeg(100, 50);
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(jpegData);

        // Assert
        Assert.Equal("JPEG", info.Format);
        Assert.Equal(100, info.Width);
        Assert.Equal(50, info.Height);
        Assert.Equal("DeviceRGB", info.ColorSpace);
        Assert.Equal(3, info.ColorComponents);
        Assert.Equal(8, info.BitsPerComponent);
        Assert.NotNull(info.RawData);
        Assert.Null(info.Palette);
    }

    [Fact]
    public void JpegParser_Parse_GrayscaleJpeg_ReturnsGrayscaleColorSpace()
    {
        // Arrange
        var jpegData = CreateGrayscaleJpeg(50, 50);
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(jpegData);

        // Assert
        Assert.Equal("JPEG", info.Format);
        Assert.Equal(50, info.Width);
        Assert.Equal(50, info.Height);
        Assert.Equal("DeviceGray", info.ColorSpace);
        Assert.Equal(1, info.ColorComponents);
        Assert.Equal(8, info.BitsPerComponent);
    }

    [Fact]
    public void JpegParser_Parse_CmykJpeg_ReturnsCmykColorSpace()
    {
        // Arrange
        var jpegData = CreateCmykJpeg(50, 50);
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(jpegData);

        // Assert
        Assert.Equal("JPEG", info.Format);
        Assert.Equal(50, info.Width);
        Assert.Equal(50, info.Height);
        Assert.Equal("DeviceCMYK", info.ColorSpace);
        Assert.Equal(4, info.ColorComponents);
    }

    [Fact]
    public void JpegParser_Parse_JpegWithJfifDpi_ExtractsDpiCorrectly()
    {
        // Arrange
        var jpegData = CreateJpegWithJfifDpi(50, 50, 300, 300);
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(jpegData);

        // Assert
        Assert.Equal(300, info.HorizontalDpi);
        Assert.Equal(300, info.VerticalDpi);
    }

    [Fact]
    public void JpegParser_Parse_JpegWithDpiInCm_ConvertsToDpi()
    {
        // Arrange
        var jpegData = CreateJpegWithJfifDpiInCm(50, 50, 118, 118); // ~300 DPI
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(jpegData);

        // Assert
        Assert.True(info.HorizontalDpi > 295 && info.HorizontalDpi < 305); // ~300 DPI
        Assert.True(info.VerticalDpi > 295 && info.VerticalDpi < 305);
    }

    [Fact]
    public void JpegParser_Parse_JpegWithoutDpi_UsesDefaultDpi()
    {
        // Arrange
        var jpegData = CreateSimpleJpeg(50, 50);
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(jpegData);

        // Assert
        Assert.Equal(72, info.HorizontalDpi);
        Assert.Equal(72, info.VerticalDpi);
    }

    [Fact]
    public void JpegParser_Parse_InvalidJpegSignature_ThrowsException()
    {
        // Arrange
        var invalidData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG signature
        var parser = new JpegParser();

        // Act & Assert
        var exception = Assert.Throws<InvalidDataException>(() => parser.Parse(invalidData));
        Assert.Contains("Invalid JPEG signature", exception.Message);
    }

    [Fact]
    public void JpegParser_Parse_JpegWithoutDimensions_ThrowsException()
    {
        // Arrange
        // Create a JPEG with only SOI and EOI markers (no SOF)
        var jpegData = new List<byte>();
        jpegData.AddRange(new byte[] { 0xFF, 0xD8 }); // SOI
        jpegData.AddRange(new byte[] { 0xFF, 0xD9 }); // EOI
        var parser = new JpegParser();

        // Act & Assert
        var exception = Assert.Throws<InvalidDataException>(() => parser.Parse(jpegData.ToArray()));
        Assert.Contains("invalid dimensions", exception.Message);
    }

    [Fact]
    public void JpegParser_FormatName_ReturnsJpeg()
    {
        // Arrange
        var parser = new JpegParser();

        // Act
        string formatName = parser.FormatName;

        // Assert
        Assert.Equal("JPEG", formatName);
    }

    [Fact]
    public void ImageFormatDetector_Detect_JpegSignature_ReturnsJPEG()
    {
        // Arrange
        var jpegData = CreateSimpleJpeg(10, 10);

        // Act
        string format = ImageFormatDetector.Detect(jpegData);

        // Assert
        Assert.Equal("JPEG", format);
    }

    // Helper method to create a minimal JPEG for testing
    private static byte[] CreateSimpleJpeg(int width, int height)
    {
        var jpeg = new List<byte>();

        // SOI (Start of Image)
        jpeg.AddRange(new byte[] { 0xFF, 0xD8 });

        // APP0 (JFIF) - optional but common
        jpeg.AddRange(new byte[] { 0xFF, 0xE0 }); // APP0 marker
        jpeg.AddRange(new byte[] { 0x00, 0x10 }); // Length = 16
        jpeg.AddRange(new byte[] { 0x4A, 0x46, 0x49, 0x46, 0x00 }); // "JFIF\0"
        jpeg.Add(0x01); jpeg.Add(0x01); // Version 1.1
        jpeg.Add(0x00); // Density units: no units (aspect ratio only)
        jpeg.AddRange(new byte[] { 0x00, 0x01 }); // X density = 1
        jpeg.AddRange(new byte[] { 0x00, 0x01 }); // Y density = 1
        jpeg.Add(0x00); jpeg.Add(0x00); // Thumbnail dimensions

        // SOF0 (Start of Frame - Baseline DCT)
        jpeg.AddRange(new byte[] { 0xFF, 0xC0 }); // SOF0 marker
        jpeg.AddRange(new byte[] { 0x00, 0x11 }); // Length = 17 (for 3 components)
        jpeg.Add(0x08); // Precision (8 bits)
        jpeg.Add((byte)((height >> 8) & 0xFF)); // Height MSB
        jpeg.Add((byte)(height & 0xFF)); // Height LSB
        jpeg.Add((byte)((width >> 8) & 0xFF)); // Width MSB
        jpeg.Add((byte)(width & 0xFF)); // Width LSB
        jpeg.Add(0x03); // Number of components (RGB = 3)

        // Component 1 (Y)
        jpeg.Add(0x01); // Component ID
        jpeg.Add(0x22); // Sampling factors (2x2)
        jpeg.Add(0x00); // Quantization table ID

        // Component 2 (Cb)
        jpeg.Add(0x02); // Component ID
        jpeg.Add(0x11); // Sampling factors (1x1)
        jpeg.Add(0x01); // Quantization table ID

        // Component 3 (Cr)
        jpeg.Add(0x03); // Component ID
        jpeg.Add(0x11); // Sampling factors (1x1)
        jpeg.Add(0x01); // Quantization table ID

        // SOS (Start of Scan)
        jpeg.AddRange(new byte[] { 0xFF, 0xDA }); // SOS marker
        jpeg.AddRange(new byte[] { 0x00, 0x0C }); // Length = 12
        jpeg.Add(0x03); // Number of components
        jpeg.Add(0x01); jpeg.Add(0x00); // Component 1, DC/AC table
        jpeg.Add(0x02); jpeg.Add(0x11); // Component 2, DC/AC table
        jpeg.Add(0x03); jpeg.Add(0x11); // Component 3, DC/AC table
        jpeg.Add(0x00); jpeg.Add(0x3F); jpeg.Add(0x00); // Spectral selection

        // Compressed image data (minimal)
        jpeg.AddRange(new byte[] { 0xFF, 0x00 }); // Stuffed byte

        // EOI (End of Image)
        jpeg.AddRange(new byte[] { 0xFF, 0xD9 });

        return jpeg.ToArray();
    }

    private static byte[] CreateGrayscaleJpeg(int width, int height)
    {
        var jpeg = new List<byte>();

        // SOI
        jpeg.AddRange(new byte[] { 0xFF, 0xD8 });

        // SOF0 - Grayscale (1 component)
        jpeg.AddRange(new byte[] { 0xFF, 0xC0 });
        jpeg.AddRange(new byte[] { 0x00, 0x0B }); // Length = 11 (for 1 component)
        jpeg.Add(0x08); // Precision
        jpeg.Add((byte)((height >> 8) & 0xFF));
        jpeg.Add((byte)(height & 0xFF));
        jpeg.Add((byte)((width >> 8) & 0xFF));
        jpeg.Add((byte)(width & 0xFF));
        jpeg.Add(0x01); // Number of components (Grayscale = 1)
        jpeg.Add(0x01); // Component ID
        jpeg.Add(0x11); // Sampling factors
        jpeg.Add(0x00); // Quantization table

        // SOS
        jpeg.AddRange(new byte[] { 0xFF, 0xDA });
        jpeg.AddRange(new byte[] { 0x00, 0x08 }); // Length
        jpeg.Add(0x01); // Number of components
        jpeg.Add(0x01); jpeg.Add(0x00);
        jpeg.Add(0x00); jpeg.Add(0x3F); jpeg.Add(0x00);

        // Minimal data
        jpeg.AddRange(new byte[] { 0xFF, 0x00 });

        // EOI
        jpeg.AddRange(new byte[] { 0xFF, 0xD9 });

        return jpeg.ToArray();
    }

    private static byte[] CreateCmykJpeg(int width, int height)
    {
        var jpeg = new List<byte>();

        // SOI
        jpeg.AddRange(new byte[] { 0xFF, 0xD8 });

        // SOF0 - CMYK (4 components)
        jpeg.AddRange(new byte[] { 0xFF, 0xC0 });
        jpeg.AddRange(new byte[] { 0x00, 0x14 }); // Length = 20 (for 4 components)
        jpeg.Add(0x08); // Precision
        jpeg.Add((byte)((height >> 8) & 0xFF));
        jpeg.Add((byte)(height & 0xFF));
        jpeg.Add((byte)((width >> 8) & 0xFF));
        jpeg.Add((byte)(width & 0xFF));
        jpeg.Add(0x04); // Number of components (CMYK = 4)

        // Components C, M, Y, K
        for (int i = 1; i <= 4; i++)
        {
            jpeg.Add((byte)i); // Component ID
            jpeg.Add(0x11); // Sampling factors
            jpeg.Add(0x00); // Quantization table
        }

        // SOS
        jpeg.AddRange(new byte[] { 0xFF, 0xDA });
        jpeg.AddRange(new byte[] { 0x00, 0x0E }); // Length
        jpeg.Add(0x04); // Number of components
        for (int i = 1; i <= 4; i++)
        {
            jpeg.Add((byte)i); jpeg.Add(0x00);
        }
        jpeg.Add(0x00); jpeg.Add(0x3F); jpeg.Add(0x00);

        // Minimal data
        jpeg.AddRange(new byte[] { 0xFF, 0x00 });

        // EOI
        jpeg.AddRange(new byte[] { 0xFF, 0xD9 });

        return jpeg.ToArray();
    }

    private static byte[] CreateJpegWithJfifDpi(int width, int height, int dpiX, int dpiY)
    {
        var jpeg = new List<byte>();

        // SOI
        jpeg.AddRange(new byte[] { 0xFF, 0xD8 });

        // APP0 (JFIF) with DPI
        jpeg.AddRange(new byte[] { 0xFF, 0xE0 });
        jpeg.AddRange(new byte[] { 0x00, 0x10 }); // Length
        jpeg.AddRange(new byte[] { 0x4A, 0x46, 0x49, 0x46, 0x00 }); // "JFIF\0"
        jpeg.Add(0x01); jpeg.Add(0x01); // Version
        jpeg.Add(0x01); // Density units: dots per inch
        jpeg.Add((byte)((dpiX >> 8) & 0xFF));
        jpeg.Add((byte)(dpiX & 0xFF));
        jpeg.Add((byte)((dpiY >> 8) & 0xFF));
        jpeg.Add((byte)(dpiY & 0xFF));
        jpeg.Add(0x00); jpeg.Add(0x00); // Thumbnail

        // SOF0
        jpeg.AddRange(new byte[] { 0xFF, 0xC0 });
        jpeg.AddRange(new byte[] { 0x00, 0x11 });
        jpeg.Add(0x08);
        jpeg.Add((byte)((height >> 8) & 0xFF));
        jpeg.Add((byte)(height & 0xFF));
        jpeg.Add((byte)((width >> 8) & 0xFF));
        jpeg.Add((byte)(width & 0xFF));
        jpeg.Add(0x03);
        jpeg.Add(0x01); jpeg.Add(0x22); jpeg.Add(0x00);
        jpeg.Add(0x02); jpeg.Add(0x11); jpeg.Add(0x01);
        jpeg.Add(0x03); jpeg.Add(0x11); jpeg.Add(0x01);

        // SOS
        jpeg.AddRange(new byte[] { 0xFF, 0xDA });
        jpeg.AddRange(new byte[] { 0x00, 0x0C });
        jpeg.Add(0x03);
        jpeg.Add(0x01); jpeg.Add(0x00);
        jpeg.Add(0x02); jpeg.Add(0x11);
        jpeg.Add(0x03); jpeg.Add(0x11);
        jpeg.Add(0x00); jpeg.Add(0x3F); jpeg.Add(0x00);

        jpeg.AddRange(new byte[] { 0xFF, 0x00 });

        // EOI
        jpeg.AddRange(new byte[] { 0xFF, 0xD9 });

        return jpeg.ToArray();
    }

    private static byte[] CreateJpegWithJfifDpiInCm(int width, int height, int dpiXcm, int dpiYcm)
    {
        var jpeg = new List<byte>();

        // SOI
        jpeg.AddRange(new byte[] { 0xFF, 0xD8 });

        // APP0 (JFIF) with DPI in cm
        jpeg.AddRange(new byte[] { 0xFF, 0xE0 });
        jpeg.AddRange(new byte[] { 0x00, 0x10 }); // Length
        jpeg.AddRange(new byte[] { 0x4A, 0x46, 0x49, 0x46, 0x00 }); // "JFIF\0"
        jpeg.Add(0x01); jpeg.Add(0x01); // Version
        jpeg.Add(0x02); // Density units: dots per cm
        jpeg.Add((byte)((dpiXcm >> 8) & 0xFF));
        jpeg.Add((byte)(dpiXcm & 0xFF));
        jpeg.Add((byte)((dpiYcm >> 8) & 0xFF));
        jpeg.Add((byte)(dpiYcm & 0xFF));
        jpeg.Add(0x00); jpeg.Add(0x00); // Thumbnail

        // SOF0
        jpeg.AddRange(new byte[] { 0xFF, 0xC0 });
        jpeg.AddRange(new byte[] { 0x00, 0x11 });
        jpeg.Add(0x08);
        jpeg.Add((byte)((height >> 8) & 0xFF));
        jpeg.Add((byte)(height & 0xFF));
        jpeg.Add((byte)((width >> 8) & 0xFF));
        jpeg.Add((byte)(width & 0xFF));
        jpeg.Add(0x03);
        jpeg.Add(0x01); jpeg.Add(0x22); jpeg.Add(0x00);
        jpeg.Add(0x02); jpeg.Add(0x11); jpeg.Add(0x01);
        jpeg.Add(0x03); jpeg.Add(0x11); jpeg.Add(0x01);

        // SOS
        jpeg.AddRange(new byte[] { 0xFF, 0xDA });
        jpeg.AddRange(new byte[] { 0x00, 0x0C });
        jpeg.Add(0x03);
        jpeg.Add(0x01); jpeg.Add(0x00);
        jpeg.Add(0x02); jpeg.Add(0x11);
        jpeg.Add(0x03); jpeg.Add(0x11);
        jpeg.Add(0x00); jpeg.Add(0x3F); jpeg.Add(0x00);

        jpeg.AddRange(new byte[] { 0xFF, 0x00 });

        // EOI
        jpeg.AddRange(new byte[] { 0xFF, 0xD9 });

        return jpeg.ToArray();
    }
}
