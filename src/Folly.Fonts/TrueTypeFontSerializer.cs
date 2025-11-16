using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Folly.Fonts.Models;

namespace Folly.Fonts;

/// <summary>
/// Serializes a FontFile to TrueType format.
/// Used for creating font subsets that can be embedded in PDF files.
/// </summary>
public class TrueTypeFontSerializer
{
    private class TableEntry
    {
        public string Tag { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public uint Checksum { get; set; }
        public uint Offset { get; set; }
    }

    /// <summary>
    /// Serializes a font file to TrueType format.
    /// </summary>
    /// <param name="font">The font file to serialize.</param>
    /// <returns>Byte array containing the TrueType font data.</returns>
    public static byte[] Serialize(FontFile font)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        if (!font.IsTrueType)
            throw new NotSupportedException("Only TrueType fonts can be serialized to TrueType format");

        using var memoryStream = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(memoryStream, leaveOpen: true);

        // Step 1: Generate table data for all required tables
        var tables = GenerateTables(font);

        // Step 2: Calculate offsets for each table
        uint currentOffset = CalculateHeaderSize((ushort)tables.Count);
        foreach (var table in tables)
        {
            table.Offset = currentOffset;
            // Align to 4-byte boundary
            currentOffset += (uint)table.Data.Length;
            uint remainder = currentOffset % 4;
            if (remainder != 0)
                currentOffset += (4 - remainder);
        }

        // Step 3: Write table directory
        WriteTableDirectory(writer, tables);

        // Step 4: Write table data
        foreach (var table in tables)
        {
            writer.WriteBytes(table.Data);
            writer.WritePadding(4); // Align to 4-byte boundary
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Calculates the size of the TrueType header (table directory).
    /// </summary>
    private static uint CalculateHeaderSize(ushort numTables)
    {
        // Offset table: 12 bytes + (numTables * 16 bytes per table record)
        return 12 + (uint)(numTables * 16);
    }

    /// <summary>
    /// Generates all required TrueType tables for the font.
    /// </summary>
    private static List<TableEntry> GenerateTables(FontFile font)
    {
        var tables = new List<TableEntry>();

        // Required tables in typical order
        tables.Add(CreateHeadTable(font));
        tables.Add(CreateHheaTable(font));
        tables.Add(CreateMaxpTable(font));
        tables.Add(CreateHmtxTable(font));
        tables.Add(CreateLocaTable(font));
        tables.Add(CreateGlyfTable(font));
        tables.Add(CreateNameTable(font));
        tables.Add(CreateCmapTable(font));
        tables.Add(CreatePostTable(font));

        // Optional tables
        if (font.OS2 != null)
        {
            tables.Add(CreateOS2Table(font));
        }

        // Sort tables alphabetically by tag (required by TrueType spec)
        tables = tables.OrderBy(t => t.Tag).ToList();

        // Calculate checksums for all tables
        foreach (var table in tables)
        {
            table.Checksum = CalculateChecksum(table.Data);
        }

        return tables;
    }

    /// <summary>
    /// Writes the TrueType table directory (offset table).
    /// </summary>
    private static void WriteTableDirectory(BigEndianBinaryWriter writer, List<TableEntry> tables)
    {
        ushort numTables = (ushort)tables.Count;

        // Calculate search range, entry selector, and range shift
        ushort entrySelector = 0;
        ushort searchRange = 1;
        while (searchRange * 2 <= numTables)
        {
            searchRange *= 2;
            entrySelector++;
        }
        searchRange *= 16; // Each table record is 16 bytes

        ushort rangeShift = (ushort)(numTables * 16 - searchRange);

        // Write offset table
        writer.WriteUInt32(0x00010000); // sfntVersion (1.0 for TrueType)
        writer.WriteUInt16(numTables);
        writer.WriteUInt16(searchRange);
        writer.WriteUInt16(entrySelector);
        writer.WriteUInt16(rangeShift);

        // Write table directory entries
        foreach (var table in tables)
        {
            writer.WriteFixedString(table.Tag, 4);
            writer.WriteUInt32(table.Checksum);
            writer.WriteUInt32(table.Offset);
            writer.WriteUInt32((uint)table.Data.Length);
        }
    }

    /// <summary>
    /// Calculates the TrueType checksum for a table.
    /// Sum of all uint32 values in the table.
    /// </summary>
    private static uint CalculateChecksum(byte[] data)
    {
        uint sum = 0;
        int length = data.Length;

        // Sum all uint32 values (4-byte chunks)
        for (int i = 0; i + 3 < length; i += 4)
        {
            uint value = ((uint)data[i] << 24)
                       | ((uint)data[i + 1] << 16)
                       | ((uint)data[i + 2] << 8)
                       | data[i + 3];
            sum += value;
        }

        // Handle remaining bytes (if length is not a multiple of 4)
        int remainder = length % 4;
        if (remainder > 0)
        {
            uint value = 0;
            for (int i = length - remainder; i < length; i++)
            {
                value = (value << 8) | data[i];
            }
            // Shift remaining bytes to the left
            value <<= (4 - remainder) * 8;
            sum += value;
        }

        return sum;
    }

    // Table creation methods

    private static TableEntry CreateHeadTable(FontFile font)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms, leaveOpen: true);

