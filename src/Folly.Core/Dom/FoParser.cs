namespace Folly.Dom;

/// <summary>
/// Parses XSL-FO XML into an FO DOM.
/// </summary>
internal static class FoParser
{
    private const string FoNamespace = "http://www.w3.org/1999/XSL/Format";

    /// <summary>
    /// Parses an XDocument into an FoRoot.
    /// </summary>
    public static FoRoot Parse(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = document.Root;
        if (root == null)
            throw new InvalidOperationException("Document has no root element");

        var localName = root.Name.LocalName;
        if (localName != "root")
            throw new InvalidOperationException($"Expected fo:root element, found {localName}");

        return ParseRoot(root);
    }

    private static FoRoot ParseRoot(XElement element)
    {
        FoLayoutMasterSet? layoutMasterSet = null;
        var pageSequences = new List<FoPageSequence>();

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "layout-master-set":
                    layoutMasterSet = ParseLayoutMasterSet(child);
                    break;
                case "page-sequence":
                    pageSequences.Add(ParsePageSequence(child));
                    break;
            }
        }

        return new FoRoot
        {
            Properties = ParseProperties(element),
            LayoutMasterSet = layoutMasterSet,
            PageSequences = pageSequences
        };
    }

    private static FoLayoutMasterSet ParseLayoutMasterSet(XElement element)
    {
        var pageMasters = new List<FoSimplePageMaster>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "simple-page-master")
                pageMasters.Add(ParseSimplePageMaster(child));
        }

        return new FoLayoutMasterSet
        {
            Properties = ParseProperties(element),
            SimplePageMasters = pageMasters
        };
    }

    private static FoSimplePageMaster ParseSimplePageMaster(XElement element)
    {
        FoRegionBody? regionBody = null;
        FoRegion? regionBefore = null;
        FoRegion? regionAfter = null;

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "region-body":
                    regionBody = new FoRegionBody { Properties = ParseProperties(child) };
                    break;
                case "region-before":
                    regionBefore = new FoRegionBefore { Properties = ParseProperties(child) };
                    break;
                case "region-after":
                    regionAfter = new FoRegionAfter { Properties = ParseProperties(child) };
                    break;
            }
        }

        return new FoSimplePageMaster
        {
            Properties = ParseProperties(element),
            RegionBody = regionBody,
            RegionBefore = regionBefore,
            RegionAfter = regionAfter
        };
    }

    private static FoPageSequence ParsePageSequence(XElement element)
    {
        FoFlow? flow = null;
        var staticContents = new List<FoStaticContent>();

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "static-content":
                    staticContents.Add(ParseStaticContent(child));
                    break;
                case "flow":
                    flow = ParseFlow(child);
                    break;
            }
        }

        return new FoPageSequence
        {
            Properties = ParseProperties(element),
            StaticContents = staticContents,
            Flow = flow
        };
    }

    private static FoStaticContent ParseStaticContent(XElement element)
    {
        var blocks = new List<FoBlock>();
        var retrieveMarkers = new List<FoRetrieveMarker>();

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "block":
                    blocks.Add(ParseBlock(child));
                    break;
                case "retrieve-marker":
                    retrieveMarkers.Add(ParseRetrieveMarker(child));
                    break;
            }
        }

        return new FoStaticContent
        {
            Properties = ParseProperties(element),
            Blocks = blocks,
            RetrieveMarkers = retrieveMarkers
        };
    }

    private static FoFlow ParseFlow(XElement element)
    {
        var blocks = new List<FoBlock>();
        var tables = new List<FoTable>();
        var lists = new List<FoListBlock>();

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "block":
                    blocks.Add(ParseBlock(child));
                    break;
                case "table":
                    tables.Add(ParseTable(child));
                    break;
                case "list-block":
                    lists.Add(ParseListBlock(child));
                    break;
            }
        }

        // Also collect direct text content
        var textContent = element.Nodes()
            .OfType<XText>()
            .Select(t => t.Value)
            .FirstOrDefault();

        return new FoFlow
        {
            Properties = ParseProperties(element),
            Blocks = blocks,
            Tables = tables,
            Lists = lists,
            TextContent = textContent
        };
    }

    private static FoBlock ParseBlock(XElement element)
    {
        var children = new List<FoElement>();

        // Collect text content
        var textContent = string.Join("", element.Nodes()
            .OfType<XText>()
            .Select(t => t.Value));

        // Parse child elements
        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "block":
                    children.Add(ParseBlock(child));
                    break;
                case "external-graphic":
                    children.Add(ParseExternalGraphic(child));
                    break;
                case "page-number":
                    children.Add(ParsePageNumber(child));
                    break;
                case "marker":
                    children.Add(ParseMarker(child));
                    break;
            }
        }

        return new FoBlock
        {
            Properties = ParseProperties(element),
            Children = children,
            TextContent = string.IsNullOrWhiteSpace(textContent) ? null : textContent
        };
    }

    private static FoExternalGraphic ParseExternalGraphic(XElement element)
    {
        return new FoExternalGraphic
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoPageNumber ParsePageNumber(XElement element)
    {
        return new FoPageNumber
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoMarker ParseMarker(XElement element)
    {
        var blocks = new List<FoBlock>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "block")
                blocks.Add(ParseBlock(child));
        }

        return new FoMarker
        {
            Properties = ParseProperties(element),
            Blocks = blocks
        };
    }

    private static FoRetrieveMarker ParseRetrieveMarker(XElement element)
    {
        return new FoRetrieveMarker
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoTable ParseTable(XElement element)
    {
        var columns = new List<FoTableColumn>();
        FoTableHeader? header = null;
        FoTableFooter? footer = null;
        FoTableBody? body = null;

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "table-column":
                    columns.Add(ParseTableColumn(child));
                    break;
                case "table-header":
                    header = ParseTableHeader(child);
                    break;
                case "table-footer":
                    footer = ParseTableFooter(child);
                    break;
                case "table-body":
                    body = ParseTableBody(child);
                    break;
            }
        }

        return new FoTable
        {
            Properties = ParseProperties(element),
            Columns = columns,
            Header = header,
            Footer = footer,
            Body = body
        };
    }

    private static FoTableColumn ParseTableColumn(XElement element)
    {
        return new FoTableColumn
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoTableHeader ParseTableHeader(XElement element)
    {
        var rows = new List<FoTableRow>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "table-row")
                rows.Add(ParseTableRow(child));
        }

        return new FoTableHeader
        {
            Properties = ParseProperties(element),
            Rows = rows
        };
    }

    private static FoTableFooter ParseTableFooter(XElement element)
    {
        var rows = new List<FoTableRow>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "table-row")
                rows.Add(ParseTableRow(child));
        }

        return new FoTableFooter
        {
            Properties = ParseProperties(element),
            Rows = rows
        };
    }

    private static FoTableBody ParseTableBody(XElement element)
    {
        var rows = new List<FoTableRow>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "table-row")
                rows.Add(ParseTableRow(child));
        }

        return new FoTableBody
        {
            Properties = ParseProperties(element),
            Rows = rows
        };
    }

    private static FoTableRow ParseTableRow(XElement element)
    {
        var cells = new List<FoTableCell>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "table-cell")
                cells.Add(ParseTableCell(child));
        }

        return new FoTableRow
        {
            Properties = ParseProperties(element),
            Cells = cells
        };
    }

    private static FoTableCell ParseTableCell(XElement element)
    {
        var blocks = new List<FoBlock>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "block")
                blocks.Add(ParseBlock(child));
        }

        return new FoTableCell
        {
            Properties = ParseProperties(element),
            Blocks = blocks
        };
    }

    private static FoListBlock ParseListBlock(XElement element)
    {
        var items = new List<FoListItem>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "list-item")
                items.Add(ParseListItem(child));
        }

        return new FoListBlock
        {
            Properties = ParseProperties(element),
            Items = items
        };
    }

    private static FoListItem ParseListItem(XElement element)
    {
        FoListItemLabel? label = null;
        FoListItemBody? body = null;

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "list-item-label":
                    label = ParseListItemLabel(child);
                    break;
                case "list-item-body":
                    body = ParseListItemBody(child);
                    break;
            }
        }

        return new FoListItem
        {
            Properties = ParseProperties(element),
            Label = label,
            Body = body
        };
    }

    private static FoListItemLabel ParseListItemLabel(XElement element)
    {
        var blocks = new List<FoBlock>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "block")
                blocks.Add(ParseBlock(child));
        }

        return new FoListItemLabel
        {
            Properties = ParseProperties(element),
            Blocks = blocks
        };
    }

    private static FoListItemBody ParseListItemBody(XElement element)
    {
        var blocks = new List<FoBlock>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "block")
                blocks.Add(ParseBlock(child));
        }

        return new FoListItemBody
        {
            Properties = ParseProperties(element),
            Blocks = blocks
        };
    }

    private static FoProperties ParseProperties(XElement element)
    {
        var props = new FoProperties();

        foreach (var attr in element.Attributes())
        {
            // Skip namespace declarations
            if (attr.IsNamespaceDeclaration)
                continue;

            var name = attr.Name.LocalName;
            props[name] = attr.Value;
        }

        return props;
    }
}
