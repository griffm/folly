using System;
using System.Collections.Generic;
using System.IO;
using Folly.Fonts.Models;
using Folly.Fonts.OpenType;

namespace Folly.Fonts.Tables;

/// <summary>
/// Parser for the 'GPOS' (Glyph Positioning) table.
/// Implements OpenType advanced positioning features like kerning and mark attachment.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/gpos
/// </summary>
public static class GposTableParser
{
    /// <summary>
    /// Parses the 'GPOS' table and populates the GPOS data in the font file.
    /// This is an optional table, so missing GPOS data is not an error.
    /// </summary>
    public static void Parse(Stream stream, TableRecord table, FontFile font)
    {
        using var reader = FontFileReader.CreateTableReader(stream, table);
        long tableStart = reader.Position;

        try
        {
            var gpos = new GposData();

            // Read GPOS header
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

            // Parse script list (reuse GSUB parser logic)
            if (scriptListOffset > 0)
            {
                reader.Seek(tableStart + scriptListOffset);
                ParseScriptList(reader, tableStart + scriptListOffset, gpos);
            }

            // Parse feature list
            if (featureListOffset > 0)
            {
                reader.Seek(tableStart + featureListOffset);
                ParseFeatureList(reader, tableStart + featureListOffset, gpos);
            }

            // Parse lookup list
            if (lookupListOffset > 0)
            {
                reader.Seek(tableStart + lookupListOffset);
                ParseLookupList(reader, tableStart + lookupListOffset, gpos);
            }

            font.Gpos = gpos;
        }
        catch (Exception ex)
        {
            // If parsing fails, don't crash - just don't populate GPOS data
            font.Logger.Warning($"GPOS table parsing failed: {ex.Message}. " +
                              "Glyph positioning features may not work correctly.", ex);
            font.Gpos = null;
        }
    }

