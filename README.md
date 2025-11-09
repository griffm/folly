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
- **Performance** - Target: 200-page documents in under 10 seconds
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
using static Folly.Fluent.Fo;

var doc = Document(d => d
    .LayoutMasters(lm => lm
        .SimplePageMaster("A4", a => a.PageSize(595, 842)
            .Margins(36).RegionBody().RegionBefore(36).RegionAfter(36)))
    .PageSequence("A4", ps => ps
        .StaticContent(Region.Before, s => s.Block("Title Page"))
        .Flow("xsl-region-body", f => f
            .Block("Hello Folly!")
            .Table(t => t.Columns(3, 200, 200, 195)
                .Body(body => body
                    .Row(r => r
                        .Cell("A").Cell("B").Cell("C")))))))
.SavePdf("output.pdf");
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

This generates 14 example PDFs showcasing Folly's capabilities:
- **Hello World** - Basic document with simple text
- **Multiple Blocks** - Different fonts and sizes
- **Text Alignment** - Start, center, and end alignment
- **Borders and Backgrounds** - Various styling options
- **Multi-Page Document** - Automatic pagination
- **Invoice** - Real-world business document
- **Tables** - Complex table layouts with spanning
- **Images** - JPEG and PNG embedding
- **Lists** - Ordered and unordered list formatting
- **Keep/Break Constraints** - Page break control
- **Headers and Footers** - Static content with page numbers
- **Markers** - Dynamic headers with chapter titles
- **Conditional Page Masters** - Different layouts for first/odd/even pages
- **Multi-Column Layout** - Newspaper-style 3-column formatting

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
**Milestone M3: Pagination Mastery** 🔄 (In Progress - 75% complete)

The core rendering engine is fully operational with extensive feature support:

**FO Document Processing:**
- XSL-FO 1.1 XML parsing with namespace support
- Immutable FO DOM representation
- Property inheritance and resolution
- Layout master sets and page masters
- Conditional page masters (first, odd, even pages)

**Layout Engine:**
- Multi-page layout with automatic pagination
- Greedy line breaking with word wrapping
- Text alignment (start, center, end)
- Margins, padding, borders, and backgrounds
- Font metrics and text measurement
- Block and inline area generation
- Table layout with column/row spanning
- List formatting (fo:list-block)
- Keep-together and break-before/after constraints
- Static-content for headers and footers
- Markers for dynamic content (fo:marker, fo:retrieve-marker)
- Multi-column layout (column-count, column-gap)

**PDF Rendering:**
- PDF 1.7 output with correct structure
- Standard Type 1 fonts (Helvetica, Times, Courier)
- Text positioning with baseline alignment
- Border rendering (solid, dashed, dotted)
- Background colors (named and hex formats)
- Graphics state management
- Image embedding (JPEG passthrough, PNG decoding)
- Page numbers (fo:page-number)

**Quality Assurance:**
- 11 passing unit tests (100% success rate)
- 14 working example PDFs
- 100% qpdf validation success (zero errors)
- Verified with qpdf 11.9.0

See [PLAN.md](PLAN.md) for detailed roadmap and upcoming milestones.

## Contributing

Contributions are welcome! Please feel free to submit issues, fork the repository, and send pull requests.

## License

MIT License - see LICENSE file for details.

## Acknowledgments

Folly implements the [XSL-FO 1.1 W3C Recommendation](https://www.w3.org/TR/xsl11/) for formatting objects.
