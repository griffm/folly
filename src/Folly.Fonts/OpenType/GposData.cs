using System.Collections.Generic;

namespace Folly.Fonts.OpenType;

/// <summary>
/// Represents the GPOS (Glyph Positioning) table data.
/// The GPOS table provides data for glyph positioning (e.g., kerning, mark positioning).
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/gpos
/// </summary>
public class GposData
{
    /// <summary>
    /// Scripts defined in this GPOS table.
    /// </summary>
    public List<OpenTypeScript> Scripts { get; set; } = new();

    /// <summary>
    /// Features defined in this GPOS table.
    /// </summary>
    public List<OpenTypeFeature> Features { get; set; } = new();

    /// <summary>
    /// Lookup tables for glyph positioning.
    /// </summary>
    public List<GposLookup> Lookups { get; set; } = new();
}

/// <summary>
/// Represents a single GPOS lookup table.
/// </summary>
public class GposLookup
{
    /// <summary>
    /// The type of positioning performed by this lookup.
    /// </summary>
    public GposLookupType LookupType { get; set; }

    /// <summary>
    /// Lookup flags (RTL, ignore marks, etc.).
    /// </summary>
    public ushort LookupFlag { get; set; }

    /// <summary>
    /// Subtables for this lookup.
    /// </summary>
    public List<IGposSubtable> Subtables { get; set; } = new();
}

/// <summary>
/// GPOS lookup types.
/// </summary>
public enum GposLookupType : ushort
{
    /// <summary>Adjust position of a single glyph</summary>
    SingleAdjustment = 1,
    /// <summary>Adjust position of a pair of glyphs (kerning)</summary>
    PairAdjustment = 2,
    /// <summary>Cursive attachment</summary>
    CursiveAttachment = 3,
    /// <summary>Mark-to-base attachment</summary>
    MarkToBase = 4,
    /// <summary>Mark-to-ligature attachment</summary>
    MarkToLigature = 5,
    /// <summary>Mark-to-mark attachment</summary>
    MarkToMark = 6,
    /// <summary>Apply positioning in context</summary>
    Context = 7,
    /// <summary>Apply positioning in chained context</summary>
    ChainedContext = 8,
    /// <summary>Extension for other lookup types</summary>
    Extension = 9
}

/// <summary>
/// Base interface for GPOS subtables.
/// </summary>
public interface IGposSubtable
{
    /// <summary>Gets the type of this GPOS lookup.</summary>
    GposLookupType LookupType { get; }
}

/// <summary>
/// Value record for glyph positioning adjustments.
/// Contains deltas for X and Y placement and advance.
/// </summary>
public struct ValueRecord
{
    /// <summary>
    /// Horizontal adjustment for placement, in font units.
    /// </summary>
    public short XPlacement { get; set; }

    /// <summary>
    /// Vertical adjustment for placement, in font units.
    /// </summary>
    public short YPlacement { get; set; }

    /// <summary>
    /// Horizontal adjustment for advance width, in font units.
    /// </summary>
    public short XAdvance { get; set; }

    /// <summary>
    /// Vertical adjustment for advance height, in font units.
    /// </summary>
    public short YAdvance { get; set; }

    /// <summary>
    /// Gets a value indicating whether this value record has any non-zero adjustments.
    /// </summary>
    public readonly bool HasAdjustment =>
        XPlacement != 0 || YPlacement != 0 || XAdvance != 0 || YAdvance != 0;

    /// <summary>
    /// Creates a zero value record.
    /// </summary>
    public static ValueRecord Zero => new()
    {
        XPlacement = 0,
        YPlacement = 0,
        XAdvance = 0,
        YAdvance = 0
    };
}

/// <summary>
/// Single adjustment subtable (Type 1).
/// Adjusts the position of a single glyph.
/// </summary>
public class SingleAdjustmentSubtable : IGposSubtable
{
    /// <inheritdoc />
    public GposLookupType LookupType => GposLookupType.SingleAdjustment;

    /// <summary>
    /// Maps glyph ID to positioning adjustment.
    /// </summary>
    public Dictionary<ushort, ValueRecord> Adjustments { get; set; } = new();
}

/// <summary>
/// Pair adjustment subtable (Type 2).
/// Adjusts the position of a pair of glyphs (used for kerning).
/// </summary>
public class PairAdjustmentSubtable : IGposSubtable
{
    /// <inheritdoc />
    public GposLookupType LookupType => GposLookupType.PairAdjustment;

    /// <summary>
    /// Maps a pair of glyph IDs to their positioning adjustments.
    /// Key: (firstGlyph, secondGlyph)
    /// Value: (adjustment for first glyph, adjustment for second glyph)
    /// </summary>
    public Dictionary<(ushort, ushort), (ValueRecord, ValueRecord)> PairAdjustments { get; set; } = new();
}

/// <summary>
/// Anchor point for mark attachment.
/// </summary>
public struct AnchorPoint
{
    /// <summary>
    /// X coordinate of the anchor point, in font units.
    /// </summary>
    public short X { get; set; }

    /// <summary>
    /// Y coordinate of the anchor point, in font units.
    /// </summary>
    public short Y { get; set; }
}

/// <summary>
/// Mark-to-base attachment subtable (Type 4).
/// Attaches mark glyphs (accents, diacritics) to base glyphs.
/// </summary>
public class MarkToBaseSubtable : IGposSubtable
{
    /// <inheritdoc />
    public GposLookupType LookupType => GposLookupType.MarkToBase;

    /// <summary>
    /// Maps mark glyph ID to (mark class, anchor point).
    /// </summary>
    public Dictionary<ushort, (ushort markClass, AnchorPoint anchor)> MarkGlyphs { get; set; } = new();

    /// <summary>
    /// Maps base glyph ID to anchor points for each mark class.
    /// Key: base glyph ID
    /// Value: array of anchor points, indexed by mark class
    /// </summary>
    public Dictionary<ushort, AnchorPoint[]> BaseGlyphs { get; set; } = new();
}

/// <summary>
/// Mark-to-mark attachment subtable (Type 6).
/// Attaches mark glyphs to other mark glyphs (e.g., stacking diacritics).
/// </summary>
public class MarkToMarkSubtable : IGposSubtable
{
    /// <inheritdoc />
    public GposLookupType LookupType => GposLookupType.MarkToMark;

    /// <summary>
    /// Maps mark1 glyph ID to (mark class, anchor point).
    /// Mark1 is the mark being attached.
    /// </summary>
    public Dictionary<ushort, (ushort markClass, AnchorPoint anchor)> Mark1Glyphs { get; set; } = new();

    /// <summary>
    /// Maps mark2 glyph ID to anchor points for each mark class.
    /// Mark2 is the mark being attached to.
    /// Key: mark2 glyph ID
    /// Value: array of anchor points, indexed by mark class
    /// </summary>
    public Dictionary<ushort, AnchorPoint[]> Mark2Glyphs { get; set; } = new();
}

/// <summary>
/// Cursive attachment subtable (Type 3).
/// Connects glyphs in cursive scripts.
/// </summary>
public class CursiveAttachmentSubtable : IGposSubtable
{
    /// <inheritdoc />
    public GposLookupType LookupType => GposLookupType.CursiveAttachment;

    /// <summary>
    /// Maps glyph ID to (entry anchor, exit anchor).
    /// Entry anchor is where the previous glyph connects.
    /// Exit anchor is where the next glyph connects.
    /// </summary>
    public Dictionary<ushort, (AnchorPoint? entry, AnchorPoint? exit)> CursiveAnchors { get; set; } = new();
}
