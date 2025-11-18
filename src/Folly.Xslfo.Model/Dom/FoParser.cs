namespace Folly.Xslfo;

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

        var foRoot = ParseRoot(root);

        // Establish parent-child relationships for property inheritance
        EstablishParentRelationships(foRoot);

        return foRoot;
    }

    /// <summary>
    /// Recursively sets parent references for all elements in the tree.
    /// This enables property inheritance to work correctly.
    /// </summary>
    private static void EstablishParentRelationships(FoElement element, FoElement? parent = null)
    {
        element.Parent = parent;

        // Process standard children
        foreach (var child in element.Children)
        {
            EstablishParentRelationships(child, element);
        }

        // Process type-specific children
        switch (element)
        {
            case FoRoot root:
                if (root.LayoutMasterSet != null)
                    EstablishParentRelationships(root.LayoutMasterSet, root);
                if (root.Declarations != null)
                    EstablishParentRelationships(root.Declarations, root);
                if (root.BookmarkTree != null)
                    EstablishParentRelationships(root.BookmarkTree, root);
                foreach (var seq in root.PageSequences)
                    EstablishParentRelationships(seq, root);
                break;

            case FoLayoutMasterSet masterSet:
                foreach (var master in masterSet.SimplePageMasters)
                    EstablishParentRelationships(master, masterSet);
                foreach (var seqMaster in masterSet.PageSequenceMasters)
                    EstablishParentRelationships(seqMaster, masterSet);
                break;

            case FoSimplePageMaster pageMaster:
                if (pageMaster.RegionBody != null)
                    EstablishParentRelationships(pageMaster.RegionBody, pageMaster);
                if (pageMaster.RegionBefore != null)
                    EstablishParentRelationships(pageMaster.RegionBefore, pageMaster);
                if (pageMaster.RegionAfter != null)
                    EstablishParentRelationships(pageMaster.RegionAfter, pageMaster);
                if (pageMaster.RegionStart != null)
                    EstablishParentRelationships(pageMaster.RegionStart, pageMaster);
                if (pageMaster.RegionEnd != null)
                    EstablishParentRelationships(pageMaster.RegionEnd, pageMaster);
                break;

            case FoPageSequenceMaster seqMaster:
                if (seqMaster.SinglePageMasterReference != null)
                    EstablishParentRelationships(seqMaster.SinglePageMasterReference, seqMaster);
                if (seqMaster.RepeatablePageMasterAlternatives != null)
                    EstablishParentRelationships(seqMaster.RepeatablePageMasterAlternatives, seqMaster);
                break;

            case FoRepeatablePageMasterAlternatives alternatives:
                foreach (var condRef in alternatives.ConditionalPageMasterReferences)
                    EstablishParentRelationships(condRef, alternatives);
                break;

            case FoPageSequence pageSeq:
                foreach (var staticContent in pageSeq.StaticContents)
                    EstablishParentRelationships(staticContent, pageSeq);
                if (pageSeq.Flow != null)
                    EstablishParentRelationships(pageSeq.Flow, pageSeq);
                break;

            case FoStaticContent staticContent:
                foreach (var block in staticContent.Blocks)
                    EstablishParentRelationships(block, staticContent);
                foreach (var marker in staticContent.RetrieveMarkers)
                    EstablishParentRelationships(marker, staticContent);
                break;

            case FoFlow flow:
                foreach (var block in flow.Blocks)
                    EstablishParentRelationships(block, flow);
                foreach (var table in flow.Tables)
                    EstablishParentRelationships(table, flow);
                foreach (var tableAndCaption in flow.TableAndCaptions)
                    EstablishParentRelationships(tableAndCaption, flow);
                foreach (var list in flow.Lists)
                    EstablishParentRelationships(list, flow);
                break;

            case FoBlock block:
                foreach (var footnote in block.Footnotes)
                    EstablishParentRelationships(footnote, block);
                foreach (var floatElem in block.Floats)
                    EstablishParentRelationships(floatElem, block);
                break;

            case FoMarker marker:
                foreach (var block in marker.Blocks)
                    EstablishParentRelationships(block, marker);
                break;

            case FoFootnote footnote:
                if (footnote.FootnoteBody != null)
                    EstablishParentRelationships(footnote.FootnoteBody, footnote);
                break;

            case FoFootnoteBody footnoteBody:
                foreach (var block in footnoteBody.Blocks)
                    EstablishParentRelationships(block, footnoteBody);
                break;

            case FoFloat floatElem:
                foreach (var block in floatElem.Blocks)
                    EstablishParentRelationships(block, floatElem);
                break;

            case FoTable table:
                foreach (var col in table.Columns)
                    EstablishParentRelationships(col, table);
                if (table.Header != null)
                    EstablishParentRelationships(table.Header, table);
                if (table.Footer != null)
                    EstablishParentRelationships(table.Footer, table);
                if (table.Body != null)
                    EstablishParentRelationships(table.Body, table);
                break;

            case FoTableAndCaption tableAndCaption:
                if (tableAndCaption.Caption != null)
                    EstablishParentRelationships(tableAndCaption.Caption, tableAndCaption);
                if (tableAndCaption.Table != null)
                    EstablishParentRelationships(tableAndCaption.Table, tableAndCaption);
                break;

            case FoTableCaption caption:
                foreach (var block in caption.Blocks)
                    EstablishParentRelationships(block, caption);
                break;

            case FoTableHeader header:
                foreach (var row in header.Rows)
                    EstablishParentRelationships(row, header);
                break;

            case FoTableFooter footer:
                foreach (var row in footer.Rows)
                    EstablishParentRelationships(row, footer);
                break;

            case FoTableBody body:
                foreach (var row in body.Rows)
                    EstablishParentRelationships(row, body);
                break;

            case FoTableRow row:
                foreach (var cell in row.Cells)
                    EstablishParentRelationships(cell, row);
                break;

            case FoTableCell cell:
                foreach (var block in cell.Blocks)
                    EstablishParentRelationships(block, cell);
                break;

            case FoListBlock listBlock:
                foreach (var item in listBlock.Items)
                    EstablishParentRelationships(item, listBlock);
                break;

            case FoListItem listItem:
                if (listItem.Label != null)
                    EstablishParentRelationships(listItem.Label, listItem);
                if (listItem.Body != null)
                    EstablishParentRelationships(listItem.Body, listItem);
                break;

            case FoListItemLabel label:
                foreach (var block in label.Blocks)
                    EstablishParentRelationships(block, label);
                break;

            case FoListItemBody body:
                foreach (var block in body.Blocks)
                    EstablishParentRelationships(block, body);
                break;

            case FoDeclarations declarations:
                if (declarations.Info != null)
                    EstablishParentRelationships(declarations.Info, declarations);
                break;

            case FoBookmarkTree bookmarkTree:
                foreach (var bookmark in bookmarkTree.Bookmarks)
                    EstablishParentRelationships(bookmark, bookmarkTree);
                break;

            case FoBookmark bookmark:
                foreach (var child in bookmark.Children)
                    EstablishParentRelationships(child, bookmark);
                break;

            case FoWrapper wrapper:
                foreach (var child in wrapper.Children)
                    EstablishParentRelationships(child, wrapper);
                break;

            case FoBidiOverride bidiOverride:
                foreach (var child in bidiOverride.Children)
                    EstablishParentRelationships(child, bidiOverride);
                break;

            case FoBlockContainer blockContainer:
                foreach (var child in blockContainer.Children)
                    EstablishParentRelationships(child, blockContainer);
                break;

            case FoInlineContainer inlineContainer:
                foreach (var child in inlineContainer.Children)
                    EstablishParentRelationships(child, inlineContainer);
                break;
        }
    }

    private static FoRoot ParseRoot(XElement element)
    {
        FoLayoutMasterSet? layoutMasterSet = null;
        FoDeclarations? declarations = null;
        FoBookmarkTree? bookmarkTree = null;
        var pageSequences = new List<FoPageSequence>();

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "layout-master-set":
                    layoutMasterSet = ParseLayoutMasterSet(child);
                    break;
                case "declarations":
                    declarations = ParseDeclarations(child);
                    break;
                case "bookmark-tree":
                    bookmarkTree = ParseBookmarkTree(child);
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
            Declarations = declarations,
            BookmarkTree = bookmarkTree,
            PageSequences = pageSequences
        };
    }

    private static FoLayoutMasterSet ParseLayoutMasterSet(XElement element)
    {
        var pageMasters = new List<FoSimplePageMaster>();
        var pageSequenceMasters = new List<FoPageSequenceMaster>();

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "simple-page-master":
                    pageMasters.Add(ParseSimplePageMaster(child));
                    break;
                case "page-sequence-master":
                    pageSequenceMasters.Add(ParsePageSequenceMaster(child));
                    break;
            }
        }

        return new FoLayoutMasterSet
        {
            Properties = ParseProperties(element),
            SimplePageMasters = pageMasters,
            PageSequenceMasters = pageSequenceMasters
        };
    }

    private static FoSimplePageMaster ParseSimplePageMaster(XElement element)
    {
        FoRegionBody? regionBody = null;
        FoRegion? regionBefore = null;
        FoRegion? regionAfter = null;
        FoRegion? regionStart = null;
        FoRegion? regionEnd = null;

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
                case "region-start":
                    regionStart = new FoRegionStart { Properties = ParseProperties(child) };
                    break;
                case "region-end":
                    regionEnd = new FoRegionEnd { Properties = ParseProperties(child) };
                    break;
            }
        }

        return new FoSimplePageMaster
        {
            Properties = ParseProperties(element),
            RegionBody = regionBody,
            RegionBefore = regionBefore,
            RegionAfter = regionAfter,
            RegionStart = regionStart,
            RegionEnd = regionEnd
        };
    }

    private static FoPageSequenceMaster ParsePageSequenceMaster(XElement element)
    {
        FoSinglePageMasterReference? singlePageMasterRef = null;
        FoRepeatablePageMasterReference? repeatablePageMasterRef = null;
        FoRepeatablePageMasterAlternatives? repeatablePageMasterAlternatives = null;

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "single-page-master-reference":
                    singlePageMasterRef = ParseSinglePageMasterReference(child);
                    break;
                case "repeatable-page-master-reference":
                    repeatablePageMasterRef = ParseRepeatablePageMasterReference(child);
                    break;
                case "repeatable-page-master-alternatives":
                    repeatablePageMasterAlternatives = ParseRepeatablePageMasterAlternatives(child);
                    break;
            }
        }

        return new FoPageSequenceMaster
        {
            Properties = ParseProperties(element),
            SinglePageMasterReference = singlePageMasterRef,
            RepeatablePageMasterReference = repeatablePageMasterRef,
            RepeatablePageMasterAlternatives = repeatablePageMasterAlternatives
        };
    }

    private static FoSinglePageMasterReference ParseSinglePageMasterReference(XElement element)
    {
        return new FoSinglePageMasterReference
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoRepeatablePageMasterReference ParseRepeatablePageMasterReference(XElement element)
    {
        return new FoRepeatablePageMasterReference
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoRepeatablePageMasterAlternatives ParseRepeatablePageMasterAlternatives(XElement element)
    {
        var conditionalRefs = new List<FoConditionalPageMasterReference>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "conditional-page-master-reference")
                conditionalRefs.Add(ParseConditionalPageMasterReference(child));
        }

        return new FoRepeatablePageMasterAlternatives
        {
            Properties = ParseProperties(element),
            ConditionalPageMasterReferences = conditionalRefs
        };
    }

    private static FoConditionalPageMasterReference ParseConditionalPageMasterReference(XElement element)
    {
        return new FoConditionalPageMasterReference
        {
            Properties = ParseProperties(element)
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
        var tableAndCaptions = new List<FoTableAndCaption>();
        var lists = new List<FoListBlock>();
        var blockContainers = new List<FoBlockContainer>();

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
                case "table-and-caption":
                    tableAndCaptions.Add(ParseTableAndCaption(child));
                    break;
                case "list-block":
                    lists.Add(ParseListBlock(child));
                    break;
                case "block-container":
                    blockContainers.Add(ParseBlockContainer(child));
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
            TableAndCaptions = tableAndCaptions,
            Lists = lists,
            BlockContainers = blockContainers,
            TextContent = textContent
        };
    }

    private static FoBlock ParseBlock(XElement element)
    {
        var children = new List<FoElement>();
        var footnotes = new List<FoFootnote>();
        var floats = new List<FoFloat>();

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
                case "block-container":
                    children.Add(ParseBlockContainer(child));
                    break;
                case "external-graphic":
                    children.Add(ParseExternalGraphic(child));
                    break;
                case "instream-foreign-object":
                    children.Add(ParseInstreamForeignObject(child));
                    break;
                case "page-number":
                    children.Add(ParsePageNumber(child));
                    break;
                case "page-number-citation":
                    children.Add(ParsePageNumberCitation(child));
                    break;
                case "page-number-citation-last":
                    children.Add(ParsePageNumberCitationLast(child));
                    break;
                case "index-range-begin":
                    children.Add(ParseIndexRangeBegin(child));
                    break;
                case "index-range-end":
                    children.Add(ParseIndexRangeEnd(child));
                    break;
                case "index-key-reference":
                    children.Add(ParseIndexKeyReference(child));
                    break;
                case "marker":
                    children.Add(ParseMarker(child));
                    break;
                case "footnote":
                    footnotes.Add(ParseFootnote(child));
                    break;
                case "float":
                    floats.Add(ParseFloat(child));
                    break;
                case "basic-link":
                    children.Add(ParseBasicLink(child));
                    break;
                case "inline":
                    children.Add(ParseInline(child));
                    break;
                case "inline-container":
                    children.Add(ParseInlineContainer(child));
                    break;
                case "leader":
                    children.Add(ParseLeader(child));
                    break;
                case "character":
                    children.Add(ParseCharacter(child));
                    break;
                case "wrapper":
                    children.Add(ParseWrapper(child));
                    break;
                case "bidi-override":
                    children.Add(ParseBidiOverride(child));
                    break;
                case "multi-switch":
                    children.Add(ParseMultiSwitch(child));
                    break;
                case "multi-properties":
                    children.Add(ParseMultiProperties(child));
                    break;
            }
        }

        return new FoBlock
        {
            Properties = ParseProperties(element),
            Children = children,
            Footnotes = footnotes,
            Floats = floats,
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

    private static FoInstreamForeignObject ParseInstreamForeignObject(XElement element)
    {
        // Extract the first non-FO child element (e.g., svg:svg)
        XElement? foreignContent = null;
        foreach (var child in element.Elements())
        {
            // Skip elements in the FO namespace, take the first foreign element
            if (child.Name.Namespace != FoNamespace)
            {
                foreignContent = child;
                break;
            }
        }

        return new FoInstreamForeignObject
        {
            Properties = ParseProperties(element),
            ForeignContent = foreignContent
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

    private static FoRetrieveTableMarker ParseRetrieveTableMarker(XElement element)
    {
        return new FoRetrieveTableMarker
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoMultiSwitch ParseMultiSwitch(XElement element)
    {
        var multiCases = new List<FoMultiCase>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "multi-case")
                multiCases.Add(ParseMultiCase(child));
        }

        return new FoMultiSwitch
        {
            Properties = ParseProperties(element),
            MultiCases = multiCases
        };
    }

    private static FoMultiCase ParseMultiCase(XElement element)
    {
        var blocks = new List<FoBlock>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "block")
                blocks.Add(ParseBlock(child));
        }

        return new FoMultiCase
        {
            Properties = ParseProperties(element),
            Blocks = blocks
        };
    }

    private static FoMultiToggle ParseMultiToggle(XElement element)
    {
        var inlines = new List<FoInline>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "inline")
                inlines.Add(ParseInline(child));
        }

        return new FoMultiToggle
        {
            Properties = ParseProperties(element),
            Inlines = inlines
        };
    }

    private static FoMultiProperties ParseMultiProperties(XElement element)
    {
        var multiPropertySets = new List<FoMultiPropertySet>();
        FoElement? wrapper = null;

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            if (name == "multi-property-set")
            {
                multiPropertySets.Add(ParseMultiPropertySet(child));
            }
            else if (name == "wrapper")
            {
                // Parse the wrapper element (typically a block or inline)
                var wrapperChild = child.Elements().FirstOrDefault();
                if (wrapperChild != null)
                {
                    wrapper = wrapperChild.Name.LocalName switch
                    {
                        "block" => ParseBlock(wrapperChild),
                        "inline" => ParseInline(wrapperChild),
                        _ => null
                    };
                }
            }
        }

        return new FoMultiProperties
        {
            Properties = ParseProperties(element),
            MultiPropertySets = multiPropertySets,
            Wrapper = wrapper
        };
    }

    private static FoMultiPropertySet ParseMultiPropertySet(XElement element)
    {
        return new FoMultiPropertySet
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoFootnote ParseFootnote(XElement element)
    {
        FoFootnoteBody? footnoteBody = null;
        string? inlineText = null;

        // Parse inline element (first child) - extract text content
        var inlineElement = element.Elements().FirstOrDefault(e => e.Name.LocalName == "inline");
        if (inlineElement != null)
        {
            inlineText = inlineElement.Value;
        }

        // Parse footnote-body element
        var bodyElement = element.Elements().FirstOrDefault(e => e.Name.LocalName == "footnote-body");
        if (bodyElement != null)
        {
            footnoteBody = ParseFootnoteBody(bodyElement);
        }

        return new FoFootnote
        {
            Properties = ParseProperties(element),
            InlineText = inlineText,
            FootnoteBody = footnoteBody
        };
    }

    private static FoFootnoteBody ParseFootnoteBody(XElement element)
    {
        var blocks = new List<FoBlock>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "block")
                blocks.Add(ParseBlock(child));
        }

        return new FoFootnoteBody
        {
            Properties = ParseProperties(element),
            Blocks = blocks
        };
    }

    private static FoFloat ParseFloat(XElement element)
    {
        var blocks = new List<FoBlock>();

        // Parse block children that make up the float content
        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "block")
                blocks.Add(ParseBlock(child));
        }

        return new FoFloat
        {
            Properties = ParseProperties(element),
            Blocks = blocks
        };
    }

    private static FoBasicLink ParseBasicLink(XElement element)
    {
        var children = new List<FoElement>();

        // Collect text content
        var textContent = string.Join("", element.Nodes()
            .OfType<XText>()
            .Select(t => t.Value));

        // Parse child elements (can contain nested blocks, images, etc.)
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
                case "instream-foreign-object":
                    children.Add(ParseInstreamForeignObject(child));
                    break;
                case "page-number":
                    children.Add(ParsePageNumber(child));
                    break;
            }
        }

        return new FoBasicLink
        {
            Properties = ParseProperties(element),
            Children = children,
            TextContent = string.IsNullOrWhiteSpace(textContent) ? null : textContent
        };
    }

    private static FoInline ParseInline(XElement element)
    {
        var children = new List<FoElement>();

        // Collect text content
        var textContent = string.Join("", element.Nodes()
            .OfType<XText>()
            .Select(t => t.Value));

        // Parse child elements (inline can contain nested inlines, page-numbers, etc.)
        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "inline":
                    children.Add(ParseInline(child));
                    break;
                case "inline-container":
                    children.Add(ParseInlineContainer(child));
                    break;
                case "page-number":
                    children.Add(ParsePageNumber(child));
                    break;
                case "page-number-citation":
                    children.Add(ParsePageNumberCitation(child));
                    break;
                case "page-number-citation-last":
                    children.Add(ParsePageNumberCitationLast(child));
                    break;
                case "index-range-begin":
                    children.Add(ParseIndexRangeBegin(child));
                    break;
                case "index-range-end":
                    children.Add(ParseIndexRangeEnd(child));
                    break;
                case "index-key-reference":
                    children.Add(ParseIndexKeyReference(child));
                    break;
                case "basic-link":
                    children.Add(ParseBasicLink(child));
                    break;
                case "leader":
                    children.Add(ParseLeader(child));
                    break;
                case "character":
                    children.Add(ParseCharacter(child));
                    break;
                case "wrapper":
                    children.Add(ParseWrapper(child));
                    break;
                case "bidi-override":
                    children.Add(ParseBidiOverride(child));
                    break;
                case "external-graphic":
                    children.Add(ParseExternalGraphic(child));
                    break;
                case "instream-foreign-object":
                    children.Add(ParseInstreamForeignObject(child));
                    break;
            }
        }

        return new FoInline
        {
            Properties = ParseProperties(element),
            Children = children,
            TextContent = string.IsNullOrWhiteSpace(textContent) ? null : textContent
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
        var retrieveTableMarkers = new List<FoRetrieveTableMarker>();

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "block":
                    blocks.Add(ParseBlock(child));
                    break;
                case "retrieve-table-marker":
                    retrieveTableMarkers.Add(ParseRetrieveTableMarker(child));
                    break;
            }
        }

        return new FoTableCell
        {
            Properties = ParseProperties(element),
            Blocks = blocks,
            RetrieveTableMarkers = retrieveTableMarkers
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

    private static FoBookmarkTree ParseBookmarkTree(XElement element)
    {
        var bookmarks = new List<FoBookmark>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "bookmark")
                bookmarks.Add(ParseBookmark(child));
        }

        return new FoBookmarkTree
        {
            Properties = ParseProperties(element),
            Bookmarks = bookmarks
        };
    }

    private static FoBookmark ParseBookmark(XElement element)
    {
        string? title = null;
        var children = new List<FoBookmark>();

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "bookmark-title":
                    title = child.Value;
                    break;
                case "bookmark":
                    children.Add(ParseBookmark(child));
                    break;
            }
        }

        return new FoBookmark
        {
            Properties = ParseProperties(element),
            Title = title,
            Children = children
        };
    }

    private static FoDeclarations ParseDeclarations(XElement element)
    {
        FoInfo? info = null;

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "info")
                info = ParseInfo(child);
        }

        return new FoDeclarations
        {
            Properties = ParseProperties(element),
            Info = info
        };
    }

    private static FoInfo ParseInfo(XElement element)
    {
        string? title = null;
        string? author = null;
        string? subject = null;
        string? keywords = null;
        string? creator = null;

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "title":
                    title = child.Value;
                    break;
                case "author":
                    author = child.Value;
                    break;
                case "subject":
                    subject = child.Value;
                    break;
                case "keywords":
                    keywords = child.Value;
                    break;
                case "creator":
                    creator = child.Value;
                    break;
            }
        }

        return new FoInfo
        {
            Properties = ParseProperties(element),
            Title = title,
            Author = author,
            Subject = subject,
            Keywords = keywords,
            Creator = creator
        };
    }

    private static FoLeader ParseLeader(XElement element)
    {
        return new FoLeader
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoPageNumberCitation ParsePageNumberCitation(XElement element)
    {
        return new FoPageNumberCitation
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoPageNumberCitationLast ParsePageNumberCitationLast(XElement element)
    {
        return new FoPageNumberCitationLast
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoCharacter ParseCharacter(XElement element)
    {
        return new FoCharacter
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoWrapper ParseWrapper(XElement element)
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
                case "inline":
                    children.Add(ParseInline(child));
                    break;
                case "wrapper":
                    children.Add(ParseWrapper(child));
                    break;
                case "page-number":
                    children.Add(ParsePageNumber(child));
                    break;
                case "page-number-citation":
                    children.Add(ParsePageNumberCitation(child));
                    break;
                case "page-number-citation-last":
                    children.Add(ParsePageNumberCitationLast(child));
                    break;
                case "index-range-begin":
                    children.Add(ParseIndexRangeBegin(child));
                    break;
                case "index-range-end":
                    children.Add(ParseIndexRangeEnd(child));
                    break;
                case "index-key-reference":
                    children.Add(ParseIndexKeyReference(child));
                    break;
                case "leader":
                    children.Add(ParseLeader(child));
                    break;
                case "character":
                    children.Add(ParseCharacter(child));
                    break;
                case "bidi-override":
                    children.Add(ParseBidiOverride(child));
                    break;
                case "inline-container":
                    children.Add(ParseInlineContainer(child));
                    break;
                case "external-graphic":
                    children.Add(ParseExternalGraphic(child));
                    break;
                case "instream-foreign-object":
                    children.Add(ParseInstreamForeignObject(child));
                    break;
                case "basic-link":
                    children.Add(ParseBasicLink(child));
                    break;
            }
        }

        return new FoWrapper
        {
            Properties = ParseProperties(element),
            Children = children
        };
    }

    private static FoBidiOverride ParseBidiOverride(XElement element)
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
                case "inline":
                    children.Add(ParseInline(child));
                    break;
                case "page-number":
                    children.Add(ParsePageNumber(child));
                    break;
                case "page-number-citation":
                    children.Add(ParsePageNumberCitation(child));
                    break;
                case "page-number-citation-last":
                    children.Add(ParsePageNumberCitationLast(child));
                    break;
                case "index-range-begin":
                    children.Add(ParseIndexRangeBegin(child));
                    break;
                case "index-range-end":
                    children.Add(ParseIndexRangeEnd(child));
                    break;
                case "index-key-reference":
                    children.Add(ParseIndexKeyReference(child));
                    break;
                case "leader":
                    children.Add(ParseLeader(child));
                    break;
                case "character":
                    children.Add(ParseCharacter(child));
                    break;
                case "wrapper":
                    children.Add(ParseWrapper(child));
                    break;
                case "bidi-override":
                    children.Add(ParseBidiOverride(child));
                    break;
                case "inline-container":
                    children.Add(ParseInlineContainer(child));
                    break;
                case "external-graphic":
                    children.Add(ParseExternalGraphic(child));
                    break;
                case "instream-foreign-object":
                    children.Add(ParseInstreamForeignObject(child));
                    break;
                case "basic-link":
                    children.Add(ParseBasicLink(child));
                    break;
            }
        }

        return new FoBidiOverride
        {
            Properties = ParseProperties(element),
            Children = children
        };
    }

    private static FoBlockContainer ParseBlockContainer(XElement element)
    {
        var children = new List<FoElement>();

        // Parse child elements (blocks, tables, lists, etc.)
        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "block":
                    children.Add(ParseBlock(child));
                    break;
                case "block-container":
                    children.Add(ParseBlockContainer(child));
                    break;
                case "table":
                    children.Add(ParseTable(child));
                    break;
                case "list-block":
                    children.Add(ParseListBlock(child));
                    break;
            }
        }

        return new FoBlockContainer
        {
            Properties = ParseProperties(element),
            Children = children
        };
    }

    private static FoInlineContainer ParseInlineContainer(XElement element)
    {
        var children = new List<FoElement>();

        // Parse child elements (blocks mainly)
        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "block":
                    children.Add(ParseBlock(child));
                    break;
                case "block-container":
                    children.Add(ParseBlockContainer(child));
                    break;
                case "table":
                    children.Add(ParseTable(child));
                    break;
            }
        }

        return new FoInlineContainer
        {
            Properties = ParseProperties(element),
            Children = children
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

        // Expand shorthand properties
        ExpandShorthandProperties(props);

        return props;
    }

    /// <summary>
    /// Expands shorthand properties (margin, padding, border-*) into their individual properties.
    /// For example, margin="10pt" expands to margin-left="10pt", margin-right="10pt", etc.
    /// </summary>
    private static void ExpandShorthandProperties(FoProperties props)
    {
        // Expand margin shorthand
        if (props.HasProperty("margin") && !string.IsNullOrWhiteSpace(props["margin"]))
        {
            var value = props["margin"]!;
            // Only set individual properties if they aren't already explicitly set
            if (!props.HasProperty("margin-left"))
                props["margin-left"] = value;
            if (!props.HasProperty("margin-right"))
                props["margin-right"] = value;
            if (!props.HasProperty("margin-top"))
                props["margin-top"] = value;
            if (!props.HasProperty("margin-bottom"))
                props["margin-bottom"] = value;
        }

        // Expand padding shorthand
        if (props.HasProperty("padding") && !string.IsNullOrWhiteSpace(props["padding"]))
        {
            var value = props["padding"]!;
            if (!props.HasProperty("padding-left"))
                props["padding-left"] = value;
            if (!props.HasProperty("padding-right"))
                props["padding-right"] = value;
            if (!props.HasProperty("padding-top"))
                props["padding-top"] = value;
            if (!props.HasProperty("padding-bottom"))
                props["padding-bottom"] = value;
        }

        // Expand border shorthand (e.g., border="2pt solid black")
        // Parse into border-width, border-style, and border-color
        if (props.HasProperty("border") && !string.IsNullOrWhiteSpace(props["border"]))
        {
            var borderValue = props["border"]!.Trim();
            var parts = borderValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Parse border shorthand: can contain width, style, and/or color in any order
            foreach (var part in parts)
            {
                // Check if it's a width (contains 'pt', 'px', 'mm', etc. or is a number)
                if (part.Contains("pt") || part.Contains("px") || part.Contains("mm") ||
                    part.Contains("cm") || part.Contains("in") || part.Contains("em") ||
                    char.IsDigit(part[0]))
                {
                    if (!props.HasProperty("border-width"))
                        props["border-width"] = part;
                }
                // Check if it's a style keyword
                else if (part is "none" or "hidden" or "dotted" or "dashed" or "solid" or
                         "double" or "groove" or "ridge" or "inset" or "outset")
                {
                    if (!props.HasProperty("border-style"))
                        props["border-style"] = part;
                }
                // Otherwise assume it's a color
                else
                {
                    if (!props.HasProperty("border-color"))
                        props["border-color"] = part;
                }
            }
        }

        // Expand border-width shorthand
        if (props.HasProperty("border-width") && !string.IsNullOrWhiteSpace(props["border-width"]))
        {
            var value = props["border-width"]!;
            if (!props.HasProperty("border-top-width"))
                props["border-top-width"] = value;
            if (!props.HasProperty("border-right-width"))
                props["border-right-width"] = value;
            if (!props.HasProperty("border-bottom-width"))
                props["border-bottom-width"] = value;
            if (!props.HasProperty("border-left-width"))
                props["border-left-width"] = value;
        }

        // Expand border-style shorthand
        if (props.HasProperty("border-style") && !string.IsNullOrWhiteSpace(props["border-style"]))
        {
            var value = props["border-style"]!;
            if (!props.HasProperty("border-top-style"))
                props["border-top-style"] = value;
            if (!props.HasProperty("border-right-style"))
                props["border-right-style"] = value;
            if (!props.HasProperty("border-bottom-style"))
                props["border-bottom-style"] = value;
            if (!props.HasProperty("border-left-style"))
                props["border-left-style"] = value;
        }

        // Expand border-color shorthand
        if (props.HasProperty("border-color") && !string.IsNullOrWhiteSpace(props["border-color"]))
        {
            var value = props["border-color"]!;
            if (!props.HasProperty("border-top-color"))
                props["border-top-color"] = value;
            if (!props.HasProperty("border-right-color"))
                props["border-right-color"] = value;
            if (!props.HasProperty("border-bottom-color"))
                props["border-bottom-color"] = value;
            if (!props.HasProperty("border-left-color"))
                props["border-left-color"] = value;
        }
    }

    private static FoTableAndCaption ParseTableAndCaption(XElement element)
    {
        FoTableCaption? caption = null;
        FoTable? table = null;

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "table-caption":
                    caption = ParseTableCaption(child);
                    break;
                case "table":
                    table = ParseTable(child);
                    break;
            }
        }

        return new FoTableAndCaption
        {
            Properties = ParseProperties(element),
            Caption = caption,
            Table = table
        };
    }

    private static FoTableCaption ParseTableCaption(XElement element)
    {
        var blocks = new List<FoBlock>();

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "block")
                blocks.Add(ParseBlock(child));
        }

        return new FoTableCaption
        {
            Properties = ParseProperties(element),
            Blocks = blocks
        };
    }

    private static FoIndexRangeBegin ParseIndexRangeBegin(XElement element)
    {
        return new FoIndexRangeBegin
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoIndexRangeEnd ParseIndexRangeEnd(XElement element)
    {
        return new FoIndexRangeEnd
        {
            Properties = ParseProperties(element)
        };
    }

    private static FoIndexKeyReference ParseIndexKeyReference(XElement element)
    {
        var prefixes = new List<FoIndexPageNumberPrefix>();
        var suffixes = new List<FoIndexPageNumberSuffix>();
        var citationLists = new List<FoIndexPageCitationList>();

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "index-page-number-prefix":
                    prefixes.Add(ParseIndexPageNumberPrefix(child));
                    break;
                case "index-page-number-suffix":
                    suffixes.Add(ParseIndexPageNumberSuffix(child));
                    break;
                case "index-page-citation-list":
                    citationLists.Add(ParseIndexPageCitationList(child));
                    break;
            }
        }

        return new FoIndexKeyReference
        {
            Properties = ParseProperties(element),
            PageNumberPrefixes = prefixes,
            PageNumberSuffixes = suffixes,
            PageCitationLists = citationLists
        };
    }

    private static FoIndexPageNumberPrefix ParseIndexPageNumberPrefix(XElement element)
    {
        var textContent = string.Join("", element.Nodes()
            .OfType<XText>()
            .Select(t => t.Value));

        return new FoIndexPageNumberPrefix
        {
            Properties = ParseProperties(element),
            TextContent = string.IsNullOrWhiteSpace(textContent) ? null : textContent
        };
    }

    private static FoIndexPageNumberSuffix ParseIndexPageNumberSuffix(XElement element)
    {
        var textContent = string.Join("", element.Nodes()
            .OfType<XText>()
            .Select(t => t.Value));

        return new FoIndexPageNumberSuffix
        {
            Properties = ParseProperties(element),
            TextContent = string.IsNullOrWhiteSpace(textContent) ? null : textContent
        };
    }

    private static FoIndexPageCitationList ParseIndexPageCitationList(XElement element)
    {
        var listSeparators = new List<FoIndexPageCitationListSeparator>();
        var rangeSeparators = new List<FoIndexPageCitationRangeSeparator>();

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "index-page-citation-list-separator":
                    listSeparators.Add(ParseIndexPageCitationListSeparator(child));
                    break;
                case "index-page-citation-range-separator":
                    rangeSeparators.Add(ParseIndexPageCitationRangeSeparator(child));
                    break;
            }
        }

        return new FoIndexPageCitationList
        {
            Properties = ParseProperties(element),
            ListSeparators = listSeparators,
            RangeSeparators = rangeSeparators
        };
    }

    private static FoIndexPageCitationListSeparator ParseIndexPageCitationListSeparator(XElement element)
    {
        var textContent = string.Join("", element.Nodes()
            .OfType<XText>()
            .Select(t => t.Value));

        return new FoIndexPageCitationListSeparator
        {
            Properties = ParseProperties(element),
            TextContent = string.IsNullOrWhiteSpace(textContent) ? null : textContent
        };
    }

    private static FoIndexPageCitationRangeSeparator ParseIndexPageCitationRangeSeparator(XElement element)
    {
        var textContent = string.Join("", element.Nodes()
            .OfType<XText>()
            .Select(t => t.Value));

        return new FoIndexPageCitationRangeSeparator
        {
            Properties = ParseProperties(element),
            TextContent = string.IsNullOrWhiteSpace(textContent) ? null : textContent
        };
    }
}
