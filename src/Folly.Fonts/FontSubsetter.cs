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
    public static byte[] CreateSubset(FontFile font, HashSet<char> usedCharacters)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        if (usedCharacters == null || usedCharacters.Count == 0)
            throw new ArgumentException("Must specify at least one character to include in subset", nameof(usedCharacters));

        if (!font.IsTrueType)
            throw new NotSupportedException("Font subsetting is currently only supported for TrueType fonts. OpenType/CFF support coming soon.");

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
    /// </summary>
    private static Dictionary<ushort, ushort> BuildGlyphMapping(FontFile font, HashSet<char> usedCharacters)
    {
        var glyphMapping = new Dictionary<ushort, ushort>();
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
    private static FontFile CreateSubsetFontFile(FontFile originalFont, Dictionary<ushort, ushort> glyphMapping)
    {
        var subsetFont = new FontFile
        {
            // Copy basic font info
            FamilyName = originalFont.FamilyName,
            SubfamilyName = originalFont.SubfamilyName,
            FullName = originalFont.FullName,
            // TODO: Add subset tag to PostScript name (e.g., "ABCDEF+FontName")
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
            OS2 = originalFont.OS2, // TODO: Clone instead of reference
            Post = originalFont.Post, // TODO: Clone instead of reference
        };

        // Build reverse mapping for character lookups
        var newCharToGlyphIndex = new Dictionary<int, ushort>();
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
            ushort oldIndex = kvp.Key;
            ushort newIndex = kvp.Value;

            if (oldIndex < originalFont.GlyphAdvanceWidths.Length)
            {
                subsetFont.GlyphAdvanceWidths[newIndex] = originalFont.GlyphAdvanceWidths[oldIndex];
            }

            if (oldIndex < originalFont.GlyphLeftSideBearings.Length)
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
                ushort oldIndex = kvp.Key;
                ushort newIndex = kvp.Value;

                if (oldIndex < originalFont.Glyphs.Length)
                {
                    subsetFont.Glyphs[newIndex] = originalFont.Glyphs[oldIndex];
                }
            }
        }

        // Copy kerning pairs that involve subset glyphs
        // TODO: Remap kerning pair indices to new glyph indices
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
    /// Serializes a subset font to TrueType format.
    /// </summary>
    private static byte[] SerializeToTrueType(FontFile font)
    {
        // TODO: Implement TrueType font file generation
        // This requires rebuilding all font tables in the correct format:
        // 1. head - Font header
        // 2. hhea - Horizontal header
        // 3. maxp - Maximum profile
        // 4. hmtx - Horizontal metrics
        // 5. name - Font naming table
        // 6. cmap - Character to glyph mapping
        // 7. loca - Glyph location index
        // 8. glyf - Glyph data
        // 9. post - PostScript information
        // 10. OS/2 - Windows metrics
        // Plus table directory and checksum calculation

        throw new NotImplementedException(
            "TrueType font serialization is not yet implemented. " +
            "This will be completed in the next iteration of Phase 3.2.");
    }
}
