using System.Xml.Linq;
using Folly;
using Folly.Dom;

namespace Folly.UnitTests.Helpers;

/// <summary>
/// Helper utilities for building FO document snippets for testing.
/// </summary>
public static class FoSnippetBuilder
{
    private static readonly XNamespace FoNs = "http://www.w3.org/1999/XSL/Format";

    /// <summary>
    /// Creates a minimal FO document with a simple page master and custom flow content.
    /// </summary>
    /// <param name="flowContent">The content to place in the flow.</param>
    /// <param name="pageWidth">Page width (default: "210mm").</param>
    /// <param name="pageHeight">Page height (default: "297mm").</param>
    /// <param name="margin">Page margin (default: "1in").</param>
    /// <returns>An FO document ready for layout/rendering.</returns>
    public static FoDocument CreateSimpleDocument(
        XElement flowContent,
        string pageWidth = "210mm",
        string pageHeight = "297mm",
        string margin = "1in")
    {
        var xml = new XElement(FoNs + "root",
            new XElement(FoNs + "layout-master-set",
                new XElement(FoNs + "simple-page-master",
                    new XAttribute("master-name", "page"),
                    new XAttribute("page-width", pageWidth),
                    new XAttribute("page-height", pageHeight),
                    new XElement(FoNs + "region-body",
                        new XAttribute("margin", margin)))),
            new XElement(FoNs + "page-sequence",
                new XAttribute("master-reference", "page"),
                new XElement(FoNs + "flow",
                    new XAttribute("flow-name", "xsl-region-body"),
                    flowContent)));

        return FoDocument.Load(xml);
    }

    /// <summary>
    /// Creates a simple block with text content.
    /// </summary>
    /// <param name="content">The text content.</param>
    /// <param name="attributes">Optional attributes (e.g., font-size, color).</param>
    /// <returns>An fo:block element.</returns>
    public static XElement CreateBlock(string content, params (string name, string value)[] attributes)
    {
        var block = new XElement(FoNs + "block", content);

        foreach (var (name, value) in attributes)
        {
            block.SetAttributeValue(name, value);
        }

        return block;
    }

    /// <summary>
    /// Creates a block with visibility property.
    /// </summary>
    /// <param name="content">The text content.</param>
    /// <param name="visibility">The visibility value (visible, hidden, collapse).</param>
    /// <returns>An fo:block element with visibility set.</returns>
    public static XElement CreateBlockWithVisibility(string content, string visibility)
    {
        return CreateBlock(content, ("visibility", visibility));
    }

    /// <summary>
    /// Creates a block with clip property.
    /// </summary>
    /// <param name="content">The text content.</param>
    /// <param name="clip">The clip value (e.g., "rect(10pt, 100pt, 50pt, 10pt)").</param>
    /// <returns>An fo:block element with clip set.</returns>
    public static XElement CreateBlockWithClip(string content, string clip)
    {
        return CreateBlock(content, ("clip", clip));
    }

    /// <summary>
    /// Creates a block-container with overflow property.
    /// </summary>
    /// <param name="width">Container width.</param>
    /// <param name="height">Container height.</param>
    /// <param name="overflow">Overflow value (visible, hidden, scroll, auto).</param>
    /// <param name="content">Child content elements.</param>
    /// <returns>An fo:block-container element.</returns>
    public static XElement CreateBlockContainer(
        string width,
        string height,
        string overflow,
        params XElement[] content)
    {
        return new XElement(FoNs + "block-container",
            new XAttribute("width", width),
            new XAttribute("height", height),
            new XAttribute("overflow", overflow),
            content);
    }

    /// <summary>
    /// Creates a table with specified column widths.
    /// </summary>
    /// <param name="columnWidths">Column width specifications (e.g., "100pt", "25%", "2*", "auto").</param>
    /// <param name="rows">Optional row content.</param>
    /// <returns>An fo:table element.</returns>
    public static XElement CreateTable(string[] columnWidths, params XElement[] rows)
    {
        var table = new XElement(FoNs + "table");

        // Add columns
        foreach (var width in columnWidths)
        {
            table.Add(new XElement(FoNs + "table-column",
                new XAttribute("column-width", width)));
        }

        // Add table body with rows
        if (rows.Length > 0)
        {
            var body = new XElement(FoNs + "table-body", rows);
            table.Add(body);
        }

        return table;
    }

