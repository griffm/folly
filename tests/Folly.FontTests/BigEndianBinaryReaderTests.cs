using System.IO;
using System.Text;
using Xunit;

namespace Folly.Fonts.Tests;

public class BigEndianBinaryReaderTests
{
    [Fact]
    public void ReadUInt16_BigEndian_ReturnsCorrectValue()
    {
        // 0x1234 in big-endian: [0x12, 0x34]
        byte[] data = { 0x12, 0x34 };
        using var stream = new MemoryStream(data);
        using var reader = new BigEndianBinaryReader(stream);

        ushort value = reader.ReadUInt16();

        Assert.Equal(0x1234, value);
    }

    [Fact]
    public void ReadInt16_BigEndian_ReturnsCorrectValue()
    {
        // -1000 (0xFC18) in big-endian: [0xFC, 0x18]
        byte[] data = { 0xFC, 0x18 };
        using var stream = new MemoryStream(data);
        using var reader = new BigEndianBinaryReader(stream);

        short value = reader.ReadInt16();

        Assert.Equal(-1000, value);
    }

    [Fact]
    public void ReadUInt32_BigEndian_ReturnsCorrectValue()
    {
        // 0x12345678 in big-endian: [0x12, 0x34, 0x56, 0x78]
        byte[] data = { 0x12, 0x34, 0x56, 0x78 };
        using var stream = new MemoryStream(data);
        using var reader = new BigEndianBinaryReader(stream);

        uint value = reader.ReadUInt32();

        Assert.Equal(0x12345678u, value);
    }

    [Fact]
    public void ReadFixed_ReturnsCorrectValue()
    {
        // 1.5 in Fixed format (16.16): 1.5 * 65536 = 98304 (0x00018000)
        // Big-endian: [0x00, 0x01, 0x80, 0x00]
        byte[] data = { 0x00, 0x01, 0x80, 0x00 };
        using var stream = new MemoryStream(data);
        using var reader = new BigEndianBinaryReader(stream);

        double value = reader.ReadFixed();

        Assert.Equal(1.5, value, precision: 5);
    }

    [Fact]
    public void ReadF2Dot14_ReturnsCorrectValue()
    {
        // 1.5 in F2DOT14 format: 1.5 * 16384 = 24576 (0x6000)
        // Big-endian: [0x60, 0x00]
        byte[] data = { 0x60, 0x00 };
        using var stream = new MemoryStream(data);
        using var reader = new BigEndianBinaryReader(stream);

        double value = reader.ReadF2Dot14();

        Assert.Equal(1.5, value, precision: 4);
    }

    [Fact]
    public void ReadTag_ReturnsCorrectString()
    {
        // Tag "head" as ASCII bytes
        byte[] data = Encoding.ASCII.GetBytes("head");
        using var stream = new MemoryStream(data);
        using var reader = new BigEndianBinaryReader(stream);

        string tag = reader.ReadTag();

        Assert.Equal("head", tag);
    }

    [Fact]
    public void Position_ReturnsAndSetsCorrectly()
    {
        byte[] data = new byte[100];
        using var stream = new MemoryStream(data);
        using var reader = new BigEndianBinaryReader(stream);

        Assert.Equal(0, reader.Position);

        reader.Position = 50;
        Assert.Equal(50, reader.Position);
    }

    [Fact]
    public void Seek_MovesToCorrectPosition()
    {
        byte[] data = new byte[100];
        using var stream = new MemoryStream(data);
        using var reader = new BigEndianBinaryReader(stream);

        reader.Seek(25);
        Assert.Equal(25, reader.Position);

        reader.Seek(10, SeekOrigin.Current);
        Assert.Equal(35, reader.Position);
    }

    [Fact]
    public void Skip_AdvancesPosition()
    {
        byte[] data = new byte[100];
        using var stream = new MemoryStream(data);
        using var reader = new BigEndianBinaryReader(stream);

        reader.Skip(20);
        Assert.Equal(20, reader.Position);
    }
}
