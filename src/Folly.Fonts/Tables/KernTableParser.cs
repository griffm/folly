using System;
using System.IO;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'kern' (kerning) table.
/// This table contains kerning pairs that adjust spacing between specific glyph combinations.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/kern
/// Note: The 'kern' table is being superseded by the GPOS table in OpenType, but is still widely used.
/// </summary>
public static class KernTableParser
{
    /// <summary>
    /// Parses the 'kern' table and populates the kerning pairs in the font file.
    /// This is an optional table, so missing kern data is not an error.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);

        // Version (uint16) - should be 0
        ushort version = reader.ReadUInt16();

        if (version != 0)
        {
            // Some fonts use the newer "kern" table format (not the OpenType one)
            // For now, we'll skip unsupported versions
            return;
        }

        // Number of subtables (uint16)
        ushort numTables = reader.ReadUInt16();

        // Process each subtable
        for (int i = 0; i < numTables; i++)
        {
            ParseSubtable(reader, font);
        }
    }

    private static void ParseSubtable(BigEndianBinaryReader reader, FontFile font)
    {
        long subtableStart = reader.Position;

        // Version (uint16) - should be 0
        ushort version = reader.ReadUInt16();

        // Length (uint16) - length of this subtable in bytes
        ushort length = reader.ReadUInt16();

        // Coverage (uint16) - describes what type of information is in this table
        ushort coverage = reader.ReadUInt16();

        // Extract coverage flags
        bool horizontal = (coverage & 0x0001) != 0;     // Bit 0: 1 = horizontal kerning
        bool minimum = (coverage & 0x0002) != 0;        // Bit 1: 1 = minimum values
        bool crossStream = (coverage & 0x0004) != 0;    // Bit 2: 1 = cross-stream
        bool override_ = (coverage & 0x0008) != 0;      // Bit 3: 1 = override accumulated value

        // Bits 8-15: Format of the subtable
        byte format = (byte)((coverage >> 8) & 0xFF);

        // We only support format 0 (the most common)
        // Format 0: Ordered list of kerning pairs
        if (format == 0 && horizontal && !crossStream)
        {
            ParseFormat0(reader, font, override_);
        }
        else
        {
            // Skip unsupported subtable formats
            long bytesRead = reader.Position - subtableStart;
            long bytesToSkip = length - bytesRead;
            if (bytesToSkip > 0)
            {
                reader.Skip((int)bytesToSkip);
            }
        }
    }

    /// <summary>
    /// Parses format 0 kerning subtable (ordered list of kerning pairs).
    /// </summary>
    private static void ParseFormat0(BigEndianBinaryReader reader, FontFile font, bool override_)
    {
        // nPairs (uint16) - number of kerning pairs
        ushort nPairs = reader.ReadUInt16();

        // searchRange (uint16) - largest power of 2 <= nPairs
        reader.Skip(2);

        // entrySelector (uint16) - log2 of largest power of 2 <= nPairs
        reader.Skip(2);

        // rangeShift (uint16) - nPairs - searchRange
        reader.Skip(2);

        // Read kerning pairs
        for (int i = 0; i < nPairs; i++)
        {
            // Left glyph index (uint16)
            ushort leftGlyphIndex = reader.ReadUInt16();

            // Right glyph index (uint16)
            ushort rightGlyphIndex = reader.ReadUInt16();

            // Kerning value (int16) - in font units
            short value = reader.ReadInt16();

            // Store the kerning pair
            var key = (leftGlyphIndex, rightGlyphIndex);

            if (override_)
            {
                // Override mode: replace any existing value
                font.KerningPairs[key] = value;
            }
            else
            {
                // Accumulate mode: add to existing value
                if (font.KerningPairs.TryGetValue(key, out short existingValue))
                {
                    font.KerningPairs[key] = (short)(existingValue + value);
                }
                else
                {
                    font.KerningPairs[key] = value;
                }
            }
        }
    }
}
