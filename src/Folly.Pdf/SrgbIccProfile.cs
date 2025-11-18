using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Folly.Pdf;

/// <summary>
/// Provides sRGB ICC profile data for PDF/A OutputIntent.
/// PDF/A requires an OutputIntent with an ICC profile to define device-independent color.
/// </summary>
internal static class SrgbIccProfile
{
    /// <summary>
    /// Gets a minimal sRGB v2 ICC profile for PDF/A compliance.
    /// This is a simplified sRGB profile that meets PDF/A requirements.
    /// Based on IEC 61966-2-1:1999 sRGB color space specification.
    /// </summary>
    /// <remarks>
    /// This profile is a minimal ICC v2 profile containing:
    /// - Profile header (128 bytes)
    /// - Tag table with required tags (desc, wtpt, rXYZ, gXYZ, bXYZ, rTRC, gTRC, bTRC)
    /// - Tag data sections
    ///
    /// The profile defines the sRGB color space with:
    /// - D65 white point (0.3127, 0.3290)
    /// - sRGB primaries (ITU-R BT.709)
    /// - sRGB gamma (approximately 2.2 with linear segment)
    /// </remarks>
    public static byte[] GetProfile()
    {
        // For production use, this should be a proper ICC profile.
        // For now, we'll create a minimal valid ICC v2 profile that declares sRGB.
        // In a real implementation, you would embed a complete sRGB ICC profile from:
        // - http://www.color.org/srgbprofiles.xalter (official sRGB profiles)
        // - Or bundle a standard sRGB profile with the library
        //
        // For this implementation, we'll use a minimal approach that's PDF/A compliant.

        var profile = new List<byte>();

        // ICC Profile Header (128 bytes)
        profile.AddRange(BitConverter.GetBytes(1352)); // Profile size (placeholder, will update at end)
        profile.AddRange(new byte[] { 0, 0, 0, 0 }); // Preferred CMM type (none)
        profile.AddRange(new byte[] { 2, 0, 0, 0 }); // Profile version 2.0.0
        profile.AddRange(System.Text.Encoding.ASCII.GetBytes("mntr")); // Profile class: monitor (display)
        profile.AddRange(System.Text.Encoding.ASCII.GetBytes("RGB ")); // Color space: RGB
        profile.AddRange(System.Text.Encoding.ASCII.GetBytes("XYZ ")); // PCS: XYZ

        // Date/time: 2024-01-01 00:00:00 (not critical for PDF/A)
        profile.AddRange(new byte[] { 0x07, 0xE8, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0 });

        profile.AddRange(System.Text.Encoding.ASCII.GetBytes("acsp")); // Profile file signature
        profile.AddRange(new byte[] { 0, 0, 0, 0 }); // Primary platform (none)
        profile.AddRange(new byte[] { 0, 0, 0, 0 }); // Profile flags
        profile.AddRange(new byte[] { 0, 0, 0, 0 }); // Device manufacturer (none)
        profile.AddRange(new byte[] { 0, 0, 0, 0 }); // Device model (none)
        profile.AddRange(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }); // Device attributes
        profile.AddRange(new byte[] { 0, 0, 0, 0 }); // Rendering intent: perceptual

        // PCS illuminant (D50): X=0.9642, Y=1.0, Z=0.8249
        profile.AddRange(BitConverter.GetBytes(0x0000F6D6).AsEnumerable().AsEnumerable().Reverse().ToArray()); // X
        profile.AddRange(BitConverter.GetBytes(0x00010000).AsEnumerable().AsEnumerable().Reverse().ToArray()); // Y
        profile.AddRange(BitConverter.GetBytes(0x0000D32D).AsEnumerable().AsEnumerable().Reverse().ToArray()); // Z

        profile.AddRange(System.Text.Encoding.ASCII.GetBytes("Fly ")); // Profile creator signature
        profile.AddRange(new byte[44]); // Reserved (must be zero)

