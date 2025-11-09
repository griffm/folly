namespace Folly.Dom;

/// <summary>
/// Represents the fo:float element.
/// A float is pulled from the normal flow and positioned to the side,
/// allowing content to flow around it (e.g., images with text wrapping).
/// </summary>
public sealed class FoFloat : FoElement
{
    /// <inheritdoc/>
    public override string Name => "float";

    /// <summary>
    /// Gets the float position: "start" (left), "end" (right), "before", "left", "right", etc.
    /// Most common values are "start" and "end".
    /// </summary>
    public string Float => Properties.GetString("float", "before");

    /// <summary>
    /// Gets the blocks that make up the float content.
    /// This content will be positioned to the side with text flowing around it.
    /// </summary>
    public IReadOnlyList<FoBlock> Blocks { get; init; } = Array.Empty<FoBlock>();
}
