using Folly.Images;
using Folly.Images.Parsers;
using Xunit;

namespace Folly.Images.Tests;

public class PngParserTests
{
    [Fact]
    public void PngParser_CanParse_ValidPngData_ReturnsTrue()
    {
        // Arrange
        var pngData = CreateSimplePngRgb(2, 2);
        var parser = new PngParser();

        // Act
        bool canParse = parser.CanParse(pngData);

        // Assert
        Assert.True(canParse);
    }

    [Fact]
    public void PngParser_CanParse_InvalidData_ReturnsFalse()
    {
        // Arrange
        var invalidData = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG signature
        var parser = new PngParser();

        // Act
        bool canParse = parser.CanParse(invalidData);

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void PngParser_CanParse_EmptyData_ReturnsFalse()
    {
        // Arrange
        var parser = new PngParser();

        // Act
        bool canParse = parser.CanParse(Array.Empty<byte>());

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void PngParser_CanParse_NullData_ReturnsFalse()
    {
        // Arrange
        var parser = new PngParser();

        // Act
        bool canParse = parser.CanParse(null!);

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void PngParser_Parse_SimplePngRgb_ReturnsCorrectInfo()
    {
        // Arrange
        var pngData = CreateSimplePngRgb(100, 50);
        var parser = new PngParser();

        // Act
        var info = parser.Parse(pngData);

        // Assert
        Assert.Equal("PNG", info.Format);
        Assert.Equal(100, info.Width);
        Assert.Equal(50, info.Height);
        Assert.Equal("DeviceRGB", info.ColorSpace);
        Assert.Equal(3, info.ColorComponents);
        Assert.Equal(8, info.BitsPerComponent);
        Assert.NotNull(info.RawData);
    }

    [Fact]
    public void PngParser_Parse_PngGrayscale_ReturnsGrayscaleColorSpace()
    {
        // Arrange
        var pngData = CreatePngGrayscale(50, 50);
        var parser = new PngParser();

        // Act
        var info = parser.Parse(pngData);

        // Assert
        Assert.Equal("PNG", info.Format);
        Assert.Equal(50, info.Width);
        Assert.Equal(50, info.Height);
        Assert.Equal("DeviceGray", info.ColorSpace);
        Assert.Equal(1, info.ColorComponents);
    }

    [Fact]
    public void PngParser_Parse_PngIndexed_ReturnsIndexedColorSpace()
    {
        // Arrange
        var pngData = CreatePngIndexed(50, 50);
        var parser = new PngParser();

        // Act
        var info = parser.Parse(pngData);

        // Assert
        Assert.Equal("PNG", info.Format);
        Assert.Equal("Indexed", info.ColorSpace);
        Assert.Equal(1, info.ColorComponents);
        Assert.NotNull(info.Palette);
    }

    [Fact]
    public void PngParser_Parse_PngRgba_ReturnsRgbColorSpace()
    {
        // Arrange
        var pngData = CreatePngRgba(50, 50);
        var parser = new PngParser();

        // Act
        var info = parser.Parse(pngData);

        // Assert
        Assert.Equal("PNG", info.Format);
        Assert.Equal("DeviceRGB", info.ColorSpace);
        Assert.Equal(3, info.ColorComponents);
    }

    [Fact]
    public void PngParser_Parse_PngGrayscaleAlpha_ReturnsGrayColorSpace()
    {
        // Arrange
        var pngData = CreatePngGrayscaleAlpha(50, 50);
        var parser = new PngParser();

        // Act
        var info = parser.Parse(pngData);

        // Assert
        Assert.Equal("PNG", info.Format);
        Assert.Equal("DeviceGray", info.ColorSpace);
        Assert.Equal(1, info.ColorComponents);
    }

    [Fact]
    public void PngParser_Parse_PngWithTransparency_ExtractsTransparency()
    {
        // Arrange
        var pngData = CreatePngWithTransparency(50, 50);
        var parser = new PngParser();

        // Act
        var info = parser.Parse(pngData);

        // Assert
        Assert.NotNull(info.Transparency);
    }

    [Fact]
    public void PngParser_Parse_PngWithDpi_ExtractsDpiCorrectly()
    {
        // Arrange
        var pngData = CreatePngWithDpi(50, 50, 300, 300);
        var parser = new PngParser();

        // Act
        var info = parser.Parse(pngData);

        // Assert
        Assert.True(info.HorizontalDpi > 295 && info.HorizontalDpi < 305); // Allow for rounding
        Assert.True(info.VerticalDpi > 295 && info.VerticalDpi < 305);
    }

    [Fact]
    public void PngParser_Parse_PngWithoutDpi_UsesDefaultDpi()
    {
        // Arrange
        var pngData = CreateSimplePngRgb(50, 50);
        var parser = new PngParser();

        // Act
        var info = parser.Parse(pngData);

        // Assert
        Assert.Equal(72, info.HorizontalDpi);
        Assert.Equal(72, info.VerticalDpi);
    }

    [Fact]
    public void PngParser_Parse_Png16BitDepth_Returns16BitDepth()
    {
        // Arrange
        var pngData = CreatePng16Bit(50, 50);
        var parser = new PngParser();

        // Act
        var info = parser.Parse(pngData);

        // Assert
        Assert.Equal(16, info.BitsPerComponent);
    }

    [Fact]
    public void PngParser_Parse_InvalidBitDepth_ThrowsException()
    {
        // Arrange
        var pngData = CreatePngWithInvalidBitDepth(50, 50, 7); // Invalid bit depth
        var parser = new PngParser();

        // Act & Assert
        var exception = Assert.Throws<InvalidDataException>(() => parser.Parse(pngData));
        Assert.Contains("bit depth", exception.Message.ToLower());
    }

    [Fact]
    public void PngParser_Parse_InvalidColorType_ThrowsException()
    {
        // Arrange
        var pngData = CreatePngWithInvalidColorType(50, 50, 5); // Invalid color type
        var parser = new PngParser();

        // Act & Assert
        var exception = Assert.Throws<InvalidDataException>(() => parser.Parse(pngData));
        Assert.Contains("color type", exception.Message.ToLower());
    }

    [Fact]
    public void PngParser_Parse_InvalidSignature_ThrowsException()
    {
        // Arrange
        var invalidData = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG signature
        var parser = new PngParser();

        // Act & Assert
        var exception = Assert.Throws<InvalidDataException>(() => parser.Parse(invalidData));
        Assert.Contains("Invalid PNG signature", exception.Message);
    }

    [Fact]
    public void PngParser_Parse_PngWithoutDimensions_ThrowsException()
    {
        // Arrange
        var pngData = new List<byte>();
        // PNG signature
        pngData.AddRange(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        // IEND chunk only (no IHDR)
        WriteChunk(pngData, "IEND", Array.Empty<byte>());
        var parser = new PngParser();

        // Act & Assert
        var exception = Assert.Throws<InvalidDataException>(() => parser.Parse(pngData.ToArray()));
        Assert.Contains("invalid dimensions", exception.Message);
    }

    [Fact]
    public void PngParser_FormatName_ReturnsPng()
    {
        // Arrange
        var parser = new PngParser();

        // Act
        string formatName = parser.FormatName;

        // Assert
        Assert.Equal("PNG", formatName);
    }

    [Fact]
    public void ImageFormatDetector_Detect_PngSignature_ReturnsPNG()
    {
        // Arrange
        var pngData = CreateSimplePngRgb(10, 10);

        // Act
        string format = ImageFormatDetector.Detect(pngData);

        // Assert
        Assert.Equal("PNG", format);
    }

    // Helper methods to create PNG test data

    private static byte[] CreateSimplePngRgb(int width, int height)
    {
        return CreatePng(width, height, 8, 2, null, null); // Color type 2 = RGB
    }

    private static byte[] CreatePngGrayscale(int width, int height)
    {
        return CreatePng(width, height, 8, 0, null, null); // Color type 0 = Grayscale
    }

    private static byte[] CreatePngIndexed(int width, int height)
    {
        // Create a simple palette (4 colors)
        var palette = new byte[]
        {
            0xFF, 0x00, 0x00, // Red
            0x00, 0xFF, 0x00, // Green
            0x00, 0x00, 0xFF, // Blue
            0xFF, 0xFF, 0xFF  // White
        };
        return CreatePng(width, height, 8, 3, palette, null); // Color type 3 = Indexed
    }

    private static byte[] CreatePngRgba(int width, int height)
    {
        return CreatePng(width, height, 8, 6, null, null); // Color type 6 = RGBA
    }

    private static byte[] CreatePngGrayscaleAlpha(int width, int height)
    {
        return CreatePng(width, height, 8, 4, null, null); // Color type 4 = Grayscale + Alpha
    }

    private static byte[] CreatePngWithTransparency(int width, int height)
    {
        var transparency = new byte[] { 0xFF, 0xFF }; // Transparent color
        return CreatePng(width, height, 8, 2, null, transparency);
    }

    private static byte[] CreatePngWithDpi(int width, int height, int dpiX, int dpiY)
    {
        // Convert DPI to pixels per meter
        int pixelsPerMeterX = (int)(dpiX * 39.3701);
        int pixelsPerMeterY = (int)(dpiY * 39.3701);

        var png = new List<byte>();

        // PNG signature
        png.AddRange(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // IHDR chunk
        var ihdr = CreateIhdrData(width, height, 8, 2);
        WriteChunk(png, "IHDR", ihdr);

        // pHYs chunk (physical dimensions)
        var phys = new List<byte>();
        WriteInt32BE(phys, pixelsPerMeterX);
        WriteInt32BE(phys, pixelsPerMeterY);
        phys.Add(1); // Unit specifier: meters
        WriteChunk(png, "pHYs", phys.ToArray());

        // IDAT chunk (minimal compressed data)
        WriteChunk(png, "IDAT", CreateMinimalIdatData(width, height, 2));

        // IEND chunk
        WriteChunk(png, "IEND", Array.Empty<byte>());

        return png.ToArray();
    }

    private static byte[] CreatePng16Bit(int width, int height)
    {
        return CreatePng(width, height, 16, 2, null, null); // 16-bit RGB
    }

    private static byte[] CreatePngWithInvalidBitDepth(int width, int height, int bitDepth)
    {
        return CreatePng(width, height, bitDepth, 2, null, null);
    }

    private static byte[] CreatePngWithInvalidColorType(int width, int height, int colorType)
    {
        return CreatePng(width, height, 8, colorType, null, null);
    }

    private static byte[] CreatePng(int width, int height, int bitDepth, int colorType, byte[]? palette, byte[]? transparency)
    {
        var png = new List<byte>();

        // PNG signature
        png.AddRange(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // IHDR chunk
        var ihdr = CreateIhdrData(width, height, bitDepth, colorType);
        WriteChunk(png, "IHDR", ihdr);

        // PLTE chunk (if indexed color)
        if (palette != null)
        {
            WriteChunk(png, "PLTE", palette);
        }

        // tRNS chunk (if transparency)
        if (transparency != null)
        {
            WriteChunk(png, "tRNS", transparency);
        }

        // IDAT chunk (minimal compressed data)
        WriteChunk(png, "IDAT", CreateMinimalIdatData(width, height, colorType));

        // IEND chunk
        WriteChunk(png, "IEND", Array.Empty<byte>());

        return png.ToArray();
    }

    private static byte[] CreateIhdrData(int width, int height, int bitDepth, int colorType)
    {
        var ihdr = new List<byte>();
        WriteInt32BE(ihdr, width);
        WriteInt32BE(ihdr, height);
        ihdr.Add((byte)bitDepth);
        ihdr.Add((byte)colorType);
        ihdr.Add(0); // Compression method
        ihdr.Add(0); // Filter method
        ihdr.Add(0); // Interlace method
        return ihdr.ToArray();
    }

    private static byte[] CreateMinimalIdatData(int width, int height, int colorType)
    {
        // Calculate bytes per pixel
        int bytesPerPixel = colorType switch
        {
            0 => 1,  // Grayscale
            2 => 3,  // RGB
            3 => 1,  // Indexed
            4 => 2,  // Grayscale + Alpha
            6 => 4,  // RGBA
            _ => 3
        };

        // Create scanlines (filter type byte + pixel data)
        var scanlines = new List<byte>();
        for (int y = 0; y < height; y++)
        {
            scanlines.Add(0); // Filter type: None
            for (int x = 0; x < width * bytesPerPixel; x++)
            {
                scanlines.Add(0); // Pixel data (all zeros)
            }
        }

        // Compress with zlib
        using var outputStream = new MemoryStream();
        using (var zlibStream = new System.IO.Compression.ZLibStream(outputStream, System.IO.Compression.CompressionMode.Compress))
        {
            zlibStream.Write(scanlines.ToArray(), 0, scanlines.Count);
        }
        return outputStream.ToArray();
    }

    private static void WriteChunk(List<byte> png, string type, byte[] data)
    {
        // Chunk length
        WriteInt32BE(png, data.Length);

        // Chunk type
        png.AddRange(System.Text.Encoding.ASCII.GetBytes(type));

        // Chunk data
        png.AddRange(data);

        // CRC (simplified - just use zeros for test data)
        WriteInt32BE(png, (int)CalculateCrc(type, data));
    }

    private static uint CalculateCrc(string type, byte[] data)
    {
        // Simplified CRC calculation for testing
        // In production PNG files, this should be a proper CRC32
        var crcData = new List<byte>();
        crcData.AddRange(System.Text.Encoding.ASCII.GetBytes(type));
        crcData.AddRange(data);

        uint crc = 0xFFFFFFFF;
        foreach (byte b in crcData)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ 0xEDB88320;
                else
                    crc >>= 1;
            }
        }
        return crc ^ 0xFFFFFFFF;
    }

    private static void WriteInt32BE(List<byte> data, int value)
    {
        data.Add((byte)((value >> 24) & 0xFF));
        data.Add((byte)((value >> 16) & 0xFF));
        data.Add((byte)((value >> 8) & 0xFF));
        data.Add((byte)(value & 0xFF));
    }
}
