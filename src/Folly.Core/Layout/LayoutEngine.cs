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

        // Layout the flow content, creating pages as needed
        LayoutFlowWithPagination(areaTree, pageMaster, flow);
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

    private void LayoutFlowWithPagination(AreaTree areaTree, Dom.FoSimplePageMaster pageMaster, Dom.FoFlow flow)
    {
        // Calculate the body region dimensions
        var regionBody = pageMaster.RegionBody;
        var bodyMarginTop = regionBody?.MarginTop ?? 72;
        var bodyMarginBottom = regionBody?.MarginBottom ?? 72;
        var bodyMarginLeft = regionBody?.MarginLeft ?? 72;
        var bodyMarginRight = regionBody?.MarginRight ?? 72;

        var bodyWidth = pageMaster.PageWidth - bodyMarginLeft - bodyMarginRight;
        var bodyHeight = pageMaster.PageHeight - bodyMarginTop - bodyMarginBottom;

        // Create first page
        var currentPage = CreatePage(pageMaster, pageNumber: 1);
        var currentY = bodyMarginTop;
        var pageNumber = 1;

        // Layout each block in the flow
        foreach (var foBlock in flow.Blocks)
        {
            var blockArea = LayoutBlock(foBlock, bodyMarginLeft, currentY, bodyWidth);
            if (blockArea == null)
                continue;

            var blockTotalHeight = blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;

            // Check if block fits on current page
            if (currentY + blockTotalHeight > pageMaster.PageHeight - bodyMarginBottom)
            {
                // Block doesn't fit - add current page and create new one
                areaTree.AddPage(currentPage);
                pageNumber++;
                currentPage = CreatePage(pageMaster, pageNumber);
                currentY = bodyMarginTop;

                // Re-position the block for the new page
                blockArea = LayoutBlock(foBlock, bodyMarginLeft, currentY, bodyWidth);
                if (blockArea == null)
                    continue;
            }

            currentPage.AddArea(blockArea);
            currentY += blockArea.Height + blockArea.MarginTop + blockArea.MarginBottom;
        }

        // Layout each table in the flow
        foreach (var foTable in flow.Tables)
        {
            var tableArea = LayoutTable(foTable, bodyMarginLeft, currentY, bodyWidth);
            if (tableArea == null)
                continue;

            // Check if table fits on current page
            if (currentY + tableArea.Height > pageMaster.PageHeight - bodyMarginBottom)
            {
                // Table doesn't fit - add current page and create new one
                areaTree.AddPage(currentPage);
                pageNumber++;
                currentPage = CreatePage(pageMaster, pageNumber);
                currentY = bodyMarginTop;

                // Re-position the table for the new page
                tableArea.X = bodyMarginLeft;
                tableArea.Y = currentY;
            }

            currentPage.AddArea(tableArea);
            currentY += tableArea.Height;
        }

        // Add the last page
        areaTree.AddPage(currentPage);
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
            PaddingRight = foBlock.PaddingRight,
            BackgroundColor = foBlock.BackgroundColor,
            BorderWidth = foBlock.BorderWidth,
            BorderColor = foBlock.BorderColor,
            BorderStyle = foBlock.BorderStyle
        };

        // Calculate content width (available width minus margins and padding)
        var contentWidth = availableWidth - foBlock.MarginLeft - foBlock.MarginRight - foBlock.PaddingLeft - foBlock.PaddingRight;

        var currentY = foBlock.PaddingTop;

        // Check for child elements (images, nested blocks)
        if (foBlock.Children.Count > 0)
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
                    var nestedArea = LayoutBlock(nestedBlock, foBlock.PaddingLeft, currentY, contentWidth);
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
}
