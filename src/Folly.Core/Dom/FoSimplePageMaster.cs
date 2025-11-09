namespace Folly.Dom;

/// <summary>
/// Represents the fo:simple-page-master element.
/// </summary>
public sealed class FoSimplePageMaster : FoElement
{
    /// <inheritdoc/>
    public override string Name => "simple-page-master";

    /// <summary>
    /// Gets the master name.
    /// </summary>
    public string MasterName => Properties.GetString("master-name");

    /// <summary>
    /// Gets the page width in points.
    /// </summary>
    public double PageWidth => Properties.GetLength("page-width", 595); // Default A4 width

    /// <summary>
    /// Gets the page height in points.
    /// </summary>
    public double PageHeight => Properties.GetLength("page-height", 842); // Default A4 height

    /// <summary>
    /// Gets the region-body child.
    /// </summary>
    public FoRegionBody? RegionBody { get; init; }

    /// <summary>
    /// Gets the region-before child.
    /// </summary>
    public FoRegion? RegionBefore { get; init; }

    /// <summary>
    /// Gets the region-after child.
    /// </summary>
    public FoRegion? RegionAfter { get; init; }
}
