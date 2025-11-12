using System;
using System.IO;
using System.Text;

namespace Folly.Fonts;

/// <summary>
/// Binary writer for big-endian (network byte order) data.
/// TrueType/OpenType fonts store all multi-byte data in big-endian format.
/// </summary>
public class BigEndianBinaryWriter : IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private bool _disposed;

    /// <summary>
    /// Creates a new BigEndianBinaryWriter for the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="leaveOpen">True to leave the stream open after disposing; otherwise, false.</param>
    public BigEndianBinaryWriter(Stream stream, bool leaveOpen = false)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Gets the current position in the stream.
    /// </summary>
    public long Position => _stream.Position;

    /// <summary>
    /// Writes a byte to the stream.
    /// </summary>
    public void WriteByte(byte value)
    {
        _stream.WriteByte(value);
    }

    /// <summary>
    /// Writes a signed byte to the stream.
    /// </summary>
    public void WriteSByte(sbyte value)
    {
        _stream.WriteByte((byte)value);
    }

    /// <summary>
    /// Writes a 16-bit unsigned integer in big-endian format.
    /// </summary>
    public void WriteUInt16(ushort value)
    {
        _stream.WriteByte((byte)(value >> 8));
        _stream.WriteByte((byte)value);
    }

    /// <summary>
    /// Writes a 16-bit signed integer in big-endian format.
    /// </summary>
    public void WriteInt16(short value)
    {
        WriteUInt16((ushort)value);
    }

    /// <summary>
    /// Writes a 32-bit unsigned integer in big-endian format.
    /// </summary>
    public void WriteUInt32(uint value)
    {
        _stream.WriteByte((byte)(value >> 24));
        _stream.WriteByte((byte)(value >> 16));
        _stream.WriteByte((byte)(value >> 8));
        _stream.WriteByte((byte)value);
    }

    /// <summary>
    /// Writes a 32-bit signed integer in big-endian format.
    /// </summary>
    public void WriteInt32(int value)
    {
        WriteUInt32((uint)value);
    }

    /// <summary>
    /// Writes a 64-bit unsigned integer in big-endian format.
    /// </summary>
    public void WriteUInt64(ulong value)
    {
        WriteUInt32((uint)(value >> 32));
        WriteUInt32((uint)value);
    }

    /// <summary>
    /// Writes a 64-bit signed integer in big-endian format.
    /// </summary>
    public void WriteInt64(long value)
    {
        WriteUInt64((ulong)value);
    }

    /// <summary>
    /// Writes a byte array to the stream.
    /// </summary>
    public void WriteBytes(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));

        _stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Writes a fixed-length ASCII string (padded with zeros if needed).
    /// Used for table tags and other fixed-size strings in TrueType fonts.
    /// </summary>
    public void WriteFixedString(string value, int length)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        byte[] bytes = new byte[length];
        int bytesToCopy = Math.Min(value.Length, length);
        Encoding.ASCII.GetBytes(value, 0, bytesToCopy, bytes, 0);

        _stream.Write(bytes, 0, length);
    }

    /// <summary>
    /// Writes padding bytes (zeros) to align to a specified boundary.
    /// TrueType tables must be aligned to 4-byte boundaries.
    /// </summary>
    public void WritePadding(int alignTo = 4)
    {
        long position = _stream.Position;
        long remainder = position % alignTo;

        if (remainder != 0)
        {
            int paddingBytes = alignTo - (int)remainder;
            for (int i = 0; i < paddingBytes; i++)
            {
                _stream.WriteByte(0);
            }
        }
    }

    /// <summary>
    /// Seeks to a specific position in the stream.
    /// </summary>
    public void Seek(long position)
    {
        _stream.Seek(position, SeekOrigin.Begin);
    }

    /// <summary>
    /// Disposes the writer and optionally the underlying stream.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (!_leaveOpen)
        {
            _stream?.Dispose();
        }

        _disposed = true;
    }
}
