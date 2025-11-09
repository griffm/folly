namespace Folly.Dom;

/// <summary>
/// Represents an fo:list-block element.
/// </summary>
public sealed class FoListBlock : FoElement
{
    /// <inheritdoc/>
    public override string Name => "list-block";

    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<FoListItem> Items { get; init; } = Array.Empty<FoListItem>();

    /// <summary>
    /// Gets the provisional distance between starts.
    /// </summary>
    public double ProvisionalDistanceBetweenStarts => LengthParser.Parse(Properties.GetString("provisional-distance-between-starts", "24pt"));

    /// <summary>
    /// Gets the provisional label separation.
    /// </summary>
    public double ProvisionalLabelSeparation => LengthParser.Parse(Properties.GetString("provisional-label-separation", "6pt"));

    /// <summary>
    /// Gets the space before.
    /// </summary>
    public double SpaceBefore => LengthParser.Parse(Properties.GetString("space-before", "0pt"));

    /// <summary>
    /// Gets the space after.
    /// </summary>
    public double SpaceAfter => LengthParser.Parse(Properties.GetString("space-after", "0pt"));
}

/// <summary>
/// Represents an fo:list-item element.
/// </summary>
public sealed class FoListItem : FoElement
{
    /// <inheritdoc/>
    public override string Name => "list-item";

    /// <summary>
    /// Gets the list item label.
    /// </summary>
    public FoListItemLabel? Label { get; init; }

    /// <summary>
    /// Gets the list item body.
    /// </summary>
    public FoListItemBody? Body { get; init; }

    /// <summary>
    /// Gets the space before.
    /// </summary>
    public double SpaceBefore => LengthParser.Parse(Properties.GetString("space-before", "0pt"));

    /// <summary>
    /// Gets the space after.
    /// </summary>
    public double SpaceAfter => LengthParser.Parse(Properties.GetString("space-after", "0pt"));
}

/// <summary>
/// Represents an fo:list-item-label element.
/// </summary>
public sealed class FoListItemLabel : FoElement
{
    /// <inheritdoc/>
    public override string Name => "list-item-label";

    /// <summary>
    /// Gets the label content blocks.
    /// </summary>
    public IReadOnlyList<FoBlock> Blocks { get; init; } = Array.Empty<FoBlock>();
}

/// <summary>
/// Represents an fo:list-item-body element.
/// </summary>
public sealed class FoListItemBody : FoElement
{
    /// <inheritdoc/>
    public override string Name => "list-item-body";

    /// <summary>
    /// Gets the body content blocks.
    /// </summary>
    public IReadOnlyList<FoBlock> Blocks { get; init; } = Array.Empty<FoBlock>();
}
