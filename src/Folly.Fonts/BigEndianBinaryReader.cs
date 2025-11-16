using System;
using System.IO;
using System.Text;

namespace Folly.Fonts;

/// <summary>
/// Binary reader for big-endian data, as required by TrueType and OpenType font files.
/// All multi-byte values in font files are stored in big-endian (network) byte order.
/// </summary>
public class BigEndianBinaryReader : IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    /// <summary>
    /// Initializes a new instance of the BigEndianBinaryReader class.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="leaveOpen">True to leave the stream open after disposing the reader.</param>
    public BigEndianBinaryReader(Stream stream, bool leaveOpen = false)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Gets the current position in the stream.
    /// </summary>
    public long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    /// <summary>
    /// Gets the length of the stream.
    /// </summary>
    public long Length => _stream.Length;

    /// <summary>
    /// Seeks to a specific position in the stream.
    /// </summary>
    public void Seek(long offset, SeekOrigin origin = SeekOrigin.Begin)
    {
        _stream.Seek(offset, origin);
    }

    /// <summary>
    /// Reads a single byte.
    /// </summary>
    public byte ReadByte()
    {
        int b = _stream.ReadByte();
        if (b == -1)
            throw new EndOfStreamException();
        return (byte)b;
    }

    /// <summary>
    /// Reads a signed 8-bit integer (FWORD in OpenType spec).
    /// </summary>
    public sbyte ReadInt8()
    {
        return (sbyte)ReadByte();
    }

    /// <summary>
    /// Reads an unsigned 8-bit integer (BYTE in OpenType spec).
    /// </summary>
    public byte ReadUInt8()
    {
        return ReadByte();
    }

    /// <summary>
    /// Reads a signed 16-bit integer in big-endian format (SHORT in OpenType spec).
    /// </summary>
    public short ReadInt16()
    {
        byte b1 = ReadByte();
        byte b2 = ReadByte();
        return (short)((b1 << 8) | b2);
    }

    /// <summary>
    /// Reads an unsigned 16-bit integer in big-endian format (USHORT in OpenType spec).
    /// </summary>
    public ushort ReadUInt16()
    {
        byte b1 = ReadByte();
        byte b2 = ReadByte();
        return (ushort)((b1 << 8) | b2);
    }

    /// <summary>
    /// Reads a signed 32-bit integer in big-endian format (LONG in OpenType spec).
    /// </summary>
    public int ReadInt32()
    {
        byte b1 = ReadByte();
        byte b2 = ReadByte();
        byte b3 = ReadByte();
        byte b4 = ReadByte();
        return (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
    }

    /// <summary>
    /// Reads an unsigned 32-bit integer in big-endian format (ULONG in OpenType spec).
    /// </summary>
    public uint ReadUInt32()
    {
        byte b1 = ReadByte();
        byte b2 = ReadByte();
        byte b3 = ReadByte();
        byte b4 = ReadByte();
        return (uint)((b1 << 24) | (b2 << 16) | (b3 << 8) | b4);
    }

    /// <summary>
    /// Reads a signed 64-bit integer in big-endian format (LONGDATETIME in OpenType spec).
    /// </summary>
    public long ReadInt64()
    {
        byte b1 = ReadByte();
        byte b2 = ReadByte();
        byte b3 = ReadByte();
        byte b4 = ReadByte();
        byte b5 = ReadByte();
        byte b6 = ReadByte();
        byte b7 = ReadByte();
        byte b8 = ReadByte();
        return ((long)b1 << 56) | ((long)b2 << 48) | ((long)b3 << 40) | ((long)b4 << 32) |
               ((long)b5 << 24) | ((long)b6 << 16) | ((long)b7 << 8) | b8;
    }

    /// <summary>
    /// Reads a 32-bit fixed-point number (16.16 format) in big-endian format (Fixed in OpenType spec).
    /// </summary>
    public double ReadFixed()
    {
        int value = ReadInt32();
        return value / 65536.0;
    }

    /// <summary>
    /// Reads a 16-bit fixed-point number (2.14 format) in big-endian format (F2DOT14 in OpenType spec).
    /// </summary>
    public double ReadF2Dot14()
    {
        short value = ReadInt16();
        return value / 16384.0;
    }

    /// <summary>
    /// Reads a 4-character tag as a string (TAG in OpenType spec).
    /// Tags are used to identify tables and features.
    /// </summary>
    public string ReadTag()
    {
        byte[] bytes = ReadBytes(4);
        return Encoding.ASCII.GetString(bytes);
    }

    /// <summary>
    /// Reads a specified number of bytes.
    /// </summary>
    public byte[] ReadBytes(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        byte[] buffer = new byte[count];
        int totalRead = 0;

        while (totalRead < count)
        {
            int read = _stream.Read(buffer, totalRead, count - totalRead);
            if (read == 0)
                throw new EndOfStreamException();
            totalRead += read;
        }

        return buffer;
    }

    /// <summary>
    /// Reads a null-terminated ASCII string.
    /// </summary>
    public string ReadNullTerminatedString()
    {
        var bytes = new System.Collections.Generic.List<byte>();
        byte b;
        while ((b = ReadByte()) != 0)
        {
            bytes.Add(b);
        }
        return Encoding.ASCII.GetString(bytes.ToArray());
    }

    /// <summary>
    /// Skips a specified number of bytes.
    /// </summary>
    public void Skip(int count)
    {
        _stream.Seek(count, SeekOrigin.Current);
    }

    /// <summary>
    /// Disposes the reader and optionally the underlying stream.
    /// </summary>
    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }
}
