namespace Folly.Pdf;

/// <summary>
/// Represents the PDF structure tree for tagged PDF (accessibility support).
/// Implements PDF 1.7 structure hierarchy for document accessibility and logical structure.
/// </summary>
/// <remarks>
/// The structure tree provides a logical hierarchy of document elements (paragraphs,
/// headings, tables, etc.) that enables screen readers and assistive technologies
/// to navigate and understand document content. This is the foundation for PDF/UA compliance.
///
/// Key concepts:
/// - Structure Tree Root: Top-level container for all structure elements
/// - Structure Elements: Nodes in the tree representing logical document parts
/// - Marked Content: PDF content wrapped with BDC...EMC operators, linked via MCID
/// - Role Types: Standard element types (P, H1-H6, Table, TR, TD, Figure, etc.)
/// </remarks>
internal sealed class PdfStructureTree
{
    private readonly List<StructureElement> _rootElements = new();
    private readonly Dictionary<int, List<MarkedContentReference>> _pageMarkedContent = new();
    private int _nextMcid;

    /// <summary>
    /// Gets the root-level structure elements (typically Document elements).
    /// </summary>
    public IReadOnlyList<StructureElement> RootElements => _rootElements;

    /// <summary>
    /// Gets all marked content references organized by page number.
    /// </summary>
    public IReadOnlyDictionary<int, List<MarkedContentReference>> PageMarkedContent => _pageMarkedContent;

    /// <summary>
    /// Creates a new structure element with the specified role.
    /// </summary>
    public StructureElement CreateElement(StructureRole role, StructureElement? parent = null)
    {
        var element = new StructureElement
        {
            Role = role,
            Parent = parent
        };

        if (parent == null)
        {
            _rootElements.Add(element);
        }
        else
        {
            parent.Children.Add(element);
        }

        return element;
    }

    /// <summary>
    /// Registers marked content for a structure element on a specific page.
    /// Returns the MCID (Marked Content Identifier) to use in PDF content stream.
    /// </summary>
    public int RegisterMarkedContent(StructureElement element, int pageNumber)
    {
        var mcid = _nextMcid++;

        if (!_pageMarkedContent.ContainsKey(pageNumber))
        {
            _pageMarkedContent[pageNumber] = new List<MarkedContentReference>();
        }

        _pageMarkedContent[pageNumber].Add(new MarkedContentReference
        {
            Mcid = mcid,
            Element = element
        });

        // Add MCID reference to structure element
        element.MarkedContentIds.Add(mcid);

        return mcid;
    }

    /// <summary>
    /// Writes the structure tree root and all structure elements to PDF.
    /// Returns the object ID of the structure tree root.
    /// </summary>
    public int WriteToPdf(PdfWriter writer, int[] pageObjectIds)
    {
        if (_rootElements.Count == 0)
        {
            // No structure tree - return 0 to indicate no StructTreeRoot
            return 0;
        }

        // First, assign object IDs to all structure elements
        AssignObjectIds(writer);

        // Write all structure elements
        foreach (var root in _rootElements)
        {
            WriteStructureElement(writer, root, pageObjectIds);
        }

        // Write parent tree (maps MCIDs to structure elements)
        var parentTreeId = WriteParentTree(writer, pageObjectIds);

        // Write structure tree root
        var structTreeRootId = writer.BeginObject();
        writer.WriteLine("<<");
        writer.WriteLine("  /Type /StructTreeRoot");

        // Write K array (root elements)
        if (_rootElements.Count == 1)
        {
            writer.WriteLine($"  /K {_rootElements[0].ObjectId} 0 R");
        }
        else
        {
            writer.Write("  /K [");
            for (int i = 0; i < _rootElements.Count; i++)
            {
                if (i > 0) writer.Write(" ");
                writer.Write($"{_rootElements[i].ObjectId} 0 R");
            }
            writer.WriteLine("]");
        }

        writer.WriteLine($"  /ParentTree {parentTreeId} 0 R");
        writer.WriteLine(">>");
        writer.EndObject();

        return structTreeRootId;
    }

    private void AssignObjectIds(PdfWriter writer)
    {
        foreach (var root in _rootElements)
        {
            AssignObjectIdRecursive(writer, root);
        }
    }

