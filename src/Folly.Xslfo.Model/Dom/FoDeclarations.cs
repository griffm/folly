namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:declarations element.
/// The declarations element contains document-level metadata and other declarations.
/// </summary>
public sealed class FoDeclarations : FoElement
{
    /// <inheritdoc/>
    public override string Name => "declarations";

    /// <summary>
    /// Gets the document information/metadata.
    /// </summary>
    public FoInfo? Info { get; init; }
}

/// <summary>
/// Represents document metadata within fo:declarations.
/// This information is typically rendered into the PDF Document Information Dictionary.
/// Note: XSL-FO 1.1 does not define a standard metadata element, so this uses a common
/// extension pattern with child elements for title, author, subject, and keywords.
/// </summary>
public sealed class FoInfo : FoElement
{
    /// <inheritdoc/>
    public override string Name => "info";

    /// <summary>
    /// Gets the document title.
    /// Maps to /Title in PDF Document Information Dictionary.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the document author.
    /// Maps to /Author in PDF Document Information Dictionary.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Gets the document subject.
    /// Maps to /Subject in PDF Document Information Dictionary.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Gets the document keywords.
    /// Maps to /Keywords in PDF Document Information Dictionary.
    /// </summary>
    public string? Keywords { get; init; }

    /// <summary>
    /// Gets the application that created the document.
    /// Maps to /Creator in PDF Document Information Dictionary.
    /// </summary>
    public string? Creator { get; init; }
}
