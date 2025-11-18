namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:marker element.
/// Markers are used to store content that can be retrieved in static-content areas.
/// Common use case: chapter/section titles in running headers.
/// </summary>
public sealed class FoMarker : FoElement
{
    /// <inheritdoc/>
    public override string Name => "marker";

    /// <summary>
    /// Gets the marker class name that identifies this marker.
    /// </summary>
    public string MarkerClassName => Properties.GetString("marker-class-name");

    /// <summary>
    /// Gets the blocks contained in this marker.
    /// </summary>
    public IReadOnlyList<FoBlock> Blocks { get; init; } = Array.Empty<FoBlock>();
}

/// <summary>
/// Represents the fo:retrieve-marker element.
/// Retrieves and displays content from a marker with the specified class name.
/// </summary>
public sealed class FoRetrieveMarker : FoElement
{
    /// <inheritdoc/>
    public override string Name => "retrieve-marker";

    /// <summary>
    /// Gets the marker class name to retrieve.
    /// </summary>
    public string RetrieveClassName => Properties.GetString("retrieve-class-name");

    /// <summary>
    /// Gets the retrieve boundary (page, page-sequence, document).
    /// Default is page-sequence.
    /// </summary>
    public string RetrieveBoundary => Properties.GetString("retrieve-boundary", "page-sequence");

    /// <summary>
    /// Gets the retrieve position (first-starting-within-page, first-including-carryover,
    /// last-starting-within-page, last-ending-within-page).
    /// Default is first-starting-within-page.
    /// </summary>
    public string RetrievePosition => Properties.GetString("retrieve-position", "first-starting-within-page");
}

/// <summary>
/// Represents the fo:retrieve-table-marker element.
/// Retrieves and displays content from a marker within table context.
/// Table markers have table scope rather than page scope.
/// </summary>
public sealed class FoRetrieveTableMarker : FoElement
{
    /// <inheritdoc/>
    public override string Name => "retrieve-table-marker";

    /// <summary>
    /// Gets the marker class name to retrieve.
    /// </summary>
    public string RetrieveClassName => Properties.GetString("retrieve-class-name");

    /// <summary>
    /// Gets the retrieve boundary for table markers (table, table-fragment, page).
    /// Default is table.
    /// </summary>
    public string RetrieveBoundary => Properties.GetString("retrieve-boundary", "table");

    /// <summary>
    /// Gets the retrieve position within table scope.
    /// Possible values: first-starting, first-including-carryover, last-starting, last-ending.
    /// Default is first-starting.
    /// </summary>
    public string RetrievePosition => Properties.GetString("retrieve-position", "first-starting");

    /// <summary>
    /// Gets the position for inline-level table markers (first-starting, last-starting, last-ending).
    /// Default is first-starting.
    /// </summary>
    public string RetrievePositionWithinTable => Properties.GetString("retrieve-position-within-table", "first-starting");

    /// <summary>
    /// Gets the boundary for inline-level table markers.
    /// Default is table.
    /// </summary>
    public string RetrieveBoundaryWithinTable => Properties.GetString("retrieve-boundary-within-table", "table");
}
