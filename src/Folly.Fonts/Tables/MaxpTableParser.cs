using System;
using System.IO;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'maxp' (maximum profile) table.
/// This table contains the number of glyphs in the font and other maximum values.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/maxp
/// </summary>
public static class MaxpTableParser
{
    /// <summary>
    /// Parses the 'maxp' table and populates the font file with maximum profile information.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);

        // Version (Fixed) - 0.5 for CFF fonts, 1.0 for TrueType fonts
        double version = reader.ReadFixed();

        // Number of glyphs (uint16)
        font.GlyphCount = reader.ReadUInt16();

        if (font.GlyphCount == 0)
        {
            throw new InvalidDataException("Font has zero glyphs");
        }

        // Version 1.0 has additional fields (TrueType-specific)
        if (Math.Abs(version - 1.0) < 0.001)
        {
            // maxPoints (uint16) - maximum points in a non-composite glyph
            reader.Skip(2);

            // maxContours (uint16) - maximum contours in a non-composite glyph
            reader.Skip(2);

            // maxCompositePoints (uint16)
            reader.Skip(2);

            // maxCompositeContours (uint16)
            reader.Skip(2);

            // maxZones (uint16) - should be 2
            reader.Skip(2);

            // maxTwilightPoints (uint16)
            reader.Skip(2);

            // maxStorage (uint16)
            reader.Skip(2);

            // maxFunctionDefs (uint16)
            reader.Skip(2);

            // maxInstructionDefs (uint16)
            reader.Skip(2);

            // maxStackElements (uint16)
            reader.Skip(2);

            // maxSizeOfInstructions (uint16)
            reader.Skip(2);

            // maxComponentElements (uint16)
            reader.Skip(2);

            // maxComponentDepth (uint16)
            reader.Skip(2);

            font.IsTrueType = true;
        }
        else if (Math.Abs(version - 0.5) < 0.001)
        {
            // Version 0.5 for CFF fonts - no additional fields
            font.IsTrueType = false;
        }
        else
        {
            throw new InvalidDataException($"Unsupported 'maxp' table version: {version}");
        }
    }
}
