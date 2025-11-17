using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Folly.Fonts.Models;

namespace Folly.Fonts;

/// <summary>
/// Creates font subsets containing only the glyphs used in a document.
/// This significantly reduces PDF file size by embedding only necessary glyphs.
/// </summary>
public class FontSubsetter
{
    /// <summary>
    /// Creates a subset of a font containing only the specified characters.
    /// </summary>
    /// <param name="font">The original font file.</param>
    /// <param name="usedCharacters">Set of characters that are actually used.</param>
    /// <returns>Byte array containing the subsetted font in TrueType format.</returns>
    /// <exception cref="NotSupportedException">Thrown when attempting to subset a CFF/OpenType font.</exception>
    /// <remarks>
    /// Currently only TrueType fonts (with 'glyf' outlines) are supported for subsetting.
    /// CFF/OpenType fonts (with CFF outlines) require CharString decompilation and are not yet supported.
    /// For CFF fonts, consider embedding the full font instead of subsetting.
    /// </remarks>
    public static byte[] CreateSubset(FontFile font, HashSet<char> usedCharacters)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        if (usedCharacters == null || usedCharacters.Count == 0)
            throw new ArgumentException("Must specify at least one character to include in subset", nameof(usedCharacters));

        if (!font.IsTrueType)
        {
            font.Logger.Error($"Cannot subset font '{font.FamilyName}': CFF/OpenType fonts with PostScript outlines are not supported for subsetting. " +
                             "Full CharString decompilation and recompilation would be required. " +
                             "Workaround: Embed the full font instead by disabling subsetting (SubsetFonts = false in PdfOptions).");

            throw new NotSupportedException(
                $"Font subsetting is not supported for CFF/OpenType fonts with PostScript outlines (font: '{font.FamilyName}'). " +
                "Only TrueType fonts with 'glyf' outlines can be subset. " +
                "To use this font, disable font subsetting (set PdfOptions.SubsetFonts = false) to embed the full font. " +
                "Note: This will increase PDF file size.");
        }

        // Step 1: Build glyph ID mapping (old index -> new index)
        var glyphMapping = BuildGlyphMapping(font, usedCharacters);

        // Step 2: Create subset font tables
        var subsetFont = CreateSubsetFontFile(font, glyphMapping);

