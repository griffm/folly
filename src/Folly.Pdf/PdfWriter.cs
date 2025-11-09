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
    /// Writes the document catalog.
    /// </summary>
    public void WriteCatalog()
    {
        // TODO: Implement catalog with pages tree
        var catalogId = BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /Catalog");
        WriteLine($"  /Pages {catalogId + 1} 0 R");
        WriteLine(">>");
        EndObject();

        // Write empty pages object for now
        BeginObject();
        WriteLine("<<");
        WriteLine("  /Type /Pages");
        WriteLine("  /Kids []");
        WriteLine("  /Count 0");
        WriteLine(">>");
        EndObject();
    }

    /// <summary>
    /// Writes a page to the PDF.
    /// </summary>
    public void WritePage(PageViewport page)
    {
        // TODO: Implement actual page rendering
        // For now, just a placeholder
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
    public void WriteXRefAndTrailer()
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
        WriteLine("  /Root 1 0 R");
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
