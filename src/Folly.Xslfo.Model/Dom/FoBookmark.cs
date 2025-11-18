namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:bookmark-tree element.
/// The bookmark tree contains the outline structure that appears in PDF viewers.
/// </summary>
public sealed class FoBookmarkTree : FoElement
{
    /// <inheritdoc/>
    public override string Name => "bookmark-tree";

    /// <summary>
    /// Gets the top-level bookmarks in the tree.
    /// </summary>
    public IReadOnlyList<FoBookmark> Bookmarks { get; init; } = Array.Empty<FoBookmark>();
}

/// <summary>
/// Represents the fo:bookmark element.
/// A bookmark is an entry in the PDF outline that can link to a destination and contain child bookmarks.
/// </summary>
public sealed class FoBookmark : FoElement
{
    /// <inheritdoc/>
    public override string Name => "bookmark";

    /// <summary>
    /// Gets the internal destination (id of target element in the document).
    /// This links the bookmark to a specific location in the PDF.
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
    /// Gets the external destination (URI for external bookmarks).
    /// Rarely used, but allows bookmarks to link to external resources.
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
    /// Gets the bookmark title text.
    /// This is the text that appears in the PDF outline.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the child bookmarks (for nested outline structure).
    /// Allows hierarchical table of contents with expandable sections.
    /// </summary>
    public new IReadOnlyList<FoBookmark> Children { get; init; } = Array.Empty<FoBookmark>();

    /// <summary>
    /// Gets the starting state (show or hide children).
    /// "show" means the bookmark starts expanded, "hide" means collapsed.
    /// Default is "hide".
    /// </summary>
    public string StartingState => Properties.GetString("starting-state", "hide");
}
