namespace Folly;

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
}
