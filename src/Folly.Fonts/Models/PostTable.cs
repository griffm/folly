namespace Folly.Fonts.Models;

/// <summary>
/// PostScript table data.
/// Contains PostScript-related font information.
/// </summary>
public class PostTable
{
    /// <summary>
    /// Format version (typically 1.0, 2.0, 2.5, or 3.0).
    /// </summary>
    public double Version { get; set; }

    /// <summary>
    /// Italic angle in counter-clockwise degrees from the vertical.
    /// Zero for upright text, negative for right-leaning text.
    /// </summary>
    public double ItalicAngle { get; set; }

    /// <summary>
    /// Underline position (distance from baseline, typically negative).
    /// </summary>
    public short UnderlinePosition { get; set; }

    /// <summary>
    /// Underline thickness.
    /// </summary>
    public short UnderlineThickness { get; set; }

    /// <summary>
    /// Whether the font is monospaced (fixed pitch).
    /// 0 for proportional, non-zero for monospaced.
    /// </summary>
    public uint IsFixedPitch { get; set; }

    /// <summary>
    /// Whether this is a monospaced font.
    /// </summary>
    public bool IsMonospaced => IsFixedPitch != 0;
}
