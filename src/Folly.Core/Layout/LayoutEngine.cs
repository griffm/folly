namespace Folly.Layout;

/// <summary>
/// Layout engine that transforms FO DOM into Area Tree.
/// </summary>
internal sealed class LayoutEngine
{
    // Constants
    private const double MinimumColumnWidth = 50.0; // Minimum column width in points
    private const double Epsilon = 1e-10; // Tolerance for floating-point comparisons
    private const double MinimumFontSize = 1.0; // Minimum valid font size in points
    private const double DefaultFontSize = 12.0; // Default font size when invalid

    private readonly LayoutOptions _options;
    private readonly Dictionary<string, List<(int PageNumber, int Sequence, Dom.FoMarker Marker)>> _markers = new();
    private readonly Dictionary<string, int> _markerSequenceCounters = new();
    private readonly Dictionary<string, List<(int Sequence, Dom.FoMarker Marker)>> _tableMarkers = new();
    private readonly Dictionary<string, int> _tableMarkerSequenceCounters = new();
    private readonly List<Dom.FoFootnote> _currentPageFootnotes = new();
    private readonly List<Dom.FoFloat> _currentPageFloats = new();
    private readonly List<LinkArea> _currentPageLinks = new();
    private Core.Hyphenation.HyphenationEngine? _hyphenationEngine;

    // Index tracking
    private readonly Dictionary<string, List<IndexEntry>> _indexEntries = new();
    private readonly Dictionary<string, IndexRangeInfo> _activeIndexRanges = new();
    private int _currentPageNumber = 0;

