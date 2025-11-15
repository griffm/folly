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

This generates 33 example PDFs showcasing Folly's capabilities:
- **Hello World** - Basic document with simple text
- **Multiple Blocks** - Different fonts and sizes
- **Text Alignment** - Start, center, and end alignment
- **Borders and Backgrounds** - Various styling options
- **Multi-Page Document** - Automatic pagination
- **Invoice** - Real-world business document
- **Tables** - Complex table layouts with spanning
- **Multi-Page Tables** - 100-row table with automatic page breaks and header repetition
- **Images** - JPEG, PNG, BMP, GIF, and TIFF embedding
- **Lists** - Ordered and unordered list formatting
- **Multi-Page Lists** - 100-item list with automatic page breaks and keep-together support
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
- **Emergency Line Breaking** - Character-level breaking for overflow words and wrap-option control
- **TrueType Fonts** - Custom font embedding with subsetting
- **Font Fallback** - System font discovery and fallback chains
- **Kerning** - Professional typography with automatic kerning
- **Table Row Spanning** - Complex table layouts with merged cells
- **Proportional Column Widths** - Flexible, responsive table layouts
- **Content-Based Column Sizing** - Auto columns sized to content
- **Table Footer Repetition** - Footers at page breaks
- **Business Letterhead** - Absolute positioning demonstration
- **Sidebars with Margin Notes** - Left and right side regions for annotations and supplementary content
- **All Image Formats** - Comprehensive demonstration of BMP, GIF, and TIFF support with zero dependencies
- **Rounded Corners** - Modern border-radius support with Bezier curves
- **Unicode BiDi (RTL Languages)** - Full UAX#9 implementation demonstrating Arabic, Hebrew, and mixed LTR/RTL content

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
- **Line breaking algorithms:** Greedy (fast, default) and Knuth-Plass (optimal, TeX-quality)
- **Professional hyphenation** using Liang's TeX algorithm (4 languages: English, German, French, Spanish)
- **Emergency line breaking** with character-level breaking for overflow words
- **wrap-option property** (wrap, no-wrap) for controlling line wrapping behavior
- Text alignment (start, center, end, justify)
- Text justification with inter-word spacing
- `text-align-last` property for last line alignment
- Margins, padding, borders, and backgrounds
- Font metrics and text measurement
- Block and inline area generation
- Inline formatting (fo:inline) for styled text spans
- **Multi-page table support** with automatic page breaking
- **Table header repetition** on new pages (configurable via `table-omit-header-at-break`)
- **Table footer repetition** at page breaks (configurable via `table-omit-footer-at-break`)
- **Table row spanning** (number-rows-spanned) with cell grid tracking for complex layouts
- **Table column spanning** (number-columns-spanned) for merged cells
- **Proportional column widths** (proportional-column-width()) for flexible, responsive table layouts
- **Content-based column sizing** - Auto columns intelligently size based on content width for optimal readability
- List formatting (fo:list-block)
- **Multi-page list support** with automatic page breaking between items
- **keep-together support on list items** to prevent breaking across pages
- Keep-together and break-before/after constraints
- **Keep-with-next/previous constraints** for preventing orphaned headings and keeping figures with captions
- **Integer keep strength values** (1-999) for fine-grained pagination control
- **Widow/orphan control** for professional typography (configurable via `widows` and `orphans` properties)
- Static-content for headers and footers
- Markers for dynamic content (fo:marker, fo:retrieve-marker)
- Multi-column layout (column-count, column-gap)
- Footnotes (fo:footnote, fo:footnote-body)
- Footnote separators (xsl-footnote-separator) for visual separation
- Floats (fo:float) for side-positioned content
- Links (fo:basic-link) for internal and external hyperlinks
- Bookmarks (fo:bookmark-tree, fo:bookmark) for PDF outline navigation
- Leaders (fo:leader) for generating dot patterns, rules, and spaces - commonly used in tables of contents
- **Full Unicode BiDi Algorithm (UAX#9)** - Complete implementation for Arabic, Hebrew, and mixed LTR/RTL text with proper handling of numbers, punctuation, and weak/neutral types
- Advanced formatting objects (fo:page-number-citation, fo:page-number-citation-last, fo:block-container, fo:inline-container, fo:wrapper, fo:character, fo:initial-property-set)
- **Side regions** (fo:region-start, fo:region-end) for left and right sidebars - ideal for margin notes, glossaries, and supplementary content
- **Absolute positioning** (fo:block-container with absolute-position) for letterheads, watermarks, and complex forms
- **Z-index layering** for controlling stacking order of absolutely positioned elements
- **Background images** (background-image, background-repeat, background-position) with support for tiling, positioning, and security validation
- **Reference orientation** (reference-orientation) for rotating block containers (0°, 90°, 180°, 270°) using PDF transformation matrices
- **Display-align** (display-align) for vertical alignment of content (center, after/bottom) within block containers

**PDF Rendering:**
- PDF 1.7 output with correct structure
- Standard Type 1 fonts (Helvetica, Times, Courier) with accurate AFM metrics
- Font metrics from Adobe Font Metrics (AFM) files (14 base PDF fonts, 200+ characters each)
- **TrueType/OpenType font support** - Embed custom fonts directly in PDFs
- **Font subsetting** - Only embeds glyphs actually used in the document (dramatically reduces file size)
- **Font fallback & system fonts** - Automatic font resolution with font-family stacks (e.g., "Roboto, Arial, sans-serif")
- **System font discovery** - Cross-platform support for Windows, macOS, and Linux system fonts
- **Generic font families** - Automatic mapping of serif, sans-serif, and monospace to system fonts
- **Kerning support** - Automatic kerning for TrueType fonts using PDF TJ operator for professional typography
- **Stream compression** - Flate (zlib) compression for optimal PDF file sizes
- **PDF metadata** - Document Information Dictionary (title, author, subject, keywords, creator, producer, dates)
- Text positioning with baseline alignment
- Border rendering (solid, dashed, dotted)
- Background colors (named and hex formats)
- **Background image rendering** with tiling (repeat, repeat-x, repeat-y, no-repeat) and positioning (keywords, percentages, lengths)
- **Rounded corners** (border-radius) - Smooth, rounded borders using Bezier curves for modern, polished design
  - Uniform radius for all corners (border-radius)
  - Individual corner control (border-top-left-radius, border-top-right-radius, border-bottom-right-radius, border-bottom-left-radius)
  - Works with solid, dashed, and dotted border styles
  - Automatic radius clamping to prevent overlap
  - Full support for absolutely positioned elements
- Graphics state management
- **Rotation transformations** using PDF transformation matrices for rotated content (0°, 90°, 180°, 270°)
- **Image embedding** with zero dependencies
  - JPEG (DCTDecode passthrough for optimal file size)
  - PNG (FlateDecode with PNG predictors, alpha channel support via SMask)
  - **BMP** (24-bit and 32-bit RGB with alpha, DPI detection, BGR→RGB conversion)
  - **GIF** (custom LZW decompression, global/local color tables, transparency, indexed color spaces)
  - **TIFF** (baseline uncompressed RGB, little/big-endian, IFD parsing, DPI extraction)
- Page numbers (fo:page-number)
- Hyperlinks (internal and external via fo:basic-link)
- PDF outline/bookmarks for document navigation

**Quality Assurance:**
- Comprehensive test suite with high success rate
  - XSL-FO conformance tests (formatting object parsing, repeatable-page-master-reference)
  - Property inheritance tests (comprehensive coverage of inheritable properties)
  - Layout engine tests (line breaking, page breaking, tables, footnotes, text justification, **multi-page tables**, **table row spanning**, **proportional column widths**, **content-based column sizing**, **footer repetition**, **multi-page lists**, **keep-with-next/previous**, **widow/orphan control**, **emergency line breaking**, **absolute positioning**)
  - **Hyphenation tests** (Liang's algorithm, multi-language support, configurable constraints)
  - **Emergency line breaking tests** (character-level breaking, wrap-option support, narrow columns)
  - **Knuth-Plass line breaking tests** (optimal line breaking, TeX-quality typography, comparison with greedy)
  - **List page breaking tests** (multi-page lists, keep-together support, nested content)
  - **Font tests** (TrueType parsing, font subsetting, serialization, PDF embedding, ToUnicode CMaps, **font fallback**, **system font discovery**, **kerning support**)
  - **BiDi tests** (UAX#9 algorithm, Hebrew, Arabic, mixed LTR/RTL, numbers, punctuation, character types, embedding levels)
  - PDF validation tests (structure, fonts, compression, metadata, links)
  - AreaTree snapshot tests (layout regression detection)
  - Fuzzing/stress tests (malformed input, extreme nesting, large tables)
  - Working example PDFs (**TrueType font embedding**, **font fallback & system fonts**, **kerning demonstration**, **table row spanning**, **proportional column widths**, **content-based column sizing**, **footer repetition**, **absolute positioning letterhead**, **rounded corners**, **Unicode BiDi (Arabic/Hebrew)**, multi-page tables, multi-page lists, keep-with-next/previous, emergency line breaking)
- qpdf validation success (zero errors)
- Verified with qpdf

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
