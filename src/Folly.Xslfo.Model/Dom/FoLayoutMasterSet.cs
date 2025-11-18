namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:layout-master-set element.
/// </summary>
public sealed class FoLayoutMasterSet : FoElement
{
    /// <inheritdoc/>
    public override string Name => "layout-master-set";

    /// <summary>
    /// Gets the simple-page-master children.
    /// </summary>
    public IReadOnlyList<FoSimplePageMaster> SimplePageMasters { get; init; } = Array.Empty<FoSimplePageMaster>();

    /// <summary>
    /// Gets the page-sequence-master children.
    /// </summary>
    public IReadOnlyList<FoPageSequenceMaster> PageSequenceMasters { get; init; } = Array.Empty<FoPageSequenceMaster>();

    /// <summary>
    /// Finds a simple page master by name.
    /// </summary>
    public FoSimplePageMaster? FindPageMaster(string masterName)
    {
        return SimplePageMasters.FirstOrDefault(pm =>
            pm.Properties.GetString("master-name") == masterName);
    }

    /// <summary>
    /// Finds a page sequence master by name.
    /// </summary>
    public FoPageSequenceMaster? FindPageSequenceMaster(string masterName)
    {
        return PageSequenceMasters.FirstOrDefault(pm => pm.MasterName == masterName);
    }
}