        // Tag Table
        var tagCount = 9; // desc, cprt, wtpt, rXYZ, gXYZ, bXYZ, rTRC, gTRC, bTRC
        profile.AddRange(BitConverter.GetBytes(tagCount).AsEnumerable().Reverse().ToArray());

        var offset = 128 + 4 + (tagCount * 12); // Header + tag count + tag table

        // Helper to add tag entry
        void AddTag(string sig, int size)
        {
            profile.AddRange(System.Text.Encoding.ASCII.GetBytes(sig));
            profile.AddRange(BitConverter.GetBytes(offset).AsEnumerable().Reverse().ToArray());
            profile.AddRange(BitConverter.GetBytes(size).AsEnumerable().Reverse().ToArray());
            offset += size;
            // Align to 4-byte boundary
            if (size % 4 != 0)
            {
                offset += 4 - (size % 4);
            }
        }

        // Tag entries (TRC curves now use 256-entry lookup tables for proper sRGB transfer function)
        var trcSize = 8 + (256 * 2); // Type signature (4) + reserved (4) + 256 ushort entries
        AddTag("desc", 90); // Profile description
        AddTag("cprt", 36); // Copyright
        AddTag("wtpt", 20); // White point
        AddTag("rXYZ", 20); // Red colorant
        AddTag("gXYZ", 20); // Green colorant
        AddTag("bXYZ", 20); // Blue colorant
        AddTag("rTRC", trcSize); // Red TRC (with proper sRGB curve)
        AddTag("gTRC", trcSize); // Green TRC (with proper sRGB curve)
        AddTag("bTRC", trcSize); // Blue TRC (with proper sRGB curve)

        // Tag data sections

        // desc tag: Profile description
        profile.AddRange(System.Text.Encoding.ASCII.GetBytes("desc"));
        profile.AddRange(new byte[] { 0, 0, 0, 0 }); // Reserved
        var desc = "sRGB IEC61966-2.1";
        profile.AddRange(BitConverter.GetBytes(desc.Length + 1).AsEnumerable().Reverse().ToArray());
        profile.AddRange(System.Text.Encoding.ASCII.GetBytes(desc));
        profile.AddRange(new byte[90 - 8 - 4 - desc.Length]); // Padding

        // cprt tag: Copyright
        profile.AddRange(System.Text.Encoding.ASCII.GetBytes("text"));
        profile.AddRange(new byte[] { 0, 0, 0, 0 }); // Reserved
        var cprt = "Public Domain";
        profile.AddRange(System.Text.Encoding.ASCII.GetBytes(cprt));
        profile.AddRange(new byte[36 - 8 - cprt.Length]); // Padding

        // wtpt tag: D65 white point in XYZ
        profile.AddRange(System.Text.Encoding.ASCII.GetBytes("XYZ "));
        profile.AddRange(new byte[] { 0, 0, 0, 0 }); // Reserved
        profile.AddRange(BitConverter.GetBytes(0x0000F351).AsEnumerable().Reverse().ToArray()); // X (D65)
        profile.AddRange(BitConverter.GetBytes(0x00010000).AsEnumerable().Reverse().ToArray()); // Y
        profile.AddRange(BitConverter.GetBytes(0x000116CC).AsEnumerable().Reverse().ToArray()); // Z

        // rXYZ tag: Red primary in XYZ
        profile.AddRange(System.Text.Encoding.ASCII.GetBytes("XYZ "));
        profile.AddRange(new byte[] { 0, 0, 0, 0 });
        profile.AddRange(BitConverter.GetBytes(0x0000A020).AsEnumerable().Reverse().ToArray()); // X
        profile.AddRange(BitConverter.GetBytes(0x00004E93).AsEnumerable().Reverse().ToArray()); // Y
        profile.AddRange(BitConverter.GetBytes(0x00000000).AsEnumerable().Reverse().ToArray()); // Z

