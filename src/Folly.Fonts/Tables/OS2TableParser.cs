using System;
using System.IO;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'OS/2' (OS/2 and Windows metrics) table.
/// This table contains Windows-specific font metrics and metadata.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/os2
/// </summary>
public static class OS2TableParser
{
    /// <summary>
    /// Parses the 'OS/2' table and populates Windows metrics.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);

        var os2 = new OS2Table();

        // Version (uint16)
        os2.Version = reader.ReadUInt16();

        // xAvgCharWidth (int16)
        os2.XAvgCharWidth = reader.ReadInt16();

        // usWeightClass (uint16)
        os2.WeightClass = reader.ReadUInt16();

        // usWidthClass (uint16)
        os2.WidthClass = reader.ReadUInt16();

        // fsType (uint16) - embedding permissions
        os2.Type = reader.ReadUInt16();

        // ySubscriptXSize (int16)
        reader.Skip(2);

        // ySubscriptYSize (int16)
        reader.Skip(2);

        // ySubscriptXOffset (int16)
        reader.Skip(2);

        // ySubscriptYOffset (int16)
        reader.Skip(2);

        // ySuperscriptXSize (int16)
        reader.Skip(2);

        // ySuperscriptYSize (int16)
        reader.Skip(2);

        // ySuperscriptXOffset (int16)
        reader.Skip(2);

        // ySuperscriptYOffset (int16)
        reader.Skip(2);

        // yStrikeoutSize (int16)
        reader.Skip(2);

        // yStrikeoutPosition (int16)
        reader.Skip(2);

        // sFamilyClass (int16)
        reader.Skip(2);

        // panose (10 bytes)
        reader.Skip(10);

        // ulUnicodeRange1 (uint32)
        reader.Skip(4);

        // ulUnicodeRange2 (uint32)
        reader.Skip(4);

        // ulUnicodeRange3 (uint32)
        reader.Skip(4);

        // ulUnicodeRange4 (uint32)
        reader.Skip(4);

        // achVendID (4 bytes)
        reader.Skip(4);

        // fsSelection (uint16)
        reader.Skip(2);

        // usFirstCharIndex (uint16)
        reader.Skip(2);

        // usLastCharIndex (uint16)
        reader.Skip(2);

        // Version 0 doesn't have the following fields
        if (os2.Version >= 1)
        {
            // sTypoAscender (int16)
            os2.TypoAscender = reader.ReadInt16();

            // sTypoDescender (int16)
            os2.TypoDescender = reader.ReadInt16();

            // sTypoLineGap (int16)
            os2.TypoLineGap = reader.ReadInt16();

            // usWinAscent (uint16)
            os2.WinAscent = reader.ReadUInt16();

            // usWinDescent (uint16)
            os2.WinDescent = reader.ReadUInt16();
        }

        // Version 1+ has additional fields (code page ranges, etc.) but we don't need them

        font.OS2 = os2;
    }
}
