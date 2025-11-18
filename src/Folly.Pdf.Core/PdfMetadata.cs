namespace Folly.Pdf;

/// <summary>
/// PDF document metadata.
/// </summary>
public sealed class PdfMetadata
{
    /// <summary>
    /// Gets or sets the document title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the document author.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the document subject.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the document keywords.
    /// </summary>
    public string? Keywords { get; set; }

    /// <summary>
    /// Gets or sets the creator application.
    /// </summary>
    public string Creator { get; set; } = "Folly XSL-FO Processor";

    /// <summary>
    /// Gets or sets the producer application.
    /// </summary>
    public string Producer { get; set; } = "Folly";
}
