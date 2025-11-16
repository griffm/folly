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

### 1. Folly.Core

The core library containing the FO DOM model, property system, and layout engine.

**Key Components:**
- **FO DOM** - Immutable representation of XSL-FO document structure
- **Property System** - Property inheritance and resolution (50+ inheritable properties)
- **Layout Engine** - Multi-pass layout with line breaking, page breaking, and area tree generation
- **Text Processing** - Hyphenation (Liang algorithm), BiDi support (UAX#9), line breaking (Greedy/Knuth-Plass)

### 2. Folly.Pdf

PDF generation and rendering components.

**Key Components:**
- **PDF Renderer** - Converts area tree to PDF content streams
- **PDF Writer** - Manages PDF object serialization and file structure
- **Font System** - TrueType/OpenType parsing, subsetting, and embedding
- **Image System** - JPEG, PNG, BMP, GIF, TIFF decoding and embedding
- **SVG System** - Complete SVG parsing and PDF conversion

### 3. Folly.Fluent

Fluent API for programmatic FO document construction.

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

- **Throughput**: ~1,333 pages/second for complex documents
- **Memory**: ~22MB for 200-page documents
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
│   ├── Folly.Core/           # FO DOM, layout engine, area tree
│   │   ├── Dom/              # FO element classes
│   │   ├── Layout/           # Layout engine and algorithms
│   │   ├── Properties/       # Property system
│   │   └── Fonts/            # Font abstractions
│   ├── Folly.Pdf/            # PDF rendering and generation
│   │   ├── PdfRenderer.cs    # Area tree → PDF conversion
│   │   ├── PdfWriter.cs      # PDF file structure
│   │   └── Svg/              # SVG subsystem
│   ├── Folly.Fonts/          # Font parsing and embedding
│   │   ├── TrueType/         # TrueType parser
│   │   └── Tables/           # Font table parsers
│   └── Folly.Fluent/         # Fluent API
├── tests/
│   ├── Folly.Tests/          # Unit tests
│   └── Folly.Benchmarks/     # Performance benchmarks
└── examples/
    └── Folly.Examples/       # Runnable examples
```

## Further Reading

- [Font System Architecture](font-system.md) - Font parsing, subsetting, and embedding
- [Layout Engine](layout-engine.md) - Text layout and pagination algorithms
- [SVG Support](svg-support.md) - SVG parsing and PDF conversion
- [PDF Generation](pdf-generation.md) - PDF structure and rendering details
