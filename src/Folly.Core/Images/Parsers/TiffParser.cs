namespace Folly.Images.Parsers;

/// <summary>
/// Parser for TIFF (Tagged Image File Format) images.
/// Supports:
/// - Compression: Uncompressed (1), LZW (5), PackBits (32773)
/// - Color modes: RGB (2), Grayscale (0, 1), Palette (3)
/// - Both little-endian (II) and big-endian (MM) byte orders
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

        // Validate compression type
        if (compression != 1 && compression != 5 && compression != 32773)
            throw new NotSupportedException($"TIFF compression type {compression} not supported. Supported: 1 (uncompressed), 5 (LZW), 32773 (PackBits).");

        // Validate photometric interpretation
        if (photometricInterpretation > 3)
            throw new NotSupportedException($"TIFF photometric interpretation {photometricInterpretation} not supported. Supported: 0-1 (grayscale), 2 (RGB), 3 (palette).");

        // Read strip/tile data
        uint[] stripOffsets = tags.TryGetValue(273, out uint stripOffsetSingle)
            ? new[] { stripOffsetSingle }
            : ReadIntArray(data, tags, 273, littleEndian);

        uint[] stripByteCounts = tags.TryGetValue(279, out uint stripByteCountSingle)
            ? new[] { stripByteCountSingle }
            : ReadIntArray(data, tags, 279, littleEndian);

        if (stripOffsets == null || stripByteCounts == null)
            throw new InvalidDataException("TIFF missing strip offsets or byte counts");

        // Read and decompress all strips
        var pixelDataList = new List<byte>();

        for (int i = 0; i < stripOffsets.Length; i++)
        {
            int stripOffset = (int)stripOffsets[i];
            int stripByteCount = (int)stripByteCounts[i];

            if (stripOffset + stripByteCount > data.Length)
                throw new InvalidDataException($"TIFF strip {i} extends beyond file");

            byte[] stripData = new byte[stripByteCount];
            Array.Copy(data, stripOffset, stripData, 0, stripByteCount);

            // Decompress based on compression type
            byte[] decompressedStrip = compression switch
            {
                1 => stripData, // Uncompressed
                5 => DecompressLzw(stripData), // LZW
                32773 => DecompressPackBits(stripData), // PackBits
                _ => throw new NotSupportedException($"Unsupported TIFF compression: {compression}")
            };

            pixelDataList.AddRange(decompressedStrip);
        }

        byte[] pixelData = pixelDataList.ToArray();

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

        // Handle photometric interpretations
        byte[]? palette = null;
        string colorSpace;
        int colorComponents;

        switch (photometricInterpretation)
        {
            case 0: // WhiteIsZero - invert grayscale
                InvertGrayscale(pixelData);
                colorSpace = "DeviceGray";
                colorComponents = 1;
                break;

            case 1: // BlackIsZero - normal grayscale
                colorSpace = "DeviceGray";
                colorComponents = 1;
                break;

            case 2: // RGB
                colorSpace = "DeviceRGB";
                colorComponents = (int)samplesPerPixel;
                break;

            case 3: // Palette color
                palette = ReadColorMap(data, tags, littleEndian);
                if (palette == null)
                    throw new InvalidDataException("TIFF palette image missing ColorMap");
                colorSpace = "Indexed";
                colorComponents = 1;
                break;

            default:
                throw new NotSupportedException($"Unsupported photometric interpretation: {photometricInterpretation}");
        }

        // Compress pixel data for PDF embedding
        byte[] compressedData = CompressData(pixelData);

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
            RawData = compressedData,
            Palette = palette
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

    private static void InvertGrayscale(byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(255 - data[i]);
        }
    }

    private static byte[]? ReadColorMap(byte[] data, Dictionary<ushort, uint> tags, bool littleEndian)
    {
        // ColorMap tag (320) contains RGB values for palette
        if (!tags.TryGetValue(320, out uint colorMapOffset))
            return null;

        // TIFF ColorMap contains 3*2^BitsPerSample 16-bit values (R, G, B)
        // We need to convert to 8-bit RGB triplets for PDF
        int paletteSize = 256; // Assume 8-bit for simplicity
        byte[] palette = new byte[paletteSize * 3];

        for (int i = 0; i < paletteSize; i++)
        {
            int redOffset = (int)colorMapOffset + (i * 2);
            int greenOffset = (int)colorMapOffset + ((paletteSize + i) * 2);
            int blueOffset = (int)colorMapOffset + ((paletteSize * 2 + i) * 2);

            if (blueOffset + 2 > data.Length)
                break;

            // Convert 16-bit values to 8-bit
            ushort r = ReadUInt16(data, redOffset, littleEndian);
            ushort g = ReadUInt16(data, greenOffset, littleEndian);
            ushort b = ReadUInt16(data, blueOffset, littleEndian);

            palette[i * 3] = (byte)(r >> 8);
            palette[i * 3 + 1] = (byte)(g >> 8);
            palette[i * 3 + 2] = (byte)(b >> 8);
        }

        return palette;
    }

    /// <summary>
    /// Decompresses TIFF LZW-compressed data.
    /// LZW compression is the same as GIF LZW.
    /// </summary>
    private static byte[] DecompressLzw(byte[] compressedData)
    {
        const int clearCode = 256;
        const int eoiCode = 257;
        int codeSize = 9;
        int nextCode = 258;

        var output = new List<byte>();
        var table = new Dictionary<int, List<byte>>();

        // Initialize code table
        for (int i = 0; i < 256; i++)
        {
            table[i] = new List<byte> { (byte)i };
        }

        int bitPosition = 0;
        int previousCode = -1;

        while (bitPosition < compressedData.Length * 8)
        {
            int code = ReadBits(compressedData, ref bitPosition, codeSize);

            if (code == eoiCode)
                break;

            if (code == clearCode)
            {
                // Reset table
                table.Clear();
                for (int i = 0; i < 256; i++)
                {
                    table[i] = new List<byte> { (byte)i };
                }
                nextCode = 258;
                codeSize = 9;
                previousCode = -1;
                continue;
            }

            List<byte> sequence;
            if (table.ContainsKey(code))
            {
                sequence = table[code];
            }
            else if (code == nextCode && previousCode >= 0)
            {
                // Special case: code not in table yet
                sequence = new List<byte>(table[previousCode]);
                sequence.Add(sequence[0]);
            }
            else
            {
                throw new InvalidDataException($"Invalid LZW code: {code}");
            }

            output.AddRange(sequence);

            if (previousCode >= 0 && nextCode < 4096)
            {
                var newSequence = new List<byte>(table[previousCode]);
                newSequence.Add(sequence[0]);
                table[nextCode] = newSequence;
                nextCode++;

                // Increase code size when needed
                if (nextCode == 512) codeSize = 10;
                else if (nextCode == 1024) codeSize = 11;
                else if (nextCode == 2048) codeSize = 12;
            }

            previousCode = code;
        }

        return output.ToArray();
    }

    /// <summary>
    /// Decompresses TIFF PackBits-compressed data.
    /// PackBits is a simple RLE compression scheme.
    /// </summary>
    private static byte[] DecompressPackBits(byte[] compressedData)
    {
        var output = new List<byte>();
        int i = 0;

        while (i < compressedData.Length)
        {
            sbyte n = (sbyte)compressedData[i++];

            if (n >= 0)
            {
                // Copy next n+1 bytes literally
                int count = n + 1;
                for (int j = 0; j < count && i < compressedData.Length; j++)
                {
                    output.Add(compressedData[i++]);
                }
            }
            else if (n != -128)
            {
                // Repeat next byte (-n+1) times
                int count = -n + 1;
                if (i < compressedData.Length)
                {
                    byte value = compressedData[i++];
                    for (int j = 0; j < count; j++)
                    {
                        output.Add(value);
                    }
                }
            }
            // n == -128 is a no-op
        }

        return output.ToArray();
    }

    private static int ReadBits(byte[] data, ref int bitPosition, int count)
    {
        int result = 0;
        for (int i = 0; i < count; i++)
        {
            int byteIndex = bitPosition / 8;
            int bitIndex = bitPosition % 8;

            if (byteIndex >= data.Length)
                break;

            int bit = (data[byteIndex] >> (7 - bitIndex)) & 1;
            result = (result << 1) | bit;
            bitPosition++;
        }
        return result;
    }
}