    private void AssignObjectIdRecursive(PdfWriter writer, StructureElement element)
    {
        element.ObjectId = writer.ReserveObjectId();
        foreach (var child in element.Children)
        {
            AssignObjectIdRecursive(writer, child);
        }
    }

    private void WriteStructureElement(PdfWriter writer, StructureElement element, int[] pageObjectIds)
    {
        writer.BeginObject(element.ObjectId);
        writer.WriteLine("<<");
        writer.WriteLine("  /Type /StructElem");
        writer.WriteLine($"  /S /{GetRoleString(element.Role)}");

        // Write parent reference
        if (element.Parent != null)
        {
            writer.WriteLine($"  /P {element.Parent.ObjectId} 0 R");
        }
        else
        {
            // Root elements reference the StructTreeRoot (will be set later)
            // For now, we'll reference the StructTreeRoot via indirect reference
            // This will be resolved when writing the StructTreeRoot
        }

        // Write language if specified
        if (!string.IsNullOrEmpty(element.Language))
        {
            writer.WriteLine($"  /Lang ({PdfWriter.EscapeString(element.Language)})");
        }

        // Write alt text if specified (for images, figures, etc.)
        if (!string.IsNullOrEmpty(element.AltText))
        {
            writer.WriteLine($"  /Alt ({PdfWriter.EscapeString(element.AltText)})");
        }

        // Write actual text if specified (replacement text for abbreviations, etc.)
        if (!string.IsNullOrEmpty(element.ActualText))
        {
            writer.WriteLine($"  /ActualText ({PdfWriter.EscapeString(element.ActualText)})");
        }

        // Write children (K entry)
        if (element.Children.Count > 0 || element.MarkedContentIds.Count > 0)
        {
            WriteKEntry(writer, element, pageObjectIds);
        }

        writer.WriteLine(">>");
        writer.EndObject();

        // Recursively write child elements
        foreach (var child in element.Children)
        {
            WriteStructureElement(writer, child, pageObjectIds);
        }
    }

