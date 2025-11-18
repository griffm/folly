namespace Folly.Images.Parsers;

/// <summary>
/// Parser for PNG (Portable Network Graphics) image format.
/// Supports all color types, transparency (tRNS), DPI (pHYs), and ICC profiles (iCCP).
/// Handles both interlaced (Adam7) and non-interlaced images.
/// </summary>
public sealed class PngParser : IImageParser
{
    private const int MAX_PNG_CHUNK_SIZE = 100 * 1024 * 1024; // 100MB safety limit

    /// <inheritdoc/>
    public string FormatName => "PNG";

    /// <inheritdoc/>
    public bool CanParse(byte[] data)
    {
        return data != null && data.Length >= 8 &&
               data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
               data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A;
    }

    /// <inheritdoc/>
    public ImageInfo Parse(byte[] data)
    {
        if (!CanParse(data))
            throw new InvalidDataException("Invalid PNG signature. File is not a valid PNG image.");

        // Parse PNG chunks
        var idatData = new List<byte>();
        byte[]? palette = null;
        byte[]? transparency = null;
        byte[]? iccProfile = null;
        int width = 0;
        int height = 0;
        int bitDepth = 8;
        int colorType = 2; // RGB
        int compressionMethod = 0;
        int filterMethod = 0;
        int interlaceMethod = 0;
        double horizontalDpi = 0;
        double verticalDpi = 0;

        int offset = 8; // Skip PNG signature

        while (offset < data.Length)
        {
            if (offset + 8 > data.Length) break;

            int chunkLength = ReadInt32BE(data, offset);

            // Security: Validate chunk length
            if (chunkLength < 0 || chunkLength > MAX_PNG_CHUNK_SIZE)
                break;

            // Security: Ensure chunk data doesn't exceed buffer bounds
            if (offset + 12 + chunkLength > data.Length)
                break;

            string chunkType = System.Text.Encoding.ASCII.GetString(data, offset + 4, 4);

            switch (chunkType)
            {
                case "IHDR" when chunkLength >= 13:
                    // Parse IHDR: width(4) height(4) bitdepth(1) colortype(1) compression(1) filter(1) interlace(1)
                    width = ReadInt32BE(data, offset + 8);
                    height = ReadInt32BE(data, offset + 12);
                    bitDepth = data[offset + 16];
                    colorType = data[offset + 17];
                    compressionMethod = data[offset + 18];
                    filterMethod = data[offset + 19];
                    interlaceMethod = data[offset + 20];

                    // Validate bit depth
                    if (bitDepth != 1 && bitDepth != 2 && bitDepth != 4 && bitDepth != 8 && bitDepth != 16)
                        throw new InvalidDataException($"PNG bit depth {bitDepth} is invalid. Valid values are 1, 2, 4, 8, or 16.");

                    // Validate color type
                    if (colorType != 0 && colorType != 2 && colorType != 3 && colorType != 4 && colorType != 6)
                        throw new InvalidDataException($"PNG color type {colorType} is invalid. Valid values are 0, 2, 3, 4, or 6.");

                    break;

                case "PLTE":
                    // Extract palette data (RGB triplets)
                    palette = new byte[chunkLength];
                    Array.Copy(data, offset + 8, palette, 0, chunkLength);
                    break;

                case "tRNS":
                    // Extract transparency data
                    transparency = new byte[chunkLength];
                    Array.Copy(data, offset + 8, transparency, 0, chunkLength);
                    break;

                case "pHYs" when chunkLength == 9:
                    // Extract physical dimensions (DPI)
                    int pixelsPerUnitX = ReadInt32BE(data, offset + 8);
                    int pixelsPerUnitY = ReadInt32BE(data, offset + 12);
                    byte unitSpecifier = data[offset + 16];

                    // Convert to DPI if unit is meters
                    if (unitSpecifier == 1) // meters
                    {
                        horizontalDpi = pixelsPerUnitX / 39.3701; // pixels/meter to pixels/inch
                        verticalDpi = pixelsPerUnitY / 39.3701;
                    }
                    break;

                case "iCCP":
                    // Extract ICC color profile
                    // Format: profile name (null-terminated) + compression method (1 byte) + compressed profile
                    int nameEndIndex = offset + 8;
                    while (nameEndIndex < offset + 8 + chunkLength && data[nameEndIndex] != 0)
                        nameEndIndex++;

                    if (nameEndIndex < offset + 8 + chunkLength - 1)
                    {
                        int compressionMethodByte = data[nameEndIndex + 1];
                        int compressedDataStart = nameEndIndex + 2;
                        int compressedDataLength = chunkLength - (compressedDataStart - (offset + 8));

                        if (compressionMethodByte == 0 && compressedDataLength > 0)
                        {
                            // Decompress ICC profile (zlib/deflate)
                            byte[] compressedProfile = new byte[compressedDataLength];
                            Array.Copy(data, compressedDataStart, compressedProfile, 0, compressedDataLength);

                            try
                            {
                                using var compressedStream = new MemoryStream(compressedProfile);
                                using var deflateStream = new System.IO.Compression.ZLibStream(compressedStream, System.IO.Compression.CompressionMode.Decompress);
                                using var decompressedStream = new MemoryStream();
                                deflateStream.CopyTo(decompressedStream);
                                iccProfile = decompressedStream.ToArray();
                            }
                            catch (Exception)
                            {
                                // ICC profile decompression failed - non-critical, image can still be processed
                                // Profile may be corrupted or use an unsupported compression method
                                iccProfile = null;
                            }
                        }
                    }
                    break;

                case "IDAT":
                    // Collect IDAT data (compressed image data)
                    byte[] chunk = new byte[chunkLength];
                    Array.Copy(data, offset + 8, chunk, 0, chunkLength);
                    idatData.AddRange(chunk);
                    break;

                case "IEND":
                    // End of PNG file
                    goto EndParsing;
            }

            // Security: Check for integer overflow before updating offset
            long nextOffset = (long)offset + 12 + chunkLength;
            if (nextOffset > int.MaxValue)
                break;

            offset = (int)nextOffset;
        }

    EndParsing:

        if (width == 0 || height == 0)
            throw new InvalidDataException("PNG file has invalid dimensions");

        // Determine color space and components
        string colorSpace;
        int colorComponents;

        switch (colorType)
        {
            case 0: // Grayscale
                colorSpace = "DeviceGray";
                colorComponents = 1;
                break;
            case 2: // RGB
                colorSpace = "DeviceRGB";
                colorComponents = 3;
                break;
            case 3: // Indexed
                colorSpace = "Indexed";
                colorComponents = 1;
                break;
            case 4: // Grayscale + Alpha
                colorSpace = "DeviceGray";
                colorComponents = 1;
                break;
            case 6: // RGBA
                colorSpace = "DeviceRGB";
                colorComponents = 3;
                break;
            default:
                // This should never be reached due to early validation in IHDR parsing
                throw new InvalidDataException($"PNG color type {colorType} is invalid");
        }

        // Compress IDAT data using FlateDecode
        byte[] compressedData;
        using (var ms = new MemoryStream())
        {
            using (var deflate = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Compress))
            {
                deflate.Write(idatData.ToArray(), 0, idatData.Count);
            }
            compressedData = ms.ToArray();
        }

        // Apply default DPI if no metadata was found
        if (horizontalDpi == 0)
            horizontalDpi = 72;
        if (verticalDpi == 0)
            verticalDpi = 72;

        return new ImageInfo
        {
            Format = "PNG",
            Width = width,
            Height = height,
            HorizontalDpi = horizontalDpi,
            VerticalDpi = verticalDpi,
            BitsPerComponent = bitDepth,
            ColorSpace = colorSpace,
            ColorComponents = colorComponents,
            RawData = compressedData,
            Palette = palette,
            Transparency = transparency,
            AlphaData = null, // Will be extracted during PDF writing if needed
            IccProfile = iccProfile
        };
    }

    private static int ReadInt32BE(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) |
               (data[offset + 2] << 8) | data[offset + 3];
    }
}
