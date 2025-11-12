using System;
using System.Collections.Generic;
using System.IO;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'cmap' (character to glyph mapping) table.
/// This table maps Unicode code points to glyph indices.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/cmap
/// </summary>
public static class CmapTableParser
{
    // Platform IDs
    private const ushort PlatformUnicode = 0;
    private const ushort PlatformWindows = 3;

    // Unicode platform encoding IDs
    private const ushort UnicodeEncodingUnicode2_0 = 3;
    private const ushort UnicodeEncodingUnicodeFull = 4;

    // Windows platform encoding IDs
    private const ushort WindowsEncodingUnicodeBmp = 1;
    private const ushort WindowsEncodingUnicodeFull = 10;

    /// <summary>
    /// Parses the 'cmap' table and populates the character-to-glyph mapping.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);

        long tableStart = reader.Position;

        // Version (uint16) - should be 0
        ushort version = reader.ReadUInt16();
        if (version != 0)
        {
            throw new InvalidDataException($"Unsupported 'cmap' table version: {version}");
        }

        // Number of encoding tables (uint16)
        ushort numTables = reader.ReadUInt16();

        // Read encoding records (8 bytes each)
        var encodingRecords = new List<EncodingRecord>();
        for (int i = 0; i < numTables; i++)
        {
            encodingRecords.Add(new EncodingRecord
            {
                PlatformId = reader.ReadUInt16(),
                EncodingId = reader.ReadUInt16(),
                Offset = reader.ReadUInt32()
            });
        }

        // Find the best subtable to use
        // Priority: Windows Unicode BMP > Unicode 2.0 > Windows Unicode Full > Unicode Full
        EncodingRecord? bestRecord = null;

        // Try Windows Unicode BMP (most common)
        bestRecord = FindEncodingRecord(encodingRecords, PlatformWindows, WindowsEncodingUnicodeBmp);

        // Try Unicode 2.0
        if (bestRecord == null)
            bestRecord = FindEncodingRecord(encodingRecords, PlatformUnicode, UnicodeEncodingUnicode2_0);

        // Try Windows Unicode Full
        if (bestRecord == null)
            bestRecord = FindEncodingRecord(encodingRecords, PlatformWindows, WindowsEncodingUnicodeFull);

        // Try Unicode Full
        if (bestRecord == null)
            bestRecord = FindEncodingRecord(encodingRecords, PlatformUnicode, UnicodeEncodingUnicodeFull);

        // Use any Unicode platform encoding
        if (bestRecord == null)
            bestRecord = encodingRecords.Find(r => r.PlatformId == PlatformUnicode);

        if (bestRecord == null)
        {
            throw new InvalidDataException("No suitable Unicode cmap subtable found");
        }

