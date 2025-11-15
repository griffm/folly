using Folly.Images;
using Folly.Images.Parsers;
using Xunit;

namespace Folly.UnitTests;

public class TiffParserTests
{
    [Fact]
    public void TiffParser_CanParse_ValidTiffLittleEndian_ReturnsTrue()
    {
        // Arrange
        var tiffData = CreateSimpleTiffLE(2, 2);
        var parser = new TiffParser();

        // Act
        bool canParse = parser.CanParse(tiffData);

        // Assert
        Assert.True(canParse);
    }

    [Fact]
    public void TiffParser_CanParse_ValidTiffBigEndian_ReturnsTrue()
    {
        // Arrange
        var tiffData = CreateSimpleTiffBE(2, 2);
        var parser = new TiffParser();

        // Act
        bool canParse = parser.CanParse(tiffData);

        // Assert
        Assert.True(canParse);
    }

    [Fact]
    public void TiffParser_CanParse_InvalidData_ReturnsFalse()
    {
        // Arrange
        var invalidData = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG signature
        var parser = new TiffParser();

        // Act
        bool canParse = parser.CanParse(invalidData);

        // Assert
        Assert.False(canParse);
    }

    [Fact]
    public void TiffParser_Parse_SimpleTiffLE_ReturnsCorrectInfo()
    {
        // Arrange
        var tiffData = CreateSimpleTiffLE(2, 2);
        var parser = new TiffParser();

        // Act
        var info = parser.Parse(tiffData);

        // Assert
        Assert.Equal("TIFF", info.Format);
        Assert.Equal(2, info.Width);
        Assert.Equal(2, info.Height);
        Assert.Equal("DeviceRGB", info.ColorSpace);
        Assert.Equal(3, info.ColorComponents);
        Assert.Equal(8, info.BitsPerComponent);
        Assert.NotNull(info.RawData);
    }

    [Fact]
    public void TiffParser_Parse_SimpleTiffBE_ReturnsCorrectInfo()
    {
        // Arrange
        var tiffData = CreateSimpleTiffBE(2, 2);
        var parser = new TiffParser();

        // Act
        var info = parser.Parse(tiffData);

        // Assert
        Assert.Equal("TIFF", info.Format);
        Assert.Equal(2, info.Width);
        Assert.Equal(2, info.Height);
        Assert.Equal("DeviceRGB", info.ColorSpace);
    }

    [Fact]
    public void ImageFormatDetector_Detect_TiffSignature_ReturnsTIFF()
    {
        // Arrange
        var tiffData = CreateSimpleTiffLE(2, 2);

        // Act
        string format = ImageFormatDetector.Detect(tiffData);

        // Assert
        Assert.Equal("TIFF", format);
    }

    // Helper method to create a minimal TIFF (little-endian) for testing
    private static byte[] CreateSimpleTiffLE(int width, int height)
    {
        var tiff = new List<byte>();

        // Calculate offsets
        int ifdOffset = 8; // IFD starts at offset 8
        int pixelDataOffset = ifdOffset + 2 + (8 * 12) + 4; // After IFD

        // TIFF Header (8 bytes)
        tiff.AddRange(new byte[] { 0x49, 0x49 }); // "II" - little-endian
        tiff.AddRange(new byte[] { 0x2A, 0x00 }); // Magic number 42
        WriteUInt32LE(tiff, (uint)ifdOffset); // Offset to first IFD

        // IFD (Image File Directory) at offset 8
        // Number of directory entries
        WriteUInt16LE(tiff, 8); // 8 tags

        // Tag 256: ImageWidth = 2
        WriteTiffEntry(tiff, 256, 3, 1, (uint)width); // Type 3 = SHORT
        // Tag 257: ImageLength = 2
        WriteTiffEntry(tiff, 257, 3, 1, (uint)height);
        // Tag 258: BitsPerSample = 8 (for RGB)
        WriteTiffEntry(tiff, 258, 3, 1, 8);
        // Tag 259: Compression = 1 (no compression)
        WriteTiffEntry(tiff, 259, 3, 1, 1);
        // Tag 262: PhotometricInterpretation = 2 (RGB)
        WriteTiffEntry(tiff, 262, 3, 1, 2);
        // Tag 273: StripOffsets = pixelDataOffset
        WriteTiffEntry(tiff, 273, 4, 1, (uint)pixelDataOffset); // Type 4 = LONG
        // Tag 277: SamplesPerPixel = 3 (RGB)
        WriteTiffEntry(tiff, 277, 3, 1, 3);
        // Tag 279: StripByteCounts = 12 (2x2 * 3 bytes)
        WriteTiffEntry(tiff, 279, 4, 1, 12);

        // Offset to next IFD (0 = no more IFDs)
        WriteUInt32LE(tiff, 0);

        // Pixel data (RGB, 2x2 = 4 pixels * 3 bytes = 12 bytes)
        // Red, Green, Blue, White
        tiff.AddRange(new byte[] { 0xFF, 0x00, 0x00 }); // Red
        tiff.AddRange(new byte[] { 0x00, 0xFF, 0x00 }); // Green
        tiff.AddRange(new byte[] { 0x00, 0x00, 0xFF }); // Blue
        tiff.AddRange(new byte[] { 0xFF, 0xFF, 0xFF }); // White

        return tiff.ToArray();
    }

