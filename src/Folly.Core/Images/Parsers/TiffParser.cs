namespace Folly.Images.Parsers;

/// <summary>
/// Parser for TIFF (Tagged Image File Format) images.
/// Supports baseline TIFF with uncompressed RGB images.
/// </summary>
public sealed class TiffParser : IImageParser
{
    /// <inheritdoc/>
    public string FormatName => "TIFF";

    /// <inheritdoc/>
    public bool CanParse(byte[] data)
    {
        if (data == null || data.Length < 8)
            return false;

        // Little-endian: "II" (0x49 0x49) + 42 (0x2A 0x00)
        // Big-endian: "MM" (0x4D 0x4D) + 42 (0x00 0x2A)
        return (data[0] == 0x49 && data[1] == 0x49 && data[2] == 0x2A && data[3] == 0x00) ||
               (data[0] == 0x4D && data[1] == 0x4D && data[2] == 0x00 && data[3] == 0x2A);
    }

    /// <inheritdoc/>
    public ImageInfo Parse(byte[] data)
    {
        if (!CanParse(data))
            throw new InvalidDataException("Invalid TIFF file signature");

        // Determine byte order
        bool littleEndian = data[0] == 0x49;

        // Read first IFD offset
        int ifdOffset = ReadInt32(data, 4, littleEndian);

        if (ifdOffset < 8 || ifdOffset >= data.Length)
            throw new InvalidDataException("Invalid TIFF IFD offset");

        // Parse IFD
        var tags = ParseIfd(data, ifdOffset, littleEndian);

        // Extract required tags
        if (!tags.TryGetValue(256, out uint imageWidth) || !tags.TryGetValue(257, out uint imageHeight))
            throw new InvalidDataException("TIFF missing required ImageWidth or ImageLength tags");

        uint compression = tags.TryGetValue(259, out var comp) ? comp : 1; // Default: no compression
        uint photometricInterpretation = tags.TryGetValue(262, out var photo) ? photo : 2; // Default: RGB
        uint samplesPerPixel = tags.TryGetValue(277, out var samples) ? samples : 3; // Default: 3 (RGB)
        uint bitsPerSample = tags.TryGetValue(258, out var bits) ? bits : 8; // Default: 8 bits

        // TODO: Support compressed TIFF (LZW, PackBits, JPEG)
        if (compression != 1)
            throw new NotSupportedException($"TIFF compression type {compression} not supported. Only uncompressed TIFF (compression=1) is supported.");

        // TODO: Support other photometric interpretations (grayscale, palette)
        if (photometricInterpretation != 2)
            throw new NotSupportedException($"TIFF photometric interpretation {photometricInterpretation} not supported. Only RGB (photometric=2) is supported.");

        // Read strip/tile data
        uint[] stripOffsets = tags.TryGetValue(273, out uint stripOffsetSingle)
            ? new[] { stripOffsetSingle }
            : ReadIntArray(data, tags, 273, littleEndian);

        uint[] stripByteCounts = tags.TryGetValue(279, out uint stripByteCountSingle)
            ? new[] { stripByteCountSingle }
            : ReadIntArray(data, tags, 279, littleEndian);

        if (stripOffsets == null || stripByteCounts == null)
            throw new InvalidDataException("TIFF missing strip offsets or byte counts");

        // Calculate total pixel data size
        int totalBytes = (int)stripByteCounts.Sum(x => (long)x);
        byte[] pixelData = new byte[totalBytes];

        // Read all strips
        int destOffset = 0;
        for (int i = 0; i < stripOffsets.Length; i++)
        {
            int stripOffset = (int)stripOffsets[i];
            int stripByteCount = (int)stripByteCounts[i];

            if (stripOffset + stripByteCount > data.Length)
                throw new InvalidDataException($"TIFF strip {i} extends beyond file");

            Array.Copy(data, stripOffset, pixelData, destOffset, stripByteCount);
            destOffset += stripByteCount;
        }

        // Extract DPI
        double horizontalDpi = 0;
        double verticalDpi = 0;

        if (tags.TryGetValue(282, out uint xResValue) && tags.TryGetValue(283, out uint yResValue))
        {
            // X/Y Resolution are stored as RATIONAL (two 32-bit values: numerator/denominator)
            // For simplicity, assume they're stored directly (this works for many TIFFs)
            var xResolution = ReadRational(data, (int)xResValue, littleEndian);
            var yResolution = ReadRational(data, (int)yResValue, littleEndian);

            uint resolutionUnit = tags.TryGetValue(296, out var unit) ? unit : 2; // Default: inch

            if (resolutionUnit == 2) // Inch
            {
                horizontalDpi = xResolution;
                verticalDpi = yResolution;
            }
            else if (resolutionUnit == 3) // Centimeter
            {
                horizontalDpi = xResolution * 2.54;
                verticalDpi = yResolution * 2.54;
            }
        }

        // Compress pixel data for PDF embedding
        byte[] compressedData = CompressData(pixelData);

        string colorSpace = samplesPerPixel == 3 ? "DeviceRGB" : "DeviceGray";
        int colorComponents = (int)samplesPerPixel;

        return new ImageInfo
        {
            Format = "TIFF",
            Width = (int)imageWidth,
            Height = (int)imageHeight,
            HorizontalDpi = horizontalDpi,
            VerticalDpi = verticalDpi,
            BitsPerComponent = (int)bitsPerSample,
            ColorSpace = colorSpace,
            ColorComponents = colorComponents,
            RawData = compressedData
        };
    }

    private static Dictionary<ushort, uint> ParseIfd(byte[] data, int offset, bool littleEndian)
    {
        var tags = new Dictionary<ushort, uint>();

        ushort entryCount = ReadUInt16(data, offset, littleEndian);
        offset += 2;

        for (int i = 0; i < entryCount; i++)
        {
            ushort tag = ReadUInt16(data, offset, littleEndian);
            ushort type = ReadUInt16(data, offset + 2, littleEndian);
            uint count = ReadUInt32(data, offset + 4, littleEndian);
            uint valueOrOffset = ReadUInt32(data, offset + 8, littleEndian);

            // For simple types (SHORT, LONG) with count=1, value is stored directly
            // For other types, valueOrOffset is an offset to the actual data
            tags[tag] = valueOrOffset;

            offset += 12;
        }

        return tags;
    }

    private static uint[] ReadIntArray(byte[] data, Dictionary<ushort, uint> tags, ushort tag, bool littleEndian)
    {
        if (!tags.TryGetValue(tag, out uint offsetOrValue))
            return Array.Empty<uint>();

        // For now, assume simple case: single value stored directly
        // TODO: Handle arrays properly by reading from offset
        return new[] { offsetOrValue };
    }

    private static double ReadRational(byte[] data, int offset, bool littleEndian)
    {
        if (offset + 8 > data.Length)
            return 0;

        uint numerator = ReadUInt32(data, offset, littleEndian);
        uint denominator = ReadUInt32(data, offset + 4, littleEndian);

        return denominator > 0 ? (double)numerator / denominator : 0;
    }

    private static ushort ReadUInt16(byte[] data, int offset, bool littleEndian)
    {
        if (littleEndian)
            return (ushort)(data[offset] | (data[offset + 1] << 8));
        else
            return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static uint ReadUInt32(byte[] data, int offset, bool littleEndian)
    {
        if (littleEndian)
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        else
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
    }

    private static int ReadInt32(byte[] data, int offset, bool littleEndian)
    {
        return (int)ReadUInt32(data, offset, littleEndian);
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
