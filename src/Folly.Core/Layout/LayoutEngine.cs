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

        // Create the Knuth-Plass line breaker
        var lineBreaker = new KnuthPlassLineBreaker(fontMetrics, availableWidth, tolerance: 1.0);

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

                        // Re-layout row for new page (grid state is maintained across pages)
                        rowArea = LayoutTableRowWithSpanning(foRow, 0, 0, columnWidths, foTable.BorderSpacing, bodyGrid, bodyRowIdx, bodyRowHeights);
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
            var expandedColumns = new List<(string widthSpec, double fixedWidth, double proportionalValue)>();

            foreach (var column in foTable.Columns)
            {
                var repeat = column.NumberColumnsRepeated;
                var widthSpec = column.ColumnWidthString;

                for (int i = 0; i < repeat; i++)
                {
                    double fixedWidth = 0;
                    double proportionalValue = 0;

                    if (widthSpec.StartsWith("proportional-column-width("))
                    {
                        // Parse proportional-column-width(N)
                        proportionalValue = ParseProportionalColumnWidth(widthSpec);
                    }
                    else if (widthSpec != "auto")
                    {
                        // Fixed width
                        fixedWidth = column.ColumnWidth;
                    }
                    // else: auto width (both values remain 0)

                    expandedColumns.Add((widthSpec, fixedWidth, proportionalValue));
                }
            }

            // Calculate widths
            var totalFixedWidth = expandedColumns.Sum(c => c.fixedWidth);
            var totalProportional = expandedColumns.Sum(c => c.proportionalValue);
            var autoCount = expandedColumns.Count(c => c.fixedWidth == 0 && c.proportionalValue == 0);

            var remainingWidth = availableWidth - totalFixedWidth - (foTable.BorderSpacing * (expandedColumns.Count + 1));

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
                    var (widthSpec, fixedWidth, proportionalValue) = expandedColumns[i];
                    if (fixedWidth == 0 && proportionalValue == 0) // auto column
                    {
                        var contentWidth = contentWidths[i];
                        autoColumnContentWidths.Add(contentWidth);
                        totalAutoContentWidth += contentWidth;
                    }
                }
            }

            // Distribute remaining width
            int autoColumnIndex = 0;
            foreach (var (widthSpec, fixedWidth, proportionalValue) in expandedColumns)
            {
                if (fixedWidth > 0)
                {
                    // Fixed width column
                    columnWidths.Add(fixedWidth);
                }
                else if (proportionalValue > 0)
                {
                    // Proportional column - get its share of the remaining width
                    var proportionalWidth = totalProportional > 0
                        ? remainingWidth * (proportionalValue / totalProportional)
                        : 0;
                    columnWidths.Add(Math.Max(50, proportionalWidth)); // Minimum 50pt
                }
                else
                {
                    // Auto width column - distribute based on content width ratios
                    double autoWidth;
                    if (totalAutoContentWidth > 0)
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

                    columnWidths.Add(Math.Max(50, autoWidth)); // Minimum 50pt
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
            Size = foBlock.FontSize
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
                        Size = inline.FontSize ?? foBlock.FontSize
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
                // Add spacing between spanned rows
                if (rowSpan > 1)
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
            var blockArea = LayoutBlock(foBlock, foCell.PaddingLeft, currentY, contentWidth);
            if (blockArea != null)
            {
                cellArea.AddChild(blockArea);
                currentY += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
            }
        }

        // Use specified height for row-spanning cells, otherwise use content height
        if (specifiedCellHeight > 0)
        {
            cellArea.Height = Math.Max(specifiedCellHeight, currentY + foCell.PaddingBottom);
        }
        else
        {
            cellArea.Height = currentY + foCell.PaddingBottom;
        }

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
            if (_occupied.TryGetValue((row, col), out var placement))
            {
                // Only return the cell area if this is NOT the origin row
                // (origin row renders the cell normally)
                if (placement.OriginRow != row)
                    return placement.CellArea;
            }
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
}