    public LayoutEngine(LayoutOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Performs layout and generates the area tree.
    /// </summary>
    public AreaTree Layout(Dom.FoRoot foRoot)
    {
        ArgumentNullException.ThrowIfNull(foRoot);

        var areaTree = new AreaTree();

        // Process each page sequence
        if (foRoot.PageSequences != null)
        {
            foreach (var pageSequence in foRoot.PageSequences)
            {
                LayoutPageSequence(areaTree, foRoot, pageSequence);
            }
        }

        return areaTree;
    }

    private void LayoutPageSequence(AreaTree areaTree, Dom.FoRoot foRoot, Dom.FoPageSequence pageSequence)
    {
        // Get the flow
        var flow = pageSequence.Flow;
        if (flow == null)
            return;

        // Remember the page count before this sequence
        int pageCountBeforeSequence = areaTree.Pages.Count;

        // Layout the flow content, creating pages as needed
        // Pass foRoot to allow dynamic page master selection
        LayoutFlowWithPagination(areaTree, foRoot, pageSequence);

        // Apply force-page-count if specified
        ApplyForcePageCount(areaTree, foRoot, pageSequence, pageCountBeforeSequence);
    }

    /// <summary>
    /// Applies the force-page-count property to ensure the page sequence ends on the correct parity.
    /// Adds blank pages if necessary to satisfy even/odd requirements.
    /// </summary>
    private void ApplyForcePageCount(AreaTree areaTree, Dom.FoRoot foRoot, Dom.FoPageSequence pageSequence, int pageCountBeforeSequence)
    {
        var forcePageCount = pageSequence.ForcePageCount.ToLowerInvariant();

        if (forcePageCount == "auto" || forcePageCount == "no-force" || areaTree.Pages.Count == 0)
            return;

        var currentPageCount = areaTree.Pages.Count;
        var lastPageNumber = currentPageCount;
        var isLastPageEven = lastPageNumber % 2 == 0;

        bool needsBlankPage = false;

        switch (forcePageCount)
        {
            case "even":
                // End on an even page
                needsBlankPage = !isLastPageEven;
                break;

            case "odd":
                // End on an odd page
                needsBlankPage = isLastPageEven;
                break;

            case "end-on-even":
                // Same as "even" - end on an even page
                needsBlankPage = !isLastPageEven;
                break;

            case "end-on-odd":
                // Same as "odd" - end on an odd page
                needsBlankPage = isLastPageEven;
                break;
        }

        if (needsBlankPage)
        {
            // Add a blank page to satisfy the force-page-count requirement
            var pageMaster = SelectPageMaster(foRoot, pageSequence, lastPageNumber + 1, lastPageNumber + 1);
            var blankPage = CreatePage(pageMaster, pageSequence, lastPageNumber + 1);
            areaTree.AddPage(blankPage);
        }
    }

    private PageViewport CreatePage(Dom.FoSimplePageMaster pageMaster, Dom.FoPageSequence pageSequence, int pageNumber)
    {
        var page = new PageViewport
        {
            Width = pageMaster.PageWidth,
            Height = pageMaster.PageHeight,
            PageNumber = pageNumber
        };

        // Add static content for headers and footers
        AddStaticContent(page, pageMaster, pageSequence, pageNumber);

        return page;
    }

    private void AddStaticContent(PageViewport page, Dom.FoSimplePageMaster pageMaster, Dom.FoPageSequence pageSequence, int pageNumber)
    {
        foreach (var staticContent in pageSequence.StaticContents)
        {
            var flowName = staticContent.FlowName;

            if (flowName == "xsl-region-before" && pageMaster.RegionBefore != null)
            {
                // Layout content in header region
                var extent = (pageMaster.RegionBefore as Dom.FoRegionBefore)?.Extent ?? 36;
                var y = pageMaster.MarginTop;
                var x = pageMaster.MarginLeft + pageMaster.RegionBefore.MarginLeft;
                var width = pageMaster.PageWidth - pageMaster.MarginLeft - pageMaster.MarginRight - pageMaster.RegionBefore.MarginLeft - pageMaster.RegionBefore.MarginRight;

                foreach (var block in staticContent.Blocks)
                {
                    var blockArea = LayoutBlock(block, x, y, width, pageNumber);
                    if (blockArea != null)
                    {
                        page.AddArea(blockArea);
                        y += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                    }
                }

                // Handle retrieve-marker elements
                foreach (var retrieveMarker in staticContent.RetrieveMarkers)
                {
                    var markerBlocks = RetrieveMarkerContent(retrieveMarker, pageNumber);
                    foreach (var block in markerBlocks)
                    {
                        var blockArea = LayoutBlock(block, x, y, width, pageNumber);
                        if (blockArea != null)
                        {
                            page.AddArea(blockArea);
                            y += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                        }
                    }
                }
            }
            else if (flowName == "xsl-region-after" && pageMaster.RegionAfter != null)
            {
                // Layout content in footer region
                var extent = (pageMaster.RegionAfter as Dom.FoRegionAfter)?.Extent ?? 36;
                var y = pageMaster.PageHeight - pageMaster.MarginBottom - extent;
                var x = pageMaster.MarginLeft + pageMaster.RegionAfter.MarginLeft;
                var width = pageMaster.PageWidth - pageMaster.MarginLeft - pageMaster.MarginRight - pageMaster.RegionAfter.MarginLeft - pageMaster.RegionAfter.MarginRight;

                foreach (var block in staticContent.Blocks)
                {
                    var blockArea = LayoutBlock(block, x, y, width, pageNumber);
                    if (blockArea != null)
                    {
                        page.AddArea(blockArea);
                        y += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                    }
                }

                // Handle retrieve-marker elements
                foreach (var retrieveMarker in staticContent.RetrieveMarkers)
                {
                    var markerBlocks = RetrieveMarkerContent(retrieveMarker, pageNumber);
                    foreach (var block in markerBlocks)
                    {
                        var blockArea = LayoutBlock(block, x, y, width, pageNumber);
                        if (blockArea != null)
                        {
                            page.AddArea(blockArea);
                            y += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                        }
                    }
                }
            }
            else if (flowName == "xsl-region-start" && pageMaster.RegionStart != null)
            {
                // Layout content in left sidebar region
                var extent = (pageMaster.RegionStart as Dom.FoRegionStart)?.Extent ?? 36;
                var regionBeforeExtent = pageMaster.RegionBefore != null ?
                    ((pageMaster.RegionBefore as Dom.FoRegionBefore)?.Extent ?? 36) : 0;
                var regionAfterExtent = pageMaster.RegionAfter != null ?
                    ((pageMaster.RegionAfter as Dom.FoRegionAfter)?.Extent ?? 36) : 0;

                var x = pageMaster.MarginLeft + pageMaster.RegionStart.MarginLeft;
                var y = pageMaster.MarginTop + regionBeforeExtent + pageMaster.RegionStart.MarginTop;
                var width = extent - pageMaster.RegionStart.MarginLeft - pageMaster.RegionStart.MarginRight;
                var availableHeight = pageMaster.PageHeight - pageMaster.MarginTop - pageMaster.MarginBottom -
                                    regionBeforeExtent - regionAfterExtent -
                                    pageMaster.RegionStart.MarginTop - pageMaster.RegionStart.MarginBottom;

                foreach (var block in staticContent.Blocks)
                {
                    var blockArea = LayoutBlock(block, x, y, width, pageNumber);
                    if (blockArea != null)
                    {
                        page.AddArea(blockArea);
                        y += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                    }
                }

                // Handle retrieve-marker elements
                foreach (var retrieveMarker in staticContent.RetrieveMarkers)
                {
                    var markerBlocks = RetrieveMarkerContent(retrieveMarker, pageNumber);
                    foreach (var block in markerBlocks)
                    {
                        var blockArea = LayoutBlock(block, x, y, width, pageNumber);
                        if (blockArea != null)
                        {
                            page.AddArea(blockArea);
                            y += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                        }
                    }
                }
            }
            else if (flowName == "xsl-region-end" && pageMaster.RegionEnd != null)
            {
                // Layout content in right sidebar region
                var extent = (pageMaster.RegionEnd as Dom.FoRegionEnd)?.Extent ?? 36;
                var regionBeforeExtent = pageMaster.RegionBefore != null ?
                    ((pageMaster.RegionBefore as Dom.FoRegionBefore)?.Extent ?? 36) : 0;
                var regionAfterExtent = pageMaster.RegionAfter != null ?
                    ((pageMaster.RegionAfter as Dom.FoRegionAfter)?.Extent ?? 36) : 0;

                var x = pageMaster.PageWidth - pageMaster.MarginRight - extent + pageMaster.RegionEnd.MarginLeft;
                var y = pageMaster.MarginTop + regionBeforeExtent + pageMaster.RegionEnd.MarginTop;
                var width = extent - pageMaster.RegionEnd.MarginLeft - pageMaster.RegionEnd.MarginRight;
                var availableHeight = pageMaster.PageHeight - pageMaster.MarginTop - pageMaster.MarginBottom -
                                    regionBeforeExtent - regionAfterExtent -
                                    pageMaster.RegionEnd.MarginTop - pageMaster.RegionEnd.MarginBottom;

                foreach (var block in staticContent.Blocks)
                {
                    var blockArea = LayoutBlock(block, x, y, width, pageNumber);
                    if (blockArea != null)
                    {
                        page.AddArea(blockArea);
                        y += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                    }
                }

                // Handle retrieve-marker elements
                foreach (var retrieveMarker in staticContent.RetrieveMarkers)
                {
                    var markerBlocks = RetrieveMarkerContent(retrieveMarker, pageNumber);
                    foreach (var block in markerBlocks)
                    {
                        var blockArea = LayoutBlock(block, x, y, width, pageNumber);
                        if (blockArea != null)
                        {
                            page.AddArea(blockArea);
                            y += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Retrieves marker content based on the specified retrieve position.
    /// Implements all XSL-FO 1.1 marker retrieve positions:
    /// - first-starting-within-page: First marker that starts on this page
    /// - first-including-carryover: First marker on this page, or last marker from previous pages if none on current page
    /// - last-starting-within-page: Last marker that starts on this page
    /// - last-ending-within-page: Last marker on or before this page (same as last-starting for point markers)
    /// </summary>
    private IReadOnlyList<Dom.FoBlock> RetrieveMarkerContent(Dom.FoRetrieveMarker retrieveMarker, int pageNumber)
    {
        var className = retrieveMarker.RetrieveClassName;
        if (string.IsNullOrEmpty(className) || !_markers.ContainsKey(className))
            return Array.Empty<Dom.FoBlock>();

        var markersForClass = _markers[className];
        var position = retrieveMarker.RetrievePosition;
        var boundary = retrieveMarker.RetrieveBoundary; // page, page-sequence, or document

        Dom.FoMarker? selectedMarker = null;

        switch (position)
        {
            case "first-starting-within-page":
                // Get the first marker that starts on this page (by sequence order)
                selectedMarker = markersForClass
                    .Where(m => m.PageNumber == pageNumber)
                    .OrderBy(m => m.Sequence)
                    .FirstOrDefault().Marker;
                break;

            case "first-including-carryover":
                // Get the first marker on this page, or if none exists, the last marker from previous pages
                var firstOnPage = markersForClass
                    .Where(m => m.PageNumber == pageNumber)
                    .OrderBy(m => m.Sequence)
                    .FirstOrDefault();

                if (firstOnPage != default)
                {
                    selectedMarker = firstOnPage.Marker;
                }
                else
                {
                    // No marker on current page, get the last marker from previous pages (carryover)
                    selectedMarker = markersForClass
                        .Where(m => m.PageNumber < pageNumber)
                        .OrderByDescending(m => m.PageNumber)
                        .ThenByDescending(m => m.Sequence)
                        .FirstOrDefault().Marker;
                }
                break;

            case "last-starting-within-page":
                // Get the last marker that starts on this page (by sequence order)
                selectedMarker = markersForClass
                    .Where(m => m.PageNumber == pageNumber)
                    .OrderByDescending(m => m.Sequence)
                    .FirstOrDefault().Marker;
                break;

            case "last-ending-within-page":
                // For point markers (which don't span pages), this is the same as last-starting-within-page
                // Get the last marker on or before this page
                selectedMarker = markersForClass
                    .Where(m => m.PageNumber <= pageNumber)
                    .OrderByDescending(m => m.PageNumber)
                    .ThenByDescending(m => m.Sequence)
                    .FirstOrDefault().Marker;
                break;

            default:
                // Default to first-starting-within-page if position is unrecognized
                selectedMarker = markersForClass
                    .Where(m => m.PageNumber == pageNumber)
                    .OrderBy(m => m.Sequence)
                    .FirstOrDefault().Marker;
                break;
        }

        return selectedMarker?.Blocks ?? Array.Empty<Dom.FoBlock>();
    }

    /// <summary>
    /// Retrieves table marker content based on retrieve-table-marker properties.
    /// Table markers have table scope rather than page scope.
    /// Supported retrieve positions:
    /// - first-starting: First marker in the table
    /// - first-including-carryover: First marker in the table (same as first-starting for tables)
    /// - last-starting: Last marker in the table
    /// - last-ending: Last marker in the table (same as last-starting for tables)
    /// </summary>
    private IReadOnlyList<Dom.FoBlock> RetrieveTableMarkerContent(Dom.FoRetrieveTableMarker retrieveMarker)
    {
        var className = retrieveMarker.RetrieveClassName;
        if (string.IsNullOrEmpty(className) || !_tableMarkers.ContainsKey(className))
            return Array.Empty<Dom.FoBlock>();

        var markersForClass = _tableMarkers[className];
        var position = retrieveMarker.RetrievePosition;

        Dom.FoMarker? selectedMarker = null;

        switch (position)
        {
            case "first-starting":
            case "first-including-carryover":
                // Get the first marker in the table (by sequence order)
                selectedMarker = markersForClass
                    .OrderBy(m => m.Sequence)
                    .FirstOrDefault().Marker;
                break;

            case "last-starting":
            case "last-ending":
                // Get the last marker in the table (by sequence order)
                selectedMarker = markersForClass
                    .OrderByDescending(m => m.Sequence)
                    .FirstOrDefault().Marker;
                break;
        }

        return selectedMarker?.Blocks ?? Array.Empty<Dom.FoBlock>();
    }

    private Dom.FoSimplePageMaster SelectPageMaster(Dom.FoRoot foRoot, Dom.FoPageSequence pageSequence, int pageNumber, int totalPages)
    {
        var masterRef = pageSequence.MasterReference;

        // First try to find a simple-page-master directly
        var simpleMaster = foRoot.LayoutMasterSet?.FindPageMaster(masterRef);
        if (simpleMaster != null)
            return simpleMaster;

        // Then try to find a page-sequence-master
        var pageSequenceMaster = foRoot.LayoutMasterSet?.FindPageSequenceMaster(masterRef);
        if (pageSequenceMaster != null)
        {
            // Use repeatable-page-master-alternatives to select based on conditions
            if (pageSequenceMaster.RepeatablePageMasterAlternatives != null)
            {
                var alternatives = pageSequenceMaster.RepeatablePageMasterAlternatives;
                foreach (var conditionalRef in alternatives.ConditionalPageMasterReferences)
                {
                    if (MatchesConditions(conditionalRef, pageNumber, totalPages))
                    {
                        var masterReference = conditionalRef.MasterReference;
                        var selectedMaster = foRoot.LayoutMasterSet?.FindPageMaster(masterReference);
                        if (selectedMaster != null)
                            return selectedMaster;
                    }
                }
            }

            // Use repeatable-page-master-reference if available
            if (pageSequenceMaster.RepeatablePageMasterReference != null)
            {
                var masterReference = pageSequenceMaster.RepeatablePageMasterReference.MasterReference;
                var selectedMaster = foRoot.LayoutMasterSet?.FindPageMaster(masterReference);
                if (selectedMaster != null)
                    return selectedMaster;
            }

            // Use single-page-master-reference if available
            if (pageSequenceMaster.SinglePageMasterReference != null)
            {
                var masterReference = pageSequenceMaster.SinglePageMasterReference.MasterReference;
                var selectedMaster = foRoot.LayoutMasterSet?.FindPageMaster(masterReference);
                if (selectedMaster != null)
                    return selectedMaster;
            }
        }

        // Default page if no master found
        var defaultMaster = new Dom.FoSimplePageMaster
        {
            Properties = new Dom.FoProperties()
        };
        defaultMaster.Properties["page-width"] = "595pt";  // A4 width
        defaultMaster.Properties["page-height"] = "842pt"; // A4 height
        return defaultMaster;
    }

    private bool MatchesConditions(Dom.FoConditionalPageMasterReference conditionalRef, int pageNumber, int totalPages)
    {
        // Check page-position
        var pagePosition = conditionalRef.PagePosition;
        if (pagePosition != "any")
        {
            if (pagePosition == "first" && pageNumber != 1)
                return false;
            if (pagePosition == "last" && pageNumber != totalPages)
                return false;
            if (pagePosition == "rest" && pageNumber == 1)
                return false;
        }

        // Check odd-or-even
        var oddOrEven = conditionalRef.OddOrEven;
        if (oddOrEven != "any")
        {
            bool isOdd = (pageNumber % 2) == 1;
            if (oddOrEven == "odd" && !isOdd)
                return false;
            if (oddOrEven == "even" && isOdd)
                return false;
        }

        // All conditions match
        return true;
    }

    private void LayoutFlowWithPagination(AreaTree areaTree, Dom.FoRoot foRoot, Dom.FoPageSequence pageSequence)
    {
        var flow = pageSequence.Flow!;

        // Helper function to calculate body margins for a given page master
        void CalculateBodyMargins(Dom.FoSimplePageMaster pageMaster,
            out double marginTop, out double marginBottom, out double marginLeft, out double marginRight,
            out double width, out double height)
        {
            var regionBody = pageMaster.RegionBody;
            var regionBodyMarginTop = regionBody?.MarginTop ?? 0;
            var regionBodyMarginBottom = regionBody?.MarginBottom ?? 0;
            var regionBodyMarginLeft = regionBody?.MarginLeft ?? 0;
            var regionBodyMarginRight = regionBody?.MarginRight ?? 0;

            // Calculate region extents
            var regionBeforeExtent = (pageMaster.RegionBefore as Dom.FoRegionBefore)?.Extent ?? 0;
            var regionAfterExtent = (pageMaster.RegionAfter as Dom.FoRegionAfter)?.Extent ?? 0;
            var regionStartExtent = (pageMaster.RegionStart as Dom.FoRegionStart)?.Extent ?? 0;
            var regionEndExtent = (pageMaster.RegionEnd as Dom.FoRegionEnd)?.Extent ?? 0;

            // Body position and dimensions must account for:
            // - Page margins (from simple-page-master)
            // - Region extents (from region-before/after/start/end)
            // - Region-body margins
            marginTop = pageMaster.MarginTop + regionBeforeExtent + regionBodyMarginTop;
            marginBottom = pageMaster.MarginBottom + regionAfterExtent + regionBodyMarginBottom;
            marginLeft = pageMaster.MarginLeft + regionStartExtent + regionBodyMarginLeft;
            marginRight = pageMaster.MarginRight + regionEndExtent + regionBodyMarginRight;

            width = pageMaster.PageWidth - marginLeft - marginRight;
            height = pageMaster.PageHeight - marginTop - marginBottom;
        }

        // Get first page master to determine body dimensions
        var firstPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber: 1, totalPages: 999);
        CalculateBodyMargins(firstPageMaster, out var bodyMarginTop, out var bodyMarginBottom,
            out var bodyMarginLeft, out var bodyMarginRight, out var bodyWidth, out var bodyHeight);

        // Multi-column support
        var regionBody = firstPageMaster.RegionBody;
        var columnCount = (regionBody as Dom.FoRegionBody)?.ColumnCount ?? 1;
        var columnGap = (regionBody as Dom.FoRegionBody)?.ColumnGap ?? 12;

        // Calculate column width: (total width - gaps) / column count
        var columnWidth = columnCount > 1
            ? (bodyWidth - (columnCount - 1) * columnGap) / columnCount
            : bodyWidth;

        // Create first page
        var pageNumber = 1;
        var currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
        var currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
        var currentY = bodyMarginTop;
        var currentColumn = 0;  // Current column index (0-based)

        // Track previous block for keep-with-next/previous constraints
        Dom.FoBlock? previousBlock = null;
        BlockArea? previousBlockArea = null;
        double previousBlockTotalHeight = 0;

        // Layout each block in the flow
        foreach (var foBlock in flow.Blocks)
        {
            // Handle break-before constraint
            if (foBlock.BreakBefore == "always" || foBlock.BreakBefore == "page")
            {
                // Force page break before this block (unless we're at the top of a new page)
                if (currentY > bodyMarginTop || currentColumn > 0)
                {
                    RenderFloats(currentPage, currentPageMaster, bodyMarginTop);
                    RenderFootnotes(currentPage, currentPageMaster, pageSequence);
                    AddLinksToPage(currentPage);
                    CheckPageLimit(areaTree);
                    areaTree.AddPage(currentPage);
                    pageNumber++;
                    currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
                    CalculateBodyMargins(currentPageMaster, out bodyMarginTop, out bodyMarginBottom,
                        out bodyMarginLeft, out bodyMarginRight, out bodyWidth, out bodyHeight);
                    currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                    currentY = bodyMarginTop;
                    currentColumn = 0;

                    // Reset previous block tracking after page break
                    previousBlock = null;
                    previousBlockArea = null;
                    previousBlockTotalHeight = 0;
                }
            }

            // Calculate X position based on current column
            var columnX = bodyMarginLeft + currentColumn * (columnWidth + columnGap);

            // Apply space-before to currentY before laying out the block
            var blockY = currentY + foBlock.SpaceBefore;

            var blockArea = LayoutBlock(foBlock, columnX, blockY, columnWidth);
            if (blockArea == null)
                continue;

            // Total height includes space-before, margins, block height, and space-after
            var blockTotalHeight = blockArea.SpaceBefore + blockArea.MarginTop + blockArea.Height + blockArea.MarginBottom + blockArea.SpaceAfter;

            // Handle keep-together constraint
            var mustKeepTogether = foBlock.KeepTogether == "always";
            var blockFitsInColumn = currentY + blockTotalHeight <= currentPageMaster.PageHeight - bodyMarginBottom;

            // Check for keep-with-next/previous constraints
            var mustKeepWithPrevious = (previousBlock != null && GetKeepStrength(previousBlock.KeepWithNext) > 0) ||
                                      GetKeepStrength(foBlock.KeepWithPrevious) > 0;

            // If block doesn't fit and must keep with previous, we need to move both blocks together
            if (!blockFitsInColumn && mustKeepWithPrevious && previousBlockArea != null && currentY > bodyMarginTop)
            {
                // Remove previous block from current page
                currentPage.RemoveArea(previousBlockArea);

                // Adjust currentY to remove previous block's contribution
                currentY -= previousBlockTotalHeight;

                // Try moving to next column
                if (currentColumn < columnCount - 1)
                {
                    // Move to next column on same page
                    currentColumn++;
                    currentY = bodyMarginTop;
                    columnX = bodyMarginLeft + currentColumn * (columnWidth + columnGap);
                }
                else
                {
                    // All columns filled - create new page
                    RenderFloats(currentPage, currentPageMaster, bodyMarginTop);
                    RenderFootnotes(currentPage, currentPageMaster, pageSequence);
                    AddLinksToPage(currentPage);
                    CheckPageLimit(areaTree);
                    areaTree.AddPage(currentPage);
                    pageNumber++;
                    currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
                    CalculateBodyMargins(currentPageMaster, out bodyMarginTop, out bodyMarginBottom,
                        out bodyMarginLeft, out bodyMarginRight, out bodyWidth, out bodyHeight);
                    currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                    currentY = bodyMarginTop;
                    currentColumn = 0;
                    columnX = bodyMarginLeft;
                }

                // Re-layout the previous block for the new column/page
                var prevBlockY = currentY + previousBlock!.SpaceBefore;
                var prevBlockArea = LayoutBlock(previousBlock, columnX, prevBlockY, columnWidth);
                if (prevBlockArea != null)
                {
                    currentPage.AddArea(prevBlockArea);
                    currentY += prevBlockArea.SpaceBefore + prevBlockArea.MarginTop + prevBlockArea.Height +
                               prevBlockArea.MarginBottom + prevBlockArea.SpaceAfter;
                    previousBlockArea = prevBlockArea;
                    previousBlockTotalHeight = prevBlockArea.SpaceBefore + prevBlockArea.MarginTop +
                                              prevBlockArea.Height + prevBlockArea.MarginBottom + prevBlockArea.SpaceAfter;
                }

                // Re-layout the current block for the new column/page
                blockY = currentY + foBlock.SpaceBefore;
                blockArea = LayoutBlock(foBlock, columnX, blockY, columnWidth);
                if (blockArea == null)
                {
                    previousBlock = foBlock;
                    previousBlockArea = null;
                    previousBlockTotalHeight = 0;
                    continue;
                }

                blockTotalHeight = blockArea.SpaceBefore + blockArea.MarginTop + blockArea.Height + blockArea.MarginBottom + blockArea.SpaceAfter;
                blockFitsInColumn = true; // We've moved to new page/column, so it should fit
            }

            // If block doesn't fit in current column/page
            if (!blockFitsInColumn)
            {
                // Only add the block to overflow page if we're NOT at the top AND NOT keep-together
                // (if we're at the top, the block is too large for any page, so we must render it anyway)
                if (currentY > bodyMarginTop || mustKeepTogether)
                {
                    // Before moving the entire block, check if we can split it to avoid widows/orphans
                    // Only consider splitting if keep-together is NOT set
                    var availableHeight = currentPageMaster.PageHeight - bodyMarginBottom - currentY;
                    var splitPoint = 0;

                    if (!mustKeepTogether && availableHeight > 0)
                    {
                        splitPoint = CalculateOptimalSplitPoint(blockArea, foBlock, availableHeight, foBlock.LineHeight);
                    }

                    if (splitPoint > 0)
                    {
                        // We can split the block to avoid widows/orphans
                        // Split the block: first part stays on current page, second part goes to next page/column
                        var (firstPart, secondPart) = SplitBlockAtLine(blockArea, foBlock, splitPoint, columnX, currentY, 0, 0);

                        // Add first part to current page
                        firstPart.X = columnX + foBlock.MarginLeft;
                        firstPart.Y = currentY + foBlock.SpaceBefore + foBlock.MarginTop;
                        currentPage.AddArea(firstPart);
                        currentY += firstPart.SpaceBefore + firstPart.MarginTop + firstPart.Height;

                        // Move to next column/page
                        if (currentColumn < columnCount - 1)
                        {
                            // Move to next column on same page
                            currentColumn++;
                            currentY = bodyMarginTop;
                            columnX = bodyMarginLeft + currentColumn * (columnWidth + columnGap);
                        }
                        else
                        {
                            // All columns filled - create new page
                            RenderFloats(currentPage, currentPageMaster, bodyMarginTop);
                            RenderFootnotes(currentPage, currentPageMaster, pageSequence);
                            AddLinksToPage(currentPage);
                            CheckPageLimit(areaTree);
                            areaTree.AddPage(currentPage);
                            pageNumber++;
                            currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
                            CalculateBodyMargins(currentPageMaster, out bodyMarginTop, out bodyMarginBottom,
                                out bodyMarginLeft, out bodyMarginRight, out bodyWidth, out bodyHeight);
                            currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                            currentY = bodyMarginTop;
                            currentColumn = 0;
                            columnX = bodyMarginLeft;
                        }

                        // Add second part to new column/page
                        secondPart.X = columnX + foBlock.MarginLeft;
                        secondPart.Y = currentY;
                        currentPage.AddArea(secondPart);
                        currentY += secondPart.Height + secondPart.MarginBottom + secondPart.SpaceAfter;

                        // Update blockArea to secondPart for tracking purposes
                        blockArea = secondPart;
                        blockTotalHeight = secondPart.Height + secondPart.MarginBottom + secondPart.SpaceAfter;
                    }
                    else
                    {
                        // Can't split or shouldn't split - move entire block to next column/page
                        // Try moving to next column
                        if (currentColumn < columnCount - 1)
                        {
                            // Move to next column on same page
                            currentColumn++;
                            currentY = bodyMarginTop;
                            columnX = bodyMarginLeft + currentColumn * (columnWidth + columnGap);
                        }
                        else
                        {
                            // All columns filled - create new page
                            RenderFloats(currentPage, currentPageMaster, bodyMarginTop);
                            RenderFootnotes(currentPage, currentPageMaster, pageSequence);
                            AddLinksToPage(currentPage);
                            CheckPageLimit(areaTree);
                            areaTree.AddPage(currentPage);
                            pageNumber++;
                            currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
                            CalculateBodyMargins(currentPageMaster, out bodyMarginTop, out bodyMarginBottom,
                                out bodyMarginLeft, out bodyMarginRight, out bodyWidth, out bodyHeight);
                            currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                            currentY = bodyMarginTop;
                            currentColumn = 0;
                            columnX = bodyMarginLeft;
                        }

                        // Re-layout the block for the new column/page
                        blockY = currentY + foBlock.SpaceBefore;
                        blockArea = LayoutBlock(foBlock, columnX, blockY, columnWidth);
                        if (blockArea == null)
                            continue;

                        blockTotalHeight = blockArea.SpaceBefore + blockArea.MarginTop + blockArea.Height + blockArea.MarginBottom + blockArea.SpaceAfter;

                        currentPage.AddArea(blockArea);
                        currentY += blockArea.SpaceBefore + blockArea.MarginTop + blockArea.Height + blockArea.MarginBottom + blockArea.SpaceAfter;
                    }
                }
                else
                {
                    // Block is too large for any page, render it anyway at top of current page
                    currentPage.AddArea(blockArea);
                    currentY += blockArea.SpaceBefore + blockArea.MarginTop + blockArea.Height + blockArea.MarginBottom + blockArea.SpaceAfter;
                }
            }
            else
            {
                // Block fits on current page
                currentPage.AddArea(blockArea);
                currentY += blockArea.SpaceBefore + blockArea.MarginTop + blockArea.Height + blockArea.MarginBottom + blockArea.SpaceAfter;
            }

            // Track this block as previous for next iteration
            previousBlock = foBlock;
            previousBlockArea = blockArea;
            previousBlockTotalHeight = blockTotalHeight;

            // Handle break-after constraint
            if (foBlock.BreakAfter == "always" || foBlock.BreakAfter == "page")
            {
                // Force page break after this block
                RenderFloats(currentPage, currentPageMaster, bodyMarginTop);
                RenderFootnotes(currentPage, currentPageMaster, pageSequence);
                AddLinksToPage(currentPage);
                CheckPageLimit(areaTree);
                areaTree.AddPage(currentPage);
                pageNumber++;
                currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
                currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                currentY = bodyMarginTop;
                currentColumn = 0;

                // Reset previous block tracking after page break
                previousBlock = null;
                previousBlockArea = null;
                previousBlockTotalHeight = 0;
            }
        }

        // Layout each table in the flow with page breaking support
        // Note: Tables span full body width, not individual columns
        foreach (var foTable in flow.Tables)
        {
            // Tables break columns - start from left margin
            // Use row-by-row layout with page breaking
            LayoutTableWithPageBreaking(
                foTable,
                foRoot,
                pageSequence,
                areaTree,
                ref currentPage,
                ref currentPageMaster,
                ref pageNumber,
                ref currentY,
                ref currentColumn,
                bodyMarginLeft,
                bodyWidth,
                ref bodyMarginTop,
                ref bodyMarginBottom,
                ref bodyMarginLeft,
                ref bodyMarginRight,
                ref bodyWidth,
                ref bodyHeight);

            currentColumn = 0;  // Reset to first column after table
        }

        // Layout each table-and-caption in the flow with page breaking support
        // Note: Table-and-captions span full body width, not individual columns
        foreach (var foTableAndCaption in flow.TableAndCaptions)
        {
            // Layout table-and-caption with page breaking
            LayoutTableAndCaptionWithPageBreaking(
                foTableAndCaption,
                foRoot,
                pageSequence,
                areaTree,
                ref currentPage,
                ref currentPageMaster,
                ref pageNumber,
                ref currentY,
                ref currentColumn,
                bodyMarginLeft,
                bodyWidth,
                ref bodyMarginTop,
                ref bodyMarginBottom,
                ref bodyMarginLeft,
                ref bodyMarginRight,
                ref bodyWidth,
                ref bodyHeight);

            currentColumn = 0;  // Reset to first column after table-and-caption
        }

        // Layout each list block in the flow
        // Note: Lists span full body width, not individual columns
        foreach (var foList in flow.Lists)
        {
            // Use new page breaking approach for lists (similar to tables)
            LayoutListBlockWithPageBreaking(
                foList,
                foRoot,
                pageSequence,
                areaTree,
                ref currentPage,
                ref currentPageMaster,
                ref pageNumber,
                ref currentY,
                ref currentColumn,
                bodyMarginLeft,
                bodyWidth,
                ref bodyMarginTop,
                ref bodyMarginBottom,
                ref bodyMarginLeft,
                ref bodyMarginRight,
                ref bodyWidth,
                ref bodyHeight);

            currentColumn = 0;  // Reset to first column after list
        }

        // Layout normal flow block containers
        // These participate in the normal flow like blocks
        foreach (var blockContainer in flow.BlockContainers)
        {
            if (blockContainer.AbsolutePosition == "auto" || blockContainer.AbsolutePosition == "")
            {
                // Calculate X position based on current column
                var columnX = bodyMarginLeft + currentColumn * (columnWidth + columnGap);

                var containerArea = LayoutNormalFlowBlockContainer(blockContainer, columnX, currentY, columnWidth, pageNumber);
                if (containerArea != null)
                {
                    // Total height includes margins and block height
                    var containerTotalHeight = containerArea.MarginTop + containerArea.Height + containerArea.MarginBottom;

                    // Check if container fits in current column
                    var containerFitsInColumn = currentY + containerTotalHeight <= currentPageMaster.PageHeight - bodyMarginBottom;

                    if (!containerFitsInColumn)
                    {
                        // Try moving to next column
                        if (currentColumn < columnCount - 1)
                        {
                            // Move to next column on same page
                            currentColumn++;
                            columnX = bodyMarginLeft + currentColumn * (columnWidth + columnGap);
                            currentY = bodyMarginTop;
                        }
                        else
                        {
                            // Need new page
                            RenderFloats(currentPage, currentPageMaster, bodyMarginTop);
                            RenderFootnotes(currentPage, currentPageMaster, pageSequence);
                            AddLinksToPage(currentPage);
                            CheckPageLimit(areaTree);
                            areaTree.AddPage(currentPage);
                            pageNumber++;
                            currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
                            CalculateBodyMargins(currentPageMaster, out bodyMarginTop, out bodyMarginBottom,
                                out bodyMarginLeft, out bodyMarginRight, out bodyWidth, out bodyHeight);
                            currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                            currentY = bodyMarginTop;
                            currentColumn = 0;
                            columnX = bodyMarginLeft;
                        }

                        // Re-layout container at new position
                        containerArea = LayoutNormalFlowBlockContainer(blockContainer, columnX, currentY, columnWidth, pageNumber);
                        if (containerArea != null)
                        {
                            containerTotalHeight = containerArea.MarginTop + containerArea.Height + containerArea.MarginBottom;
                        }
                    }

                    if (containerArea != null)
                    {
                        currentPage.AddArea(containerArea);
                        currentY += containerTotalHeight;
                    }
                }
            }
        }

        // Layout absolutely positioned block containers
        // These are positioned relative to the page, not the flow
        // Note: Currently adds to current page only; future enhancement could support page-specific positioning
        foreach (var blockContainer in flow.BlockContainers)
        {
            if (blockContainer.AbsolutePosition == "absolute")
            {
                var absoluteArea = LayoutBlockContainer(blockContainer, currentPageMaster, pageNumber);
                if (absoluteArea != null)
                {
                    currentPage.AddAbsoluteArea(absoluteArea);
                }
            }
        }

        // Add the last page
        RenderFloats(currentPage, currentPageMaster, bodyMarginTop);
        RenderFootnotes(currentPage, currentPageMaster, pageSequence);
        AddLinksToPage(currentPage);
        CheckPageLimit(areaTree);
        areaTree.AddPage(currentPage);
    }

    private BlockArea? LayoutBlock(Dom.FoBlock foBlock, double x, double y, double availableWidth, int pageNumber = 0)
    {
        var blockArea = new BlockArea
        {
            X = x + foBlock.MarginLeft,
            Y = y + foBlock.MarginTop,
            FontFamily = foBlock.FontFamily,
            FontSize = foBlock.FontSize,
            TextAlign = foBlock.TextAlign,
            MarginTop = foBlock.MarginTop,
            MarginBottom = foBlock.MarginBottom,
            MarginLeft = foBlock.MarginLeft,
            MarginRight = foBlock.MarginRight,
            SpaceBefore = foBlock.SpaceBefore,
            SpaceAfter = foBlock.SpaceAfter,
            PaddingTop = foBlock.PaddingTop,
            PaddingBottom = foBlock.PaddingBottom,
            PaddingLeft = foBlock.PaddingLeft,
            PaddingRight = foBlock.PaddingRight,
            BackgroundColor = foBlock.BackgroundColor,
            BackgroundRepeat = foBlock.BackgroundRepeat,
            BackgroundPositionHorizontal = foBlock.BackgroundPositionHorizontal,
            BackgroundPositionVertical = foBlock.BackgroundPositionVertical,
            BorderWidth = foBlock.BorderWidth,
            BorderColor = foBlock.BorderColor,
            BorderStyle = foBlock.BorderStyle,
            BorderTopWidth = foBlock.BorderTopWidth,
            BorderBottomWidth = foBlock.BorderBottomWidth,
            BorderLeftWidth = foBlock.BorderLeftWidth,
            BorderRightWidth = foBlock.BorderRightWidth,
            BorderTopStyle = foBlock.BorderTopStyle,
            BorderBottomStyle = foBlock.BorderBottomStyle,
            BorderLeftStyle = foBlock.BorderLeftStyle,
            BorderRightStyle = foBlock.BorderRightStyle,
            BorderTopColor = foBlock.BorderTopColor,
            BorderBottomColor = foBlock.BorderBottomColor,
            BorderLeftColor = foBlock.BorderLeftColor,
            BorderRightColor = foBlock.BorderRightColor,
            BorderTopLeftRadius = foBlock.BorderTopLeftRadius,
            BorderTopRightRadius = foBlock.BorderTopRightRadius,
            BorderBottomLeftRadius = foBlock.BorderBottomLeftRadius,
            BorderBottomRightRadius = foBlock.BorderBottomRightRadius,
            Visibility = foBlock.Visibility,
            Clip = foBlock.Clip,
            Overflow = foBlock.Overflow
        };

        // Load background image if specified
        LoadBackgroundImage(blockArea, foBlock.BackgroundImage);

        // Calculate content width (available width minus margins and padding)
        var contentWidth = availableWidth - foBlock.MarginLeft - foBlock.MarginRight - foBlock.PaddingLeft - foBlock.PaddingRight;

        var currentY = foBlock.PaddingTop;

        // Collect markers if present
        foreach (var child in foBlock.Children)
        {
            if (child is Dom.FoMarker marker && pageNumber > 0)
            {
                var className = marker.MarkerClassName;
                if (!string.IsNullOrEmpty(className))
                {
                    if (!_markers.ContainsKey(className))
                        _markers[className] = new List<(int, int, Dom.FoMarker)>();

                    if (!_markerSequenceCounters.ContainsKey(className))
                        _markerSequenceCounters[className] = 0;

                    int sequence = _markerSequenceCounters[className]++;
                    _markers[className].Add((pageNumber, sequence, marker));
                }
            }
        }

        // Collect footnotes if present
        foreach (var footnote in foBlock.Footnotes)
        {
            _currentPageFootnotes.Add(footnote);
        }

        // Collect floats if present
        foreach (var float_ in foBlock.Floats)
        {
            _currentPageFloats.Add(float_);
        }

        // Check for inline page number and link elements
        var hasPageNumber = foBlock.Children.Any(c => c is Dom.FoPageNumber);
        var hasBasicLink = foBlock.Children.Any(c => c is Dom.FoBasicLink);
        var hasInline = foBlock.Children.Any(c => c is Dom.FoInline);
        var hasLeader = foBlock.Children.Any(c => c is Dom.FoLeader);
        var hasBlockChildren = foBlock.Children.Any(c => c is Dom.FoBlock or Dom.FoExternalGraphic or Dom.FoInstreamForeignObject);

        // Handle block-level children (images, nested blocks)
        if (hasBlockChildren)
        {
            foreach (var child in foBlock.Children)
            {
                if (child is Dom.FoExternalGraphic graphic)
                {
                    var area = LayoutImageOrSvg(graphic.Src, graphic, foBlock.PaddingLeft, currentY, contentWidth);
                    if (area != null)
                    {
                        blockArea.AddChild(area);
                        currentY += area.Height;
                    }
                }
                else if (child is Dom.FoInstreamForeignObject foreignObject)
                {
                    var svgArea = LayoutEmbeddedSvg(foreignObject, foBlock.PaddingLeft, currentY, contentWidth);
                    if (svgArea != null)
                    {
                        blockArea.AddChild(svgArea);
                        currentY += svgArea.Height;
                    }
                }
                else if (child is Dom.FoBlock nestedBlock)
                {
                    var nestedArea = LayoutBlock(nestedBlock, foBlock.PaddingLeft, currentY, contentWidth, pageNumber);
                    if (nestedArea != null)
                    {
                        blockArea.AddChild(nestedArea);
                        currentY += nestedArea.Height + nestedArea.MarginTop + nestedArea.MarginBottom;
                    }
                }
            }

            blockArea.Width = contentWidth + foBlock.PaddingLeft + foBlock.PaddingRight;
            blockArea.Height = currentY + foBlock.PaddingBottom;
            return blockArea;
        }

        // Create font metrics for measurement (used by both links and regular text)
        // Use FontResolver to handle font family normalization and variant selection
        var blockFontFamily = Fonts.PdfBaseFontMapper.ResolveFont(
            foBlock.FontFamily,
            foBlock.FontWeight,
            foBlock.FontStyle);

        var fontMetrics = new Fonts.FontMetrics
        {
            FamilyName = blockFontFamily,
            Size = foBlock.FontSize
        };

        // Handle inline elements with formatting (including leaders)
        if (hasInline || hasLeader)
        {
            var lineArea = new LineArea
            {
                X = foBlock.PaddingLeft,
                Y = currentY,
                Width = contentWidth,
                Height = foBlock.LineHeight
            };

            double currentX = 0;

            // Process mixed content (text nodes + inline elements + leaders)
            var blockText = foBlock.TextContent ?? "";
            var inlineIndex = 0;

            foreach (var child in foBlock.Children)
            {
                if (child is Dom.FoInline inline)
                {
                    var inlineText = inline.TextContent ?? "";
                    if (!string.IsNullOrWhiteSpace(inlineText))
                    {
                        // Get font properties (now with automatic inheritance)
                        var inlineFontSize = inline.FontSize ?? 12;

                        // Use FontResolver to handle font family normalization and variant selection
                        var inlineFontFamily = Fonts.PdfBaseFontMapper.ResolveFont(
                            inline.FontFamily,
                            inline.FontWeight,
                            inline.FontStyle);

                        var inlineFontMetrics = new Fonts.FontMetrics
                        {
                            FamilyName = inlineFontFamily,
                            Size = inlineFontSize
                        };

                        var textWidth = inlineFontMetrics.MeasureWidth(inlineText);

                        var inlineArea = new InlineArea
                        {
                            X = currentX,
                            Y = 0, // Relative to line
                            Width = textWidth,
                            Height = inlineFontSize,
                            Text = inlineText,
                            FontFamily = inlineFontFamily,
                            FontSize = inlineFontSize,
                            FontWeight = inline.FontWeight,
                            FontStyle = inline.FontStyle,
                            Color = inline.Color,
                            TextDecoration = inline.TextDecoration,
                            BackgroundColor = inline.BackgroundColor,
                            BaselineOffset = inlineFontMetrics.GetAscent() + inline.BaselineShift
                        };

                        lineArea.AddInline(inlineArea);
                        currentX += textWidth;
                    }
                    inlineIndex++;
                }
                else if (child is Dom.FoLeader leader)
                {
                    // Calculate leader width (fill remaining space in line)
                    var leaderWidth = contentWidth - currentX;

                    // Get leader properties
                    var leaderPattern = leader.LeaderPattern;
                    var patternWidth = leader.LeaderPatternWidth == "use-font-metrics"
                        ? fontMetrics.MeasureWidth(".")
                        : 5.0; // Default pattern width

                    // Create leader area
                    var leaderArea = new LeaderArea
                    {
                        X = currentX,
                        Y = 0, // Relative to line
                        Width = leaderWidth,
                        Height = foBlock.FontSize,
                        LeaderPattern = leaderPattern,
                        LeaderPatternWidth = patternWidth,
                        RuleThickness = leader.RuleThickness,
                        RuleStyle = leader.RuleStyle,
                        Color = leader.Color,
                        FontFamily = leader.FontFamily,
                        FontSize = leader.FontSize ?? 12,
                        BaselineOffset = fontMetrics.GetAscent()
                    };

                    // Add as child of line area (note: LineArea.AddInline only accepts InlineArea)
                    // We need to track leaders separately or modify LineArea
                    // For now, let's add it as a child of the block area directly
                    leaderArea.X = foBlock.PaddingLeft + currentX;
                    leaderArea.Y = currentY;
                    blockArea.AddChild(leaderArea);

                    currentX += leaderWidth;
                }
                else if (child is Dom.FoBidiOverride bidiOverride)
                {
                    var bidiText = bidiOverride.TextContent ?? "";
                    if (!string.IsNullOrWhiteSpace(bidiText))
                    {
                        // Get font properties (with automatic inheritance)
                        var bidiFontSize = bidiOverride.FontSize ?? 12;
                        var bidiDirection = bidiOverride.Direction;

                        // Use FontResolver to handle font family normalization and variant selection
                        var bidiFontFamily = Fonts.PdfBaseFontMapper.ResolveFont(
                            bidiOverride.FontFamily,
                            bidiOverride.FontWeight,
                            bidiOverride.FontStyle);

                        var bidiFontMetrics = new Fonts.FontMetrics
                        {
                            FamilyName = bidiFontFamily,
                            Size = bidiFontSize
                        };

                        // Apply full Unicode BiDi Algorithm (UAX#9)
                        var baseDirection = bidiDirection == "rtl" ? 1 : 0;
                        var bidiAlgorithm = new BiDi.UnicodeBidiAlgorithm();
                        var processedText = bidiAlgorithm.ReorderText(bidiText, baseDirection);

                        var textWidth = bidiFontMetrics.MeasureWidth(processedText);

                        var bidiArea = new InlineArea
                        {
                            X = currentX,
                            Y = 0, // Relative to line
                            Width = textWidth,
                            Height = bidiFontSize,
                            Text = processedText,
                            FontFamily = bidiFontFamily,
                            FontSize = bidiFontSize,
                            FontWeight = bidiOverride.FontWeight,
                            FontStyle = bidiOverride.FontStyle,
                            Color = bidiOverride.Color,
                            Direction = bidiDirection,
                            BaselineOffset = bidiFontMetrics.GetAscent()
                        };

                        lineArea.AddInline(bidiArea);
                        currentX += textWidth;
                    }
                    inlineIndex++;
                }
            }

            // Add any remaining block text that's not in an inline
            if (!string.IsNullOrWhiteSpace(blockText))
            {
                var textWidth = fontMetrics.MeasureWidth(blockText);
                var inlineArea = new InlineArea
                {
                    X = currentX,
                    Y = 0,
                    Width = textWidth,
                    Height = foBlock.FontSize,
                    Text = blockText,
                    FontFamily = foBlock.FontFamily,
                    FontSize = foBlock.FontSize,
                    BaselineOffset = fontMetrics.GetAscent()
                };

                lineArea.AddInline(inlineArea);
            }

            blockArea.AddChild(lineArea);
            currentY += foBlock.LineHeight;

            blockArea.Width = contentWidth + foBlock.PaddingLeft + foBlock.PaddingRight;
            blockArea.Height = currentY + foBlock.PaddingBottom;
            return blockArea;
        }

        // Handle inline basic-link elements
        if (hasBasicLink)
        {
            foreach (var child in foBlock.Children)
            {
                if (child is Dom.FoBasicLink basicLink)
                {
                    var linkText = basicLink.TextContent ?? "";
                    if (!string.IsNullOrWhiteSpace(linkText))
                    {
                        // Create line area for the link text
                        var lineArea = CreateLineArea(linkText, foBlock.PaddingLeft, currentY, contentWidth, fontMetrics, foBlock);
                        blockArea.AddChild(lineArea);

                        // Create LinkArea for PDF annotation
                        // Calculate absolute position: block X + block Y + line relative position
                        var linkArea = new LinkArea
                        {
                            X = x + foBlock.PaddingLeft,
                            Y = y + currentY,
                            Width = fontMetrics.MeasureWidth(linkText),
                            Height = foBlock.LineHeight,
                            Text = linkText,
                            FontFamily = fontMetrics.FamilyName, // Use the font variant from fontMetrics
                            FontSize = foBlock.FontSize,
                            Color = basicLink.Color,
                            TextDecoration = basicLink.TextDecoration,
                            InternalDestination = basicLink.InternalDestination,
                            ExternalDestination = basicLink.ExternalDestination,
                            ShowDestination = basicLink.ShowDestination
                        };

                        _currentPageLinks.Add(linkArea);
                        currentY += foBlock.LineHeight;
                    }
                }
            }

            blockArea.Width = contentWidth + foBlock.PaddingLeft + foBlock.PaddingRight;
            blockArea.Height = currentY + foBlock.PaddingBottom;
            return blockArea;
        }

        // Get text content and substitute page numbers
        var text = foBlock.TextContent ?? "";
        if (hasPageNumber && pageNumber > 0)
        {
            // Replace page number placeholder with actual page number
            text = text + pageNumber.ToString();
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            // Empty block - set minimal height
            blockArea.Width = contentWidth + foBlock.PaddingLeft + foBlock.PaddingRight;
            blockArea.Height = foBlock.LineHeight + foBlock.PaddingTop + foBlock.PaddingBottom;
            return blockArea;
        }

        // Perform line breaking and create line areas
        var lines = BreakLines(text, contentWidth, fontMetrics, foBlock);

        for (int i = 0; i < lines.Count; i++)
        {
            var lineText = lines[i];
            var isLastLine = (i == lines.Count - 1);
            var lineArea = CreateLineArea(lineText, foBlock.PaddingLeft, currentY, contentWidth, fontMetrics, foBlock, isLastLine);
            blockArea.AddChild(lineArea);
            currentY += foBlock.LineHeight;
        }

        blockArea.Width = contentWidth + foBlock.PaddingLeft + foBlock.PaddingRight;
        blockArea.Height = currentY + foBlock.PaddingBottom;

        return blockArea;
    }

    private List<string> BreakLines(string text, double availableWidth, Fonts.FontMetrics fontMetrics, Dom.FoBlock foBlock)
    {
        var lines = new List<string>();

        // Check wrap-option property
        var wrapOption = foBlock.WrapOption.ToLowerInvariant();

        // If no-wrap, return the entire text as a single line (no breaking)
        if (wrapOption == "no-wrap")
        {
            lines.Add(text);
            return lines;
        }

        // Dispatch to the appropriate line breaking algorithm
        if (_options.LineBreaking == LineBreakingAlgorithm.Optimal)
        {
            return BreakLinesOptimal(text, availableWidth, fontMetrics, foBlock);
        }
        else
        {
            return BreakLinesGreedy(text, availableWidth, fontMetrics, foBlock);
        }
    }

    /// <summary>
    /// Greedy (first-fit) line breaking algorithm. Fast, single-pass, O(n) complexity.
    /// </summary>
    private List<string> BreakLinesGreedy(string text, double availableWidth, Fonts.FontMetrics fontMetrics, Dom.FoBlock foBlock)
    {
        var lines = new List<string>();

        // Split text into words, treating dashes as part of words (allowing breaks after them)
        var words = SplitIntoWords(text);

        if (words.Count == 0)
        {
            lines.Add("");
            return lines;
        }

        var currentLine = new StringBuilder();
        string? previousWord = null;

        foreach (var word in words)
        {
            // Determine if we need a space before this word
            // We need a space if:
            // 1. This is not the first word in the line (previousWord != null)
            // 2. AND the previous word didn't end with a dash
            bool needsSpaceBefore = previousWord != null &&
                                   !previousWord.EndsWith('—') &&
                                   !previousWord.EndsWith('–') &&
                                   !previousWord.EndsWith('-');

            // Build tentative line with this word
            string tentativeLine;
            if (currentLine.Length == 0)
            {
                // First word in line
                tentativeLine = word;
            }
            else if (needsSpaceBefore)
            {
                // Add space before word
                tentativeLine = currentLine.ToString() + " " + word;
            }
            else
            {
                // No space needed (after a dash)
                tentativeLine = currentLine.ToString() + word;
            }

            // Measure the complete tentative line (not incremental to avoid rounding errors)
            var tentativeWidth = fontMetrics.MeasureWidth(tentativeLine);

            if (tentativeWidth > availableWidth && currentLine.Length > 0)
            {
                // Adding this word would exceed width
                // Try hyphenation if enabled (both globally and for this block)
                if (_options.EnableHyphenation && foBlock.Hyphenate && !word.Contains('-') && !word.Contains('—') && !word.Contains('–'))
                {
                    // Calculate available width for hyphenation
                    var currentLineStr = currentLine.ToString();
                    var currentLineWidth = fontMetrics.MeasureWidth(currentLineStr);
                    var spaceWidth = needsSpaceBefore ? fontMetrics.MeasureWidth(" ") : 0;
                    var availableForWord = availableWidth - currentLineWidth - spaceWidth;

                    // Try to hyphenate the word
                    var hyphenated = TryHyphenateWord(word, availableForWord, fontMetrics);

                    if (hyphenated.HasValue)
                    {
                        // Hyphenation succeeded - add prefix to current line and continue with remaining
                        if (needsSpaceBefore)
                            currentLine.Append(' ');
                        currentLine.Append(hyphenated.Value.prefix);
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                        currentLine.Append(hyphenated.Value.remaining);
                        previousWord = hyphenated.Value.remaining;
                        continue;
                    }
                }

                // Hyphenation not enabled or didn't help, start a new line
                lines.Add(currentLine.ToString());
                currentLine.Clear();

                // Emergency breaking: Check if the word itself is too wide for available width
                var wordWidth = fontMetrics.MeasureWidth(word);
                if (wordWidth > availableWidth)
                {
                    // Word is too wide even by itself - break it character by character
                    var brokenLines = BreakWordByCharacter(word, availableWidth, fontMetrics);
                    foreach (var brokenLine in brokenLines)
                    {
                        if (brokenLine == brokenLines[^1])
                        {
                            // Last fragment stays in currentLine for potential continuation
                            currentLine.Append(brokenLine);
                            previousWord = brokenLine;
                        }
                        else
                        {
                            // Add complete lines
                            lines.Add(brokenLine);
                        }
                    }
                }
                else
                {
                    // Word fits on its own line
                    currentLine.Append(word);
                    previousWord = word;
                }
            }
            else
            {
                // Word fits, update current line
                if (currentLine.Length == 0)
                {
                    currentLine.Append(word);
                }
                else if (needsSpaceBefore)
                {
                    currentLine.Append(' ');
                    currentLine.Append(word);
                }
                else
                {
                    currentLine.Append(word);
                }
                previousWord = word;
            }
        }

        // Add the last line
        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        // Post-processing: Apply emergency breaking to any lines that are still too wide
        // This catches cases where a single word is placed on a line and exceeds the width
        var processedLines = new List<string>();
        foreach (var line in lines)
        {
            var lineWidth = fontMetrics.MeasureWidth(line);
            if (lineWidth > availableWidth)
            {
                // This line is too wide, apply emergency character-level breaking
                var brokenLines = BreakWordByCharacter(line, availableWidth, fontMetrics);
                processedLines.AddRange(brokenLines);
            }
            else
            {
                // Line fits, keep it as-is
                processedLines.Add(line);
            }
        }

        return processedLines;
    }

    /// <summary>
    /// Knuth-Plass optimal line breaking algorithm from TeX.
    /// Uses dynamic programming to minimize total badness across the entire paragraph.
    /// Slower than greedy (O(n²) vs O(n)) but produces superior typography.
    /// </summary>
    private List<string> BreakLinesOptimal(string text, double availableWidth, Fonts.FontMetrics fontMetrics, Dom.FoBlock foBlock)
    {
        var lines = new List<string>();

        // Split text into words
        var words = SplitIntoWords(text);

        if (words.Count == 0)
        {
            lines.Add("");
            return lines;
        }

        // Build word positions for later conversion
        var wordPositions = new List<(int start, int end)>();
        int currentPos = 0;
        foreach (var word in words)
        {
            var start = currentPos;
            var end = currentPos + word.Length;
            wordPositions.Add((start, end));
            currentPos = end + 1; // +1 for space
        }

        // Create the Knuth-Plass line breaker with configurable parameters from LayoutOptions
        var lineBreaker = new KnuthPlassLineBreaker(
            fontMetrics,
            availableWidth,
            tolerance: _options.KnuthPlassTolerance,
            spaceStretchRatio: _options.KnuthPlassSpaceStretchRatio,
            spaceShrinkRatio: _options.KnuthPlassSpaceShrinkRatio,
            linePenalty: _options.KnuthPlassLinePenalty,
            flaggedDemerit: _options.KnuthPlassFlaggedDemerit,
            fitnessDemerit: _options.KnuthPlassFitnessDemerit);

        // Find optimal breakpoints
        var breakpoints = lineBreaker.FindOptimalBreakpoints(text, words, wordPositions);

        // Convert breakpoints into lines
        if (breakpoints.Count == 0)
        {
            // No breaks needed or possible - return all text as single line
            lines.Add(string.Join(" ", words));
        }
        else
        {
            // Build lines from words based on breakpoints
            var currentLine = new StringBuilder();
            int wordIndex = 0;

            for (int i = 0; i < words.Count; i++)
            {
                if (currentLine.Length > 0)
                {
                    // Check if previous word ends with dash
                    var prevWord = words[i - 1];
                    if (!prevWord.EndsWith('—') && !prevWord.EndsWith('–') && !prevWord.EndsWith('-'))
                    {
                        currentLine.Append(' ');
                    }
                }

                currentLine.Append(words[i]);

                // Check if we should break after this word
                if (wordIndex < breakpoints.Count && wordPositions[i].end >= breakpoints[wordIndex])
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                    wordIndex++;
                }
            }

            // Add remaining text as last line
            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }
        }

        // Apply emergency breaking if any lines are still too wide
        // Note: We re-break using the greedy algorithm rather than breaking
        // the entire line character-by-character, which preserves word boundaries
        var processedLines = new List<string>();
        foreach (var line in lines)
        {
            var lineWidth = fontMetrics.MeasureWidth(line);
            if (lineWidth > availableWidth)
            {
                // Line is too wide - re-break using greedy algorithm
                // This handles emergency breaking at the word level
                var rebrokenLines = BreakLinesGreedy(line, availableWidth, fontMetrics, foBlock);
                processedLines.AddRange(rebrokenLines);
            }
            else
            {
                processedLines.Add(line);
            }
        }

        return processedLines;
    }

    /// <summary>
    /// Gets or creates the hyphenation engine based on current options.
    /// </summary>
    private Core.Hyphenation.HyphenationEngine GetHyphenationEngine()
    {
        if (_hyphenationEngine == null)
        {
            _hyphenationEngine = new Core.Hyphenation.HyphenationEngine(
                _options.HyphenationLanguage,
                _options.HyphenationMinWordLength,
                _options.HyphenationMinLeftChars,
                _options.HyphenationMinRightChars);
        }
        return _hyphenationEngine;
    }

    /// <summary>
    /// Tries to hyphenate a word to fit it on the current line.
    /// Returns null if hyphenation doesn't help, otherwise returns the (prefix with hyphen, remaining part).
    /// </summary>
    private (string prefix, string remaining)? TryHyphenateWord(
        string word,
        double availableWidth,
        Fonts.FontMetrics fontMetrics)
    {
        var engine = GetHyphenationEngine();
        var hyphenPoints = engine.FindHyphenationPoints(word);

        if (hyphenPoints.Length == 0)
            return null;

        var hyphenChar = _options.HyphenationCharacter;

        // Try hyphenation points from right to left (prefer later breaks)
        for (int i = hyphenPoints.Length - 1; i >= 0; i--)
        {
            var point = hyphenPoints[i];
            var prefix = word.Substring(0, point) + hyphenChar;
            var remaining = word.Substring(point);

            // Check if this prefix fits
            var prefixWidth = fontMetrics.MeasureWidth(prefix);
            if (prefixWidth <= availableWidth)
            {
                return (prefix, remaining);
            }
        }

        return null;
    }

    /// <summary>
    /// Emergency line breaking: Breaks a word character-by-character to fit within available width.
    /// This is a last resort when a word is too long to fit even on its own line.
    /// Returns a list of line fragments, where all but the last fit within the available width.
    /// </summary>
    /// <param name="word">The word to break.</param>
    /// <param name="availableWidth">The available width for each line.</param>
    /// <param name="fontMetrics">Font metrics for measuring text width.</param>
    /// <returns>List of line fragments.</returns>
    private List<string> BreakWordByCharacter(string word, double availableWidth, Fonts.FontMetrics fontMetrics)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(word))
        {
            lines.Add("");
            return lines;
        }

        var hyphenChar = _options.HyphenationCharacter;
        var currentFragment = new StringBuilder();

        for (int i = 0; i < word.Length; i++)
        {
            var ch = word[i];
            var tentativeFragment = currentFragment.ToString() + ch;
            var tentativeWidth = fontMetrics.MeasureWidth(tentativeFragment);

            if (tentativeWidth > availableWidth && currentFragment.Length > 0)
            {
                // Adding this character would exceed width
                // Try to add a hyphen to indicate the word continues
                var fragmentWithHyphen = currentFragment.ToString() + hyphenChar;
                var hyphenWidth = fontMetrics.MeasureWidth(fragmentWithHyphen);

                if (hyphenWidth <= availableWidth)
                {
                    // Hyphen fits, use it
                    lines.Add(fragmentWithHyphen);
                    currentFragment.Clear();
                    currentFragment.Append(ch);
                }
                else if (currentFragment.Length > 1)
                {
                    // Hyphen doesn't fit, try removing last character
                    var lastChar = currentFragment[currentFragment.Length - 1];
                    currentFragment.Length--; // Remove last char
                    fragmentWithHyphen = currentFragment.ToString() + hyphenChar;
                    hyphenWidth = fontMetrics.MeasureWidth(fragmentWithHyphen);

                    if (hyphenWidth <= availableWidth)
                    {
                        // Now it fits with hyphen
                        lines.Add(fragmentWithHyphen);
                        currentFragment.Clear();
                        currentFragment.Append(lastChar);
                        currentFragment.Append(ch);
                    }
                    else
                    {
                        // Still doesn't fit, break without hyphen
                        lines.Add(currentFragment.ToString());
                        currentFragment.Clear();
                        currentFragment.Append(lastChar);
                        currentFragment.Append(ch);
                    }
                }
                else
                {
                    // Only one character, can't fit hyphen, break without it
                    lines.Add(currentFragment.ToString());
                    currentFragment.Clear();
                    currentFragment.Append(ch);
                }
            }
            else
            {
                // Character fits, add it to current fragment
                currentFragment.Append(ch);
            }
        }

        // Add the last fragment if not empty (no hyphen on last fragment)
        if (currentFragment.Length > 0)
        {
            lines.Add(currentFragment.ToString());
        }

        // Edge case: If even a single character is too wide, we still need to return it
        // This allows the layout to continue even with extremely narrow columns
        if (lines.Count == 0)
        {
            lines.Add(word);
        }

        return lines;
    }

    /// <summary>
    /// Splits text into words, keeping dashes attached to the word they follow.
    /// This allows line breaking after dashes (em dash, en dash, hyphen) while
    /// maintaining proper spacing between words.
    ///
    /// Examples:
    ///   "hello world" → ["hello", "world"]
    ///   "hello—world" → ["hello—", "world"]  (can break after dash)
    ///   "hello-world test" → ["hello-", "world", "test"]
    /// </summary>
    private List<string> SplitIntoWords(string text)
    {
        var words = new List<string>();
        if (string.IsNullOrEmpty(text))
            return words;

        var currentWord = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            // Whitespace separates words
            if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n')
            {
                if (currentWord.Length > 0)
                {
                    words.Add(currentWord.ToString());
                    currentWord.Clear();
                }
                // Skip the whitespace (don't add it to any word)
                continue;
            }

            // Dashes are kept with the preceding word and mark a break opportunity
            bool isDash = ch == '—' || ch == '–' || ch == '-';

            currentWord.Append(ch);

            // After a dash, end the current word (allowing break after the dash)
            if (isDash)
            {
                words.Add(currentWord.ToString());
                currentWord.Clear();
            }
        }

        // Add any remaining word
        if (currentWord.Length > 0)
        {
            words.Add(currentWord.ToString());
        }

        return words;
    }

    private LineArea CreateLineArea(string text, double x, double y, double availableWidth, Fonts.FontMetrics fontMetrics, Dom.FoBlock foBlock, bool isLastLine = false)
    {
        var lineArea = new LineArea
        {
            X = x,
            Y = y,
            Width = availableWidth,
            Height = foBlock.LineHeight
        };

        // Measure the actual text width (without word spacing)
        var textWidth = fontMetrics.MeasureWidth(text);

        // Calculate X offset and word spacing based on alignment
        // Use text-align-last for the last line if specified, otherwise use text-align
        var textAlign = isLastLine ? foBlock.TextAlignLast.ToLowerInvariant() : foBlock.TextAlign.ToLowerInvariant();
        double textX = 0;
        double wordSpacing = 0;

        // Count spaces for justification calculation
        var spaceCount = text.Count(c => c == ' ');

        if (textAlign == "justify")
        {
            // For justification, calculate word spacing to distribute extra space
            var extraSpace = availableWidth - textWidth;

            // Calculate what the word spacing would be
            var potentialWordSpacing = spaceCount > 0 ? extraSpace / spaceCount : 0;

            // Maximum allowed word spacing to prevent ugly stretching
            // Base it on actual space character width, not a fixed percentage of font size
            var naturalSpaceWidth = fontMetrics.MeasureWidth(" ");
            var maxWordSpacing = naturalSpaceWidth * 3.0;  // Allow up to 300% stretch (4x normal)

            // Only justify if:
            // 1. There are spaces in the text (spaceCount > 0)
            // 2. The text is shorter than available width (extraSpace > 0) - if line is already full/over, don't justify
            // 3. The extra space is not excessive (< 50% of available width - likely last line or very short line)
            // 4. The word spacing won't be too large (prevents ugly gaps with few spaces)
            if (spaceCount > 0 && extraSpace > 0 && extraSpace < availableWidth * 0.5 && potentialWordSpacing <= maxWordSpacing)
            {
                // Calculate word spacing to distribute the extra space
                wordSpacing = potentialWordSpacing;

                // Keep text left-aligned (textX = 0) for justified text
                textX = 0;
            }
            else
            {
                // Fall back to left alignment for lines that shouldn't be justified
                // (includes: no spaces, already too wide, too much extra space, or word spacing would be excessive)
                textX = 0;
            }
        }
        else
        {
            textX = textAlign switch
            {
                "center" => (availableWidth - textWidth) / 2,
                "end" or "right" => availableWidth - textWidth,
                _ => 0 // start/left
            };
        }

        // Calculate the actual rendered width including word spacing
        // This is important for text decorations (underlines, etc.) and background colors
        var renderedWidth = textWidth + (wordSpacing * spaceCount);

        // Create inline area for the text
        // Use the font family from fontMetrics which has the correct variant applied
        var inlineArea = new InlineArea
        {
            X = textX,
            Y = 0, // Relative to line
            Width = renderedWidth,  // Use full rendered width including word spacing
            Height = foBlock.FontSize,
            Text = text,
            FontFamily = fontMetrics.FamilyName,
            FontSize = foBlock.FontSize,
            FontWeight = foBlock.FontWeight,
            FontStyle = foBlock.FontStyle,
            BaselineOffset = fontMetrics.GetAscent(),
            WordSpacing = wordSpacing
        };

        lineArea.AddInline(inlineArea);

        return lineArea;
    }

    private TableArea? LayoutTable(Dom.FoTable foTable, double x, double y, double availableWidth)
    {
        // Clear table markers at the start of each table
        _tableMarkers.Clear();
        _tableMarkerSequenceCounters.Clear();

        var tableArea = new TableArea
        {
            X = x,
            Y = y,
            BorderCollapse = foTable.BorderCollapse,
            BorderSpacing = foTable.BorderSpacing
        };

        // Calculate column widths
        var columnWidths = CalculateColumnWidths(foTable, availableWidth);
        tableArea.ColumnWidths = columnWidths;

        // Calculate total table width
        tableArea.Width = columnWidths.Sum() + (foTable.BorderSpacing * (columnWidths.Count + 1));

        // Two-pass layout for row spanning support:
        // Pass 1: Calculate row heights (without row spanning)
        var allRows = new List<Dom.FoTableRow>();
        if (foTable.Header != null)
            allRows.AddRange(foTable.Header.Rows);
        if (foTable.Body != null)
            allRows.AddRange(foTable.Body.Rows);
        if (foTable.Footer != null)
            allRows.AddRange(foTable.Footer.Rows);

        var rowHeights = CalculateTableRowHeights(allRows, columnWidths, foTable.BorderSpacing);

        // Pass 2: Layout rows with row spanning support
        var grid = new TableCellGrid();
        var currentY = 0.0;
        int rowIndex = 0;

        // Layout header rows
        if (foTable.Header != null)
        {
            foreach (var foRow in foTable.Header.Rows)
            {
                var rowArea = LayoutTableRowWithSpanning(foRow, 0, currentY, columnWidths, foTable.BorderSpacing, grid, rowIndex, rowHeights);
                if (rowArea != null)
                {
                    tableArea.AddRow(rowArea);
                    currentY += rowArea.Height;
                }
                rowIndex++;
            }
        }

        // Layout body rows
        if (foTable.Body != null)
        {
            foreach (var foRow in foTable.Body.Rows)
            {
                var rowArea = LayoutTableRowWithSpanning(foRow, 0, currentY, columnWidths, foTable.BorderSpacing, grid, rowIndex, rowHeights);
                if (rowArea != null)
                {
                    tableArea.AddRow(rowArea);
                    currentY += rowArea.Height;
                }
                rowIndex++;
            }
        }

        // Layout footer rows
        if (foTable.Footer != null)
        {
            foreach (var foRow in foTable.Footer.Rows)
            {
                var rowArea = LayoutTableRowWithSpanning(foRow, 0, currentY, columnWidths, foTable.BorderSpacing, grid, rowIndex, rowHeights);
                if (rowArea != null)
                {
                    tableArea.AddRow(rowArea);
                    currentY += rowArea.Height;
                }
                rowIndex++;
            }
        }

        tableArea.Height = currentY;

        return tableArea;
    }

    /// <summary>
    /// Calculates the heights of all rows in a table (without row spanning).
    /// This is used for the first pass of two-pass layout.
    /// </summary>
    private List<double> CalculateTableRowHeights(List<Dom.FoTableRow> rows, List<double> columnWidths, double borderSpacing)
    {
        var rowHeights = new List<double>();
        var tempGrid = new TableCellGrid(); // Temporary grid for calculation

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var foRow = rows[rowIndex];
            var maxCellHeight = 0.0;
            int cellIndexInRow = 0;

            foreach (var foCell in foRow.Cells)
            {
                // Find next available column
                int columnIndex = tempGrid.GetNextAvailableColumn(rowIndex, cellIndexInRow);

                // Calculate cell width
                var cellWidth = 0.0;
                var colSpan = foCell.NumberColumnsSpanned;
                for (int i = 0; i < colSpan && columnIndex + i < columnWidths.Count; i++)
                {
                    cellWidth += columnWidths[columnIndex + i];
                }
                if (colSpan > 1)
                    cellWidth += borderSpacing * (colSpan - 1);

                // Layout cell to get its natural height (without row spanning)
                var cellArea = LayoutTableCell(foCell, 0, 0, cellWidth, 0);
                if (cellArea != null)
                {
                    // Reserve grid cells
                    var rowSpan = foCell.NumberRowsSpanned;
                    tempGrid.ReserveCells(rowIndex, columnIndex, rowSpan, colSpan, cellArea);

                    // Only count non-spanning cells for row height
                    if (rowSpan == 1)
                    {
                        maxCellHeight = Math.Max(maxCellHeight, cellArea.Height);
                    }
                }

                cellIndexInRow = columnIndex + colSpan;
            }

            rowHeights.Add(maxCellHeight > 0 ? maxCellHeight : 20.0); // Minimum height of 20pt
        }

        return rowHeights;
    }