    private void WriteKEntry(PdfWriter writer, StructureElement element, int[] pageObjectIds)
    {
        var hasChildren = element.Children.Count > 0;
        var hasMcids = element.MarkedContentIds.Count > 0;

        if (hasChildren && !hasMcids)
        {
            // Only child structure elements
            if (element.Children.Count == 1)
            {
                writer.WriteLine($"  /K {element.Children[0].ObjectId} 0 R");
            }
            else
            {
                writer.Write("  /K [");
                for (int i = 0; i < element.Children.Count; i++)
                {
                    if (i > 0) writer.Write(" ");
                    writer.Write($"{element.Children[i].ObjectId} 0 R");
                }
                writer.WriteLine("]");
            }
        }
        else if (!hasChildren && hasMcids)
        {
            // Only marked content references
            if (element.MarkedContentIds.Count == 1)
            {
                // Single MCID: use simple integer reference with /Pg
                var mcid = element.MarkedContentIds[0];
                var pageNum = FindPageForMcid(mcid);
                if (pageNum >= 0 && pageNum < pageObjectIds.Length)
                {
                    writer.WriteLine($"  /K {mcid}");
                    writer.WriteLine($"  /Pg {pageObjectIds[pageNum]} 0 R");
                }
            }
            else
            {
                // Multiple MCIDs: check if they're all on the same page
                var mcidPages = new Dictionary<int, int>();
                foreach (var mcid in element.MarkedContentIds)
                {
                    mcidPages[mcid] = FindPageForMcid(mcid);
                }

                var uniquePages = mcidPages.Values.Distinct().Count();

                if (uniquePages == 1)
                {
                    // All MCIDs on same page: use simple array with single /Pg
                    writer.Write("  /K [");
                    for (int i = 0; i < element.MarkedContentIds.Count; i++)
                    {
                        if (i > 0) writer.Write(" ");
                        writer.Write($"{element.MarkedContentIds[i]}");
                    }
                    writer.WriteLine("]");

                    var pageNum = mcidPages.Values.First();
                    if (pageNum >= 0 && pageNum < pageObjectIds.Length)
                    {
                        writer.WriteLine($"  /Pg {pageObjectIds[pageNum]} 0 R");
                    }
                }
                else
                {
                    // MCIDs span multiple pages: use MCR dictionaries with individual /Pg references
                    writer.WriteLine("  /K [");
                    for (int i = 0; i < element.MarkedContentIds.Count; i++)
                    {
                        var mcid = element.MarkedContentIds[i];
                        var pageNum = mcidPages[mcid];

                        if (pageNum >= 0 && pageNum < pageObjectIds.Length)
                        {
                            if (i > 0)
                                writer.Write(" ");
                            writer.Write("<<");
                            writer.Write(" /Type /MCR");
                            writer.Write($" /Pg {pageObjectIds[pageNum]} 0 R");
                            writer.Write($" /MCID {mcid}");
                            writer.Write(" >>");
                        }
                    }
                    writer.WriteLine("  ]");
                }
            }
        }
        else if (hasChildren && hasMcids)
        {
            // Mixed: both children and marked content
            // Check if MCIDs are all on the same page
            var mcidPages = new Dictionary<int, int>();
            foreach (var mcid in element.MarkedContentIds)
            {
                mcidPages[mcid] = FindPageForMcid(mcid);
            }

            var uniquePages = mcidPages.Values.Distinct().Count();

            if (uniquePages == 1)
            {
                // All MCIDs on same page: use simple format with single /Pg
                writer.Write("  /K [");
                bool first = true;

                // Add marked content IDs
                foreach (var mcid in element.MarkedContentIds)
                {
                    if (!first) writer.Write(" ");
                    writer.Write($"{mcid}");
                    first = false;
                }

                // Add child structure elements
                foreach (var child in element.Children)
                {
                    if (!first) writer.Write(" ");
                    writer.Write($"{child.ObjectId} 0 R");
                    first = false;
                }

                writer.WriteLine("]");

                var pageNum = mcidPages.Values.First();
                if (pageNum >= 0 && pageNum < pageObjectIds.Length)
                {
                    writer.WriteLine($"  /Pg {pageObjectIds[pageNum]} 0 R");
                }
            }
            else
            {
                // MCIDs span multiple pages: use MCR dictionaries
                writer.Write("  /K [");
                bool first = true;

                // Add marked content IDs as MCR dictionaries
                foreach (var mcid in element.MarkedContentIds)
                {
                    if (!first) writer.Write(" ");
                    var pageNum = mcidPages[mcid];

                    if (pageNum >= 0 && pageNum < pageObjectIds.Length)
                    {
                        writer.Write("<<");
                        writer.Write(" /Type /MCR");
                        writer.Write($" /Pg {pageObjectIds[pageNum]} 0 R");
                        writer.Write($" /MCID {mcid}");
                        writer.Write(" >>");
                    }
                    first = false;
                }

                // Add child structure elements
                foreach (var child in element.Children)
                {
                    if (!first) writer.Write(" ");
                    writer.Write($"{child.ObjectId} 0 R");
                    first = false;
                }

                writer.WriteLine("]");
            }
        }
    }

    private int FindPageForMcid(int mcid)
    {
        foreach (var kvp in _pageMarkedContent)
        {
            if (kvp.Value.Any(mc => mc.Mcid == mcid))
            {
                return kvp.Key;
            }
        }
        return -1;
    }

    private int WriteParentTree(PdfWriter writer, int[] pageObjectIds)
    {
        // Build number tree mapping MCIDs to structure elements
        // For simplicity, we'll use a flat array (Nums array)
        var parentTreeId = writer.BeginObject();
        writer.WriteLine("<<");

        // Write Nums array
        writer.Write("  /Nums [");

        bool first = true;
        foreach (var pageNum in _pageMarkedContent.Keys.OrderBy(k => k))
        {
            var markedContent = _pageMarkedContent[pageNum];

            foreach (var mc in markedContent.OrderBy(m => m.Mcid))
            {
                if (!first) writer.Write(" ");
                writer.Write($"{mc.Mcid} {mc.Element.ObjectId} 0 R");
                first = false;
            }
        }

        writer.WriteLine("]");
        writer.WriteLine(">>");
        writer.EndObject();

        return parentTreeId;
    }

