using System;
using System.IO;
using Folly.Fonts.Models;
using Xunit;

namespace Folly.Fonts.Tests;

public class FontFileReaderTests
{
    [Fact]
    public void ReadTableDirectory_ValidTrueTypeFont_ReturnsCorrectDirectory()
    {
        // Create a minimal valid TrueType font header
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Offset table
        writer.Write(SwapEndianness(0x00010000u)); // sfntVersion (TrueType)
        writer.Write(SwapEndianness((ushort)2));   // numTables
        writer.Write(SwapEndianness((ushort)32));  // searchRange
        writer.Write(SwapEndianness((ushort)1));   // entrySelector
        writer.Write(SwapEndianness((ushort)0));   // rangeShift

        // Table record 1: "head"
        writer.Write(System.Text.Encoding.ASCII.GetBytes("head"));
        writer.Write(SwapEndianness(0x12345678u)); // checkSum
        writer.Write(SwapEndianness(100u));        // offset
        writer.Write(SwapEndianness(54u));         // length

        // Table record 2: "maxp"
        writer.Write(System.Text.Encoding.ASCII.GetBytes("maxp"));
        writer.Write(SwapEndianness(0xABCDEF00u)); // checkSum
        writer.Write(SwapEndianness(200u));        // offset
        writer.Write(SwapEndianness(32u));         // length

        stream.Position = 0;

        var directory = FontFileReader.ReadTableDirectory(stream);

        Assert.Equal(0x00010000u, directory.SfntVersion);
        Assert.Equal(2, directory.NumTables);
        Assert.True(directory.HasTable("head"));
        Assert.True(directory.HasTable("maxp"));

        var headTable = directory.GetTable("head");
        Assert.NotNull(headTable);
        Assert.Equal("head", headTable!.Tag);
        Assert.Equal(100u, headTable.Offset);
        Assert.Equal(54u, headTable.Length);

        var maxpTable = directory.GetTable("maxp");
        Assert.NotNull(maxpTable);
        Assert.Equal("maxp", maxpTable!.Tag);
        Assert.Equal(200u, maxpTable.Offset);
        Assert.Equal(32u, maxpTable.Length);
    }

    [Fact]
    public void ReadTableDirectory_InvalidSfntVersion_ThrowsException()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Invalid sfnt version
        writer.Write(SwapEndianness(0xDEADBEEFu));
        writer.Write(SwapEndianness((ushort)0));

        stream.Position = 0;

        Assert.Throws<InvalidDataException>(() =>
            FontFileReader.ReadTableDirectory(stream));
    }

    [Fact]
    public void GetFontTypeDescription_ReturnsCorrectDescriptions()
    {
        Assert.Equal("TrueType", FontFileReader.GetFontTypeDescription(0x00010000));
        Assert.Equal("OpenType (CFF)", FontFileReader.GetFontTypeDescription(0x4F54544F));
        Assert.Equal("Apple TrueType", FontFileReader.GetFontTypeDescription(0x74727565));
        Assert.Contains("Unknown", FontFileReader.GetFontTypeDescription(0xDEADBEEF));
    }

    // Helper method to swap endianness for test data
    private static uint SwapEndianness(uint value)
    {
        return ((value & 0x000000FFu) << 24) |
               ((value & 0x0000FF00u) << 8) |
               ((value & 0x00FF0000u) >> 8) |
               ((value & 0xFF000000u) >> 24);
    }

    private static ushort SwapEndianness(ushort value)
    {
        return (ushort)(((value & 0x00FFu) << 8) | ((value & 0xFF00u) >> 8));
    }
}
