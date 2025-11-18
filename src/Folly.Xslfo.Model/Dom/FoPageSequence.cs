namespace Folly.Xslfo;

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
    /// Gets the static-content elements.
    /// </summary>
    public IReadOnlyList<FoStaticContent> StaticContents { get; init; } = Array.Empty<FoStaticContent>();

    /// <summary>
    /// Gets the flow child element.
    /// </summary>
    public FoFlow? Flow { get; init; }

    /// <summary>
    /// Gets the force-page-count property (auto, even, odd, end-on-even, end-on-odd, no-force).
    /// Determines whether the page sequence should end on an even or odd page by adding blank pages if necessary.
    /// Default is "auto".
    /// </summary>
    public string ForcePageCount => Properties.GetString("force-page-count", "auto");
}
