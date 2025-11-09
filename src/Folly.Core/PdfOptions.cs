namespace Folly;

/// <summary>
/// Options for PDF rendering.
/// </summary>
public sealed class PdfOptions
{
    /// <summary>
    /// Gets or sets the PDF version. Currently only 1.7 is supported.
    /// </summary>
    public string PdfVersion { get; set; } = "1.7";

    /// <summary>
    /// Gets or sets whether to embed fonts.
    /// Default is true.
    /// </summary>
    public bool EmbedFonts { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to subset embedded fonts.
    /// Default is true.
    /// </summary>
    public bool SubsetFonts { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to compress PDF streams using Flate compression.
    /// Default is true for optimal file size.
    /// </summary>
    public bool CompressStreams { get; set; } = true;

    /// <summary>
    /// Gets or sets document metadata.
    /// </summary>
    public PdfMetadata Metadata { get; set; } = new();
}
