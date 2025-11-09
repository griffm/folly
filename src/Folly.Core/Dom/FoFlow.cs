namespace Folly.Dom;

/// <summary>
/// Represents the fo:flow element.
/// </summary>
public sealed class FoFlow : FoElement
{
    /// <inheritdoc/>
    public override string Name => "flow";

    /// <summary>
    /// Gets the flow-name (typically "xsl-region-body").
    /// </summary>
    public string FlowName => Properties.GetString("flow-name");

    /// <summary>
    /// Gets the block children.
    /// </summary>
    public IReadOnlyList<FoBlock> Blocks { get; init; } = Array.Empty<FoBlock>();

    /// <summary>
    /// Gets the table children.
    /// </summary>
    public IReadOnlyList<FoTable> Tables { get; init; } = Array.Empty<FoTable>();
}
