namespace Folly.Images;

/// <summary>
/// Utility methods for image handling and conversion.
/// </summary>
public static class ImageUtilities
{
    /// <summary>
    /// Converts pixel dimensions to points based on DPI.
    /// PDF uses points as its unit (1 point = 1/72 inch).
    /// </summary>
    /// <param name="pixels">Dimension in pixels</param>
    /// <param name="dpi">Dots per inch (resolution)</param>
    /// <param name="defaultDpi">Default DPI to use if dpi is 0 or invalid</param>
    /// <returns>Dimension in points</returns>
    public static double PixelsToPoints(double pixels, double dpi, double defaultDpi = 72.0)
    {
        // Use provided DPI if valid, otherwise use default
        double effectiveDpi = dpi > 0 ? dpi : defaultDpi;

        // Convert: pixels * (inches/pixel) * (points/inch)
        // pixels * (1/dpi) * 72 = pixels * 72 / dpi
        return pixels * 72.0 / effectiveDpi;
    }

    /// <summary>
    /// Calculates the intrinsic size in points for an image based on its metadata.
    /// </summary>
    /// <param name="imageInfo">Image information with dimensions and DPI</param>
    /// <param name="defaultDpi">Default DPI to use if image doesn't specify DPI</param>
    /// <returns>Tuple of (width in points, height in points)</returns>
    public static (double Width, double Height) GetIntrinsicSizeInPoints(
        ImageInfo imageInfo,
        double defaultDpi = 72.0)
    {
        double widthPoints = PixelsToPoints(
            imageInfo.Width,
            imageInfo.HorizontalDpi,
            defaultDpi);

        double heightPoints = PixelsToPoints(
            imageInfo.Height,
            imageInfo.VerticalDpi > 0 ? imageInfo.VerticalDpi : imageInfo.HorizontalDpi,
            defaultDpi);

        return (widthPoints, heightPoints);
    }
}
