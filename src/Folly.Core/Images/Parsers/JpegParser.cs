namespace Folly.Images.Parsers;

/// <summary>
/// Parser for JPEG (Joint Photographic Experts Group) image format.
/// Supports RGB and CMYK color spaces, JFIF and EXIF metadata extraction,
/// ICC color profiles, and DPI resolution information.
/// </summary>
public sealed class JpegParser : IImageParser
{
    /// <inheritdoc/>
    public string FormatName => "JPEG";

    /// <inheritdoc/>
    public bool CanParse(byte[] data)
    {
        return data != null && data.Length >= 2 &&
               data[0] == 0xFF && data[1] == 0xD8; // JPEG SOI marker
    }

    /// <inheritdoc/>
    public ImageInfo Parse(byte[] data)
    {
        if (!CanParse(data))
            throw new InvalidDataException("Invalid JPEG signature. File is not a valid JPEG image.");

        int width = 0;
        int height = 0;
        int bitsPerComponent = 8;
        string colorSpace = "DeviceRGB";
        int colorComponents = 3;
        double horizontalDpi = 0;
        double verticalDpi = 0;
        byte[]? iccProfile = null;

        int offset = 2; // Skip SOI marker (0xFF 0xD8)

        while (offset + 1 < data.Length)
        {
            // Find next marker
            if (data[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            byte marker = data[offset + 1];
            offset += 2;

            // Skip padding bytes (0xFF 0xFF)
            while (offset < data.Length && data[offset] == 0xFF)
                offset++;

            // Check for markers without length
            if (marker == 0xD8 || marker == 0xD9 || (marker >= 0xD0 && marker <= 0xD7))
                continue;

            // Read segment length
            if (offset + 2 > data.Length)
                break;

            int segmentLength = (data[offset] << 8) | data[offset + 1];
            if (segmentLength < 2 || offset + segmentLength > data.Length)
                break;

            switch (marker)
            {
                case 0xC0: // SOF0 - Baseline DCT
                case 0xC1: // SOF1 - Extended Sequential DCT
                case 0xC2: // SOF2 - Progressive DCT
                case 0xC3: // SOF3 - Lossless
                case 0xC5: // SOF5 - Differential Sequential DCT
                case 0xC6: // SOF6 - Differential Progressive DCT
                case 0xC7: // SOF7 - Differential Lossless
                case 0xC9: // SOF9 - Extended Sequential DCT (Arithmetic)
                case 0xCA: // SOF10 - Progressive DCT (Arithmetic)
                case 0xCB: // SOF11 - Lossless (Arithmetic)
                case 0xCD: // SOF13 - Differential Sequential DCT (Arithmetic)
                case 0xCE: // SOF14 - Differential Progressive DCT (Arithmetic)
                case 0xCF: // SOF15 - Differential Lossless (Arithmetic)
                    if (segmentLength >= 8)
                    {
                        bitsPerComponent = data[offset + 2];
                        height = (data[offset + 3] << 8) | data[offset + 4];
                        width = (data[offset + 5] << 8) | data[offset + 6];
                        int components = data[offset + 7];

                        // Determine color space based on number of components
                        switch (components)
                        {
                            case 1:
                                colorSpace = "DeviceGray";
                                colorComponents = 1;
                                break;
                            case 3:
                                colorSpace = "DeviceRGB";
                                colorComponents = 3;
                                break;
                            case 4:
                                colorSpace = "DeviceCMYK";
                                colorComponents = 4;
                                break;
                            default:
                                colorSpace = "DeviceRGB";
                                colorComponents = 3;
                                break;
                        }
                    }
                    break;

                case 0xE0: // APP0 - JFIF
                    if (segmentLength >= 16)
                    {
                        // Check for JFIF identifier
                        if (data[offset + 2] == 'J' && data[offset + 3] == 'F' &&
                            data[offset + 4] == 'I' && data[offset + 5] == 'F' && data[offset + 6] == 0)
                        {
                            byte densityUnits = data[offset + 9];
                            int xDensity = (data[offset + 10] << 8) | data[offset + 11];
                            int yDensity = (data[offset + 12] << 8) | data[offset + 13];

                            // Convert density to DPI
                            switch (densityUnits)
                            {
                                case 1: // dots per inch
                                    horizontalDpi = xDensity;
                                    verticalDpi = yDensity;
                                    break;
                                case 2: // dots per cm
                                    horizontalDpi = xDensity * 2.54;
                                    verticalDpi = yDensity * 2.54;
                                    break;
                                    // case 0: aspect ratio only, no DPI
                            }
                        }
                    }
                    break;

                case 0xE1: // APP1 - EXIF
                    if (segmentLength >= 14)
                    {
                        // Check for EXIF identifier
                        if (data[offset + 2] == 'E' && data[offset + 3] == 'x' &&
                            data[offset + 4] == 'i' && data[offset + 5] == 'f' && data[offset + 6] == 0 && data[offset + 7] == 0)
                        {
                            // Parse EXIF for DPI (XResolution, YResolution)
                            // This is complex and requires TIFF tag parsing
                            // For now, we'll rely on JFIF for DPI
                        }
                    }
                    break;

                case 0xE2: // APP2 - ICC Profile
                    if (segmentLength >= 14)
                    {
                        // Check for ICC_PROFILE identifier
                        if (data[offset + 2] == 'I' && data[offset + 3] == 'C' &&
                            data[offset + 4] == 'C' && data[offset + 5] == '_' &&
                            data[offset + 6] == 'P' && data[offset + 7] == 'R' &&
                            data[offset + 8] == 'O' && data[offset + 9] == 'F' &&
                            data[offset + 10] == 'I' && data[offset + 11] == 'L' &&
                            data[offset + 12] == 'E' && data[offset + 13] == 0)
                        {
                            // ICC profiles can be split across multiple APP2 markers
                            // For now, extract the first chunk
                            // Format: "ICC_PROFILE\0" + sequence number (1) + total count (1) + profile data
                            if (segmentLength >= 16)
                            {
                                int profileDataLength = segmentLength - 16;
                                iccProfile = new byte[profileDataLength];
                                Array.Copy(data, offset + 16, iccProfile, 0, profileDataLength);
                            }
                        }
                    }
                    break;

                case 0xDA: // SOS - Start of Scan (image data follows, stop parsing)
                    goto EndParsing;
            }

            offset += segmentLength;
        }

    EndParsing:

        if (width == 0 || height == 0)
            throw new InvalidDataException("JPEG file has invalid dimensions");

        return new ImageInfo
        {
            Format = "JPEG",
            Width = width,
            Height = height,
            HorizontalDpi = horizontalDpi,
            VerticalDpi = verticalDpi,
            BitsPerComponent = bitsPerComponent,
            ColorSpace = colorSpace,
            ColorComponents = colorComponents,
            RawData = data, // JPEG data is already compressed
            Palette = null,
            Transparency = null,
            AlphaData = null,
            IccProfile = iccProfile
        };
    }
}
