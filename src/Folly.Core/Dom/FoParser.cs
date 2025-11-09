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

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "flow")
            {
                flow = ParseFlow(child);
                break;
            }
        }

        return new FoPageSequence
        {
            Properties = ParseProperties(element),
            Flow = flow
        };
    }

    private static FoFlow ParseFlow(XElement element)
    {
        var blocks = new List<FoBlock>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "block")
                blocks.Add(ParseBlock(child));
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

        // Parse child blocks
        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "block")
                children.Add(ParseBlock(child));
        }

        return new FoBlock
        {
            Properties = ParseProperties(element),
            Children = children,
            TextContent = string.IsNullOrWhiteSpace(textContent) ? null : textContent
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
