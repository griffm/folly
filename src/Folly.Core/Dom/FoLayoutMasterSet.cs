namespace Folly.Dom;

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
    /// Finds a simple page master by name.
    /// </summary>
    public FoSimplePageMaster? FindPageMaster(string masterName)
    {
        return SimplePageMasters.FirstOrDefault(pm =>
            pm.Properties.GetString("master-name") == masterName);
    }
}
