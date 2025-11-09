namespace Folly.Dom;

/// <summary>
/// Represents the fo:page-sequence-master element.
/// Defines conditional page master selection for different page positions.
/// </summary>
public sealed class FoPageSequenceMaster : FoElement
{
    /// <inheritdoc/>
    public override string Name => "page-sequence-master";

    /// <summary>
    /// Gets the master name.
    /// </summary>
    public string MasterName => Properties.GetString("master-name");

    /// <summary>
    /// Gets the single-page-master-reference (for simple cases).
    /// </summary>
    public FoSinglePageMasterReference? SinglePageMasterReference { get; init; }

    /// <summary>
    /// Gets the repeatable-page-master-reference (for repeating a single master).
    /// </summary>
    public FoRepeatablePageMasterReference? RepeatablePageMasterReference { get; init; }

    /// <summary>
    /// Gets the repeatable-page-master-alternatives.
    /// </summary>
    public FoRepeatablePageMasterAlternatives? RepeatablePageMasterAlternatives { get; init; }
}

/// <summary>
/// Represents the fo:single-page-master-reference element.
/// References a single page master for all pages.
/// </summary>
public sealed class FoSinglePageMasterReference : FoElement
{
    /// <inheritdoc/>
    public override string Name => "single-page-master-reference";

    /// <summary>
    /// Gets the master reference.
    /// </summary>
    public string MasterReference => Properties.GetString("master-reference");
}

/// <summary>
/// Represents the fo:repeatable-page-master-reference element.
/// References a page master that repeats for all remaining pages.
/// </summary>
public sealed class FoRepeatablePageMasterReference : FoElement
{
    /// <inheritdoc/>
    public override string Name => "repeatable-page-master-reference";

    /// <summary>
    /// Gets the master reference.
    /// </summary>
    public string MasterReference => Properties.GetString("master-reference");

    /// <summary>
    /// Gets the maximum-repeats value (default is "no-limit").
    /// </summary>
    public string MaximumRepeats => Properties.GetString("maximum-repeats", "no-limit");
}

/// <summary>
/// Represents the fo:repeatable-page-master-alternatives element.
/// Contains conditional page master references.
/// </summary>
public sealed class FoRepeatablePageMasterAlternatives : FoElement
{
    /// <inheritdoc/>
    public override string Name => "repeatable-page-master-alternatives";

    /// <summary>
    /// Gets the conditional page master references.
    /// </summary>
    public IReadOnlyList<FoConditionalPageMasterReference> ConditionalPageMasterReferences { get; init; }
        = Array.Empty<FoConditionalPageMasterReference>();
}

/// <summary>
/// Represents the fo:conditional-page-master-reference element.
/// Selects a page master based on page position (first, odd, even, last).
/// </summary>
public sealed class FoConditionalPageMasterReference : FoElement
{
    /// <inheritdoc/>
    public override string Name => "conditional-page-master-reference";

    /// <summary>
    /// Gets the master reference.
    /// </summary>
    public string MasterReference => Properties.GetString("master-reference");

    /// <summary>
    /// Gets the page position (first, last, rest, any).
    /// </summary>
    public string PagePosition => Properties.GetString("page-position", "any");

    /// <summary>
    /// Gets the odd-or-even property (odd, even, any).
    /// </summary>
    public string OddOrEven => Properties.GetString("odd-or-even", "any");

    /// <summary>
    /// Gets the blank-or-not-blank property (blank, not-blank, any).
    /// </summary>
    public string BlankOrNotBlank => Properties.GetString("blank-or-not-blank", "any");
}
