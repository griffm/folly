using System.Text;
using System.Text.Json;

namespace Folly.Testing;

/// <summary>
/// Provides utilities for inspecting, querying, and debugging area trees.
/// This class enables precise testing of layout properties and provides
/// a tight feedback loop for development.
/// </summary>
public static class AreaTreeInspector
{
    /// <summary>
    /// Serializes an area tree to a human-readable JSON format for debugging.
    /// </summary>
    /// <param name="areaTree">The area tree to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>A JSON string representation of the area tree.</returns>
    public static string ToJson(AreaTree areaTree, AreaTreeSerializationOptions? options = null)
    {
        options ??= new AreaTreeSerializationOptions();

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = options.Indented,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        WriteAreaTree(writer, areaTree, options);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Creates a fluent query interface for the area tree.
    /// </summary>
    public static AreaTreeQuery Query(this AreaTree areaTree)
    {
        return new AreaTreeQuery(areaTree);
    }

    /// <summary>
    /// Gets a summary of the area tree for quick debugging.
    /// </summary>
    public static string GetSummary(AreaTree areaTree)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Area Tree Summary:");
        sb.AppendLine($"  Pages: {areaTree.Pages.Count}");

        for (int i = 0; i < areaTree.Pages.Count; i++)
        {
            var page = areaTree.Pages[i];
            sb.AppendLine($"  Page {i + 1}:");
            sb.AppendLine($"    Size: {page.Width:F1} x {page.Height:F1} pt");
            sb.AppendLine($"    Normal Areas: {page.Areas.Count}");
            sb.AppendLine($"    Absolute Areas: {page.AbsoluteAreas.Count}");
            sb.AppendLine($"    Links: {page.Links.Count}");

            var blocks = CountAreasByType(page.Areas);
            foreach (var kvp in blocks)
            {
                sb.AppendLine($"      {kvp.Key}: {kvp.Value}");
            }
        }

        return sb.ToString();
    }

    private static Dictionary<string, int> CountAreasByType(IReadOnlyList<Area> areas)
    {
        var counts = new Dictionary<string, int>();

        foreach (var area in areas)
        {
            var typeName = area.GetType().Name;
            if (!counts.ContainsKey(typeName))
                counts[typeName] = 0;
            counts[typeName]++;

            // Recurse into children
            if (area is BlockArea block)
            {
                foreach (var child in block.Children)
                {
                    var childType = child.GetType().Name;
                    if (!counts.ContainsKey(childType))
                        counts[childType] = 0;
                    counts[childType]++;
                }
            }
            else if (area is TableArea table)
            {
                foreach (var row in table.Rows)
                {
                    counts["TableRowArea"] = counts.GetValueOrDefault("TableRowArea") + 1;
                    counts["TableCellArea"] = counts.GetValueOrDefault("TableCellArea") + row.Cells.Count;
                }
            }
        }

        return counts;
    }

