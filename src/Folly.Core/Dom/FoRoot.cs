namespace Folly.Dom;

/// <summary>
/// Represents the fo:root element.
/// </summary>
public sealed class FoRoot : FoElement
{
    /// <inheritdoc/>
    public override string Name => "root";

    /// <summary>
    /// Gets the layout-master-set child.
    /// </summary>
    public FoLayoutMasterSet? LayoutMasterSet { get; init; }

    /// <summary>
    /// Gets the declarations element (document metadata and other declarations).
    /// </summary>
    public FoDeclarations? Declarations { get; init; }

    /// <summary>
    /// Gets the bookmark-tree (PDF outline structure).
    /// </summary>
    public FoBookmarkTree? BookmarkTree { get; init; }

    /// <summary>
    /// Gets the page-sequence children.
    /// </summary>
    public IReadOnlyList<FoPageSequence> PageSequences { get; init; } = Array.Empty<FoPageSequence>();
}
