namespace Folly.Dom;

/// <summary>
/// Represents the fo:page-sequence element.
/// </summary>
public sealed class FoPageSequence : FoElement
{
    /// <inheritdoc/>
    public override string Name => "page-sequence";

    /// <summary>
    /// Gets the master-reference.
    /// </summary>
    public string MasterReference => Properties.GetString("master-reference");

    /// <summary>
    /// Gets the flow child element.
    /// </summary>
    public FoFlow? Flow { get; init; }
}
