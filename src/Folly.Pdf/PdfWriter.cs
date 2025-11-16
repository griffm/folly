using System.IO.Compression;

namespace Folly.Pdf;

/// <summary>
/// Low-level PDF writer for creating PDF 1.7 documents.
/// </summary>
internal sealed class PdfWriter : IDisposable
{
    private readonly Stream _output;
    private readonly StreamWriter _writer;
    private readonly PdfOptions _options;
    private readonly List<long> _objectOffsets = new();
    private int _nextObjectId = 1;
    private long _position;
    private bool _disposed;
    private int? _infoObjectId;

    // Character remapping for font subsetting (maps characters to byte codes 0-255)
    // NOTE: This is legacy code for Type1 fonts. For TrueType fonts, we use glyph ID mapping instead.
    private readonly Dictionary<string, Dictionary<char, byte>> _characterRemapping = new();

    // Character to glyph ID mapping for TrueType fonts with Type 0/Identity-H encoding
    // Maps character to glyph ID (used for 2-byte character codes in PDF content streams)
    private readonly Dictionary<string, Dictionary<char, ushort>> _characterToGlyphId = new();

    // Font data cache for improved performance when the same fonts are used multiple times
    private readonly Fonts.FontDataCache? _fontDataCache;

    // Security: Maximum allowed PNG chunk size (10MB) to prevent integer overflow attacks
    private const int MAX_PNG_CHUNK_SIZE = 10 * 1024 * 1024;

    /// <summary>
    /// Gets the character remapping for a specific font (Type1 fonts only).
    /// </summary>
    public Dictionary<char, byte>? GetCharacterRemapping(string fontName)
    {
        return _characterRemapping.TryGetValue(fontName, out var mapping) ? mapping : null;
    }

    /// <summary>
    /// Gets the character to glyph ID mapping for a specific font (TrueType fonts with Identity-H).
    /// </summary>
    public Dictionary<char, ushort>? GetCharacterToGlyphId(string fontName)
    {
        return _characterToGlyphId.TryGetValue(fontName, out var mapping) ? mapping : null;
    }

    public PdfWriter(Stream output, PdfOptions options)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Initialize font data cache if configured
        var cacheOptions = options.FontCacheOptions ?? new FontCacheOptions();
        _fontDataCache = cacheOptions.MaxFontDataCacheSize > 0
            ? new Fonts.FontDataCache(cacheOptions.MaxFontDataCacheSize)
            : null;

