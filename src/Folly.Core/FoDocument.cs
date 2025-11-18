using Folly.Layout;

namespace Folly;

/// <summary>
/// Represents an XSL-FO document that can be loaded from XML and rendered to PDF.
/// </summary>
public sealed class FoDocument : IDisposable
{
    private readonly XDocument _foXml;
    private readonly Dom.FoRoot _foRoot;
    private bool _disposed;

    /// <summary>
    /// Gets the parsed FO root element.
    /// </summary>
    public Dom.FoRoot Root => _foRoot;

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

        // Security: Use secure XML reader settings to prevent XXE attacks
        var xmlReaderSettings = new System.Xml.XmlReaderSettings
        {
            DtdProcessing = System.Xml.DtdProcessing.Prohibit, // Disable DTD processing
            XmlResolver = null, // Disable external entity resolution
            MaxCharactersFromEntities = 1024, // Limit entity expansion
            MaxCharactersInDocument = 100_000_000 // 100MB limit for document size
        };

        XDocument doc;
        using (var xmlReader = System.Xml.XmlReader.Create(xml, xmlReaderSettings))
        {
            doc = XDocument.Load(xmlReader, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        }

        // Parse FO DOM
        var foRoot = Dom.FoParser.Parse(doc);

        // Note: FoLoadOptions.ValidateStructure and ValidateProperties are available
        // but not enforced by default for performance. Validation happens during layout
        // phase where errors can be reported with better context. Additional explicit
        // validation can be added here in future versions if needed.

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
        var layoutEngine = new LayoutEngine(options);
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
