using System;
using System.IO;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'hmtx' (horizontal metrics) table.
/// This table contains horizontal advance widths and left side bearings for each glyph.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/hmtx
/// </summary>
public static class HmtxTableParser
{
    /// <summary>
    /// Parses the 'hmtx' table and populates the font file with glyph metrics.
    /// Requires 'hhea' and 'maxp' tables to be parsed first.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);

        ushort numberOfHMetrics = HheaTableParser.NumberOfHMetrics;
        ushort glyphCount = font.GlyphCount;

        // Initialize arrays
        font.GlyphAdvanceWidths = new ushort[glyphCount];
        font.GlyphLeftSideBearings = new short[glyphCount];

        // Read hMetrics array (numberOfHMetrics entries)
        // Each entry is 4 bytes: advanceWidth (uint16) + lsb (int16)
        for (int i = 0; i < numberOfHMetrics; i++)
        {
            font.GlyphAdvanceWidths[i] = reader.ReadUInt16();
            font.GlyphLeftSideBearings[i] = reader.ReadInt16();
        }

        // For remaining glyphs, use the last advance width
        // but read individual left side bearings
        ushort lastAdvanceWidth = numberOfHMetrics > 0
            ? font.GlyphAdvanceWidths[numberOfHMetrics - 1]
            : (ushort)0;

        for (int i = numberOfHMetrics; i < glyphCount; i++)
        {
            font.GlyphAdvanceWidths[i] = lastAdvanceWidth;
            font.GlyphLeftSideBearings[i] = reader.ReadInt16();
        }
    }
}