        // head table version 1.0
        writer.WriteUInt32(0x00010000); // version (Fixed: 16.16)

        // Font revision - use from original font if available, otherwise default to 1.0
        uint fontRevision = font.Head?.FontRevision ?? 0x00010000;
        writer.WriteUInt32(fontRevision); // fontRevision (Fixed: 16.16)

        writer.WriteUInt32(0); // checksumAdjustment - placeholder, will be set to 0
        writer.WriteUInt32(0x5F0F3CF5); // magicNumber (required constant)

        // Flags - use from original font if available, otherwise use sensible defaults
        ushort flags = font.Head?.Flags ?? 0;
        if (flags == 0)
        {
            // Default flags if not available
            flags |= (1 << 0); // Baseline at y=0
            flags |= (1 << 1); // Left sidebearing at x=0
            flags |= (1 << 3); // Force ppem to integer values
        }
        writer.WriteUInt16(flags);

        writer.WriteUInt16(font.UnitsPerEm);

        // Timestamps - use current time for subset fonts
        long currentMacTime = HeadTable.ConvertDateTimeToMacEpoch(DateTime.UtcNow);
        writer.WriteInt64(currentMacTime); // created
        writer.WriteInt64(currentMacTime); // modified

        // Font bounding box
        writer.WriteInt16(font.XMin);
        writer.WriteInt16(font.YMin);
        writer.WriteInt16(font.XMax);
        writer.WriteInt16(font.YMax);

        // macStyle - calculate from font properties
        ushort macStyle = CalculateMacStyle(font);
        writer.WriteUInt16(macStyle);

        writer.WriteUInt16(9); // lowestRecPPEM - minimum readable size
        writer.WriteInt16(2); // fontDirectionHint (2 = strongly left to right)
        writer.WriteInt16(font.IndexToLocFormat); // indexToLocFormat (0=short, 1=long)
        writer.WriteInt16(0); // glyphDataFormat (0 for current format)

        return new TableEntry { Tag = "head", Data = ms.ToArray() };
    }

    /// <summary>
    /// Calculates the macStyle flags from font properties.
    /// Bit 0: Bold
    /// Bit 1: Italic
    /// </summary>
    private static ushort CalculateMacStyle(FontFile font)
    {
        ushort macStyle = 0;

        // First try to use macStyle from original head table
        if (font.Head != null)
        {
            macStyle = font.Head.MacStyle;
        }
        else
        {
            // Calculate from OS/2 and Post tables if available
            // Bold: OS/2 WeightClass >= 700
            if (font.OS2 != null && font.OS2.WeightClass >= 700)
            {
                macStyle |= 0x01; // Bold
            }

            // Italic: Post table ItalicAngle != 0 or OS/2 fsSelection bit 0
            if (font.Post != null && Math.Abs(font.Post.ItalicAngle) > 0.01)
            {
                macStyle |= 0x02; // Italic
            }
        }

        return macStyle;
    }

