namespace Folly.Dom;

/// <summary>
/// Represents the fo:multi-switch element.
/// Allows selection between multiple cases based on user interaction or other criteria.
/// In static PDF rendering, the first case is selected by default.
/// </summary>
public sealed class FoMultiSwitch : FoElement
{
    /// <inheritdoc/>
    public override string Name => "multi-switch";

    /// <summary>
    /// Gets the auto-restore property (true, false).
    /// Determines whether the multi-switch restores to its initial state.
    /// Default is false.
    /// </summary>
    public bool AutoRestore => Properties.GetString("auto-restore", "false").ToLowerInvariant() == "true";

    /// <summary>
    /// Gets the multi-case children.
    /// </summary>
    public IReadOnlyList<FoMultiCase> MultiCases { get; init; } = Array.Empty<FoMultiCase>();
}

/// <summary>
/// Represents the fo:multi-case element.
/// Defines one of the possible cases within a multi-switch.
/// </summary>
public sealed class FoMultiCase : FoElement
{
    /// <inheritdoc/>
    public override string Name => "multi-case";

    /// <summary>
    /// Gets the starting-state property (show, hide).
    /// Determines the initial visibility of this case.
    /// Default is hide.
    /// </summary>
    public string StartingState => Properties.GetString("starting-state", "hide");

    /// <summary>
    /// Gets the case-name property.
    /// Identifies this case for selection purposes.
    /// </summary>
    public string CaseName => Properties.GetString("case-name", "");

    /// <summary>
    /// Gets the case-title property.
    /// Provides a title for this case.
    /// </summary>
    public string CaseTitle => Properties.GetString("case-title", "");

    /// <summary>
    /// Gets the block children of this case.
    /// </summary>
    public IReadOnlyList<FoBlock> Blocks { get; init; } = Array.Empty<FoBlock>();
}

/// <summary>
/// Represents the fo:multi-toggle element.
/// Allows toggling between cases within a multi-switch.
/// In static PDF rendering, this element is ignored.
/// </summary>
public sealed class FoMultiToggle : FoElement
{
    /// <inheritdoc/>
    public override string Name => "multi-toggle";

    /// <summary>
    /// Gets the switch-to property.
    /// Specifies which case to switch to (case-name, xf-next, xf-previous).
    /// </summary>
    public string SwitchTo => Properties.GetString("switch-to", "xf-next");

    /// <summary>
    /// Gets the inline children of this toggle.
    /// </summary>
    public IReadOnlyList<FoInline> Inlines { get; init; } = Array.Empty<FoInline>();
}

/// <summary>
/// Represents the fo:multi-properties element.
/// Allows selection between different sets of formatting properties.
/// In static PDF rendering, the first property set is selected by default.
/// </summary>
public sealed class FoMultiProperties : FoElement
{
    /// <inheritdoc/>
    public override string Name => "multi-properties";

    /// <summary>
    /// Gets the multi-property-set children.
    /// </summary>
    public IReadOnlyList<FoMultiPropertySet> MultiPropertySets { get; init; } = Array.Empty<FoMultiPropertySet>();

    /// <summary>
    /// Gets the wrapper element that receives the selected properties.
    /// This is typically a block or inline element.
    /// </summary>
    public FoElement? Wrapper { get; init; }
}

/// <summary>
/// Represents the fo:multi-property-set element.
/// Defines a set of formatting properties that can be selected.
/// </summary>
public sealed class FoMultiPropertySet : FoElement
{
    /// <inheritdoc/>
    public override string Name => "multi-property-set";

    /// <summary>
    /// Gets the active-state property.
    /// Specifies link or visited state for property selection.
    /// </summary>
    public string ActiveState => Properties.GetString("active-state", "link");
}
