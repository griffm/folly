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

    /// <summary>
    /// Mapping of font family names to TrueType font file paths.
    /// When a font family is used in the document, if it's found in this dictionary,
    /// the TrueType font will be embedded. Otherwise, fallback to PDF base fonts.
    /// Example: TrueTypeFonts["Roboto"] = "/path/to/Roboto-Regular.ttf"
    /// </summary>
    public Dictionary<string, string> TrueTypeFonts { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to enable automatic font resolution with fallback.
    /// When enabled, font families will be resolved using system fonts if not found in TrueTypeFonts.
    /// Supports font family stacks like "Roboto, Arial, sans-serif".
    /// Default is false for backward compatibility.
    /// </summary>
    public bool EnableFontFallback { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to generate a tagged PDF with structure tree for accessibility.
    /// When enabled, the PDF will include a structure tree that defines logical document structure
    /// (headings, paragraphs, tables, etc.) for screen readers and assistive technologies.
    /// This is foundational for PDF/UA (Universal Accessibility) compliance.
    /// Default is false for backward compatibility.
    /// </summary>
    public bool EnableTaggedPdf { get; set; } = false;

    /// <summary>
    /// Gets or sets the behavior when image decoding fails.
    /// Default is ThrowException to ensure users are aware of image failures.
    /// </summary>
    public ImageErrorBehavior ImageErrorBehavior { get; set; } = ImageErrorBehavior.ThrowException;

    /// <summary>
    /// Gets or sets the maximum memory (in bytes) allowed for font data in memory at once.
    /// This is used to prevent OutOfMemoryException when embedding very large fonts (e.g., CJK fonts >15MB).
    /// When a font exceeds this limit, it will be streamed instead of loaded entirely into memory.
    /// Default is 50MB (52,428,800 bytes).
    /// Set to 0 to disable the limit (not recommended for production use).
    /// </summary>
    public long MaxFontMemory { get; set; } = 50 * 1024 * 1024; // 50MB

    /// <summary>
    /// Gets or sets font caching options for performance optimization.
    /// Controls system font discovery, LRU cache size, scan timeout, and persistent cache.
    /// If null, uses default options (500 font cache, 10 second timeout, persistent cache enabled).
    /// </summary>
    public FontCacheOptions? FontCacheOptions { get; set; }
}

/// <summary>
/// Defines how the PDF generator should behave when image decoding fails.
/// </summary>
public enum ImageErrorBehavior
{
    /// <summary>
    /// Throw an ImageDecodingException with detailed diagnostics.
    /// This is the recommended behavior to ensure users are aware of image failures.
    /// </summary>
    ThrowException,

    /// <summary>
    /// Replace the failed image with a 1x1 white pixel placeholder.
    /// The document will be generated successfully but the image will be missing.
    /// Use with caution as this can result in documents with missing content.
    /// </summary>
    UsePlaceholder,

    /// <summary>
    /// Skip the failed image entirely (do not render it).
    /// The document will be generated successfully but the image will be missing.
    /// Use with caution as this can result in documents with missing content.
    /// </summary>
    SkipImage
}