    private static void ParseScriptList(BigEndianBinaryReader reader, long scriptListStart, GposData gpos)
    {
        ushort scriptCount = reader.ReadUInt16();

        for (int i = 0; i < scriptCount; i++)
        {
            string scriptTag = reader.ReadTag();
            ushort scriptOffset = reader.ReadUInt16();

            long returnPos = reader.Position;

            reader.Seek(scriptListStart + scriptOffset);
            var script = ParseScript(reader, scriptListStart + scriptOffset, scriptTag);
            gpos.Scripts.Add(script);

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

    private static void ParseFeatureList(BigEndianBinaryReader reader, long featureListStart, GposData gpos)
    {
        ushort featureCount = reader.ReadUInt16();

        for (int i = 0; i < featureCount; i++)
        {
            string featureTag = reader.ReadTag();
            ushort featureOffset = reader.ReadUInt16();

            long returnPos = reader.Position;

            reader.Seek(featureListStart + featureOffset);
            var feature = ParseFeature(reader, featureTag);
            gpos.Features.Add(feature);

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

    private static void ParseLookupList(BigEndianBinaryReader reader, long lookupListStart, GposData gpos)
    {
        ushort lookupCount = reader.ReadUInt16();

        for (int i = 0; i < lookupCount; i++)
        {
            ushort lookupOffset = reader.ReadUInt16();

            long returnPos = reader.Position;

            reader.Seek(lookupListStart + lookupOffset);
            var lookup = ParseLookup(reader, lookupListStart + lookupOffset);
            gpos.Lookups.Add(lookup);

            reader.Seek(returnPos);
        }
    }

    private static GposLookup ParseLookup(BigEndianBinaryReader reader, long lookupStart)
    {
        var lookup = new GposLookup();

        lookup.LookupType = (GposLookupType)reader.ReadUInt16();
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

    private static IGposSubtable? ParseSubtable(BigEndianBinaryReader reader, long subtableStart, GposLookupType lookupType)
    {
        return lookupType switch
        {
            GposLookupType.SingleAdjustment => ParseSingleAdjustment(reader, subtableStart),
            GposLookupType.PairAdjustment => ParsePairAdjustment(reader, subtableStart),
            GposLookupType.CursiveAttachment => ParseCursiveAttachment(reader, subtableStart),
            GposLookupType.MarkToBase => ParseMarkToBase(reader, subtableStart),
            GposLookupType.MarkToMark => ParseMarkToMark(reader, subtableStart),
            // Context and chaining context can be implemented later if needed
            _ => null
        };
    }

    private static SingleAdjustmentSubtable ParseSingleAdjustment(BigEndianBinaryReader reader, long subtableStart)
    {
        var subtable = new SingleAdjustmentSubtable();

        ushort posFormat = reader.ReadUInt16();

        if (posFormat == 1)
        {
            // Format 1: Single positioning value
            ushort coverageOffset = reader.ReadUInt16();
            ushort valueFormat = reader.ReadUInt16();
            var valueRecord = ReadValueRecord(reader, valueFormat);

            long returnPos = reader.Position;
            reader.Seek(subtableStart + coverageOffset);
            var coverage = ParseCoverage(reader);
            reader.Seek(returnPos);

            foreach (var glyphId in coverage)
            {
                subtable.Adjustments[glyphId] = valueRecord;
            }
        }
        else if (posFormat == 2)
        {
            // Format 2: Array of positioning values
            ushort coverageOffset = reader.ReadUInt16();
            ushort valueFormat = reader.ReadUInt16();
            ushort valueCount = reader.ReadUInt16();

            var values = new ValueRecord[valueCount];
            for (int i = 0; i < valueCount; i++)
            {
                values[i] = ReadValueRecord(reader, valueFormat);
            }

            long returnPos = reader.Position;
            reader.Seek(subtableStart + coverageOffset);
            var coverage = ParseCoverage(reader);
            reader.Seek(returnPos);

            for (int i = 0; i < coverage.Count && i < valueCount; i++)
            {
                subtable.Adjustments[coverage[i]] = values[i];
            }
        }

        return subtable;
    }

    private static PairAdjustmentSubtable ParsePairAdjustment(BigEndianBinaryReader reader, long subtableStart)
    {
        var subtable = new PairAdjustmentSubtable();

        ushort posFormat = reader.ReadUInt16();

        if (posFormat == 1)
        {
            // Format 1: Adjustments for glyph pairs
            ushort coverageOffset = reader.ReadUInt16();
            ushort valueFormat1 = reader.ReadUInt16();
            ushort valueFormat2 = reader.ReadUInt16();
            ushort pairSetCount = reader.ReadUInt16();

            var pairSetOffsets = new ushort[pairSetCount];
            for (int i = 0; i < pairSetCount; i++)
            {
                pairSetOffsets[i] = reader.ReadUInt16();
            }

            long returnPos = reader.Position;
            reader.Seek(subtableStart + coverageOffset);
            var coverage = ParseCoverage(reader);
            reader.Seek(returnPos);

            for (int i = 0; i < pairSetCount && i < coverage.Count; i++)
            {
                if (pairSetOffsets[i] == 0)
                    continue;

                ushort firstGlyph = coverage[i];

                reader.Seek(subtableStart + pairSetOffsets[i]);
                ushort pairValueCount = reader.ReadUInt16();

                for (int j = 0; j < pairValueCount; j++)
                {
                    ushort secondGlyph = reader.ReadUInt16();
                    var value1 = ReadValueRecord(reader, valueFormat1);
                    var value2 = ReadValueRecord(reader, valueFormat2);

                    subtable.PairAdjustments[(firstGlyph, secondGlyph)] = (value1, value2);
                }
            }
        }
        else if (posFormat == 2)
        {
            // Format 2: Class pair adjustment (more memory efficient for large sets)
            // This format uses class definitions to group glyphs
            // For now, we'll skip this - it's less common than format 1
        }

        return subtable;
    }

    private static CursiveAttachmentSubtable ParseCursiveAttachment(BigEndianBinaryReader reader, long subtableStart)
    {
        var subtable = new CursiveAttachmentSubtable();

        ushort posFormat = reader.ReadUInt16();
        if (posFormat != 1)
            return subtable;

        ushort coverageOffset = reader.ReadUInt16();
        ushort entryExitCount = reader.ReadUInt16();

        var entryExitRecords = new (ushort entryOffset, ushort exitOffset)[entryExitCount];
        for (int i = 0; i < entryExitCount; i++)
        {
            entryExitRecords[i] = (reader.ReadUInt16(), reader.ReadUInt16());
        }

        long returnPos = reader.Position;
        reader.Seek(subtableStart + coverageOffset);
        var coverage = ParseCoverage(reader);
        reader.Seek(returnPos);

        for (int i = 0; i < entryExitCount && i < coverage.Count; i++)
        {
            ushort glyphId = coverage[i];
            var (entryOffset, exitOffset) = entryExitRecords[i];

            AnchorPoint? entry = null;
            AnchorPoint? exit = null;

            if (entryOffset > 0)
            {
                reader.Seek(subtableStart + entryOffset);
                entry = ReadAnchorPoint(reader);
            }

            if (exitOffset > 0)
            {
                reader.Seek(subtableStart + exitOffset);
                exit = ReadAnchorPoint(reader);
            }

            subtable.CursiveAnchors[glyphId] = (entry, exit);
        }

        return subtable;
    }

    private static MarkToBaseSubtable ParseMarkToBase(BigEndianBinaryReader reader, long subtableStart)
    {
        var subtable = new MarkToBaseSubtable();

        ushort posFormat = reader.ReadUInt16();
        if (posFormat != 1)
            return subtable;

        ushort markCoverageOffset = reader.ReadUInt16();
        ushort baseCoverageOffset = reader.ReadUInt16();
        ushort markClassCount = reader.ReadUInt16();
        ushort markArrayOffset = reader.ReadUInt16();
        ushort baseArrayOffset = reader.ReadUInt16();

        // Parse mark coverage and array
        reader.Seek(subtableStart + markCoverageOffset);
        var markCoverage = ParseCoverage(reader);

        reader.Seek(subtableStart + markArrayOffset);
        ParseMarkArray(reader, subtableStart + markArrayOffset, markCoverage, subtable.MarkGlyphs);

        // Parse base coverage and array
        reader.Seek(subtableStart + baseCoverageOffset);
        var baseCoverage = ParseCoverage(reader);

        reader.Seek(subtableStart + baseArrayOffset);
        ParseBaseArray(reader, subtableStart + baseArrayOffset, baseCoverage, markClassCount, subtable.BaseGlyphs);

        return subtable;
    }

    private static MarkToMarkSubtable ParseMarkToMark(BigEndianBinaryReader reader, long subtableStart)
    {
        var subtable = new MarkToMarkSubtable();

        ushort posFormat = reader.ReadUInt16();
        if (posFormat != 1)
            return subtable;

        ushort mark1CoverageOffset = reader.ReadUInt16();
        ushort mark2CoverageOffset = reader.ReadUInt16();
        ushort markClassCount = reader.ReadUInt16();
        ushort mark1ArrayOffset = reader.ReadUInt16();
        ushort mark2ArrayOffset = reader.ReadUInt16();

        // Parse mark1 coverage and array
        reader.Seek(subtableStart + mark1CoverageOffset);
        var mark1Coverage = ParseCoverage(reader);

        reader.Seek(subtableStart + mark1ArrayOffset);
        ParseMarkArray(reader, subtableStart + mark1ArrayOffset, mark1Coverage, subtable.Mark1Glyphs);

        // Parse mark2 coverage and array
        reader.Seek(subtableStart + mark2CoverageOffset);
        var mark2Coverage = ParseCoverage(reader);

        reader.Seek(subtableStart + mark2ArrayOffset);
        ParseMark2Array(reader, subtableStart + mark2ArrayOffset, mark2Coverage, markClassCount, subtable.Mark2Glyphs);

        return subtable;
    }

    private static void ParseMarkArray(
        BigEndianBinaryReader reader,
        long markArrayStart,
        List<ushort> coverage,
        Dictionary<ushort, (ushort markClass, AnchorPoint anchor)> output)
    {
        ushort markCount = reader.ReadUInt16();

        var records = new (ushort markClass, ushort anchorOffset)[markCount];
        for (int i = 0; i < markCount; i++)
        {
            records[i] = (reader.ReadUInt16(), reader.ReadUInt16());
        }

        for (int i = 0; i < markCount && i < coverage.Count; i++)
        {
            var (markClass, anchorOffset) = records[i];

            long returnPos = reader.Position;
            reader.Seek(markArrayStart + anchorOffset);
            var anchor = ReadAnchorPoint(reader);
            reader.Seek(returnPos);

            output[coverage[i]] = (markClass, anchor);
        }
    }

    private static void ParseBaseArray(
        BigEndianBinaryReader reader,
        long baseArrayStart,
        List<ushort> coverage,
        ushort markClassCount,
        Dictionary<ushort, AnchorPoint[]> output)
    {
        ushort baseCount = reader.ReadUInt16();

        for (int i = 0; i < baseCount && i < coverage.Count; i++)
        {
            var anchors = new AnchorPoint[markClassCount];

            for (int j = 0; j < markClassCount; j++)
            {
                ushort anchorOffset = reader.ReadUInt16();

                if (anchorOffset > 0)
                {
                    long returnPos = reader.Position;
                    reader.Seek(baseArrayStart + anchorOffset);
                    anchors[j] = ReadAnchorPoint(reader);
                    reader.Seek(returnPos);
                }
            }

            output[coverage[i]] = anchors;
        }
    }

    private static void ParseMark2Array(
        BigEndianBinaryReader reader,
        long mark2ArrayStart,
        List<ushort> coverage,
        ushort markClassCount,
        Dictionary<ushort, AnchorPoint[]> output)
    {
        ushort mark2Count = reader.ReadUInt16();

        for (int i = 0; i < mark2Count && i < coverage.Count; i++)
        {
            var anchors = new AnchorPoint[markClassCount];

            for (int j = 0; j < markClassCount; j++)
            {
                ushort anchorOffset = reader.ReadUInt16();

                if (anchorOffset > 0)
                {
                    long returnPos = reader.Position;
                    reader.Seek(mark2ArrayStart + anchorOffset);
                    anchors[j] = ReadAnchorPoint(reader);
                    reader.Seek(returnPos);
                }
            }

            output[coverage[i]] = anchors;
        }
    }

    private static AnchorPoint ReadAnchorPoint(BigEndianBinaryReader reader)
    {
        ushort anchorFormat = reader.ReadUInt16();

        var anchor = new AnchorPoint
        {
            X = reader.ReadInt16(),
            Y = reader.ReadInt16()
        };

        // Formats 2 and 3 have additional data we don't need for basic positioning
        return anchor;
    }

    private static ValueRecord ReadValueRecord(BigEndianBinaryReader reader, ushort valueFormat)
    {
        var record = new ValueRecord();

        if ((valueFormat & 0x0001) != 0) record.XPlacement = reader.ReadInt16();
        if ((valueFormat & 0x0002) != 0) record.YPlacement = reader.ReadInt16();
        if ((valueFormat & 0x0004) != 0) record.XAdvance = reader.ReadInt16();
        if ((valueFormat & 0x0008) != 0) record.YAdvance = reader.ReadInt16();

        // Skip device table offsets (formats 0x0010, 0x0020, 0x0040, 0x0080)
        // These are for fine-tuning at specific sizes - not needed for basic implementation
        if ((valueFormat & 0x0010) != 0) reader.Skip(2);
        if ((valueFormat & 0x0020) != 0) reader.Skip(2);
        if ((valueFormat & 0x0040) != 0) reader.Skip(2);
        if ((valueFormat & 0x0080) != 0) reader.Skip(2);

        return record;
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
