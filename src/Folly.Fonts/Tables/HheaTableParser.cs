using System;
using System.IO;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'hhea' (horizontal header) table.
/// This table contains horizontal layout metrics such as ascender, descender, and line gap.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/hhea
/// </summary>
public static class HheaTableParser
{
    /// <summary>
    /// Number of horizontal metrics (hMetrics) in the 'hmtx' table.
    /// This is stored temporarily and used when parsing 'hmtx'.
    /// </summary>
    public static ushort NumberOfHMetrics { get; private set; }

    /// <summary>
    /// Parses the 'hhea' table and populates the font file with horizontal metrics.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);

        // Version (Fixed) - should be 1.0
        double version = reader.ReadFixed();
        if (Math.Abs(version - 1.0) > 0.001)
        {
            throw new InvalidDataException($"Unsupported 'hhea' table version: {version}");
        }

        // Ascender (FWORD/int16) - distance from baseline to highest ascender
        font.Ascender = reader.ReadInt16();

        // Descender (FWORD/int16) - distance from baseline to lowest descender (typically negative)
        font.Descender = reader.ReadInt16();

        // Line gap (FWORD/int16) - typographic line gap
        font.LineGap = reader.ReadInt16();

        // advanceWidthMax (UFWORD/uint16) - maximum advance width
        reader.Skip(2);

        // minLeftSideBearing (FWORD/int16)
        reader.Skip(2);

        // minRightSideBearing (FWORD/int16)
        reader.Skip(2);

        // xMaxExtent (FWORD/int16)
        reader.Skip(2);

        // caretSlopeRise (int16)
        reader.Skip(2);

        // caretSlopeRun (int16)
        reader.Skip(2);

        // caretOffset (int16)
        reader.Skip(2);

        // Reserved (4 x int16)
        reader.Skip(8);

        // metricDataFormat (int16) - should be 0
        short metricDataFormat = reader.ReadInt16();
        if (metricDataFormat != 0)
        {
            throw new InvalidDataException($"Unsupported metricDataFormat: {metricDataFormat}");
        }

        // numberOfHMetrics (uint16) - number of hMetric entries in 'hmtx' table
        NumberOfHMetrics = reader.ReadUInt16();

        if (NumberOfHMetrics == 0)
        {
            throw new InvalidDataException("numberOfHMetrics is zero");
        }
    }
}
