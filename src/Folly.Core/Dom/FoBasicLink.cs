namespace Folly.Dom;

/// <summary>
/// Represents the fo:basic-link element.
/// A basic link creates a clickable area that can link to internal destinations (within the document)
/// or external destinations (URIs like http://, mailto:, etc.).
/// </summary>
public sealed class FoBasicLink : FoElement
{
    /// <inheritdoc/>
    public override string Name => "basic-link";

    /// <summary>
    /// Gets the internal destination (id of target element in the document).
    /// Used for internal links like table of contents, cross-references, etc.
    /// </summary>
    public string? InternalDestination
    {
        get
        {
            var value = Properties.GetString("internal-destination", "");
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }

    /// <summary>
    /// Gets the external destination (URI for external links).
    /// Examples: "http://example.com", "mailto:user@example.com", "file:///path/to/file.pdf"
    /// </summary>
    public string? ExternalDestination
    {
        get
        {
            var value = Properties.GetString("external-destination", "");
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }

    /// <summary>
    /// Gets the show-destination property (replace, new).
    /// Determines how the link opens: "replace" opens in same window, "new" opens in new window.
    /// Default is "replace".
    /// </summary>
    public string ShowDestination => Properties.GetString("show-destination", "replace");

    /// <summary>
    /// Gets the color of the link text.
    /// Default is blue for visibility.
    /// </summary>
    public string Color => Properties.GetString("color", "blue");

    /// <summary>
    /// Gets the text decoration (none, underline, overline, line-through).
    /// Default is underline for link visibility.
    /// </summary>
    public string TextDecoration => Properties.GetString("text-decoration", "underline");
}
