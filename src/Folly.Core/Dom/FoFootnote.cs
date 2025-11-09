namespace Folly.Dom;

/// <summary>
/// Represents the fo:footnote element.
/// A footnote is placed inline in the flow, but its body is rendered at the bottom of the page.
/// </summary>
public sealed class FoFootnote : FoElement
{
    /// <inheritdoc/>
    public override string Name => "footnote";

    /// <summary>
    /// Gets the inline text content (footnote reference/marker).
    /// This appears in the main text flow, typically as a superscript number.
    /// </summary>
    public string? InlineText { get; init; }

    /// <summary>
    /// Gets the footnote body containing the actual footnote content.
    /// This is rendered at the bottom of the page.
    /// </summary>
    public FoFootnoteBody? FootnoteBody { get; init; }
}

/// <summary>
/// Represents the fo:footnote-body element.
/// Contains the blocks that make up the footnote content at the bottom of the page.
/// </summary>
public sealed class FoFootnoteBody : FoElement
{
    /// <inheritdoc/>
    public override string Name => "footnote-body";

    /// <summary>
    /// Gets the blocks that make up the footnote content.
    /// </summary>
    public IReadOnlyList<FoBlock> Blocks { get; init; } = Array.Empty<FoBlock>();
}
