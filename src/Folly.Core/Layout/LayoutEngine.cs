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
                var y = 0.0;
                var x = pageMaster.RegionBefore.MarginLeft;
                var width = pageMaster.PageWidth - pageMaster.RegionBefore.MarginLeft - pageMaster.RegionBefore.MarginRight;

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
                var y = pageMaster.PageHeight - extent;
                var x = pageMaster.RegionAfter.MarginLeft;
                var width = pageMaster.PageWidth - pageMaster.RegionAfter.MarginLeft - pageMaster.RegionAfter.MarginRight;

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

        // Get first page master to determine body dimensions
        // Note: For simplicity, we assume body dimensions are consistent across all page masters
        var firstPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber: 1, totalPages: 999);

        var regionBody = firstPageMaster.RegionBody;
        var bodyMarginTop = regionBody?.MarginTop ?? 72;
        var bodyMarginBottom = regionBody?.MarginBottom ?? 72;
        var bodyMarginLeft = regionBody?.MarginLeft ?? 72;
        var bodyMarginRight = regionBody?.MarginRight ?? 72;

        var bodyWidth = firstPageMaster.PageWidth - bodyMarginLeft - bodyMarginRight;
        var bodyHeight = firstPageMaster.PageHeight - bodyMarginTop - bodyMarginBottom;

        // Multi-column support
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
                    RenderFootnotes(currentPage, currentPageMaster);
                    AddLinksToPage(currentPage);
                    areaTree.AddPage(currentPage);
                    pageNumber++;
                    currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
                    currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                    currentY = bodyMarginTop;
                    currentColumn = 0;
                }
            }

            // Calculate X position based on current column
            var columnX = bodyMarginLeft + currentColumn * (columnWidth + columnGap);

            var blockArea = LayoutBlock(foBlock, columnX, currentY, columnWidth);
            if (blockArea == null)
                continue;

            var blockTotalHeight = blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;

            // Handle keep-together constraint
            var mustKeepTogether = foBlock.KeepTogether == "always";
            var blockFitsInColumn = currentY + blockTotalHeight <= currentPageMaster.PageHeight - bodyMarginBottom;

            // If block doesn't fit in current column/page
            if (!blockFitsInColumn)
            {
                // Only add the block to overflow page if we're NOT at the top AND NOT keep-together
                // (if we're at the top, the block is too large for any page, so we must render it anyway)
                if (currentY > bodyMarginTop || mustKeepTogether)
                {
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
                        RenderFootnotes(currentPage, currentPageMaster);
                        AddLinksToPage(currentPage);
                        areaTree.AddPage(currentPage);
                        pageNumber++;
                        currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
                        currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                        currentY = bodyMarginTop;
                        currentColumn = 0;
                        columnX = bodyMarginLeft;
                    }

                    // Re-layout the block for the new column/page
                    blockArea = LayoutBlock(foBlock, columnX, currentY, columnWidth);
                    if (blockArea == null)
                        continue;

                    blockTotalHeight = blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
                }
                // else: block is too large for any page, render it anyway at top of current page
            }

            currentPage.AddArea(blockArea);
            currentY += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;

            // Handle break-after constraint
            if (foBlock.BreakAfter == "always" || foBlock.BreakAfter == "page")
            {
                // Force page break after this block
                RenderFloats(currentPage, currentPageMaster, bodyMarginTop);
                RenderFootnotes(currentPage, currentPageMaster);
                AddLinksToPage(currentPage);
                areaTree.AddPage(currentPage);
                pageNumber++;
                currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
                currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                currentY = bodyMarginTop;
                currentColumn = 0;
            }
        }

        // Layout each table in the flow
        // Note: Tables span full body width, not individual columns
        foreach (var foTable in flow.Tables)
        {
            // Tables break columns - start from left margin
            var tableArea = LayoutTable(foTable, bodyMarginLeft, currentY, bodyWidth);
            if (tableArea == null)
                continue;

            // Check if table fits on current page
            if (currentY + tableArea.Height > currentPageMaster.PageHeight - bodyMarginBottom)
            {
                // Table doesn't fit - add current page and create new one
                RenderFloats(currentPage, currentPageMaster, bodyMarginTop);
                RenderFootnotes(currentPage, currentPageMaster);
                AddLinksToPage(currentPage);
                areaTree.AddPage(currentPage);
                pageNumber++;
                currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
                currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
                currentY = bodyMarginTop;

                // Re-position the table for the new page
                tableArea.X = bodyMarginLeft;
                tableArea.Y = currentY;
            }

            currentPage.AddArea(tableArea);
            currentY += tableArea.Height;
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
                RenderFootnotes(currentPage, currentPageMaster);
                AddLinksToPage(currentPage);
                areaTree.AddPage(currentPage);
                pageNumber++;
                currentPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber, totalPages: 999);
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
        RenderFootnotes(currentPage, currentPageMaster);
        AddLinksToPage(currentPage);
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
            PaddingTop = foBlock.PaddingTop,
            PaddingBottom = foBlock.PaddingBottom,
            PaddingLeft = foBlock.PaddingLeft,
            PaddingRight = foBlock.PaddingRight,
            BackgroundColor = foBlock.BackgroundColor,
            BorderWidth = foBlock.BorderWidth,
            BorderColor = foBlock.BorderColor,
            BorderStyle = foBlock.BorderStyle
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
        var fontMetrics = new Fonts.FontMetrics
        {
            FamilyName = foBlock.FontFamily,
            Size = foBlock.FontSize
        };

        // Handle inline elements with formatting
        if (hasInline)
        {
            var lineArea = new LineArea
            {
                X = foBlock.PaddingLeft,
                Y = currentY,
                Width = contentWidth,
                Height = foBlock.LineHeight
            };

            double currentX = 0;

            // Process mixed content (text nodes + inline elements)
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
                        var inlineFontFamily = inline.FontFamily;
                        var inlineFontSize = inline.FontSize ?? 12;
                        var inlineFontWeight = inline.FontWeight;
                        var inlineFontStyle = inline.FontStyle;

                        // Apply font-weight by using bold variant
                        if (!string.IsNullOrEmpty(inlineFontWeight) && (inlineFontWeight == "bold" || int.TryParse(inlineFontWeight, out var weight) && weight >= 700))
                        {
                            inlineFontFamily = inlineFontFamily switch
                            {
                                "Helvetica" => "Helvetica-Bold",
                                "Times" or "Times-Roman" => "Times-Bold",
                                "Courier" => "Courier-Bold",
                                _ => inlineFontFamily + "-Bold"
                            };
                        }

                        // Apply font-style by using italic/oblique variant
                        if (!string.IsNullOrEmpty(inlineFontStyle) && (inlineFontStyle == "italic" || inlineFontStyle == "oblique"))
                        {
                            if (inlineFontFamily.EndsWith("-Bold"))
                            {
                                inlineFontFamily = inlineFontFamily.Replace("-Bold", "-BoldOblique");
                            }
                            else
                            {
                                inlineFontFamily = inlineFontFamily switch
                                {
                                    "Helvetica" => "Helvetica-Oblique",
                                    "Times" or "Times-Roman" => "Times-Italic",
                                    "Courier" => "Courier-Oblique",
                                    _ => inlineFontFamily + "-Oblique"
                                };
                            }
                        }

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
                            FontWeight = inlineFontWeight,
                            FontStyle = inlineFontStyle,
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
                            FontFamily = basicLink.Color == "blue" ? foBlock.FontFamily : foBlock.FontFamily, // Use link color if specified
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

        foreach (var lineText in lines)
        {
            var lineArea = CreateLineArea(lineText, foBlock.PaddingLeft, currentY, contentWidth, fontMetrics, foBlock);
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
        var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            lines.Add("");
            return lines;
        }

        var currentLine = new StringBuilder();
        var currentWidth = 0.0;

        foreach (var word in words)
        {
            var wordWidth = fontMetrics.MeasureWidth(word);
            var spaceWidth = fontMetrics.MeasureWidth(" ");

            // Check if adding this word would exceed available width
            var widthWithWord = currentWidth + (currentLine.Length > 0 ? spaceWidth : 0) + wordWidth;

            if (widthWithWord > availableWidth && currentLine.Length > 0)
            {
                // Start a new line
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                currentLine.Append(word);
                currentWidth = wordWidth;
            }
            else
            {
                // Add word to current line
                if (currentLine.Length > 0)
                {
                    currentLine.Append(' ');
                    currentWidth += spaceWidth;
                }
                currentLine.Append(word);
                currentWidth += wordWidth;
            }
        }

        // Add the last line
        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return lines;
    }

    private LineArea CreateLineArea(string text, double x, double y, double availableWidth, Fonts.FontMetrics fontMetrics, Dom.FoBlock foBlock)
    {
        var lineArea = new LineArea
        {
            X = x,
            Y = y,
            Width = availableWidth,
            Height = foBlock.LineHeight
        };

        // Measure the actual text width
        var textWidth = fontMetrics.MeasureWidth(text);

        // Calculate X offset based on alignment
        var textX = foBlock.TextAlign.ToLowerInvariant() switch
        {
            "center" => (availableWidth - textWidth) / 2,
            "end" or "right" => availableWidth - textWidth,
            _ => 0 // start/left
        };

        // Create inline area for the text
        var inlineArea = new InlineArea
        {
            X = textX,
            Y = 0, // Relative to line
            Width = textWidth,
            Height = foBlock.FontSize,
            Text = text,
            FontFamily = foBlock.FontFamily,
            FontSize = foBlock.FontSize,
            BaselineOffset = fontMetrics.GetAscent()
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

        // Load image data
        byte[]? imageData = null;
        double intrinsicWidth = 0;
        double intrinsicHeight = 0;
        string format = "";

        try
        {
            if (File.Exists(imagePath))
            {
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

    private void RenderFootnotes(PageViewport page, Dom.FoSimplePageMaster pageMaster)
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

        // Draw separator line
        // TODO: Add line drawing to BlockArea or create a LineArea

        // Render each footnote body
        var currentY = footnoteY;
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
}