    // Helper method to create a minimal TIFF (big-endian) for testing
    private static byte[] CreateSimpleTiffBE(int width, int height)
    {
        var tiff = new List<byte>();

        // Calculate offsets
        int ifdOffset = 8;
        int pixelDataOffset = ifdOffset + 2 + (8 * 12) + 4;

        // TIFF Header (8 bytes)
        tiff.AddRange(new byte[] { 0x4D, 0x4D }); // "MM" - big-endian
        tiff.AddRange(new byte[] { 0x00, 0x2A }); // Magic number 42
        WriteUInt32BE(tiff, (uint)ifdOffset); // Offset to first IFD

        WriteUInt16BE(tiff, 8); // 8 tags

        WriteTiffEntryBE(tiff, 256, 3, 1, (uint)width);
        WriteTiffEntryBE(tiff, 257, 3, 1, (uint)height);
        WriteTiffEntryBE(tiff, 258, 3, 1, 8);
        WriteTiffEntryBE(tiff, 259, 3, 1, 1);
        WriteTiffEntryBE(tiff, 262, 3, 1, 2);
        WriteTiffEntryBE(tiff, 273, 4, 1, (uint)pixelDataOffset);
        WriteTiffEntryBE(tiff, 277, 3, 1, 3);
        WriteTiffEntryBE(tiff, 279, 4, 1, 12);

        WriteUInt32BE(tiff, 0); // Next IFD offset

        // Pixel data
        tiff.AddRange(new byte[] { 0xFF, 0x00, 0x00 }); // Red
        tiff.AddRange(new byte[] { 0x00, 0xFF, 0x00 }); // Green
        tiff.AddRange(new byte[] { 0x00, 0x00, 0xFF }); // Blue
        tiff.AddRange(new byte[] { 0xFF, 0xFF, 0xFF }); // White

        return tiff.ToArray();
    }

    private static void WriteTiffEntry(List<byte> data, ushort tag, ushort type, uint count, uint value)
    {
        WriteUInt16LE(data, tag);
        WriteUInt16LE(data, type);
        WriteUInt32LE(data, count);
        WriteUInt32LE(data, value);
    }

    private static void WriteTiffEntryBE(List<byte> data, ushort tag, ushort type, uint count, uint value)
    {
        WriteUInt16BE(data, tag);
        WriteUInt16BE(data, type);
        WriteUInt32BE(data, count);
        WriteUInt32BE(data, value);
    }

    private static void WriteUInt16LE(List<byte> data, int value)
    {
        data.Add((byte)(value & 0xFF));
        data.Add((byte)((value >> 8) & 0xFF));
    }

    private static void WriteUInt32LE(List<byte> data, uint value)
    {
        data.Add((byte)(value & 0xFF));
        data.Add((byte)((value >> 8) & 0xFF));
        data.Add((byte)((value >> 16) & 0xFF));
        data.Add((byte)((value >> 24) & 0xFF));
    }

    private static void WriteUInt16BE(List<byte> data, int value)
    {
        data.Add((byte)((value >> 8) & 0xFF));
        data.Add((byte)(value & 0xFF));
    }

    private static void WriteUInt32BE(List<byte> data, uint value)
    {
        data.Add((byte)((value >> 24) & 0xFF));
        data.Add((byte)((value >> 16) & 0xFF));
        data.Add((byte)((value >> 8) & 0xFF));
        data.Add((byte)(value & 0xFF));
    }
}
