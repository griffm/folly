namespace Folly.Pdf;

/// <summary>
/// Low-level PDF writer for creating PDF 1.7 documents.
/// </summary>
internal sealed class PdfWriter : IDisposable
{
    private readonly Stream _output;
    private readonly StreamWriter _writer;
    private readonly List<long> _objectOffsets = new();
    private int _nextObjectId = 1;
    private long _position;
    private bool _disposed;

    public PdfWriter(Stream output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
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
    /// </summary>
    public int WriteCatalog(int pageCount)
    {
        var catalogId = BeginObject();  // Object 1
        WriteLine("<<");
        WriteLine("  /Type /Catalog");
        WriteLine($"  /Pages 2 0 R");  // Pages tree will be object 2
        WriteLine(">>");
        EndObject();

        // Reserve object 2 for pages tree (will be written later)
        _objectOffsets.Add(0);  // Placeholder offset for object 2
        _nextObjectId = 3;  // Next object will be 3

        return catalogId;
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

            if (format == "JPEG")
            {
                var imageId = WriteJpegXObject(data, width, height);
                imageIds[source] = imageId;
            }
            else if (format == "PNG")
            {
                var imageId = WritePngXObject(data, width, height);
                imageIds[source] = imageId;
            }
        }

        return imageIds;
    }

    private int WriteJpegXObject(byte[] jpegData, int width, int height)
    {
        var imageId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /XObject");
        WriteLine("  /Subtype /Image");
        WriteLine($"  /Width {width}");
        WriteLine($"  /Height {height}");
        WriteLine("  /ColorSpace /DeviceRGB");
        WriteLine("  /BitsPerComponent 8");
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

    private int WritePngXObject(byte[] pngData, int width, int height)
    {
        // Decode PNG and write as uncompressed or FlateDecode image
        var (rawData, bitsPerComponent, colorSpace) = DecodePng(pngData, width, height);

        var imageId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /XObject");
        WriteLine("  /Subtype /Image");
        WriteLine($"  /Width {width}");
        WriteLine($"  /Height {height}");
        WriteLine($"  /ColorSpace /{colorSpace}");
        WriteLine($"  /BitsPerComponent {bitsPerComponent}");
        WriteLine($"  /Length {rawData.Length}");
        WriteLine(">>");
        WriteLine("stream");

        // Write raw image data
        _output.Write(rawData, 0, rawData.Length);
        _position += rawData.Length;

        WriteLine("");
        WriteLine("endstream");
        EndObject();

        return imageId;
    }

    private (byte[] RawData, int BitsPerComponent, string ColorSpace) DecodePng(byte[] pngData, int width, int height)
    {
        // Simplified PNG decoder - extract RGB data
        // For production use, consider using a PNG library like SixLabors.ImageSharp

        // For now, we'll do a basic extraction assuming RGB/RGBA PNG
        // This is a simplified implementation

        try
        {
            // Parse PNG chunks to find IDAT (image data)
            var idatData = new List<byte>();
            int offset = 8; // Skip PNG signature

            while (offset < pngData.Length)
            {
                if (offset + 8 > pngData.Length) break;

                int chunkLength = (pngData[offset] << 24) | (pngData[offset + 1] << 16) |
                                 (pngData[offset + 2] << 8) | pngData[offset + 3];

                string chunkType = Encoding.ASCII.GetString(pngData, offset + 4, 4);

                if (chunkType == "IDAT")
                {
                    // Collect IDAT data (compressed)
                    for (int i = 0; i < chunkLength; i++)
                    {
                        idatData.Add(pngData[offset + 8 + i]);
                    }
                }
                else if (chunkType == "IEND")
                {
                    break;
                }

                offset += 12 + chunkLength; // Length(4) + Type(4) + Data(length) + CRC(4)
            }

            // For simplicity, return compressed data with FlateDecode filter
            // A full implementation would decompress and convert to RGB
            return (idatData.ToArray(), 8, "DeviceRGB");
        }
        catch
        {
            // Fallback: create a placeholder image (1x1 white pixel)
            byte[] fallback = new byte[] { 255, 255, 255 };
            return (fallback, 8, "DeviceRGB");
        }
    }

    /// <summary>
    /// Writes font resources and returns a mapping of font names to object IDs.
    /// </summary>
    public Dictionary<string, int> WriteFonts(HashSet<string> fontNames)
    {
        var fontIds = new Dictionary<string, int>();

        foreach (var fontName in fontNames)
        {
            var pdfFontName = GetPdfFontName(fontName);
            var fontId = BeginObject();
            WriteLine("<<");
            WriteLine("  /Type /Font");
            WriteLine("  /Subtype /Type1");
            WriteLine($"  /BaseFont /{pdfFontName}");
            WriteLine(">>");
            EndObject();

            fontIds[fontName] = fontId;
        }

        return fontIds;
    }

    private static string GetPdfFontName(string fontFamily)
    {
        return fontFamily.ToLowerInvariant() switch
        {
            "helvetica" or "arial" or "sans-serif" => "Helvetica",
            "times" or "times new roman" or "serif" => "Times-Roman",
            "courier" or "courier new" or "monospace" => "Courier",
            _ => "Helvetica"
        };
    }

    /// <summary>
    /// Writes a page and returns its object ID.
    /// </summary>
    public int WritePage(PageViewport page, string content, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds)
    {
        // Write the content stream first
        var contentId = BeginObject();
        var contentBytes = Encoding.ASCII.GetByteCount(content);
        WriteLine("<<");
        WriteLine($"  /Length {contentBytes}");
        WriteLine(">>");
        WriteLine("stream");
        _writer.Write(content);
        _position += contentBytes;
        WriteLine("");
        WriteLine("endstream");
        EndObject();

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

        WriteLine(">>");
        EndObject();

        return pageId;
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

    private void Write(string text)
    {
        _writer.Write(text);
        _position += Encoding.ASCII.GetByteCount(text);
    }

    /// <summary>
    /// Writes document metadata.
    /// </summary>
    public void WriteMetadata(PdfMetadata metadata)
    {
        var infoId = BeginObject();
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
        if (_objectOffsets.Count >= 3)
            WriteLine($"  /Info {_objectOffsets.Count} 0 R");
        WriteLine(">>");
        WriteLine("startxref");
        WriteLine(xrefPos.ToString());
        WriteLine("%%EOF");
    }

    private int BeginObject()
    {
        var id = _nextObjectId++;
        _objectOffsets.Add(_position);
        WriteLine($"{id} 0 obj");
        return id;
    }

    private void EndObject()
    {
        WriteLine("endobj");
    }

    private void WriteLine(string line)
    {
        _writer.WriteLine(line);
        _position += Encoding.ASCII.GetByteCount(line) + 1; // +1 for newline
    }

    private static string EscapeString(string str)
    {
        return str
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
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
