namespace Folly.Images;

/// <summary>
/// Detects image formats based on magic bytes / file signatures.
/// </summary>
public static class ImageFormatDetector
{
    /// <summary>
    /// Detects the image format from raw byte data.
    /// </summary>
    /// <param name="data">The image file data.</param>
    /// <returns>The detected format name (JPEG, PNG, BMP, GIF, TIFF) or "UNKNOWN".</returns>
    public static string Detect(byte[] data)
    {
        if (data == null || data.Length < 8)
            return "UNKNOWN";

        // JPEG: FF D8 FF
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "JPEG";

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (data.Length >= 8 &&
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return "PNG";

        // BMP: 42 4D (BM)
        if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D)
            return "BMP";

        // GIF: 47 49 46 38 (GIF8) followed by either 37 61 (7a) or 39 61 (9a)
        if (data.Length >= 6 &&
            data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38 &&
            (data[4] == 0x37 || data[4] == 0x39) && data[5] == 0x61)
            return "GIF";

        // TIFF: 49 49 2A 00 (little-endian) or 4D 4D 00 2A (big-endian)
        if (data.Length >= 4)
        {
            if ((data[0] == 0x49 && data[1] == 0x49 && data[2] == 0x2A && data[3] == 0x00) ||
                (data[0] == 0x4D && data[1] == 0x4D && data[2] == 0x00 && data[3] == 0x2A))
                return "TIFF";
        }

        return "UNKNOWN";
    }
}
