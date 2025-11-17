using System;
using System.Collections.Generic;
using System.Linq;
using Folly.Fonts.Models;

namespace Folly.Fonts.OpenType;

/// <summary>
/// Applies OpenType layout features to shape text with ligatures, kerning, and mark positioning.
/// This is a text shaping engine that implements the OpenType layout algorithm.
/// </summary>
public class OpenTypeShaper
{
    private readonly FontFile _font;
    private readonly HashSet<string> _enabledFeatures;

    /// <summary>
    /// Creates a new OpenType shaper for the specified font.
    /// </summary>
    /// <param name="font">Font file with GSUB and/or GPOS data.</param>
    /// <param name="enabledFeatures">
    /// Set of feature tags to enable (e.g., "liga", "kern").
    /// If null, defaults to standard features: liga, clig, kern, mark, mkmk.
    /// </param>
    public OpenTypeShaper(FontFile font, HashSet<string>? enabledFeatures = null)
    {
        _font = font ?? throw new ArgumentNullException(nameof(font));

        // Default enabled features for quality typography
        _enabledFeatures = enabledFeatures ?? new HashSet<string>
        {
            OpenTypeFeature.CommonFeatures.StandardLigatures,
            OpenTypeFeature.CommonFeatures.ContextualLigatures,
            OpenTypeFeature.CommonFeatures.Kerning,
            OpenTypeFeature.CommonFeatures.MarkPositioning,
            OpenTypeFeature.CommonFeatures.MarkToMark
        };
    }

    /// <summary>
    /// Shapes a string of text, applying OpenType features.
    /// </summary>
    /// <param name="text">Input text to shape.</param>
    /// <param name="script">Script tag (e.g., "latn" for Latin). Defaults to "latn".</param>
    /// <param name="language">Language tag (e.g., "dflt", "ENG "). Defaults to "dflt".</param>
    /// <returns>Shaped glyph run with positioned glyphs.</returns>
    public GlyphRun Shape(string text, string script = "latn", string language = "dflt")
    {
        if (string.IsNullOrEmpty(text))
            return new GlyphRun();

        // Step 1: Convert characters to glyph IDs
        var glyphs = new List<ShapedGlyph>();
        foreach (char c in text)
        {
            if (_font.CharacterToGlyphIndex.TryGetValue(c, out uint glyphId))
            {
                glyphs.Add(new ShapedGlyph
                {
                    GlyphId = glyphId,
                    Character = c,
                    XAdvance = (uint)_font.GlyphAdvanceWidths.Length > glyphId
                        ? (short)_font.GlyphAdvanceWidths[glyphId]
                        : (short)0
                });
            }
        }

        // Step 2: Apply GSUB (glyph substitution) features
        if (_font.Gsub != null)
        {
            glyphs = ApplyGsubFeatures(glyphs, script, language);
        }

        // Step 3: Apply GPOS (glyph positioning) features
        if (_font.Gpos != null)
        {
            ApplyGposFeatures(glyphs, script, language);
        }

        // Step 4: Apply legacy kern table if GPOS kerning not available
        if (_font.Gpos == null && _font.KerningPairs.Count > 0)
        {
            ApplyLegacyKerning(glyphs);
        }

        return new GlyphRun { Glyphs = glyphs };
    }

    /// <summary>
    /// Applies GSUB (glyph substitution) features like ligatures.
    /// </summary>
    private List<ShapedGlyph> ApplyGsubFeatures(List<ShapedGlyph> glyphs, string script, string language)
    {
        var lookupIndices = GetLookupIndicesForScript(_font.Gsub!, script, language);

        foreach (var lookupIndex in lookupIndices)
        {
            if (lookupIndex >= _font.Gsub!.Lookups.Count)
                continue;

            var lookup = _font.Gsub.Lookups[lookupIndex];

            // Apply each subtable in the lookup
            foreach (var subtable in lookup.Subtables)
            {
                glyphs = ApplyGsubSubtable(glyphs, subtable);
            }
        }

        return glyphs;
    }

    /// <summary>
    /// Applies a single GSUB subtable to the glyph list.
    /// </summary>
    private List<ShapedGlyph> ApplyGsubSubtable(List<ShapedGlyph> glyphs, IGsubSubtable subtable)
    {
        return subtable switch
        {
            LigatureSubstitutionSubtable ligSub => ApplyLigatureSubstitution(glyphs, ligSub),
            SingleSubstitutionSubtable singleSub => ApplySingleSubstitution(glyphs, singleSub),
            AlternateSubstitutionSubtable altSub => ApplyAlternateSubstitution(glyphs, altSub),
            MultipleSubstitutionSubtable multSub => ApplyMultipleSubstitution(glyphs, multSub),
            _ => glyphs
        };
    }