        // Parse the selected subtable
        reader.Seek(tableStart + bestRecord.Offset);
        ParseSubtable(reader, font);
    }

    private static EncodingRecord? FindEncodingRecord(
        List<EncodingRecord> records,
        ushort platformId,
        ushort encodingId)
    {
        return records.Find(r => r.PlatformId == platformId && r.EncodingId == encodingId);
    }

    private static void ParseSubtable(BigEndianBinaryReader reader, FontFile font)
    {
        long subtableStart = reader.Position;

        // Format (uint16)
        ushort format = reader.ReadUInt16();

        switch (format)
        {
            case 0:
                ParseFormat0(reader, font);
                break;
            case 4:
                ParseFormat4(reader, font);
                break;
            case 12:
                ParseFormat12(reader, font);
                break;
            default:
                throw new NotSupportedException($"cmap format {format} is not supported");
        }
    }

    /// <summary>
    /// Parses cmap format 0 (byte encoding table).
    /// Simple 1-to-1 mapping for 8-bit character codes.
    /// </summary>
    private static void ParseFormat0(BigEndianBinaryReader reader, FontFile font)
    {
        // Length (uint16)
        reader.Skip(2);

        // Language (uint16)
        reader.Skip(2);

        // Glyph ID array (256 bytes)
        for (int i = 0; i < 256; i++)
        {
            byte glyphId = reader.ReadByte();
            if (glyphId != 0)
            {
                font.CharacterToGlyphIndex[i] = glyphId;
            }
        }
    }

    /// <summary>
    /// Parses cmap format 4 (segment mapping to delta values).
    /// Most common format for Unicode BMP (U+0000 to U+FFFF).
    /// </summary>
    private static void ParseFormat4(BigEndianBinaryReader reader, FontFile font)
    {
        // Length (uint16)
        reader.Skip(2);

        // Language (uint16)
        reader.Skip(2);

        // segCountX2 (uint16)
        ushort segCountX2 = reader.ReadUInt16();
        ushort segCount = (ushort)(segCountX2 / 2);

        // searchRange (uint16)
        reader.Skip(2);

        // entrySelector (uint16)
        reader.Skip(2);

        // rangeShift (uint16)
        reader.Skip(2);

        // Read arrays
        var endCode = new ushort[segCount];
        for (int i = 0; i < segCount; i++)
            endCode[i] = reader.ReadUInt16();

        // reservedPad (uint16)
        reader.Skip(2);

        var startCode = new ushort[segCount];
        for (int i = 0; i < segCount; i++)
            startCode[i] = reader.ReadUInt16();

        var idDelta = new short[segCount];
        for (int i = 0; i < segCount; i++)
            idDelta[i] = reader.ReadInt16();

        var idRangeOffsetPos = reader.Position;
        var idRangeOffset = new ushort[segCount];
        for (int i = 0; i < segCount; i++)
            idRangeOffset[i] = reader.ReadUInt16();

        // Process segments
        for (int i = 0; i < segCount; i++)
        {
            ushort start = startCode[i];
            ushort end = endCode[i];
            short delta = idDelta[i];
            ushort rangeOffset = idRangeOffset[i];

            // Skip the final segment (0xFFFF)
            if (start == 0xFFFF && end == 0xFFFF)
                continue;

            for (int codePoint = start; codePoint <= end; codePoint++)
            {
                ushort glyphIndex;

                if (rangeOffset == 0)
                {
                    // Simple delta mapping
                    glyphIndex = (ushort)((codePoint + delta) & 0xFFFF);
                }
                else
                {
                    // Use glyphIdArray
                    long offset = idRangeOffsetPos + (i * 2) + rangeOffset + ((codePoint - start) * 2);
                    long savedPos = reader.Position;
                    reader.Seek(offset);
                    glyphIndex = reader.ReadUInt16();
                    reader.Seek(savedPos);

                    if (glyphIndex != 0)
                    {
                        glyphIndex = (ushort)((glyphIndex + delta) & 0xFFFF);
                    }
                }

                if (glyphIndex != 0)
                {
                    font.CharacterToGlyphIndex[codePoint] = glyphIndex;
                }
            }
        }
    }

    /// <summary>
    /// Parses cmap format 12 (segmented coverage).
    /// Supports full Unicode range (beyond BMP, up to U+10FFFF).
    /// </summary>
    private static void ParseFormat12(BigEndianBinaryReader reader, FontFile font)
    {
        // Reserved (uint16) - should be 0
        reader.Skip(2);

        // Length (uint32)
        reader.Skip(4);

        // Language (uint32)
        reader.Skip(4);

        // numGroups (uint32)
        uint numGroups = reader.ReadUInt32();

        // Read sequential map groups (12 bytes each)
        for (uint i = 0; i < numGroups; i++)
        {
            uint startCharCode = reader.ReadUInt32();
            uint endCharCode = reader.ReadUInt32();
            uint startGlyphId = reader.ReadUInt32();

            // Map the range
            for (uint codePoint = startCharCode; codePoint <= endCharCode; codePoint++)
            {
                uint glyphIndex = startGlyphId + (codePoint - startCharCode);

                // Store in dictionary (only if fits in ushort for now)
                // TODO: Support glyphs beyond 65535
                if (glyphIndex <= 0xFFFF)
                {
                    font.CharacterToGlyphIndex[(int)codePoint] = (ushort)glyphIndex;
                }
            }
        }
    }

    private class EncodingRecord
    {
        public ushort PlatformId { get; set; }
        public ushort EncodingId { get; set; }
        public uint Offset { get; set; }
    }
}
