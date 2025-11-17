using System;
using System.Collections.Generic;
using System.IO;
using Folly.Fonts.Models;
using Folly.Fonts.OpenType;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'GSUB' (Glyph Substitution) table.
/// Implements OpenType advanced typography features like ligatures and contextual alternates.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/gsub
/// </summary>
public static class GsubTableParser
{
    /// <summary>
    /// Parses the 'GSUB' table and populates the GSUB data in the font file.
    /// This is an optional table, so missing GSUB data is not an error.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);
        long tableStart = reader.Position;

        try
        {
            var gsub = new GsubData();

            // Read GSUB header
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();

            if (majorVersion != 1)
            {
                // Unsupported version
                return;
            }

            // Offsets to main subtables
            ushort scriptListOffset = reader.ReadUInt16();
            ushort featureListOffset = reader.ReadUInt16();
            ushort lookupListOffset = reader.ReadUInt16();

            // Parse script list
            if (scriptListOffset > 0)
            {
                reader.Seek(tableStart + scriptListOffset);
                ParseScriptList(reader, tableStart + scriptListOffset, gsub);
            }

            // Parse feature list
            if (featureListOffset > 0)
            {
                reader.Seek(tableStart + featureListOffset);
                ParseFeatureList(reader, tableStart + featureListOffset, gsub);
            }

            // Parse lookup list
            if (lookupListOffset > 0)
            {
                reader.Seek(tableStart + lookupListOffset);
                ParseLookupList(reader, tableStart + lookupListOffset, gsub);
            }

            font.Gsub = gsub;
        }
        catch (Exception ex)
        {
            // If parsing fails, don't crash - just don't populate GSUB data
            // This allows fonts with malformed GSUB tables to still be used
            font.Logger.Warning($"GSUB table parsing failed: {ex.Message}. " +
                              "Advanced typographic features (ligatures, substitutions) may not work correctly.", ex);
            font.Gsub = null;
        }
    }

    private static void ParseScriptList(BigEndianBinaryReader reader, long scriptListStart, GsubData gsub)
    {
        ushort scriptCount = reader.ReadUInt16();

        for (int i = 0; i < scriptCount; i++)
        {
            string scriptTag = reader.ReadTag();
            ushort scriptOffset = reader.ReadUInt16();

            long returnPos = reader.Position;

            reader.Seek(scriptListStart + scriptOffset);
            var script = ParseScript(reader, scriptListStart + scriptOffset, scriptTag);
            gsub.Scripts.Add(script);

            reader.Seek(returnPos);
        }
    }

    private static OpenTypeScript ParseScript(BigEndianBinaryReader reader, long scriptStart, string scriptTag)
    {
        var script = new OpenTypeScript { Tag = scriptTag };

        ushort defaultLangSysOffset = reader.ReadUInt16();
        ushort langSysCount = reader.ReadUInt16();

        // Parse default language system
        if (defaultLangSysOffset > 0)
        {
            reader.Seek(scriptStart + defaultLangSysOffset);
            var defaultLangSys = ParseLanguageSystem(reader, "dflt");
            script.LanguageSystems.Add(defaultLangSys);
        }

        // Parse additional language systems
        for (int i = 0; i < langSysCount; i++)
        {
            string langTag = reader.ReadTag();
            ushort langSysOffset = reader.ReadUInt16();

            long returnPos = reader.Position;

            reader.Seek(scriptStart + langSysOffset);
            var langSys = ParseLanguageSystem(reader, langTag);
            script.LanguageSystems.Add(langSys);

            reader.Seek(returnPos);
        }

        return script;
    }

    private static OpenTypeLanguageSystem ParseLanguageSystem(BigEndianBinaryReader reader, string langTag)
    {
        var langSys = new OpenTypeLanguageSystem { Tag = langTag };

        ushort lookupOrder = reader.ReadUInt16();  // Reserved, should be NULL
        ushort requiredFeatureIndex = reader.ReadUInt16();
        ushort featureIndexCount = reader.ReadUInt16();

        for (int i = 0; i < featureIndexCount; i++)
        {
            ushort featureIndex = reader.ReadUInt16();
            langSys.FeatureIndices.Add(featureIndex);
        }

        return langSys;
    }

    private static void ParseFeatureList(BigEndianBinaryReader reader, long featureListStart, GsubData gsub)
    {
        ushort featureCount = reader.ReadUInt16();

        for (int i = 0; i < featureCount; i++)
        {
            string featureTag = reader.ReadTag();
            ushort featureOffset = reader.ReadUInt16();

            long returnPos = reader.Position;

            reader.Seek(featureListStart + featureOffset);
            var feature = ParseFeature(reader, featureTag);
            gsub.Features.Add(feature);

            reader.Seek(returnPos);
        }
    }

    private static OpenTypeFeature ParseFeature(BigEndianBinaryReader reader, string featureTag)
    {
        var feature = new OpenTypeFeature { Tag = featureTag };

        ushort featureParams = reader.ReadUInt16();  // Reserved, should be NULL
        ushort lookupIndexCount = reader.ReadUInt16();

        for (int i = 0; i < lookupIndexCount; i++)
        {
            ushort lookupIndex = reader.ReadUInt16();
            feature.LookupIndices.Add(lookupIndex);
        }

        return feature;
    }

    private static void ParseLookupList(BigEndianBinaryReader reader, long lookupListStart, GsubData gsub)
    {
        ushort lookupCount = reader.ReadUInt16();

        for (int i = 0; i < lookupCount; i++)
        {
            ushort lookupOffset = reader.ReadUInt16();

            long returnPos = reader.Position;

            reader.Seek(lookupListStart + lookupOffset);
            var lookup = ParseLookup(reader, lookupListStart + lookupOffset);
            gsub.Lookups.Add(lookup);

            reader.Seek(returnPos);
        }
    }

    private static GsubLookup ParseLookup(BigEndianBinaryReader reader, long lookupStart)
    {
        var lookup = new GsubLookup();

        lookup.LookupType = (GsubLookupType)reader.ReadUInt16();
        lookup.LookupFlag = reader.ReadUInt16();
        ushort subTableCount = reader.ReadUInt16();

        // Read subtable offsets
        var subtableOffsets = new ushort[subTableCount];
        for (int i = 0; i < subTableCount; i++)
        {
            subtableOffsets[i] = reader.ReadUInt16();
        }

        // Parse each subtable based on lookup type
        foreach (var offset in subtableOffsets)
        {
            long returnPos = reader.Position;

            reader.Seek(lookupStart + offset);
            var subtable = ParseSubtable(reader, lookupStart + offset, lookup.LookupType);
            if (subtable != null)
            {
                lookup.Subtables.Add(subtable);
            }

            reader.Seek(returnPos);
        }

        return lookup;
    }

    private static IGsubSubtable? ParseSubtable(BigEndianBinaryReader reader, long subtableStart, GsubLookupType lookupType)
    {
        return lookupType switch
        {
            GsubLookupType.Single => ParseSingleSubstitution(reader, subtableStart),
            GsubLookupType.Ligature => ParseLigatureSubstitution(reader, subtableStart),
            GsubLookupType.Alternate => ParseAlternateSubstitution(reader, subtableStart),
            GsubLookupType.Multiple => ParseMultipleSubstitution(reader, subtableStart),
            // Context, chaining context, and extension subtables are more complex
            // and less commonly used - can be implemented later if needed
            _ => null
        };
    }

    private static SingleSubstitutionSubtable ParseSingleSubstitution(BigEndianBinaryReader reader, long subtableStart)
    {
        var subtable = new SingleSubstitutionSubtable();

        ushort substFormat = reader.ReadUInt16();

        if (substFormat == 1)
        {
            // Format 1: Single substitution with delta
            ushort coverageOffset = reader.ReadUInt16();
            short deltaGlyphID = reader.ReadInt16();

            reader.Seek(subtableStart + coverageOffset);
            var coverage = ParseCoverage(reader);

            foreach (var glyphId in coverage)
            {
                ushort outputGlyph = (ushort)(glyphId + deltaGlyphID);
                subtable.Substitutions[glyphId] = outputGlyph;
            }
        }
        else if (substFormat == 2)
        {
            // Format 2: Single substitution with array
            ushort coverageOffset = reader.ReadUInt16();
            ushort glyphCount = reader.ReadUInt16();

            var substituteGlyphIDs = new ushort[glyphCount];
            for (int i = 0; i < glyphCount; i++)
            {
                substituteGlyphIDs[i] = reader.ReadUInt16();
            }

            long returnPos = reader.Position;
            reader.Seek(subtableStart + coverageOffset);
            var coverage = ParseCoverage(reader);
            reader.Seek(returnPos);

            for (int i = 0; i < coverage.Count && i < glyphCount; i++)
            {
                subtable.Substitutions[coverage[i]] = substituteGlyphIDs[i];
            }
        }

        return subtable;
    }

    private static LigatureSubstitutionSubtable ParseLigatureSubstitution(BigEndianBinaryReader reader, long subtableStart)
    {
        var subtable = new LigatureSubstitutionSubtable();

        ushort substFormat = reader.ReadUInt16();

        if (substFormat != 1)
            return subtable;

        ushort coverageOffset = reader.ReadUInt16();
        ushort ligSetCount = reader.ReadUInt16();

        var ligSetOffsets = new ushort[ligSetCount];
        for (int i = 0; i < ligSetCount; i++)
        {
            ligSetOffsets[i] = reader.ReadUInt16();
        }

        long returnPos = reader.Position;
        reader.Seek(subtableStart + coverageOffset);
        var coverage = ParseCoverage(reader);
        reader.Seek(returnPos);

        for (int i = 0; i < ligSetCount && i < coverage.Count; i++)
        {
            if (ligSetOffsets[i] == 0)
                continue;

            ushort firstGlyph = coverage[i];

            reader.Seek(subtableStart + ligSetOffsets[i]);
            ushort ligatureCount = reader.ReadUInt16();

            var ligatureOffsets = new ushort[ligatureCount];
            for (int j = 0; j < ligatureCount; j++)
            {
                ligatureOffsets[j] = reader.ReadUInt16();
            }

            var ligatures = new List<Ligature>();
            long ligSetPos = reader.Position - (ligatureCount * 2) - 2;

            foreach (var ligOffset in ligatureOffsets)
            {
                reader.Seek(ligSetPos + ligOffset);

                ushort ligGlyph = reader.ReadUInt16();
                ushort compCount = reader.ReadUInt16();

                var components = new ushort[compCount - 1];  // First component is the key
                for (int k = 0; k < compCount - 1; k++)
                {
                    components[k] = reader.ReadUInt16();
                }

                ligatures.Add(new Ligature
                {
                    LigatureGlyph = ligGlyph,
                    ComponentGlyphIds = components
                });
            }

            subtable.Ligatures[firstGlyph] = ligatures;
        }

        return subtable;
    }

    private static AlternateSubstitutionSubtable ParseAlternateSubstitution(BigEndianBinaryReader reader, long subtableStart)
    {
        var subtable = new AlternateSubstitutionSubtable();

        ushort substFormat = reader.ReadUInt16();
        if (substFormat != 1)
            return subtable;

        ushort coverageOffset = reader.ReadUInt16();
        ushort alternateSetCount = reader.ReadUInt16();

        var alternateSetOffsets = new ushort[alternateSetCount];
        for (int i = 0; i < alternateSetCount; i++)
        {
            alternateSetOffsets[i] = reader.ReadUInt16();
        }

        long returnPos = reader.Position;
        reader.Seek(subtableStart + coverageOffset);
        var coverage = ParseCoverage(reader);
        reader.Seek(returnPos);

        for (int i = 0; i < alternateSetCount && i < coverage.Count; i++)
        {
            if (alternateSetOffsets[i] == 0)
                continue;

            ushort glyphId = coverage[i];

            reader.Seek(subtableStart + alternateSetOffsets[i]);
            ushort glyphCount = reader.ReadUInt16();

            var alternates = new List<ushort>();
            for (int j = 0; j < glyphCount; j++)
            {
                alternates.Add(reader.ReadUInt16());
            }

            subtable.Alternates[glyphId] = alternates;
        }

        return subtable;
    }

    private static MultipleSubstitutionSubtable ParseMultipleSubstitution(BigEndianBinaryReader reader, long subtableStart)
    {
        var subtable = new MultipleSubstitutionSubtable();

        ushort substFormat = reader.ReadUInt16();
        if (substFormat != 1)
            return subtable;

        ushort coverageOffset = reader.ReadUInt16();
        ushort sequenceCount = reader.ReadUInt16();

        var sequenceOffsets = new ushort[sequenceCount];
        for (int i = 0; i < sequenceCount; i++)
        {
            sequenceOffsets[i] = reader.ReadUInt16();
        }

        long returnPos = reader.Position;
        reader.Seek(subtableStart + coverageOffset);
        var coverage = ParseCoverage(reader);
        reader.Seek(returnPos);

        for (int i = 0; i < sequenceCount && i < coverage.Count; i++)
        {
            if (sequenceOffsets[i] == 0)
                continue;

            ushort glyphId = coverage[i];

            reader.Seek(subtableStart + sequenceOffsets[i]);
            ushort glyphCount = reader.ReadUInt16();

            var substituteGlyphs = new ushort[glyphCount];
            for (int j = 0; j < glyphCount; j++)
            {
                substituteGlyphs[j] = reader.ReadUInt16();
            }

            subtable.Substitutions[glyphId] = substituteGlyphs;
        }

        return subtable;
    }

    /// <summary>
    /// Parses a coverage table, which lists glyphs covered by a subtable.
    /// </summary>
    private static List<ushort> ParseCoverage(BigEndianBinaryReader reader)
    {
        var glyphs = new List<ushort>();

        ushort coverageFormat = reader.ReadUInt16();

        if (coverageFormat == 1)
        {
            // Format 1: List of glyph IDs
            ushort glyphCount = reader.ReadUInt16();
            for (int i = 0; i < glyphCount; i++)
            {
                glyphs.Add(reader.ReadUInt16());
            }
        }
        else if (coverageFormat == 2)
        {
            // Format 2: Range of glyph IDs
            ushort rangeCount = reader.ReadUInt16();
            for (int i = 0; i < rangeCount; i++)
            {
                ushort startGlyphID = reader.ReadUInt16();
                ushort endGlyphID = reader.ReadUInt16();
                ushort startCoverageIndex = reader.ReadUInt16();

                for (ushort g = startGlyphID; g <= endGlyphID; g++)
                {
                    glyphs.Add(g);
                }
            }
        }

        return glyphs;
    }
}
