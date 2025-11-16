using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Folly.Fonts.CFF;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'CFF ' (Compact Font Format) table.
/// CFF is used in OpenType fonts with PostScript outlines.
/// Spec: https://adobe-type-tools.github.io/font-tech-notes/pdfs/5176.CFF.pdf
/// </summary>
public static class CffTableParser
{
    /// <summary>
    /// Parses the 'CFF ' table and populates basic CFF data in the font file.
    /// This is a simplified parser that extracts essential information for PDF embedding.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);
        long tableStart = reader.Position;

        try
        {
            var cffData = new CffData();

            // Store raw CFF data for subsetting and embedding
            reader.Seek(tableStart);
            cffData.RawData = reader.ReadBytes((int)table.Length);
            reader.Seek(tableStart);

            // Parse CFF header
            byte major = reader.ReadByte();
            byte minor = reader.ReadByte();
            byte hdrSize = reader.ReadByte();
            byte offSize = reader.ReadByte();

            if (major != 1)
            {
                // Unsupported CFF version
                return;
            }

            // Skip to end of header
            reader.Seek(tableStart + hdrSize);

            // Parse Name INDEX (font names)
            var names = ParseIndex(reader);
            if (names.Count > 0)
            {
                cffData.FontName = Encoding.ASCII.GetString(names[0]);
            }

            // Parse Top DICT INDEX
            var topDictData = ParseIndex(reader);
            if (topDictData.Count > 0)
            {
                cffData.TopDict = ParseTopDict(topDictData[0]);
            }

            // Parse String INDEX (for SID lookups)
            var stringIndex = ParseIndex(reader);

            // Parse Global Subr INDEX
            cffData.GlobalSubrs = ParseIndex(reader);

            // Note: We're implementing a simplified parser here.
            // Full CFF parsing requires:
            // 1. Parsing CharStrings INDEX
            // 2. Parsing Charset
            // 3. Parsing Encoding
            // 4. For CIDFonts: parsing FDArray, FDSelect
            //
            // For Phase 8.2, we store the raw CFF data which can be:
            // - Embedded directly in PDFs (for non-subset fonts)
            // - Used as a base for subsetting operations
            //
            // CharString parsing (Type 2 CharStrings) is complex and not
            // strictly necessary for basic PDF embedding.

            font.IsTrueType = false; // Mark as CFF font

            // Store a marker that CFF is present (full parsing deferred)
            // In a production implementation, you would:
            // 1. Parse CharStrings to get glyph data
            // 2. Parse Charset to map glyph IDs to SIDs
            // 3. Parse Encoding to map char codes to glyph IDs
            // 4. Extract metrics for width calculations
        }
        catch
        {
            // If CFF parsing fails, don't crash - just don't populate CFF data
            // This allows fonts with malformed CFF tables to potentially still work
        }
    }

    /// <summary>
    /// Parses a CFF INDEX structure (array of variable-length objects).
    /// </summary>
    private static List<byte[]> ParseIndex(BigEndianBinaryReader reader)
    {
        var result = new List<byte[]>();

        ushort count = reader.ReadUInt16();
        if (count == 0)
            return result;

        byte offSize = reader.ReadByte();

        // Read offsets
        var offsets = new uint[count + 1];
        for (int i = 0; i <= count; i++)
        {
            offsets[i] = ReadOffset(reader, offSize);
        }

        long dataStart = reader.Position;

        // Read data for each object
        for (int i = 0; i < count; i++)
        {
            uint offset = offsets[i] - 1; // Offsets are 1-based
            uint nextOffset = offsets[i + 1] - 1;
            uint length = nextOffset - offset;

            reader.Seek(dataStart + offset);
            result.Add(reader.ReadBytes((int)length));
        }

        // Seek to end of INDEX
        reader.Seek(dataStart + offsets[count] - 1);

        return result;
    }

    /// <summary>
    /// Reads an offset value of variable size.
    /// </summary>
    private static uint ReadOffset(BigEndianBinaryReader reader, byte offSize)
    {
        return offSize switch
        {
            1 => reader.ReadByte(),
            2 => reader.ReadUInt16(),
            3 => ReadUInt24(reader),
            4 => reader.ReadUInt32(),
            _ => 0
        };
    }

    /// <summary>
    /// Reads a 24-bit unsigned integer.
    /// </summary>
    private static uint ReadUInt24(BigEndianBinaryReader reader)
    {
        uint b1 = reader.ReadByte();
        uint b2 = reader.ReadByte();
        uint b3 = reader.ReadByte();
        return (b1 << 16) | (b2 << 8) | b3;
    }

    /// <summary>
    /// Parses a Top DICT (simplified version extracting key values).
    /// </summary>
    private static CffTopDict ParseTopDict(byte[] data)
    {
        var dict = new CffTopDict();

        // CFF DICT parsing is complex - it uses a stack-based format
        // with operators and operands. For Phase 8.2, we implement
        // a simplified parser that extracts only the most critical values.
        //
        // Full DICT parsing requires:
        // 1. Stack-based operand accumulation
        // 2. Operator recognition (1 or 2 byte operators)
        // 3. Number decoding (variable length integers/reals)
        //
        // Common operators:
        // - 5: FontBBox
        // - 17: CharStrings
        // - 15: charset
        // - 16: Encoding
        // - 18: Private DICT
        // - (12,30): ROS (for CIDFonts)
        // - (12,36): FDArray (for CIDFonts)
        // - (12,37): FDSelect (for CIDFonts)

        // For now, return default values
        // A full implementation would parse the DICT bytecode here

        return dict;
    }
}
