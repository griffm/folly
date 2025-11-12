using System;
using System.Collections.Generic;

namespace Folly.Fonts.Models;

/// <summary>
/// Represents a parsed TrueType or OpenType font file.
/// </summary>
public class FontFile
{
    /// <summary>
    /// Font family name (from 'name' table).
    /// </summary>
    public string FamilyName { get; set; } = string.Empty;

    /// <summary>
    /// Font subfamily name, e.g., "Regular", "Bold", "Italic" (from 'name' table).
    /// </summary>
    public string SubfamilyName { get; set; } = string.Empty;

    /// <summary>
    /// Full font name (from 'name' table).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// PostScript name (from 'name' table).
    /// </summary>
    public string PostScriptName { get; set; } = string.Empty;

    /// <summary>
    /// Font version string (from 'name' table).
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Units per em (from 'head' table). Typically 1000 or 2048.
    /// This is the number of font units per em square.
    /// </summary>
    public ushort UnitsPerEm { get; set; }

    /// <summary>
    /// Ascender value in font units (from 'hhea' table).
    /// Distance from baseline to top of font bounding box.
    /// </summary>
    public short Ascender { get; set; }

    /// <summary>
    /// Descender value in font units (from 'hhea' table).
    /// Distance from baseline to bottom of font bounding box (typically negative).
    /// </summary>
    public short Descender { get; set; }

    /// <summary>
    /// Line gap value in font units (from 'hhea' table).
    /// Additional space between lines.
    /// </summary>
    public short LineGap { get; set; }

    /// <summary>
    /// Number of glyphs in the font (from 'maxp' table).
    /// </summary>
    public ushort GlyphCount { get; set; }

    /// <summary>
    /// Character to glyph index mapping (from 'cmap' table).
    /// Maps Unicode code points to glyph indices.
    /// </summary>
    public Dictionary<int, ushort> CharacterToGlyphIndex { get; set; } = new();

    /// <summary>
    /// Horizontal advance widths for each glyph (from 'hmtx' table).
    /// Index is glyph index, value is advance width in font units.
    /// </summary>
    public ushort[] GlyphAdvanceWidths { get; set; } = Array.Empty<ushort>();

    /// <summary>
    /// Left side bearings for each glyph (from 'hmtx' table).
    /// Index is glyph index, value is left side bearing in font units.
    /// </summary>
    public short[] GlyphLeftSideBearings { get; set; } = Array.Empty<short>();

    /// <summary>
    /// Glyph offsets in the 'glyf' table (from 'loca' table).
    /// Used to locate glyph data. Null if this is a CFF font.
    /// </summary>
    public uint[]? GlyphOffsets { get; set; }

    /// <summary>
    /// Kerning pairs mapping (from 'kern' table, if present).
    /// Key is (leftGlyphIndex, rightGlyphIndex), value is kerning adjustment in font units.
    /// </summary>
    public Dictionary<(ushort, ushort), short> KerningPairs { get; set; } = new();

    /// <summary>
    /// Font bounding box minimum X coordinate (from 'head' table).
    /// </summary>
    public short XMin { get; set; }

    /// <summary>
    /// Font bounding box minimum Y coordinate (from 'head' table).
    /// </summary>
    public short YMin { get; set; }

    /// <summary>
    /// Font bounding box maximum X coordinate (from 'head' table).
    /// </summary>
    public short XMax { get; set; }

    /// <summary>
    /// Font bounding box maximum Y coordinate (from 'head' table).
    /// </summary>
    public short YMax { get; set; }

    /// <summary>
    /// Index to location format (from 'head' table).
    /// 0 for short offsets (Offset16), 1 for long offsets (Offset32).
    /// </summary>
    public short IndexToLocFormat { get; set; }

    /// <summary>
    /// Whether this is a TrueType font (has 'glyf' table) or OpenType/CFF font (has 'CFF ' table).
    /// </summary>
    public bool IsTrueType { get; set; }

    /// <summary>
    /// OS/2 table metrics (Windows-specific font metrics).
    /// </summary>
    public OS2Table? OS2 { get; set; }

    /// <summary>
    /// PostScript table data.
    /// </summary>
    public PostTable? Post { get; set; }

    /// <summary>
    /// Gets the advance width for a character in font units.
    /// Returns 0 if the character is not in the font.
    /// </summary>
    public ushort GetAdvanceWidth(char character)
    {
        int codePoint = character;
        if (!CharacterToGlyphIndex.TryGetValue(codePoint, out ushort glyphIndex))
            return 0;

        if (glyphIndex >= GlyphAdvanceWidths.Length)
            return 0;

        return GlyphAdvanceWidths[glyphIndex];
    }

    /// <summary>
    /// Gets the kerning adjustment between two characters in font units.
    /// Returns 0 if no kerning is defined for this pair.
    /// </summary>
    public short GetKerning(char left, char right)
    {
        int leftCodePoint = left;
        int rightCodePoint = right;

        if (!CharacterToGlyphIndex.TryGetValue(leftCodePoint, out ushort leftGlyphIndex))
            return 0;

        if (!CharacterToGlyphIndex.TryGetValue(rightCodePoint, out ushort rightGlyphIndex))
            return 0;

        KerningPairs.TryGetValue((leftGlyphIndex, rightGlyphIndex), out short kerning);
        return kerning;
    }

    /// <summary>
    /// Checks if the font contains a specific character.
    /// </summary>
    public bool HasCharacter(char character)
    {
        return CharacterToGlyphIndex.ContainsKey(character);
    }
}
