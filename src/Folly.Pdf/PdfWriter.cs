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
    public int WritePage(PageViewport page, string content, Dictionary<string, int> fontIds)
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

        // Write font resources
        if (fontIds.Count > 0)
        {
            WriteLine("  /Resources <<");
            WriteLine("    /Font <<");
            foreach (var kvp in fontIds)
            {
                WriteLine($"      /F{kvp.Value} {kvp.Value} 0 R");
            }
            WriteLine("    >>");
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
