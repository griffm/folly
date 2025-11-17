namespace Folly.Images.Parsers;

/// <summary>
/// Parser for BMP (Windows Bitmap) image format.
/// Supports:
/// - Color depths: 8-bit indexed, 24-bit RGB, 32-bit RGBA
/// - Compression: Uncompressed (BI_RGB), RLE8, RLE4
/// - Top-down and bottom-up orientation
/// </summary>
public sealed class BmpParser : IImageParser
{
    /// <inheritdoc/>
    public string FormatName => "BMP";

    /// <inheritdoc/>
    public bool CanParse(byte[] data)
    {
        return data != null && data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D;
    }

    /// <inheritdoc/>
    public ImageInfo Parse(byte[] data)
    {
        if (!CanParse(data))
            throw new InvalidDataException("Invalid BMP file signature");

        if (data.Length < 54) // Minimum BMP header size
            throw new InvalidDataException("BMP file is too small");

        // Parse BMP file header (14 bytes)
        // int fileSize = ReadInt32LE(data, 2);
        int dataOffset = ReadInt32LE(data, 10);

        // Parse DIB header (BITMAPINFOHEADER = 40 bytes minimum)
        int dibHeaderSize = ReadInt32LE(data, 14);

        if (dibHeaderSize < 40)
            throw new NotSupportedException($"Unsupported BMP DIB header size: {dibHeaderSize}. Only BITMAPINFOHEADER (40 bytes) is supported.");

        int width = ReadInt32LE(data, 18);
        int height = ReadInt32LE(data, 22);
        bool bottomUp = height > 0;
        height = Math.Abs(height); // Height can be negative for top-down BMPs

        // int planes = ReadInt16LE(data, 26); // Should be 1
        int bitsPerPixel = ReadInt16LE(data, 28);
        int compression = ReadInt32LE(data, 30);

        // Validate compression and bits per pixel combination
        // 0 = BI_RGB (uncompressed), 1 = BI_RLE8, 2 = BI_RLE4
        if (compression > 2)
            throw new NotSupportedException($"BMP compression method {compression} not supported. Supported: 0 (uncompressed), 1 (RLE8), 2 (RLE4).");

        if (compression == 1 && bitsPerPixel != 8)
            throw new InvalidDataException($"RLE8 compression requires 8-bit color depth, found {bitsPerPixel}-bit.");

        if (compression == 2 && bitsPerPixel != 4)
            throw new InvalidDataException($"RLE4 compression requires 4-bit color depth, found {bitsPerPixel}-bit.");

        if (bitsPerPixel != 4 && bitsPerPixel != 8 && bitsPerPixel != 24 && bitsPerPixel != 32)
            throw new NotSupportedException($"BMP with {bitsPerPixel} bits per pixel not supported. Supported: 4, 8, 24, 32.");

        // Parse DPI from pixels per meter (if present)
        double horizontalDpi = 0;
        double verticalDpi = 0;

        if (dibHeaderSize >= 40)
        {
            int xPixelsPerMeter = ReadInt32LE(data, 38);
            int yPixelsPerMeter = ReadInt32LE(data, 42);

            if (xPixelsPerMeter > 0)
                horizontalDpi = xPixelsPerMeter * 0.0254; // Convert pixels/meter to pixels/inch

            if (yPixelsPerMeter > 0)
                verticalDpi = yPixelsPerMeter * 0.0254;
        }

        // Read color palette for indexed color images (4-bit and 8-bit)
        byte[]? palette = null;
        if (bitsPerPixel <= 8)
        {
            // Number of colors used (offset 46), or default to 2^bitsPerPixel
            int colorsUsed = dibHeaderSize >= 40 ? ReadInt32LE(data, 46) : 0;
            if (colorsUsed == 0)
                colorsUsed = 1 << bitsPerPixel; // 2^bitsPerPixel

            int paletteOffset = 14 + dibHeaderSize;
            int paletteSize = colorsUsed * 4; // Each entry is 4 bytes: B, G, R, Reserved

            if (paletteOffset + paletteSize > data.Length)
                throw new InvalidDataException("BMP color palette is truncated");

            // Convert BMP palette (BGRA) to PDF palette (RGB)
            palette = new byte[colorsUsed * 3];
            for (int i = 0; i < colorsUsed; i++)
            {
                byte b = data[paletteOffset + i * 4];
                byte g = data[paletteOffset + i * 4 + 1];
                byte r = data[paletteOffset + i * 4 + 2];
                // byte reserved = data[paletteOffset + i * 4 + 3];

                palette[i * 3] = r;
                palette[i * 3 + 1] = g;
                palette[i * 3 + 2] = b;
            }
        }

        // Decode pixel data based on format
        byte[] pixelData;
        byte[]? alphaData = null;
        string colorSpace;
        int colorComponents;

        if (bitsPerPixel == 8)
        {
            // 8-bit indexed color
            if (compression == 1) // RLE8
            {
                pixelData = DecompressRle8(data, dataOffset, width, height, bottomUp);
            }
            else // Uncompressed
            {
                int rowStride = ((width + 3) / 4) * 4; // Padded to 4-byte boundary
                pixelData = DecodeIndexed8Bit(data, dataOffset, width, height, rowStride, bottomUp);
            }

            colorSpace = "Indexed";
            colorComponents = 1;
        }
        else if (bitsPerPixel == 4)
        {
            // 4-bit indexed color
            if (compression == 2) // RLE4
            {
                pixelData = DecompressRle4(data, dataOffset, width, height, bottomUp);
            }
            else // Uncompressed
            {
                int rowStride = ((width + 7) / 8) * 4; // 2 pixels per byte, padded to 4-byte boundary
                pixelData = DecodeIndexed4Bit(data, dataOffset, width, height, rowStride, bottomUp);
            }

            colorSpace = "Indexed";
            colorComponents = 1;
        }
        else if (bitsPerPixel == 32)
        {
            // 32-bit RGBA
            int rowStride = width * 4;
            var (rgbData, alpha) = Decode32BitRgba(data, dataOffset, width, height, rowStride, bottomUp);
            pixelData = rgbData;
            alphaData = alpha;
            colorSpace = "DeviceRGB";
            colorComponents = 3;
        }
        else // 24-bit RGB
        {
            int rowStride = ((width * 3 + 3) / 4) * 4; // Padded to 4-byte boundary
            pixelData = Decode24BitRgb(data, dataOffset, width, height, rowStride, bottomUp);
            colorSpace = "DeviceRGB";
            colorComponents = 3;
        }

        // Compress pixel data for PDF embedding
        byte[] compressedData = CompressData(pixelData);
        byte[]? compressedAlpha = alphaData != null ? CompressData(alphaData) : null;

        return new ImageInfo
        {
            Format = "BMP",
            Width = width,
            Height = height,
            HorizontalDpi = horizontalDpi,
            VerticalDpi = verticalDpi,
            BitsPerComponent = 8,
            ColorSpace = colorSpace,
            ColorComponents = colorComponents,
            RawData = compressedData,
            AlphaData = compressedAlpha,
            Palette = palette
        };
    }

