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
    /// Gets the page-sequence children.
    /// </summary>
    public IReadOnlyList<FoPageSequence> PageSequences { get; init; } = Array.Empty<FoPageSequence>();
}
