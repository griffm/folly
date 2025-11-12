namespace Folly.Layout;

/// <summary>
/// Layout engine that transforms FO DOM into Area Tree.
/// </summary>
internal sealed class LayoutEngine
{
    private readonly LayoutOptions _options;
    private readonly Dictionary<string, List<(int PageNumber, Dom.FoMarker Marker)>> _markers = new();
    private readonly List<Dom.FoFootnote> _currentPageFootnotes = new();
    private readonly List<Dom.FoFloat> _currentPageFloats = new();
    private readonly List<LinkArea> _currentPageLinks = new();
    private Core.Hyphenation.HyphenationEngine? _hyphenationEngine;

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

        // Layout the flow content, creating pages as needed
        // Pass foRoot to allow dynamic page master selection
        LayoutFlowWithPagination(areaTree, foRoot, pageSequence);
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
        }
    }

    private IReadOnlyList<Dom.FoBlock> RetrieveMarkerContent(Dom.FoRetrieveMarker retrieveMarker, int pageNumber)
    {
        var className = retrieveMarker.RetrieveClassName;
        if (string.IsNullOrEmpty(className) || !_markers.ContainsKey(className))
            return Array.Empty<Dom.FoBlock>();

        var markersForClass = _markers[className];
        var position = retrieveMarker.RetrievePosition;

        // Simplified implementation: support first-starting-within-page and last-ending-within-page
        Dom.FoMarker? selectedMarker = null;

        if (position == "first-starting-within-page" || position == "first-including-carryover")
        {
            // Get the first marker on this page
            selectedMarker = markersForClass
                .Where(m => m.PageNumber == pageNumber)
                .OrderBy(m => m.PageNumber)
                .FirstOrDefault().Marker;
        }
        else if (position == "last-starting-within-page" || position == "last-ending-within-page")
        {
            // Get the last marker on this page
            selectedMarker = markersForClass
                .Where(m => m.PageNumber == pageNumber)
                .OrderBy(m => m.PageNumber)
                .LastOrDefault().Marker;
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

            // Body position and dimensions must account for:
            // - Page margins (from simple-page-master)
            // - Region extents (from region-before/after)
            // - Region-body margins
            marginTop = pageMaster.MarginTop + regionBeforeExtent + regionBodyMarginTop;
            marginBottom = pageMaster.MarginBottom + regionAfterExtent + regionBodyMarginBottom;
            marginLeft = pageMaster.MarginLeft + regionBodyMarginLeft;
            marginRight = pageMaster.MarginRight + regionBodyMarginRight;

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

        // Layout each list block in the flow
        // Note: Lists span full body width, not individual columns
        foreach (var foList in flow.Lists)
        {
            // Lists break columns - start from left margin
            var listArea = LayoutListBlock(foList, bodyMarginLeft, currentY, bodyWidth);
            if (listArea == null)
                continue;

            // Check if list fits on current page
            if (currentY + listArea.Height > currentPageMaster.PageHeight - bodyMarginBottom)
            {
                // List doesn't fit - add current page and create new one
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

                // Re-position the list for the new page
                listArea = LayoutListBlock(foList, bodyMarginLeft, currentY, bodyWidth);
                if (listArea == null)
                    continue;
            }

            currentPage.AddArea(listArea);
            currentY += listArea.Height;
            currentColumn = 0;  // Reset to first column after list
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
            BorderRightColor = foBlock.BorderRightColor
        };

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
                        _markers[className] = new List<(int, Dom.FoMarker)>();

                    _markers[className].Add((pageNumber, marker));
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
        var hasBlockChildren = foBlock.Children.Any(c => c is Dom.FoBlock or Dom.FoExternalGraphic);

        // Handle block-level children (images, nested blocks)
        if (hasBlockChildren)
        {
            foreach (var child in foBlock.Children)
            {
                if (child is Dom.FoExternalGraphic graphic)
                {
                    var imageArea = LayoutImage(graphic, foBlock.PaddingLeft, currentY, contentWidth);
                    if (imageArea != null)
                    {
                        blockArea.AddChild(imageArea);
                        currentY += imageArea.Height;
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
        var blockFontFamily = Fonts.FontResolver.ResolveFont(
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
                        var inlineFontFamily = Fonts.FontResolver.ResolveFont(
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
                        var bidiFontFamily = Fonts.FontResolver.ResolveFont(
                            bidiOverride.FontFamily,
                            bidiOverride.FontWeight,
                            bidiOverride.FontStyle);

                        var bidiFontMetrics = new Fonts.FontMetrics
                        {
                            FamilyName = bidiFontFamily,
                            Size = bidiFontSize
                        };

                        // Apply text reordering for RTL direction
                        var processedText = bidiDirection == "rtl" ? ReverseText(bidiText) : bidiText;

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
        var lines = BreakLines(text, contentWidth, fontMetrics);

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

    private List<string> BreakLines(string text, double availableWidth, Fonts.FontMetrics fontMetrics)
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
                // Try hyphenation if enabled
                if (_options.EnableHyphenation && !word.Contains('-') && !word.Contains('—') && !word.Contains('–'))
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
                currentLine.Append(word);
                previousWord = word;
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

        return lines;
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

        var currentY = 0.0;

        // Layout header rows
        if (foTable.Header != null)
        {
            foreach (var foRow in foTable.Header.Rows)
            {
                var rowArea = LayoutTableRow(foRow, 0, currentY, columnWidths, foTable.BorderSpacing);
                if (rowArea != null)
                {
                    tableArea.AddRow(rowArea);
                    currentY += rowArea.Height;
                }
            }
        }

        // Layout body rows
        if (foTable.Body != null)
        {
            foreach (var foRow in foTable.Body.Rows)
            {
                var rowArea = LayoutTableRow(foRow, 0, currentY, columnWidths, foTable.BorderSpacing);
                if (rowArea != null)
                {
                    tableArea.AddRow(rowArea);
                    currentY += rowArea.Height;
                }
            }
        }

        // Layout footer rows
        if (foTable.Footer != null)
        {
            foreach (var foRow in foTable.Footer.Rows)
            {
                var rowArea = LayoutTableRow(foRow, 0, currentY, columnWidths, foTable.BorderSpacing);
                if (rowArea != null)
                {
                    tableArea.AddRow(rowArea);
                    currentY += rowArea.Height;
                }
            }
        }

        tableArea.Height = currentY;

        return tableArea;
    }

    /// <summary>
    /// Layouts a table with support for page breaking between rows.
    /// This method handles:
    /// - Row-by-row iteration with page break detection
    /// - Header repetition on new pages (controlled by table-omit-header-at-break)
    /// - Footer placement (controlled by table-omit-footer-at-break)
    /// - Keep-together constraints on rows
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
        // Calculate column widths once for the entire table
        var columnWidths = CalculateColumnWidths(foTable, tableWidth);
        var calculatedTableWidth = columnWidths.Sum() + (foTable.BorderSpacing * (columnWidths.Count + 1));

        // Render header on first page
        if (foTable.Header != null)
        {
            foreach (var foRow in foTable.Header.Rows)
            {
                var rowArea = LayoutTableRow(foRow, 0, 0, columnWidths, foTable.BorderSpacing);
                if (rowArea != null)
                {
                    rowArea.X = tableX;
                    rowArea.Y = currentY;

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
            foreach (var foRow in foTable.Body.Rows)
            {
                // Calculate row height first to check if it fits
                var rowArea = LayoutTableRow(foRow, 0, 0, columnWidths, foTable.BorderSpacing);
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
                        if (foTable.Header != null && !foTable.TableOmitHeaderAtBreak)
                        {
                            foreach (var headerRow in foTable.Header.Rows)
                            {
                                var headerRowArea = LayoutTableRow(headerRow, 0, 0, columnWidths, foTable.BorderSpacing);
                                if (headerRowArea != null)
                                {
                                    headerRowArea.X = tableX;
                                    headerRowArea.Y = currentY;

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

                        // Re-layout row for new page
                        rowArea = LayoutTableRow(foRow, 0, 0, columnWidths, foTable.BorderSpacing);
                        if (rowArea == null)
                            continue;
                    }
                    // else: row is too large for any page, render it anyway at current position
                }

                // Adjust row position to absolute coordinates
                rowArea.X = tableX;
                rowArea.Y = currentY;

                // Create a wrapper table area for just this row
                var rowTableArea = new TableArea
                {
                    X = tableX,
                    Y = currentY,
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

        // Layout footer rows (if not omitted)
        if (foTable.Footer != null && !foTable.TableOmitFooterAtBreak)
        {
            foreach (var foRow in foTable.Footer.Rows)
            {
                var rowArea = LayoutTableRow(foRow, 0, 0, columnWidths, foTable.BorderSpacing);
                if (rowArea == null)
                    continue;

                var rowHeight = rowArea.Height;
                var pageBottom = currentPageMaster.PageHeight - bodyMarginBottom;

                // Check if footer row fits on current page
                if (currentY + rowHeight > pageBottom)
                {
                    // Create new page for footer
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

                    // Re-layout footer row for new page
                    rowArea = LayoutTableRow(foRow, 0, 0, columnWidths, foTable.BorderSpacing);
                    if (rowArea == null)
                        continue;
                }

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
    }

    private List<double> CalculateColumnWidths(Dom.FoTable foTable, double availableWidth)
    {
        var columnWidths = new List<double>();

        // If table has column specifications, use them
        if (foTable.Columns.Count > 0)
        {
            foreach (var column in foTable.Columns)
            {
                var repeat = column.NumberColumnsRepeated;
                for (int i = 0; i < repeat; i++)
                {
                    // Handle column width (simplified - support pt values)
                    var width = column.ColumnWidth;
                    if (width > 0)
                    {
                        columnWidths.Add(width);
                    }
                    else
                    {
                        // Auto width - will be calculated later
                        columnWidths.Add(0);
                    }
                }
            }

            // If any columns are auto (0), distribute remaining width
            var specifiedWidth = columnWidths.Where(w => w > 0).Sum();
            var autoCount = columnWidths.Count(w => w == 0);
            if (autoCount > 0)
            {
                var remainingWidth = availableWidth - specifiedWidth - (foTable.BorderSpacing * (columnWidths.Count + 1));
                var autoWidth = Math.Max(50, remainingWidth / autoCount); // Minimum 50pt per column
                for (int i = 0; i < columnWidths.Count; i++)
                {
                    if (columnWidths[i] == 0)
                        columnWidths[i] = autoWidth;
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

    private TableRowArea? LayoutTableRow(Dom.FoTableRow foRow, double x, double y, List<double> columnWidths, double borderSpacing)
    {
        var rowArea = new TableRowArea
        {
            X = x,
            Y = y
        };

        var currentX = borderSpacing;
        var maxCellHeight = 0.0;
        int columnIndex = 0;

        foreach (var foCell in foRow.Cells)
        {
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

            var cellArea = LayoutTableCell(foCell, currentX, 0, cellWidth);
            if (cellArea != null)
            {
                cellArea.ColumnIndex = columnIndex;
                rowArea.AddCell(cellArea);
                maxCellHeight = Math.Max(maxCellHeight, cellArea.Height);
            }

            currentX += cellWidth + borderSpacing;
            columnIndex += colSpan;
        }

        rowArea.Height = maxCellHeight;
        rowArea.Width = currentX;

        return rowArea;
    }

    private TableCellArea? LayoutTableCell(Dom.FoTableCell foCell, double x, double y, double cellWidth)
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
            var blockArea = LayoutBlock(foBlock, foCell.PaddingLeft, currentY, contentWidth);
            if (blockArea != null)
            {
                cellArea.AddChild(blockArea);
                currentY += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
            }
        }

        cellArea.Height = currentY + foCell.PaddingBottom;

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

                // Detect format and dimensions
                var imageInfo = DetectImageFormat(imageData);
                format = imageInfo.Format;
                intrinsicWidth = imageInfo.Width;
                intrinsicHeight = imageInfo.Height;
            }
        }
        catch
        {
            // Image not found or couldn't be loaded
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

    private (string Format, double Width, double Height) DetectImageFormat(byte[] data)
    {
        // JPEG detection
        if (data.Length > 2 && data[0] == 0xFF && data[1] == 0xD8)
        {
            var (width, height) = GetJpegDimensions(data);
            return ("JPEG", width, height);
        }

        // PNG detection
        if (data.Length > 8 &&
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
        {
            var (width, height) = GetPngDimensions(data);
            return ("PNG", width, height);
        }

        return ("UNKNOWN", 0, 0);
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

    private BlockArea? LayoutListBlock(Dom.FoListBlock foList, double x, double y, double availableWidth)
    {
        var listArea = new BlockArea
        {
            X = x,
            Y = y + foList.SpaceBefore,
            Width = availableWidth,
            Height = 0
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

            // Layout label
            if (foItem.Label != null)
            {
                var labelY = 0.0;
                foreach (var labelBlock in foItem.Label.Blocks)
                {
                    var labelBlockArea = LayoutBlock(labelBlock, 0, labelY, labelWidth);
                    if (labelBlockArea != null)
                    {
                        // Adjust position to be relative to list item
                        labelBlockArea.X = 0;
                        labelBlockArea.Y = currentY + labelY;
                        listArea.AddChild(labelBlockArea);
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
                    var bodyBlockArea = LayoutBlock(bodyBlock, bodyStartX, bodyY, bodyWidth);
                    if (bodyBlockArea != null)
                    {
                        // Adjust position to be relative to list item
                        bodyBlockArea.X = bodyStartX;
                        bodyBlockArea.Y = currentY + bodyY;
                        listArea.AddChild(bodyBlockArea);
                        bodyY += bodyBlockArea.Height + bodyBlockArea.MarginTop + bodyBlockArea.MarginBottom;
                    }
                }
                bodyHeight = bodyY;
            }

            // List item height is the maximum of label and body heights
            var itemHeight = Math.Max(labelHeight, bodyHeight);
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

            // Calculate float width (default to 200pt, or 1/3 of body width)
            var floatWidth = Math.Min(200, bodyWidth / 3);

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
    /// Reverses text for RTL (right-to-left) rendering.
    /// This is a simplified implementation that reverses character order.
    /// For proper BiDi support, a full Unicode BiDi algorithm implementation would be needed.
    /// </summary>
    private string ReverseText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var chars = text.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
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
        catch
        {
            // If path resolution fails, reject it
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
            BorderRightColor = originalBlock.BorderRightColor
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
            BorderRightColor = originalBlock.BorderRightColor
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
}
