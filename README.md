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

## Current Status

**Milestone M0: Foundation** ✅ (Completed)

The foundational structure is in place with:
- Solution structure and build infrastructure
- Core API skeletons (FoDocument, AreaTree, PdfRenderer)
- CI/CD pipeline with GitHub Actions
- Nerdbank.GitVersioning for semantic versioning
- Initial test coverage

See [PLAN.md](PLAN.md) for detailed roadmap and upcoming milestones.

## Contributing

Contributions are welcome! Please feel free to submit issues, fork the repository, and send pull requests.

## License

MIT License - see LICENSE file for details.

## Acknowledgments

Folly implements the [XSL-FO 1.1 W3C Recommendation](https://www.w3.org/TR/xsl11/) for formatting objects.
