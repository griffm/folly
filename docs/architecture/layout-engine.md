# Layout Engine Architecture

The Folly layout engine transforms the FO DOM into an area tree representation suitable for PDF rendering.

## Overview

The layout engine is responsible for:

- **Text measurement** - Calculating character and word widths using font metrics
- **Line breaking** - Breaking paragraphs into lines using greedy or Knuth-Plass algorithms
- **Hyphenation** - Breaking words using Liang's TeX hyphenation algorithm
- **Page breaking** - Distributing content across pages with keep/break constraints
- **BiDi text** - Reordering RTL text using the Unicode BiDi algorithm (UAX#9)
- **Table layout** - Multi-page tables with header/footer repetition
- **List formatting** - Automatic numbering and indentation

## Layout Pipeline

```
FO DOM
  ↓
Property Resolution
  ↓
Area Tree Generation
  ├─ Page Layout
  ├─ Block Layout
  ├─ Line Breaking
  ├─ Table Layout
  └─ List Layout
  ↓
Area Tree (ready for PDF rendering)
```

## Key Algorithms

### Line Breaking

**Greedy Algorithm** (default):
- First-fit approach: break at first acceptable breakpoint
- Fast: O(n) where n is number of words
- Good quality for most documents
- Excellent performance

**Knuth-Plass Algorithm** (opt-in):
- TeX-quality optimal line breaking
- Minimizes total "badness" across entire paragraph
- Considers stretch and shrink of spaces
- O(n²) complexity but produces superior typography
- Slower than greedy but still performant

### Hyphenation (Liang's Algorithm)

Implemented using source generators with embedded pattern files:

1. Patterns loaded at compile time (zero runtime overhead)
2. Apply patterns to words to find valid breakpoints
3. Respect minimum word length and left/right character constraints
4. Support for multiple languages: English, German, French, Spanish

Pattern compilation ensures fast runtime performance.

### BiDi Text (UAX#9)

Full implementation of Unicode Bidirectional Algorithm:

1. Resolve character types (L, R, AL, EN, etc.)
2. Resolve explicit levels
3. Resolve weak types
4. Resolve neutral types
5. Resolve implicit levels
6. Reorder text by levels

Handles complex mixed LTR/RTL text with proper number and punctuation handling.

## Area Tree Structure

The area tree is an intermediate representation between FO and PDF:

```
PageViewport
├─ BlockArea (region-body)
│  ├─ BlockArea (paragraph)
│  │  ├─ LineArea
│  │  │  └─ InlineArea (text)
│  │  └─ LineArea
│  │     └─ InlineArea (text)
│  └─ TableArea
│     ├─ TableRowArea
│     │  └─ TableCellArea
│     │     └─ BlockArea
│     └─ TableRowArea
│        └─ TableCellArea
│           └─ BlockArea
├─ BlockArea (region-before - header)
└─ BlockArea (region-after - footer)
```

## Implementation Details

For detailed algorithm descriptions and code organization, see the source code in `src/Folly.Core/Layout/`.

## Performance Characteristics

The layout engine is optimized for high throughput with the following relative cost distribution:
- Line breaking and text measurement: Significant portion of layout time
- Page breaking and pagination: Substantial portion of layout time
- Table and list layout: Moderate portion of layout time
- Property resolution: Smaller portion of layout time

See [Performance Guide](../guides/performance.md) for performance analysis.
