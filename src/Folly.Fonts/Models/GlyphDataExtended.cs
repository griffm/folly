namespace Folly.Fonts.Models;

/// <summary>
/// Represents a TrueType simple glyph outline (contours with on-curve and off-curve points).
/// </summary>
public class SimpleGlyphOutline
{
    /// <summary>
    /// Array of endpoint indices for each contour.
    /// For example, if EndPoints = [5, 12], the glyph has 2 contours:
    /// - Contour 0: points 0-5 (6 points)
    /// - Contour 1: points 6-12 (7 points)
    /// </summary>
    public ushort[] EndPoints { get; set; } = Array.Empty<ushort>();

    /// <summary>
    /// TrueType instruction bytecode for glyph hinting.
    /// Can be empty if no hinting is present or if we strip hints.
    /// </summary>
    public byte[] Instructions { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Flags for each point in the glyph.
    /// Bit 0: On-curve point (1) vs off-curve control point (0)
    /// Bit 1: X-coordinate is 1 byte (vs 2 bytes)
    /// Bit 2: Y-coordinate is 1 byte (vs 2 bytes)
    /// Bit 3: Repeat flag (next byte indicates repeat count)
    /// Bit 4: X-coordinate is positive (if 1-byte) or same as previous (if 2-byte omitted)
    /// Bit 5: Y-coordinate is positive (if 1-byte) or same as previous (if 2-byte omitted)
    /// </summary>
    public byte[] Flags { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// X coordinates for each point (absolute values).
    /// </summary>
    public short[] XCoordinates { get; set; } = Array.Empty<short>();

    /// <summary>
    /// Y coordinates for each point (absolute values).
    /// </summary>
    public short[] YCoordinates { get; set; } = Array.Empty<short>();

    /// <summary>
    /// Total number of points in the glyph.
    /// </summary>
    public int PointCount => Flags.Length;

    /// <summary>
    /// Number of contours in the glyph.
    /// </summary>
    public int ContourCount => EndPoints.Length;
}

/// <summary>
/// Represents a composite glyph component (reference to another glyph with transformation).
/// </summary>
public class CompositeGlyphComponent
{
    /// <summary>
    /// Flags controlling component behavior.
    /// Bit 0 (ARG_1_AND_2_ARE_WORDS): Arguments are 16-bit vs 8-bit
    /// Bit 1 (ARGS_ARE_XY_VALUES): Arguments are x/y offsets vs point numbers
    /// Bit 3 (WE_HAVE_A_SCALE): Simple scale factor
    /// Bit 6 (WE_HAVE_AN_X_AND_Y_SCALE): Separate x and y scale
    /// Bit 7 (WE_HAVE_A_TWO_BY_TWO): 2x2 transformation matrix
    /// Bit 9 (USE_MY_METRICS): Use metrics from this component
    /// Bit 10 (OVERLAP_COMPOUND): Components overlap
    /// </summary>
    public ushort Flags { get; set; }

    /// <summary>
    /// Glyph index of the component glyph.
    /// </summary>
    public ushort GlyphIndex { get; set; }

    /// <summary>
    /// First argument (x offset or point number).
    /// </summary>
    public short Argument1 { get; set; }

    /// <summary>
    /// Second argument (y offset or point number).
    /// </summary>
    public short Argument2 { get; set; }

    /// <summary>
    /// Transformation matrix or scale values.
    /// Format depends on flags (can be 1, 2, or 4 F2Dot14 values).
    /// </summary>
    public float[] TransformValues { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Whether there are more components after this one.
    /// </summary>
    public bool HasMoreComponents => (Flags & 0x0020) != 0; // MORE_COMPONENTS flag
}

/// <summary>
/// Represents a composite glyph outline (made up of other glyphs).
/// </summary>
public class CompositeGlyphOutline
{
    /// <summary>
    /// Array of component glyphs that make up this composite glyph.
    /// </summary>
    public List<CompositeGlyphComponent> Components { get; set; } = new();

    /// <summary>
    /// Optional instructions for the composite glyph (applied after component assembly).
    /// </summary>
    public byte[] Instructions { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Represents glyph outline data from the 'glyf' table (TrueType fonts).
/// Extended with full outline data for rendering and serialization.
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
    /// Simple glyph outline data (contours, points, flags, coordinates).
    /// Null for composite or empty glyphs.
    /// </summary>
    public SimpleGlyphOutline? SimpleOutline { get; set; }

    /// <summary>
    /// Composite glyph outline data (component references and transformations).
    /// Null for simple or empty glyphs.
    /// </summary>
    public CompositeGlyphOutline? CompositeOutline { get; set; }

    /// <summary>
    /// Raw glyph data bytes from the original font.
    /// Used for efficient copying during font subsetting.
    /// </summary>
    public byte[]? RawGlyphData { get; set; }

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

    /// <summary>
    /// Calculates the serialized size of this glyph in bytes.
    /// Used for loca table offset calculations.
    /// </summary>
    public int CalculateSerializedSize()
    {
        if (IsEmptyGlyph)
            return 0;

        // If we have raw glyph data, use its length
        if (RawGlyphData != null)
            return RawGlyphData.Length;

        // Otherwise calculate from outline data
        int size = 10; // Header: numberOfContours (2) + xMin (2) + yMin (2) + xMax (2) + yMax (2)

        if (SimpleOutline != null)
        {
            // EndPoints array: 2 bytes per contour
            size += SimpleOutline.EndPoints.Length * 2;

            // Instruction length + instructions
            size += 2 + SimpleOutline.Instructions.Length;

            // Flags (compressed with repeat counts)
            size += SimpleOutline.Flags.Length; // Approximate (can be smaller with repeats)

            // X coordinates (can be 1 or 2 bytes each)
            size += SimpleOutline.XCoordinates.Length * 2; // Worst case

            // Y coordinates (can be 1 or 2 bytes each)
            size += SimpleOutline.YCoordinates.Length * 2; // Worst case
        }
        else if (CompositeOutline != null)
        {
            // Component data (variable size per component)
            foreach (var component in CompositeOutline.Components)
            {
                size += 4; // Flags (2) + glyphIndex (2)
                size += 4; // Arguments (worst case: 2 shorts)
                size += component.TransformValues.Length * 2; // F2Dot14 values
            }

            // Instructions (if present)
            if (CompositeOutline.Instructions.Length > 0)
            {
                size += 2 + CompositeOutline.Instructions.Length;
            }
        }

        // Word-align
        if (size % 2 != 0)
            size++;

        return size;
    }
}