    private static TableEntry CreateHheaTable(FontFile font)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms, leaveOpen: true);

        writer.WriteUInt32(0x00010000); // version 1.0
        writer.WriteInt16(font.Ascender);
        writer.WriteInt16(font.Descender);
        writer.WriteInt16(font.LineGap);

        // advanceWidthMax
        ushort maxAdvanceWidth = font.GlyphAdvanceWidths.Length > 0
            ? font.GlyphAdvanceWidths.Max()
            : (ushort)0;
        writer.WriteUInt16(maxAdvanceWidth);

        // minLeftSideBearing
        short minLsb = font.GlyphLeftSideBearings.Length > 0
            ? font.GlyphLeftSideBearings.Min()
            : (short)0;
        writer.WriteInt16(minLsb);

        // minRightSideBearing - calculate from glyph data
        short minRsb = CalculateMinRightSideBearing(font);
        writer.WriteInt16(minRsb);

        // xMaxExtent - calculate from glyph data
        // xMaxExtent = max(lsb + (xMax - xMin)) for all glyphs
        short xMaxExtent = CalculateXMaxExtent(font);
        writer.WriteInt16(xMaxExtent);

        writer.WriteInt16(1); // caretSlopeRise (vertical caret)
        writer.WriteInt16(0); // caretSlopeRun
        writer.WriteInt16(0); // caretOffset
        writer.WriteInt16(0); // reserved
        writer.WriteInt16(0); // reserved
        writer.WriteInt16(0); // reserved
        writer.WriteInt16(0); // reserved
        writer.WriteInt16(0); // metricDataFormat (0 for current format)
        writer.WriteUInt16((ushort)font.GlyphAdvanceWidths.Length); // numberOfHMetrics

        return new TableEntry { Tag = "hhea", Data = ms.ToArray() };
    }

    /// <summary>
    /// Calculates the minimum right side bearing from glyph data.
    /// minRightSideBearing = min(advanceWidth - lsb - (xMax - xMin))
    /// </summary>
    private static short CalculateMinRightSideBearing(FontFile font)
    {
        if (font.Glyphs == null || font.Glyphs.Length == 0)
            return 0;

        short minRsb = short.MaxValue;

        for (int i = 0; i < font.GlyphCount && i < font.Glyphs.Length; i++)
        {
            var glyph = font.Glyphs[i];
            if (glyph == null || glyph.IsEmptyGlyph)
                continue;

            // Get metrics for this glyph
            ushort advanceWidth = i < font.GlyphAdvanceWidths.Length
                ? font.GlyphAdvanceWidths[i]
                : (ushort)0;
            short lsb = i < font.GlyphLeftSideBearings.Length
                ? font.GlyphLeftSideBearings[i]
                : (short)0;

            // Calculate right side bearing
            // rsb = advanceWidth - lsb - (xMax - xMin)
            short glyphWidth = (short)(glyph.XMax - glyph.XMin);
            short rsb = (short)(advanceWidth - lsb - glyphWidth);

            if (rsb < minRsb)
                minRsb = rsb;
        }

        return minRsb == short.MaxValue ? (short)0 : minRsb;
    }

    /// <summary>
    /// Calculates the maximum extent in the x direction.
    /// xMaxExtent = max(lsb + (xMax - xMin)) for all glyphs
    /// </summary>
    private static short CalculateXMaxExtent(FontFile font)
    {
        if (font.Glyphs == null || font.Glyphs.Length == 0)
            return font.XMax;

        short maxExtent = 0;

        for (int i = 0; i < font.GlyphCount && i < font.Glyphs.Length; i++)
        {
            var glyph = font.Glyphs[i];
            if (glyph == null || glyph.IsEmptyGlyph)
                continue;

            // Get left side bearing for this glyph
            short lsb = i < font.GlyphLeftSideBearings.Length
                ? font.GlyphLeftSideBearings[i]
                : (short)0;

            // Calculate extent: lsb + (xMax - xMin)
            short glyphWidth = (short)(glyph.XMax - glyph.XMin);
            short extent = (short)(lsb + glyphWidth);

            if (extent > maxExtent)
                maxExtent = extent;
        }

        return maxExtent > 0 ? maxExtent : font.XMax;
    }

    private static TableEntry CreateMaxpTable(FontFile font)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms, leaveOpen: true);

        writer.WriteUInt32(0x00010000); // version 1.0
        writer.WriteUInt16(font.GlyphCount);

        // TrueType version 1.0 maxp requires additional fields
        // For simplicity, we'll use conservative values
        writer.WriteUInt16(100); // maxPoints
        writer.WriteUInt16(50);  // maxContours
        writer.WriteUInt16(0);   // maxCompositePoints
        writer.WriteUInt16(0);   // maxCompositeContours
        writer.WriteUInt16(2);   // maxZones (2 for all fonts)
        writer.WriteUInt16(0);   // maxTwilightPoints
        writer.WriteUInt16(0);   // maxStorage
        writer.WriteUInt16(0);   // maxFunctionDefs
        writer.WriteUInt16(0);   // maxInstructionDefs
        writer.WriteUInt16(0);   // maxStackElements
        writer.WriteUInt16(0);   // maxSizeOfInstructions
        writer.WriteUInt16(0);   // maxComponentElements
        writer.WriteUInt16(0);   // maxComponentDepth

        return new TableEntry { Tag = "maxp", Data = ms.ToArray() };
    }

    private static TableEntry CreateHmtxTable(FontFile font)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms, leaveOpen: true);

        // Write horizontal metrics for each glyph
        for (int i = 0; i < font.GlyphCount; i++)
        {
            ushort advanceWidth = i < font.GlyphAdvanceWidths.Length
                ? font.GlyphAdvanceWidths[i]
                : (ushort)0;
            short lsb = i < font.GlyphLeftSideBearings.Length
                ? font.GlyphLeftSideBearings[i]
                : (short)0;

            writer.WriteUInt16(advanceWidth);
            writer.WriteInt16(lsb);
        }

        return new TableEntry { Tag = "hmtx", Data = ms.ToArray() };
    }

    private static TableEntry CreateLocaTable(FontFile font)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms, leaveOpen: true);

        // loca table contains offsets to glyph data in the glyf table
        // We need to calculate offsets based on glyph data sizes

        uint currentOffset = 0;
        var offsets = new List<uint>();

        if (font.Glyphs != null)
        {
            for (int i = 0; i < font.GlyphCount; i++)
            {
                offsets.Add(currentOffset);

                // Calculate actual glyph data size from RawGlyphData
                if (i < font.Glyphs.Length && font.Glyphs[i] != null)
                {
                    uint glyphSize = (uint)font.Glyphs[i].GetSerializedSize();
                    currentOffset += glyphSize;
                }
            }
            // Add final offset (end of last glyph)
            offsets.Add(currentOffset);
        }
        else
        {
            // No glyph data - create empty loca table
            for (int i = 0; i <= font.GlyphCount; i++)
            {
                offsets.Add(0);
            }
        }

        // Write offsets in the appropriate format
        if (font.IndexToLocFormat == 0)
        {
            // Short format: offsets divided by 2
            foreach (var offset in offsets)
            {
                writer.WriteUInt16((ushort)(offset / 2));
            }
        }
        else
        {
            // Long format: full offsets
            foreach (var offset in offsets)
            {
                writer.WriteUInt32(offset);
            }
        }

        return new TableEntry { Tag = "loca", Data = ms.ToArray() };
    }

    private static TableEntry CreateGlyfTable(FontFile font)
    {
        using var ms = new MemoryStream();

        // glyf table contains glyph outline data
        // We write the raw glyph data bytes captured during parsing
        // This preserves all outline data (contours, points, flags, coordinates, instructions, hints)

        if (font.Glyphs != null)
        {
            for (int i = 0; i < font.GlyphCount && i < font.Glyphs.Length; i++)
            {
                var glyph = font.Glyphs[i];
                if (glyph != null && glyph.RawGlyphData != null && glyph.RawGlyphData.Length > 0)
                {
                    // Write raw glyph data verbatim
                    // This preserves the exact glyph outline from the original font
                    ms.Write(glyph.RawGlyphData, 0, glyph.RawGlyphData.Length);
                }
                // Empty glyphs (like space) have no data to write
            }
        }

        return new TableEntry { Tag = "glyf", Data = ms.ToArray() };
    }

    private static TableEntry CreateNameTable(FontFile font)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms, leaveOpen: true);

        // Build name records
        var nameRecords = new List<(ushort platformId, ushort encodingId, ushort languageId, ushort nameId, string value)>
        {
            // Platform 3 (Windows), Encoding 1 (Unicode BMP), Language 0x0409 (en-US)
            (3, 1, 0x0409, 1, font.FamilyName),      // Family name
            (3, 1, 0x0409, 2, font.SubfamilyName),   // Subfamily name
            (3, 1, 0x0409, 4, font.FullName),        // Full name
            (3, 1, 0x0409, 6, font.PostScriptName),  // PostScript name
        };

        // Remove empty names
        nameRecords = nameRecords.Where(r => !string.IsNullOrEmpty(r.value)).ToList();

        // Format 0: simple format
        writer.WriteUInt16(0); // format
        writer.WriteUInt16((ushort)nameRecords.Count);

        // String storage offset (after all name records)
        ushort stringOffset = (ushort)(6 + nameRecords.Count * 12);
        writer.WriteUInt16(stringOffset);

        // Write name records
        ushort currentStringOffset = 0;
        var stringData = new MemoryStream();
        var stringWriter = new BigEndianBinaryWriter(stringData, leaveOpen: true);

        foreach (var record in nameRecords)
        {
            // Convert string to UTF-16 BE (Windows Unicode)
            byte[] stringBytes = System.Text.Encoding.BigEndianUnicode.GetBytes(record.value);

            writer.WriteUInt16(record.platformId);
            writer.WriteUInt16(record.encodingId);
            writer.WriteUInt16(record.languageId);
            writer.WriteUInt16(record.nameId);
            writer.WriteUInt16((ushort)stringBytes.Length);
            writer.WriteUInt16(currentStringOffset);

            stringWriter.WriteBytes(stringBytes);
            currentStringOffset += (ushort)stringBytes.Length;
        }

        // Write string data
        writer.WriteBytes(stringData.ToArray());

        return new TableEntry { Tag = "name", Data = ms.ToArray() };
    }

    private static TableEntry CreateCmapTable(FontFile font)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms, leaveOpen: true);

        // Create a format 4 cmap subtable (Windows Unicode BMP)
        var format4Data = CreateCmapFormat4(font);

        // Write cmap header
        writer.WriteUInt16(0); // version
        writer.WriteUInt16(1); // numTables (we'll create one subtable)

        // Write encoding record
        writer.WriteUInt16(3); // platformId (Windows)
        writer.WriteUInt16(1); // encodingId (Unicode BMP)
        writer.WriteUInt32(12); // offset to subtable (after header + encoding records)

        // Write subtable
        writer.WriteBytes(format4Data);

        return new TableEntry { Tag = "cmap", Data = ms.ToArray() };
    }

    private static byte[] CreateCmapFormat4(FontFile font)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms, leaveOpen: true);

        // Build segments from character to glyph mapping
        var sortedChars = font.CharacterToGlyphIndex.OrderBy(kvp => kvp.Key).ToList();

        if (sortedChars.Count == 0)
        {
            // Empty cmap - create minimal format 4 table
            writer.WriteUInt16(4); // format
            writer.WriteUInt16(16); // length
            writer.WriteUInt16(0); // language
            writer.WriteUInt16(4); // segCountX2 (2 segments * 2)
            writer.WriteUInt16(4); // searchRange
            writer.WriteUInt16(1); // entrySelector
            writer.WriteUInt16(0); // rangeShift

            // End codes
            writer.WriteUInt16(0xFFFF);
            writer.WriteUInt16(0xFFFF);

            // Reserved pad
            writer.WriteUInt16(0);

            // Start codes
            writer.WriteUInt16(0xFFFF);
            writer.WriteUInt16(0xFFFF);

            // ID deltas
            writer.WriteInt16(1);
            writer.WriteInt16(1);

            // ID range offsets
            writer.WriteUInt16(0);
            writer.WriteUInt16(0);

            return ms.ToArray();
        }

        // Build segments - simplified implementation: one segment per contiguous range
        var segments = new List<(ushort startCode, ushort endCode, short idDelta)>();
        int rangeStart = sortedChars[0].Key;
        int rangeEnd = rangeStart;
        ushort firstGlyphInRange = sortedChars[0].Value;

        for (int i = 1; i < sortedChars.Count; i++)
        {
            int currentChar = sortedChars[i].Key;

            if (currentChar == rangeEnd + 1 && sortedChars[i].Value == sortedChars[i-1].Value + 1)
            {
                // Continue current segment
                rangeEnd = currentChar;
            }
            else
            {
                // End current segment, start new one
                short idDelta = (short)(firstGlyphInRange - rangeStart);
                segments.Add(((ushort)rangeStart, (ushort)rangeEnd, idDelta));

                rangeStart = currentChar;
                rangeEnd = currentChar;
                firstGlyphInRange = sortedChars[i].Value;
            }
        }

        // Add final segment
        {
            short idDelta = (short)(firstGlyphInRange - rangeStart);
            segments.Add(((ushort)rangeStart, (ushort)rangeEnd, idDelta));
        }

        // Add terminator segment (required by spec)
        segments.Add((0xFFFF, 0xFFFF, 1));

        ushort segCount = (ushort)segments.Count;
        ushort segCountX2 = (ushort)(segCount * 2);

        // Calculate search parameters
        ushort searchRange = 2;
        ushort entrySelector = 0;
        while (searchRange * 2 <= segCountX2)
        {
            searchRange *= 2;
            entrySelector++;
        }
        ushort rangeShift = (ushort)(segCountX2 - searchRange);

        // Calculate table length
        ushort length = (ushort)(16 + segCount * 8); // header + segment arrays

        // Write format 4 subtable
        writer.WriteUInt16(4); // format
        writer.WriteUInt16(length);
        writer.WriteUInt16(0); // language (0 for Unicode)
        writer.WriteUInt16(segCountX2);
        writer.WriteUInt16(searchRange);
        writer.WriteUInt16(entrySelector);
        writer.WriteUInt16(rangeShift);

        // Write end codes
        foreach (var seg in segments)
        {
            writer.WriteUInt16(seg.endCode);
        }

        writer.WriteUInt16(0); // reserved pad

        // Write start codes
        foreach (var seg in segments)
        {
            writer.WriteUInt16(seg.startCode);
        }

        // Write ID deltas
        foreach (var seg in segments)
        {
            writer.WriteInt16(seg.idDelta);
        }

        // Write ID range offsets (all 0 for simple delta-based mapping)
        for (int i = 0; i < segCount; i++)
        {
            writer.WriteUInt16(0);
        }

        return ms.ToArray();
    }

    private static TableEntry CreatePostTable(FontFile font)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms, leaveOpen: true);

        // Use format 3.0 (no PostScript glyph names)
        writer.WriteUInt32(0x00030000); // version 3.0

        // Use values from Post table if available, otherwise use defaults
        if (font.Post != null)
        {
            // Convert italic angle to Fixed 16.16 format
            int italicAngleFixed = (int)(font.Post.ItalicAngle * 65536.0);
            writer.WriteInt32(italicAngleFixed); // italicAngle (Fixed)
            writer.WriteInt16(font.Post.UnderlinePosition); // underlinePosition
            writer.WriteInt16(font.Post.UnderlineThickness); // underlineThickness
            writer.WriteUInt32(font.Post.IsFixedPitch); // isFixedPitch
        }
        else
        {
            writer.WriteInt32(0); // italicAngle (Fixed)
            writer.WriteInt16(0); // underlinePosition
            writer.WriteInt16(0); // underlineThickness
            writer.WriteUInt32(0); // isFixedPitch
        }

        writer.WriteUInt32(0); // minMemType42
        writer.WriteUInt32(0); // maxMemType42
        writer.WriteUInt32(0); // minMemType1
        writer.WriteUInt32(0); // maxMemType1

        return new TableEntry { Tag = "post", Data = ms.ToArray() };
    }

    private static TableEntry CreateOS2Table(FontFile font)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms, leaveOpen: true);

        if (font.OS2 == null)
            return new TableEntry { Tag = "OS/2", Data = Array.Empty<byte>() };

        var os2 = font.OS2;

        // Use version from original table, or default to version 4
        ushort version = os2.Version > 0 ? os2.Version : (ushort)4;
        writer.WriteUInt16(version);
        writer.WriteInt16(os2.XAvgCharWidth);
        writer.WriteUInt16(os2.WeightClass);
        writer.WriteUInt16(os2.WidthClass);
        writer.WriteUInt16(os2.Type);

        // ySubscript and ySuperscript fields (8 fields) - use defaults
        writer.WriteInt16(0); // ySubscriptXSize
        writer.WriteInt16(0); // ySubscriptYSize
        writer.WriteInt16(0); // ySubscriptXOffset
        writer.WriteInt16(0); // ySubscriptYOffset
        writer.WriteInt16(0); // ySuperscriptXSize
        writer.WriteInt16(0); // ySuperscriptYSize
        writer.WriteInt16(0); // ySuperscriptXOffset
        writer.WriteInt16(0); // ySuperscriptYOffset

        // yStrikeout fields - use defaults
        writer.WriteInt16(0); // yStrikeoutSize
        writer.WriteInt16(0); // yStrikeoutPosition

        // sFamilyClass - use default
        writer.WriteInt16(0);

        // panose (10 bytes) - use defaults (all zeros)
        for (int i = 0; i < 10; i++)
        {
            writer.WriteByte(0);
        }

        // Unicode range bits (4 uint32s) - use defaults (all bits set)
        writer.WriteUInt32(0xFFFFFFFF); // ulUnicodeRange1
        writer.WriteUInt32(0xFFFFFFFF); // ulUnicodeRange2
        writer.WriteUInt32(0xFFFFFFFF); // ulUnicodeRange3
        writer.WriteUInt32(0xFFFFFFFF); // ulUnicodeRange4

        // achVendID (4 bytes)
        writer.WriteFixedString("NONE", 4);

        // fsSelection - use default
        writer.WriteUInt16(0);

        // usFirstCharIndex and usLastCharIndex - calculate from character map
        ushort firstChar = 0xFFFF;
        ushort lastChar = 0;
        foreach (var kvp in font.CharacterToGlyphIndex)
        {
            if (kvp.Key < firstChar)
                firstChar = (ushort)kvp.Key;
            if (kvp.Key > lastChar && kvp.Key <= 0xFFFF)
                lastChar = (ushort)kvp.Key;
        }
        writer.WriteUInt16(firstChar);
        writer.WriteUInt16(lastChar);

        // Version 0 ends here, version 1+ continues
        if (version >= 1)
        {
            writer.WriteInt16(os2.TypoAscender);
            writer.WriteInt16(os2.TypoDescender);
            writer.WriteInt16(os2.TypoLineGap);
            writer.WriteUInt16(os2.WinAscent);
            writer.WriteUInt16(os2.WinDescent);
        }

        // Version 1+ has code page ranges
        if (version >= 1)
        {
            writer.WriteUInt32(0); // ulCodePageRange1
            writer.WriteUInt32(0); // ulCodePageRange2
        }

        // Version 2+ has x-height and cap-height
        if (version >= 2)
        {
            writer.WriteInt16(0); // sxHeight
            writer.WriteInt16(0); // sCapHeight
            writer.WriteUInt16(0); // usDefaultChar
            writer.WriteUInt16(32); // usBreakChar (space character)
            writer.WriteUInt16(0); // usMaxContext
        }

        // Version 5+ has optical point sizes (we don't support version 5)

        return new TableEntry { Tag = "OS/2", Data = ms.ToArray() };
    }
}
