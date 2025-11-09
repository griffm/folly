namespace Folly;

/// <summary>
/// Represents an XSL-FO document that can be loaded from XML and rendered to PDF.
/// </summary>
public sealed class FoDocument : IDisposable
{
    private readonly XDocument _foXml;
    private readonly Dom.FoRoot _foRoot;
    private bool _disposed;

    private FoDocument(XDocument foXml, Dom.FoRoot foRoot)
    {
        _foXml = foXml ?? throw new ArgumentNullException(nameof(foXml));
        _foRoot = foRoot ?? throw new ArgumentNullException(nameof(foRoot));
    }

    /// <summary>
    /// Loads an XSL-FO document from a file path.
    /// </summary>
    /// <param name="path">Path to the .fo XML file.</param>
    /// <param name="options">Optional load options.</param>
    /// <returns>A new FoDocument instance.</returns>
    public static FoDocument Load(string path, FoLoadOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var stream = File.OpenRead(path);
        return Load(stream, options);
    }

    /// <summary>
    /// Loads an XSL-FO document from a stream.
    /// </summary>
    /// <param name="xml">Stream containing the XSL-FO XML.</param>
    /// <param name="options">Optional load options.</param>
    /// <returns>A new FoDocument instance.</returns>
    public static FoDocument Load(Stream xml, FoLoadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(xml);

        options ??= new FoLoadOptions();

        var doc = XDocument.Load(xml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);

        // Parse FO DOM
        var foRoot = Dom.FoParser.Parse(doc);

        // TODO: Validate FO structure
        // TODO: Resolve properties and validate

        return new FoDocument(doc, foRoot);
    }

    // SavePdf methods are implemented as extension methods in Folly.Pdf project

    /// <summary>
    /// Builds the area tree from the FO document.
    /// </summary>
    /// <param name="options">Optional layout options.</param>
    /// <returns>The constructed area tree.</returns>
    public AreaTree BuildAreaTree(LayoutOptions? options = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        options ??= new LayoutOptions();

        // Build area tree from FO DOM using layout engine
        var layoutEngine = new Layout.LayoutEngine(options);
        return layoutEngine.Layout(_foRoot);
    }

    /// <summary>
    /// Disposes resources used by the document.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
