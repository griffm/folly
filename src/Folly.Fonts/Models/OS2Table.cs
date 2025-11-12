namespace Folly.Fonts.Models;

/// <summary>
/// OS/2 and Windows metrics table data.
/// Contains Windows-specific font metrics and metadata.
/// </summary>
public class OS2Table
{
    /// <summary>
    /// Table version number.
    /// </summary>
    public ushort Version { get; set; }

    /// <summary>
    /// Average weighted advance width of lower case letters and space.
    /// </summary>
    public short XAvgCharWidth { get; set; }

    /// <summary>
    /// Visual weight (stroke thickness) of the font (1-1000).
    /// 400 is normal, 700 is bold.
    /// </summary>
    public ushort WeightClass { get; set; }

    /// <summary>
    /// Width classification (1-9).
    /// 5 is normal/medium, 1 is ultra-condensed, 9 is ultra-expanded.
    /// </summary>
    public ushort WidthClass { get; set; }

    /// <summary>
    /// Type flags (embedding permissions, etc.).
    /// </summary>
    public ushort Type { get; set; }

    /// <summary>
    /// Typographic ascender (recommended distance above baseline).
    /// </summary>
    public short TypoAscender { get; set; }

    /// <summary>
    /// Typographic descender (recommended distance below baseline, typically negative).
    /// </summary>
    public short TypoDescender { get; set; }

    /// <summary>
    /// Typographic line gap.
    /// </summary>
    public short TypoLineGap { get; set; }

    /// <summary>
    /// Windows ascender (must be equal to or greater than TypoAscender).
    /// </summary>
    public ushort WinAscent { get; set; }

    /// <summary>
    /// Windows descender (must be equal to or greater than absolute value of TypoDescender).
    /// </summary>
    public ushort WinDescent { get; set; }
}
