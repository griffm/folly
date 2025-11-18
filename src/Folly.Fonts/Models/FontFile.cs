using System;
using System.Collections.Generic;
using Folly.Fonts.Logging;
using Folly.Fonts.CFF;
using Folly.Fonts.OpenType;

namespace Folly.Fonts.Models;

/// <summary>
/// Represents a parsed TrueType or OpenType font file.
/// </summary>
public class FontFile
{
    /// <summary>
    /// Logger for diagnostic messages during font parsing.
    /// Set by the caller to enable logging; defaults to NullLogger.
    /// </summary>
    internal ILogger Logger { get; set; } = NullLogger.Instance;
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
    /// Supports full range of glyph indices including those beyond 65535 for large fonts.
    /// </summary>
    public Dictionary<int, uint> CharacterToGlyphIndex { get; set; } = new();

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
    /// Glyph data for each glyph (from 'glyf' table for TrueType fonts).
    /// Contains glyph bounding boxes and outline information.
    /// Null for CFF fonts or if glyf table was not parsed.
    /// </summary>
    public GlyphData[]? Glyphs { get; set; }

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
    /// Font header ('head') table data.
    /// </summary>
    public HeadTable? Head { get; set; }

    /// <summary>
    /// OS/2 table metrics (Windows-specific font metrics).
    /// </summary>
    public OS2Table? OS2 { get; set; }

    /// <summary>
    /// PostScript table data.
    /// </summary>
    public PostTable? Post { get; set; }

    /// <summary>
    /// GSUB (Glyph Substitution) table data for OpenType features like ligatures.
    /// Null if the font does not have a GSUB table.
    /// </summary>
    public GsubData? Gsub { get; set; }

    /// <summary>
    /// GPOS (Glyph Positioning) table data for OpenType features like kerning and mark positioning.
    /// Null if the font does not have a GPOS table.
    /// </summary>
    public GposData? Gpos { get; set; }

    /// <summary>
    /// CFF (Compact Font Format) table data for OpenType fonts with PostScript outlines.
    /// Null if the font does not have a CFF table (i.e., it's a TrueType font).
    /// </summary>
    public CffData? Cff { get; set; }

    /// <summary>
    /// Gets the advance width for a character in font units.
    /// Returns 0 if the character is not in the font.
    /// </summary>
    public ushort GetAdvanceWidth(char character)
    {
        int codePoint = character;
        if (!CharacterToGlyphIndex.TryGetValue(codePoint, out uint glyphIndex))
            return 0;

        if (glyphIndex >= (uint)GlyphAdvanceWidths.Length)
            return 0;

        return GlyphAdvanceWidths[glyphIndex];
    }

    /// <summary>
    /// Gets the kerning adjustment between two characters in font units.
    /// Returns 0 if no kerning is defined for this pair.
    /// Note: Kerning pairs use 16-bit indices. Glyphs beyond 65535 will not have kerning.
    /// </summary>
    public short GetKerning(char left, char right)
    {
        int leftCodePoint = left;
        int rightCodePoint = right;

        if (!CharacterToGlyphIndex.TryGetValue(leftCodePoint, out uint leftGlyphIndex))
            return 0;

        if (!CharacterToGlyphIndex.TryGetValue(rightCodePoint, out uint rightGlyphIndex))
            return 0;

        // Kerning tables use 16-bit glyph indices
        if (leftGlyphIndex > 0xFFFF || rightGlyphIndex > 0xFFFF)
            return 0;

        KerningPairs.TryGetValue(((ushort)leftGlyphIndex, (ushort)rightGlyphIndex), out short kerning);
        return kerning;
    }

    /// <summary>
    /// Checks if the font contains a specific character.
    /// </summary>
    public bool HasCharacter(char character)
    {
        return CharacterToGlyphIndex.ContainsKey(character);
    }

    /// <summary>
    /// Gets the glyph index for a character.
    /// Returns null if the character is not in the font.
    /// </summary>
    public uint? GetGlyphIndex(char character)
    {
        if (CharacterToGlyphIndex.TryGetValue(character, out uint glyphIndex))
            return glyphIndex;
        return null;
    }

    /// <summary>
    /// Gets the glyph data for a character.
    /// Returns null if the character is not in the font or glyph data is not available.
    /// </summary>
    public GlyphData? GetGlyphData(char character)
    {
        var glyphIndex = GetGlyphIndex(character);
        if (glyphIndex == null || Glyphs == null || glyphIndex >= (uint)Glyphs.Length)
            return null;

        return Glyphs[glyphIndex.Value];
    }

    /// <summary>
    /// Calculates the total width of a string in font units, including kerning.
    /// Returns 0 if any character is not in the font.
    /// </summary>
    public int GetTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int totalWidth = 0;
        char? previousChar = null;

        foreach (char c in text)
        {
            // Add advance width
            totalWidth += GetAdvanceWidth(c);

            // Add kerning if we have a previous character
            if (previousChar.HasValue)
            {
                totalWidth += GetKerning(previousChar.Value, c);
            }

            previousChar = c;
        }

        return totalWidth;
    }

    /// <summary>
    /// Converts font units to pixels at a given point size and DPI.
    /// </summary>
    /// <param name="fontUnits">Value in font units.</param>
    /// <param name="pointSize">Font size in points (e.g., 12pt).</param>
    /// <param name="dpi">Dots per inch (typically 72 or 96).</param>
    /// <returns>Value in pixels.</returns>
    public double FontUnitsToPixels(int fontUnits, double pointSize, double dpi = 72.0)
    {
        return fontUnits * pointSize * dpi / (72.0 * UnitsPerEm);
    }

    /// <summary>
    /// Gets the line height in font units.
    /// Line height = ascender - descender + line gap.
    /// </summary>
    public int GetLineHeight()
    {
        return Ascender - Descender + LineGap;
    }
}
