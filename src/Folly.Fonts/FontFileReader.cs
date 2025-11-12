using System;
using System.IO;
using Folly.Fonts.Models;

namespace Folly.Fonts;

/// <summary>
/// Low-level reader for font file structure and table directory.
/// This class handles the offset table and table directory parsing.
/// </summary>
public class FontFileReader
{
    /// <summary>
    /// Reads the table directory from a font file stream.
    /// </summary>
    /// <param name="stream">Stream containing the font data.</param>
    /// <returns>Parsed table directory.</returns>
    public static TableDirectory ReadTableDirectory(Stream stream)
    {
        using var reader = new BigEndianBinaryReader(stream, leaveOpen: true);

        var directory = new TableDirectory();

        // Read offset table (first 12 bytes)
        directory.SfntVersion = reader.ReadUInt32();
        directory.NumTables = reader.ReadUInt16();

        // Skip searchRange, entrySelector, rangeShift (used for binary search optimization)
        reader.Skip(6);

        // Validate font type
        if (!IsValidFontType(directory.SfntVersion))
        {
            throw new InvalidDataException(
                $"Invalid font type: 0x{directory.SfntVersion:X8}. " +
                "Expected TrueType (0x00010000) or OpenType/CFF ('OTTO').");
        }

        // Read table records (16 bytes each)
        for (int i = 0; i < directory.NumTables; i++)
        {
            var record = new TableRecord
            {
                Tag = reader.ReadTag(),
                CheckSum = reader.ReadUInt32(),
                Offset = reader.ReadUInt32(),
                Length = reader.ReadUInt32()
            };

            directory.Tables[record.Tag] = record;
        }

        return directory;
    }

    /// <summary>
    /// Checks if the sfnt version indicates a valid TrueType or OpenType font.
    /// </summary>
    private static bool IsValidFontType(uint sfntVersion)
    {
        return sfntVersion == 0x00010000  // TrueType with TrueType outlines
            || sfntVersion == 0x4F54544F  // 'OTTO' - OpenType with CFF outlines
            || sfntVersion == 0x74727565  // 'true' - Apple TrueType
            || sfntVersion == 0x74797031; // 'typ1' - Type 1
    }

    /// <summary>
    /// Gets the font type description based on sfnt version.
    /// </summary>
    public static string GetFontTypeDescription(uint sfntVersion)
    {
        return sfntVersion switch
        {
            0x00010000 => "TrueType",
            0x4F54544F => "OpenType (CFF)",
            0x74727565 => "Apple TrueType",
            0x74797031 => "Type 1",
            _ => $"Unknown (0x{sfntVersion:X8})"
        };
    }

    /// <summary>
    /// Reads raw table data from a font file stream.
    /// </summary>
    /// <param name="stream">Stream containing the font data.</param>
    /// <param name="table">Table record to read.</param>
    /// <returns>Byte array containing the table data.</returns>
    public static byte[] ReadTableData(Stream stream, TableRecord table)
    {
        using var reader = new BigEndianBinaryReader(stream, leaveOpen: true);
        reader.Seek(table.Offset);
        return reader.ReadBytes((int)table.Length);
    }

    /// <summary>
    /// Creates a reader positioned at the start of a specific table.
    /// </summary>
    /// <param name="stream">Stream containing the font data.</param>
    /// <param name="table">Table record to position at.</param>
    /// <returns>BigEndianBinaryReader positioned at the table start.</returns>
    public static BigEndianBinaryReader CreateTableReader(Stream stream, TableRecord table)
    {
        var reader = new BigEndianBinaryReader(stream, leaveOpen: true);
        reader.Seek(table.Offset);
        return reader;
    }
}
