namespace Folly.Dom;

/// <summary>
/// Base class for region elements.
/// </summary>
public abstract class FoRegion : FoElement
{
    /// <summary>
    /// Gets the margin-top in points.
    /// </summary>
    public double MarginTop => Properties.GetLength("margin-top", 0);

    /// <summary>
    /// Gets the margin-bottom in points.
    /// </summary>
    public double MarginBottom => Properties.GetLength("margin-bottom", 0);

    /// <summary>
    /// Gets the margin-left in points.
    /// </summary>
    public double MarginLeft => Properties.GetLength("margin-left", 0);

    /// <summary>
    /// Gets the margin-right in points.
    /// </summary>
    public double MarginRight => Properties.GetLength("margin-right", 0);
}

/// <summary>
/// Represents the fo:region-body element.
/// </summary>
public sealed class FoRegionBody : FoRegion
{
    /// <inheritdoc/>
    public override string Name => "region-body";
}

/// <summary>
/// Represents the fo:region-before element.
/// </summary>
public sealed class FoRegionBefore : FoRegion
{
    /// <inheritdoc/>
    public override string Name => "region-before";

    /// <summary>
    /// Gets the extent (height) of the region.
    /// </summary>
    public double Extent => Properties.GetLength("extent", 36);
}

/// <summary>
/// Represents the fo:region-after element.
/// </summary>
public sealed class FoRegionAfter : FoRegion
{
    /// <inheritdoc/>
    public override string Name => "region-after";

    /// <summary>
    /// Gets the extent (height) of the region.
    /// </summary>
    public double Extent => Properties.GetLength("extent", 36);
}
