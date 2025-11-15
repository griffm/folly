namespace Folly.Images;

/// <summary>
/// Interface for image format parsers.
/// Each supported format (JPEG, PNG, BMP, GIF, TIFF) implements this interface.
/// </summary>
public interface IImageParser
{
    /// <summary>
    /// Gets the format name (e.g., "JPEG", "PNG", "BMP", "GIF", "TIFF").
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Determines whether this parser can handle the given image data
    /// based on magic bytes/file signature.
    /// </summary>
    bool CanParse(byte[] data);

    /// <summary>
    /// Parses the image data and extracts metadata and decoded image data.
    /// </summary>
    /// <param name="data">The raw image file data.</param>
    /// <returns>Parsed image information including dimensions, color space, and decoded data.</returns>
    ImageInfo Parse(byte[] data);
}
