namespace Folly.Images.Parsers;

/// <summary>
/// Parser for GIF (Graphics Interchange Format) image format.
/// Supports GIF87a and GIF89a with indexed colors and transparency.
/// Handles LZW decompression without external dependencies.
///
/// <para>
/// <strong>LIMITATION: Single-frame extraction only.</strong>
/// Animated GIFs and multi-frame GIFs will be parsed as static images showing only the first frame.
/// This is appropriate for PDF embedding since PDF does not natively support animated images.
/// GIF disposal methods, frame delays, and animation metadata are not processed.
/// </para>
/// </summary>
public sealed class GifParser : IImageParser
{
    /// <inheritdoc/>
    public string FormatName => "GIF";

    /// <inheritdoc/>
    public bool CanParse(byte[] data)
    {
        return data != null && data.Length >= 6 &&
               data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && // "GIF"
               data[3] == 0x38 && (data[4] == 0x37 || data[4] == 0x39) && data[5] == 0x61; // "87a" or "89a"
    }

    /// <inheritdoc/>
    public ImageInfo Parse(byte[] data)
    {
        if (!CanParse(data))
            throw new InvalidDataException("Invalid GIF file signature");

        if (data.Length < 13) // Minimum: 6 (header) + 7 (logical screen descriptor)
            throw new InvalidDataException("GIF file is too small");

        // Parse Logical Screen Descriptor (after 6-byte header)
        int width = ReadUInt16LE(data, 6);
        int height = ReadUInt16LE(data, 8);
        byte flags = data[10];
        // byte bgColorIndex = data[11];
        byte aspectRatio = data[12];

        bool hasGlobalColorTable = (flags & 0x80) != 0;
        int colorTableSize = hasGlobalColorTable ? (2 << (flags & 0x07)) : 0;

        // Parse global color table
        byte[]? globalColorTable = null;
        int offset = 13;

        if (hasGlobalColorTable)
        {
            int globalTableBytes = colorTableSize * 3; // RGB triplets
            if (offset + globalTableBytes > data.Length)
                throw new InvalidDataException("GIF global color table is truncated");

            globalColorTable = new byte[globalTableBytes];
            Array.Copy(data, offset, globalColorTable, 0, globalTableBytes);
            offset += globalTableBytes;
        }

        // IMPLEMENTATION NOTE: Single-frame extraction
        // Multi-frame GIFs (including animations) contain multiple image descriptors (0x2C blocks).
        // This parser extracts only the FIRST frame for PDF embedding.
        // Rationale: PDF format does not support animated images, so extracting the first frame
        // provides a static representation suitable for document embedding.
        // Animation metadata (disposal methods, delays), subsequent frames, and loop counts are ignored.
        int transparentColorIndex = -1;

        // Scan for blocks until we find an image descriptor or trailer
        byte[]? localColorTable = null;
        int imageLeft = 0, imageTop = 0, imageWidth = 0, imageHeight = 0;
        bool interlaced = false;

        while (offset < data.Length)
        {
            byte blockType = data[offset];

            if (blockType == 0x21) // Extension block
            {
                offset++;
                if (offset >= data.Length) break;

                byte label = data[offset];
                offset++;

                if (label == 0xF9) // Graphic Control Extension
                {
                    // Parse transparency info
                    if (offset + 6 <= data.Length)
                    {
                        byte gceFlags = data[offset + 1];
                        bool hasTransparency = (gceFlags & 0x01) != 0;

                        if (hasTransparency)
                        {
                            transparentColorIndex = data[offset + 4];
                        }

                        offset += 6; // Skip GCE (1 block size + 4 data + 1 terminator)
                    }
                }
                else
                {
                    // Skip other extension blocks
                    offset = SkipDataSubBlocks(data, offset);
                }
            }
            else if (blockType == 0x2C) // Image Descriptor
            {
                if (offset + 10 > data.Length)
                    throw new InvalidDataException("GIF image descriptor is truncated");

                imageLeft = ReadUInt16LE(data, offset + 1);
                imageTop = ReadUInt16LE(data, offset + 3);
                imageWidth = ReadUInt16LE(data, offset + 5);
                imageHeight = ReadUInt16LE(data, offset + 7);
                byte imageFlags = data[offset + 9];
                offset += 10;

                bool hasLocalColorTable = (imageFlags & 0x80) != 0;
                interlaced = (imageFlags & 0x40) != 0;
                int localColorTableSize = hasLocalColorTable ? (2 << (imageFlags & 0x07)) : 0;

                if (hasLocalColorTable)
                {
                    int localTableBytes = localColorTableSize * 3;
                    if (offset + localTableBytes > data.Length)
                        throw new InvalidDataException("GIF local color table is truncated");

                    localColorTable = new byte[localTableBytes];
                    Array.Copy(data, offset, localColorTable, 0, localTableBytes);
                    offset += localTableBytes;
                }

                // Found the first image descriptor - extract this frame and stop scanning.
                // Multi-frame/animated GIFs will only show their first frame in the PDF.
                break;
            }
            else if (blockType == 0x3B) // Trailer
            {
                break;
            }
            else
            {
                // Unknown block, skip
                offset++;
            }
        }

        if (imageWidth == 0 || imageHeight == 0)
            throw new InvalidDataException("No valid image data found in GIF");

        // Decode LZW compressed image data
        if (offset >= data.Length)
            throw new InvalidDataException("GIF image data is missing");

        byte lzwMinimumCodeSize = data[offset];
        offset++;

        byte[] compressedData = ReadDataSubBlocks(data, ref offset);
        byte[] indexedPixels = DecodeLzw(compressedData, lzwMinimumCodeSize, imageWidth * imageHeight);

        // Deinterlace if necessary
        if (interlaced)
        {
            indexedPixels = DeinterlaceGif(indexedPixels, imageWidth, imageHeight);
        }

        // Select color table (local takes precedence over global)
        byte[]? colorTable = localColorTable ?? globalColorTable;

        if (colorTable == null)
            throw new InvalidDataException("GIF has no color table");

        // Convert indexed pixels to RGB
        byte[] rgbData = new byte[imageWidth * imageHeight * 3];
        byte[]? alphaData = null;

        if (transparentColorIndex >= 0)
        {
            alphaData = new byte[imageWidth * imageHeight];
        }

        for (int i = 0; i < indexedPixels.Length; i++)
        {
            int colorIndex = indexedPixels[i];
            int paletteOffset = colorIndex * 3;

            if (paletteOffset + 2 < colorTable.Length)
            {
                rgbData[i * 3] = colorTable[paletteOffset];
                rgbData[i * 3 + 1] = colorTable[paletteOffset + 1];
                rgbData[i * 3 + 2] = colorTable[paletteOffset + 2];

                if (alphaData != null)
                {
                    alphaData[i] = (byte)(colorIndex == transparentColorIndex ? 0x00 : 0xFF);
                }
            }
        }

        // Compress RGB data for PDF embedding
        byte[] compressedRgb = CompressData(rgbData);
        byte[]? compressedAlpha = alphaData != null ? CompressData(alphaData) : null;

        // Calculate DPI from aspect ratio (if present)
        double horizontalDpi = 0;
        double verticalDpi = 0;

        if (aspectRatio != 0)
        {
            // Aspect ratio = (Pixel Aspect Ratio + 15) / 64
            double pixelAspectRatio = (aspectRatio + 15.0) / 64.0;
            // For simplicity, assume 72 DPI and adjust vertical based on aspect ratio
            horizontalDpi = 72;
            verticalDpi = 72 / pixelAspectRatio;
        }

        return new ImageInfo
        {
            Format = "GIF",
            Width = imageWidth,
            Height = imageHeight,
            HorizontalDpi = horizontalDpi,
            VerticalDpi = verticalDpi,
            BitsPerComponent = 8,
            ColorSpace = "DeviceRGB",
            ColorComponents = 3,
            RawData = compressedRgb,
            AlphaData = compressedAlpha,
            Palette = colorTable
        };
    }