    /// <summary>
    /// Creates a table row with cells.
    /// </summary>
    /// <param name="cellContents">Content for each cell.</param>
    /// <returns>An fo:table-row element.</returns>
    public static XElement CreateTableRow(params string[] cellContents)
    {
        var row = new XElement(FoNs + "table-row");

        foreach (var content in cellContents)
        {
            row.Add(new XElement(FoNs + "table-cell",
                new XElement(FoNs + "block", content)));
        }

        return row;
    }

    /// <summary>
    /// Creates a table with caption.
    /// </summary>
    /// <param name="captionText">Caption text.</param>
    /// <param name="captionSide">Caption side (before, after, start, end, top, bottom, left, right).</param>
    /// <param name="columnWidths">Column widths for the table.</param>
    /// <param name="rows">Table rows.</param>
    /// <returns>An fo:table-and-caption element.</returns>
    public static XElement CreateTableWithCaption(
        string captionText,
        string captionSide,
        string[] columnWidths,
        params XElement[] rows)
    {
        return new XElement(FoNs + "table-and-caption",
            new XElement(FoNs + "table-caption",
                new XAttribute("caption-side", captionSide),
                new XElement(FoNs + "block", captionText)),
            CreateTable(columnWidths, rows));
    }

    /// <summary>
    /// Creates a float element with specified width.
    /// </summary>
    /// <param name="width">Float width (e.g., "100pt", "25%", "auto").</param>
    /// <param name="floatSide">Float side (start, end, left, right).</param>
    /// <param name="content">Float content.</param>
    /// <returns>An fo:float element.</returns>
    public static XElement CreateFloat(string width, string floatSide, params XElement[] content)
    {
        return new XElement(FoNs + "float",
            new XAttribute("float", floatSide),
            new XElement(FoNs + "block",
                new XAttribute("width", width),
                content));
    }

    /// <summary>
    /// Creates a marker element.
    /// </summary>
    /// <param name="markerClassName">Marker class name.</param>
    /// <param name="content">Marker content.</param>
    /// <returns>An fo:marker element.</returns>
    public static XElement CreateMarker(string markerClassName, string content)
    {
        return new XElement(FoNs + "marker",
            new XAttribute("marker-class-name", markerClassName),
            content);
    }

    /// <summary>
    /// Creates a retrieve-marker element.
    /// </summary>
    /// <param name="markerClassName">Marker class name to retrieve.</param>
    /// <param name="retrievePosition">Retrieve position (first-starting-within-page, etc.).</param>
    /// <returns>An fo:retrieve-marker element.</returns>
    public static XElement CreateRetrieveMarker(string markerClassName, string retrievePosition)
    {
        return new XElement(FoNs + "retrieve-marker",
            new XAttribute("retrieve-class-name", markerClassName),
            new XAttribute("retrieve-position", retrievePosition));
    }

    /// <summary>
    /// Creates an external-graphic element (image).
    /// </summary>
    /// <param name="src">Image source path.</param>
    /// <param name="width">Optional width.</param>
    /// <param name="height">Optional height.</param>
    /// <returns>An fo:external-graphic element.</returns>
    public static XElement CreateImage(string src, string? width = null, string? height = null)
    {
        var image = new XElement(FoNs + "external-graphic",
            new XAttribute("src", src));

        if (width != null)
            image.SetAttributeValue("width", width);

        if (height != null)
            image.SetAttributeValue("height", height);

        return image;
    }

    /// <summary>
    /// Creates a page sequence with force-page-count.
    /// </summary>
    /// <param name="forcePageCount">Force page count value (auto, even, odd, end-on-even, end-on-odd).</param>
    /// <param name="content">Page sequence content.</param>
    /// <returns>An fo:page-sequence element.</returns>
    public static XElement CreatePageSequenceWithForcePageCount(
        string forcePageCount,
        params XElement[] content)
    {
        return new XElement(FoNs + "page-sequence",
            new XAttribute("master-reference", "page"),
            new XAttribute("force-page-count", forcePageCount),
            new XElement(FoNs + "flow",
                new XAttribute("flow-name", "xsl-region-body"),
                content));
    }
}
