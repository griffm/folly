using Folly.Images;
using Folly.Images.Parsers;
using Xunit;

namespace Folly.UnitTests;

public class BmpParserTests
{
    [Fact]
    public void BmpParser_CanParse_ValidBmpData_ReturnsTrue()
    {
        // Arrange
        var bmpData = CreateSimpleBmp24(2, 2);
        var parser = new BmpParser();

        // Act
        bool canParse = parser.CanParse(bmpData);

        // Assert
        Assert.True(canParse);
    }

    [Fact]
    public void BmpParser_CanParse_InvalidData_ReturnsFalse()
    {
        // Arrange
        var invalidData = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG signature
        var parser = new BmpParser();

        // Act
        bool canParse = parser.CanParse(invalidData);

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void BmpParser_Parse_SimpleBmp24_ReturnsCorrectInfo()
    {
        // Arrange
        var bmpData = CreateSimpleBmp24(2, 2);
        var parser = new BmpParser();

        // Act
        var info = parser.Parse(bmpData);

        // Assert
        Assert.Equal("BMP", info.Format);
        Assert.Equal(2, info.Width);
        Assert.Equal(2, info.Height);
        Assert.Equal("DeviceRGB", info.ColorSpace);
        Assert.Equal(3, info.ColorComponents);
        Assert.Equal(8, info.BitsPerComponent);
        Assert.NotNull(info.RawData);
        Assert.Null(info.AlphaData); // 24-bit BMP has no alpha
    }

    [Fact]
    public void BmpParser_Parse_Bmp32WithAlpha_ExtractsAlphaChannel()
    {
        // Arrange
        var bmpData = CreateSimpleBmp32(2, 2);
        var parser = new BmpParser();

        // Act
        var info = parser.Parse(bmpData);

        // Assert
        Assert.Equal("BMP", info.Format);
        Assert.Equal(2, info.Width);
        Assert.Equal(2, info.Height);
        Assert.Equal("DeviceRGB", info.ColorSpace);
        Assert.NotNull(info.AlphaData); // 32-bit BMP should have alpha channel
    }

    [Fact]
    public void BmpParser_Parse_BmpWithDpi_ExtractsDpiCorrectly()
    {
        // Arrange
        var bmpData = CreateBmpWithDpi(2, 2, 300, 300); // 300 DPI
        var parser = new BmpParser();

        // Act
        var info = parser.Parse(bmpData);

        // Assert
        Assert.True(info.HorizontalDpi > 290 && info.HorizontalDpi < 310); // Allow for rounding
        Assert.True(info.VerticalDpi > 290 && info.VerticalDpi < 310);
    }

    [Fact]
    public void ImageFormatDetector_Detect_BmpSignature_ReturnsBMP()
    {
        // Arrange
        var bmpData = CreateSimpleBmp24(2, 2);

        // Act
        string format = ImageFormatDetector.Detect(bmpData);

        // Assert
        Assert.Equal("BMP", format);
    }

    // Helper method to create a minimal 24-bit BMP for testing
    private static byte[] CreateSimpleBmp24(int width, int height)
    {
        // Calculate row stride (must be multiple of 4)
        int rowStride = ((width * 3 + 3) / 4) * 4;
        int pixelDataSize = rowStride * height;
        int fileSize = 54 + pixelDataSize; // 14 (file header) + 40 (DIB header) + pixel data

        var bmp = new byte[fileSize];

        // BMP File Header (14 bytes)
        bmp[0] = 0x42; // 'B'
        bmp[1] = 0x4D; // 'M'
        WriteInt32LE(bmp, 2, fileSize);
        WriteInt32LE(bmp, 10, 54); // Data offset

        // BITMAPINFOHEADER (40 bytes)
        WriteInt32LE(bmp, 14, 40); // DIB header size
        WriteInt32LE(bmp, 18, width);
        WriteInt32LE(bmp, 22, height); // Positive = bottom-up
        WriteInt16LE(bmp, 26, 1); // Planes
        WriteInt16LE(bmp, 28, 24); // Bits per pixel
        WriteInt32LE(bmp, 30, 0); // Compression (BI_RGB)
        WriteInt32LE(bmp, 34, pixelDataSize);
        WriteInt32LE(bmp, 38, 0); // X pixels per meter
        WriteInt32LE(bmp, 42, 0); // Y pixels per meter

        // Pixel data (BGR format, bottom-up)
        // Fill with simple pattern: Red, Green, Blue, White
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = 54 + y * rowStride + x * 3;
                if ((x + y) % 2 == 0)
                {
                    bmp[offset] = 0xFF; // Blue
                    bmp[offset + 1] = 0x00; // Green
                    bmp[offset + 2] = 0x00; // Red
                }
                else
                {
                    bmp[offset] = 0x00; // Blue
                    bmp[offset + 1] = 0xFF; // Green
                    bmp[offset + 2] = 0x00; // Red
                }
            }
        }

        return bmp;
    }

    // Helper method to create a minimal 32-bit BMP with alpha channel
    private static byte[] CreateSimpleBmp32(int width, int height)
    {
        int rowStride = width * 4; // 32 bits = 4 bytes per pixel, no padding needed
        int pixelDataSize = rowStride * height;
        int fileSize = 54 + pixelDataSize;

        var bmp = new byte[fileSize];

        // BMP File Header
        bmp[0] = 0x42; // 'B'
        bmp[1] = 0x4D; // 'M'
        WriteInt32LE(bmp, 2, fileSize);
        WriteInt32LE(bmp, 10, 54); // Data offset

        // BITMAPINFOHEADER
        WriteInt32LE(bmp, 14, 40); // DIB header size
        WriteInt32LE(bmp, 18, width);
        WriteInt32LE(bmp, 22, height);
        WriteInt16LE(bmp, 26, 1); // Planes
        WriteInt16LE(bmp, 28, 32); // Bits per pixel
        WriteInt32LE(bmp, 30, 0); // Compression (BI_RGB)
        WriteInt32LE(bmp, 34, pixelDataSize);

        // Pixel data (BGRA format)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = 54 + y * rowStride + x * 4;
                bmp[offset] = 0xFF; // Blue
                bmp[offset + 1] = 0x00; // Green
                bmp[offset + 2] = 0x00; // Red
                bmp[offset + 3] = 0x80; // Alpha (50% transparent)
            }
        }

        return bmp;
    }

    // Helper method to create BMP with specific DPI
    private static byte[] CreateBmpWithDpi(int width, int height, int dpiX, int dpiY)
    {
        var bmp = CreateSimpleBmp24(width, height);

        // Convert DPI to pixels per meter
        int xPixelsPerMeter = (int)(dpiX / 0.0254);
        int yPixelsPerMeter = (int)(dpiY / 0.0254);

        // Write pixels per meter to DIB header
        WriteInt32LE(bmp, 38, xPixelsPerMeter);
        WriteInt32LE(bmp, 42, yPixelsPerMeter);

        return bmp;
    }

    private static void WriteInt32LE(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
        data[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static void WriteInt16LE(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
    }
}
