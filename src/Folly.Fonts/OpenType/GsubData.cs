using System.Collections.Generic;

namespace Folly.Fonts.OpenType;

/// <summary>
/// Represents the GSUB (Glyph Substitution) table data.
/// The GSUB table provides data for glyph substitution (e.g., ligatures, contextual alternates).
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/gsub
/// </summary>
public class GsubData
{
    /// <summary>
    /// Scripts defined in this GSUB table.
    /// </summary>
    public List<OpenTypeScript> Scripts { get; set; } = new();

    /// <summary>
    /// Features defined in this GSUB table.
    /// </summary>
    public List<OpenTypeFeature> Features { get; set; } = new();

    /// <summary>
    /// Lookup tables for glyph substitution.
    /// </summary>
    public List<GsubLookup> Lookups { get; set; } = new();
}

/// <summary>
/// Represents a single GSUB lookup table.
/// </summary>
public class GsubLookup
{
    /// <summary>
    /// The type of substitution performed by this lookup.
    /// </summary>
    public GsubLookupType LookupType { get; set; }

    /// <summary>
    /// Lookup flags (RTL, ignore marks, etc.).
    /// </summary>
    public ushort LookupFlag { get; set; }

    /// <summary>
    /// Subtables for this lookup.
    /// </summary>
    public List<IGsubSubtable> Subtables { get; set; } = new();
}

/// <summary>
/// GSUB lookup types.
/// </summary>
public enum GsubLookupType : ushort
{
    /// <summary>Replace one glyph with one glyph</summary>
    Single = 1,
    /// <summary>Replace one glyph with multiple glyphs</summary>
    Multiple = 2,
    /// <summary>Replace one glyph with one of several alternatives</summary>
    Alternate = 3,
    /// <summary>Replace multiple glyphs with one glyph</summary>
    Ligature = 4,
    /// <summary>Apply substitutions in context</summary>
    Context = 5,
    /// <summary>Apply substitutions in chaining context</summary>
    ChainingContext = 6,
    /// <summary>Extension for other lookup types</summary>
    ExtensionSubstitution = 7,
    /// <summary>Reverse chaining contextual substitution</summary>
    ReverseChainingContext = 8
}

/// <summary>
/// Base interface for GSUB subtables.
/// </summary>
public interface IGsubSubtable
{
    /// <summary>Gets the type of this GSUB lookup.</summary>
    GsubLookupType LookupType { get; }
}

/// <summary>
/// Single substitution subtable (Type 1).
/// Replaces a single glyph with another single glyph.
/// </summary>
public class SingleSubstitutionSubtable : IGsubSubtable
{
    /// <inheritdoc />
    public GsubLookupType LookupType => GsubLookupType.Single;

    /// <summary>
    /// Maps input glyph ID to output glyph ID.
    /// </summary>
    public Dictionary<ushort, ushort> Substitutions { get; set; } = new();
}

/// <summary>
/// Ligature substitution subtable (Type 4).
/// Replaces multiple glyphs with a single ligature glyph.
/// </summary>
public class LigatureSubstitutionSubtable : IGsubSubtable
{
    /// <inheritdoc />
    public GsubLookupType LookupType => GsubLookupType.Ligature;

    /// <summary>
    /// Maps a starting glyph to a list of possible ligatures.
    /// Key: First glyph in the sequence.
    /// Value: List of ligatures that start with this glyph.
    /// </summary>
    public Dictionary<ushort, List<Ligature>> Ligatures { get; set; } = new();
}

/// <summary>
/// Represents a single ligature substitution.
/// </summary>
public class Ligature
{
    /// <summary>
    /// The glyph ID of the ligature to substitute.
    /// </summary>
    public ushort LigatureGlyph { get; set; }

    /// <summary>
    /// The component glyph IDs that form this ligature.
    /// The first glyph is the key in the parent dictionary, so this array contains the remaining glyphs.
    /// For example, for "fi" ligature, if 'f' is the key, this array contains the glyph ID for 'i'.
    /// </summary>
    public ushort[] ComponentGlyphIds { get; set; } = System.Array.Empty<ushort>();

    /// <summary>
    /// Gets the total number of components in this ligature (including the first glyph).
    /// </summary>
    public int ComponentCount => ComponentGlyphIds.Length + 1;
}

/// <summary>
/// Alternate substitution subtable (Type 3).
/// Replaces one glyph with one of several alternates.
/// </summary>
public class AlternateSubstitutionSubtable : IGsubSubtable
{
    /// <inheritdoc />
    public GsubLookupType LookupType => GsubLookupType.Alternate;

    /// <summary>
    /// Maps input glyph ID to a list of alternate glyph IDs.
    /// </summary>
    public Dictionary<ushort, List<ushort>> Alternates { get; set; } = new();
}

/// <summary>
/// Multiple substitution subtable (Type 2).
/// Replaces one glyph with multiple glyphs.
/// </summary>
public class MultipleSubstitutionSubtable : IGsubSubtable
{
    /// <inheritdoc />
    public GsubLookupType LookupType => GsubLookupType.Multiple;

    /// <summary>
    /// Maps input glyph ID to an array of output glyph IDs.
    /// </summary>
    public Dictionary<ushort, ushort[]> Substitutions { get; set; } = new();
}
