namespace Folly.Images.Parsers;

/// <summary>
/// Parser for BMP (Windows Bitmap) image format.
/// Supports uncompressed 24-bit RGB and 32-bit RGBA BMPs.
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

        // TODO: Support more BMP variants (8-bit indexed, RLE compression)
        if (compression != 0) // 0 = BI_RGB (uncompressed)
            throw new NotSupportedException($"Compressed BMP (compression method {compression}) not supported. Only uncompressed RGB BMPs are supported.");

        if (bitsPerPixel != 24 && bitsPerPixel != 32)
            throw new NotSupportedException($"BMP with {bitsPerPixel} bits per pixel not supported. Only 24-bit and 32-bit BMPs are supported.");

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

        // Decode pixel data
        // BMP rows are padded to 4-byte boundaries
        int bytesPerPixel = bitsPerPixel / 8;
        int rowStride = ((width * bitsPerPixel + 31) / 32) * 4; // Rounded to nearest 4-byte boundary

        if (dataOffset + rowStride * height > data.Length)
            throw new InvalidDataException("BMP pixel data is truncated");

        // Convert to RGB (or RGBA) with proper row ordering
        // BMP stores pixels bottom-up by default, but we need top-down for PDF
        byte[] rgbData;
        byte[]? alphaData = null;
        string colorSpace;
        int colorComponents;

        if (bitsPerPixel == 32)
        {
            // 32-bit RGBA
            rgbData = new byte[width * height * 3]; // RGB only
            alphaData = new byte[width * height]; // Separate alpha channel
            colorSpace = "DeviceRGB";
            colorComponents = 3;

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
        }
        else // 24-bit RGB
        {
            rgbData = new byte[width * height * 3];
            colorSpace = "DeviceRGB";
            colorComponents = 3;

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
        }

        // Compress RGB data for PDF embedding
        byte[] compressedData = CompressData(rgbData);
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
            AlphaData = compressedAlpha
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
}
