namespace Folly.Dom;

/// <summary>
/// Represents the fo:static-content element.
/// Static content is used for repeating content like headers and footers.
/// </summary>
public sealed class FoStaticContent : FoElement
{
    /// <inheritdoc/>
    public override string Name => "static-content";

    /// <summary>
    /// Gets the flow-name that identifies which region this content belongs to.
    /// Common values: xsl-region-before, xsl-region-after, xsl-region-start, xsl-region-end.
    /// </summary>
    public string FlowName => Properties.GetString("flow-name");

    /// <summary>
    /// Gets the block children.
    /// </summary>
    public IReadOnlyList<FoBlock> Blocks { get; init; } = Array.Empty<FoBlock>();

    /// <summary>
    /// Gets the retrieve-marker children.
    /// </summary>
    public IReadOnlyList<FoRetrieveMarker> RetrieveMarkers { get; init; } = Array.Empty<FoRetrieveMarker>();
}
