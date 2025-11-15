namespace Folly.Images;

/// <summary>
/// Contains metadata about a parsed image.
/// </summary>
public sealed class ImageInfo
{
    /// <summary>
    /// Gets or sets the image format (JPEG, PNG, BMP, GIF, TIFF).
    /// </summary>
    public required string Format { get; init; }

    /// <summary>
    /// Gets or sets the image width in pixels.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Gets or sets the image height in pixels.
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// Gets or sets the horizontal DPI/resolution.
    /// Returns 0 if not specified in the image.
    /// </summary>
    public double HorizontalDpi { get; init; }

    /// <summary>
    /// Gets or sets the vertical DPI/resolution.
    /// Returns 0 if not specified in the image.
    /// </summary>
    public double VerticalDpi { get; init; }

    /// <summary>
    /// Gets or sets the EXIF orientation value (1-8).
    /// 1 = normal, 3 = 180°, 6 = 90° CW, 8 = 270° CW
    /// Returns 1 (normal) if no EXIF orientation is present.
    /// </summary>
    public int Orientation { get; init; } = 1;

    /// <summary>
    /// Gets or sets the bits per component/channel (typically 8 or 16).
    /// </summary>
    public int BitsPerComponent { get; init; } = 8;

    /// <summary>
    /// Gets or sets the color space (DeviceRGB, DeviceGray, DeviceCMYK, Indexed).
    /// </summary>
    public string ColorSpace { get; init; } = "DeviceRGB";

    /// <summary>
    /// Gets or sets the number of color components (1=gray, 3=RGB, 4=CMYK).
    /// </summary>
    public int ColorComponents { get; init; } = 3;

    /// <summary>
    /// Gets or sets the raw image data (may be compressed depending on format).
    /// </summary>
    public byte[]? RawData { get; init; }

    /// <summary>
    /// Gets or sets the palette data for indexed color images (PNG, GIF).
    /// </summary>
    public byte[]? Palette { get; init; }

    /// <summary>
    /// Gets or sets the transparency data (tRNS chunk for PNG, transparent color for GIF).
    /// </summary>
    public byte[]? Transparency { get; init; }

    /// <summary>
    /// Gets or sets the alpha channel data (separate from RGB, for PNG RGBA).
    /// </summary>
    public byte[]? AlphaData { get; init; }

    /// <summary>
    /// Gets whether this image requires rotation based on EXIF orientation.
    /// </summary>
    public bool RequiresRotation => Orientation >= 5 && Orientation <= 8;

    /// <summary>
    /// Gets the rotation angle in degrees (0, 90, 180, 270) based on EXIF orientation.
    /// </summary>
    public int RotationDegrees => Orientation switch
    {
        3 => 180,  // Rotate 180°
        6 => 90,   // Rotate 90° CW
        8 => 270,  // Rotate 270° CW (90° CCW)
        _ => 0     // No rotation
    };

    /// <summary>
    /// Gets the effective width after applying EXIF orientation.
    /// For orientations 5-8 (rotated 90° or 270°), width and height are swapped.
    /// </summary>
    public int EffectiveWidth => RequiresRotation ? Height : Width;

    /// <summary>
    /// Gets the effective height after applying EXIF orientation.
    /// For orientations 5-8 (rotated 90° or 270°), width and height are swapped.
    /// </summary>
    public int EffectiveHeight => RequiresRotation ? Width : Height;
}
