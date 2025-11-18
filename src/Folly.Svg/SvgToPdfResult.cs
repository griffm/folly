namespace Folly.Svg;

/// <summary>
/// Represents the result of converting an SVG document to PDF.
/// Contains both the content stream (drawing commands) and resource definitions
/// (gradients, patterns, images) that must be added to the PDF Resources dictionary.
/// </summary>
public sealed class SvgToPdfResult
{
    /// <summary>
    /// Gets the PDF content stream (drawing commands).
    /// This goes into the PDF page's content stream.
    /// </summary>
    public required string ContentStream { get; init; }

    /// <summary>
    /// Gets the shading definitions (gradients) that need to be added to the PDF Resources.
    /// Keys are shading names (e.g., "Shading1"), values are PDF shading dictionaries.
    /// </summary>
    public Dictionary<string, string> Shadings { get; init; } = new();

    /// <summary>
    /// Gets the pattern definitions that need to be added to the PDF Resources.
    /// Keys are pattern names (e.g., "Pattern1"), values are PDF pattern dictionaries.
    /// </summary>
    public Dictionary<string, string> Patterns { get; init; } = new();

    /// <summary>
    /// Gets the XObject definitions (images, forms) that need to be added to the PDF Resources.
    /// Keys are XObject names (e.g., "Image1"), values are XObject data.
    /// </summary>
    public Dictionary<string, byte[]> XObjects { get; init; } = new();

    /// <summary>
    /// Gets the graphics state definitions that need to be added to the PDF Resources.
    /// Keys are ExtGState names (e.g., "GS1"), values are graphics state dictionaries.
    /// </summary>
    public Dictionary<string, string> GraphicsStates { get; init; } = new();
}
