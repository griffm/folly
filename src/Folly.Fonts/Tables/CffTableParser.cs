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
    /// Parses the 'CFF ' table and populates CFF data in the font file.
    /// Extracts essential CFF information including CharStrings offsets, Charset, and Encoding.
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
                font.Logger.Warning($"Unsupported CFF version {major}.{minor} (expected 1.x)");
                return;
            }

            // Skip to end of header
            reader.Seek(tableStart + hdrSize);

            // Parse Name INDEX (font names)
            var names = ParseIndex(reader);
            if (names.Count > 0)
            {
                cffData.FontName = Encoding.ASCII.GetString(names[0]);
                font.Logger.Debug($"CFF font name: {cffData.FontName}");
            }
            else
            {
                font.Logger.Warning("CFF Name INDEX is empty");
            }

            // Parse Top DICT INDEX
            var topDictData = ParseIndex(reader);
            if (topDictData.Count > 0)
            {
                cffData.TopDict = ParseTopDict(topDictData[0]);

                // Check if this is a CID font
                if (cffData.TopDict.ROS.HasValue)
                {
                    cffData.IsCIDFont = true;
                    font.Logger.Debug($"CFF is a CIDFont (ROS: {cffData.TopDict.ROS})");
                }

                // Log extracted offsets
                if (cffData.TopDict.CharStringsOffset > 0)
                {
                    font.Logger.Debug($"CFF CharStrings offset: {cffData.TopDict.CharStringsOffset}");
                }
                if (cffData.TopDict.CharsetOffset > 0)
                {
                    font.Logger.Debug($"CFF Charset offset: {cffData.TopDict.CharsetOffset}");
                }
                if (cffData.TopDict.EncodingOffset > 0)
                {
                    font.Logger.Debug($"CFF Encoding offset: {cffData.TopDict.EncodingOffset}");
                }
            }
            else
            {
                font.Logger.Warning("CFF Top DICT INDEX is empty");
            }

            // Parse String INDEX (for SID lookups)
            var stringIndex = ParseIndex(reader);
            font.Logger.Debug($"CFF String INDEX contains {stringIndex.Count} strings");

            // Parse Global Subr INDEX
            cffData.GlobalSubrs = ParseIndex(reader);
            font.Logger.Debug($"CFF Global Subrs INDEX contains {cffData.GlobalSubrs.Count} subroutines");

            // Mark as CFF font
            font.IsTrueType = false;

            // Store the parsed CFF data
            font.Cff = cffData;

            font.Logger.Info($"Successfully parsed CFF table for font: {cffData.FontName}");
        }
        catch (Exception ex)
        {
            font.Logger.Error($"CFF table parsing failed: {ex.Message}", ex);
            // Allow font to continue loading - PDF embedding may still work with raw data
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
    /// Parses a Top DICT extracting all key values.
    /// </summary>
    private static CffTopDict ParseTopDict(byte[] data)
    {
        var dict = new CffTopDict();
        var stack = new List<double>();
        int i = 0;

        while (i < data.Length)
        {
            byte b0 = data[i];

            if (b0 >= 0 && b0 <= 21) // Operator
            {
                if (b0 == 12 && i + 1 < data.Length) // Two-byte operator
                {
                    byte b1 = data[i + 1];
                    ProcessTwoByteOperator(b1, stack, dict);
                    i += 2;
                }
                else
                {
                    ProcessOneByteOperator(b0, stack, dict);
                    i++;
                }
                stack.Clear();
            }
            else // Operand (number)
            {
                var (number, bytesRead) = ReadDictNumber(data, i);
                stack.Add(number);
                i += bytesRead;
            }
        }

        return dict;
    }

    /// <summary>
    /// Processes a one-byte DICT operator.
    /// </summary>
    private static void ProcessOneByteOperator(byte op, List<double> stack, CffTopDict dict)
    {
        switch (op)
        {
            case 5: // FontBBox
                if (stack.Count >= 4)
                {
                    dict.FontBBox = new double[]
                    {
                        stack[0], stack[1], stack[2], stack[3]
                    };
                }
                break;

            case 15: // charset
                if (stack.Count >= 1)
                {
                    dict.CharsetOffset = (int)stack[0];
                }
                break;

            case 16: // Encoding
                if (stack.Count >= 1)
                {
                    dict.EncodingOffset = (int)stack[0];
                }
                break;

            case 17: // CharStrings
                if (stack.Count >= 1)
                {
                    dict.CharStringsOffset = (int)stack[0];
                }
                break;

            case 18: // Private (size offset)
                if (stack.Count >= 2)
                {
                    dict.Private = ((int)stack[0], (int)stack[1]);
                }
                break;

            // Other operators can be added as needed
        }
    }

    /// <summary>
    /// Processes a two-byte DICT operator (12 followed by second byte).
    /// </summary>
    private static void ProcessTwoByteOperator(byte op, List<double> stack, CffTopDict dict)
    {
        switch (op)
        {
            case 7: // FontMatrix
                if (stack.Count >= 6)
                {
                    dict.FontMatrix = new double[]
                    {
                        stack[0], stack[1], stack[2],
                        stack[3], stack[4], stack[5]
                    };
                }
                break;

            case 30: // ROS (Registry-Ordering-Supplement) - CIDFont marker
                if (stack.Count >= 3)
                {
                    // ROS values are SID references, but for now we just note it's a CID font
                    dict.ROS = ("Adobe", "Identity", (int)stack[2]);
                }
                break;

            case 36: // FDArray - CIDFont
                if (stack.Count >= 1)
                {
                    dict.FDArrayOffset = (int)stack[0];
                }
                break;

            case 37: // FDSelect - CIDFont
                if (stack.Count >= 1)
                {
                    dict.FDSelectOffset = (int)stack[0];
                }
                break;

            // Other two-byte operators can be added as needed
        }
    }

    /// <summary>
    /// Reads a DICT number (integer or real) and returns the value and number of bytes consumed.
    /// </summary>
    private static (double value, int bytesRead) ReadDictNumber(byte[] data, int offset)
    {
        if (offset >= data.Length)
            return (0, 0);

        byte b0 = data[offset];

        // Integer ranges (CFF spec Table 3)
        if (b0 >= 32 && b0 <= 246)
        {
            // Small integer: b0 - 139
            return (b0 - 139, 1);
        }
        else if (b0 >= 247 && b0 <= 250)
        {
            // Positive integer: +((b0 - 247) * 256 + b1 + 108)
            if (offset + 1 >= data.Length) return (0, 1);
            byte b1 = data[offset + 1];
            return ((b0 - 247) * 256 + b1 + 108, 2);
        }
        else if (b0 >= 251 && b0 <= 254)
        {
            // Negative integer: -((b0 - 251) * 256 + b1 + 108)
            if (offset + 1 >= data.Length) return (0, 1);
            byte b1 = data[offset + 1];
            return (-((b0 - 251) * 256 + b1 + 108), 2);
        }
        else if (b0 == 28)
        {
            // 16-bit integer: b1 << 8 | b2
            if (offset + 2 >= data.Length) return (0, 1);
            short value = (short)((data[offset + 1] << 8) | data[offset + 2]);
            return (value, 3);
        }
        else if (b0 == 29)
        {
            // 32-bit integer: b1 << 24 | b2 << 16 | b3 << 8 | b4
            if (offset + 4 >= data.Length) return (0, 1);
            int value = (data[offset + 1] << 24) | (data[offset + 2] << 16) |
                       (data[offset + 3] << 8) | data[offset + 4];
            return (value, 5);
        }
        else if (b0 == 30)
        {
            // Real number (nibble-encoded)
            return ReadDictReal(data, offset);
        }

        return (0, 1);
    }

    /// <summary>
    /// Reads a nibble-encoded real number from a DICT.
    /// </summary>
    private static (double value, int bytesRead) ReadDictReal(byte[] data, int offset)
    {
        var sb = new StringBuilder();
        int i = offset + 1;
        bool done = false;

        while (i < data.Length && !done)
        {
            byte b = data[i];

            // Process high nibble
            int highNibble = (b >> 4) & 0x0F;
            if (!ProcessRealNibble(highNibble, sb, ref done))
                break;

            if (done)
                break;

            // Process low nibble
            int lowNibble = b & 0x0F;
            if (!ProcessRealNibble(lowNibble, sb, ref done))
                break;

            i++;
        }

        double value = 0;
        if (sb.Length > 0 && double.TryParse(sb.ToString(), out double parsed))
        {
            value = parsed;
        }

        return (value, i - offset + 1);
    }

    /// <summary>
    /// Processes a single nibble in a real number.
    /// Returns false to stop processing, true to continue.
    /// </summary>
    private static bool ProcessRealNibble(int nibble, StringBuilder sb, ref bool done)
    {
        switch (nibble)
        {
            case 0x0: case 0x1: case 0x2: case 0x3: case 0x4:
            case 0x5: case 0x6: case 0x7: case 0x8: case 0x9:
                sb.Append((char)('0' + nibble));
                break;
            case 0xA:
                sb.Append('.');
                break;
            case 0xB:
                sb.Append('E');
                break;
            case 0xC:
                sb.Append("E-");
                break;
            case 0xD:
                // Reserved
                break;
            case 0xE:
                sb.Append('-');
                break;
            case 0xF:
                done = true;
                break;
        }
        return true;
    }
}