    private static string GetRoleString(StructureRole role)
    {
        return role switch
        {
            StructureRole.Document => "Document",
            StructureRole.Part => "Part",
            StructureRole.Section => "Sect",
            StructureRole.Paragraph => "P",
            StructureRole.Heading1 => "H1",
            StructureRole.Heading2 => "H2",
            StructureRole.Heading3 => "H3",
            StructureRole.Heading4 => "H4",
            StructureRole.Heading5 => "H5",
            StructureRole.Heading6 => "H6",
            StructureRole.Heading => "H",
            StructureRole.List => "L",
            StructureRole.ListItem => "LI",
            StructureRole.ListLabel => "Lbl",
            StructureRole.ListBody => "LBody",
            StructureRole.Table => "Table",
            StructureRole.TableRow => "TR",
            StructureRole.TableHeader => "TH",
            StructureRole.TableData => "TD",
            StructureRole.TableBody => "TBody",
            StructureRole.TableHead => "THead",
            StructureRole.TableFoot => "TFoot",
            StructureRole.Figure => "Figure",
            StructureRole.Formula => "Formula",
            StructureRole.Form => "Form",
            StructureRole.Link => "Link",
            StructureRole.Annotation => "Annot",
            StructureRole.Quote => "BlockQuote",
            StructureRole.Note => "Note",
            StructureRole.Reference => "Reference",
            StructureRole.BibliographyEntry => "BibEntry",
            StructureRole.Code => "Code",
            StructureRole.Span => "Span",
            StructureRole.Division => "Div",
            _ => "P" // Default to paragraph
        };
    }
}

/// <summary>
/// Represents a single element in the PDF structure tree.
/// </summary>
internal sealed class StructureElement
{
    /// <summary>
    /// Gets or sets the PDF object ID for this structure element.
    /// </summary>
    public int ObjectId { get; set; }

    /// <summary>
    /// Gets or sets the role (structure type) of this element.
    /// </summary>
    public StructureRole Role { get; set; }

    /// <summary>
    /// Gets or sets the parent structure element (null for root elements).
    /// </summary>
    public StructureElement? Parent { get; set; }

    /// <summary>
    /// Gets the child structure elements.
    /// </summary>
    public List<StructureElement> Children { get; } = new();

    /// <summary>
    /// Gets the marked content IDs (MCIDs) associated with this element.
    /// </summary>
    public List<int> MarkedContentIds { get; } = new();

    /// <summary>
    /// Gets or sets the language for this element (e.g., "en-US", "fr-FR").
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the alternative text description (for images, figures, etc.).
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Gets or sets the actual text (replacement text for abbreviations, etc.).
    /// </summary>
    public string? ActualText { get; set; }
}

/// <summary>
/// Represents a reference to marked content in a PDF page content stream.
/// </summary>
internal sealed class MarkedContentReference
{
    /// <summary>
    /// Gets or sets the MCID (Marked Content Identifier) used in the PDF content stream.
    /// </summary>
    public int Mcid { get; set; }

    /// <summary>
    /// Gets or sets the structure element this marked content belongs to.
    /// </summary>
    public required StructureElement Element { get; set; }
}

/// <summary>
/// Standard PDF structure roles (element types).
/// Based on PDF 1.7 specification, Table 10.20 (Standard structure types).
/// </summary>
internal enum StructureRole
{
    // Grouping elements
    Document,
    Part,
    Section,
    Division,

    // Paragraphs and headings
    Paragraph,
    Heading,
    Heading1,
    Heading2,
    Heading3,
    Heading4,
    Heading5,
    Heading6,

    // Lists
    List,
    ListItem,
    ListLabel,
    ListBody,

    // Tables
    Table,
    TableRow,
    TableHeader,
    TableData,
    TableBody,
    TableHead,
    TableFoot,

    // Inline elements
    Span,
    Quote,
    Note,
    Reference,
    BibliographyEntry,
    Code,
    Link,

    // Special elements
    Figure,
    Formula,
    Form,
    Annotation
}
