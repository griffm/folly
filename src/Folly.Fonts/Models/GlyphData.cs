namespace Folly.Fonts.Models;

/// <summary>
/// Represents glyph outline data from the 'glyf' table (TrueType fonts).
/// </summary>
public class GlyphData
{
    /// <summary>
    /// Number of contours in the glyph.
    /// Positive values indicate simple glyphs, -1 indicates composite glyphs, 0 indicates empty glyphs.
    /// </summary>
    public short NumberOfContours { get; set; }

    /// <summary>
    /// Minimum X coordinate for the glyph bounding box.
    /// </summary>
    public short XMin { get; set; }

    /// <summary>
    /// Minimum Y coordinate for the glyph bounding box.
    /// </summary>
    public short YMin { get; set; }

    /// <summary>
    /// Maximum X coordinate for the glyph bounding box.
    /// </summary>
    public short XMax { get; set; }

    /// <summary>
    /// Maximum Y coordinate for the glyph bounding box.
    /// </summary>
    public short YMax { get; set; }

    /// <summary>
    /// Whether this is a simple glyph (true) or composite glyph (false).
    /// Empty glyphs (like space) have NumberOfContours = 0.
    /// </summary>
    public bool IsSimpleGlyph => NumberOfContours >= 0;

    /// <summary>
    /// Whether this is a composite glyph made up of other glyphs.
    /// </summary>
    public bool IsCompositeGlyph => NumberOfContours < 0;

    /// <summary>
    /// Whether this is an empty glyph (no outline data).
    /// </summary>
    public bool IsEmptyGlyph => NumberOfContours == 0 && XMin == 0 && YMin == 0 && XMax == 0 && YMax == 0;

    /// <summary>
    /// Width of the glyph bounding box.
    /// </summary>
    public int Width => XMax - XMin;

    /// <summary>
    /// Height of the glyph bounding box.
    /// </summary>
    public int Height => YMax - YMin;
}
