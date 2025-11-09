namespace Folly.Layout;

/// <summary>
/// Layout engine that transforms FO DOM into Area Tree.
/// </summary>
internal sealed class LayoutEngine
{
    private readonly LayoutOptions _options;

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
        // Find the page master
        var masterRef = pageSequence.MasterReference;
        var pageMaster = foRoot.LayoutMasterSet?.FindPageMaster(masterRef);

        if (pageMaster == null)
        {
            // Default page if no master found
            pageMaster = new Dom.FoSimplePageMaster
            {
                Properties = new Dom.FoProperties()
            };
            pageMaster.Properties["page-width"] = "595pt";  // A4 width
            pageMaster.Properties["page-height"] = "842pt"; // A4 height
        }

        // Get the flow
        var flow = pageSequence.Flow;
        if (flow == null)
            return;

        // For now, create a single page with all content
        // TODO: Implement proper page breaking
        var page = CreatePage(pageMaster, pageNumber: 1);

        // Layout the flow content into the page
        LayoutFlow(page, pageMaster, flow);

        areaTree.AddPage(page);
    }

    private PageViewport CreatePage(Dom.FoSimplePageMaster pageMaster, int pageNumber)
    {
        return new PageViewport
        {
            Width = pageMaster.PageWidth,
            Height = pageMaster.PageHeight,
            PageNumber = pageNumber
        };
    }

    private void LayoutFlow(PageViewport page, Dom.FoSimplePageMaster pageMaster, Dom.FoFlow flow)
    {
        // Calculate the body region dimensions
        var regionBody = pageMaster.RegionBody;
        var bodyMarginTop = regionBody?.MarginTop ?? 72;
        var bodyMarginBottom = regionBody?.MarginBottom ?? 72;
        var bodyMarginLeft = regionBody?.MarginLeft ?? 72;
        var bodyMarginRight = regionBody?.MarginRight ?? 72;

        var bodyWidth = page.Width - bodyMarginLeft - bodyMarginRight;
        var bodyHeight = page.Height - bodyMarginTop - bodyMarginBottom;

        // Track current Y position for block placement
        var currentY = bodyMarginTop;

        // Layout each block in the flow
        foreach (var foBlock in flow.Blocks)
        {
            var blockArea = LayoutBlock(foBlock, bodyMarginLeft, currentY, bodyWidth);
            if (blockArea != null)
            {
                page.AddArea(blockArea);
                currentY += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;

                // TODO: Check if we've exceeded page height and need a new page
                if (currentY > page.Height - bodyMarginBottom)
                    break; // For now, just stop adding blocks
            }
        }
    }

    private BlockArea? LayoutBlock(Dom.FoBlock foBlock, double x, double y, double availableWidth)
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
            PaddingRight = foBlock.PaddingRight
        };

        // Calculate content width (available width minus margins and padding)
        var contentWidth = availableWidth - foBlock.MarginLeft - foBlock.MarginRight - foBlock.PaddingLeft - foBlock.PaddingRight;

        // Get text content
        var text = foBlock.TextContent;
        if (string.IsNullOrWhiteSpace(text))
        {
            // Empty block - set minimal height
            blockArea.Width = contentWidth + foBlock.PaddingLeft + foBlock.PaddingRight;
            blockArea.Height = foBlock.LineHeight + foBlock.PaddingTop + foBlock.PaddingBottom;
            return blockArea;
        }

        // Create font metrics for measurement
        var fontMetrics = new Fonts.FontMetrics
        {
            FamilyName = foBlock.FontFamily,
            Size = foBlock.FontSize
        };

        // Perform line breaking and create line areas
        var lines = BreakLines(text, contentWidth, fontMetrics);

        var currentLineY = foBlock.PaddingTop;
        foreach (var lineText in lines)
        {
            var lineArea = CreateLineArea(lineText, foBlock.PaddingLeft, currentLineY, contentWidth, fontMetrics, foBlock);
            blockArea.AddChild(lineArea);
            currentLineY += foBlock.LineHeight;
        }

        blockArea.Width = contentWidth + foBlock.PaddingLeft + foBlock.PaddingRight;
        blockArea.Height = currentLineY + foBlock.PaddingBottom;

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
}
