namespace Folly;

/// <summary>
/// Line breaking algorithm selection.
/// </summary>
public enum LineBreakingAlgorithm
{
    /// <summary>
    /// Fast greedy (first-fit) algorithm. Single-pass, O(n) complexity.
    /// Default for best performance.
    /// </summary>
    Greedy,

    /// <summary>
    /// Optimal Knuth-Plass algorithm from TeX. Multi-pass with dynamic programming, O(n²) complexity.
    /// Produces better-quality line breaks by minimizing total badness across the paragraph.
    /// Recommended for high-quality typography where rendering time is less critical.
    /// </summary>
    Optimal
}

/// <summary>
/// Options for the layout engine.
/// </summary>
public sealed class LayoutOptions
{
    /// <summary>
    /// Gets or sets whether to enable strict layout validation.
    /// Default is true.
    /// </summary>
    public bool StrictLayout { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of layout iterations for complex cases.
    /// Default is 10.
    /// </summary>
    public int MaxIterations { get; set; } = 10;

    /// <summary>
    /// Gets or sets the line breaking algorithm to use.
    /// Default is Greedy for best performance. Set to Optimal for TeX-quality line breaking.
    /// </summary>
    public LineBreakingAlgorithm LineBreaking { get; set; } = LineBreakingAlgorithm.Greedy;

    /// <summary>
    /// Gets or sets the allowed base path for loading external images.
    /// If set, all image paths must be within this directory (prevents path traversal attacks).
    /// If null, image loading is unrestricted (not recommended for untrusted input).
    /// </summary>
    public string? AllowedImageBasePath { get; set; }

    /// <summary>
    /// Gets or sets whether to allow absolute image paths.
    /// Default is false for security (relative paths only).
    /// </summary>
    public bool AllowAbsoluteImagePaths { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of pages that can be generated.
    /// Default is 10000. Set to prevent DoS attacks with infinite page generation.
    /// </summary>
    public int MaxPages { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum image size in bytes that can be loaded.
    /// Default is 50MB. Set to prevent DoS attacks with huge images.
    /// </summary>
    public long MaxImageSizeBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum number of cells in a table.
    /// Default is 100000. Set to prevent DoS attacks with huge tables.
    /// </summary>
    public int MaxTableCells { get; set; } = 100000;

    /// <summary>
    /// Gets or sets the maximum nesting depth for elements.
    /// Default is 100. Set to prevent stack overflow from deeply nested structures.
    /// </summary>
    public int MaxNestingDepth { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to enable automatic hyphenation.
    /// Default is false. When enabled, words will be hyphenated according to language rules.
    /// </summary>
    public bool EnableHyphenation { get; set; } = false;

    /// <summary>
    /// Gets or sets the language code for hyphenation.
    /// Default is "en-US". Supported values: "en-US", "de-DE", "fr-FR", "es-ES".
    /// </summary>
    public string HyphenationLanguage { get; set; } = "en-US";

    /// <summary>
    /// Gets or sets the minimum word length for hyphenation.
    /// Default is 5. Words shorter than this will not be hyphenated.
    /// </summary>
    public int HyphenationMinWordLength { get; set; } = 5;

    /// <summary>
    /// Gets or sets the minimum number of characters before a hyphen.
    /// Default is 2.
    /// </summary>
    public int HyphenationMinLeftChars { get; set; } = 2;

    /// <summary>
    /// Gets or sets the minimum number of characters after a hyphen.
    /// Default is 3.
    /// </summary>
    public int HyphenationMinRightChars { get; set; } = 3;

    /// <summary>
    /// Gets or sets the hyphenation character to use.
    /// Default is '-' (regular hyphen). Can be set to '\u00AD' (soft hyphen) for PDF.
    /// </summary>
    public char HyphenationCharacter { get; set; } = '-';

    /// <summary>
    /// Gets or sets the default DPI (dots per inch) to use for images that don't specify resolution metadata.
    /// Common values: 72 (screen/PDF standard), 96 (Windows default), 150 (draft print), 300 (print quality).
    /// Default is 72 DPI, which is the PDF standard for images without embedded DPI metadata.
    /// </summary>
    public double DefaultImageDpi { get; set; } = 72.0;
}
