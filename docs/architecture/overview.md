# Folly Architecture Overview

Folly is a standalone .NET 8 library that transforms XSL-FO (Formatting Objects) documents into high-quality PDF 1.7 files with zero runtime dependencies beyond the .NET base class library.

## System Architecture

```
┌────────────────────────────────────────────────────────────┐
│                   Folly Architecture                        │
└────────────────────────────────────────────────────────────┘

┌──────────────┐
│   XSL-FO     │
│   Document   │
└──────┬───────┘
       │
       ▼
┌──────────────┐    ┌─────────────────────────────────────┐
│  FO Parser   │───▶│         FO DOM Model                │
│              │    │  - Element hierarchy                 │
└──────────────┘    │  - Property inheritance             │
                    │  - Layout masters                    │
                    └──────────┬──────────────────────────┘
                               │
                               ▼
                    ┌──────────────────────────────────────┐
                    │       Layout Engine                   │
                    │  - Line breaking (Greedy/Knuth-Plass)│
                    │  - Hyphenation (Liang algorithm)     │
                    │  - Page breaking                      │
                    │  - Table layout                       │
                    │  - BiDi support (UAX#9)              │
                    └──────────┬───────────────────────────┘
                               │
                               ▼
                    ┌──────────────────────────────────────┐
                    │         Area Tree                     │
                    │  - Page areas                         │
                    │  - Block areas                        │
                    │  - Line areas                         │
                    │  - Inline areas                       │
                    └──────────┬───────────────────────────┘
                               │
                               ▼
                    ┌──────────────────────────────────────┐
                    │       PDF Renderer                    │
                    │  - Content stream generation         │
                    │  - Font embedding & subsetting       │
                    │  - Image embedding                    │
                    │  - SVG conversion                     │
                    └──────────┬───────────────────────────┘
                               │
                               ▼
                    ┌──────────────────────────────────────┐
                    │        PDF Writer                     │
                    │  - Object serialization              │
                    │  - Stream compression                │
                    │  - Cross-reference table             │
                    │  - PDF structure                      │
                    └──────────┬───────────────────────────┘
                               │
                               ▼
                    ┌──────────────────────────────────────┐
                    │        PDF 1.7 Document              │
                    └──────────────────────────────────────┘
```

## Core Components

Folly is designed as a modular suite of composable libraries:

### Tier 1: Foundation Libraries (Zero Dependencies)

**Folly.Typography** - Text layout primitives:
- Unicode BiDi (UAX#9)
- Hyphenation (Liang's algorithm)
- Optimal line breaking (Knuth-Plass)

**Folly.Images** - Image format parsers:
- JPEG, PNG, BMP, GIF, TIFF
- DPI extraction and ICC profiles

**Folly.Svg** - SVG 1.1 parser:
- Complete SVG support with CSS
- PDF rendering backend

**Folly.Fonts** - Font parsing and embedding:
- TrueType/OpenType parsing
- Font subsetting and embedding
- System font discovery

### Tier 2: Layout Abstraction

**Folly.Layout** - Format-agnostic layout model:
- Area Tree intermediate representation
- Block, inline, table, and line areas
- Generic layout primitives

### Tier 3: Format-Specific Libraries

**Folly.Xslfo.Model** - XSL-FO document model:
- FO DOM with property inheritance
- XML parser
- Property system

**Folly.Xslfo.Layout** - XSL-FO layout engine:
- Converts FO DOM to Area Tree
- Tables, lists, footnotes, markers
- Multi-column layout

**Folly.Pdf.Core** - PDF generation:
- PDF 1.7 writer
- Font and image embedding
- Content stream generation

### Tier 4: Composition

**Folly.Core** - High-level API:
- Orchestrates all components
- FoDocument API
- Extension methods

**Folly.Fluent** - Fluent API:
- Programmatic FO document construction

## Data Flow

### 1. Parsing Phase

```csharp
XSL-FO XML → FO DOM → Property Resolution
```

- XML is parsed into an immutable FO DOM tree
- Properties are inherited and resolved according to XSL-FO 1.1 specification
- Layout masters and page sequences are extracted

### 2. Layout Phase

```csharp
FO DOM → Layout Engine → Area Tree
```

- Text is measured using font metrics
- Lines are broken using greedy or Knuth-Plass algorithms
- Hyphenation is applied where appropriate
- BiDi reordering is performed for RTL text
- Pages are created with automatic pagination
- Tables, lists, and other structures are laid out

### 3. Rendering Phase

```csharp
Area Tree → PDF Renderer → PDF 1.7
```

- Areas are converted to PDF content streams
- Fonts are embedded and subset
- Images are decoded and embedded
- SVG is converted to PDF graphics operators
- PDF structure is serialized with compression

## Key Design Principles

### Zero Dependencies

Folly has **no runtime dependencies** beyond the .NET 8 base class library. All functionality is implemented in-house:

- TrueType/OpenType font parsing
- Image decoding (JPEG, PNG, BMP, GIF, TIFF)
- SVG parsing and rendering
- Compression (Flate/zlib)
- Hyphenation pattern processing

### Performance First

- **Excellent throughput** for complex documents
- **Minimal memory footprint** for large documents
- **Scaling**: Linear to sub-linear O(n) performance
- Text width caching for repeated measurements
- Lazy evaluation where possible
- Efficient binary serialization

### Immutability

- FO DOM elements are immutable
- Properties are computed once and cached
- Encourages correct, predictable behavior
- Simplifies concurrent processing

### Extensibility

- Clean separation of concerns
- Interface-based abstractions (IFontProvider, IFont)
- Provider pattern for fonts
- Pluggable line breaking algorithms
- Configurable layout options

## Package Structure

```
Folly/
├── src/
│   ├── Folly.Typography/         # Text layout primitives (BiDi, hyphenation, line breaking)
│   ├── Folly.Images/             # Image format parsers (JPEG, PNG, BMP, GIF, TIFF)
│   ├── Folly.Svg/                # SVG 1.1 parser and rendering
│   ├── Folly.Fonts/              # Font parsing, subsetting, and embedding
│   ├── Folly.Layout/             # Format-agnostic Area Tree model
│   ├── Folly.Xslfo.Model/        # XSL-FO DOM and property system
│   ├── Folly.Xslfo.Layout/       # XSL-FO layout engine
│   ├── Folly.Pdf.Core/           # PDF 1.7 generation and rendering
│   ├── Folly.Core/               # High-level API and orchestration
│   ├── Folly.Fluent/             # Fluent API for document construction
│   └── Folly.SourceGenerators.*/ # Build-time code generation
├── tests/
│   ├── Folly.Typography.Tests/   # Typography unit tests
│   ├── Folly.Images.Tests/       # Image parser unit tests
│   ├── Folly.Svg.Tests/          # SVG parser unit tests
│   ├── Folly.UnitTests/          # Integration tests
│   ├── Folly.SpecTests/          # XSL-FO specification tests
│   └── Folly.Benchmarks/         # Performance benchmarks
└── examples/
    ├── Folly.Examples/           # Runnable XSL-FO examples
    └── svg-examples/             # SVG example files
```

## Further Reading

- [Font System](../../src/Folly.Fonts/README.md) - TrueType/OpenType parsing, subsetting, and embedding
- [Layout Engine](layout-engine.md) - Text layout and pagination algorithms
- [SVG Support](svg-support.md) - SVG parsing and PDF conversion
- [PDF Generation](pdf-generation.md) - PDF structure and rendering details