    private static int ReadInt32LE(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
    }

    private static int ReadInt16LE(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8);
    }

    private static byte[] CompressData(byte[] data)
    {
        using var output = new MemoryStream();
        using (var compressor = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
        {
            compressor.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    /// <summary>
    /// Decodes 24-bit RGB BMP data (BGR format) to RGB.
    /// </summary>
    private static byte[] Decode24BitRgb(byte[] data, int dataOffset, int width, int height, int rowStride, bool bottomUp)
    {
        byte[] rgbData = new byte[width * height * 3];

        for (int y = 0; y < height; y++)
        {
            int srcRow = bottomUp ? (height - 1 - y) : y;
            int srcOffset = dataOffset + srcRow * rowStride;
            int dstOffset = y * width * 3;

            for (int x = 0; x < width; x++)
            {
                // BMP stores as BGR, PDF needs RGB
                byte b = data[srcOffset + x * 3];
                byte g = data[srcOffset + x * 3 + 1];
                byte r = data[srcOffset + x * 3 + 2];

                rgbData[dstOffset + x * 3] = r;
                rgbData[dstOffset + x * 3 + 1] = g;
                rgbData[dstOffset + x * 3 + 2] = b;
            }
        }

        return rgbData;
    }

    /// <summary>
    /// Decodes 32-bit RGBA BMP data (BGRA format) to RGB + separate alpha.
    /// </summary>
    private static (byte[] RgbData, byte[] AlphaData) Decode32BitRgba(byte[] data, int dataOffset, int width, int height, int rowStride, bool bottomUp)
    {
        byte[] rgbData = new byte[width * height * 3];
        byte[] alphaData = new byte[width * height];

        for (int y = 0; y < height; y++)
        {
            int srcRow = bottomUp ? (height - 1 - y) : y;
            int srcOffset = dataOffset + srcRow * rowStride;
            int dstOffset = y * width * 3;
            int alphaOffset = y * width;

            for (int x = 0; x < width; x++)
            {
                // BMP stores as BGRA, PDF needs RGB + separate alpha
                byte b = data[srcOffset + x * 4];
                byte g = data[srcOffset + x * 4 + 1];
                byte r = data[srcOffset + x * 4 + 2];
                byte a = data[srcOffset + x * 4 + 3];

                rgbData[dstOffset + x * 3] = r;
                rgbData[dstOffset + x * 3 + 1] = g;
                rgbData[dstOffset + x * 3 + 2] = b;
                alphaData[alphaOffset + x] = a;
            }
        }

        return (rgbData, alphaData);
    }

    /// <summary>
    /// Decodes 8-bit indexed color BMP data.
    /// </summary>
    private static byte[] DecodeIndexed8Bit(byte[] data, int dataOffset, int width, int height, int rowStride, bool bottomUp)
    {
        byte[] indexedData = new byte[width * height];

        for (int y = 0; y < height; y++)
        {
            int srcRow = bottomUp ? (height - 1 - y) : y;
            int srcOffset = dataOffset + srcRow * rowStride;
            int dstOffset = y * width;

            for (int x = 0; x < width; x++)
            {
                indexedData[dstOffset + x] = data[srcOffset + x];
            }
        }

        return indexedData;
    }

    /// <summary>
    /// Decodes 4-bit indexed color BMP data (2 pixels per byte).
    /// </summary>
    private static byte[] DecodeIndexed4Bit(byte[] data, int dataOffset, int width, int height, int rowStride, bool bottomUp)
    {
        byte[] indexedData = new byte[width * height];

        for (int y = 0; y < height; y++)
        {
            int srcRow = bottomUp ? (height - 1 - y) : y;
            int srcOffset = dataOffset + srcRow * rowStride;
            int dstOffset = y * width;

            for (int x = 0; x < width; x++)
            {
                int byteIndex = x / 2;
                int nibbleIndex = x % 2;

                // High nibble for even pixels, low nibble for odd pixels
                byte pixelByte = data[srcOffset + byteIndex];
                byte index = nibbleIndex == 0 ? (byte)(pixelByte >> 4) : (byte)(pixelByte & 0x0F);

                indexedData[dstOffset + x] = index;
            }
        }

        return indexedData;
    }

    /// <summary>
    /// Decompresses RLE8-encoded BMP data.
    /// RLE8 encoding:
    /// - Encoded mode: count + value (repeat value count times)
    /// - Absolute mode: 0 + count + literal bytes (padded to word boundary)
    /// - End of line: 0 + 0
    /// - End of bitmap: 0 + 1
    /// - Delta: 0 + 2 + dx + dy
    /// </summary>
    private static byte[] DecompressRle8(byte[] data, int dataOffset, int width, int height, bool bottomUp)
    {
        byte[] output = new byte[width * height];
        int srcIndex = dataOffset;
        int x = 0;
        int y = 0;

        while (srcIndex < data.Length && y < height)
        {
            byte count = data[srcIndex++];
            if (srcIndex >= data.Length) break;
            byte value = data[srcIndex++];

            if (count > 0)
            {
                // Encoded mode: repeat value count times
                for (int i = 0; i < count && x < width; i++)
                {
                    int dstRow = bottomUp ? (height - 1 - y) : y;
                    output[dstRow * width + x] = value;
                    x++;
                }
            }
            else
            {
                // Escape code
                if (value == 0)
                {
                    // End of line
                    x = 0;
                    y++;
                }
                else if (value == 1)
                {
                    // End of bitmap
                    break;
                }
                else if (value == 2)
                {
                    // Delta
                    if (srcIndex + 1 >= data.Length) break;
                    byte dx = data[srcIndex++];
                    byte dy = data[srcIndex++];
                    x += dx;
                    y += dy;
                }
                else
                {
                    // Absolute mode: value = count of literal bytes
                    for (int i = 0; i < value && srcIndex < data.Length && x < width; i++)
                    {
                        int dstRow = bottomUp ? (height - 1 - y) : y;
                        output[dstRow * width + x] = data[srcIndex++];
                        x++;
                    }

                    // Pad to word boundary (even byte count)
                    if (value % 2 == 1)
                        srcIndex++;
                }
            }
        }

        return output;
    }

    /// <summary>
    /// Decompresses RLE4-encoded BMP data.
    /// RLE4 encoding is similar to RLE8 but with 4-bit indices (2 pixels per byte).
    /// </summary>
    private static byte[] DecompressRle4(byte[] data, int dataOffset, int width, int height, bool bottomUp)
    {
        byte[] output = new byte[width * height];
        int srcIndex = dataOffset;
        int x = 0;
        int y = 0;

        while (srcIndex < data.Length && y < height)
        {
            byte count = data[srcIndex++];
            if (srcIndex >= data.Length) break;
            byte value = data[srcIndex++];

            if (count > 0)
            {
                // Encoded mode: value contains 2 pixels (high nibble, low nibble)
                byte pixel1 = (byte)(value >> 4);
                byte pixel2 = (byte)(value & 0x0F);

                for (int i = 0; i < count && x < width; i++)
                {
                    int dstRow = bottomUp ? (height - 1 - y) : y;
                    output[dstRow * width + x] = (i % 2 == 0) ? pixel1 : pixel2;
                    x++;
                }
            }
            else
            {
                // Escape code
                if (value == 0)
                {
                    // End of line
                    x = 0;
                    y++;
                }
                else if (value == 1)
                {
                    // End of bitmap
                    break;
                }
                else if (value == 2)
                {
                    // Delta
                    if (srcIndex + 1 >= data.Length) break;
                    byte dx = data[srcIndex++];
                    byte dy = data[srcIndex++];
                    x += dx;
                    y += dy;
                }
                else
                {
                    // Absolute mode: value = count of pixels (not bytes!)
                    int byteCount = (value + 1) / 2; // Number of bytes needed for value pixels

                    for (int i = 0; i < value && srcIndex < data.Length && x < width; i++)
                    {
                        int byteIndex = i / 2;
                        int nibbleIndex = i % 2;

                        if (srcIndex + byteIndex >= data.Length) break;

                        byte pixelByte = data[srcIndex + byteIndex];
                        byte index = nibbleIndex == 0 ? (byte)(pixelByte >> 4) : (byte)(pixelByte & 0x0F);

                        int dstRow = bottomUp ? (height - 1 - y) : y;
                        output[dstRow * width + x] = index;
                        x++;
                    }

                    srcIndex += byteCount;

                    // Pad to word boundary
                    if (byteCount % 2 == 1)
                        srcIndex++;
                }
            }
        }

        return output;
    }
}