        // Step 3: Serialize to TrueType format
        return SerializeToTrueType(subsetFont);
    }

    /// <summary>
    /// Builds a mapping from old glyph indices to new glyph indices in the subset.
    /// Always includes glyph 0 (.notdef) as required by the TrueType spec.
    /// Supports fonts with glyph indices beyond 65535.
    /// </summary>
    private static Dictionary<uint, ushort> BuildGlyphMapping(FontFile font, HashSet<char> usedCharacters)
    {
        var glyphMapping = new Dictionary<uint, ushort>();
        ushort newGlyphIndex = 0;

        // Always include glyph 0 (.notdef) - required by TrueType spec
        glyphMapping[0] = newGlyphIndex++;

        // Add glyphs for all used characters
        var sortedChars = usedCharacters.OrderBy(c => c).ToList();
        foreach (char c in sortedChars)
        {
            var oldGlyphIndex = font.GetGlyphIndex(c);
            if (oldGlyphIndex.HasValue && !glyphMapping.ContainsKey(oldGlyphIndex.Value))
            {
                glyphMapping[oldGlyphIndex.Value] = newGlyphIndex++;
            }
        }

        return glyphMapping;
    }

    /// <summary>
    /// Creates a new FontFile containing only the subset of glyphs.
    /// </summary>
    private static FontFile CreateSubsetFontFile(FontFile originalFont, Dictionary<uint, ushort> glyphMapping)
    {
        var subsetFont = new FontFile
        {
            // Copy basic font info
            FamilyName = originalFont.FamilyName,
            SubfamilyName = originalFont.SubfamilyName,
            FullName = originalFont.FullName,
            // Add PDF/A compliant 6-character subset tag to PostScript name (e.g., "ABCDEF+FontName")
            PostScriptName = GenerateSubsetPostScriptName(originalFont.PostScriptName),
            Version = originalFont.Version,
            UnitsPerEm = originalFont.UnitsPerEm,
            Ascender = originalFont.Ascender,
            Descender = originalFont.Descender,
            LineGap = originalFont.LineGap,
            XMin = originalFont.XMin,
            YMin = originalFont.YMin,
            XMax = originalFont.XMax,
            YMax = originalFont.YMax,
            IndexToLocFormat = originalFont.IndexToLocFormat,
            IsTrueType = true,
            GlyphCount = (ushort)glyphMapping.Count,
            // Clone tables instead of referencing to ensure independent subset
            Head = CloneHeadTable(originalFont.Head),
            OS2 = CloneOS2Table(originalFont.OS2),
            Post = ClonePostTable(originalFont.Post),
        };

        // Build reverse mapping for character lookups
        var newCharToGlyphIndex = new Dictionary<int, uint>();
        foreach (var kvp in originalFont.CharacterToGlyphIndex)
        {
            if (glyphMapping.TryGetValue(kvp.Value, out ushort newIndex))
            {
                newCharToGlyphIndex[kvp.Key] = newIndex;
            }
        }
        subsetFont.CharacterToGlyphIndex = newCharToGlyphIndex;

        // Copy glyph metrics for subset glyphs
        subsetFont.GlyphAdvanceWidths = new ushort[subsetFont.GlyphCount];
        subsetFont.GlyphLeftSideBearings = new short[subsetFont.GlyphCount];

        foreach (var kvp in glyphMapping)
        {
            uint oldIndex = kvp.Key;
            ushort newIndex = kvp.Value;

            if (oldIndex < (uint)originalFont.GlyphAdvanceWidths.Length)
            {
                subsetFont.GlyphAdvanceWidths[newIndex] = originalFont.GlyphAdvanceWidths[oldIndex];
            }

            if (oldIndex < (uint)originalFont.GlyphLeftSideBearings.Length)
            {
                subsetFont.GlyphLeftSideBearings[newIndex] = originalFont.GlyphLeftSideBearings[oldIndex];
            }
        }

        // Copy glyph data for subset glyphs
        if (originalFont.Glyphs != null)
        {
            subsetFont.Glyphs = new GlyphData[subsetFont.GlyphCount];
            foreach (var kvp in glyphMapping)
            {
                uint oldIndex = kvp.Key;
                ushort newIndex = kvp.Value;

                if (oldIndex < (uint)originalFont.Glyphs.Length)
                {
                    subsetFont.Glyphs[newIndex] = originalFont.Glyphs[oldIndex];
                }
            }
        }

        // Remap kerning pairs to use new glyph indices in the subset.
        // For each kerning pair in the original font, we check if both glyphs
        // (left and right) are included in the subset. If both are present,
        // we add the kerning pair to the subset using the new (remapped) glyph indices.
        // This ensures proper kerning in subset fonts for character pairs like "AV", "To", etc.
        subsetFont.KerningPairs = new Dictionary<(ushort, ushort), short>();
        foreach (var kvp in originalFont.KerningPairs)
        {
            var (leftOld, rightOld) = kvp.Key;
            if (glyphMapping.TryGetValue(leftOld, out ushort leftNew) &&
                glyphMapping.TryGetValue(rightOld, out ushort rightNew))
            {
                subsetFont.KerningPairs[(leftNew, rightNew)] = kvp.Value;
            }
        }

        return subsetFont;
    }

    /// <summary>
    /// Generates a subset PostScript name with a unique 6-character tag prefix.
    /// Format: "ABCDEF+OriginalName"
    /// PDF/A spec requires 6 uppercase letters (A-Z) followed by "+" and the original name.
    /// </summary>
    private static string GenerateSubsetPostScriptName(string originalName)
    {
        // Generate a pseudo-random 6-character tag based on the original name
        // In practice, this should be unique per subset to avoid naming conflicts
        int hash = originalName.GetHashCode();
        char[] tag = new char[6];
        for (int i = 0; i < 6; i++)
        {
            tag[i] = (char)('A' + ((hash >> (i * 5)) & 0x1F) % 26);
        }

        return new string(tag) + "+" + originalName;
    }

    /// <summary>
    /// Clones a HeadTable to ensure subset font has independent metadata.
    /// </summary>
    private static HeadTable? CloneHeadTable(HeadTable? source)
    {
        if (source == null)
            return null;

        return new HeadTable
        {
            FontRevision = source.FontRevision,
            Created = source.Created,
            Modified = source.Modified,
            Flags = source.Flags,
            MacStyle = source.MacStyle
        };
    }

    /// <summary>
    /// Clones an OS2Table to ensure subset font has independent metrics.
    /// </summary>
    private static OS2Table? CloneOS2Table(OS2Table? source)
    {
        if (source == null)
            return null;

        return new OS2Table
        {
            Version = source.Version,
            XAvgCharWidth = source.XAvgCharWidth,
            WeightClass = source.WeightClass,
            WidthClass = source.WidthClass,
            Type = source.Type,
            TypoAscender = source.TypoAscender,
            TypoDescender = source.TypoDescender,
            TypoLineGap = source.TypoLineGap,
            WinAscent = source.WinAscent,
            WinDescent = source.WinDescent
        };
    }

    /// <summary>
    /// Clones a PostTable to ensure subset font has independent PostScript info.
    /// </summary>
    private static PostTable? ClonePostTable(PostTable? source)
    {
        if (source == null)
            return null;

        return new PostTable
        {
            Version = source.Version,
            ItalicAngle = source.ItalicAngle,
            UnderlinePosition = source.UnderlinePosition,
            UnderlineThickness = source.UnderlineThickness,
            IsFixedPitch = source.IsFixedPitch
        };
    }

    /// <summary>
    /// Serializes a subset font to TrueType format.
    /// </summary>
    private static byte[] SerializeToTrueType(FontFile font)
    {
        return TrueTypeFontSerializer.Serialize(font);
    }
}
