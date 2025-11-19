using Folly.Images;
using Folly.Images.Parsers;
using Xunit;

namespace Folly.Images.Tests;

public class GifParserTests
{
    [Fact]
    public void GifParser_CanParse_ValidGif89a_ReturnsTrue()
    {
        // Arrange
        var gifData = CreateSimpleGif89a(2, 2);
        var parser = new GifParser();

        // Act
        bool canParse = parser.CanParse(gifData);

        // Assert
        Assert.True(canParse);
    }

    [Fact]
    public void GifParser_CanParse_ValidGif87a_ReturnsTrue()
    {
        // Arrange
        var gifData = CreateSimpleGif87a(2, 2);
        var parser = new GifParser();

        // Act
        bool canParse = parser.CanParse(gifData);

        // Assert
        Assert.True(canParse);
    }

    [Fact]
    public void GifParser_CanParse_InvalidData_ReturnsFalse()
    {
        // Arrange
        var invalidData = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG signature
        var parser = new GifParser();

        // Act
        bool canParse = parser.CanParse(invalidData);

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void GifParser_Parse_SimpleGif_ReturnsCorrectInfo()
    {
        // Arrange
        var gifData = CreateSimpleGif89a(2, 2);
        var parser = new GifParser();

        // Act
        var info = parser.Parse(gifData);

        // Assert
        Assert.Equal("GIF", info.Format);
        Assert.Equal(2, info.Width);
        Assert.Equal(2, info.Height);
        Assert.Equal("DeviceRGB", info.ColorSpace);
        Assert.Equal(3, info.ColorComponents);
        Assert.Equal(8, info.BitsPerComponent);
        Assert.NotNull(info.RawData);
        Assert.NotNull(info.Palette);
    }

    [Fact]
    public void ImageFormatDetector_Detect_GifSignature_ReturnsGIF()
    {
        // Arrange
        var gifData = CreateSimpleGif89a(2, 2);

        // Act
        string format = ImageFormatDetector.Detect(gifData);

        // Assert
        Assert.Equal("GIF", format);
    }

    [Fact]
    public void GifParser_CanParse_EmptyData_ReturnsFalse()
    {
        // Arrange
        var parser = new GifParser();

        // Act
        bool canParse = parser.CanParse(Array.Empty<byte>());

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void GifParser_CanParse_NullData_ReturnsFalse()
    {
        // Arrange
        var parser = new GifParser();

        // Act
        bool canParse = parser.CanParse(null!);

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void GifParser_Parse_InvalidGifSignature_ThrowsException()
    {
        // Arrange
        var invalidData = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG signature
        var parser = new GifParser();

        // Act & Assert
        var exception = Assert.Throws<InvalidDataException>(() => parser.Parse(invalidData));
        Assert.Contains("Invalid GIF", exception.Message);
    }

    [Fact]
    public void GifParser_FormatName_ReturnsGif()
    {
        // Arrange
        var parser = new GifParser();

        // Act
        string formatName = parser.FormatName;

        // Assert
        Assert.Equal("GIF", formatName);
    }

    [Fact]
    public void GifParser_Parse_LargerGif_ReturnsCorrectDimensions()
    {
        // Arrange
        var gifData = CreateSimpleGif89a(100, 50);
        var parser = new GifParser();

        // Act
        var info = parser.Parse(gifData);

        // Assert
        Assert.Equal(100, info.Width);
        Assert.Equal(50, info.Height);
    }

    [Fact]
    public void ImageFormatDetector_Detect_Gif87aSignature_ReturnsGIF()
    {
        // Arrange
        var gifData = CreateSimpleGif87a(2, 2);

        // Act
        string format = ImageFormatDetector.Detect(gifData);

        // Assert
        Assert.Equal("GIF", format);
    }

    // Helper method to create a minimal GIF89a for testing
    // Creates a 2x2 image with 4 colors (red, green, blue, white)
    private static byte[] CreateSimpleGif89a(int width, int height)
    {
        var gif = new List<byte>();

        // Header: "GIF89a"
        gif.AddRange(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }); // "GIF89a"

        // Logical Screen Descriptor
        gif.Add((byte)(width & 0xFF));         // Width LSB
        gif.Add((byte)((width >> 8) & 0xFF));  // Width MSB
        gif.Add((byte)(height & 0xFF));        // Height LSB
        gif.Add((byte)((height >> 8) & 0xFF)); // Height MSB
        gif.Add(0xF1);  // Flags: global color table, 4 colors (2^(1+1)=4)
        gif.Add(0x00);  // Background color index
        gif.Add(0x00);  // Aspect ratio

        // Global Color Table (4 colors * 3 bytes RGB = 12 bytes)
        // Color 0: Red
        gif.AddRange(new byte[] { 0xFF, 0x00, 0x00 });
        // Color 1: Green
        gif.AddRange(new byte[] { 0x00, 0xFF, 0x00 });
        // Color 2: Blue
        gif.AddRange(new byte[] { 0x00, 0x00, 0xFF });
        // Color 3: White
        gif.AddRange(new byte[] { 0xFF, 0xFF, 0xFF });

        // Image Descriptor
        gif.Add(0x2C);  // Image separator
        gif.Add(0x00); gif.Add(0x00); // Left position = 0
        gif.Add(0x00); gif.Add(0x00); // Top position = 0
        gif.Add((byte)(width & 0xFF));         // Width LSB
        gif.Add((byte)((width >> 8) & 0xFF));  // Width MSB
        gif.Add((byte)(height & 0xFF));        // Height LSB
        gif.Add((byte)((height >> 8) & 0xFF)); // Height MSB
        gif.Add(0x00);  // Flags: no local color table, not interlaced

        // Image Data (LZW compressed)
        gif.Add(0x02);  // LZW minimum code size (2 bits for 4 colors)

        // LZW compressed data for 2x2 pixels: [0, 1, 2, 3]
        // This is a simple pattern: clear code (4), then 0, 1, 2, 3, EOI (5)
        // Encoded in variable-length codes starting at 3 bits
        byte[] lzwData = EncodeLzwSimple(new byte[] { 0, 1, 2, 3 }, 2);

        // Write LZW data in sub-blocks (max 255 bytes per block)
        int lzwOffset = 0;
        while (lzwOffset < lzwData.Length)
        {
            int blockSize = Math.Min(255, lzwData.Length - lzwOffset);
            gif.Add((byte)blockSize);
            gif.AddRange(lzwData.Skip(lzwOffset).Take(blockSize));
            lzwOffset += blockSize;
        }

        gif.Add(0x00);  // Block terminator

        // Trailer
        gif.Add(0x3B);  // GIF trailer

        return gif.ToArray();
    }

    private static byte[] CreateSimpleGif87a(int width, int height)
    {
        var gif = new List<byte>();

        // Header: "GIF87a"
        gif.AddRange(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }); // "GIF87a"

        // Rest is the same as GIF89a for our simple test
        gif.Add((byte)(width & 0xFF));
        gif.Add((byte)((width >> 8) & 0xFF));
        gif.Add((byte)(height & 0xFF));
        gif.Add((byte)((height >> 8) & 0xFF));
        gif.Add(0xF1);
        gif.Add(0x00);
        gif.Add(0x00);

        // Global Color Table (4 colors)
        gif.AddRange(new byte[] { 0xFF, 0x00, 0x00 }); // Red
        gif.AddRange(new byte[] { 0x00, 0xFF, 0x00 }); // Green
        gif.AddRange(new byte[] { 0x00, 0x00, 0xFF }); // Blue
        gif.AddRange(new byte[] { 0xFF, 0xFF, 0xFF }); // White

        // Image Descriptor
        gif.Add(0x2C);
        gif.Add(0x00); gif.Add(0x00);
        gif.Add(0x00); gif.Add(0x00);
        gif.Add((byte)(width & 0xFF));
        gif.Add((byte)((width >> 8) & 0xFF));
        gif.Add((byte)(height & 0xFF));
        gif.Add((byte)((height >> 8) & 0xFF));
        gif.Add(0x00);

        // Image Data
        gif.Add(0x02);  // LZW minimum code size

        byte[] lzwData = EncodeLzwSimple(new byte[] { 0, 1, 2, 3 }, 2);

        int lzwOffset = 0;
        while (lzwOffset < lzwData.Length)
        {
            int blockSize = Math.Min(255, lzwData.Length - lzwOffset);
            gif.Add((byte)blockSize);
            gif.AddRange(lzwData.Skip(lzwOffset).Take(blockSize));
            lzwOffset += blockSize;
        }

        gif.Add(0x00);  // Block terminator

        // Trailer
        gif.Add(0x3B);

        return gif.ToArray();
    }

    // Simple LZW encoder for test data generation
    // Encodes: clear_code, data[0], data[1], ..., eoi_code
    private static byte[] EncodeLzwSimple(byte[] data, int minimumCodeSize)
    {
        int clearCode = 1 << minimumCodeSize;
        int eoiCode = clearCode + 1;
        int codeSize = minimumCodeSize + 1;

        var bits = new List<int>();

        // Start with clear code
        bits.Add(clearCode);

        // Add each pixel value
        foreach (byte pixel in data)
        {
            bits.Add(pixel);
        }

        // End with EOI code
        bits.Add(eoiCode);

        // Pack bits into bytes
        var bytes = new List<byte>();
        int currentByte = 0;
        int bitPosition = 0;

        foreach (int code in bits)
        {
            for (int i = 0; i < codeSize; i++)
            {
                int bit = (code >> i) & 1;
                currentByte |= bit << bitPosition;
                bitPosition++;

                if (bitPosition == 8)
                {
                    bytes.Add((byte)currentByte);
                    currentByte = 0;
                    bitPosition = 0;
                }
            }
        }

        // Add any remaining bits
        if (bitPosition > 0)
        {
            bytes.Add((byte)currentByte);
        }

        return bytes.ToArray();
    }
}