    /// <summary>
    /// Layouts a table with support for page breaking between rows.
    /// This method handles:
    /// - Row-by-row iteration with page break detection
    /// - Header repetition on new pages (controlled by table-omit-header-at-break)
    /// - Footer placement (controlled by table-omit-footer-at-break)
    /// - Keep-together constraints on rows
    /// - Row spanning with grid tracking
    /// </summary>
    private void LayoutTableWithPageBreaking(
        Dom.FoTable foTable,
        Dom.FoRoot foRoot,
        Dom.FoPageSequence pageSequence,
        AreaTree areaTree,
        ref PageViewport currentPage,
        ref Dom.FoSimplePageMaster currentPageMaster,
        ref int pageNumber,
        ref double currentY,
        ref int currentColumn,
        double tableX,
        double tableWidth,
        ref double bodyMarginTop,
        ref double bodyMarginBottom,
        ref double bodyMarginLeft,
        ref double bodyMarginRight,
        ref double bodyWidth,
        ref double bodyHeight)
    {
        // Clear table markers at the start of each table
        _tableMarkers.Clear();
        _tableMarkerSequenceCounters.Clear();

        // Calculate column widths once for the entire table
        var columnWidths = CalculateColumnWidths(foTable, tableWidth);
        var calculatedTableWidth = columnWidths.Sum() + (foTable.BorderSpacing * (columnWidths.Count + 1));

        // Pre-calculate row heights for all sections (for row spanning support)
        var allHeaderRows = foTable.Header?.Rows.ToList() ?? new List<Dom.FoTableRow>();
        var allBodyRows = foTable.Body?.Rows.ToList() ?? new List<Dom.FoTableRow>();
        var allFooterRows = foTable.Footer?.Rows.ToList() ?? new List<Dom.FoTableRow>();

        var headerRowHeights = allHeaderRows.Count > 0 ? CalculateTableRowHeights(allHeaderRows, columnWidths, foTable.BorderSpacing) : new List<double>();
        var bodyRowHeights = allBodyRows.Count > 0 ? CalculateTableRowHeights(allBodyRows, columnWidths, foTable.BorderSpacing) : new List<double>();
        var footerRowHeights = allFooterRows.Count > 0 ? CalculateTableRowHeights(allFooterRows, columnWidths, foTable.BorderSpacing) : new List<double>();

        // Create grid for body rows (continuous across pages)
        var bodyGrid = new TableCellGrid();

        // Render header on first page
        if (foTable.Header != null && allHeaderRows.Count > 0)
        {
            // Headers get their own grid (independent from body)
            var headerGrid = new TableCellGrid();
            for (int headerIdx = 0; headerIdx < allHeaderRows.Count; headerIdx++)
            {
                var foRow = allHeaderRows[headerIdx];
                var rowArea = LayoutTableRowWithSpanning(foRow, 0, 0, columnWidths, foTable.BorderSpacing, headerGrid, headerIdx, headerRowHeights);
                if (rowArea != null)
                {
                    // Header row coordinates should be relative to its wrapper table, not absolute
                    rowArea.X = 0;
                    rowArea.Y = 0;

                    var headerTableArea = new TableArea
                    {
                        X = tableX,
                        Y = currentY,
                        Width = calculatedTableWidth,
                        Height = rowArea.Height,
                        BorderCollapse = foTable.BorderCollapse,
                        BorderSpacing = foTable.BorderSpacing,
                        ColumnWidths = columnWidths
                    };
                    headerTableArea.AddRow(rowArea);

                    currentPage.AddArea(headerTableArea);
                    currentY += rowArea.Height;
                }
            }
        }

        // Layout body rows with page breaking
        if (foTable.Body != null)
        {
            for (int bodyRowIdx = 0; bodyRowIdx < allBodyRows.Count; bodyRowIdx++)
            {
                var foRow = allBodyRows[bodyRowIdx];

                // Calculate row height first to check if it fits
                var rowArea = LayoutTableRowWithSpanning(foRow, 0, 0, columnWidths, foTable.BorderSpacing, bodyGrid, bodyRowIdx, bodyRowHeights);
                if (rowArea == null)
                    continue;

                var rowHeight = rowArea.Height;
                var pageBottom = currentPageMaster.PageHeight - bodyMarginBottom;

                // Check keep-together constraint
                var mustKeepTogether = foRow.KeepTogether == "always";

                // Check if row fits on current page
                var rowFitsOnPage = currentY + rowHeight <= pageBottom;

                if (!rowFitsOnPage)
                {
                    // Row doesn't fit - decide whether to break
                    // If we're at the top of the page and the row is too large, render it anyway
                    // Otherwise, create a new page
                    if (currentY > bodyMarginTop || mustKeepTogether)
                    {
                        // Render footer before page break (if footer repetition is enabled)
                        if (!foTable.TableOmitFooterAtBreak && foTable.Footer != null && allFooterRows.Count > 0)
                        {
                            RenderTableFooter(
                                foTable,
                                allFooterRows,
                                footerRowHeights,
                                columnWidths,
                                tableX,
                                calculatedTableWidth,
                                ref currentY,
                                currentPage);
                        }

                        // Create new page
                        RenderFloats(currentPage, currentPageMaster, bodyMarginTop);
                        RenderFootnotes(currentPage, currentPageMaster, pageSequence);
                        AddLinksToPage(currentPage);
                        CheckPageLimit(areaTree);
                        areaTree.AddPage(currentPage);
                        pageNumber++;
                        currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);

                        // Recalculate body margins for new page master
                        var regionBody = currentPageMaster.RegionBody;
                        var regionBodyMarginTop = regionBody?.MarginTop ?? 0;
                        var regionBodyMarginBottom = regionBody?.MarginBottom ?? 0;
                        var regionBodyMarginLeft = regionBody?.MarginLeft ?? 0;
                        var regionBodyMarginRight = regionBody?.MarginRight ?? 0;

                        var regionBeforeExtent = (currentPageMaster.RegionBefore as Dom.FoRegionBefore)?.Extent ?? 0;
                        var regionAfterExtent = (currentPageMaster.RegionAfter as Dom.FoRegionAfter)?.Extent ?? 0;

                        bodyMarginTop = currentPageMaster.MarginTop + regionBeforeExtent + regionBodyMarginTop;
                        bodyMarginBottom = currentPageMaster.MarginBottom + regionAfterExtent + regionBodyMarginBottom;
                        bodyMarginLeft = currentPageMaster.MarginLeft + regionBodyMarginLeft;
                        bodyMarginRight = currentPageMaster.MarginRight + regionBodyMarginRight;

                        bodyWidth = currentPageMaster.PageWidth - bodyMarginLeft - bodyMarginRight;
                        bodyHeight = currentPageMaster.PageHeight - bodyMarginTop - bodyMarginBottom;

                        currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                        currentY = bodyMarginTop;
                        currentColumn = 0;

                        // Render header on new page (if not omitted)
                        if (!foTable.TableOmitHeaderAtBreak && foTable.Header != null && allHeaderRows.Count > 0)
                        {
                            // Headers get their own grid (independent from body)
                            var headerGrid = new TableCellGrid();
                            for (int headerIdx = 0; headerIdx < allHeaderRows.Count; headerIdx++)
                            {
                                var headerRow = allHeaderRows[headerIdx];
                                var headerRowArea = LayoutTableRowWithSpanning(headerRow, 0, 0, columnWidths, foTable.BorderSpacing, headerGrid, headerIdx, headerRowHeights);
                                if (headerRowArea != null)
                                {
                                    // Header row coordinates should be relative to its wrapper table, not absolute
                                    headerRowArea.X = 0;
                                    headerRowArea.Y = 0;

                                    var headerTableArea = new TableArea
                                    {
                                        X = tableX,
                                        Y = currentY,
                                        Width = calculatedTableWidth,
                                        Height = headerRowArea.Height,
                                        BorderCollapse = foTable.BorderCollapse,
                                        BorderSpacing = foTable.BorderSpacing,
                                        ColumnWidths = columnWidths
                                    };
                                    headerTableArea.AddRow(headerRowArea);

                                    currentPage.AddArea(headerTableArea);
                                    currentY += headerRowArea.Height;
                                }
                            }
                        }

                        // NOTE: DO NOT re-layout the row here! The rowArea from line 1840 is still valid.
                        // Re-laying out would corrupt the bodyGrid state since it's already been updated.
                        // The row coordinates will be adjusted below (lines 1946-1947).
                    }
                    // else: row is too large for any page, render it anyway at current position
                }

                // Row coordinates should be relative to its wrapper table, not absolute
                rowArea.X = 0;
                rowArea.Y = 0;

                // Create a wrapper table area for just this row at the current Y position
                var rowTableArea = new TableArea
                {
                    X = tableX,
                    Y = currentY,  // Table is at absolute Y position
                    Width = calculatedTableWidth,
                    Height = rowArea.Height,
                    BorderCollapse = foTable.BorderCollapse,
                    BorderSpacing = foTable.BorderSpacing,
                    ColumnWidths = columnWidths
                };
                rowTableArea.AddRow(rowArea);

                currentPage.AddArea(rowTableArea);
                currentY += rowArea.Height;
            }
        }

