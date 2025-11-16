using System.Collections.Generic;

namespace Folly.Fonts.CFF;

/// <summary>
/// Represents a parsed CFF (Compact Font Format) table.
/// CFF is used in OpenType fonts with PostScript outlines (.otf files).
/// Spec: https://adobe-type-tools.github.io/font-tech-notes/pdfs/5176.CFF.pdf
/// </summary>
public class CffData
{
    /// <summary>
    /// Font name from the Name INDEX.
    /// </summary>
    public string FontName { get; set; } = string.Empty;

    /// <summary>
    /// Top DICT data containing font-level information.
    /// </summary>
    public CffTopDict TopDict { get; set; } = new();

    /// <summary>
    /// Character strings (glyph outlines) as raw CharString data.
    /// Key: glyph index, Value: CharString bytecode
    /// </summary>
    public Dictionary<int, byte[]> CharStrings { get; set; } = new();

    /// <summary>
    /// Global subroutines used by CharStrings.
    /// </summary>
    public List<byte[]> GlobalSubrs { get; set; } = new();

    /// <summary>
    /// Local subroutines used by CharStrings (for CIDFonts).
    /// </summary>
    public List<byte[]> LocalSubrs { get; set; } = new();

    /// <summary>
    /// Character set (maps glyph IDs to character codes).
    /// </summary>
    public List<int> Charset { get; set; } = new();

    /// <summary>
    /// Encoding (maps character codes to glyph IDs).
    /// </summary>
    public Dictionary<int, int> Encoding { get; set; } = new();

    /// <summary>
    /// Whether this is a CID font (multi-byte font for CJK).
    /// </summary>
    public bool IsCIDFont { get; set; }

    /// <summary>
    /// Raw CFF table data (useful for subsetting).
    /// </summary>
    public byte[] RawData { get; set; } = System.Array.Empty<byte>();
}

/// <summary>
/// Top DICT contains font-level information.
/// </summary>
public class CffTopDict
{
    /// <summary>
    /// Font bounding box [xMin, yMin, xMax, yMax].
    /// </summary>
    public double[] FontBBox { get; set; } = new double[4];

    /// <summary>
    /// CharStrings INDEX offset.
    /// </summary>
    public int CharStringsOffset { get; set; }

    /// <summary>
    /// Charset offset.
    /// </summary>
    public int CharsetOffset { get; set; }

    /// <summary>
    /// Encoding offset.
    /// </summary>
    public int EncodingOffset { get; set; }

    /// <summary>
    /// Private DICT size and offset.
    /// </summary>
    public (int size, int offset) Private { get; set; }

    /// <summary>
    /// Font matrix (default: [0.001, 0, 0, 0.001, 0, 0]).
    /// </summary>
    public double[] FontMatrix { get; set; } = { 0.001, 0, 0, 0.001, 0, 0 };

    /// <summary>
    /// CID font specific: ROS (Registry-Ordering-Supplement).
    /// </summary>
    public (string registry, string ordering, int supplement)? ROS { get; set; }

    /// <summary>
    /// CID font specific: CIDFont offset.
    /// </summary>
    public int CIDFontOffset { get; set; }

    /// <summary>
    /// CID font specific: FDArray offset (Font DICT array).
    /// </summary>
    public int FDArrayOffset { get; set; }

    /// <summary>
    /// CID font specific: FDSelect offset.
    /// </summary>
    public int FDSelectOffset { get; set; }
}

/// <summary>
/// Private DICT contains font-specific data.
/// </summary>
public class CffPrivateDict
{
    /// <summary>
    /// Default width for glyphs.
    /// </summary>
    public double DefaultWidthX { get; set; } = 0;

    /// <summary>
    /// Nominal width for glyphs.
    /// </summary>
    public double NominalWidthX { get; set; } = 0;

    /// <summary>
    /// Local Subrs offset (relative to Private DICT).
    /// </summary>
    public int SubrsOffset { get; set; }

    /// <summary>
    /// Blue values for alignment zones.
    /// </summary>
    public List<double> BlueValues { get; set; } = new();

    /// <summary>
    /// Other blue values.
    /// </summary>
    public List<double> OtherBlues { get; set; } = new();

    /// <summary>
    /// Family blues.
    /// </summary>
    public List<double> FamilyBlues { get; set; } = new();

    /// <summary>
    /// Family other blues.
    /// </summary>
    public List<double> FamilyOtherBlues { get; set; } = new();

    /// <summary>
    /// Standard horizontal stem width.
    /// </summary>
    public double StdHW { get; set; }

    /// <summary>
    /// Standard vertical stem width.
    /// </summary>
    public double StdVW { get; set; }
}