        // gXYZ tag: Green primary in XYZ
        profile.AddRange(System.Text.Encoding.ASCII.GetBytes("XYZ "));
        profile.AddRange(new byte[] { 0, 0, 0, 0 });
        profile.AddRange(BitConverter.GetBytes(0x00004C40).AsEnumerable().Reverse().ToArray()); // X
        profile.AddRange(BitConverter.GetBytes(0x0000999A).AsEnumerable().Reverse().ToArray()); // Y
        profile.AddRange(BitConverter.GetBytes(0x00000D97).AsEnumerable().Reverse().ToArray()); // Z

        // bXYZ tag: Blue primary in XYZ
        profile.AddRange(System.Text.Encoding.ASCII.GetBytes("XYZ "));
        profile.AddRange(new byte[] { 0, 0, 0, 0 });
        profile.AddRange(BitConverter.GetBytes(0x0000266F).AsEnumerable().Reverse().ToArray()); // X
        profile.AddRange(BitConverter.GetBytes(0x00001574).AsEnumerable().Reverse().ToArray()); // Y
        profile.AddRange(BitConverter.GetBytes(0x0000B8AB).AsEnumerable().Reverse().ToArray()); // Z

        // rTRC, gTRC, bTRC tags: Proper sRGB transfer function
        // sRGB uses a piecewise function:
        // - Linear segment for dark values: output = 12.92 * input (for input <= 0.0031308)
        // - Gamma segment for bright values: output = 1.055 * input^(1/2.4) - 0.055 (for input > 0.0031308)
        // We create a 256-entry lookup table that implements this transfer function
        var srgbCurve = GenerateSrgbToneCurve();

        for (int i = 0; i < 3; i++)
        {
            profile.AddRange(System.Text.Encoding.ASCII.GetBytes("curv"));
            profile.AddRange(new byte[] { 0, 0, 0, 0 }); // Reserved
            profile.AddRange(BitConverter.GetBytes(256).AsEnumerable().Reverse().ToArray()); // Count = 256 (LUT)

            // Add 256 ushort values representing the sRGB transfer function
            foreach (var value in srgbCurve)
            {
                profile.AddRange(BitConverter.GetBytes(value).AsEnumerable().Reverse().ToArray());
            }
        }

        // Update profile size in header
        var size = profile.Count;
        var sizeBytes = BitConverter.GetBytes(size).AsEnumerable().Reverse().ToArray();
        profile[0] = sizeBytes[0];
        profile[1] = sizeBytes[1];
        profile[2] = sizeBytes[2];
        profile[3] = sizeBytes[3];

        return profile.ToArray();
    }

    /// <summary>
    /// Gets the output condition identifier for sRGB.
    /// This is used in the OutputIntent dictionary.
    /// </summary>
    public static string OutputConditionIdentifier => "sRGB IEC61966-2.1";

    /// <summary>
    /// Gets the output condition string for sRGB.
    /// </summary>
    public static string OutputCondition => "sRGB IEC61966-2.1";

    /// <summary>
    /// Gets the registry name for sRGB (ICC profile registry).
    /// </summary>
    public static string RegistryName => "http://www.color.org";

    /// <summary>
    /// Generates the sRGB tone reproduction curve (TRC) as a 256-entry lookup table.
    /// Implements the IEC 61966-2-1 sRGB transfer function with linear and gamma segments.
    /// </summary>
    /// <returns>Array of 256 ushort values (0-65535 range) representing the sRGB transfer function.</returns>
    private static ushort[] GenerateSrgbToneCurve()
    {
        var curve = new ushort[256];

        for (int i = 0; i < 256; i++)
        {
            // Input value in 0.0-1.0 range
            double input = i / 255.0;

            // Apply sRGB transfer function (encoding from linear to sRGB)
            double output;
            if (input <= 0.0031308)
            {
                // Linear segment for dark values
                output = 12.92 * input;
            }
            else
            {
                // Gamma segment for bright values
                output = 1.055 * Math.Pow(input, 1.0 / 2.4) - 0.055;
            }

            // Clamp to 0.0-1.0 and convert to ushort (0-65535)
            output = Math.Clamp(output, 0.0, 1.0);
            curve[i] = (ushort)Math.Round(output * 65535.0);
        }

        return curve;
    }
}
