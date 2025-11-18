using Folly.Fonts.Logging;

namespace Folly;

/// <summary>
/// Standard page sizes in points (1 point = 1/72 inch).
/// </summary>
public static class PageSizes
{
    /// <summary>
    /// A4 paper size (210mm × 297mm = 595pt × 842pt).
    /// Standard in most countries outside North America.
    /// </summary>
    public static readonly (double Width, double Height) A4 = (595, 842);

    /// <summary>
    /// US Letter paper size (8.5in × 11in = 612pt × 792pt).
    /// Standard in United States, Canada, and Mexico.
    /// </summary>
    public static readonly (double Width, double Height) Letter = (612, 792);

    /// <summary>
    /// US Legal paper size (8.5in × 14in = 612pt × 1008pt).
    /// </summary>
    public static readonly (double Width, double Height) Legal = (612, 1008);

    /// <summary>
    /// A3 paper size (297mm × 420mm = 842pt × 1191pt).
    /// Twice the size of A4.
    /// </summary>
    public static readonly (double Width, double Height) A3 = (842, 1191);

    /// <summary>
    /// A5 paper size (148mm × 210mm = 420pt × 595pt).
    /// Half the size of A4.
    /// </summary>
    public static readonly (double Width, double Height) A5 = (420, 595);
}

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

    // Knuth-Plass Line Breaking Parameters

    /// <summary>
    /// Gets or sets the space stretch ratio for the Knuth-Plass line breaking algorithm.
    /// This controls how much inter-word spaces can be expanded to fill a line.
    /// Default is 0.5 (TeX default), meaning spaces can stretch up to 50% of their normal width.
    /// Only applies when LineBreaking is set to Optimal.
    /// </summary>
    public double KnuthPlassSpaceStretchRatio { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the space shrink ratio for the Knuth-Plass line breaking algorithm.
    /// This controls how much inter-word spaces can be compressed to fit a line.
    /// Default is 0.333 (TeX default), meaning spaces can shrink up to 33.3% of their normal width.
    /// Only applies when LineBreaking is set to Optimal.
    /// </summary>
    public double KnuthPlassSpaceShrinkRatio { get; set; } = 0.333;

    /// <summary>
    /// Gets or sets the tolerance for line badness in the Knuth-Plass algorithm.
    /// Higher values allow more variation from ideal line width (looser justification).
    /// Lower values enforce stricter justification (may result in overfull/underfull boxes).
    /// Default is 1.0 (TeX default). Range: 0.1 to 10.0.
    /// Only applies when LineBreaking is set to Optimal.
    /// </summary>
    public double KnuthPlassTolerance { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the penalty for each line break in the Knuth-Plass algorithm.
    /// Higher values result in fewer lines (longer paragraphs).
    /// Lower values result in more lines (shorter paragraphs).
    /// Default is 10.0 (TeX default). Range: 0.0 to 100.0.
    /// Only applies when LineBreaking is set to Optimal.
    /// </summary>
    public double KnuthPlassLinePenalty { get; set; } = 10.0;

    /// <summary>
    /// Gets or sets the penalty for consecutive hyphens or very bad lines in the Knuth-Plass algorithm.
    /// Higher values discourage consecutive hyphenated lines.
    /// Default is 100.0 (TeX default). Range: 0.0 to 1000.0.
    /// Only applies when LineBreaking is set to Optimal.
    /// </summary>
    public double KnuthPlassFlaggedDemerit { get; set; } = 100.0;

    /// <summary>
    /// Gets or sets the penalty for fitness class changes in consecutive lines (Knuth-Plass algorithm).
    /// Fitness classes track whether lines are tight, normal, loose, or very loose.
    /// Higher values encourage consistent line spacing throughout the paragraph.
    /// Default is 100.0 (TeX default). Range: 0.0 to 1000.0.
    /// Only applies when LineBreaking is set to Optimal.
    /// </summary>
    public double KnuthPlassFitnessDemerit { get; set; } = 100.0;

    /// <summary>
    /// Gets or sets the hyphen penalty for the Knuth-Plass algorithm.
    /// This is the cost of breaking a word with a hyphen.
    /// Higher values discourage hyphenation.
    /// Default is 50.0 (TeX default). Range: 0.0 to 1000.0.
    /// Only applies when LineBreaking is set to Optimal and EnableHyphenation is true.
    /// </summary>
    public double KnuthPlassHyphenPenalty { get; set; } = 50.0;

    /// <summary>
    /// Gets or sets the logger for layout operations.
    /// Default is NullLogger (no logging). Set to ConsoleLogger or custom ILogger for diagnostic output.
    /// </summary>
    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <summary>
    /// Gets or sets the default page size (width, height) in points when no page master is defined.
    /// Default is A4 (595pt × 842pt). Common alternatives: PageSizes.Letter, PageSizes.Legal, PageSizes.A3, PageSizes.A5.
    /// Only used as a fallback when the FO document doesn't specify page dimensions.
    /// </summary>
    public (double Width, double Height) DefaultPageSize { get; set; } = PageSizes.A4;
}
