using System;
using System.IO;
using Folly.Fonts.Models;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'glyf' (glyph data) table.
/// This table contains TrueType glyph outline data (contours, points, instructions).
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/glyf
/// </summary>
public static class GlyfTableParser
{
    /// <summary>
    /// Parses glyph headers from the 'glyf' table.
    /// This reads the glyph bounding boxes and basic information for each glyph.
    /// Full outline parsing (contours and points) is not yet implemented.
    /// Requires 'loca' table to be parsed first.
    /// </summary>
    public static GlyphData[] Parse(Stream stream, TableRecord table, FontFile font)
    {
        if (!font.IsTrueType || font.GlyphOffsets == null)
        {
            // CFF fonts don't have a 'glyf' table, or loca wasn't parsed
            return Array.Empty<GlyphData>();
        }

        var glyphs = new GlyphData[font.GlyphCount];

        for (ushort i = 0; i < font.GlyphCount; i++)
        {
            glyphs[i] = ParseGlyphHeader(stream, table, font, i);
        }

        return glyphs;
    }

    /// <summary>
    /// Parses the header of a single glyph and captures raw glyph data.
    /// The raw data is used for font subsetting - we copy glyphs verbatim
    /// instead of parsing and re-serializing complex outlines.
    /// </summary>
    private static GlyphData ParseGlyphHeader(Stream stream, TableRecord table, FontFile font, ushort glyphIndex)
    {
        var location = LocaTableParser.GetGlyphDataLocation(font, glyphIndex);

        if (location == null)
        {
            // Empty glyph (no outline data)
            return new GlyphData
            {
                NumberOfContours = 0,
                XMin = 0,
                YMin = 0,
                XMax = 0,
                YMax = 0,
                RawGlyphData = Array.Empty<byte>()
            };
        }

        var (offset, length) = location.Value;

        // Seek to glyph data location
        using var reader = new BigEndianBinaryReader(stream, leaveOpen: true);
        reader.Seek(table.Offset + offset);

        // Read glyph header (10 bytes)
        var glyph = new GlyphData
        {
            NumberOfContours = reader.ReadInt16(),
            XMin = reader.ReadInt16(),
            YMin = reader.ReadInt16(),
            XMax = reader.ReadInt16(),
            YMax = reader.ReadInt16()
        };

        // Capture complete raw glyph data for subsetting/serialization
        // This preserves all outline data (contours, points, flags, coordinates, instructions, hints)
        // without needing to parse and re-serialize complex glyph structures
        if (length > 0)
        {
            reader.Seek(table.Offset + offset); // Rewind to start of glyph data
            glyph.RawGlyphData = reader.ReadBytes((int)length);
        }
        else
        {
            glyph.RawGlyphData = Array.Empty<byte>();
        }

        return glyph;
    }

    /// <summary>
    /// Gets the glyph data for a specific glyph index.
    /// Returns null if the glyph has no outline data.
    /// </summary>
    public static GlyphData? GetGlyphData(Stream stream, TableRecord table, FontFile font, ushort glyphIndex)
    {
        if (glyphIndex >= font.GlyphCount)
            return null;

        return ParseGlyphHeader(stream, table, font, glyphIndex);
    }
}
