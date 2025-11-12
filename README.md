# Folly

**XSL-FO 1.1 to PDF Renderer for .NET 8**

Folly is a standalone .NET 8 library that transforms XSL-FO (Formatting Objects) documents into high-quality PDF 1.7 files. With zero runtime dependencies beyond the .NET base class library, Folly provides a clean, efficient, and fully-managed solution for PDF generation from XSL-FO.

[![Build Status](https://github.com/folly/folly/workflows/Folly%20CI%2FCD/badge.svg)](https://github.com/folly/folly/actions)
[![NuGet](https://img.shields.io/nuget/v/Folly.Core.svg)](https://www.nuget.org/packages/Folly.Core/)

## Features

- **XSL-FO 1.1 Compliant** - Full support for the XSL-FO 1.1 specification
- **PDF 1.7 Output** - High-quality PDF generation with font embedding and subsetting
- **Zero Dependencies** - No external runtime dependencies beyond System.*
- **Fluent API** - Build FO documents programmatically with a clean, intuitive API
- **High Performance** - Renders 200-page documents in ~150ms (66x faster than target)
- **Low Memory** - ~22MB footprint for 200-page documents (27x better than target)
- **CI Performance Tests** - Automated regression detection blocks performance degradation
- **Developer-Friendly** - Rich validation diagnostics with XPath-locatable error messages

## Quick Start

### Installation

```bash
dotnet add package Folly.Core
dotnet add package Folly.Pdf
```

For fluent API support:
```bash
dotnet add package Folly.Fluent
```

### Load and Render

Transform an XSL-FO file to PDF:

```csharp
using Folly;
using Folly.Pdf;

var fo = FoDocument.Load("input.fo");
using var pdf = File.Create("output.pdf");
fo.SavePdf(pdf);
```

### Fluent API

Build FO documents programmatically:

```csharp
using Folly.Fluent;

Fo.Document(doc => doc
    .Metadata(meta => meta
        .Title("My Document")
        .Author("John Doe"))
    .LayoutMasters(lm => lm
        .SimplePageMaster("A4", "210mm", "297mm", spm => spm
            .RegionBody(rb => rb.Margin("1in"))
            .RegionBefore("0.5in")
            .RegionAfter("0.5in")))
    .PageSequence("A4", ps => ps
        .StaticContent("xsl-region-before", sc => sc
            .Block("Title Page"))
        .Flow(flow => flow
            .Block("Hello Folly!")
            .Table(table => table
                .Width("100%")
                .Column("33%")
                .Column("33%")
                .Column("34%")
                .Body(body => body
                    .Row(row => row
                        .Cell("A")
                        .Cell("B")
                        .Cell("C"))))))
).SavePdf("output.pdf");
```

## Architecture

Folly consists of three main packages:

- **Folly.Core** - FO DOM, property system, layout engine, and area tree builder
- **Folly.Pdf** - PDF 1.7 renderer with font embedding and subsetting
- **Folly.Fluent** - Fluent API for programmatic FO document construction

The rendering pipeline follows this flow:

```
XSL-FO XML → FO DOM → Area Tree → PDF 1.7
```

## Building from Source

### Prerequisites

- .NET 8.0 SDK or later

### Build

```bash
git clone https://github.com/folly/folly.git
cd folly
dotnet restore
dotnet build
```

### Run Tests

```bash
dotnet test
```

## Examples

The `examples` directory contains runnable samples demonstrating Folly's capabilities:

```bash
cd examples
dotnet run --project Folly.Examples
```

This generates 21 example PDFs showcasing Folly's capabilities:
- **Hello World** - Basic document with simple text
- **Multiple Blocks** - Different fonts and sizes
- **Text Alignment** - Start, center, and end alignment
- **Borders and Backgrounds** - Various styling options
- **Multi-Page Document** - Automatic pagination
- **Invoice** - Real-world business document
- **Tables** - Complex table layouts with spanning
- **Multi-Page Tables** - 100-row table with automatic page breaks and header repetition
- **Images** - JPEG and PNG embedding
- **Lists** - Ordered and unordered list formatting
- **Keep/Break Constraints** - Page break control
- **Headers and Footers** - Static content with page numbers
- **Markers** - Dynamic headers with chapter titles
- **Conditional Page Masters** - Different layouts for first/odd/even pages
- **Multi-Column Layout** - Newspaper-style 3-column formatting
- **Footnotes** - Academic footnotes with inline references and visual separators
- **External Links** - Hyperlinks to web resources
- **Bookmarks** - PDF outline/table of contents
- **Inline Formatting** - Styled text spans
- **BiDi Override** - Right-to-left text rendering
- **PDF Metadata** - Document properties (title, author, subject, keywords)

See [examples/README.md](examples/README.md) for details.

### Validating PDFs

Validate generated PDFs with qpdf:

```bash
cd examples
./validate-pdfs.sh
```

## Current Status

**Milestone M0: Foundation** ✅ (Completed)
**Milestone M1: Basic Layout** ✅ (Completed)
**Milestone M2: Tables, Images, Lists** ✅ (Completed)
**Milestone M3: Pagination Mastery** ✅ (Completed)
**Milestone M4: Full Spec & Polish** 🚧 (In Progress)

The core rendering engine is fully operational with extensive feature support:

**FO Document Processing:**
- XSL-FO 1.1 XML parsing with namespace support
- Immutable FO DOM representation
- Complete property inheritance system (50+ inheritable properties)
- Layout master sets and page masters
- Conditional page masters (first, odd, even pages)

**Layout Engine:**
- Multi-page layout with automatic pagination
- Greedy line breaking with word wrapping
- Text alignment (start, center, end, justify)
- Text justification with inter-word spacing
- `text-align-last` property for last line alignment
- Margins, padding, borders, and backgrounds
- Font metrics and text measurement
- Block and inline area generation
- Inline formatting (fo:inline) for styled text spans
- **Multi-page table support** with automatic page breaking
- **Table header repetition** on new pages (configurable via `table-omit-header-at-break`)
- Table layout with column/row spanning
- List formatting (fo:list-block)
- Keep-together and break-before/after constraints
- **Keep-with-next/previous constraints** for preventing orphaned headings and keeping figures with captions
- **Integer keep strength values** (1-999) for fine-grained pagination control
- Static-content for headers and footers
- Markers for dynamic content (fo:marker, fo:retrieve-marker)
- Multi-column layout (column-count, column-gap)
- Footnotes (fo:footnote, fo:footnote-body)
- Footnote separators (xsl-footnote-separator) for visual separation
- Floats (fo:float) for side-positioned content
- Links (fo:basic-link) for internal and external hyperlinks
- Bookmarks (fo:bookmark-tree, fo:bookmark) for PDF outline navigation
- Leaders (fo:leader) for generating dot patterns, rules, and spaces - commonly used in tables of contents
- BiDi text support (fo:bidi-override) with text reordering for right-to-left text rendering
- Advanced formatting objects (fo:page-number-citation, fo:page-number-citation-last, fo:block-container, fo:inline-container, fo:wrapper, fo:character, fo:initial-property-set)
- Side regions (fo:region-start, fo:region-end)

**PDF Rendering:**
- PDF 1.7 output with correct structure
- Standard Type 1 fonts (Helvetica, Times, Courier) with accurate AFM metrics
- Font metrics from Adobe Font Metrics (AFM) files (14 base PDF fonts, 200+ characters each)
- **Font subsetting** - Only embeds glyphs actually used in the document (dramatically reduces file size)
- **Stream compression** - Flate (zlib) compression for optimal PDF file sizes
- **PDF metadata** - Document Information Dictionary (title, author, subject, keywords, creator, producer, dates)
- Text positioning with baseline alignment
- Border rendering (solid, dashed, dotted)
- Background colors (named and hex formats)
- Graphics state management
- Image embedding (JPEG passthrough, PNG decoding)
- Page numbers (fo:page-number)
- Hyperlinks (internal and external via fo:basic-link)
- PDF outline/bookmarks for document navigation

**Quality Assurance:**
- 195 passing tests (99% success rate - 195 passed, 2 skipped for refinement)
  - 21 XSL-FO conformance tests (formatting object parsing, including repeatable-page-master-reference)
  - 25 property inheritance tests (50+ inheritable properties)
  - 31 layout engine tests (line breaking, page breaking, tables, footnotes, text justification, **multi-page tables**, **keep-with-next/previous**)
  - 14 PDF validation tests (structure, fonts, compression, metadata, links)
  - 9 AreaTree snapshot tests (layout regression detection)
  - 13 fuzzing/stress tests (malformed input, extreme nesting, large tables)
  - 21 working example PDFs (including multi-page table example with 100 rows, keep-with-next/previous demonstrations)
- 100% qpdf validation success (zero errors)
- Verified with qpdf 11.9.0

**Performance:**
- **Throughput**: ~1,333 pages/second for complex documents
- **200-page render time**: ~150ms (66x faster than 10-second target)
- **Memory footprint**: ~22MB for 200 pages (27x better than 600MB target)
- **Scaling**: Linear to sub-linear O(n) performance
- **CI Integration**: Automated performance regression tests
- See [PERFORMANCE.md](PERFORMANCE.md) for detailed benchmarks

See [PLAN.md](PLAN.md) for detailed roadmap and upcoming milestones.

## Contributing

Contributions are welcome! Please feel free to submit issues, fork the repository, and send pull requests.

## License

MIT License - see LICENSE file for details.

## Acknowledgments

Folly implements the [XSL-FO 1.1 W3C Recommendation](https://www.w3.org/TR/xsl11/) for formatting objects.