        // Render footer at end of table (always)
        if (foTable.Footer != null && allFooterRows.Count > 0)
        {
            RenderTableFooter(
                foTable,
                allFooterRows,
                footerRowHeights,
                columnWidths,
                tableX,
                calculatedTableWidth,
                ref currentY,
                currentPage);
        }
    }

    /// <summary>
    /// Layouts a table-and-caption element with support for page breaking.
    /// The caption is positioned according to the caption-side property.
    /// </summary>
    private void LayoutTableAndCaptionWithPageBreaking(
        Dom.FoTableAndCaption foTableAndCaption,
        Dom.FoRoot foRoot,
        Dom.FoPageSequence pageSequence,
        AreaTree areaTree,
        ref PageViewport currentPage,
        ref Dom.FoSimplePageMaster currentPageMaster,
        ref int pageNumber,
        ref double currentY,
        ref int currentColumn,
        double tableX,
        double tableWidth,
        ref double bodyMarginTop,
        ref double bodyMarginBottom,
        ref double bodyMarginLeft,
        ref double bodyMarginRight,
        ref double bodyWidth,
        ref double bodyHeight)
    {
        var caption = foTableAndCaption.Caption;
        var table = foTableAndCaption.Table;

        if (table == null)
            return;

        // Get caption side (default: before)
        var captionSide = caption?.CaptionSide ?? "before";

        // Normalize caption-side to before/after
        // before = top, start; after = bottom, end
        var renderCaptionBefore = captionSide is "before" or "top" or "start";

        // Render caption before table if needed
        if (renderCaptionBefore && caption != null)
        {
            LayoutTableCaption(
                caption,
                foRoot,
                pageSequence,
                areaTree,
                ref currentPage,
                ref currentPageMaster,
                ref pageNumber,
                ref currentY,
                ref currentColumn,
                tableX,
                tableWidth,
                ref bodyMarginTop,
                ref bodyMarginBottom,
                ref bodyMarginLeft,
                ref bodyMarginRight,
                ref bodyWidth,
                ref bodyHeight);
        }

        // Layout the table
        LayoutTableWithPageBreaking(
            table,
            foRoot,
            pageSequence,
            areaTree,
            ref currentPage,
            ref currentPageMaster,
            ref pageNumber,
            ref currentY,
            ref currentColumn,
            tableX,
            tableWidth,
            ref bodyMarginTop,
            ref bodyMarginBottom,
            ref bodyMarginLeft,
            ref bodyMarginRight,
            ref bodyWidth,
            ref bodyHeight);

        // Render caption after table if needed
        if (!renderCaptionBefore && caption != null)
        {
            LayoutTableCaption(
                caption,
                foRoot,
                pageSequence,
                areaTree,
                ref currentPage,
                ref currentPageMaster,
                ref pageNumber,
                ref currentY,
                ref currentColumn,
                tableX,
                tableWidth,
                ref bodyMarginTop,
                ref bodyMarginBottom,
                ref bodyMarginLeft,
                ref bodyMarginRight,
                ref bodyWidth,
                ref bodyHeight);
        }
    }

    /// <summary>
    /// Layouts a table caption as a series of blocks.
    /// </summary>
    private void LayoutTableCaption(
        Dom.FoTableCaption caption,
        Dom.FoRoot foRoot,
        Dom.FoPageSequence pageSequence,
        AreaTree areaTree,
        ref PageViewport currentPage,
        ref Dom.FoSimplePageMaster currentPageMaster,
        ref int pageNumber,
        ref double currentY,
        ref int currentColumn,
        double captionX,
        double captionWidth,
        ref double bodyMarginTop,
        ref double bodyMarginBottom,
        ref double bodyMarginLeft,
        ref double bodyMarginRight,
        ref double bodyWidth,
        ref double bodyHeight)
    {
        // Layout each block in the caption using LayoutBlock
        foreach (var block in caption.Blocks)
        {
            var blockY = currentY + block.SpaceBefore;
            var blockArea = LayoutBlock(block, captionX, blockY, captionWidth, pageNumber);

            if (blockArea != null)
            {
                currentPage.AddArea(blockArea);

                // Update currentY for next block/element
                var blockTotalHeight = blockArea.SpaceBefore + blockArea.MarginTop +
                                      blockArea.Height + blockArea.MarginBottom + blockArea.SpaceAfter;
                currentY += blockTotalHeight;
            }
        }
    }

    /// <summary>
    /// Renders table footer rows at the current position.
    /// This helper is used both for footer repetition at page breaks and for the final footer.
    /// </summary>
    private void RenderTableFooter(
        Dom.FoTable foTable,
        List<Dom.FoTableRow> footerRows,
        List<double> footerRowHeights,
        List<double> columnWidths,
        double tableX,
        double calculatedTableWidth,
        ref double currentY,
        PageViewport currentPage)
    {
        // Footer gets its own grid (independent from body)
        var footerGrid = new TableCellGrid();

        for (int footerIdx = 0; footerIdx < footerRows.Count; footerIdx++)
        {
            var foRow = footerRows[footerIdx];
            var rowArea = LayoutTableRowWithSpanning(foRow, 0, 0, columnWidths, foTable.BorderSpacing, footerGrid, footerIdx, footerRowHeights);
            if (rowArea == null)
                continue;

            // Adjust row position to absolute coordinates
            rowArea.X = tableX;
            rowArea.Y = currentY;

            // Create a wrapper table area for just the footer row
            var footerTableArea = new TableArea
            {
                X = tableX,
                Y = currentY,
                Width = calculatedTableWidth,
                Height = rowArea.Height,
                BorderCollapse = foTable.BorderCollapse,
                BorderSpacing = foTable.BorderSpacing,
                ColumnWidths = columnWidths
            };
            footerTableArea.AddRow(rowArea);

            currentPage.AddArea(footerTableArea);
            currentY += rowArea.Height;
        }
    }

    private void LayoutListBlockWithPageBreaking(
        Dom.FoListBlock foList,
        Dom.FoRoot foRoot,
        Dom.FoPageSequence pageSequence,
        AreaTree areaTree,
        ref PageViewport currentPage,
        ref Dom.FoSimplePageMaster currentPageMaster,
        ref int pageNumber,
        ref double currentY,
        ref int currentColumn,
        double listX,
        double listWidth,
        ref double bodyMarginTop,
        ref double bodyMarginBottom,
        ref double bodyMarginLeft,
        ref double bodyMarginRight,
        ref double bodyWidth,
        ref double bodyHeight)
    {
        // Apply space-before at the start of the list
        currentY += foList.SpaceBefore;

        // Calculate label and body widths (same for all items in the list)
        var labelWidth = foList.ProvisionalDistanceBetweenStarts - foList.ProvisionalLabelSeparation;
        var bodyStartX = foList.ProvisionalDistanceBetweenStarts;
        var listBodyWidth = listWidth - bodyStartX;

        // Layout each list item with page breaking
        foreach (var foItem in foList.Items)
        {
            // Apply space-before for this item
            currentY += foItem.SpaceBefore;

            // First, layout the item to calculate its height
            var itemLabelAreas = new List<BlockArea>();
            var itemBodyAreas = new List<BlockArea>();
            double labelHeight = 0;
            double itemBodyHeight = 0;

            // Layout label
            if (foItem.Label != null)
            {
                var labelY = 0.0;
                foreach (var labelBlock in foItem.Label.Blocks)
                {
                    var labelBlockArea = LayoutBlock(labelBlock, 0, labelY, labelWidth);
                    if (labelBlockArea != null)
                    {
                        // Store relative position for now
                        labelBlockArea.X = 0;
                        labelBlockArea.Y = labelY;
                        itemLabelAreas.Add(labelBlockArea);
                        labelY += labelBlockArea.Height + labelBlockArea.MarginTop + labelBlockArea.MarginBottom;
                    }
                }
                labelHeight = labelY;
            }

            // Layout body
            if (foItem.Body != null)
            {
                var bodyY = 0.0;
                foreach (var bodyBlock in foItem.Body.Blocks)
                {
                    var bodyBlockArea = LayoutBlock(bodyBlock, 0, bodyY, listBodyWidth);
                    if (bodyBlockArea != null)
                    {
                        // Store relative position for now
                        bodyBlockArea.X = 0;
                        bodyBlockArea.Y = bodyY;
                        itemBodyAreas.Add(bodyBlockArea);
                        bodyY += bodyBlockArea.Height + bodyBlockArea.MarginTop + bodyBlockArea.MarginBottom;
                    }
                }
                itemBodyHeight = bodyY;
            }

            // List item height is the maximum of label and body heights
            var itemHeight = Math.Max(labelHeight, itemBodyHeight) + foItem.SpaceAfter;
            var pageBottom = currentPageMaster.PageHeight - bodyMarginBottom;

            // Check keep-together constraint
            var mustKeepTogether = foItem.KeepTogether == "always";

            // Check if item fits on current page
            var itemFitsOnPage = currentY + itemHeight <= pageBottom;

            if (!itemFitsOnPage)
            {
                // Item doesn't fit - decide whether to break
                // If we're at the top of the page and the item is too large, render it anyway
                // Otherwise, create a new page
                if (currentY > bodyMarginTop || mustKeepTogether)
                {
                    // Create new page
                    RenderFloats(currentPage, currentPageMaster, bodyMarginTop);
                    RenderFootnotes(currentPage, currentPageMaster, pageSequence);
                    AddLinksToPage(currentPage);
                    CheckPageLimit(areaTree);
                    areaTree.AddPage(currentPage);
                    pageNumber++;
                    currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);

                    // Recalculate body margins for new page master
                    var regionBody = currentPageMaster.RegionBody;
                    var regionBodyMarginTop = regionBody?.MarginTop ?? 0;
                    var regionBodyMarginBottom = regionBody?.MarginBottom ?? 0;
                    var regionBodyMarginLeft = regionBody?.MarginLeft ?? 0;
                    var regionBodyMarginRight = regionBody?.MarginRight ?? 0;

                    var regionBeforeExtent = (currentPageMaster.RegionBefore as Dom.FoRegionBefore)?.Extent ?? 0;
                    var regionAfterExtent = (currentPageMaster.RegionAfter as Dom.FoRegionAfter)?.Extent ?? 0;

                    bodyMarginTop = currentPageMaster.MarginTop + regionBeforeExtent + regionBodyMarginTop;
                    bodyMarginBottom = currentPageMaster.MarginBottom + regionAfterExtent + regionBodyMarginBottom;
                    bodyMarginLeft = currentPageMaster.MarginLeft + regionBodyMarginLeft;
                    bodyMarginRight = currentPageMaster.MarginRight + regionBodyMarginRight;

                    bodyWidth = currentPageMaster.PageWidth - bodyMarginLeft - bodyMarginRight;
                    bodyHeight = currentPageMaster.PageHeight - bodyMarginTop - bodyMarginBottom;

                    currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                    currentY = bodyMarginTop;
                    currentColumn = 0;

                    // Note: No need to re-layout the item, we already have the areas calculated
                }
                // else: item is too large for any page, render it anyway at current position
            }

            // Add label areas to page with absolute positions
            foreach (var labelArea in itemLabelAreas)
            {
                labelArea.X = listX;
                labelArea.Y = currentY + labelArea.Y;
                currentPage.AddArea(labelArea);
            }

            // Add body areas to page with absolute positions
            foreach (var bodyArea in itemBodyAreas)
            {
                bodyArea.X = listX + bodyStartX;
                bodyArea.Y = currentY + bodyArea.Y;
                currentPage.AddArea(bodyArea);
            }

            // Move Y position past this item
            currentY += itemHeight;
        }

        // Apply space-after at the end of the list
        currentY += foList.SpaceAfter;
    }

    private List<double> CalculateColumnWidths(Dom.FoTable foTable, double availableWidth)
    {
        var columnWidths = new List<double>();

        // If table has column specifications, use them
        if (foTable.Columns.Count > 0)
        {
            // Expand repeated columns and categorize them
            var expandedColumns = new List<(string widthSpec, double fixedWidth, double percentageValue, double proportionalValue)>();

            foreach (var column in foTable.Columns)
            {
                var repeat = column.NumberColumnsRepeated;
                var widthSpec = column.ColumnWidthString;

                for (int i = 0; i < repeat; i++)
                {
                    double fixedWidth = 0;
                    double percentageValue = 0;
                    double proportionalValue = 0;

                    if (widthSpec.StartsWith("proportional-column-width("))
                    {
                        // Parse proportional-column-width(N)
                        proportionalValue = ParseProportionalColumnWidth(widthSpec);
                    }
                    else if (widthSpec.EndsWith("%"))
                    {
                        // Percentage width (e.g., "25%")
                        if (double.TryParse(widthSpec.TrimEnd('%'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var pct))
                        {
                            percentageValue = Math.Max(0, Math.Min(100, pct)); // Clamp to 0-100%
                        }
                    }
                    else if (widthSpec != "auto")
                    {
                        // Fixed width (absolute length)
                        fixedWidth = column.ColumnWidth;
                    }
                    // else: auto width (all values remain 0)

                    expandedColumns.Add((widthSpec, fixedWidth, percentageValue, proportionalValue));
                }
            }

            // Calculate spacing
            var spacing = foTable.BorderSpacing * (expandedColumns.Count + 1);
            var availableForColumns = availableWidth - spacing;

            // Calculate widths for each category
            var totalFixedWidth = expandedColumns.Sum(c => c.fixedWidth);
            var totalPercentage = expandedColumns.Sum(c => c.percentageValue);
            var totalProportional = expandedColumns.Sum(c => c.proportionalValue);
            var autoCount = expandedColumns.Count(c =>
                Math.Abs(c.fixedWidth) < Epsilon &&
                Math.Abs(c.percentageValue) < Epsilon &&
                Math.Abs(c.proportionalValue) < Epsilon);

            // Calculate percentage widths (relative to available width for columns)
            var totalPercentageWidth = (totalPercentage / 100.0) * availableForColumns;

            // Remaining width for proportional and auto columns
            var remainingWidth = availableForColumns - totalFixedWidth - totalPercentageWidth;

            // For auto columns, measure content widths to determine optimal sizing
            List<double> autoColumnContentWidths = new List<double>();
            double totalAutoContentWidth = 0;

            if (autoCount > 0)
            {
                // Measure content widths for all columns
                var contentWidths = MeasureColumnContentWidths(foTable, expandedColumns.Count);

                // Extract content widths for auto columns only
                for (int i = 0; i < expandedColumns.Count; i++)
                {
                    var (_, fixedWidth, percentageValue, proportionalValue) = expandedColumns[i];
                    if (Math.Abs(fixedWidth) < Epsilon &&
                        Math.Abs(percentageValue) < Epsilon &&
                        Math.Abs(proportionalValue) < Epsilon) // auto column
                    {
                        var contentWidth = contentWidths[i];
                        autoColumnContentWidths.Add(contentWidth);
                        totalAutoContentWidth += contentWidth;
                    }
                }
            }

            // Distribute remaining width
            int autoColumnIndex = 0;
            foreach (var (_, fixedWidth, percentageValue, proportionalValue) in expandedColumns)
            {
                if (fixedWidth > Epsilon)
                {
                    // Fixed width column (absolute length)
                    columnWidths.Add(Math.Max(MinimumColumnWidth, fixedWidth));
                }
                else if (percentageValue > Epsilon)
                {
                    // Percentage width column
                    var percentageWidth = (percentageValue / 100.0) * availableForColumns;
                    columnWidths.Add(Math.Max(MinimumColumnWidth, percentageWidth));
                }
                else if (proportionalValue > Epsilon)
                {
                    // Proportional column - get its share of the remaining width
                    var proportionalWidth = totalProportional > Epsilon
                        ? remainingWidth * (proportionalValue / totalProportional)
                        : 0;
                    columnWidths.Add(Math.Max(MinimumColumnWidth, proportionalWidth));
                }
                else
                {
                    // Auto width column - distribute based on content width ratios
                    double autoWidth;
                    if (totalAutoContentWidth > Epsilon)
                    {
                        // Distribute remaining width proportional to content width
                        var contentWidth = autoColumnContentWidths[autoColumnIndex];
                        autoWidth = remainingWidth * (contentWidth / totalAutoContentWidth);
                    }
                    else
                    {
                        // No content or all empty - distribute equally
                        autoWidth = autoCount > 0 ? remainingWidth / autoCount : 0;
                    }

                    columnWidths.Add(Math.Max(MinimumColumnWidth, autoWidth));
                    autoColumnIndex++;
                }
            }
        }
        else
        {
            // No column specifications - determine from first row of body
            var cellCount = foTable.Body?.Rows.FirstOrDefault()?.Cells.Count ?? 1;
            var spacing = foTable.BorderSpacing * (cellCount + 1);
            var columnWidth = (availableWidth - spacing) / cellCount;

            for (int i = 0; i < cellCount; i++)
            {
                columnWidths.Add(columnWidth);
            }
        }

        return columnWidths;
    }

    /// <summary>
    /// Parses a proportional-column-width() function and returns the numeric value.
    /// Example: "proportional-column-width(2)" returns 2.0
    /// </summary>
    private double ParseProportionalColumnWidth(string widthSpec)
    {
        if (!widthSpec.StartsWith("proportional-column-width(") || !widthSpec.EndsWith(")"))
            return 1.0; // Default to 1 if malformed

        var valueStr = widthSpec.Substring("proportional-column-width(".Length, widthSpec.Length - "proportional-column-width(".Length - 1).Trim();

        if (double.TryParse(valueStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
            return Math.Max(0, value); // Ensure non-negative

        return 1.0; // Default to 1 if parsing fails
    }

    /// <summary>
    /// Calculates the width for a float element based on explicit width, content, or heuristics.
    /// </summary>
    /// <param name="foFloat">The float element</param>
    /// <param name="bodyWidth">Available body width</param>
    /// <returns>The calculated float width in points</returns>
    private double CalculateFloatWidth(Dom.FoFloat foFloat, double bodyWidth)
    {
        // Check for explicit width property
        var widthSpec = foFloat.Properties.GetString("width", "auto");

        if (widthSpec != "auto")
        {
            // Explicit width specified
            if (widthSpec.EndsWith("%"))
            {
                // Percentage width
                if (double.TryParse(widthSpec.TrimEnd('%'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var pct))
                {
                    return Math.Max(MinimumColumnWidth, (pct / 100.0) * bodyWidth);
                }
            }
            else
            {
                // Absolute length
                var explicitWidth = Dom.LengthParser.Parse(widthSpec);
                if (explicitWidth > Epsilon)
                {
                    return Math.Max(MinimumColumnWidth, explicitWidth);
                }
            }
        }

        // No explicit width - measure content to determine optimal width
        var contentWidth = MeasureFloatMinimumWidth(foFloat);

        // Use content width, but don't exceed 1/3 of body width (reasonable maximum for floats)
        var maxFloatWidth = bodyWidth / 3.0;
        return Math.Max(MinimumColumnWidth, Math.Min(contentWidth, maxFloatWidth));
    }

    /// <summary>
    /// Measures the minimum width needed to display the float's content.
    /// </summary>
    private double MeasureFloatMinimumWidth(Dom.FoFloat foFloat)
    {
        double maxWidth = 0;

        foreach (var block in foFloat.Blocks)
        {
            var blockMinWidth = MeasureBlockMinimumWidth(block);
            maxWidth = Math.Max(maxWidth, blockMinWidth);
        }

        // If no measurable content, use a reasonable default
        return maxWidth > Epsilon ? maxWidth : 150.0;
    }

    /// <summary>
    /// Validates and sanitizes a font size to ensure it's within reasonable bounds.
    /// </summary>
    private static double ValidateFontSize(double fontSize)
    {
        // Return default for invalid sizes (zero, negative, or NaN)
        if (double.IsNaN(fontSize) || fontSize < MinimumFontSize)
            return DefaultFontSize;

        return fontSize;
    }

    /// <summary>
    /// Measures the minimum width required for a block's content (longest word/element).
    /// This is used for content-based column sizing.
    /// </summary>
    private double MeasureBlockMinimumWidth(Dom.FoBlock foBlock)
    {
        // Get font metrics for this block
        var blockFontFamily = Fonts.PdfBaseFontMapper.ResolveFont(
            foBlock.FontFamily,
            foBlock.FontWeight,
            foBlock.FontStyle);

        var fontMetrics = new Fonts.FontMetrics
        {
            FamilyName = blockFontFamily,
            Size = ValidateFontSize(foBlock.FontSize)
        };

        var minWidth = 0.0;

        // Measure text content (find longest word)
        var text = foBlock.TextContent ?? "";
        if (!string.IsNullOrWhiteSpace(text))
        {
            // Split into words and find the longest
            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                var wordWidth = fontMetrics.MeasureWidth(word);
                minWidth = Math.Max(minWidth, wordWidth);
            }
        }

        // Handle inline elements
        foreach (var child in foBlock.Children)
        {
            if (child is Dom.FoInline inline)
            {
                var inlineText = inline.TextContent ?? "";
                if (!string.IsNullOrWhiteSpace(inlineText))
                {
                    var inlineFontFamily = Fonts.PdfBaseFontMapper.ResolveFont(
                        inline.FontFamily,
                        inline.FontWeight,
                        inline.FontStyle);

                    var inlineFontMetrics = new Fonts.FontMetrics
                    {
                        FamilyName = inlineFontFamily,
                        Size = ValidateFontSize(inline.FontSize ?? foBlock.FontSize)
                    };

                    var inlineWords = inlineText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in inlineWords)
                    {
                        var wordWidth = inlineFontMetrics.MeasureWidth(word);
                        minWidth = Math.Max(minWidth, wordWidth);
                    }
                }
            }
        }

        // Add padding to the minimum width
        minWidth += foBlock.PaddingLeft + foBlock.PaddingRight;

        return minWidth;
    }

    /// <summary>
    /// Measures the minimum width required for a table cell's content.
    /// Includes padding and borders.
    /// </summary>
    private double MeasureCellMinimumWidth(Dom.FoTableCell foCell)
    {
        var minWidth = 0.0;

        // Measure each block in the cell
        foreach (var foBlock in foCell.Blocks)
        {
            var blockMinWidth = MeasureBlockMinimumWidth(foBlock);
            minWidth = Math.Max(minWidth, blockMinWidth);
        }

        // Add cell padding and borders
        minWidth += foCell.PaddingLeft + foCell.PaddingRight + (foCell.BorderWidth * 2);

        return minWidth;
    }

    /// <summary>
    /// Measures content widths for all columns in a table.
    /// Returns a list of minimum widths for each column based on cell content.
    /// </summary>
    private List<double> MeasureColumnContentWidths(Dom.FoTable foTable, int columnCount)
    {
        var columnWidths = new List<double>();
        for (int i = 0; i < columnCount; i++)
            columnWidths.Add(0);

        // Collect all rows (header, body, footer)
        var allRows = new List<Dom.FoTableRow>();
        if (foTable.Header != null)
            allRows.AddRange(foTable.Header.Rows);
        if (foTable.Body != null)
            allRows.AddRange(foTable.Body.Rows);
        if (foTable.Footer != null)
            allRows.AddRange(foTable.Footer.Rows);

        // Measure content in each cell
        foreach (var row in allRows)
        {
            int columnIndex = 0;
            foreach (var cell in row.Cells)
            {
                if (columnIndex >= columnCount)
                    break;

                // Measure this cell's content
                var cellWidth = MeasureCellMinimumWidth(cell);

                // For spanning cells, divide the width across spanned columns
                var colSpan = cell.NumberColumnsSpanned;
                if (colSpan > 1)
                {
                    cellWidth /= colSpan;
                }

                // Update the maximum width for this column
                columnWidths[columnIndex] = Math.Max(columnWidths[columnIndex], cellWidth);

                columnIndex += colSpan;
            }
        }

        return columnWidths;
    }

    private TableRowArea? LayoutTableRow(Dom.FoTableRow foRow, double x, double y, List<double> columnWidths, double borderSpacing)
    {
        // Use the row spanning version with an empty grid (for backward compatibility)
        var grid = new TableCellGrid();
        var rowHeights = new List<double>(); // Empty for single-row layout
        return LayoutTableRowWithSpanning(foRow, x, y, columnWidths, borderSpacing, grid, 0, rowHeights);
    }

    /// <summary>
    /// Layouts a table row with support for row spanning.
    /// </summary>
    /// <param name="foRow">The table row to layout.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="columnWidths">The calculated column widths.</param>
    /// <param name="borderSpacing">The border spacing.</param>
    /// <param name="grid">The cell grid tracking spanning cells.</param>
    /// <param name="rowIndex">The current row index (0-based).</param>
    /// <param name="rowHeights">Pre-calculated row heights for row spanning.</param>
    private TableRowArea? LayoutTableRowWithSpanning(
        Dom.FoTableRow foRow,
        double x,
        double y,
        List<double> columnWidths,
        double borderSpacing,
        TableCellGrid grid,
        int rowIndex,
        List<double> rowHeights)
    {
        var rowArea = new TableRowArea
        {
            X = x,
            Y = y
        };

        var currentX = borderSpacing;
        var maxCellHeight = 0.0;
        int cellIndexInRow = 0;

        foreach (var foCell in foRow.Cells)
        {
            // Find the next available column (skip columns occupied by row-spanning cells)
            int columnIndex = grid.GetNextAvailableColumn(rowIndex, cellIndexInRow);

            // Calculate cell width (sum of spanned columns)
            var cellWidth = 0.0;
            var colSpan = foCell.NumberColumnsSpanned;
            for (int i = 0; i < colSpan && columnIndex + i < columnWidths.Count; i++)
            {
                cellWidth += columnWidths[columnIndex + i];
            }
            // Add spacing between spanned columns
            if (colSpan > 1)
                cellWidth += borderSpacing * (colSpan - 1);

            // Calculate cell height for row spanning
            var rowSpan = foCell.NumberRowsSpanned;
            double cellHeight = 0;

            // If row heights are available and cell spans multiple rows, sum them up
            if (rowHeights.Count > 0 && rowSpan > 1)
            {
                for (int i = 0; i < rowSpan && rowIndex + i < rowHeights.Count; i++)
                {
                    cellHeight += rowHeights[rowIndex + i];
                }
                // Add spacing between spanned rows (rowSpan > 1 guaranteed by outer condition)
                cellHeight += borderSpacing * (rowSpan - 1);
            }

            // Layout the cell
            var cellArea = LayoutTableCell(foCell, currentX, 0, cellWidth, cellHeight > 0 ? cellHeight : 0);
            if (cellArea != null)
            {
                cellArea.ColumnIndex = columnIndex;
                rowArea.AddCell(cellArea);

                // Reserve grid cells for this cell's span
                grid.ReserveCells(rowIndex, columnIndex, rowSpan, colSpan, cellArea);

                // Track maximum cell height (only for cells that don't span rows, or if row heights not available)
                if (rowSpan == 1 || rowHeights.Count == 0)
                {
                    maxCellHeight = Math.Max(maxCellHeight, cellArea.Height);
                }
            }

            currentX += cellWidth + borderSpacing;
            cellIndexInRow = columnIndex + colSpan;
        }

        // Use the calculated row height if available, otherwise use max cell height
        rowArea.Height = rowHeights.Count > rowIndex ? rowHeights[rowIndex] : maxCellHeight;
        rowArea.Width = currentX;

        return rowArea;
    }

    private TableCellArea? LayoutTableCell(Dom.FoTableCell foCell, double x, double y, double cellWidth, double specifiedCellHeight = 0)
    {
        var cellArea = new TableCellArea
        {
            X = x,
            Y = y,
            Width = cellWidth,
            NumberColumnsSpanned = foCell.NumberColumnsSpanned,
            NumberRowsSpanned = foCell.NumberRowsSpanned,
            PaddingTop = foCell.PaddingTop,
            PaddingBottom = foCell.PaddingBottom,
            PaddingLeft = foCell.PaddingLeft,
            PaddingRight = foCell.PaddingRight,
            BorderWidth = foCell.BorderWidth,
            BorderStyle = foCell.BorderStyle,
            BorderColor = foCell.BorderColor,
            BackgroundColor = foCell.BackgroundColor ?? "transparent",
            TextAlign = foCell.TextAlign,
            VerticalAlign = foCell.VerticalAlign
        };

        // Calculate content width (cell width minus padding)
        var contentWidth = cellWidth - foCell.PaddingLeft - foCell.PaddingRight - (foCell.BorderWidth * 2);
        var currentY = foCell.PaddingTop;

        // Layout blocks within the cell
        foreach (var foBlock in foCell.Blocks)
        {
            // Track markers within table scope
            foreach (var child in foBlock.Children)
            {
                if (child is Dom.FoMarker marker)
                {
                    var className = marker.MarkerClassName;
                    if (!string.IsNullOrEmpty(className))
                    {
                        if (!_tableMarkers.ContainsKey(className))
                            _tableMarkers[className] = new List<(int, Dom.FoMarker)>();

                        if (!_tableMarkerSequenceCounters.ContainsKey(className))
                            _tableMarkerSequenceCounters[className] = 0;

                        int sequence = _tableMarkerSequenceCounters[className]++;
                        _tableMarkers[className].Add((sequence, marker));
                    }
                }
            }

            var blockArea = LayoutBlock(foBlock, foCell.PaddingLeft, currentY, contentWidth);
            if (blockArea != null)
            {
                cellArea.AddChild(blockArea);
                currentY += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
            }
        }

        // Handle retrieve-table-marker elements
        foreach (var retrieveTableMarker in foCell.RetrieveTableMarkers)
        {
            var markerBlocks = RetrieveTableMarkerContent(retrieveTableMarker);
            foreach (var block in markerBlocks)
            {
                var blockArea = LayoutBlock(block, foCell.PaddingLeft, currentY, contentWidth);
                if (blockArea != null)
                {
                    cellArea.AddChild(blockArea);
                    currentY += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                }
            }
        }

        // Use specified height for row-spanning cells, otherwise use content height
        cellArea.Height = specifiedCellHeight > 0
            ? Math.Max(specifiedCellHeight, currentY + foCell.PaddingBottom)
            : currentY + foCell.PaddingBottom;

        return cellArea;
    }

    private ImageArea? LayoutImage(Dom.FoExternalGraphic graphic, double x, double y, double availableWidth)
    {
        var src = graphic.Src;
        if (string.IsNullOrWhiteSpace(src))
            return null;

        // Resolve relative paths
        var imagePath = src;
        if (src.StartsWith("url(") && src.EndsWith(")"))
        {
            imagePath = src.Substring(4, src.Length - 5).Trim('\'', '"');
        }

        // Security: Validate image path to prevent path traversal attacks
        if (!ValidateImagePath(imagePath))
        {
            // Path validation failed - reject the image
            return null;
        }

        // Load image data
        byte[]? imageData = null;
        double intrinsicWidth = 0;
        double intrinsicHeight = 0;
        string format = "";

        try
        {
            if (File.Exists(imagePath))
            {
                var fileInfo = new FileInfo(imagePath);

                // Security: Check image size limit to prevent DoS
                if (_options.MaxImageSizeBytes > 0 && fileInfo.Length > _options.MaxImageSizeBytes)
                {
                    // Image exceeds size limit
                    return null;
                }

                imageData = File.ReadAllBytes(imagePath);

                // Detect format and dimensions with DPI
                var imageInfo = DetectImageFormat(imageData);
                if (imageInfo == null)
                    return null;

                format = imageInfo.Format;

                // Convert pixel dimensions to points based on DPI
                var (widthInPoints, heightInPoints) = Images.ImageUtilities.GetIntrinsicSizeInPoints(
                    imageInfo,
                    _options.DefaultImageDpi);

                intrinsicWidth = widthInPoints;
                intrinsicHeight = heightInPoints;
            }
        }
        catch (Exception ex)
        {
            // Image not found or couldn't be loaded
            _options.Logger.Warning(
                $"Failed to load image from '{imagePath}': {ex.Message}. " +
                "Image will be skipped in the layout.",
                ex);
            return null;
        }

        if (imageData == null || intrinsicWidth == 0 || intrinsicHeight == 0)
            return null;

        // Calculate display dimensions
        var (displayWidth, displayHeight) = CalculateImageDimensions(
            graphic,
            intrinsicWidth,
            intrinsicHeight,
            availableWidth);

        var imageArea = new ImageArea
        {
            X = x,
            Y = y,
            Width = displayWidth,
            Height = displayHeight,
            Source = imagePath,
            Format = format,
            ImageData = imageData,
            IntrinsicWidth = intrinsicWidth,
            IntrinsicHeight = intrinsicHeight,
            Scaling = graphic.Scaling
        };

        return imageArea;
    }

    private Images.ImageInfo? DetectImageFormat(byte[] data)
    {
        // Use new image parser infrastructure
        var format = Images.ImageFormatDetector.Detect(data);

        try
        {
            switch (format)
            {
                case "JPEG":
                    {
                        var parser = new Images.Parsers.JpegParser();
                        return parser.Parse(data);
                    }

                case "PNG":
                    {
                        var parser = new Images.Parsers.PngParser();
                        return parser.Parse(data);
                    }

                case "BMP":
                    {
                        var parser = new Images.Parsers.BmpParser();
                        return parser.Parse(data);
                    }

                case "GIF":
                    {
                        var parser = new Images.Parsers.GifParser();
                        return parser.Parse(data);
                    }

                case "TIFF":
                    {
                        var parser = new Images.Parsers.TiffParser();
                        return parser.Parse(data);
                    }

                case "SVG":
                    {
                        // SVG dimensions are extracted during layout, return null (handled separately)
                        return null;
                    }

                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            // Parser failed, return null
            _options.Logger.Debug($"Image format parsing failed: {ex.Message}");
            return null;
        }
    }

    private (double Width, double Height) GetJpegDimensions(byte[] data)
    {
        // Parse JPEG markers to find SOF (Start of Frame) marker
        int offset = 2; // Skip initial FF D8

        while (offset < data.Length - 1)
        {
            if (data[offset] != 0xFF)
                break;

            byte marker = data[offset + 1];
            offset += 2;

            // SOF markers (C0-CF except C4, C8, CC)
            if (marker >= 0xC0 && marker <= 0xCF &&
                marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
            {
                // Read segment length
                if (offset + 7 >= data.Length)
                    break;

                int height = (data[offset + 3] << 8) | data[offset + 4];
                int width = (data[offset + 5] << 8) | data[offset + 6];

                return (width, height);
            }

            // Read segment length and skip
            if (offset + 2 > data.Length)
                break;

            int length = (data[offset] << 8) | data[offset + 1];
            offset += length;
        }

        return (100, 100); // Default fallback
    }

    private (double Width, double Height) GetPngDimensions(byte[] data)
    {
        // PNG IHDR chunk is always at offset 8 and contains width/height at offset 16/20
        if (data.Length < 24)
            return (100, 100);

        int width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
        int height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];

        return (width, height);
    }

    /// <summary>
    /// Loads a background image and attaches it to a BlockArea.
    /// </summary>
    private void LoadBackgroundImage(BlockArea blockArea, string backgroundImageUri)
    {
        if (string.IsNullOrWhiteSpace(backgroundImageUri))
            return;

        // Resolve relative paths and url() syntax
        var imagePath = backgroundImageUri;
        if (backgroundImageUri.StartsWith("url(") && backgroundImageUri.EndsWith(")"))
        {
            imagePath = backgroundImageUri.Substring(4, backgroundImageUri.Length - 5).Trim('\'', '"');
        }

        // Security: Validate image path to prevent path traversal attacks
        if (!ValidateImagePath(imagePath))
        {
            // Path validation failed - reject the image
            return;
        }

        // Load image data
        try
        {
            if (File.Exists(imagePath))
            {
                var fileInfo = new FileInfo(imagePath);

                // Security: Check image size limit to prevent DoS
                if (_options.MaxImageSizeBytes > 0 && fileInfo.Length > _options.MaxImageSizeBytes)
                {
                    // Image exceeds size limit
                    return;
                }

                var imageData = File.ReadAllBytes(imagePath);

                // Detect format and dimensions with DPI
                var imageInfo = DetectImageFormat(imageData);

                if (imageInfo != null && imageInfo.Width > 0 && imageInfo.Height > 0)
                {
                    // Convert pixel dimensions to points based on DPI
                    var (widthInPoints, heightInPoints) = Images.ImageUtilities.GetIntrinsicSizeInPoints(
                        imageInfo,
                        _options.DefaultImageDpi);

                    blockArea.BackgroundImage = imagePath;
                    blockArea.BackgroundImageData = imageData;
                    blockArea.BackgroundImageFormat = imageInfo.Format;
                    blockArea.BackgroundImageWidth = widthInPoints;
                    blockArea.BackgroundImageHeight = heightInPoints;
                }
            }
        }
        catch (FileNotFoundException)
        {
            // Image file not found - background images are optional, continue rendering
        }
        catch (UnauthorizedAccessException)
        {
            // Insufficient permissions to read image - continue rendering without it
        }
        catch (IOException)
        {
            // I/O error reading image file - continue rendering without it
        }
        catch (InvalidDataException)
        {
            // Image format is invalid or corrupted - continue rendering without it
        }
    }

    /// <summary>
    /// Loads a background image and attaches it to an AbsolutePositionedArea.
    /// </summary>
    private void LoadBackgroundImage(AbsolutePositionedArea area, string backgroundImageUri)
    {
        if (string.IsNullOrWhiteSpace(backgroundImageUri))
            return;

        // Resolve relative paths and url() syntax
        var imagePath = backgroundImageUri;
        if (backgroundImageUri.StartsWith("url(") && backgroundImageUri.EndsWith(")"))
        {
            imagePath = backgroundImageUri.Substring(4, backgroundImageUri.Length - 5).Trim('\'', '"');
        }

        // Security: Validate image path to prevent path traversal attacks
        if (!ValidateImagePath(imagePath))
        {
            // Path validation failed - reject the image
            return;
        }

        // Load image data
        try
        {
            if (File.Exists(imagePath))
            {
                var fileInfo = new FileInfo(imagePath);

                // Security: Check image size limit to prevent DoS
                if (_options.MaxImageSizeBytes > 0 && fileInfo.Length > _options.MaxImageSizeBytes)
                {
                    // Image exceeds size limit
                    return;
                }

                var imageData = File.ReadAllBytes(imagePath);

                // Detect format and dimensions with DPI
                var imageInfo = DetectImageFormat(imageData);

                if (imageInfo != null && imageInfo.Width > 0 && imageInfo.Height > 0)
                {
                    // Convert pixel dimensions to points based on DPI
                    var (widthInPoints, heightInPoints) = Images.ImageUtilities.GetIntrinsicSizeInPoints(
                        imageInfo,
                        _options.DefaultImageDpi);

                    area.BackgroundImage = imagePath;
                    area.BackgroundImageData = imageData;
                    area.BackgroundImageFormat = imageInfo.Format;
                    area.BackgroundImageWidth = widthInPoints;
                    area.BackgroundImageHeight = heightInPoints;
                }
            }
        }
        catch (FileNotFoundException)
        {
            // Image file not found - background images are optional, continue rendering
        }
        catch (UnauthorizedAccessException)
        {
            // Insufficient permissions to read image - continue rendering without it
        }
        catch (IOException)
        {
            // I/O error reading image file - continue rendering without it
        }
        catch (InvalidDataException)
        {
            // Image format is invalid or corrupted - continue rendering without it
        }
    }

    private (double Width, double Height) CalculateImageDimensions(
        Dom.FoExternalGraphic graphic,
        double intrinsicWidth,
        double intrinsicHeight,
        double availableWidth)
    {
        // Parse content-width and content-height
        var contentWidth = graphic.ContentWidth;
        var contentHeight = graphic.ContentHeight;

        double? explicitWidth = null;
        double? explicitHeight = null;

        if (contentWidth != "auto")
        {
            explicitWidth = Dom.LengthParser.Parse(contentWidth);
        }

        if (contentHeight != "auto")
        {
            explicitHeight = Dom.LengthParser.Parse(contentHeight);
        }

        // If both dimensions specified, use them
        if (explicitWidth.HasValue && explicitHeight.HasValue)
        {
            return (explicitWidth.Value, explicitHeight.Value);
        }

        // If only width specified, calculate height maintaining aspect ratio
        if (explicitWidth.HasValue)
        {
            var aspectRatio = intrinsicHeight / intrinsicWidth;
            return (explicitWidth.Value, explicitWidth.Value * aspectRatio);
        }

        // If only height specified, calculate width maintaining aspect ratio
        if (explicitHeight.HasValue)
        {
            var aspectRatio = intrinsicWidth / intrinsicHeight;
            return (explicitHeight.Value * aspectRatio, explicitHeight.Value);
        }

        // Auto sizing - use intrinsic size but constrain to available width
        if (intrinsicWidth > availableWidth)
        {
            var aspectRatio = intrinsicHeight / intrinsicWidth;
            return (availableWidth, availableWidth * aspectRatio);
        }

        return (intrinsicWidth, intrinsicHeight);
    }

    /// <summary>
    /// Routes to either LayoutImage or LayoutSvg based on file format.
    /// </summary>
    private Area? LayoutImageOrSvg(string src, Dom.FoExternalGraphic graphic, double x, double y, double availableWidth)
    {
        if (string.IsNullOrWhiteSpace(src))
            return null;

        // Resolve relative paths
        var imagePath = src;
        if (src.StartsWith("url(") && src.EndsWith(")"))
        {
            imagePath = src.Substring(4, src.Length - 5).Trim('\'', '"');
        }

        // Security: Validate image path
        if (!ValidateImagePath(imagePath))
            return null;

        // Load file to detect format
        try
        {
            if (File.Exists(imagePath))
            {
                var fileInfo = new FileInfo(imagePath);

                // Security: Check image size limit
                if (_options.MaxImageSizeBytes > 0 && fileInfo.Length > _options.MaxImageSizeBytes)
                    return null;

                var imageData = File.ReadAllBytes(imagePath);
                var format = Images.ImageFormatDetector.Detect(imageData);

                if (format == "SVG")
                {
                    // Handle SVG file
                    return LayoutSvgFromFile(imagePath, imageData, graphic, x, y, availableWidth);
                }
                else
                {
                    // Handle raster image using existing method
                    return LayoutImage(graphic, x, y, availableWidth);
                }
            }
        }
        catch
        {
            // File not found or couldn't be loaded
        }

        return null;
    }

    /// <summary>
    /// Layouts SVG content from fo:instream-foreign-object.
    /// </summary>
    private SvgArea? LayoutEmbeddedSvg(Dom.FoInstreamForeignObject foreignObject, double x, double y, double availableWidth)
    {
        if (foreignObject.ForeignContent == null)
            return null;

        try
        {
            // Parse the SVG XML element by converting it to bytes
            var svgXml = foreignObject.ForeignContent.ToString();
            var svgBytes = System.Text.Encoding.UTF8.GetBytes(svgXml);
            var svgDoc = Svg.SvgDocument.Parse(svgBytes);
            if (svgDoc == null)
                return null;

            // Extract intrinsic dimensions from SVG
            var (intrinsicWidth, intrinsicHeight) = GetSvgDimensions(svgDoc);

            // Calculate display dimensions using same logic as images
            var (displayWidth, displayHeight) = CalculateSvgDimensions(
                foreignObject,
                intrinsicWidth,
                intrinsicHeight,
                availableWidth);

            return new SvgArea
            {
                X = x,
                Y = y,
                Width = displayWidth,
                Height = displayHeight,
                SvgDocument = svgDoc,
                Scaling = foreignObject.Scaling,
                IntrinsicWidth = intrinsicWidth,
                IntrinsicHeight = intrinsicHeight
            };
        }
        catch (Exception ex)
        {
            // SVG parsing failed
            _options.Logger.Warning(
                $"Failed to parse embedded SVG from foreignObject: {ex.Message}. " +
                "SVG will be skipped in the layout.",
                ex);
            return null;
        }
    }

    /// <summary>
    /// Layouts SVG content from an external file via fo:external-graphic.
    /// </summary>
    private SvgArea? LayoutSvgFromFile(string path, byte[] svgData, Dom.FoExternalGraphic graphic, double x, double y, double availableWidth)
    {
        try
        {
            // Parse SVG from file data
            var svgDoc = Svg.SvgDocument.Parse(svgData);
            if (svgDoc == null)
                return null;

            // Extract intrinsic dimensions from SVG
            var (intrinsicWidth, intrinsicHeight) = GetSvgDimensions(svgDoc);

            // Calculate display dimensions
            var (displayWidth, displayHeight) = CalculateImageDimensions(
                graphic,
                intrinsicWidth,
                intrinsicHeight,
                availableWidth);

            return new SvgArea
            {
                X = x,
                Y = y,
                Width = displayWidth,
                Height = displayHeight,
                SvgDocument = svgDoc,
                Source = path,
                Scaling = graphic.Scaling,
                IntrinsicWidth = intrinsicWidth,
                IntrinsicHeight = intrinsicHeight
            };
        }
        catch (Exception ex)
        {
            // SVG parsing failed
            _options.Logger.Warning(
                $"Failed to parse SVG from file '{path}': {ex.Message}. " +
                "SVG will be skipped in the layout.",
                ex);
            return null;
        }
    }

    /// <summary>
    /// Extracts intrinsic dimensions from an SVG document.
    /// </summary>
    private (double Width, double Height) GetSvgDimensions(Svg.SvgDocument svgDoc)
    {
        // Use effective dimensions which already fall back correctly
        double width = svgDoc.EffectiveWidthPt;
        double height = svgDoc.EffectiveHeightPt;

        // If still zero, use reasonable defaults
        if (width == 0) width = 100;
        if (height == 0) height = 100;

        return (width, height);
    }

    /// <summary>
    /// Calculates display dimensions for SVG based on FO properties.
    /// </summary>
    private (double Width, double Height) CalculateSvgDimensions(
        Dom.FoInstreamForeignObject foreignObject,
        double intrinsicWidth,
        double intrinsicHeight,
        double availableWidth)
    {
        // Parse content-width and content-height
        var contentWidth = foreignObject.ContentWidth;
        var contentHeight = foreignObject.ContentHeight;

        double? explicitWidth = null;
        double? explicitHeight = null;

        if (contentWidth != "auto")
        {
            explicitWidth = Dom.LengthParser.Parse(contentWidth);
        }

        if (contentHeight != "auto")
        {
            explicitHeight = Dom.LengthParser.Parse(contentHeight);
        }

        // If both dimensions specified, use them
        if (explicitWidth.HasValue && explicitHeight.HasValue)
        {
            return (explicitWidth.Value, explicitHeight.Value);
        }

        // If only width specified, calculate height maintaining aspect ratio
        if (explicitWidth.HasValue)
        {
            var aspectRatio = intrinsicHeight / intrinsicWidth;
            return (explicitWidth.Value, explicitWidth.Value * aspectRatio);
        }

        // If only height specified, calculate width maintaining aspect ratio
        if (explicitHeight.HasValue)
        {
            var aspectRatio = intrinsicWidth / intrinsicHeight;
            return (explicitHeight.Value * aspectRatio, explicitHeight.Value);
        }

        // Auto sizing - use intrinsic size but constrain to available width
        if (intrinsicWidth > availableWidth)
        {
            var aspectRatio = intrinsicHeight / intrinsicWidth;
            return (availableWidth, availableWidth * aspectRatio);
        }

        return (intrinsicWidth, intrinsicHeight);
    }

    private BlockArea? LayoutListBlock(Dom.FoListBlock foList, double x, double y, double availableWidth)
    {
        var listArea = new BlockArea
        {
            X = x,
            Y = y + foList.SpaceBefore,
            Width = availableWidth,
            Height = 0,
            StructureHint = "List" // Mark as list for PDF tagging
        };

        var currentY = 0.0;

        // Layout each list item
        foreach (var foItem in foList.Items)
        {
            currentY += foItem.SpaceBefore;

            var labelWidth = foList.ProvisionalDistanceBetweenStarts - foList.ProvisionalLabelSeparation;
            var bodyStartX = foList.ProvisionalDistanceBetweenStarts;
            var bodyWidth = availableWidth - bodyStartX;

            double labelHeight = 0;
            double bodyHeight = 0;

            // Create list item container
            var listItemArea = new BlockArea
            {
                X = 0,
                Y = currentY,
                Width = availableWidth,
                Height = 0,
                StructureHint = "ListItem" // Mark as list item for PDF tagging
            };

            // Layout label
            if (foItem.Label != null)
            {
                // Create label container
                var labelContainer = new BlockArea
                {
                    X = 0,
                    Y = 0,
                    Width = labelWidth,
                    Height = 0,
                    StructureHint = "ListLabel" // Mark as list label for PDF tagging
                };

                var labelY = 0.0;
                foreach (var labelBlock in foItem.Label.Blocks)
                {
                    var labelBlockArea = LayoutBlock(labelBlock, 0, labelY, labelWidth);
                    if (labelBlockArea != null)
                    {
                        labelContainer.AddChild(labelBlockArea);
                        labelY += labelBlockArea.Height + labelBlockArea.MarginTop + labelBlockArea.MarginBottom;
                    }
                }
                labelContainer.Height = labelY;
                labelHeight = labelY;
                listItemArea.AddChild(labelContainer);
            }

            // Layout body
            if (foItem.Body != null)
            {
                // Create body container
                var bodyContainer = new BlockArea
                {
                    X = bodyStartX,
                    Y = 0,
                    Width = bodyWidth,
                    Height = 0,
                    StructureHint = "ListBody" // Mark as list body for PDF tagging
                };

                var bodyY = 0.0;
                foreach (var bodyBlock in foItem.Body.Blocks)
                {
                    var bodyBlockArea = LayoutBlock(bodyBlock, 0, bodyY, bodyWidth);
                    if (bodyBlockArea != null)
                    {
                        bodyContainer.AddChild(bodyBlockArea);
                        bodyY += bodyBlockArea.Height + bodyBlockArea.MarginTop + bodyBlockArea.MarginBottom;
                    }
                }
                bodyContainer.Height = bodyY;
                bodyHeight = bodyY;
                listItemArea.AddChild(bodyContainer);
            }

            // List item height is the maximum of label and body heights
            var itemHeight = Math.Max(labelHeight, bodyHeight);
            listItemArea.Height = itemHeight;
            listArea.AddChild(listItemArea);
            currentY += itemHeight + foItem.SpaceAfter;
        }

        listArea.Height = currentY + foList.SpaceAfter;

        return listArea;
    }

    private void RenderFootnotes(PageViewport page, Dom.FoSimplePageMaster pageMaster, Dom.FoPageSequence pageSequence)
    {
        if (_currentPageFootnotes.Count == 0)
            return;

        // Calculate footnote area position
        var regionBody = pageMaster.RegionBody;
        var bodyMarginLeft = regionBody?.MarginLeft ?? 72;
        var bodyMarginRight = regionBody?.MarginRight ?? 72;
        var bodyMarginBottom = regionBody?.MarginBottom ?? 72;

        var bodyWidth = pageMaster.PageWidth - bodyMarginLeft - bodyMarginRight;

        // Start footnotes 36pt from bottom margin
        var footnoteY = pageMaster.PageHeight - bodyMarginBottom - 36;
        var currentY = footnoteY;

        // Render footnote separator if defined
        var separatorContent = pageSequence.StaticContents.FirstOrDefault(sc => sc.FlowName == "xsl-footnote-separator");
        if (separatorContent != null)
        {
            // Render separator blocks (typically contains a leader for a horizontal line)
            foreach (var block in separatorContent.Blocks)
            {
                var blockArea = LayoutBlock(block, bodyMarginLeft, currentY, bodyWidth);
                if (blockArea != null)
                {
                    page.AddArea(blockArea);
                    currentY += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                }
            }
        }

        // Render each footnote body
        foreach (var footnote in _currentPageFootnotes)
        {
            if (footnote.FootnoteBody != null)
            {
                foreach (var block in footnote.FootnoteBody.Blocks)
                {
                    var blockArea = LayoutBlock(block, bodyMarginLeft, currentY, bodyWidth);
                    if (blockArea != null)
                    {
                        page.AddArea(blockArea);
                        currentY += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                    }
                }
            }
        }

        // Clear footnotes for next page
        _currentPageFootnotes.Clear();
    }

    private void RenderFloats(PageViewport page, Dom.FoSimplePageMaster pageMaster, double currentY)
    {
        if (_currentPageFloats.Count == 0)
            return;

        var regionBody = pageMaster.RegionBody;
        var bodyMarginLeft = regionBody?.MarginLeft ?? 72;
        var bodyMarginRight = regionBody?.MarginRight ?? 72;
        var bodyWidth = pageMaster.PageWidth - bodyMarginLeft - bodyMarginRight;

        // Track separate Y positions for start (left) and end (right) floats
        var startFloatY = currentY;
        var endFloatY = currentY;

        foreach (var foFloat in _currentPageFloats)
        {
            if (foFloat.Blocks.Count == 0)
                continue;

            // Determine float position ("start" = left, "end" = right)
            var floatPosition = foFloat.Float?.ToLowerInvariant() ?? "start";
            var isStartFloat = floatPosition == "start" || floatPosition == "left";

            // Calculate float width
            var floatWidth = CalculateFloatWidth(foFloat, bodyWidth);

            // Calculate X position based on float side
            var floatX = isStartFloat
                ? bodyMarginLeft
                : pageMaster.PageWidth - bodyMarginRight - floatWidth;

            // Use appropriate Y position based on float side
            var floatY = isStartFloat ? startFloatY : endFloatY;

            // Layout the float blocks
            var floatTotalHeight = 0.0;
            foreach (var block in foFloat.Blocks)
            {
                var blockArea = LayoutBlock(block, floatX, floatY + floatTotalHeight, floatWidth);
                if (blockArea != null)
                {
                    page.AddArea(blockArea);
                    floatTotalHeight += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                }
            }

            // Update Y position for this float side
            if (isStartFloat)
                startFloatY += floatTotalHeight;
            else
                endFloatY += floatTotalHeight;
        }

        // Clear floats for next page
        _currentPageFloats.Clear();
    }

    private void AddLinksToPage(PageViewport page)
    {
        if (_currentPageLinks.Count == 0)
            return;

        // Add all collected links to the page
        foreach (var link in _currentPageLinks)
        {
            page.AddLink(link);
        }

        // Clear links for next page
        _currentPageLinks.Clear();
    }


    /// <summary>
    /// Checks if adding a new page would exceed the maximum page limit.
    /// Throws an exception if the limit would be exceeded.
    /// </summary>
    private void CheckPageLimit(AreaTree areaTree)
    {
        if (_options.MaxPages > 0 && areaTree.Pages.Count >= _options.MaxPages)
        {
            throw new InvalidOperationException(
                $"Maximum page limit of {_options.MaxPages} exceeded. " +
                "This limit prevents DoS attacks from malicious documents. " +
                "Increase LayoutOptions.MaxPages if this is a legitimate large document.");
        }
    }

    /// <summary>
    /// Validates an image path to prevent path traversal attacks.
    /// Returns true if the path is allowed, false otherwise.
    /// </summary>
    private bool ValidateImagePath(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return false;

        try
        {
            // Get the full canonical path
            var fullPath = Path.GetFullPath(imagePath);

            // Check if absolute paths are allowed
            if (Path.IsPathRooted(imagePath) && !_options.AllowAbsoluteImagePaths)
            {
                return false;
            }

            // If AllowedImageBasePath is set, ensure the path is within that directory
            if (!string.IsNullOrWhiteSpace(_options.AllowedImageBasePath))
            {
                var basePath = Path.GetFullPath(_options.AllowedImageBasePath);

                // Ensure both paths end with directory separator for proper comparison
                if (!basePath.EndsWith(Path.DirectorySeparatorChar))
                {
                    basePath += Path.DirectorySeparatorChar;
                }

                // Check if the full path starts with the base path (case-insensitive on Windows)
                var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!fullPath.StartsWith(basePath, comparison))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            // If path resolution fails, reject it
            _options.Logger.Warning(
                $"Image path validation failed for '{imagePath}': {ex.Message}. " +
                "Path will be rejected for security reasons.",
                ex);
            return false;
        }
    }

    /// <summary>
    /// Calculates the optimal split point for a block to avoid widow/orphan lines.
    /// Returns the number of lines that should go on the first page, or 0 if the block shouldn't be split.
    /// </summary>
    private static int CalculateOptimalSplitPoint(
        BlockArea blockArea,
        Dom.FoBlock foBlock,
        double availableHeight,
        double lineHeight)
    {
        // Check if this block contains line areas (text block with multiple lines)
        var lineAreas = blockArea.Children.OfType<LineArea>().ToList();
        if (lineAreas.Count == 0)
            return 0; // Not a text block with lines

        // Calculate how many complete lines fit in the available height
        // Account for padding and margins
        var availableForLines = availableHeight - blockArea.PaddingTop - blockArea.MarginTop;
        var maxLinesThatFit = (int)Math.Floor(availableForLines / lineHeight);

        // If all lines fit, no split needed
        if (maxLinesThatFit >= lineAreas.Count)
            return 0;

        // If no lines fit (or only one would fit and we need at least orphans), don't split here
        if (maxLinesThatFit < foBlock.Orphans)
            return 0;

        // Calculate the split point considering widow/orphan constraints
        var linesForFirstPage = maxLinesThatFit;
        var linesForSecondPage = lineAreas.Count - linesForFirstPage;

        // Check orphan constraint (minimum lines on first page)
        if (linesForFirstPage < foBlock.Orphans)
            return 0; // Can't satisfy orphan constraint

        // Check widow constraint (minimum lines on second page)
        if (linesForSecondPage < foBlock.Widows)
        {
            // Try to move some lines to second page to satisfy widow constraint
            linesForFirstPage = lineAreas.Count - foBlock.Widows;
            linesForSecondPage = foBlock.Widows;

            // Verify orphan constraint still holds
            if (linesForFirstPage < foBlock.Orphans)
                return 0; // Can't satisfy both constraints
        }

        return linesForFirstPage;
    }

    /// <summary>
    /// Splits a block area at the specified line number, returning two block areas.
    /// The first block contains lines [0, splitAtLine), the second contains [splitAtLine, end).
    /// </summary>
    private static (BlockArea firstPart, BlockArea secondPart) SplitBlockAtLine(
        BlockArea originalBlock,
        Dom.FoBlock foBlock,
        int splitAtLine,
        double firstPageX,
        double firstPageY,
        double secondPageX,
        double secondPageY)
    {
        // Get all line areas
        var allLines = originalBlock.Children.OfType<LineArea>().ToList();

        // Create first block (lines 0 to splitAtLine-1)
        var firstBlock = new BlockArea
        {
            X = firstPageX + foBlock.MarginLeft,
            Y = firstPageY + foBlock.MarginTop,
            Width = originalBlock.Width,
            FontFamily = originalBlock.FontFamily,
            FontSize = originalBlock.FontSize,
            TextAlign = originalBlock.TextAlign,
            MarginTop = originalBlock.MarginTop,
            MarginBottom = 0, // No bottom margin on split first part
            MarginLeft = originalBlock.MarginLeft,
            MarginRight = originalBlock.MarginRight,
            SpaceBefore = originalBlock.SpaceBefore,
            SpaceAfter = 0, // No space-after on split first part
            PaddingTop = originalBlock.PaddingTop,
            PaddingBottom = 0, // No bottom padding on split first part
            PaddingLeft = originalBlock.PaddingLeft,
            PaddingRight = originalBlock.PaddingRight,
            BackgroundColor = originalBlock.BackgroundColor,
            BorderTopWidth = originalBlock.BorderTopWidth,
            BorderBottomWidth = 0, // No bottom border on split first part
            BorderLeftWidth = originalBlock.BorderLeftWidth,
            BorderRightWidth = originalBlock.BorderRightWidth,
            BorderTopStyle = originalBlock.BorderTopStyle,
            BorderBottomStyle = "none",
            BorderLeftStyle = originalBlock.BorderLeftStyle,
            BorderRightStyle = originalBlock.BorderRightStyle,
            BorderTopColor = originalBlock.BorderTopColor,
            BorderLeftColor = originalBlock.BorderLeftColor,
            BorderRightColor = originalBlock.BorderRightColor,
            BorderTopLeftRadius = originalBlock.BorderTopLeftRadius,
            BorderTopRightRadius = originalBlock.BorderTopRightRadius,
            BorderBottomLeftRadius = 0, // No bottom radius on split first part
            BorderBottomRightRadius = 0 // No bottom radius on split first part
        };

        var currentY = firstBlock.PaddingTop;
        for (int i = 0; i < splitAtLine; i++)
        {
            var line = allLines[i];
            var newLine = new LineArea
            {
                X = line.X,
                Y = currentY,
                Width = line.Width,
                Height = line.Height
            };

            // Copy inline areas
            foreach (var inline in line.Inlines)
            {
                newLine.AddInline(new InlineArea
                {
                    X = inline.X,
                    Y = inline.Y,
                    Width = inline.Width,
                    Height = inline.Height,
                    Text = inline.Text,
                    FontFamily = inline.FontFamily,
                    FontSize = inline.FontSize,
                    FontWeight = inline.FontWeight,
                    FontStyle = inline.FontStyle,
                    Color = inline.Color,
                    TextDecoration = inline.TextDecoration,
                    BackgroundColor = inline.BackgroundColor,
                    BaselineOffset = inline.BaselineOffset,
                    Direction = inline.Direction,
                    WordSpacing = inline.WordSpacing
                });
            }

            firstBlock.AddChild(newLine);
            currentY += line.Height;
        }
        firstBlock.Height = currentY; // No bottom padding on split part

        // Create second block (lines splitAtLine to end)
        var secondBlock = new BlockArea
        {
            X = secondPageX + foBlock.MarginLeft,
            Y = secondPageY, // No top margin on split second part
            Width = originalBlock.Width,
            FontFamily = originalBlock.FontFamily,
            FontSize = originalBlock.FontSize,
            TextAlign = originalBlock.TextAlign,
            MarginTop = 0, // No top margin on split second part
            MarginBottom = originalBlock.MarginBottom,
            MarginLeft = originalBlock.MarginLeft,
            MarginRight = originalBlock.MarginRight,
            SpaceBefore = 0, // No space-before on split second part
            SpaceAfter = originalBlock.SpaceAfter,
            PaddingTop = 0, // No top padding on split second part
            PaddingBottom = originalBlock.PaddingBottom,
            PaddingLeft = originalBlock.PaddingLeft,
            PaddingRight = originalBlock.PaddingRight,
            BackgroundColor = originalBlock.BackgroundColor,
            BorderTopWidth = 0, // No top border on split second part
            BorderBottomWidth = originalBlock.BorderBottomWidth,
            BorderLeftWidth = originalBlock.BorderLeftWidth,
            BorderRightWidth = originalBlock.BorderRightWidth,
            BorderTopStyle = "none",
            BorderBottomStyle = originalBlock.BorderBottomStyle,
            BorderLeftStyle = originalBlock.BorderLeftStyle,
            BorderRightStyle = originalBlock.BorderRightStyle,
            BorderBottomColor = originalBlock.BorderBottomColor,
            BorderLeftColor = originalBlock.BorderLeftColor,
            BorderRightColor = originalBlock.BorderRightColor,
            BorderTopLeftRadius = 0, // No top radius on split second part
            BorderTopRightRadius = 0, // No top radius on split second part
            BorderBottomLeftRadius = originalBlock.BorderBottomLeftRadius,
            BorderBottomRightRadius = originalBlock.BorderBottomRightRadius
        };

        currentY = 0; // No top padding on split part
        for (int i = splitAtLine; i < allLines.Count; i++)
        {
            var line = allLines[i];
            var newLine = new LineArea
            {
                X = line.X,
                Y = currentY,
                Width = line.Width,
                Height = line.Height
            };

            // Copy inline areas
            foreach (var inline in line.Inlines)
            {
                newLine.AddInline(new InlineArea
                {
                    X = inline.X,
                    Y = inline.Y,
                    Width = inline.Width,
                    Height = inline.Height,
                    Text = inline.Text,
                    FontFamily = inline.FontFamily,
                    FontSize = inline.FontSize,
                    FontWeight = inline.FontWeight,
                    FontStyle = inline.FontStyle,
                    Color = inline.Color,
                    TextDecoration = inline.TextDecoration,
                    BackgroundColor = inline.BackgroundColor,
                    BaselineOffset = inline.BaselineOffset,
                    Direction = inline.Direction,
                    WordSpacing = inline.WordSpacing
                });
            }

            secondBlock.AddChild(newLine);
            currentY += line.Height;
        }
        secondBlock.Height = currentY + secondBlock.PaddingBottom;

        return (firstBlock, secondBlock);
    }

    /// <summary>
    /// Gets the keep strength from a keep property value.
    /// Returns 0 for "auto", 999 for "always", or the integer value (1-999) if specified.
    /// </summary>
    private static int GetKeepStrength(string? keepValue)
    {
        if (string.IsNullOrEmpty(keepValue) || keepValue == "auto")
            return 0;

        if (keepValue == "always")
            return 999;

        // Try parsing as integer (1-999)
        if (int.TryParse(keepValue, out var strength))
        {
            // Clamp to valid range
            return Math.Max(1, Math.Min(999, strength));
        }

        return 0;
    }

    /// <summary>
    /// Lays out a block container with normal flow positioning.
    /// </summary>
    private BlockArea? LayoutNormalFlowBlockContainer(
        Dom.FoBlockContainer blockContainer,
        double x,
        double y,
        double availableWidth,
        int pageNumber = 0)
    {
        // Calculate container dimensions
        var containerWidth = blockContainer.Width == "auto"
            ? availableWidth - blockContainer.MarginLeft - blockContainer.MarginRight
            : ParseLengthOrPercentage(blockContainer.Width, availableWidth);

        // Create the block container area
        var containerArea = new BlockArea
        {
            X = x + blockContainer.MarginLeft,
            Y = y + blockContainer.MarginTop,
            Width = containerWidth,
            FontFamily = "", // Block containers don't have inherent font properties
            FontSize = 12,   // Default, children will override
            TextAlign = "start",
            MarginTop = blockContainer.MarginTop,
            MarginBottom = blockContainer.MarginBottom,
            MarginLeft = blockContainer.MarginLeft,
            MarginRight = blockContainer.MarginRight,
            PaddingTop = blockContainer.PaddingBefore,
            PaddingBottom = blockContainer.PaddingAfter,
            PaddingLeft = blockContainer.PaddingStart,
            PaddingRight = blockContainer.PaddingEnd,
            BorderTopWidth = blockContainer.BorderTopWidth,
            BorderBottomWidth = blockContainer.BorderBottomWidth,
            BorderLeftWidth = blockContainer.BorderLeftWidth,
            BorderRightWidth = blockContainer.BorderRightWidth,
            BorderTopStyle = blockContainer.BorderTopStyle,
            BorderBottomStyle = blockContainer.BorderBottomStyle,
            BorderLeftStyle = blockContainer.BorderLeftStyle,
            BorderRightStyle = blockContainer.BorderRightStyle,
            BorderTopColor = blockContainer.BorderTopColor,
            BorderBottomColor = blockContainer.BorderBottomColor,
            BorderLeftColor = blockContainer.BorderLeftColor,
            BorderRightColor = blockContainer.BorderRightColor,
            BorderTopLeftRadius = blockContainer.BorderTopLeftRadius,
            BorderTopRightRadius = blockContainer.BorderTopRightRadius,
            BorderBottomLeftRadius = blockContainer.BorderBottomLeftRadius,
            BorderBottomRightRadius = blockContainer.BorderBottomRightRadius,
            BackgroundColor = blockContainer.BackgroundColor,
            Visibility = blockContainer.Visibility
        };

        // Load background image if specified
        LoadBackgroundImage(containerArea, blockContainer.BackgroundImage);

        // Calculate content area (subtract padding and borders)
        var contentX = containerArea.X + blockContainer.PaddingStart + blockContainer.BorderLeftWidth;
        var contentY = containerArea.Y + blockContainer.PaddingBefore + blockContainer.BorderTopWidth;
        var contentWidth = containerWidth - blockContainer.PaddingStart - blockContainer.PaddingEnd -
                           blockContainer.BorderLeftWidth - blockContainer.BorderRightWidth;

        // Layout children within the container
        var currentY = contentY;
        foreach (var child in blockContainer.Children)
        {
            Area? childArea = null;

            if (child is Dom.FoBlock foBlock)
            {
                childArea = LayoutBlock(foBlock, contentX, currentY, contentWidth, pageNumber);
            }
            else if (child is Dom.FoTable foTable)
            {
                childArea = LayoutTable(foTable, contentX, currentY, contentWidth);
            }
            else if (child is Dom.FoListBlock foList)
            {
                childArea = LayoutListBlock(foList, contentX, currentY, contentWidth);
            }
            else if (child is Dom.FoBlockContainer nestedContainer)
            {
                // Recursively layout nested containers
                if (nestedContainer.AbsolutePosition == "absolute")
                {
                    // Nested absolute containers within normal flow containers are not currently supported
                    // This would require creating an AbsolutePositionedArea and converting coordinates
                    continue;
                }
                else
                {
                    childArea = LayoutNormalFlowBlockContainer(nestedContainer, contentX, currentY, contentWidth, pageNumber);
                }
            }

            if (childArea != null)
            {
                containerArea.AddChild(childArea);
                if (childArea is BlockArea blockArea)
                {
                    currentY += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom +
                               blockArea.SpaceBefore + blockArea.SpaceAfter;
                }
                else
                {
                    currentY += childArea.Height;
                }
            }
        }

        // Set final height based on content or explicit height
        var contentHeight = currentY - contentY;
        if (blockContainer.Height != "auto")
        {
            var explicitHeight = ParseLengthOrPercentage(blockContainer.Height, contentHeight);
            containerArea.Height = Math.Max(explicitHeight, contentHeight) +
                                  blockContainer.PaddingBefore + blockContainer.PaddingAfter +
                                  blockContainer.BorderTopWidth + blockContainer.BorderBottomWidth;
        }
        else
        {
            containerArea.Height = contentHeight +
                                  blockContainer.PaddingBefore + blockContainer.PaddingAfter +
                                  blockContainer.BorderTopWidth + blockContainer.BorderBottomWidth;
        }

        return containerArea;
    }

    /// <summary>
    /// Lays out a block container with absolute positioning.
    /// </summary>
    private AbsolutePositionedArea? LayoutBlockContainer(
        Dom.FoBlockContainer blockContainer,
        Dom.FoSimplePageMaster pageMaster,
        int pageNumber,
        double parentX = 0,
        double parentY = 0,
        double? parentWidth = null,
        double? parentHeight = null)
    {
        // Only handle absolute positioning
        if (blockContainer.AbsolutePosition != "absolute")
        {
            return null;
        }

        // Calculate absolute position (relative to parent if provided, otherwise relative to page)
        var (posX, posY, containerWidth, containerHeight) = CalculateAbsolutePosition(
            blockContainer, pageMaster, parentX, parentY, parentWidth, parentHeight);

        // Create the absolutely positioned area
        var absoluteArea = new AbsolutePositionedArea
        {
            X = posX,
            Y = posY,
            Width = containerWidth,
            Height = containerHeight,
            Position = blockContainer.AbsolutePosition,
            ZIndex = ParseZIndex(blockContainer.ZIndex),
            ReferenceOrientation = NormalizeRotation(blockContainer.ReferenceOrientation),
            DisplayAlign = blockContainer.DisplayAlign,
            Visibility = blockContainer.Visibility,
            Clip = blockContainer.Clip,
            Overflow = blockContainer.Overflow,
            BackgroundColor = blockContainer.BackgroundColor,
            BackgroundRepeat = blockContainer.BackgroundRepeat,
            BackgroundPositionHorizontal = blockContainer.BackgroundPositionHorizontal,
            BackgroundPositionVertical = blockContainer.BackgroundPositionVertical,
            PaddingTop = blockContainer.PaddingBefore,
            PaddingBottom = blockContainer.PaddingAfter,
            PaddingLeft = blockContainer.PaddingStart,
            PaddingRight = blockContainer.PaddingEnd,
            BorderTopWidth = blockContainer.BorderTopWidth,
            BorderBottomWidth = blockContainer.BorderBottomWidth,
            BorderLeftWidth = blockContainer.BorderLeftWidth,
            BorderRightWidth = blockContainer.BorderRightWidth,
            BorderTopStyle = blockContainer.BorderTopStyle,
            BorderBottomStyle = blockContainer.BorderBottomStyle,
            BorderLeftStyle = blockContainer.BorderLeftStyle,
            BorderRightStyle = blockContainer.BorderRightStyle,
            BorderTopColor = blockContainer.BorderTopColor,
            BorderBottomColor = blockContainer.BorderBottomColor,
            BorderLeftColor = blockContainer.BorderLeftColor,
            BorderRightColor = blockContainer.BorderRightColor,
            BorderTopLeftRadius = blockContainer.BorderTopLeftRadius,
            BorderTopRightRadius = blockContainer.BorderTopRightRadius,
            BorderBottomLeftRadius = blockContainer.BorderBottomLeftRadius,
            BorderBottomRightRadius = blockContainer.BorderBottomRightRadius
        };

        // Load background image if specified
        LoadBackgroundImage(absoluteArea, blockContainer.BackgroundImage);

        // Calculate content area (subtract padding and borders)
        var contentX = posX + blockContainer.PaddingStart + blockContainer.BorderStartWidth;
        var contentY = posY + blockContainer.PaddingBefore + blockContainer.BorderBeforeWidth;
        var contentWidth = containerWidth - blockContainer.PaddingStart - blockContainer.PaddingEnd -
                           blockContainer.BorderStartWidth - blockContainer.BorderEndWidth;

        // Layout children within the absolute container
        var currentY = contentY;
        foreach (var child in blockContainer.Children)
        {
            Area? childArea = null;

            if (child is Dom.FoBlock foBlock)
            {
                childArea = LayoutBlock(foBlock, contentX, currentY, contentWidth, pageNumber);
            }
            else if (child is Dom.FoTable foTable)
            {
                childArea = LayoutTable(foTable, contentX, currentY, contentWidth);
            }
            else if (child is Dom.FoListBlock foList)
            {
                childArea = LayoutListBlock(foList, contentX, currentY, contentWidth);
            }
            else if (child is Dom.FoBlockContainer nestedContainer)
            {
                // Nested absolute containers are positioned relative to the parent container's content area
                var contentHeight = containerHeight - blockContainer.PaddingBefore - blockContainer.PaddingAfter -
                                   blockContainer.BorderBeforeWidth - blockContainer.BorderAfterWidth;
                var nestedArea = LayoutBlockContainer(nestedContainer, pageMaster, pageNumber,
                    contentX, contentY, contentWidth, contentHeight);
                if (nestedArea != null)
                {
                    childArea = nestedArea;
                }
            }
            else if (child is Dom.FoMultiSwitch foMultiSwitch)
            {
                // Static rendering: select the first case with starting-state="show", or the first case
                var selectedCase = foMultiSwitch.MultiCases.FirstOrDefault(c => c.StartingState == "show")
                                ?? foMultiSwitch.MultiCases.FirstOrDefault();
                if (selectedCase != null)
                {
                    // Layout the blocks from the selected case
                    foreach (var caseBlock in selectedCase.Blocks)
                    {
                        var caseChildArea = LayoutBlock(caseBlock, contentX, currentY, contentWidth, pageNumber);
                        if (caseChildArea != null)
                        {
                            absoluteArea.AddChild(caseChildArea);
                            currentY += caseChildArea.Height + caseChildArea.MarginTop + caseChildArea.MarginBottom;
                        }
                    }
                    childArea = null; // Already added children
                }
            }
            else if (child is Dom.FoMultiProperties foMultiProperties)
            {
                // Static rendering: apply the first property set and render the wrapper
                if (foMultiProperties.Wrapper != null)
                {
                    // For now, just render the wrapper without applying property sets
                    // (applying property sets would require merging properties)
                    if (foMultiProperties.Wrapper is Dom.FoBlock wrapperBlock)
                    {
                        childArea = LayoutBlock(wrapperBlock, contentX, currentY, contentWidth, pageNumber);
                    }
                }
            }

            if (childArea != null)
            {
                absoluteArea.AddChild(childArea);
                currentY += childArea.Height;

                // Add margins if it's a block area
                if (childArea is BlockArea blockArea)
                {
                    currentY += blockArea.MarginTop + blockArea.MarginBottom +
                                blockArea.SpaceBefore + blockArea.SpaceAfter;
                }
            }
        }

        // Update the container height based on content if height was "auto"
        if (blockContainer.Height == "auto")
        {
            absoluteArea.Height = currentY - posY;
        }

        // Apply display-align vertical alignment
        ApplyDisplayAlign(absoluteArea, blockContainer.DisplayAlign, contentY, currentY);

        return absoluteArea;
    }

    /// <summary>
    /// Calculates the absolute position and dimensions of a block container.
    /// </summary>
    private (double x, double y, double width, double height) CalculateAbsolutePosition(
        Dom.FoBlockContainer blockContainer,
        Dom.FoSimplePageMaster pageMaster,
        double parentX = 0,
        double parentY = 0,
        double? parentWidth = null,
        double? parentHeight = null)
    {
        // Use parent dimensions if provided, otherwise use page dimensions
        var referenceWidth = parentWidth ?? pageMaster.PageWidth;
        var referenceHeight = parentHeight ?? pageMaster.PageHeight;
        var pageWidth = pageMaster.PageWidth;
        var pageHeight = pageMaster.PageHeight;

        // Calculate width first (needed for positioning when using right)
        double containerWidth;
        bool hasLeft = blockContainer.Left != "auto";
        bool hasRight = blockContainer.Right != "auto";
        bool hasWidth = blockContainer.Width != "auto";

        if (hasWidth)
        {
            containerWidth = ParseLengthOrPercentage(blockContainer.Width, referenceWidth);
        }
        else if (hasLeft && hasRight)
        {
            // If both left and right specified with auto width, calculate width from constraints
            var leftOffset = ParseLengthOrPercentage(blockContainer.Left, referenceWidth);
            var rightOffset = ParseLengthOrPercentage(blockContainer.Right, referenceWidth);
            containerWidth = referenceWidth - leftOffset - rightOffset;
        }
        else
        {
            // Default to full width (will be adjusted based on content if needed)
            containerWidth = referenceWidth;
        }

        // Calculate X position (left takes precedence over right)
        double x;
        if (hasLeft)
        {
            x = parentX + ParseLengthOrPercentage(blockContainer.Left, referenceWidth);
        }
        else if (hasRight)
        {
            var rightOffset = ParseLengthOrPercentage(blockContainer.Right, referenceWidth);
            x = parentX + referenceWidth - rightOffset - containerWidth;
        }
        else
        {
            x = parentX; // Default to left edge of parent
        }

        // Calculate height (needed for positioning when using bottom)
        double containerHeight;
        bool hasTop = blockContainer.Top != "auto";
        bool hasBottom = blockContainer.Bottom != "auto";
        bool hasHeight = blockContainer.Height != "auto";

        if (hasHeight)
        {
            containerHeight = ParseLengthOrPercentage(blockContainer.Height, referenceHeight);
        }
        else if (hasTop && hasBottom)
        {
            // If both top and bottom specified with auto height, calculate height from constraints
            var topOffset = ParseLengthOrPercentage(blockContainer.Top, referenceHeight);
            var bottomOffset = ParseLengthOrPercentage(blockContainer.Bottom, referenceHeight);
            containerHeight = referenceHeight - topOffset - bottomOffset;
        }
        else
        {
            // Default to full height (will be adjusted based on content)
            containerHeight = referenceHeight;
        }

        // Calculate Y position (top takes precedence over bottom)
        double y;
        if (hasTop)
        {
            y = parentY + ParseLengthOrPercentage(blockContainer.Top, referenceHeight);
        }
        else if (hasBottom)
        {
            var bottomOffset = ParseLengthOrPercentage(blockContainer.Bottom, referenceHeight);
            y = parentY + referenceHeight - bottomOffset - containerHeight;
        }
        else
        {
            y = parentY; // Default to top edge of parent
        }

        return (x, y, containerWidth, containerHeight);
    }

    /// <summary>
    /// Parses a length or percentage value.
    /// </summary>
    private double ParseLengthOrPercentage(string value, double referenceValue)
    {
        if (string.IsNullOrEmpty(value) || value == "auto")
            return 0;

        if (value.EndsWith("%"))
        {
            if (double.TryParse(value.TrimEnd('%'), out var percentage))
            {
                return (percentage / 100.0) * referenceValue;
            }
        }

        return Dom.LengthParser.Parse(value);
    }

    /// <summary>
    /// Parses the z-index value.
    /// </summary>
    private int ParseZIndex(string zIndex)
    {
        if (string.IsNullOrEmpty(zIndex) || zIndex == "auto")
            return 0;

        if (int.TryParse(zIndex, out var value))
            return value;

        return 0;
    }

    /// <summary>
    /// Normalizes rotation angles to the range 0, 90, 180, 270.
    /// Converts negative rotations (e.g., -90 to 270) and ensures valid values.
    /// </summary>
    private static int NormalizeRotation(int rotation)
    {
        // Normalize to 0-359 range
        rotation = rotation % 360;
        if (rotation < 0)
            rotation += 360;

        // Round to nearest 90-degree increment
        if (rotation <= 45)
            return 0;
        if (rotation <= 135)
            return 90;
        if (rotation <= 225)
            return 180;
        if (rotation <= 315)
            return 270;
        return 0;
    }

    /// <summary>
    /// Applies vertical alignment (display-align) to children of an absolutely positioned area.
    /// Adjusts child Y positions to implement center or after (bottom) alignment.
    /// </summary>
    private static void ApplyDisplayAlign(AbsolutePositionedArea area, string displayAlign, double contentY, double currentY)
    {
        // Only apply for center or after alignment
        if (displayAlign != "center" && displayAlign != "after")
            return;

        // Calculate total content height and available height
        var contentHeight = currentY - contentY;
        var availableHeight = area.Height - area.PaddingTop - area.PaddingBottom;

        // If content fits exactly or overflows, no adjustment needed
        if (contentHeight >= availableHeight)
            return;

        // Calculate vertical offset based on alignment
        double yOffset;
        if (displayAlign == "center")
        {
            yOffset = (availableHeight - contentHeight) / 2.0;
        }
        else // displayAlign == "after"
        {
            yOffset = availableHeight - contentHeight;
        }

        // Adjust Y position of all children
        foreach (var child in area.Children)
        {
            child.Y += yOffset;
        }
    }

    /// <summary>
    /// Processes element tree to track index entries (index-range-begin/end).
    /// This must be called as we traverse the FO tree during layout.
    /// </summary>
    private void TrackIndexElements(Dom.FoElement element, int pageNumber)
    {
        // Update current page number
        _currentPageNumber = pageNumber;

        // Handle index-range-begin
        if (element is Dom.FoIndexRangeBegin rangeBegin)
        {
            var id = rangeBegin.Id;
            var key = rangeBegin.IndexKey;
            var indexClass = rangeBegin.IndexClass;

            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(key))
            {
                _activeIndexRanges[id] = new IndexRangeInfo
                {
                    Key = key,
                    IndexClass = indexClass,
                    StartPage = pageNumber
                };
            }
        }

        // Handle index-range-end
        if (element is Dom.FoIndexRangeEnd rangeEnd)
        {
            var refId = rangeEnd.RefId;

            if (!string.IsNullOrEmpty(refId) && _activeIndexRanges.TryGetValue(refId, out var rangeInfo))
            {
                // Add index entry for the completed range
                if (!_indexEntries.ContainsKey(rangeInfo.Key))
                    _indexEntries[rangeInfo.Key] = new List<IndexEntry>();

                _indexEntries[rangeInfo.Key].Add(new IndexEntry
                {
                    Key = rangeInfo.Key,
                    IndexClass = rangeInfo.IndexClass,
                    PageNumber = rangeInfo.StartPage,
                    IsRange = true,
                    RangeEndPage = pageNumber
                });

                // Remove from active ranges
                _activeIndexRanges.Remove(refId);
            }
        }

        // Recursively process children
        foreach (var child in element.Children)
        {
            TrackIndexElements(child, pageNumber);
        }
    }

    /// <summary>
    /// Generates index content for an index-key-reference element.
    /// Returns formatted text with page numbers for the referenced index key.
    /// </summary>
    private string GenerateIndexContent(Dom.FoIndexKeyReference keyRef)
    {
        var refKey = keyRef.RefIndexKey;
        var indexClass = keyRef.IndexClass;

        if (string.IsNullOrEmpty(refKey))
            return "";

        // Get entries for this key
        if (!_indexEntries.TryGetValue(refKey, out var entries))
            return "";

        // Filter by index class if specified
        if (!string.IsNullOrEmpty(indexClass))
        {
            entries = entries.Where(e => e.IndexClass == indexClass).ToList();
        }

        if (entries.Count == 0)
            return "";

        // Get unique page numbers
        var pageNumbers = new SortedSet<int>();
        foreach (var entry in entries)
        {
            foreach (var page in entry.GetPageNumbers())
            {
                pageNumbers.Add(page);
            }
        }

        // Get prefix and suffix
        var prefix = keyRef.PageNumberPrefixes.FirstOrDefault()?.Text ?? "";
        var suffix = keyRef.PageNumberSuffixes.FirstOrDefault()?.Text ?? "";

        // Get separators
        var citationList = keyRef.PageCitationLists.FirstOrDefault();
        var listSeparator = citationList?.ListSeparators.FirstOrDefault()?.Text ?? ", ";
        var rangeSeparator = citationList?.RangeSeparators.FirstOrDefault()?.Text ?? "–";

        // Check if we should merge sequential pages into ranges
        var mergeSequential = citationList?.MergeSequentialPageNumbers == "merge";
        var mergeMinLength = citationList?.MergeRangesMinimumLength ?? 2;

        List<string> formattedPages = new();

        if (mergeSequential)
        {
            // Merge consecutive pages into ranges
            var sortedPages = pageNumbers.ToList();
            int i = 0;
            while (i < sortedPages.Count)
            {
                int rangeStart = sortedPages[i];
                int rangeEnd = rangeStart;

                // Find consecutive pages
                while (i + 1 < sortedPages.Count && sortedPages[i + 1] == sortedPages[i] + 1)
                {
                    rangeEnd = sortedPages[i + 1];
                    i++;
                }

                // Format as range or single page
                if (rangeEnd - rangeStart + 1 >= mergeMinLength)
                {
                    formattedPages.Add($"{rangeStart}{rangeSeparator}{rangeEnd}");
                }
                else
                {
                    // Add individual pages
                    for (int p = rangeStart; p <= rangeEnd; p++)
                    {
                        formattedPages.Add(p.ToString());
                    }
                }

                i++;
            }
        }
        else
        {
            // List all pages individually
            formattedPages.AddRange(pageNumbers.Select(p => p.ToString()));
        }

        // Combine with prefix and suffix
        var result = string.Join(listSeparator, formattedPages);
        return prefix + result + suffix;
    }

    /// <summary>
    /// Tracks which cells occupy which grid positions in a table, handling row and column spanning.
    /// </summary>
    private sealed class TableCellGrid
    {
        private readonly Dictionary<(int row, int col), CellPlacement> _occupied = new();

        /// <summary>
        /// Represents a cell placement in the grid.
        /// </summary>
        private sealed class CellPlacement
        {
            public int OriginRow { get; init; }
            public int OriginCol { get; init; }
            public int RowSpan { get; init; }
            public int ColSpan { get; init; }
            public TableCellArea? CellArea { get; init; }
        }

        /// <summary>
        /// Gets the next available column index in the specified row.
        /// </summary>
        public int GetNextAvailableColumn(int row, int startColumn = 0)
        {
            int col = startColumn;
            while (_occupied.ContainsKey((row, col)))
                col++;
            return col;
        }

        /// <summary>
        /// Reserves cells in the grid for a cell with row and column spanning.
        /// </summary>
        public void ReserveCells(int row, int col, int rowSpan, int colSpan, TableCellArea? cellArea = null)
        {
            var placement = new CellPlacement
            {
                OriginRow = row,
                OriginCol = col,
                RowSpan = rowSpan,
                ColSpan = colSpan,
                CellArea = cellArea
            };

            // Mark all grid positions occupied by this cell
            for (int r = 0; r < rowSpan; r++)
            {
                for (int c = 0; c < colSpan; c++)
                {
                    _occupied[(row + r, col + c)] = placement;
                }
            }
        }

        /// <summary>
        /// Checks if a cell position is occupied.
        /// </summary>
        public bool IsOccupied(int row, int col)
        {
            return _occupied.ContainsKey((row, col));
        }

        /// <summary>
        /// Gets the cell area that occupies a specific grid position (if from a row-spanning cell).
        /// </summary>
        public TableCellArea? GetCellAt(int row, int col)
        {
            // Only return the cell area if this is NOT the origin row (origin row renders the cell normally)
            if (_occupied.TryGetValue((row, col), out var placement) && placement.OriginRow != row)
                return placement.CellArea;

            return null;
        }

        /// <summary>
        /// Gets the row span information for a cell at a specific position.
        /// </summary>
        public (bool isSpanning, int originRow, int rowSpan)? GetSpanInfo(int row, int col)
        {
            if (_occupied.TryGetValue((row, col), out var placement))
            {
                return (placement.OriginRow != row, placement.OriginRow, placement.RowSpan);
            }
            return null;
        }
    }

    /// <summary>
    /// Represents an index entry with page number(s).
    /// </summary>
    private sealed class IndexEntry
    {
        public required string Key { get; init; }
        public required string IndexClass { get; init; }
        public required int PageNumber { get; init; }
        public bool IsRange { get; init; }
        public int? RangeEndPage { get; init; }

        /// <summary>
        /// Gets all page numbers for this entry (single page or range).
        /// </summary>
        public IEnumerable<int> GetPageNumbers()
        {
            if (IsRange && RangeEndPage.HasValue)
            {
                for (int i = PageNumber; i <= RangeEndPage.Value; i++)
                    yield return i;
            }
            else
            {
                yield return PageNumber;
            }
        }
    }

    /// <summary>
    /// Tracks an active index range (from index-range-begin to index-range-end).
    /// </summary>
    private sealed class IndexRangeInfo
    {
        public required string Key { get; init; }
        public required string IndexClass { get; init; }
        public required int StartPage { get; init; }
    }
}