    private static int SkipDataSubBlocks(byte[] data, int offset)
    {
        while (offset < data.Length)
        {
            byte blockSize = data[offset];
            offset++;

            if (blockSize == 0) break; // Block terminator

            offset += blockSize;
            if (offset > data.Length) break;
        }

        return offset;
    }

    private static byte[] ReadDataSubBlocks(byte[] data, ref int offset)
    {
        var blocks = new List<byte>();

        while (offset < data.Length)
        {
            byte blockSize = data[offset];
            offset++;

            if (blockSize == 0) break; // Block terminator

            if (offset + blockSize > data.Length)
                throw new InvalidDataException("GIF data sub-block extends beyond file");

            for (int i = 0; i < blockSize; i++)
            {
                blocks.Add(data[offset + i]);
            }

            offset += blockSize;
        }

        return blocks.ToArray();
    }

    /// <summary>
    /// Deinterlaces GIF image data using the 4-pass interlace scheme.
    /// Pass 1: Every 8th row, starting at row 0
    /// Pass 2: Every 8th row, starting at row 4
    /// Pass 3: Every 4th row, starting at row 2
    /// Pass 4: Every 2nd row, starting at row 1
    /// </summary>
    private static byte[] DeinterlaceGif(byte[] interlacedData, int width, int height)
    {
        byte[] deinterlaced = new byte[width * height];
        int sourceIndex = 0;

        // Pass 1: Every 8th row, starting at row 0
        for (int row = 0; row < height; row += 8)
        {
            for (int col = 0; col < width; col++)
            {
                if (sourceIndex < interlacedData.Length)
                {
                    deinterlaced[row * width + col] = interlacedData[sourceIndex++];
                }
            }
        }

        // Pass 2: Every 8th row, starting at row 4
        for (int row = 4; row < height; row += 8)
        {
            for (int col = 0; col < width; col++)
            {
                if (sourceIndex < interlacedData.Length)
                {
                    deinterlaced[row * width + col] = interlacedData[sourceIndex++];
                }
            }
        }

        // Pass 3: Every 4th row, starting at row 2
        for (int row = 2; row < height; row += 4)
        {
            for (int col = 0; col < width; col++)
            {
                if (sourceIndex < interlacedData.Length)
                {
                    deinterlaced[row * width + col] = interlacedData[sourceIndex++];
                }
            }
        }

        // Pass 4: Every 2nd row, starting at row 1
        for (int row = 1; row < height; row += 2)
        {
            for (int col = 0; col < width; col++)
            {
                if (sourceIndex < interlacedData.Length)
                {
                    deinterlaced[row * width + col] = interlacedData[sourceIndex++];
                }
            }
        }

        return deinterlaced;
    }