    /// <summary>
    /// Applies ligature substitution (e.g., f + i → fi).
    /// </summary>
    private List<ShapedGlyph> ApplyLigatureSubstitution(List<ShapedGlyph> glyphs, LigatureSubstitutionSubtable subtable)
    {
        var result = new List<ShapedGlyph>();
        int i = 0;

        while (i < glyphs.Count)
        {
            bool ligatureApplied = false;

            // Check if current glyph can start a ligature
            // Note: GSUB tables use 16-bit glyph indices, glyphs > 65535 are skipped
            if (glyphs[i].GlyphId <= 0xFFFF && subtable.Ligatures.TryGetValue((ushort)glyphs[i].GlyphId, out var ligatures))
            {
                // Try each ligature, longest first
                foreach (var ligature in ligatures.OrderByDescending(l => l.ComponentCount))
                {
                    // Check if we have enough glyphs remaining
                    if (i + ligature.ComponentCount > glyphs.Count)
                        continue;

                    // Check if the sequence matches
                    bool matches = true;
                    for (int j = 0; j < ligature.ComponentGlyphIds.Length; j++)
                    {
                        if (glyphs[i + j + 1].GlyphId != ligature.ComponentGlyphIds[j])
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (matches)
                    {
                        // Apply ligature substitution
                        var ligGlyph = new ShapedGlyph
                        {
                            GlyphId = ligature.LigatureGlyph,
                            Character = glyphs[i].Character, // Keep first character
                            XAdvance = _font.GlyphAdvanceWidths.Length > ligature.LigatureGlyph
                                ? (short)_font.GlyphAdvanceWidths[ligature.LigatureGlyph]
                                : (short)0
                        };

                        result.Add(ligGlyph);
                        i += ligature.ComponentCount; // Skip the component glyphs
                        ligatureApplied = true;
                        break;
                    }
                }
            }

            if (!ligatureApplied)
            {
                result.Add(glyphs[i]);
                i++;
            }
        }

        return result;
    }

    /// <summary>
    /// Applies single substitution (e.g., small caps).
    /// </summary>
    private List<ShapedGlyph> ApplySingleSubstitution(List<ShapedGlyph> glyphs, SingleSubstitutionSubtable subtable)
    {
        for (int i = 0; i < glyphs.Count; i++)
        {
            if (glyphs[i].GlyphId <= 0xFFFF && subtable.Substitutions.TryGetValue((ushort)glyphs[i].GlyphId, out ushort newGlyphId))
            {
                glyphs[i] = new ShapedGlyph
                {
                    GlyphId = newGlyphId,
                    Character = glyphs[i].Character,
                    XAdvance = _font.GlyphAdvanceWidths.Length > newGlyphId
                        ? (short)_font.GlyphAdvanceWidths[newGlyphId]
                        : (short)0,
                    XPlacement = glyphs[i].XPlacement,
                    YPlacement = glyphs[i].YPlacement,
                    YAdvance = glyphs[i].YAdvance
                };
            }
        }

        return glyphs;
    }

    /// <summary>
    /// Applies alternate substitution (uses first alternate).
    /// </summary>
    private List<ShapedGlyph> ApplyAlternateSubstitution(List<ShapedGlyph> glyphs, AlternateSubstitutionSubtable subtable)
    {
        for (int i = 0; i < glyphs.Count; i++)
        {
            if (glyphs[i].GlyphId <= 0xFFFF && subtable.Alternates.TryGetValue((ushort)glyphs[i].GlyphId, out var alternates) && alternates.Count > 0)
            {
                // Use the first alternate (in a full implementation, this could be user-selectable)
                ushort newGlyphId = alternates[0];
                glyphs[i] = new ShapedGlyph
                {
                    GlyphId = newGlyphId,
                    Character = glyphs[i].Character,
                    XAdvance = _font.GlyphAdvanceWidths.Length > newGlyphId
                        ? (short)_font.GlyphAdvanceWidths[newGlyphId]
                        : (short)0,
                    XPlacement = glyphs[i].XPlacement,
                    YPlacement = glyphs[i].YPlacement,
                    YAdvance = glyphs[i].YAdvance
                };
            }
        }

        return glyphs;
    }

    /// <summary>
    /// Applies multiple substitution (one glyph → multiple glyphs).
    /// </summary>
    private List<ShapedGlyph> ApplyMultipleSubstitution(List<ShapedGlyph> glyphs, MultipleSubstitutionSubtable subtable)
    {
        var result = new List<ShapedGlyph>();

        foreach (var glyph in glyphs)
        {
            if (glyph.GlyphId <= 0xFFFF && subtable.Substitutions.TryGetValue((ushort)glyph.GlyphId, out var substitutes))
            {
                foreach (var subGlyphId in substitutes)
                {
                    result.Add(new ShapedGlyph
                    {
                        GlyphId = subGlyphId,
                        Character = glyph.Character,
                        XAdvance = _font.GlyphAdvanceWidths.Length > subGlyphId
                            ? (short)_font.GlyphAdvanceWidths[subGlyphId]
                            : (short)0
                    });
                }
            }
            else
            {
                result.Add(glyph);
            }
        }

        return result;
    }

    /// <summary>
    /// Applies GPOS (glyph positioning) features like kerning and mark positioning.
    /// </summary>
    private void ApplyGposFeatures(List<ShapedGlyph> glyphs, string script, string language)
    {
        var lookupIndices = GetLookupIndicesForScript(_font.Gpos!, script, language);

        foreach (var lookupIndex in lookupIndices)
        {
            if (lookupIndex >= _font.Gpos!.Lookups.Count)
                continue;

            var lookup = _font.Gpos.Lookups[lookupIndex];

            // Apply each subtable in the lookup
            foreach (var subtable in lookup.Subtables)
            {
                ApplyGposSubtable(glyphs, subtable);
            }
        }
    }

    /// <summary>
    /// Applies a single GPOS subtable to the glyph list.
    /// </summary>
    private void ApplyGposSubtable(List<ShapedGlyph> glyphs, IGposSubtable subtable)
    {
        switch (subtable)
        {
            case PairAdjustmentSubtable pairAdj:
                ApplyPairAdjustment(glyphs, pairAdj);
                break;
            case SingleAdjustmentSubtable singleAdj:
                ApplySingleAdjustment(glyphs, singleAdj);
                break;
                // Mark-to-base and mark-to-mark positioning can be added here
        }
    }

    /// <summary>
    /// Applies pair adjustment (kerning) from GPOS.
    /// </summary>
    private void ApplyPairAdjustment(List<ShapedGlyph> glyphs, PairAdjustmentSubtable subtable)
    {
        for (int i = 0; i < glyphs.Count - 1; i++)
        {
            // GPOS tables use 16-bit glyph indices
            if (glyphs[i].GlyphId > 0xFFFF || glyphs[i + 1].GlyphId > 0xFFFF)
                continue;

            var key = ((ushort)glyphs[i].GlyphId, (ushort)glyphs[i + 1].GlyphId);

            if (subtable.PairAdjustments.TryGetValue(key, out var adjustment))
            {
                var (value1, value2) = adjustment;

                // Apply adjustments to the glyphs
                glyphs[i] = glyphs[i] with
                {
                    XPlacement = (short)(glyphs[i].XPlacement + value1.XPlacement),
                    YPlacement = (short)(glyphs[i].YPlacement + value1.YPlacement),
                    XAdvance = (short)(glyphs[i].XAdvance + value1.XAdvance),
                    YAdvance = (short)(glyphs[i].YAdvance + value1.YAdvance)
                };

                glyphs[i + 1] = glyphs[i + 1] with
                {
                    XPlacement = (short)(glyphs[i + 1].XPlacement + value2.XPlacement),
                    YPlacement = (short)(glyphs[i + 1].YPlacement + value2.YPlacement),
                    XAdvance = (short)(glyphs[i + 1].XAdvance + value2.XAdvance),
                    YAdvance = (short)(glyphs[i + 1].YAdvance + value2.YAdvance)
                };
            }
        }
    }

    /// <summary>
    /// Applies single adjustment to individual glyphs.
    /// </summary>
    private void ApplySingleAdjustment(List<ShapedGlyph> glyphs, SingleAdjustmentSubtable subtable)
    {
        for (int i = 0; i < glyphs.Count; i++)
        {
            if (glyphs[i].GlyphId <= 0xFFFF && subtable.Adjustments.TryGetValue((ushort)glyphs[i].GlyphId, out var value))
            {
                glyphs[i] = glyphs[i] with
                {
                    XPlacement = (short)(glyphs[i].XPlacement + value.XPlacement),
                    YPlacement = (short)(glyphs[i].YPlacement + value.YPlacement),
                    XAdvance = (short)(glyphs[i].XAdvance + value.XAdvance),
                    YAdvance = (short)(glyphs[i].YAdvance + value.YAdvance)
                };
            }
        }
    }

    /// <summary>
    /// Applies legacy 'kern' table kerning (fallback when GPOS is not available).
    /// </summary>
    private void ApplyLegacyKerning(List<ShapedGlyph> glyphs)
    {
        for (int i = 0; i < glyphs.Count - 1; i++)
        {
            // Kern table uses 16-bit glyph indices
            if (glyphs[i].GlyphId > 0xFFFF || glyphs[i + 1].GlyphId > 0xFFFF)
                continue;

            var key = ((ushort)glyphs[i].GlyphId, (ushort)glyphs[i + 1].GlyphId);

            if (_font.KerningPairs.TryGetValue(key, out short kerning))
            {
                glyphs[i] = glyphs[i] with
                {
                    XAdvance = (short)(glyphs[i].XAdvance + kerning)
                };
            }
        }
    }

    /// <summary>
    /// Gets the list of lookup indices for the specified script and language.
    /// </summary>
    private List<ushort> GetLookupIndicesForScript(GsubData gsub, string script, string language)
    {
        var lookupIndices = new List<ushort>();

        // Find the script
        var scriptData = gsub.Scripts.FirstOrDefault(s => s.Tag == script)
            ?? gsub.Scripts.FirstOrDefault(s => s.Tag == "DFLT");

        if (scriptData == null)
            return lookupIndices;

        // Find the language system
        var langSys = scriptData.LanguageSystems.FirstOrDefault(ls => ls.Tag == language)
            ?? scriptData.LanguageSystems.FirstOrDefault(ls => ls.Tag == "dflt");

        if (langSys == null)
            return lookupIndices;

        // Collect lookup indices for enabled features
        foreach (var featureIndex in langSys.FeatureIndices)
        {
            if (featureIndex >= gsub.Features.Count)
                continue;

            var feature = gsub.Features[featureIndex];

            // Check if this feature is enabled
            if (_enabledFeatures.Contains(feature.Tag))
            {
                lookupIndices.AddRange(feature.LookupIndices);
            }
        }

        return lookupIndices;
    }

    /// <summary>
    /// Gets the list of lookup indices for the specified script and language (GPOS version).
    /// </summary>
    private List<ushort> GetLookupIndicesForScript(GposData gpos, string script, string language)
    {
        var lookupIndices = new List<ushort>();

        // Find the script
        var scriptData = gpos.Scripts.FirstOrDefault(s => s.Tag == script)
            ?? gpos.Scripts.FirstOrDefault(s => s.Tag == "DFLT");

        if (scriptData == null)
            return lookupIndices;

        // Find the language system
        var langSys = scriptData.LanguageSystems.FirstOrDefault(ls => ls.Tag == language)
            ?? scriptData.LanguageSystems.FirstOrDefault(ls => ls.Tag == "dflt");

        if (langSys == null)
            return lookupIndices;

        // Collect lookup indices for enabled features
        foreach (var featureIndex in langSys.FeatureIndices)
        {
            if (featureIndex >= gpos.Features.Count)
                continue;

            var feature = gpos.Features[featureIndex];

            // Check if this feature is enabled
            if (_enabledFeatures.Contains(feature.Tag))
            {
                lookupIndices.AddRange(feature.LookupIndices);
            }
        }

        return lookupIndices;
    }
}

/// <summary>
/// Represents a shaped glyph with positioning information.
/// </summary>
public record struct ShapedGlyph
{
    /// <summary>
    /// Glyph ID in the font. Supports glyph indices beyond 65535.
    /// </summary>
    public uint GlyphId { get; init; }

    /// <summary>
    /// Original character (may be null for ligatures or substituted glyphs).
    /// </summary>
    public char Character { get; init; }

    /// <summary>
    /// Horizontal advance width in font units.
    /// </summary>
    public short XAdvance { get; init; }

    /// <summary>
    /// Vertical advance (usually 0 for horizontal text).
    /// </summary>
    public short YAdvance { get; init; }

    /// <summary>
    /// Horizontal placement offset in font units.
    /// </summary>
    public short XPlacement { get; init; }

    /// <summary>
    /// Vertical placement offset in font units.
    /// </summary>
    public short YPlacement { get; init; }
}

/// <summary>
/// Represents the result of text shaping.
/// </summary>
public class GlyphRun
{
    /// <summary>
    /// Shaped glyphs with positioning information.
    /// </summary>
    public List<ShapedGlyph> Glyphs { get; set; } = new();

    /// <summary>
    /// Gets the total advance width of the glyph run in font units.
    /// </summary>
    public int TotalAdvanceWidth => Glyphs.Sum(g => g.XAdvance);
}
