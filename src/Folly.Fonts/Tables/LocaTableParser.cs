using System;
using System.IO;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'loca' (index to location) table.
/// This table stores offsets to glyph data in the 'glyf' table.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/loca
/// </summary>
public static class LocaTableParser
{
    /// <summary>
    /// Parses the 'loca' table and populates glyph offsets.
    /// Requires 'head' and 'maxp' tables to be parsed first.
    /// Only applicable for TrueType fonts (not CFF fonts).
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        if (!font.IsTrueType)
        {
            // CFF fonts don't have a 'loca' table
            return;
        }

        using var reader = FontFileReader.CreateTableReader(stream, table);

        ushort glyphCount = font.GlyphCount;
        short indexToLocFormat = font.IndexToLocFormat;

        // The 'loca' table has numGlyphs + 1 entries
        // The last entry marks the end of the last glyph's data
        font.GlyphOffsets = new uint[glyphCount + 1];

        if (indexToLocFormat == 0)
        {
            // Short format (Offset16): offsets are stored as uint16, multiplied by 2
            for (int i = 0; i <= glyphCount; i++)
            {
                font.GlyphOffsets[i] = (uint)(reader.ReadUInt16() * 2);
            }
        }
        else if (indexToLocFormat == 1)
        {
            // Long format (Offset32): offsets are stored as uint32
            for (int i = 0; i <= glyphCount; i++)
            {
                font.GlyphOffsets[i] = reader.ReadUInt32();
            }
        }
        else
        {
            throw new InvalidDataException($"Invalid indexToLocFormat: {indexToLocFormat}");
        }
    }

    /// <summary>
    /// Gets the offset and length of glyph data for a specific glyph index.
    /// Returns (offset, length) or null if the glyph has no data (empty glyph).
    /// </summary>
    public static (uint offset, uint length)? GetGlyphDataLocation(FontFile font, ushort glyphIndex)
    {
        if (font.GlyphOffsets == null || glyphIndex >= font.GlyphCount)
            return null;

        uint offset = font.GlyphOffsets[glyphIndex];
        uint nextOffset = font.GlyphOffsets[glyphIndex + 1];

        uint length = nextOffset - offset;

        if (length == 0)
            return null; // Empty glyph (e.g., space character)

        return (offset, length);
    }
}