    // Simple LZW decoder for GIF images (zero dependencies)
    private static byte[] DecodeLzw(byte[] compressedData, int minimumCodeSize, int expectedPixels)
    {
        int clearCode = 1 << minimumCodeSize;
        int eoiCode = clearCode + 1;
        int codeSize = minimumCodeSize + 1;
        int nextCode = eoiCode + 1;

        var output = new List<byte>(expectedPixels);
        var table = new Dictionary<int, List<byte>>();

        // Initialize code table
        for (int i = 0; i < clearCode; i++)
        {
            table[i] = new List<byte> { (byte)i };
        }

        int bitPosition = 0;
        int previousCode = -1;

        while (bitPosition < compressedData.Length * 8)
        {
            int code = ReadBits(compressedData, ref bitPosition, codeSize);

            if (code == clearCode)
            {
                // Reset table
                table.Clear();
                for (int i = 0; i < clearCode; i++)
                {
                    table[i] = new List<byte> { (byte)i };
                }
                nextCode = eoiCode + 1;
                codeSize = minimumCodeSize + 1;
                previousCode = -1;
                continue;
            }

            if (code == eoiCode)
            {
                break; // End of information
            }

            List<byte> sequence;

            if (table.ContainsKey(code))
            {
                sequence = table[code];
            }
            else if (code == nextCode && previousCode >= 0 && table.ContainsKey(previousCode))
            {
                // Special case: code not in table yet
                sequence = new List<byte>(table[previousCode]);
                sequence.Add(sequence[0]);
            }
            else
            {
                // Invalid code
                break;
            }

            output.AddRange(sequence);

            if (previousCode >= 0 && nextCode < 4096 && table.ContainsKey(previousCode))
            {
                var newSequence = new List<byte>(table[previousCode]);
                newSequence.Add(sequence[0]);
                table[nextCode] = newSequence;
                nextCode++;

                // Increase code size when table is full at current size
                if (nextCode >= (1 << codeSize) && codeSize < 12)
                {
                    codeSize++;
                }
            }

            previousCode = code;
        }

        return output.ToArray();
    }

    private static int ReadBits(byte[] data, ref int bitPosition, int bitsToRead)
    {
        int result = 0;

        for (int i = 0; i < bitsToRead; i++)
        {
            int byteIndex = bitPosition / 8;
            int bitIndex = bitPosition % 8;

            if (byteIndex >= data.Length)
                break;

            int bit = (data[byteIndex] >> bitIndex) & 1;
            result |= bit << i;
            bitPosition++;
        }

        return result;
    }

    private static ushort ReadUInt16LE(byte[] data, int offset)
    {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
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
