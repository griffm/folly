# Folly Development Plan

This document outlines the detailed development plan, milestones, and technical specifications for the Folly XSL-FO processor.

## Objectives & Constraints

- **Name**: Folly — a standalone .NET 8 library
- **Purpose**: Fully compliant XSL-FO 1.1 processor that outputs PDF 1.7 only (no XPS/SVG in v1)
- **Platform**: .NET 8 (C#), managed code only
- **Dependencies**: Zero runtime dependencies beyond System.* (dev/test dependencies allowed)
- **Input**: .fo XML files and a fluent C# API (Folly.Fluent) to build FO documents in memory
- **Output**: PDF via internal renderer
- **CI/CD**: GitHub Actions with automatic build, test, and publish on merges to main
- **Versioning**: Nerdbank.GitVersioning

## Features

### Must-Have (v1.0)

#### XSL-FO 1.1 Pipeline
- Parse FO XML → immutable FO DOM with property resolution & validation
- Full layout engine:
  - Pagination with conditional page masters
  - Block/inline formatting model
  - Tables with complex column/row spanning
  - Footnotes and floats
  - Markers for running headers/footers
  - White-space handling and inheritance
  - Keeps and breaks
  - BiDi hooks for right-to-left text
- Build deterministic Area Tree

#### PDF Renderer (Only Backend)
- Produce PDF 1.7 with:
  - Font embedding/subsetting for TTF/OTF
  - Stable text placement with precise positioning
  - Links (internal and external)
  - Metadata and bookmarks
  - Images (JPEG passthrough, PNG decoding via built-in libraries)
  - Graphic primitives: borders, backgrounds, rounded corners, fills

#### Performance Targets
- 200-page typical mixed document in <10 seconds
- Memory footprint <600MB

#### Developer Ergonomics
- Rich validation diagnostics
- XPath-locatable error messages
- Clear exception messages with context

### Nice-to-Have (Future Releases)

- PDF/A compliance for archival
- Tagged PDF for accessibility
- Pluggable hyphenation dictionaries
- CLI tools for FO diffing and visual inspection
- Multi-threaded layout engine
- Streaming support for extreme workloads

## Repository Structure

```
/src
  /Folly.Core      # FO DOM, property system, layout, area tree
  /Folly.Pdf       # PDF renderer
  /Folly.Fluent    # Fluent builder API (optional reference)
/tests
  /Folly.SpecTests # XSL-FO 1.1 conformance tests
  /Folly.UnitTests # Component/unit tests
/build
  version.json
  Directory.Build.props
/.github
  /workflows
    folly-ci.yml
```

## Testing & Conformance Strategy

### Coverage Metrics
- FO-specific traceability matrix tied to XSL-FO 1.1 spec clauses
- Track coverage of each formatting object and property
- Measure which spec sections have test coverage

### PDF Validation
- Parse output PDF and validate structure
- Assert correct font subsets and embedding
- Verify resource usage (objects, streams, fonts)
- Check text placement accuracy
- Validate link destinations and metadata

### Golden AreaTree Snapshots
- Deterministic layout verification
- Store AreaTree as JSON for each test case
- Compare against golden snapshots to detect layout regressions

### Fuzzing & Stress Testing
- Malformed XML input handling
- Extreme nesting (deeply nested blocks/inlines)
- Table stress tests (large tables, complex spanning)
- Property inheritance chains
- Edge cases in keeps/breaks logic

### CI Requirements
- Block merge if test coverage decreases
- Block merge if conformance tests fail
- Block merge if performance regresses beyond threshold

## Versioning & CI/CD

### Nerdbank.GitVersioning

Configuration via `version.json`:

```json
{
  "version": "0.1.0",
  "publicReleaseRefSpec": [ "^refs/heads/main$" ]
}
```

Version format: `{major}.{minor}.{patch}+{commitsSinceVersionChange}`

### GitHub Actions Workflow

Triggered on:
- Pull requests to `main`
- Pushes to `main`

Pipeline steps:
1. Build (Release configuration)
2. Run unit tests with coverage
3. Run conformance tests
4. Upload coverage to Codecov
5. **On main branch only**:
   - Pack NuGet packages
   - Publish to NuGet.org

## Milestones

### M0: Foundation ✅ (1-2 weeks) - **COMPLETED**

**Goal**: Establish project structure, build pipeline, and core skeletons

**Deliverables**:
- [x] Repository structure with solution and projects
- [x] CI/CD pipeline with GitHub Actions
- [x] Nerdbank.GitVersioning configuration
- [x] Empty PDF writer skeleton
- [x] FO parser skeleton with Load methods
- [x] Core types: FoDocument, AreaTree, PdfRenderer, PdfWriter
- [x] Initial test infrastructure
- [x] Build succeeds with zero warnings
- [x] Tests pass (basic sanity checks)

### M1: Basic Layout ✅ (3-4 weeks) - **COMPLETED**

**Goal**: Implement fundamental block/inline model and simple pagination

**Deliverables**:
- [x] Block area generation
- [x] Inline area generation (text, inline containers)
- [x] Simple page master support
- [x] Single-column layout
- [x] Font metrics and text measurement
- [x] Basic text rendering to PDF
- [x] Line breaking algorithm (greedy word-based)
- [x] Margin/padding/border support (all border styles)
- [x] Background color rendering (named and hex)
- [x] Multi-page pagination with automatic page breaking
- [x] Text alignment (start, center, end)
- [x] **Output**: 6 validated example PDFs with formatted text

**Success Criteria**:
- ✅ Can render multi-page documents with text blocks
- ✅ Correct line breaking and page breaking
- ✅ Basic font rendering works (Helvetica, Times, Courier)
- ✅ All PDFs pass qpdf validation (100% success rate)
- ✅ 11 passing unit tests (100% success rate)

### M2: Tables, Images, Lists ✅ (4-6 weeks) - **COMPLETED**

**Goal**: Implement complex layout structures

**Deliverables**:
- [x] Full table layout algorithm
  - [x] Column width calculation
  - [x] Row height calculation
  - [x] Cell spanning (rowspan, colspan)
  - [x] Border collapse model
- [x] Image support
  - [x] JPEG passthrough
  - [x] PNG decoding and embedding
  - [x] Image scaling and positioning
- [x] List blocks (ordered, unordered)
- [x] Border rendering (all styles)
- [x] Background colors and images
- [x] Keep-together and keep-with-next/previous
- [x] Break-before and break-after

**Success Criteria**:
- ✅ Complex tables render correctly with all border styles
- ✅ Images embedded properly in PDF (JPEG and PNG)
- ✅ Keep/break constraints honored
- ✅ List blocks with label and body formatting

### M3: Pagination Mastery ✅ (3-4 weeks) - **COMPLETED**

**Goal**: Advanced pagination features

**Deliverables**:
- [x] Static-content for headers/footers
- [x] Region support (before, after, body)
- [x] Page number citations (fo:page-number)
- [x] Markers (fo:marker and fo:retrieve-marker)
- [x] Conditional page masters (page-sequence-master)
  - [x] Page position conditions (first, rest, any)
  - [x] Odd/even page conditions
  - [x] Repeatable page master alternatives
- [x] Multi-column layout
  - [x] Column-count property
  - [x] Column-gap property
  - [x] Content flow across columns
  - [x] Column balancing
- [x] Footnotes with footnote-body
- [x] Floats (side floats) - Basic implementation with start/end positioning
- [ ] Region support (start, end) - side regions (deferred to M4)
- [ ] Initial property inheritance refinement (deferred to M4)

**Success Criteria**:
- ✅ Running headers/footers work via static-content and markers
- ✅ Page numbers display correctly
- ✅ Conditional page masters switch correctly (first, odd, even)
- ✅ Multi-column layout functions properly with intelligent column flow
- ✅ Footnotes placed correctly at bottom of pages
- ✅ Floats positioned to left/right sides of content

### M4: Full Spec & Polish 🚧 (4-6 weeks) - **IN PROGRESS**

**Goal**: Complete XSL-FO 1.1 conformance and production readiness

**Deliverables**:
- [x] Complete property system with full inheritance - 50+ inheritable properties, parent-child relationships, computed values
- [x] Inline formatting (fo:inline) - text styling within blocks
- [x] All remaining formatting objects - Added fo:leader, fo:page-number-citation, fo:page-number-citation-last, fo:block-container, fo:inline-container, fo:wrapper, fo:character, fo:bidi-override, fo:initial-property-set, fo:region-start, fo:region-end
- [x] BiDi text support - fo:bidi-override fully implemented with layout engine support and text reordering for RTL direction
- [x] Leaders and page number formatting - fo:leader with dot patterns and rules fully implemented in layout engine and PDF renderer, fo:page-number-citation implemented
- [x] Bookmarks (PDF outline) - fo:bookmark-tree and fo:bookmark
- [x] Internal and external links - fo:basic-link with internal-destination and external-destination
- [x] AFM font metrics - Accurate character widths from Adobe Font Metrics files for all 14 standard PDF fonts (200+ characters each), replacing approximate metrics with real data for improved text layout precision
- [ ] Complete metadata support
- [x] PDF compression optimization - Flate (zlib) compression for PDF content streams, dramatically reducing file sizes
- [x] Font subsetting optimization - Only embeds the glyphs actually used in each document, further reducing file sizes
- [ ] Performance profiling and optimization
- [ ] Complete test suite with high coverage
- [ ] Documentation and samples
- [ ] **Publish 1.0.0 to NuGet**

**Success Criteria**:
- Pass XSL-FO 1.1 conformance test suite
- Meet performance targets (200 pages <10s, <600MB)
- Zero critical bugs
- API stable and documented
- Ready for production use

## Technical Architecture

### FO DOM

Immutable object model representing the XSL-FO tree:
- Property resolution and inheritance
- Validation against XSL-FO 1.1 spec
- XPath support for error reporting

### Layout Engine

Transforms FO DOM into Area Tree:
- **Refinement Phase**: Resolve all properties, expand shorthands
- **Layout Phase**: Generate areas with dimensions and positions
- **Line Breaking**: Knuth-Plass algorithm or similar
- **Page Breaking**: Optimal page breaks with keep/break constraints

### Area Tree

Intermediate representation between FO and PDF:
- Page viewports with regions
- Block areas with dimensions
- Line areas with inline areas
- Positioned areas (floats, absolutely positioned)

### PDF Renderer

Transforms Area Tree into PDF 1.7:
- PDF object generation
- Font embedding and subsetting
- Content stream generation
- Graphics state management
- Resource dictionary management
- Cross-reference table and trailer

## Performance Considerations

### Memory Management
- Streaming XML parsing where possible
- Lazy evaluation of properties
- Dispose pattern for large objects
- Memory pooling for frequently allocated objects

### CPU Optimization
- Cache property resolution results
- Optimize font metrics lookups
- Efficient text measurement
- Minimize string allocations

### Benchmarking
- Continuous performance tracking in CI
- Regression detection
- Profile hot paths regularly

## Future Enhancements (Post-1.0)

- **PDF/A Support**: Archival-quality PDFs
- **Tagged PDF**: Accessibility features
- **Hyphenation**: Line breaking with hyphenation dictionaries
- **SVG Support**: SVG graphics in FO documents
- **MathML**: Mathematical formulas
- **Barcode Support**: Common barcode formats
- **CLI Tool**: Command-line FO to PDF conversion
- **Visual Diff Tool**: Compare FO document rendering