        _writer = new StreamWriter(_output, Encoding.ASCII, leaveOpen: true)
        {
            NewLine = "\n",
            AutoFlush = true
        };
    }

    /// <summary>
    /// Writes the PDF header.
    /// </summary>
    public void WriteHeader(string version)
    {
        WriteLine($"%PDF-{version}");
        // Write binary comment to mark as binary PDF
        WriteLine("%âãÏÓ");
    }

    /// <summary>
    /// Writes the document catalog and returns its object ID.
    /// Also reserves object ID 2 for the pages tree.
    /// This MUST be called before any other objects are created to ensure correct object numbering.
    /// </summary>
    public int WriteCatalog(int pageCount, Dom.FoBookmarkTree? bookmarkTree = null, int structTreeRootId = 0, int xmpMetadataId = 0, int outputIntentId = 0)
    {
        // Reserve object 1 for catalog
        var catalogId = 1;
        _objectOffsets.Add(0);  // Placeholder offset for catalog (object 1)

        // Reserve object 2 for pages tree (will be written later)
        _objectOffsets.Add(0);  // Placeholder offset for pages (object 2)
        _nextObjectId = 3;  // Next objects start at 3

        // Write outline (bookmarks) if present (gets IDs 3, 4, 5, etc.)
        int? outlineId = null;
        if (bookmarkTree != null && bookmarkTree.Bookmarks.Count > 0)
        {
            outlineId = WriteOutline(bookmarkTree);
        }

        // Now write the catalog at object 1
        _objectOffsets[0] = _position;  // Update catalog position
        WriteLine("1 0 obj");
        WriteLine("<<");
        WriteLine("  /Type /Catalog");
        WriteLine($"  /Pages 2 0 R");  // Pages tree will be object 2

        // Add outline reference if bookmarks exist
        if (outlineId.HasValue)
        {
            WriteLine($"  /Outlines {outlineId.Value} 0 R");
        }

        // Add structure tree root reference if tagged PDF is enabled
        if (structTreeRootId > 0)
        {
            WriteLine($"  /StructTreeRoot {structTreeRootId} 0 R");
            WriteLine("  /MarkInfo << /Marked true >>");
        }

        // Add XMP metadata reference for PDF/A compliance
        if (xmpMetadataId > 0)
        {
            WriteLine($"  /Metadata {xmpMetadataId} 0 R");
        }

        // Add OutputIntents for PDF/A compliance
        if (outputIntentId > 0)
        {
            WriteLine($"  /OutputIntents [ {outputIntentId} 0 R ]");
        }

        WriteLine(">>");
        WriteLine("endobj");

        return catalogId;
    }

    /// <summary>
    /// Updates the catalog object with additional references (structure tree, metadata, etc.)
    /// that weren't known when the catalog was first written.
    /// This rewrites object 1 at the current position.
    /// </summary>
    public void UpdateCatalog(int catalogId, Dom.FoBookmarkTree? bookmarkTree, int structTreeRootId, int xmpMetadataId, int outputIntentId)
    {
        // Write outline (bookmarks) if present and not already written
        int? outlineId = null;
        if (bookmarkTree != null && bookmarkTree.Bookmarks.Count > 0)
        {
            // Check if outline was already written during initial catalog creation
            // If _nextObjectId == 3, no outline was written yet
            // Otherwise, outline was written and we need to get its ID
            // For simplicity, we'll write a new outline here (this is safe because bookmarks are small)
            outlineId = WriteOutline(bookmarkTree);
        }

        // Update catalog at object 1
        _objectOffsets[0] = _position;  // Update catalog position
        WriteLine("1 0 obj");
        WriteLine("<<");
        WriteLine("  /Type /Catalog");
        WriteLine($"  /Pages 2 0 R");  // Pages tree will be object 2

        // Add outline reference if bookmarks exist
        if (outlineId.HasValue)
        {
            WriteLine($"  /Outlines {outlineId.Value} 0 R");
        }

        // Add structure tree root reference if tagged PDF is enabled
        if (structTreeRootId > 0)
        {
            WriteLine($"  /StructTreeRoot {structTreeRootId} 0 R");
            WriteLine("  /MarkInfo << /Marked true >>");
        }

        // Add XMP metadata reference for PDF/A compliance
        if (xmpMetadataId > 0)
        {
            WriteLine($"  /Metadata {xmpMetadataId} 0 R");
        }

        // Add OutputIntents for PDF/A compliance
        if (outputIntentId > 0)
        {
            WriteLine($"  /OutputIntents [ {outputIntentId} 0 R ]");
        }

        WriteLine(">>");
        WriteLine("endobj");
    }

    /// <summary>
    /// Writes image XObjects and returns a mapping of image sources to object IDs.
    /// </summary>
    public Dictionary<string, int> WriteImages(Dictionary<string, (byte[] Data, string Format, int Width, int Height)> images)
    {
        var imageIds = new Dictionary<string, int>();

        foreach (var kvp in images)
        {
            var source = kvp.Key;
            var (data, format, width, height) = kvp.Value;

            int imageId = -1;

            try
            {
                switch (format)
                {
                    case "JPEG":
                        imageId = WriteJpegXObject(data, width, height, source);
                        break;

                    case "PNG":
                        imageId = WritePngXObject(data, width, height, source);
                        break;

                    case "BMP":
                    case "GIF":
                    case "TIFF":
                        // Use new image parsers for BMP, GIF, TIFF
                        imageId = WriteDecodedImageXObject(data, format, width, height, source);
                        break;

                    default:
                        // Unknown format, skip
                        continue;
                }
            }
            catch (NotSupportedException)
            {
                // Re-throw validation exceptions (interlaced images, unsupported formats, etc.)
                throw;
            }
            catch (InvalidDataException)
            {
                // Re-throw validation exceptions (corrupted files, invalid formats, etc.)
                throw;
            }
            catch (ImageDecodingException)
            {
                // Re-throw image decoding exceptions
                throw;
            }
            catch (Exception ex)
            {
                // Wrap other unexpected exceptions in ImageDecodingException
                throw new ImageDecodingException(
                    $"Failed to process image: {ex.Message}",
                    imagePath: source,
                    imageFormat: format,
                    failureReason: ex.GetType().Name,
                    innerException: ex);
            }

            if (imageId > 0)
            {
                imageIds[source] = imageId;
            }
        }

        return imageIds;
    }

    private int WriteJpegXObject(byte[] jpegData, int width, int height, string? imagePath = null)
    {
        // Parse JPEG metadata using the full parser to get ICC profile if present
        Folly.Images.ImageInfo? imageInfo = null;
        try
        {
            var parser = new Folly.Images.Parsers.JpegParser();
            imageInfo = parser.Parse(jpegData);
        }
        catch
        {
            // Parser failed, fall back to simple metadata extraction
        }

        string colorSpace;
        int bitsPerComponent;
        int? iccProfileId = null;

        if (imageInfo != null)
        {
            width = imageInfo.Width;
            height = imageInfo.Height;
            bitsPerComponent = imageInfo.BitsPerComponent;
            colorSpace = imageInfo.ColorSpace;

            // Embed ICC profile if present
            if (imageInfo.IccProfile != null && imageInfo.IccProfile.Length > 0)
            {
                iccProfileId = WriteIccProfile(imageInfo.IccProfile, imageInfo.ColorComponents);
                // Use ICCBased color space instead of Device* when ICC profile is present
                // Keep DeviceCMYK for now for simplicity
            }
        }
        else
        {
            // Fallback to simple parsing
            var (parsedWidth, parsedHeight, parsedBits, parsedColorSpace) = ParseJpegMetadata(jpegData);
            if (parsedWidth > 0) width = parsedWidth;
            if (parsedHeight > 0) height = parsedHeight;
            bitsPerComponent = parsedBits;
            colorSpace = parsedColorSpace;
        }

        var imageId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /XObject");
        WriteLine("  /Subtype /Image");
        WriteLine($"  /Width {width}");
        WriteLine($"  /Height {height}");

        // Use ICC-based color space if ICC profile is embedded
        if (iccProfileId.HasValue)
        {
            WriteLine($"  /ColorSpace [/ICCBased {iccProfileId.Value} 0 R]");
        }
        else
        {
            WriteLine($"  /ColorSpace /{colorSpace}");
        }

        WriteLine($"  /BitsPerComponent {bitsPerComponent}");
        WriteLine("  /Filter /DCTDecode");
        WriteLine($"  /Length {jpegData.Length}");
        WriteLine(">>");
        WriteLine("stream");

        // Write raw JPEG data
        _output.Write(jpegData, 0, jpegData.Length);
        _position += jpegData.Length;

        WriteLine("");
        WriteLine("endstream");
        EndObject();

        return imageId;
    }

    private int WritePngXObject(byte[] pngData, int width, int height, string? imagePath = null)
    {
        // Decode PNG and write as FlateDecode image with PNG predictors
        var (compressedData, bitsPerComponent, colorSpace, colorComponents, palette, transparency, alphaData) = DecodePng(pngData, width, height, imagePath);

        // Create SMask if alpha channel is present
        int? smaskId = null;
        if (alphaData != null)
        {
            smaskId = WriteSMask(alphaData, width, height, bitsPerComponent);
        }

        var imageId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /XObject");
        WriteLine("  /Subtype /Image");
        WriteLine($"  /Width {width}");
        WriteLine($"  /Height {height}");

        // Handle indexed color space with palette
        if (palette != null)
        {
            // PDF Indexed ColorSpace: [/Indexed baseColorSpace hival lookup]
            // hival is the maximum palette index (paletteSize - 1)
            int paletteSize = palette.Length / 3; // RGB triplets
            int hival = paletteSize - 1;

            // Write color space as an array
            Write("  /ColorSpace [/Indexed /DeviceRGB ");
            Write(hival.ToString());
            Write(" <");
            // Write palette as hex string
            foreach (byte b in palette)
            {
                Write(b.ToString("X2"));
            }
            WriteLine(">]");
        }
        else
        {
            WriteLine($"  /ColorSpace /{colorSpace}");
        }

        WriteLine($"  /BitsPerComponent {bitsPerComponent}");

        // Add SMask reference if alpha channel is present
        if (smaskId.HasValue)
        {
            WriteLine($"  /SMask {smaskId.Value} 0 R");
        }

        // Handle tRNS transparency (simple color masking for RGB/Grayscale)
        if (transparency != null && palette == null && !smaskId.HasValue)
        {
            // For RGB: tRNS contains 6 bytes (R R G G B B as 16-bit values)
            // For Grayscale: tRNS contains 2 bytes (gray value as 16-bit)
            if (colorSpace == "DeviceRGB" && transparency.Length == 6)
            {
                // RGB transparent color
                int r = (transparency[0] << 8) | transparency[1];
                int g = (transparency[2] << 8) | transparency[3];
                int b = (transparency[4] << 8) | transparency[5];
                WriteLine($"  /Mask [{r} {r} {g} {g} {b} {b}]");
            }
            else if (colorSpace == "DeviceGray" && transparency.Length == 2)
            {
                // Grayscale transparent value
                int gray = (transparency[0] << 8) | transparency[1];
                WriteLine($"  /Mask [{gray} {gray}]");
            }
            // TODO: Handle indexed color with tRNS (requires SMask or palette expansion)
        }

        WriteLine("  /Filter /FlateDecode");

        // Only use PNG predictors if we haven't already unfiltered (i.e., no alpha channel)
        if (!smaskId.HasValue)
        {
            WriteLine("  /DecodeParms <<");
            WriteLine("    /Predictor 15");
            WriteLine($"    /Colors {colorComponents}");
            WriteLine($"    /BitsPerComponent {bitsPerComponent}");
            WriteLine($"    /Columns {width}");
            WriteLine("  >>");
        }

        WriteLine($"  /Length {compressedData.Length}");
        WriteLine(">>");
        WriteLine("stream");

        // Write compressed PNG IDAT data
        _output.Write(compressedData, 0, compressedData.Length);
        _position += compressedData.Length;

        WriteLine("");
        WriteLine("endstream");
        EndObject();

        return imageId;
    }

    /// <summary>
    /// Writes an image XObject for decoded image formats (BMP, GIF, TIFF).
    /// Uses the new image parser infrastructure.
    /// </summary>
    private int WriteDecodedImageXObject(byte[] imageData, string format, int width, int height, string? imagePath = null)
    {
        try
        {
            Folly.Images.ImageInfo? imageInfo = null;

            // Parse image using appropriate parser
            switch (format)
            {
                case "BMP":
                    {
                        var parser = new Folly.Images.Parsers.BmpParser();
                        imageInfo = parser.Parse(imageData);
                        break;
                    }

                case "GIF":
                    {
                        var parser = new Folly.Images.Parsers.GifParser();
                        imageInfo = parser.Parse(imageData);
                        break;
                    }

                case "TIFF":
                    {
                        var parser = new Folly.Images.Parsers.TiffParser();
                        imageInfo = parser.Parse(imageData);
                        break;
                    }

                default:
                    throw new NotSupportedException($"Unsupported image format: {format}");
            }

            if (imageInfo == null || imageInfo.RawData == null)
                throw new InvalidDataException($"Failed to parse {format} image");

            // Create SMask if alpha channel is present
            int? smaskId = null;
            if (imageInfo.AlphaData != null)
            {
                smaskId = WriteSMask(imageInfo.AlphaData, imageInfo.Width, imageInfo.Height, imageInfo.BitsPerComponent);
            }

            // Write main image XObject
            var imageId = BeginObject();
            WriteLine("<<");
            WriteLine("  /Type /XObject");
            WriteLine("  /Subtype /Image");
            WriteLine($"  /Width {imageInfo.Width}");
            WriteLine($"  /Height {imageInfo.Height}");

            // Handle indexed color space with palette (for GIF)
            if (imageInfo.Palette != null)
            {
                // PDF Indexed ColorSpace: [/Indexed baseColorSpace hival lookup]
                int paletteSize = imageInfo.Palette.Length / 3; // RGB triplets
                int hival = paletteSize - 1;

                Write("  /ColorSpace [/Indexed /DeviceRGB ");
                Write(hival.ToString());
                Write(" <");
                foreach (byte b in imageInfo.Palette)
                {
                    Write(b.ToString("X2"));
                }
                WriteLine(">]");
            }
            else
            {
                WriteLine($"  /ColorSpace /{imageInfo.ColorSpace}");
            }

            WriteLine($"  /BitsPerComponent {imageInfo.BitsPerComponent}");

            // Add SMask reference if alpha channel present
            if (smaskId.HasValue)
            {
                WriteLine($"  /SMask {smaskId.Value} 0 R");
            }

            // Transparency for GIF (indexed color with transparent index)
            if (imageInfo.Transparency != null && imageInfo.ColorSpace != "DeviceRGB")
            {
                // For grayscale transparency
                if (imageInfo.Transparency.Length == 2)
                {
                    int gray = (imageInfo.Transparency[0] << 8) | imageInfo.Transparency[1];
                    WriteLine($"  /Mask [{gray} {gray}]");
                }
                // For RGB transparency
                else if (imageInfo.Transparency.Length == 6)
                {
                    int r1 = (imageInfo.Transparency[0] << 8) | imageInfo.Transparency[1];
                    int g1 = (imageInfo.Transparency[2] << 8) | imageInfo.Transparency[3];
                    int b1 = (imageInfo.Transparency[4] << 8) | imageInfo.Transparency[5];
                    WriteLine($"  /Mask [{r1} {r1} {g1} {g1} {b1} {b1}]");
                }
            }

            WriteLine("  /Filter /FlateDecode");
            WriteLine($"  /Length {imageInfo.RawData.Length}");
            WriteLine(">>");
            WriteLine("stream");

            // Write compressed image data
            _output.Write(imageInfo.RawData, 0, imageInfo.RawData.Length);
            _position += imageInfo.RawData.Length;

            WriteLine("");
            WriteLine("endstream");
            EndObject();

            return imageId;
        }
        catch (Exception ex)
        {
            // Log error and return -1 to indicate failure
            // In production, you might want to log this
            Console.Error.WriteLine($"Failed to write {format} image: {ex.Message}");
            return -1;
        }
    }

    /// <summary>
    /// Writes an SMask (Soft Mask) XObject for alpha channel data.
    /// </summary>
    private int WriteSMask(byte[] alphaData, int width, int height, int bitsPerComponent)
    {
        var smaskId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /XObject");
        WriteLine("  /Subtype /Image");
        WriteLine($"  /Width {width}");
        WriteLine($"  /Height {height}");
        WriteLine("  /ColorSpace /DeviceGray"); // Alpha is always grayscale
        WriteLine($"  /BitsPerComponent {bitsPerComponent}");
        WriteLine("  /Filter /FlateDecode");
        WriteLine($"  /Length {alphaData.Length}");
        WriteLine(">>");
        WriteLine("stream");

        // Write compressed alpha data (already unfiltered)
        _output.Write(alphaData, 0, alphaData.Length);
        _position += alphaData.Length;

        WriteLine("");
        WriteLine("endstream");
        EndObject();

        return smaskId;
    }

    /// <summary>
    /// Writes an ICC color profile as a PDF ICCBased color space stream.
    /// </summary>
    private int WriteIccProfile(byte[] iccProfileData, int numComponents)
    {
        // Compress ICC profile data
        byte[] compressedProfile;
        using (var output = new MemoryStream())
        {
            using (var compressor = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                compressor.Write(iccProfileData, 0, iccProfileData.Length);
            }
            compressedProfile = output.ToArray();
        }

        // Write ICC profile stream
        var profileId = BeginObject();
        WriteLine("<<");
        WriteLine($"  /N {numComponents}"); // Number of color components
        WriteLine("  /Filter /FlateDecode");
        WriteLine($"  /Length {compressedProfile.Length}");
        WriteLine(">>");
        WriteLine("stream");

        _output.Write(compressedProfile, 0, compressedProfile.Length);
        _position += compressedProfile.Length;

        WriteLine("");
        WriteLine("endstream");
        EndObject();

        return profileId;
    }

    private (byte[] CompressedData, int BitsPerComponent, string ColorSpace, int ColorComponents, byte[]? Palette, byte[]? Transparency, byte[]? AlphaData) DecodePng(byte[] pngData, int width, int height, string? imagePath = null)
    {
        // Simplified PNG decoder - extract IDAT chunks and parse IHDR for metadata
        // For production use, consider using a PNG library like SixLabors.ImageSharp

        try
        {
            // Validate PNG signature: 89 50 4E 47 0D 0A 1A 0A
            if (pngData.Length < 8 ||
                pngData[0] != 0x89 || pngData[1] != 0x50 || pngData[2] != 0x4E || pngData[3] != 0x47 ||
                pngData[4] != 0x0D || pngData[5] != 0x0A || pngData[6] != 0x1A || pngData[7] != 0x0A)
            {
                throw new InvalidDataException("Invalid PNG signature. File is not a valid PNG image.");
            }

            // Parse PNG chunks to find IHDR, PLTE, tRNS, pHYs, and IDAT
            var idatData = new List<byte>();
            byte[]? palette = null;
            byte[]? transparency = null; // tRNS chunk data
            int pixelsPerUnitX = 0;
            int pixelsPerUnitY = 0;
            byte unitSpecifier = 0; // 0 = unknown, 1 = meter
            int offset = 8; // Skip PNG signature

            // Default values (will be overridden by IHDR)
            int bitDepth = 8;
            int colorType = 2; // RGB
            int interlaceMethod = 0; // 0 = no interlace, 1 = Adam7

            while (offset < pngData.Length)
            {
                if (offset + 8 > pngData.Length) break;

                int chunkLength = (pngData[offset] << 24) | (pngData[offset + 1] << 16) |
                                 (pngData[offset + 2] << 8) | pngData[offset + 3];

                // Security: Validate chunk length to prevent integer overflow attacks
                if (chunkLength < 0 || chunkLength > MAX_PNG_CHUNK_SIZE)
                {
                    // Invalid or suspiciously large chunk - abort processing
                    break;
                }

                // Security: Ensure chunk data doesn't exceed buffer bounds
                if (offset + 12 + chunkLength > pngData.Length)
                {
                    // Chunk extends beyond buffer - abort processing
                    break;
                }

                string chunkType = Encoding.ASCII.GetString(pngData, offset + 4, 4);

                if (chunkType == "IHDR" && chunkLength >= 13)
                {
                    // Parse IHDR: width(4) height(4) bitdepth(1) colortype(1) compression(1) filter(1) interlace(1)
                    bitDepth = pngData[offset + 8 + 8];
                    colorType = pngData[offset + 8 + 9];
                    interlaceMethod = pngData[offset + 8 + 12];

                    // Validate interlace immediately after parsing IHDR
                    if (interlaceMethod != 0)
                    {
                        throw new NotSupportedException($"Interlaced PNG images (Adam7) are not supported. Please convert to non-interlaced format.");
                    }
                }
                else if (chunkType == "PLTE")
                {
                    // Extract palette data (RGB triplets)
                    palette = new byte[chunkLength];
                    Array.Copy(pngData, offset + 8, palette, 0, chunkLength);
                }
                else if (chunkType == "tRNS")
                {
                    // Extract transparency data
                    // For indexed: alpha values for palette entries
                    // For RGB: transparent color (6 bytes: R R G G B B as 16-bit values)
                    // For grayscale: transparent gray value (2 bytes as 16-bit value)
                    transparency = new byte[chunkLength];
                    Array.Copy(pngData, offset + 8, transparency, 0, chunkLength);
                }
                else if (chunkType == "pHYs" && chunkLength == 9)
                {
                    // Extract physical dimensions
                    // Format: pixelsPerUnitX(4) pixelsPerUnitY(4) unitSpecifier(1)
                    pixelsPerUnitX = (pngData[offset + 8] << 24) | (pngData[offset + 9] << 16) |
                                     (pngData[offset + 10] << 8) | pngData[offset + 11];
                    pixelsPerUnitY = (pngData[offset + 12] << 24) | (pngData[offset + 13] << 16) |
                                     (pngData[offset + 14] << 8) | pngData[offset + 15];
                    unitSpecifier = pngData[offset + 16];
                }
                else if (chunkType == "IDAT")
                {
                    // Collect IDAT data (compressed with PNG filters) - use AddRange for performance
                    // AddRange is much faster than byte-by-byte copying
                    byte[] chunk = new byte[chunkLength];
                    Array.Copy(pngData, offset + 8, chunk, 0, chunkLength);
                    idatData.AddRange(chunk);
                }
                else if (chunkType == "IEND")
                {
                    break;
                }

                // Security: Check for integer overflow before updating offset
                long nextOffset = (long)offset + 12 + chunkLength;
                if (nextOffset > int.MaxValue)
                {
                    // Offset overflow - abort processing
                    break;
                }

                offset = (int)nextOffset; // Length(4) + Type(4) + Data(length) + CRC(4)
            }

            // Validate PNG format
            // (interlace check moved earlier - happens immediately after IHDR parsing)

            // Indexed color requires a palette
            if (colorType == 3 && palette == null)
            {
                throw new InvalidDataException("Indexed color PNG is missing required PLTE (palette) chunk.");
            }

            // Validate bit depth for color type
            bool validBitDepth = colorType switch
            {
                0 => bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8 || bitDepth == 16, // Grayscale
                2 => bitDepth == 8 || bitDepth == 16, // RGB
                3 => bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8, // Indexed
                4 => bitDepth == 8 || bitDepth == 16, // Grayscale + Alpha
                6 => bitDepth == 8 || bitDepth == 16, // RGBA
                _ => false
            };

            if (!validBitDepth)
            {
                throw new InvalidDataException($"Invalid bit depth {bitDepth} for PNG color type {colorType}.");
            }

            // Note: 16-bit images are fully supported by PDF and passed through without conversion
            // This preserves maximum quality for high-bit-depth images

            // Map PNG color type to PDF color space and component count
            string colorSpace;
            int colorComponents;
            int totalComponents; // Including alpha
            bool hasAlpha = (colorType == 4 || colorType == 6);

            switch (colorType)
            {
                case 0: // Grayscale
                    colorSpace = "DeviceGray";
                    colorComponents = 1;
                    totalComponents = 1;
                    break;
                case 2: // RGB
                    colorSpace = "DeviceRGB";
                    colorComponents = 3;
                    totalComponents = 3;
                    break;
                case 3: // Indexed (palette)
                    // For indexed color, data is palette indices (1 component)
                    colorSpace = "DeviceRGB"; // Will be overridden by Indexed colorspace
                    colorComponents = 1;
                    totalComponents = 1;
                    break;
                case 4: // Grayscale + Alpha
                    colorSpace = "DeviceGray";
                    colorComponents = 1;
                    totalComponents = 2;
                    break;
                case 6: // RGBA
                    colorSpace = "DeviceRGB";
                    colorComponents = 3;
                    totalComponents = 4;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported PNG color type: {colorType}");
            }

            // Handle alpha channels by decompressing, separating, and recompressing
            byte[]? alphaData = null;
            byte[] compressedColorData;

            if (hasAlpha)
            {
                // Decompress and unfilter the PNG data
                byte[] unfilteredData = DecompressAndUnfilterPng(idatData.ToArray(), width, height, bitDepth, totalComponents);

                // Separate color and alpha channels
                var (colorData, separatedAlpha) = SeparateColorAndAlpha(unfilteredData, width, height, bitDepth, totalComponents, colorComponents);

                // Recompress both streams
                compressedColorData = CompressWithDeflate(colorData);
                alphaData = CompressWithDeflate(separatedAlpha);
            }
            else
            {
                // No alpha channel, use original compressed data
                compressedColorData = idatData.ToArray();
            }

            // Return compressed color data (and alpha data if present)
            // PDF readers will decompress with FlateDecode and apply PNG predictor filters (if no alpha)
            // For alpha images, we've already unfiltered and separated, so no predictor params needed
            return (compressedColorData, bitDepth, colorSpace, colorComponents, palette, transparency, alphaData);
        }
        catch (NotSupportedException)
        {
            // Re-throw validation exceptions (interlaced images, etc.)
            throw;
        }
        catch (InvalidDataException)
        {
            // Re-throw validation exceptions (corrupted files, invalid formats, etc.)
            throw;
        }
        catch (Exception ex)
        {
            // Handle unexpected decoding errors based on configured behavior
            if (_options.ImageErrorBehavior == ImageErrorBehavior.ThrowException)
            {
                throw new ImageDecodingException(
                    "PNG image decoding failed",
                    imagePath: imagePath,
                    imageFormat: "PNG",
                    failureReason: ex.Message,
                    innerException: ex);
            }
            else if (_options.ImageErrorBehavior == ImageErrorBehavior.UsePlaceholder)
            {
                // Return a 1x1 white pixel placeholder (backward compatibility mode)
                byte[] fallback = new byte[] { 255, 255, 255 };
                return (fallback, 8, "DeviceRGB", 3, null, null, null);
            }
            else // SkipImage
            {
                // For SkipImage, we still need to return something valid, so use placeholder
                // The caller should check the error behavior and handle accordingly
                byte[] fallback = new byte[] { 255, 255, 255 };
                return (fallback, 8, "DeviceRGB", 3, null, null, null);
            }
        }
    }

    /// <summary>
    /// Decompresses PNG IDAT data and reverses PNG filters.
    /// </summary>
    private byte[] DecompressAndUnfilterPng(byte[] idatData, int width, int height, int bitsPerComponent, int colorComponents)
    {
        // PNG IDAT data is compressed with zlib, which has a 2-byte header and 4-byte Adler-32 checksum trailer
        // DeflateStream expects raw deflate data, so we skip the zlib wrapper
        // Zlib header format: CMF (1 byte) + FLG (1 byte)
        if (idatData.Length < 6)
        {
            throw new InvalidDataException("IDAT data too short for zlib format");
        }

        // Skip 2-byte zlib header and 4-byte trailer (Adler-32)
        int deflateStart = 2;
        int deflateLength = idatData.Length - 6;

        // Decompress IDAT data using DeflateStream (skip zlib wrapper)
        using var inputStream = new MemoryStream(idatData, deflateStart, deflateLength);
        using var deflateStream = new System.IO.Compression.DeflateStream(inputStream, System.IO.Compression.CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        deflateStream.CopyTo(outputStream);
        byte[] decompressed = outputStream.ToArray();

        // Calculate scanline size (filter byte + pixel data)
        int bytesPerPixel = (bitsPerComponent * colorComponents + 7) / 8;
        int scanlineSize = 1 + ((width * bitsPerComponent * colorComponents + 7) / 8);

        if (decompressed.Length != scanlineSize * height)
        {
            throw new InvalidDataException($"Decompressed PNG data size mismatch. Expected {scanlineSize * height}, got {decompressed.Length}");
        }

        // Reverse PNG filters
        byte[] unfiltered = new byte[scanlineSize * height];
        for (int y = 0; y < height; y++)
        {
            int scanlineOffset = y * scanlineSize;
            byte filterType = decompressed[scanlineOffset];

            // Copy filter byte
            unfiltered[scanlineOffset] = 0; // No filter in output

            // Reverse the filter
            for (int x = 0; x < scanlineSize - 1; x++)
            {
                int currentByte = scanlineOffset + 1 + x;
                byte raw = decompressed[currentByte];
                byte left = x >= bytesPerPixel ? unfiltered[currentByte - bytesPerPixel] : (byte)0;
                byte up = y > 0 ? unfiltered[currentByte - scanlineSize] : (byte)0;
                byte upLeft = (y > 0 && x >= bytesPerPixel) ? unfiltered[currentByte - scanlineSize - bytesPerPixel] : (byte)0;

                byte reconstructed = filterType switch
                {
                    0 => raw, // None
                    1 => (byte)(raw + left), // Sub
                    2 => (byte)(raw + up), // Up
                    3 => (byte)(raw + ((left + up) / 2)), // Average
                    4 => (byte)(raw + PaethPredictor(left, up, upLeft)), // Paeth
                    _ => throw new NotSupportedException($"Unknown PNG filter type: {filterType}")
                };

                unfiltered[currentByte] = reconstructed;
            }
        }

        return unfiltered;
    }

    /// <summary>
    /// Paeth predictor for PNG filter type 4.
    /// </summary>
    private byte PaethPredictor(byte a, byte b, byte c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);

        if (pa <= pb && pa <= pc) return a;
        if (pb <= pc) return b;
        return c;
    }

    /// <summary>
    /// Separates color and alpha channels from unfiltered PNG data.
    /// Returns (colorData, alphaData) as raw uncompressed streams with filter bytes.
    /// </summary>
    private (byte[] ColorData, byte[] AlphaData) SeparateColorAndAlpha(byte[] unfilteredData, int width, int height, int bitsPerComponent, int totalComponents, int colorComponents)
    {
        int scanlineSize = 1 + ((width * bitsPerComponent * totalComponents + 7) / 8);
        int colorScanlineSize = 1 + ((width * bitsPerComponent * colorComponents + 7) / 8);
        int alphaScanlineSize = 1 + ((width * bitsPerComponent + 7) / 8); // Alpha is always 1 component

        byte[] colorData = new byte[colorScanlineSize * height];
        byte[] alphaData = new byte[alphaScanlineSize * height];

        int bytesPerPixel = (bitsPerComponent * totalComponents + 7) / 8;
        int colorBytesPerPixel = (bitsPerComponent * colorComponents + 7) / 8;
        int alphaBytesPerPixel = (bitsPerComponent + 7) / 8;

        for (int y = 0; y < height; y++)
        {
            // Add filter byte (0 = no filter)
            colorData[y * colorScanlineSize] = 0;
            alphaData[y * alphaScanlineSize] = 0;

            for (int x = 0; x < width; x++)
            {
                int srcOffset = y * scanlineSize + 1 + x * bytesPerPixel;
                int colorOffset = y * colorScanlineSize + 1 + x * colorBytesPerPixel;
                int alphaOffset = y * alphaScanlineSize + 1 + x * alphaBytesPerPixel;

                // Copy color components
                for (int c = 0; c < colorBytesPerPixel; c++)
                {
                    colorData[colorOffset + c] = unfilteredData[srcOffset + c];
                }

                // Copy alpha component (last component)
                for (int a = 0; a < alphaBytesPerPixel; a++)
                {
                    alphaData[alphaOffset + a] = unfilteredData[srcOffset + colorBytesPerPixel + a];
                }
            }
        }

        return (colorData, alphaData);
    }

    /// <summary>
    /// Compresses raw image data using Deflate.
    /// </summary>
    private byte[] CompressWithDeflate(byte[] data)
    {
        using var outputStream = new MemoryStream();
        using (var deflateStream = new System.IO.Compression.DeflateStream(outputStream, System.IO.Compression.CompressionLevel.Optimal))
        {
            deflateStream.Write(data, 0, data.Length);
        }
        return outputStream.ToArray();
    }

    /// <summary>
    /// Parses JPEG header to extract image metadata.
    /// Returns (width, height, bitsPerComponent, colorSpace).
    /// </summary>
    private (int Width, int Height, int BitsPerComponent, string ColorSpace) ParseJpegMetadata(byte[] jpegData)
    {
        // Default fallback values
        int width = 0, height = 0, bitsPerComponent = 8;
        string colorSpace = "DeviceRGB";

        try
        {
            // Verify JPEG signature (SOI marker: 0xFF 0xD8)
            if (jpegData.Length < 2 || jpegData[0] != 0xFF || jpegData[1] != 0xD8)
            {
                return (width, height, bitsPerComponent, colorSpace);
            }

            int offset = 2;

            // Parse JPEG markers to find SOF (Start of Frame)
            while (offset + 1 < jpegData.Length)
            {
                // Find next marker (0xFF followed by non-zero byte)
                if (jpegData[offset] != 0xFF)
                {
                    offset++;
                    continue;
                }

                byte marker = jpegData[offset + 1];
                offset += 2;

                // Skip padding bytes (0xFF 0x00 is stuffed 0xFF, not a marker)
                if (marker == 0x00)
                {
                    continue;
                }

                // SOI, EOI, TEM, RSTn markers have no length field
                if (marker == 0xD8 || marker == 0xD9 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
                {
                    continue;
                }

                // Read marker length
                if (offset + 1 >= jpegData.Length)
                {
                    break;
                }

                int length = (jpegData[offset] << 8) | jpegData[offset + 1];

                // Security: Validate marker length
                if (length < 2 || offset + length > jpegData.Length)
                {
                    break;
                }

                // Check if this is a SOF marker (Start of Frame)
                // SOF0 (Baseline DCT): 0xC0
                // SOF1 (Extended Sequential DCT): 0xC1
                // SOF2 (Progressive DCT): 0xC2
                // SOF3 (Lossless): 0xC3
                // SOF5-SOF7, SOF9-SOF11, SOF13-SOF15 are other SOF variants
                bool isSof = (marker >= 0xC0 && marker <= 0xC3) ||
                             (marker >= 0xC5 && marker <= 0xC7) ||
                             (marker >= 0xC9 && marker <= 0xCB) ||
                             (marker >= 0xCD && marker <= 0xCF);

                if (isSof)
                {
                    // SOF structure:
                    // - 2 bytes: length (already read)
                    // - 1 byte: data precision (bits per component)
                    // - 2 bytes: image height
                    // - 2 bytes: image width
                    // - 1 byte: number of components

                    if (offset + 7 <= jpegData.Length)
                    {
                        bitsPerComponent = jpegData[offset + 2];
                        height = (jpegData[offset + 3] << 8) | jpegData[offset + 4];
                        width = (jpegData[offset + 5] << 8) | jpegData[offset + 6];
                        int numComponents = jpegData[offset + 7];

                        // Map number of components to PDF color space
                        colorSpace = numComponents switch
                        {
                            1 => "DeviceGray",    // Grayscale
                            3 => "DeviceRGB",     // RGB or YCbCr (commonly treated as RGB)
                            4 => "DeviceCMYK",    // CMYK
                            _ => "DeviceRGB"      // Default fallback
                        };

                        break; // Found SOF, stop parsing
                    }
                }

                // Skip to next marker
                offset += length;
            }
        }
        catch
        {
            // If parsing fails, return default values
            // Caller will use width/height from image loading
        }

        return (width, height, bitsPerComponent, colorSpace);
    }

    /// <summary>
    /// Writes font resources and returns a mapping of font names to object IDs.
    /// </summary>
    public Dictionary<string, int> WriteFonts(
        HashSet<string> fontNames,
        Dictionary<string, HashSet<char>> characterUsage,
        bool subsetFonts,
        Dictionary<string, string>? trueTypeFonts = null,
        bool enableFontFallback = false)
    {
        var fontIds = new Dictionary<string, int>();
        var embedder = new TrueTypeFontEmbedder(this);

        // Create FontResolver if fallback is enabled
        Fonts.FontResolver? fontResolver = null;
        if (enableFontFallback)
        {
            fontResolver = new Fonts.FontResolver(trueTypeFonts, _options.FontCacheOptions);
        }

        foreach (var fontName in fontNames)
        {
            int fontId;
            string? fontPath = null;

            // Try to get font path from explicit mapping first
            if (trueTypeFonts != null && trueTypeFonts.TryGetValue(fontName, out fontPath))
            {
                // Font explicitly mapped
            }
            // Try font resolution with fallback if enabled
            else if (fontResolver != null)
            {
                fontPath = fontResolver.ResolveFontFamily(fontName);
            }

            // If we have a font path, try to embed it
            if (fontPath != null)
            {
                try
                {
                    fontId = EmbedTrueTypeFont(fontPath, fontName, characterUsage, subsetFonts, embedder);
                }
                catch (Exception)
                {
                    // Fall back to Type1 font if TrueType embedding fails
                    fontId = WriteType1Font(fontName, characterUsage, subsetFonts);
                }
            }
            else
            {
                // Use Type1 font
                fontId = WriteType1Font(fontName, characterUsage, subsetFonts);
            }

            fontIds[fontName] = fontId;
        }

        return fontIds;
    }

    /// <summary>
    /// Embeds a TrueType font from a file path.
    /// </summary>
    private int EmbedTrueTypeFont(
        string fontPath,
        string fontName,
        Dictionary<string, HashSet<char>> characterUsage,
        bool subsetFonts,
        TrueTypeFontEmbedder embedder)
    {
        // Load the font
        var font = Fonts.FontParser.Parse(fontPath);

        // Get used characters for this font
        var usedChars = characterUsage.TryGetValue(fontName, out var chars) ? chars : new HashSet<char>();

        // Determine whether to subset or embed full font
        byte[] fontData;
        Fonts.Models.FontFile fontToEmbed;

        if (subsetFonts && usedChars.Count > 0)
        {
            // Create and parse subset
            fontData = Fonts.FontSubsetter.CreateSubset(font, usedChars);
            using var ms = new MemoryStream(fontData);
            fontToEmbed = Fonts.FontParser.Parse(ms);
        }
        else
        {
            // Check font file size before loading into memory
            var fontFileInfo = new FileInfo(fontPath);
            long fontFileSize = fontFileInfo.Length;

            // Enforce memory quota for large fonts (prevents OutOfMemoryException)
            if (_options.MaxFontMemory > 0 && fontFileSize > _options.MaxFontMemory)
            {
                throw new InvalidOperationException(
                    $"Font file '{Path.GetFileName(fontPath)}' ({fontFileSize:N0} bytes) exceeds the maximum allowed font memory ({_options.MaxFontMemory:N0} bytes). " +
                    $"To resolve this issue, you can: " +
                    $"1) Enable font subsetting (PdfOptions.SubsetFonts = true) to reduce font size, " +
                    $"2) Increase the memory limit (PdfOptions.MaxFontMemory), or " +
                    $"3) Use a smaller font file. " +
                    $"Font path: {fontPath}");
            }

            // Load font data from cache or disk
            fontData = _fontDataCache != null
                ? _fontDataCache.LoadFontData(fontPath)
                : File.ReadAllBytes(fontPath);
            fontToEmbed = font;
        }

        // Build character to glyph index mapping
        var charToGlyph = BuildCharacterToGlyphMapping(fontToEmbed, usedChars);

        // Store character to glyph ID mapping for content stream generation
        _characterToGlyphId[fontName] = charToGlyph;

        // Embed the font
        return embedder.EmbedTrueTypeFont(
            fontToEmbed.PostScriptName,
            fontData,
            charToGlyph,
            fontToEmbed.UnitsPerEm,
            fontToEmbed.Ascender,
            fontToEmbed.Descender,
            fontToEmbed.XMin,
            fontToEmbed.YMin,
            fontToEmbed.XMax,
            fontToEmbed.YMax,
            fontToEmbed.GlyphAdvanceWidths);
    }

    /// <summary>
    /// Builds a character to glyph ID mapping for a font.
    /// </summary>
    private static Dictionary<char, ushort> BuildCharacterToGlyphMapping(
        Fonts.Models.FontFile font,
        HashSet<char> usedChars)
    {
        var charToGlyph = new Dictionary<char, ushort>();

        // If we have specific characters to map (subset), use only those
        if (usedChars.Count > 0)
        {
            foreach (var ch in usedChars)
            {
                if (font.CharacterToGlyphIndex.TryGetValue(ch, out var glyphIndex))
                {
                    charToGlyph[ch] = glyphIndex;
                }
            }
        }
        else
        {
            // Otherwise, map all characters in the font (full embedding)
            foreach (var kvp in font.CharacterToGlyphIndex)
            {
                if (kvp.Key <= char.MaxValue)
                {
                    charToGlyph[(char)kvp.Key] = kvp.Value;
                }
            }
        }

        return charToGlyph;
    }

    /// <summary>
    /// Writes a Type1 font (PDF base font).
    /// </summary>
    private int WriteType1Font(
        string fontName,
        Dictionary<string, HashSet<char>> characterUsage,
        bool subsetFonts)
    {
        var pdfFontName = GetPdfFontName(fontName);

        // Write encoding dictionary FIRST (before starting font object)
        // so we don't nest object creation
        int? encodingId = null;
        if (subsetFonts && characterUsage.TryGetValue(fontName, out var usedChars) && usedChars.Count > 0)
        {
            encodingId = WriteCustomEncoding(fontName, usedChars);
        }

        // Now write the font dictionary
        var fontId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /Font");
        WriteLine("  /Subtype /Type1");
        WriteLine($"  /BaseFont /{pdfFontName}");

        // Reference the encoding dictionary if one was created
        if (encodingId.HasValue)
        {
            WriteLine($"  /Encoding {encodingId.Value} 0 R");
        }

        WriteLine(">>");
        EndObject();

        return fontId;
    }

    /// <summary>
    /// Writes a custom encoding dictionary for font subsetting.
    /// </summary>
    private int WriteCustomEncoding(string fontName, HashSet<char> usedChars)
    {
        var encodingId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /Encoding");
        WriteLine("  /BaseEncoding /WinAnsiEncoding");

        // Create character remapping for this font
        // Characters with codes > 255 need to be remapped to unused slots in 0-255
        var remapping = new Dictionary<char, byte>();

        // Find unused byte codes (avoiding common control characters and special codes)
        // We'll use slots 128-159 which are undefined in WinAnsiEncoding
        var availableSlots = new List<byte>();
        for (byte b = 128; b < 160; b++)
        {
            availableSlots.Add(b);
        }
        int slotIndex = 0;

        // Build remapping for high-Unicode characters
        var sortedChars = usedChars.OrderBy(c => (int)c).ToList();
        foreach (var ch in sortedChars)
        {
            if ((int)ch <= 255)
            {
                // Characters 0-255 map to themselves
                remapping[ch] = (byte)(int)ch;
            }
            else
            {
                // Characters > 255 need remapping
                if (slotIndex < availableSlots.Count)
                {
                    remapping[ch] = availableSlots[slotIndex++];
                }
                else
                {
                    // Fallback: use modulo 256 (may cause collisions but better than nothing)
                    remapping[ch] = (byte)((int)ch % 256);
                }
            }
        }

        // Store the remapping for this font
        _characterRemapping[fontName] = remapping;

        // Create Differences array with remapped character codes
        if (sortedChars.Count > 0)
        {
            WriteLine("  /Differences [");

            // Group by remapped byte code for more compact representation
            var charsByCode = sortedChars
                .GroupBy(ch => remapping[ch])
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in charsByCode)
            {
                var byteCode = group.Key;
                Write($"    {byteCode}");

                foreach (var ch in group)
                {
                    var charName = GetCharacterName(ch);
                    Write($" /{charName}");
                }

                WriteLine("");
            }

            WriteLine("  ]");
        }

        WriteLine(">>");
        EndObject();

        return encodingId;
    }

    /// <summary>
    /// Gets the PostScript character name for a given character using the Adobe Glyph List.
    /// </summary>
    private static string GetCharacterName(char ch)
    {
        int codePoint = (int)ch;

        // Try to get the glyph name from the Adobe Glyph List
        if (AdobeGlyphList.TryGetGlyphName(codePoint, out var glyphName))
        {
            return glyphName;
        }

        // Fallback for unmapped characters: use uniXXXX format
        // This is a standard PDF convention for characters without standard glyph names
        return $"uni{codePoint:X4}";
    }

    private static string GetPdfFontName(string fontFamily)
    {
        var lowerFamily = fontFamily.ToLowerInvariant();

        // If the font family already specifies a variant (Bold, Italic, etc.), return it as-is
        if (lowerFamily.Contains("-bold") || lowerFamily.Contains("-italic") ||
            lowerFamily.Contains("-oblique") || lowerFamily.Contains("bold") && lowerFamily.Contains("italic"))
        {
            // Normalize the case for PDF standard font names
            return fontFamily switch
            {
                // Times variants
                _ when lowerFamily.StartsWith("times") && lowerFamily.Contains("bold") && lowerFamily.Contains("italic") => "Times-BoldItalic",
                _ when lowerFamily.StartsWith("times") && lowerFamily.Contains("bold") => "Times-Bold",
                _ when lowerFamily.StartsWith("times") && lowerFamily.Contains("italic") => "Times-Italic",

                // Helvetica variants
                _ when lowerFamily.StartsWith("helvetica") && lowerFamily.Contains("bold") && lowerFamily.Contains("oblique") => "Helvetica-BoldOblique",
                _ when lowerFamily.StartsWith("helvetica") && lowerFamily.Contains("bold") => "Helvetica-Bold",
                _ when lowerFamily.StartsWith("helvetica") && lowerFamily.Contains("oblique") => "Helvetica-Oblique",

                // Courier variants
                _ when lowerFamily.StartsWith("courier") && lowerFamily.Contains("bold") && lowerFamily.Contains("oblique") => "Courier-BoldOblique",
                _ when lowerFamily.StartsWith("courier") && lowerFamily.Contains("bold") => "Courier-Bold",
                _ when lowerFamily.StartsWith("courier") && lowerFamily.Contains("oblique") => "Courier-Oblique",

                // Default: return as-is (case-corrected)
                _ => fontFamily
            };
        }

        // Map generic font families to base fonts
        return lowerFamily switch
        {
            "helvetica" or "arial" or "sans-serif" => "Helvetica",
            "times" or "times new roman" or "serif" => "Times-Roman",
            "courier" or "courier new" or "monospace" => "Courier",
            _ => fontFamily // Return original if not recognized
        };
    }

    /// <summary>
    /// Writes a page and returns its object ID.
    /// </summary>
    public int WritePage(PageViewport page, string content, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, bool compressStreams = true)
    {
        // Write the content stream first
        var contentId = BeginObject();

        byte[] streamData;
        bool isCompressed = false;

        if (compressStreams)
        {
            // Compress the content using Flate compression
            // Use Latin1 (ISO-8859-1) encoding to support the full 0-255 byte range
            // needed for font subsetting with character remapping
            var uncompressedBytes = Encoding.Latin1.GetBytes(content);

            using (var compressedStream = new MemoryStream())
            {
                // Write zlib header (for PDF FlateDecode compatibility)
                // zlib format: CMF (Compression Method and Flags) + FLG (Flags)
                // CMF: 0x78 = deflate with 32K window
                // FLG: 0x9C = default compression, FCHECK bits set so (CMF * 256 + FLG) % 31 == 0
                compressedStream.WriteByte(0x78);
                compressedStream.WriteByte(0x9C);

                // Use DeflateStream for the actual compression
                using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    deflateStream.Write(uncompressedBytes, 0, uncompressedBytes.Length);
                }

                // Write Adler-32 checksum (required by zlib format)
                var adler32 = CalculateAdler32(uncompressedBytes);
                compressedStream.WriteByte((byte)(adler32 >> 24));
                compressedStream.WriteByte((byte)(adler32 >> 16));
                compressedStream.WriteByte((byte)(adler32 >> 8));
                compressedStream.WriteByte((byte)adler32);

                streamData = compressedStream.ToArray();
                isCompressed = true;
            }
        }
        else
        {
            // Use Latin1 (ISO-8859-1) encoding to support the full 0-255 byte range
            streamData = Encoding.Latin1.GetBytes(content);
        }

        WriteLine("<<");
        WriteLine($"  /Length {streamData.Length}");
        if (isCompressed)
        {
            WriteLine("  /Filter /FlateDecode");
        }
        WriteLine(">>");
        WriteLine("stream");

        // Write binary stream data
        _output.Write(streamData, 0, streamData.Length);
        _position += streamData.Length;

        WriteLine("");
        WriteLine("endstream");
        EndObject();

        // Write link annotation objects
        var annotationIds = new List<int>();
        foreach (var link in page.Links)
        {
            var annotId = WriteLinkAnnotation(link, page.Height);
            annotationIds.Add(annotId);
        }

        // Write the page object
        var pageId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /Page");
        WriteLine("  /Parent 2 0 R"); // Reference to pages object
        WriteLine($"  /MediaBox [0 0 {page.Width:F2} {page.Height:F2}]");
        WriteLine($"  /Contents {contentId} 0 R");

        // Write resources (fonts and images)
        if (fontIds.Count > 0 || imageIds.Count > 0)
        {
            WriteLine("  /Resources <<");

            // Write font resources
            if (fontIds.Count > 0)
            {
                WriteLine("    /Font <<");
                foreach (var kvp in fontIds)
                {
                    WriteLine($"      /F{kvp.Value} {kvp.Value} 0 R");
                }
                WriteLine("    >>");
            }

            // Write image resources
            if (imageIds.Count > 0)
            {
                WriteLine("    /XObject <<");
                foreach (var kvp in imageIds)
                {
                    WriteLine($"      /Im{kvp.Value} {kvp.Value} 0 R");
                }
                WriteLine("    >>");
            }

            WriteLine("  >>");
        }

        // Write annotations array if there are links
        if (annotationIds.Count > 0)
        {
            WriteLine("  /Annots [");
            foreach (var annotId in annotationIds)
            {
                WriteLine($"    {annotId} 0 R");
            }
            WriteLine("  ]");
        }

        WriteLine(">>");
        EndObject();

        return pageId;
    }

    /// <summary>
    /// Writes a link annotation object and returns its object ID.
    /// </summary>
    private int WriteLinkAnnotation(LinkArea link, double pageHeight)
    {
        var annotId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /Annot");
        WriteLine("  /Subtype /Link");

        // Calculate PDF rectangle coordinates (bottom-left origin)
        var x1 = link.X;
        var y1 = pageHeight - link.Y - link.Height;
        var x2 = link.X + link.Width;
        var y2 = pageHeight - link.Y;
        WriteLine($"  /Rect [{x1:F2} {y1:F2} {x2:F2} {y2:F2}]");

        // No border
        WriteLine("  /Border [0 0 0]");

        // Determine if internal or external link
        if (!string.IsNullOrEmpty(link.ExternalDestination))
        {
            // External link (URI action)
            WriteLine("  /A <<");
            WriteLine("    /S /URI");
            WriteLine($"    /URI ({EscapeString(link.ExternalDestination)})");
            WriteLine("  >>");
        }
        else if (!string.IsNullOrEmpty(link.InternalDestination))
        {
            // Internal link (named destination)
            // For MVP, we'll use a simple named destination
            // In a full implementation, this would resolve to actual page numbers
            WriteLine($"  /Dest /{EscapeString(link.InternalDestination)}");
        }

        WriteLine(">>");
        EndObject();

        return annotId;
    }

    /// <summary>
    /// Writes the PDF outline (bookmarks) and returns the root outline object ID.
    /// </summary>
    private int WriteOutline(Dom.FoBookmarkTree bookmarkTree)
    {
        // Write all bookmark items first, collecting their IDs
        var bookmarkIds = new List<int>();
        foreach (var bookmark in bookmarkTree.Bookmarks)
        {
            var bookmarkId = WriteBookmarkItem(bookmark, null, null);
            bookmarkIds.Add(bookmarkId);
        }

        // Link siblings together
        for (int i = 0; i < bookmarkIds.Count; i++)
        {
            int? prev = i > 0 ? bookmarkIds[i - 1] : null;
            int? next = i < bookmarkIds.Count - 1 ? bookmarkIds[i + 1] : null;
            // Note: We would need to update the bookmark objects with Prev/Next references
            // For simplicity, we'll skip this in the MVP
        }

        // Write the root Outlines object
        var outlineId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /Outlines");

        if (bookmarkIds.Count > 0)
        {
            WriteLine($"  /First {bookmarkIds[0]} 0 R");
            WriteLine($"  /Last {bookmarkIds[bookmarkIds.Count - 1]} 0 R");
            WriteLine($"  /Count {bookmarkIds.Count}");
        }

        WriteLine(">>");
        EndObject();

        return outlineId;
    }

    /// <summary>
    /// Writes a single bookmark item and its children recursively.
    /// Returns the object ID of this bookmark.
    /// </summary>
    private int WriteBookmarkItem(Dom.FoBookmark bookmark, int? parentId, int? prevId)
    {
        // Recursively write child bookmarks first
        var childIds = new List<int>();
        foreach (var child in bookmark.Children)
        {
            var childId = WriteBookmarkItem(child, null, null); // Parent will be set after we know this bookmark's ID
            childIds.Add(childId);
        }

        // Write this bookmark object
        var bookmarkId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Title (" + EscapeString(bookmark.Title ?? "Untitled") + ")");

        // Add parent reference if provided
        if (parentId.HasValue)
        {
            WriteLine($"  /Parent {parentId.Value} 0 R");
        }

        // Add destination (internal link to page)
        if (!string.IsNullOrEmpty(bookmark.InternalDestination))
        {
            // For MVP, use named destination
            // In full implementation, would resolve to /Dest [pageRef /XYZ x y zoom]
            WriteLine($"  /Dest /{EscapeString(bookmark.InternalDestination)}");
        }
        else if (!string.IsNullOrEmpty(bookmark.ExternalDestination))
        {
            // External URI action
            WriteLine("  /A <<");
            WriteLine("    /S /URI");
            WriteLine($"    /URI ({EscapeString(bookmark.ExternalDestination)})");
            WriteLine("  >>");
        }

        // Add child references
        if (childIds.Count > 0)
        {
            WriteLine($"  /First {childIds[0]} 0 R");
            WriteLine($"  /Last {childIds[childIds.Count - 1]} 0 R");

            // Count: positive if expanded, negative if collapsed
            var count = bookmark.StartingState == "show" ? childIds.Count : -childIds.Count;
            WriteLine($"  /Count {count}");
        }

        WriteLine(">>");
        EndObject();

        return bookmarkId;
    }

    /// <summary>
    /// Writes the pages tree at object ID 2.
    /// </summary>
    public void WritePages(int pagesObjectId, List<int> pageIds, IReadOnlyList<PageViewport> pages)
    {
        // Write pages tree as object 2 (reserved in WriteCatalog)
        // Update the offset for object 2 (index 1 in _objectOffsets)
        _objectOffsets[1] = _position;

        WriteLine("2 0 obj");
        WriteLine("<<");
        WriteLine("  /Type /Pages");
        Write("  /Kids [");
        for (int i = 0; i < pageIds.Count; i++)
        {
            Write($"{pageIds[i]} 0 R");
            if (i < pageIds.Count - 1)
                Write(" ");
        }
        WriteLine("]");
        WriteLine($"  /Count {pageIds.Count}");
        WriteLine(">>");
        WriteLine("endobj");
    }

    /// <summary>
    /// Writes document metadata and returns the Info object ID.
    /// </summary>
    public int WriteMetadata(PdfMetadata metadata)
    {
        var infoId = BeginObject();
        _infoObjectId = infoId;

        WriteLine("<<");

        if (!string.IsNullOrWhiteSpace(metadata.Title))
            WriteLine($"  /Title ({EscapeString(metadata.Title)})");

        if (!string.IsNullOrWhiteSpace(metadata.Author))
            WriteLine($"  /Author ({EscapeString(metadata.Author)})");

        if (!string.IsNullOrWhiteSpace(metadata.Subject))
            WriteLine($"  /Subject ({EscapeString(metadata.Subject)})");

        if (!string.IsNullOrWhiteSpace(metadata.Keywords))
            WriteLine($"  /Keywords ({EscapeString(metadata.Keywords)})");

        WriteLine($"  /Creator ({EscapeString(metadata.Creator)})");
        WriteLine($"  /Producer ({EscapeString(metadata.Producer)})");
        WriteLine($"  /CreationDate (D:{DateTime.UtcNow:yyyyMMddHHmmss}Z)");

        WriteLine(">>");
        EndObject();

        return infoId;
    }

    /// <summary>
    /// Writes XMP metadata stream for PDF/A compliance and returns its object ID.
    /// XMP (Extensible Metadata Platform) is required for PDF/A.
    /// </summary>
    public int WriteXmpMetadata(PdfMetadata metadata, PdfALevel pdfALevel, string pdfVersion)
    {
        var xmpData = XmpMetadataWriter.CreateXmpMetadata(metadata, pdfALevel, pdfVersion);

        var metadataId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /Metadata");
        WriteLine("  /Subtype /XML");
        WriteLine($"  /Length {xmpData.Length}");
        WriteLine(">>");
        WriteLine("stream");

        // Write XMP data as binary
        _writer.Flush();
        _output.Write(xmpData, 0, xmpData.Length);
        _position += xmpData.Length;

        WriteLine("");
        WriteLine("endstream");
        EndObject();

        return metadataId;
    }

    /// <summary>
    /// Writes OutputIntent array for PDF/A compliance and returns its object ID.
    /// OutputIntent defines the intended output device or production condition.
    /// </summary>
    public int WriteOutputIntent()
    {
        // First, write the ICC profile stream
        var iccProfile = SrgbIccProfile.GetProfile();
        var iccProfileId = BeginObject();
        WriteLine("<<");
        WriteLine("  /N 3"); // Number of color components (RGB = 3)
        WriteLine($"  /Length {iccProfile.Length}");

        if (_options.CompressStreams)
        {
            WriteLine("  /Filter /FlateDecode");
            var compressed = CompressWithDeflate(iccProfile);
            WriteLine(">>");
            WriteLine("stream");
            _writer.Flush();
            _output.Write(compressed, 0, compressed.Length);
            _position += compressed.Length;
        }
        else
        {
            WriteLine(">>");
            WriteLine("stream");
            _writer.Flush();
            _output.Write(iccProfile, 0, iccProfile.Length);
            _position += iccProfile.Length;
        }

        WriteLine("");
        WriteLine("endstream");
        EndObject();

        // Now write the OutputIntent dictionary
        var outputIntentId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /OutputIntent");
        WriteLine("  /S /GTS_PDFA1"); // PDF/A output intent subtype
        WriteLine($"  /OutputConditionIdentifier ({EscapeString(SrgbIccProfile.OutputConditionIdentifier)})");
        WriteLine($"  /OutputCondition ({EscapeString(SrgbIccProfile.OutputCondition)})");
        WriteLine($"  /RegistryName ({EscapeString(SrgbIccProfile.RegistryName)})");
        WriteLine($"  /DestOutputProfile {iccProfileId} 0 R");
        WriteLine(">>");
        EndObject();

        return outputIntentId;
    }

    /// <summary>
    /// Writes the cross-reference table and trailer.
    /// </summary>
    public void WriteXRefAndTrailer(int catalogId)
    {
        var xrefPos = _position;

        WriteLine("xref");
        WriteLine($"0 {_objectOffsets.Count + 1}");
        WriteLine("0000000000 65535 f ");

        foreach (var offset in _objectOffsets)
        {
            WriteLine($"{offset:D10} 00000 n ");
        }

        WriteLine("trailer");
        WriteLine("<<");
        WriteLine($"  /Size {_objectOffsets.Count + 1}");
        WriteLine($"  /Root {catalogId} 0 R");
        if (_infoObjectId.HasValue)
            WriteLine($"  /Info {_infoObjectId.Value} 0 R");
        WriteLine(">>");
        WriteLine("startxref");
        WriteLine(xrefPos.ToString());
        WriteLine("%%EOF");
    }

    internal int BeginObject()
    {
        var id = _nextObjectId++;
        _objectOffsets.Add(_position);
        WriteLine($"{id} 0 obj");
        return id;
    }

    /// <summary>
    /// Begins an object with a specific pre-assigned object ID.
    /// Used when object IDs need to be reserved before writing.
    /// </summary>
    internal void BeginObject(int objectId)
    {
        // Ensure the offset list is large enough
        while (_objectOffsets.Count <= objectId - 1)
        {
            _objectOffsets.Add(0);
        }

        // Update the offset for this object
        _objectOffsets[objectId - 1] = _position;
        WriteLine($"{objectId} 0 obj");

        // Update next object ID if necessary
        if (objectId >= _nextObjectId)
        {
            _nextObjectId = objectId + 1;
        }
    }

    /// <summary>
    /// Reserves an object ID without writing the object yet.
    /// The object can be written later using BeginObject(int objectId).
    /// </summary>
    internal int ReserveObjectId()
    {
        var id = _nextObjectId++;
        _objectOffsets.Add(0);  // Placeholder offset, will be updated when object is written
        return id;
    }

    internal void EndObject()
    {
        WriteLine("endobj");
    }

    internal void WriteLine(string line)
    {
        _writer.WriteLine(line);
        _position += Encoding.ASCII.GetByteCount(line) + 1; // +1 for newline
    }

    /// <summary>
    /// Writes a string without a newline.
    /// </summary>
    internal void Write(string text)
    {
        _writer.Write(text);
        _writer.Flush();
        _position += Encoding.ASCII.GetByteCount(text);
    }

    /// <summary>
    /// Gets the underlying stream for writing binary data.
    /// Used by font embedder and other binary data writers.
    /// </summary>
    internal Stream GetStream()
    {
        _writer.Flush();
        return _output;
    }

    /// <summary>
    /// Updates the position counter after writing binary data directly to the stream.
    /// </summary>
    internal void UpdatePosition(int bytesWritten)
    {
        _position += bytesWritten;
    }

    /// <summary>
    /// Calculates Adler-32 checksum for zlib format.
    /// </summary>
    private static uint CalculateAdler32(byte[] data)
    {
        const uint MOD_ADLER = 65521;
        uint a = 1, b = 0;

        foreach (byte bite in data)
        {
            a = (a + bite) % MOD_ADLER;
            b = (b + a) % MOD_ADLER;
        }

        return (b << 16) | a;
    }

    /// <summary>
    /// Escapes a string for safe inclusion in PDF string literals.
    /// Prevents PDF metadata injection attacks by escaping special characters.
    /// </summary>
    internal static string EscapeString(string str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;

        // Security: Escape backslashes first to avoid double-escaping
        var result = str
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");

        // Security: Remove null bytes and other control characters that could break PDF structure
        var sb = new System.Text.StringBuilder(result.Length);
        foreach (char c in result)
        {
            if (c == '\0' || (c < 32 && c != '\t' && c != '\n' && c != '\r'))
            {
                // Skip dangerous control characters
                continue;
            }
            sb.Append(c);
        }

        return sb.ToString();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _writer.Dispose();
            _disposed = true;
        }
    }
}
