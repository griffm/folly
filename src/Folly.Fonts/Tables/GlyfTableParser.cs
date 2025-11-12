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
    /// Parses the header of a single glyph.
    /// Returns null if the glyph has no outline data (e.g., space character).
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
                YMax = 0
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

        // TODO: Parse full outline data (simple glyph or composite glyph)
        // For now, we only parse the header which gives us the bounding box
        // This is sufficient for many use cases (layout, metrics, etc.)

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
