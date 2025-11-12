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
        int hMetricsToRead = Math.Min(numberOfHMetrics, glyphCount);
        for (int i = 0; i < hMetricsToRead; i++)
        {
            font.GlyphAdvanceWidths[i] = reader.ReadUInt16();
            font.GlyphLeftSideBearings[i] = reader.ReadInt16();
        }

        // For remaining glyphs, use the last advance width
        // but read individual left side bearings
        // Note: If numberOfHMetrics >= glyphCount, there are no additional LSBs to read
        if (numberOfHMetrics < glyphCount && numberOfHMetrics > 0)
        {
            ushort lastAdvanceWidth = font.GlyphAdvanceWidths[numberOfHMetrics - 1];

            // Calculate how many additional LSBs we should read
            // The hmtx table should contain exactly (glyphCount - numberOfHMetrics) additional LSBs
            long startPosition = table.Offset + (numberOfHMetrics * 4); // After hMetrics array
            long expectedLsbBytes = (glyphCount - numberOfHMetrics) * 2;
            long actualTableEnd = table.Offset + table.Length;
            long actualLsbBytes = actualTableEnd - startPosition;

            // Read only as many LSBs as are actually present in the table
            int lsbCount = Math.Min(glyphCount - numberOfHMetrics, (int)(actualLsbBytes / 2));

            for (int i = 0; i < lsbCount; i++)
            {
                font.GlyphAdvanceWidths[numberOfHMetrics + i] = lastAdvanceWidth;
                font.GlyphLeftSideBearings[numberOfHMetrics + i] = reader.ReadInt16();
            }

            // For any remaining glyphs beyond what's in the table (malformed fonts),
            // use the last values we have
            if (numberOfHMetrics + lsbCount < glyphCount)
            {
                short lastLsb = lsbCount > 0
                    ? font.GlyphLeftSideBearings[numberOfHMetrics + lsbCount - 1]
                    : (short)0;

                for (int i = numberOfHMetrics + lsbCount; i < glyphCount; i++)
                {
                    font.GlyphAdvanceWidths[i] = lastAdvanceWidth;
                    font.GlyphLeftSideBearings[i] = lastLsb;
                }
            }
        }
        else if (numberOfHMetrics >= glyphCount)
        {
            // All glyphs have full hMetrics entries, nothing more to do
        }
        else
        {
            // numberOfHMetrics is 0 (malformed font)
            // Arrays are already initialized to zero
        }
    }
}
