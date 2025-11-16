namespace Folly.Fonts.Models;

/// <summary>
/// Font header ('head') table data.
/// Contains global font information including version, timestamps, and style flags.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/head
/// </summary>
public class HeadTable
{
    /// <summary>
    /// Font revision number (Fixed 16.16 format stored as uint32).
    /// Example: 0x00010000 = version 1.0
    /// </summary>
    public uint FontRevision { get; set; }

    /// <summary>
    /// Font creation timestamp (Mac epoch: seconds since 1904-01-01 00:00:00 UTC).
    /// </summary>
    public long Created { get; set; }

    /// <summary>
    /// Font modification timestamp (Mac epoch: seconds since 1904-01-01 00:00:00 UTC).
    /// </summary>
    public long Modified { get; set; }

    /// <summary>
    /// Font flags indicating various font properties.
    /// Bit 0: Baseline at y=0
    /// Bit 1: Left sidebearing at x=0
    /// Bit 2: Instructions may depend on point size
    /// Bit 3: Force ppem to integer values
    /// Bit 4: Instructions may alter advance width
    /// </summary>
    public ushort Flags { get; set; }

    /// <summary>
    /// Mac style flags indicating font style.
    /// Bit 0: Bold
    /// Bit 1: Italic
    /// Bit 2: Underline
    /// Bit 3: Outline
    /// Bit 4: Shadow
    /// Bit 5: Condensed
    /// Bit 6: Extended
    /// </summary>
    public ushort MacStyle { get; set; }

    /// <summary>
    /// Gets whether the font is bold according to MacStyle.
    /// </summary>
    public bool IsBold => (MacStyle & 0x01) != 0;

    /// <summary>
    /// Gets whether the font is italic according to MacStyle.
    /// </summary>
    public bool IsItalic => (MacStyle & 0x02) != 0;

    /// <summary>
    /// Gets the created timestamp as a DateTime (UTC).
    /// </summary>
    public DateTime GetCreatedDateTime()
    {
        return ConvertMacEpochToDateTime(Created);
    }

    /// <summary>
    /// Gets the modified timestamp as a DateTime (UTC).
    /// </summary>
    public DateTime GetModifiedDateTime()
    {
        return ConvertMacEpochToDateTime(Modified);
    }

    /// <summary>
    /// Converts Mac epoch time (seconds since 1904-01-01) to DateTime.
    /// </summary>
    private static DateTime ConvertMacEpochToDateTime(long macEpochSeconds)
    {
        // Mac epoch starts at 1904-01-01 00:00:00 UTC
        var macEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return macEpoch.AddSeconds(macEpochSeconds);
    }

    /// <summary>
    /// Converts DateTime to Mac epoch time (seconds since 1904-01-01).
    /// </summary>
    public static long ConvertDateTimeToMacEpoch(DateTime dateTime)
    {
        // Mac epoch starts at 1904-01-01 00:00:00 UTC
        var macEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Ensure we're working with UTC
        if (dateTime.Kind == DateTimeKind.Local)
        {
            dateTime = dateTime.ToUniversalTime();
        }
        else if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            // Assume UTC for unspecified
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        var timeSpan = dateTime - macEpoch;
        return (long)timeSpan.TotalSeconds;
    }
}