    private static void WriteAreaTree(Utf8JsonWriter writer, AreaTree areaTree, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("pageCount", areaTree.Pages.Count);

        writer.WriteStartArray("pages");
        foreach (var page in areaTree.Pages)
        {
            WritePage(writer, page, options);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private static void WritePage(Utf8JsonWriter writer, PageViewport page, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("pageNumber", page.PageNumber);
        writer.WriteNumber("width", page.Width);
        writer.WriteNumber("height", page.Height);

        writer.WriteStartArray("areas");
        foreach (var area in page.Areas)
        {
            WriteArea(writer, area, options);
        }
        writer.WriteEndArray();

        if (page.AbsoluteAreas.Count > 0)
        {
            writer.WriteStartArray("absoluteAreas");
            foreach (var area in page.AbsoluteAreas)
            {
                WriteAbsoluteArea(writer, area, options);
            }
            writer.WriteEndArray();
        }

        if (page.Links.Count > 0 && options.IncludeLinks)
        {
            writer.WriteStartArray("links");
            foreach (var link in page.Links)
            {
                WriteLinkArea(writer, link, options);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteArea(Utf8JsonWriter writer, Area area, AreaTreeSerializationOptions options)
    {
        switch (area)
        {
            case BlockArea block:
                WriteBlockArea(writer, block, options);
                break;
            case TableArea table:
                WriteTableArea(writer, table, options);
                break;
            case ImageArea image:
                WriteImageArea(writer, image, options);
                break;
            case FloatArea floatArea:
                WriteFloatArea(writer, floatArea, options);
                break;
            default:
                WriteBaseArea(writer, area, options);
                break;
        }
    }

    private static void WriteBlockArea(Utf8JsonWriter writer, BlockArea block, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "BlockArea");
        WriteGeometry(writer, block);

        if (options.IncludeTypography)
        {
            WriteNullableString(writer, "fontFamily", block.FontFamily);
            if (block.FontSize != 0)
                writer.WriteNumber("fontSize", block.FontSize);
            WriteNullableString(writer, "textAlign", block.TextAlign);
        }

        if (options.IncludeSpacing)
        {
            WriteSpacing(writer, block);
        }

        if (options.IncludeVisuals)
        {
            WriteNullableString(writer, "backgroundColor", block.BackgroundColor);
            WriteBorders(writer, block);
        }

        if (options.IncludeContent && block.Children.Count > 0)
        {
            writer.WriteStartArray("children");
            foreach (var child in block.Children)
            {
                WriteBlockChild(writer, child, options);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteBlockChild(Utf8JsonWriter writer, object child, AreaTreeSerializationOptions options)
    {
        if (child is LineArea line)
        {
            WriteLineArea(writer, line, options);
        }
        else if (child is Area area)
        {
            WriteArea(writer, area, options);
        }
    }

    private static void WriteLineArea(Utf8JsonWriter writer, LineArea line, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "LineArea");
        WriteGeometry(writer, line);

        if (options.IncludeContent)
        {
            writer.WriteStartArray("inlines");
            foreach (var inline in line.Inlines)
            {
                WriteInlineArea(writer, inline, options);
            }
            writer.WriteEndArray();

            if (options.IncludeTextContent)
            {
                var text = string.Join("", line.Inlines.Select(i => i.Text ?? ""));
                writer.WriteString("text", text);
            }
        }

        writer.WriteEndObject();
    }

    private static void WriteInlineArea(Utf8JsonWriter writer, InlineArea inline, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "InlineArea");
        WriteGeometry(writer, inline);

        if (options.IncludeTextContent)
        {
            WriteNullableString(writer, "text", inline.Text);
        }

        if (options.IncludeTypography)
        {
            WriteNullableString(writer, "fontFamily", inline.FontFamily);
            if (inline.FontSize != 0)
                writer.WriteNumber("fontSize", inline.FontSize);
            WriteNullableString(writer, "fontWeight", inline.FontWeight);
            WriteNullableString(writer, "fontStyle", inline.FontStyle);
            WriteNullableString(writer, "color", inline.Color);
            WriteNullableString(writer, "textDecoration", inline.TextDecoration);

            // IMPORTANT: Word spacing for justified text
            if (inline.WordSpacing != 0)
                writer.WriteNumber("wordSpacing", inline.WordSpacing);

            if (inline.BaselineOffset != 0)
                writer.WriteNumber("baselineOffset", inline.BaselineOffset);
        }

        if (options.IncludeVisuals)
        {
            WriteNullableString(writer, "backgroundColor", inline.BackgroundColor);
        }

        writer.WriteEndObject();
    }

    private static void WriteTableArea(Utf8JsonWriter writer, TableArea table, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "TableArea");
        WriteGeometry(writer, table);

        if (options.IncludeTableDetails)
        {
            WriteNullableString(writer, "borderCollapse", table.BorderCollapse);
            if (table.BorderSpacing != 0)
                writer.WriteNumber("borderSpacing", table.BorderSpacing);

            // IMPORTANT: Column widths for proportional column testing
            if (table.ColumnWidths.Count > 0)
            {
                writer.WriteStartArray("columnWidths");
                foreach (var width in table.ColumnWidths)
                {
                    writer.WriteNumberValue(Math.Round(width, 2));
                }
                writer.WriteEndArray();
            }
        }

        if (options.IncludeContent)
        {
            writer.WriteStartArray("rows");
            foreach (var row in table.Rows)
            {
                WriteTableRow(writer, row, options);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteTableRow(Utf8JsonWriter writer, TableRowArea row, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "TableRowArea");
        WriteGeometry(writer, row);

        if (options.IncludeContent)
        {
            writer.WriteStartArray("cells");
            foreach (var cell in row.Cells)
            {
                WriteTableCell(writer, cell, options);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteTableCell(Utf8JsonWriter writer, TableCellArea cell, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "TableCellArea");
        WriteGeometry(writer, cell);

        if (options.IncludeTableDetails)
        {
            if (cell.NumberColumnsSpanned > 1)
                writer.WriteNumber("colSpan", cell.NumberColumnsSpanned);
            if (cell.NumberRowsSpanned > 1)
                writer.WriteNumber("rowSpan", cell.NumberRowsSpanned);
            writer.WriteNumber("columnIndex", cell.ColumnIndex);
            WriteNullableString(writer, "verticalAlign", cell.VerticalAlign);
        }

        if (options.IncludeSpacing)
        {
            WriteCellPadding(writer, cell);
        }

        if (options.IncludeVisuals)
        {
            WriteNullableString(writer, "backgroundColor", cell.BackgroundColor);
            WriteCellBorder(writer, cell);
        }

        if (options.IncludeContent && cell.Children.Count > 0)
        {
            writer.WriteStartArray("children");
            foreach (var child in cell.Children)
            {
                WriteArea(writer, child, options);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteImageArea(Utf8JsonWriter writer, ImageArea image, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "ImageArea");
        WriteGeometry(writer, image);

        if (options.IncludeContent)
        {
            WriteNullableString(writer, "source", image.Source);
            WriteNullableString(writer, "format", image.Format);
            if (image.IntrinsicWidth != 0)
                writer.WriteNumber("intrinsicWidth", image.IntrinsicWidth);
            if (image.IntrinsicHeight != 0)
                writer.WriteNumber("intrinsicHeight", image.IntrinsicHeight);
        }

        writer.WriteEndObject();
    }

    private static void WriteFloatArea(Utf8JsonWriter writer, FloatArea floatArea, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "FloatArea");
        WriteGeometry(writer, floatArea);

        WriteNullableString(writer, "float", floatArea.Float);

        if (options.IncludeContent && floatArea.Blocks.Count > 0)
        {
            writer.WriteStartArray("blocks");
            foreach (var block in floatArea.Blocks)
            {
                WriteBlockArea(writer, block, options);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteAbsoluteArea(Utf8JsonWriter writer, AbsolutePositionedArea area, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "AbsolutePositionedArea");
        WriteGeometry(writer, area);

        WriteNullableString(writer, "position", area.Position);
        writer.WriteNumber("zIndex", area.ZIndex);

        if (options.IncludeContent && area.Children.Count > 0)
        {
            writer.WriteStartArray("children");
            foreach (var child in area.Children)
            {
                WriteArea(writer, child, options);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteLinkArea(Utf8JsonWriter writer, LinkArea link, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "LinkArea");
        WriteGeometry(writer, link);

        WriteNullableString(writer, "internalDestination", link.InternalDestination);
        WriteNullableString(writer, "externalDestination", link.ExternalDestination);

        if (options.IncludeTextContent)
        {
            WriteNullableString(writer, "text", link.Text);
        }

        writer.WriteEndObject();
    }

    private static void WriteBaseArea(Utf8JsonWriter writer, Area area, AreaTreeSerializationOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", area.GetType().Name);
        WriteGeometry(writer, area);
        writer.WriteEndObject();
    }

    private static void WriteGeometry(Utf8JsonWriter writer, Area area)
    {
        writer.WriteNumber("x", Math.Round(area.X, 2));
        writer.WriteNumber("y", Math.Round(area.Y, 2));
        writer.WriteNumber("width", Math.Round(area.Width, 2));
        writer.WriteNumber("height", Math.Round(area.Height, 2));
    }

    private static void WriteSpacing(Utf8JsonWriter writer, BlockArea block)
    {
        if (block.MarginTop != 0 || block.MarginBottom != 0 ||
            block.MarginLeft != 0 || block.MarginRight != 0)
        {
            writer.WriteStartObject("margin");
            if (block.MarginTop != 0)
                writer.WriteNumber("top", block.MarginTop);
            if (block.MarginBottom != 0)
                writer.WriteNumber("bottom", block.MarginBottom);
            if (block.MarginLeft != 0)
                writer.WriteNumber("left", block.MarginLeft);
            if (block.MarginRight != 0)
                writer.WriteNumber("right", block.MarginRight);
            writer.WriteEndObject();
        }

        if (block.PaddingTop != 0 || block.PaddingBottom != 0 ||
            block.PaddingLeft != 0 || block.PaddingRight != 0)
        {
            writer.WriteStartObject("padding");
            if (block.PaddingTop != 0)
                writer.WriteNumber("top", block.PaddingTop);
            if (block.PaddingBottom != 0)
                writer.WriteNumber("bottom", block.PaddingBottom);
            if (block.PaddingLeft != 0)
                writer.WriteNumber("left", block.PaddingLeft);
            if (block.PaddingRight != 0)
                writer.WriteNumber("right", block.PaddingRight);
            writer.WriteEndObject();
        }

        if (block.SpaceBefore != 0)
            writer.WriteNumber("spaceBefore", block.SpaceBefore);
        if (block.SpaceAfter != 0)
            writer.WriteNumber("spaceAfter", block.SpaceAfter);
    }

    private static void WriteBorders(Utf8JsonWriter writer, BlockArea block)
    {
        var hasBorder = block.BorderTopWidth != 0 || block.BorderBottomWidth != 0 ||
                       block.BorderLeftWidth != 0 || block.BorderRightWidth != 0;

        if (!hasBorder) return;

        writer.WriteStartObject("border");

        if (block.BorderTopWidth != 0)
        {
            writer.WriteStartObject("top");
            writer.WriteNumber("width", block.BorderTopWidth);
            WriteNullableString(writer, "style", block.BorderTopStyle);
            WriteNullableString(writer, "color", block.BorderTopColor);
            writer.WriteEndObject();
        }

        if (block.BorderBottomWidth != 0)
        {
            writer.WriteStartObject("bottom");
            writer.WriteNumber("width", block.BorderBottomWidth);
            WriteNullableString(writer, "style", block.BorderBottomStyle);
            WriteNullableString(writer, "color", block.BorderBottomColor);
            writer.WriteEndObject();
        }

        if (block.BorderLeftWidth != 0)
        {
            writer.WriteStartObject("left");
            writer.WriteNumber("width", block.BorderLeftWidth);
            WriteNullableString(writer, "style", block.BorderLeftStyle);
            WriteNullableString(writer, "color", block.BorderLeftColor);
            writer.WriteEndObject();
        }

        if (block.BorderRightWidth != 0)
        {
            writer.WriteStartObject("right");
            writer.WriteNumber("width", block.BorderRightWidth);
            WriteNullableString(writer, "style", block.BorderRightStyle);
            WriteNullableString(writer, "color", block.BorderRightColor);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteCellPadding(Utf8JsonWriter writer, TableCellArea cell)
    {
        if (cell.PaddingTop != 0 || cell.PaddingBottom != 0 ||
            cell.PaddingLeft != 0 || cell.PaddingRight != 0)
        {
            writer.WriteStartObject("padding");
            if (cell.PaddingTop != 0)
                writer.WriteNumber("top", cell.PaddingTop);
            if (cell.PaddingBottom != 0)
                writer.WriteNumber("bottom", cell.PaddingBottom);
            if (cell.PaddingLeft != 0)
                writer.WriteNumber("left", cell.PaddingLeft);
            if (cell.PaddingRight != 0)
                writer.WriteNumber("right", cell.PaddingRight);
            writer.WriteEndObject();
        }
    }

    private static void WriteCellBorder(Utf8JsonWriter writer, TableCellArea cell)
    {
        if (cell.BorderWidth != 0)
        {
            writer.WriteStartObject("border");
            writer.WriteNumber("width", cell.BorderWidth);
            WriteNullableString(writer, "style", cell.BorderStyle);
            WriteNullableString(writer, "color", cell.BorderColor);
            writer.WriteEndObject();
        }
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string propertyName, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            writer.WriteString(propertyName, value);
        }
    }
}

/// <summary>
/// Options for controlling area tree serialization.
/// </summary>
public class AreaTreeSerializationOptions
{
    /// <summary>
    /// Whether to indent the JSON output (default: true).
    /// </summary>
    public bool Indented { get; set; } = true;

    /// <summary>
    /// Whether to include typography information (fonts, sizes, etc.).
    /// </summary>
    public bool IncludeTypography { get; set; } = true;

    /// <summary>
    /// Whether to include spacing information (margins, padding, etc.).
    /// </summary>
    public bool IncludeSpacing { get; set; } = true;

    /// <summary>
    /// Whether to include visual styling (colors, borders, etc.).
    /// </summary>
    public bool IncludeVisuals { get; set; } = true;

    /// <summary>
    /// Whether to include content (children, inlines, etc.).
    /// </summary>
    public bool IncludeContent { get; set; } = true;

    /// <summary>
    /// Whether to include text content from inlines.
    /// </summary>
    public bool IncludeTextContent { get; set; } = true;

    /// <summary>
    /// Whether to include table-specific details (column widths, spanning, etc.).
    /// </summary>
    public bool IncludeTableDetails { get; set; } = true;

    /// <summary>
    /// Whether to include link areas.
    /// </summary>
    public bool IncludeLinks { get; set; } = true;

    /// <summary>
    /// Gets a minimal set of options (geometry only).
    /// </summary>
    public static AreaTreeSerializationOptions Minimal => new()
    {
        IncludeTypography = false,
        IncludeSpacing = false,
        IncludeVisuals = false,
        IncludeContent = false,
        IncludeTextContent = false,
        IncludeTableDetails = false,
        IncludeLinks = false
    };

    /// <summary>
    /// Gets options optimized for layout testing (geometry and spacing).
    /// </summary>
    public static AreaTreeSerializationOptions LayoutTesting => new()
    {
        IncludeTypography = false,
        IncludeSpacing = true,
        IncludeVisuals = false,
        IncludeContent = true,
        IncludeTextContent = false,
        IncludeTableDetails = true,
        IncludeLinks = false
    };
}
