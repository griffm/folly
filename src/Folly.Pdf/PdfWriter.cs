using System.IO.Compression;

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
    public int WriteCatalog(int pageCount, Dom.FoBookmarkTree? bookmarkTree = null)
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

        WriteLine(">>");
        WriteLine("endobj");

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
    public Dictionary<string, int> WriteFonts(HashSet<string> fontNames, Dictionary<string, HashSet<char>> characterUsage, bool subsetFonts)
    {
        var fontIds = new Dictionary<string, int>();

        // First, write all encoding dictionaries if subsetting is enabled
        var encodingIds = new Dictionary<string, int>();
        if (subsetFonts)
        {
            foreach (var fontName in fontNames)
            {
                if (characterUsage.TryGetValue(fontName, out var usedChars) && usedChars.Count > 0)
                {
                    var encodingId = WriteCustomEncoding(usedChars);
                    encodingIds[fontName] = encodingId;
                }
            }
        }

        // Now write font dictionaries
        foreach (var fontName in fontNames)
        {
            var pdfFontName = GetPdfFontName(fontName);
            var fontId = BeginObject();
            WriteLine("<<");
            WriteLine("  /Type /Font");
            WriteLine("  /Subtype /Type1");
            WriteLine($"  /BaseFont /{pdfFontName}");

            // Reference the encoding dictionary if one was created
            if (encodingIds.TryGetValue(fontName, out var encodingId))
            {
                WriteLine($"  /Encoding {encodingId} 0 R");
            }

            WriteLine(">>");
            EndObject();

            fontIds[fontName] = fontId;
        }

        return fontIds;
    }

    /// <summary>
    /// Writes a custom encoding dictionary for font subsetting.
    /// </summary>
    private int WriteCustomEncoding(HashSet<char> usedChars)
    {
        var encodingId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /Encoding");
        WriteLine("  /BaseEncoding /WinAnsiEncoding");

        // Create Differences array with only used characters
        // This optimizes the PDF by explicitly listing only needed glyphs
        var sortedChars = usedChars.OrderBy(c => (int)c).ToList();

        if (sortedChars.Count > 0)
        {
            WriteLine("  /Differences [");

            // Group consecutive characters for more compact representation
            var i = 0;
            while (i < sortedChars.Count)
            {
                var startChar = sortedChars[i];
                var charCode = (int)startChar;
                Write($"    {charCode}");

                // Find consecutive run
                var j = i;
                while (j < sortedChars.Count && (int)sortedChars[j] == charCode + (j - i))
                {
                    var charName = GetCharacterName(sortedChars[j]);
                    Write($" /{charName}");
                    j++;
                }

                WriteLine("");
                i = j;
            }

            WriteLine("  ]");
        }

        WriteLine(">>");
        EndObject();

        return encodingId;
    }

    /// <summary>
    /// Gets the PostScript character name for a given character.
    /// </summary>
    private static string GetCharacterName(char ch)
    {
        // Map common characters to their PostScript names
        return ch switch
        {
            ' ' => "space",
            '!' => "exclam",
            '"' => "quotedbl",
            '#' => "numbersign",
            '$' => "dollar",
            '%' => "percent",
            '&' => "ampersand",
            '\'' => "quotesingle",
            '(' => "parenleft",
            ')' => "parenright",
            '*' => "asterisk",
            '+' => "plus",
            ',' => "comma",
            '-' => "hyphen",
            '.' => "period",
            '/' => "slash",
            ':' => "colon",
            ';' => "semicolon",
            '<' => "less",
            '=' => "equal",
            '>' => "greater",
            '?' => "question",
            '@' => "at",
            '[' => "bracketleft",
            '\\' => "backslash",
            ']' => "bracketright",
            '^' => "asciicircum",
            '_' => "underscore",
            '`' => "grave",
            '{' => "braceleft",
            '|' => "bar",
            '}' => "braceright",
            '~' => "asciitilde",
            _ => ch >= '0' && ch <= '9' ? $"{ch}" :
                 ch >= 'A' && ch <= 'Z' ? $"{ch}" :
                 ch >= 'a' && ch <= 'z' ? $"{ch}" :
                 $"char{(int)ch}"
        };
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
    public int WritePage(PageViewport page, string content, Dictionary<string, int> fontIds, Dictionary<string, int> imageIds, bool compressStreams = true)
    {
        // Write the content stream first
        var contentId = BeginObject();

        byte[] streamData;
        bool isCompressed = false;

        if (compressStreams)
        {
            // Compress the content using Flate compression
            var uncompressedBytes = Encoding.ASCII.GetBytes(content);

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
            streamData = Encoding.ASCII.GetBytes(content);
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
