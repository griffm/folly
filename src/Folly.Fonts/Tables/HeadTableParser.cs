using System;
using System.IO;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'head' (font header) table.
/// This table contains global font information including units per em and bounding box.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/head
/// </summary>
public static class HeadTableParser
{
    /// <summary>
    /// Parses the 'head' table and populates the font file with header information.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);

        // Version (Fixed) - should be 1.0
        double version = reader.ReadFixed();
        if (Math.Abs(version - 1.0) > 0.001)
        {
            throw new InvalidDataException($"Unsupported 'head' table version: {version}");
        }

        // Font revision (Fixed)
        reader.Skip(4);

        // Checksum adjustment (uint32) - for whole font file validation
        reader.Skip(4);

        // Magic number (uint32) - should be 0x5F0F3CF5
        uint magicNumber = reader.ReadUInt32();
        if (magicNumber != 0x5F0F3CF5)
        {
            throw new InvalidDataException($"Invalid magic number in 'head' table: 0x{magicNumber:X8}");
        }

        // Flags (uint16)
        reader.Skip(2);

        // Units per em (uint16)
        font.UnitsPerEm = reader.ReadUInt16();

        // Valid range is 16 to 16384
        if (font.UnitsPerEm < 16 || font.UnitsPerEm > 16384)
        {
            throw new InvalidDataException($"Invalid unitsPerEm: {font.UnitsPerEm}");
        }

        // Created (longDateTime) - 8 bytes
        reader.Skip(8);

        // Modified (longDateTime) - 8 bytes
        reader.Skip(8);

        // Bounding box
        font.XMin = reader.ReadInt16();
        font.YMin = reader.ReadInt16();
        font.XMax = reader.ReadInt16();
        font.YMax = reader.ReadInt16();

        // Mac style (uint16)
        reader.Skip(2);

        // Lowest recommended PPEM (uint16)
        reader.Skip(2);

        // Font direction hint (int16)
        reader.Skip(2);

        // Index to location format (int16)
        // 0 for short offsets (Offset16), 1 for long offsets (Offset32)
        font.IndexToLocFormat = reader.ReadInt16();

        if (font.IndexToLocFormat != 0 && font.IndexToLocFormat != 1)
        {
            throw new InvalidDataException($"Invalid indexToLocFormat: {font.IndexToLocFormat}");
        }

        // Glyph data format (int16) - should be 0
        short glyphDataFormat = reader.ReadInt16();
        if (glyphDataFormat != 0)
        {
            throw new InvalidDataException($"Unsupported glyphDataFormat: {glyphDataFormat}");
        }
    }
}
