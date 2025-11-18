# Folly Project Refactoring Plan

## Executive Summary

This plan outlines a comprehensive refactoring of Folly into a more modular, testable, and reusable architecture. The current codebase (~25K LOC in `Folly.Core`) contains several independent subsystems that could be extracted into standalone libraries useful far beyond XSL-FO processing.

**Current Status (January 2025):**
- Monolithic `Folly.Core` mixing XSL-FO-specific and reusable components
- Excellent internal separation (BiDi, SVG, Images, Fonts are isolated)
- Clean Area Tree intermediate representation
- Strong foundation for refactoring

**Refactoring Goal:**
Transform Folly from a monolithic XSL-FO renderer into a suite of composable libraries:
- **10 focused packages** instead of 1 monolithic core
- **5+ libraries** useful outside XSL-FO context
- **Easier testing** - components testable in isolation
- **Clearer architecture** - explicit boundaries and dependencies
- **Better evolution** - easy to add formats, features, renderers

**Key Principle:** Maintain 100% API compatibility for existing users while restructuring internals.

---

## Current Architecture Assessment

### The Good

**Folly.Fonts** (~8K LOC)
- Already perfectly separated
- Zero dependencies on other Folly components
- Complete TrueType/OpenType parser
- Reusable in any text rendering system

**Area Tree**
- Clean intermediate representation
- Format-agnostic (not tied to XSL-FO or PDF)
- Could serve multiple input/output formats

**Independent Subsystems** (buried in Folly.Core)
- BiDi algorithm (~1K LOC) - pure UAX#9 implementation
- Hyphenation engine (~1K LOC) - Liang's algorithm
- SVG subsystem (~5.5K LOC) - complete parser and renderer
- Image parsers (~2K LOC) - JPEG, PNG, BMP, GIF, TIFF
- Knuth-Plass line breaker - optimal line breaking

### The Challenge

**Folly.Core is Monolithic** (~25K LOC)
```
Folly.Core/
├── Dom/              (FO-specific, ~8K LOC)
├── Layout/           (FO-specific, ~7K LOC)
├── Svg/              (Reusable!, ~5.5K LOC)
├── Images/           (Reusable!, ~2K LOC)
├── Fonts/            (Reusable!, ~500 LOC utilities)
├── BiDi/             (Reusable!, ~1K LOC)
├── Hyphenation/      (Reusable!, ~1K LOC)
└── AreaTree.cs       (Reusable!, ~800 LOC)
```

**Problems:**
- Cannot use BiDi algorithm without referencing all of Folly.Core
- Cannot use SVG parser for non-PDF outputs
- Cannot test components in isolation
- Hard to understand component boundaries
- Difficult to reuse outside XSL-FO context

**Tight Coupling:**
- LayoutEngine ↔ FO DOM classes (expected for FO semantics)
- PdfRenderer ↔ Area Tree (single output format)

**Loose Coupling (Good Design):**
- IImageParser plugin interface
- ILogger abstraction
- Fonts subsystem (completely independent)
- Area Tree (format-agnostic)

---

## Proposed Refactored Architecture

### Tier 1: Foundation Libraries (Zero FO/PDF Coupling)

#### 1. Folly.Typography
**~3,000 lines | Zero dependencies**

**Contains:**
- BiDi algorithm (UAX#9) - Unicode bidirectional text
- Hyphenation engine (Liang's algorithm, 4 languages)
- Knuth-Plass line breaker - optimal line breaking
- Text measurement utilities

**Why separate:**
- Useful for ANY text layout system (HTML, ePub, markdown, word processors)
- Completely independent of XSL-FO concepts
- Testable with simple string inputs
- Could add more languages/algorithms without FO coupling

**Example use cases:**
```csharp
// Use in a web rendering engine
var bidi = new BidiAlgorithm();
var reordered = bidi.Reorder("مرحبا Hello");

// Use in a word processor
var hyphenator = new Hyphenator("en-US");
var breakPoints = hyphenator.FindBreakPoints("extraordinary");

// Use in a PDF library
var lineBreaker = new KnuthPlassLineBreaker(options);
var breaks = lineBreaker.FindOptimalBreaks(text, width);
```

**Benefits:**
- Any text rendering system can use BiDi support
- Reusable hyphenation for document generators
- High-quality line breaking for typesetting systems

---

#### 2. Folly.Images
**~2,000 lines | Zero dependencies**

**Contains:**
- All image parsers (JPEG, PNG, BMP, GIF, TIFF)
- IImageParser plugin interface
- ImageFormatDetector - auto-detection
- DPI extraction, alpha channel handling
- ICC profile parsing

**Why separate:**
- Needed by ePub, Office formats, HTML-to-PDF, any document generator
- No PDF/FO concepts involved
- Easy to add new formats (WebP, AVIF, HEIF)
- Testable with raw image bytes

**Example use cases:**
```csharp
// Use in an ePub generator
var detector = new ImageFormatDetector();
var parser = detector.Detect(imageBytes);
var info = parser.Parse(imageBytes);
Console.WriteLine($"{info.Width}x{info.Height}, {info.Format}, {info.DpiX}x{info.DpiY}");

// Use in a CMS
var pngParser = new PngParser();
var imageInfo = pngParser.Parse(File.ReadAllBytes("photo.png"));
if (imageInfo.IccProfile != null)
    Console.WriteLine("Image has embedded color profile");
```

**Benefits:**
- Any document format can embed images
- Reusable for image galleries, CMS systems
- Format detection useful for file validators

---

#### 3. Folly.Svg
**~5,500 lines | Zero dependencies**

**Contains:**
- SVG parsing (shapes, paths, text, gradients, filters, clipping)
- CSS stylesheet support (305 lines parser)
- Transform/matrix operations
- Path command parser (all 14 commands)
- ISvgRenderer abstraction

**Why separate:**
- SVG used in countless contexts beyond XSL-FO
- Could render to multiple backends (PDF, HTML Canvas, raster)
- Testable independently with SVG strings

**Refactor needed:**
- Abstract the PDF operator generation into ISvgRenderer interface
- Provide SvgToPdfRenderer implementation
- Opens door for SvgToCanvasRenderer, SvgToImageRenderer, etc.

**Example use cases:**
```csharp
// Use in a web-based diagram tool
var svg = SvgDocument.Parse("<svg>...</svg>");
var pdfRenderer = new SvgToPdfRenderer();
pdfRenderer.Render(svg, pdfStream);

// Or render to PNG
var imageRenderer = new SvgToImageRenderer();
var bitmap = imageRenderer.Render(svg, width: 800, height: 600);

// Or render to HTML Canvas
var canvasRenderer = new SvgToCanvasRenderer();
var jsCode = canvasRenderer.Render(svg); // Generates Canvas API calls
```

**Benefits:**
- Diagramming tools can use SVG parser
- Icon renderers, chart libraries
- Game engines needing vector graphics

---

#### 4. Folly.Fonts *(already exists - keep as-is)*
**~8,000 lines | Perfect separation already achieved**

**Contains:**
- TrueType/OpenType font parsing
- Font subsetting (glyph filtering)
- Font serialization to PDF-embeddable format
- System font discovery (Windows, macOS, Linux)
- OpenType GPOS/GSUB shaping
- CFF font support

**Status:** Already independent, zero dependencies, perfect architecture.

---

### Tier 2: Layout Abstraction

#### 5. Folly.Layout
**~1,500 lines | Zero dependencies**

**Contains:**
- Area hierarchy (BlockArea, InlineArea, TableArea, etc.)
- AreaTree model - intermediate representation
- Generic layout primitives (margins, padding, borders, positioning)
- Link/annotation areas
- Z-index/absolute positioning
- Visibility, clipping, overflow properties

**Why separate:**
- Area Tree is **already format-agnostic**
- Could be target for HTML layout, Markdown layout, custom formats
- Clean separation between "what to render" (layout) and "how to render" (PDF/HTML/etc.)

**Example use cases:**
```csharp
// HTML-to-PDF engine
var htmlParser = new HtmlParser();
var areaTree = htmlParser.Layout(htmlDocument); // Produces AreaTree
var pdfRenderer = new PdfRenderer();
pdfRenderer.Render(areaTree, pdfStream);

// Or render to HTML
var htmlRenderer = new HtmlRenderer();
htmlRenderer.Render(areaTree, htmlStream);

// Or render to image
var imageRenderer = new ImageRenderer();
var bitmap = imageRenderer.Render(areaTree, width, height);
```

**Benefits:**
- Multiple input formats can produce Area Trees
- Multiple output formats can consume Area Trees
- Layout logic separated from rendering logic

---

### Tier 3: Format-Specific Libraries

#### 6. Folly.Pdf.Core
**~6,000 lines | Depends on: Folly.Layout, Folly.Fonts, Folly.Images, Folly.Svg**

**Contains:**
- Low-level PDF writing (PdfWriter)
- PDF object model (catalog, pages, resources)
- Graphics state management
- Font embedding integration
- Image embedding integration
- Metadata/XMP support
- PDF/A compliance
- Structure tree (tagged PDF)

**Why separate:**
- PDF generation useful beyond XSL-FO (reports, forms, diagrams)
- Clear dependency: Folly.Layout → Folly.Pdf.Core
- Could be used by other format converters

**Example use cases:**
```csharp
// Diagram-to-PDF converter
var diagram = DiagramParser.Parse(diagramFile);
var areaTree = DiagramLayoutEngine.Layout(diagram);
var pdfWriter = new PdfWriter(options);
pdfWriter.Render(areaTree, outputStream);

// Report generator
var report = ReportBuilder.Build(data);
var areaTree = ReportLayoutEngine.Layout(report);
var pdfRenderer = new PdfRenderer();
pdfRenderer.Render(areaTree, "report.pdf");
```

---

#### 7. Folly.Xslfo.Model
**~8,000 lines | Zero dependencies**

**Contains:**
- FoElement hierarchy (FoBlock, FoTable, FoInline, etc.)
- FO properties system with inheritance
- FoParser (XML → FO DOM)
- Property metadata (50+ inheritable properties)

**Why separate:**
- Isolates XSL-FO specification knowledge
- Could be used for FO→HTML, FO→ePub converters
- Testable without layout or PDF

**Example use cases:**
```csharp
// XSL-FO to HTML converter
var foDoc = FoDocument.Load("document.fo");
var htmlConverter = new FoToHtmlConverter();
var html = htmlConverter.Convert(foDoc);

// XSL-FO validator
var foDoc = FoDocument.Load("document.fo");
var validator = new FoValidator();
var errors = validator.Validate(foDoc);
```

---

#### 8. Folly.Xslfo.Layout
**~7,000 lines | Depends on: Folly.Xslfo.Model, Folly.Layout, Folly.Typography, Folly.Fonts, Folly.Images, Folly.Svg**

**Contains:**
- LayoutEngine (FO DOM → Area Tree)
- Table layout logic (spanning, header/footer repetition)
- List layout logic
- Footnotes, floats, markers
- Keep constraints, widow/orphan control
- Multi-column layout

**Why separate:**
- Clear single responsibility: FO semantics → geometric layout
- Testable by comparing Area Trees (current snapshot tests already do this!)
- All FO-specific layout logic isolated

---

### Tier 4: Composition/API

#### 9. Folly (or Folly.Xslfo)
**~500 lines | Depends on: Folly.Xslfo.Model, Folly.Xslfo.Layout, Folly.Pdf.Core**

**Contains:**
- High-level FoDocument API
- Orchestrates: Model → Layout → PDF
- LoadOptions, LayoutOptions, PdfOptions
- Extension methods (SavePdf)

**Why separate:**
- Tiny orchestration layer
- Users wanting "XSL-FO → PDF" just reference this package
- Pulls in all necessary dependencies transparently

**Example:**
```csharp
// User code - unchanged from current API
var doc = FoDocument.Load("document.fo");
doc.SavePdf("output.pdf", new PdfOptions { PdfACompliance = PdfALevel.PdfA2b });
```

---

#### 10. Folly.Fluent *(keep as-is)*
**Programmatic FO document builder**

Depends on: Folly (composition package)

---

## Dependency Graph (Proposed)

```
Tier 1: Foundation (Independent)
  ├── Folly.Typography     [BiDi, Hyphenation, LineBreaking]
  ├── Folly.Images         [JPEG, PNG, BMP, GIF, TIFF parsers]
  ├── Folly.Svg            [SVG parsing + ISvgRenderer]
  └── Folly.Fonts          [TrueType/OpenType] (already exists)

Tier 2: Layout Abstraction
  └── Folly.Layout         [AreaTree, Area hierarchy]
        ↑ (no dependencies)

Tier 3: Format-Specific
  ├── Folly.Pdf.Core
  │     ↑ depends on: Folly.Layout, Folly.Fonts, Folly.Images, Folly.Svg
  │
  ├── Folly.Xslfo.Model    [FO DOM, parser, properties]
  │     ↑ (no dependencies)
  │
  └── Folly.Xslfo.Layout   [LayoutEngine: FO DOM → AreaTree]
        ↑ depends on: Folly.Xslfo.Model, Folly.Layout,
                       Folly.Typography, Folly.Fonts, Folly.Images, Folly.Svg

Tier 4: Composition
  ├── Folly (or Folly.Xslfo)  [High-level API, orchestration]
  │     ↑ depends on: Folly.Xslfo.Model, Folly.Xslfo.Layout, Folly.Pdf.Core
  │
  └── Folly.Fluent            [Fluent document builder]
        ↑ depends on: Folly
```

**Key characteristics:**
- Tier 1: Zero dependencies (pure .NET 8)
- Tier 2: Zero dependencies (pure .NET 8)
- Tier 3: Format-specific, depends on Tier 1 + Tier 2
- Tier 4: Thin orchestration, depends on Tier 3

---

## Benefits Analysis

### 1. Testability

| Component | Current Testing Challenges | After Refactoring |
|-----------|---------------------------|-------------------|
| BiDi | Must reference all of Folly.Core | Test against `Folly.Typography` with simple strings |
| SVG | Hard to test without PDF output | Mock `ISvgRenderer`, test parsing independently |
| Images | Buried in Core | Test each parser with raw bytes |
| Layout primitives | Mixed with FO semantics | Test `AreaTree` generation from simple inputs |
| FO parsing | Requires full pipeline | Test DOM creation without layout/PDF |

**Concrete example:**
```csharp
// BEFORE: Testing BiDi requires Folly.Core reference
[Fact]
public void BiDi_MixedText_ReordersCorrectly()
{
    // Must set up FO document, parse XML, run layout...
}

// AFTER: Direct unit test
[Fact]
public void BiDi_MixedText_ReordersCorrectly()
{
    var bidi = new BidiAlgorithm();
    var result = bidi.Reorder("Hello مرحبا");
    Assert.Equal("Hello ابحرم", result); // Expected visual order
}
```

### 2. Reusability Outside XSL-FO

| Component | Potential Uses |
|-----------|----------------|
| **Folly.Typography** | Web browsers, word processors, markdown renderers, chat apps (BiDi), ePub generators |
| **Folly.Images** | ePub generators, email clients, CMS systems, image galleries, file validators |
| **Folly.Svg** | Diagramming tools, icon renderers, chart libraries, game engines, vector editors |
| **Folly.Fonts** | Any text rendering system, PDF libraries, graphics apps, font tools |
| **Folly.Layout** | HTML-to-PDF, markdown-to-PDF, ePub layout, custom document formats |
| **Folly.Pdf.Core** | Report generators, form fillers, diagram-to-PDF, chart-to-PDF |

**Example scenario - HTML-to-PDF:**
```csharp
// Developer wants to build HTML→PDF (no XSL-FO needed)
// Can use:
//   Folly.Layout (for area tree)
//   Folly.Pdf.Core (for rendering)
//   Folly.Typography (for text features)
//   Folly.Fonts (for font embedding)
// WITHOUT pulling in XSL-FO parser, layout engine, etc.

var html = "<html><body><p>Hello</p></body></html>";
var areaTree = new HtmlLayoutEngine().Layout(html);
var pdfRenderer = new PdfRenderer();
pdfRenderer.Render(areaTree, "output.pdf");
```

### 3. Easier to Reason About

**Current mental model:**
- "Folly.Core does... everything? Layout? Parsing? SVG? Images? BiDi?"
- Hard to know where to start
- ~25K lines in one project

**After refactoring:**
- "I need SVG support → look at `Folly.Svg`" (~5.5K lines, focused)
- "I need hyphenation → look at `Folly.Typography`" (~3K lines)
- "I want to understand FO parsing → look at `Folly.Xslfo.Model`"
- Each project has clear, single purpose
- Dependency graph is explicit and understandable

**Documentation becomes clearer:**
```markdown
# Folly.Typography

Provides text layout primitives:
- Unicode BiDi (UAX#9)
- Hyphenation (Liang's algorithm, 4 languages)
- Optimal line breaking (Knuth-Plass)

Used by: Folly.Xslfo.Layout, your custom text systems

Dependencies: None (pure .NET 8)

Installation:
dotnet add package Folly.Typography
```

### 4. Easier Evolution

**Adding new formats:**
- Want ePub support? Create `Folly.Epub` using `Folly.Layout`, `Folly.Images`, etc.
- Want HTML-to-PDF? Create `Folly.Html.Layout` producing Area Trees
- Want SVG-to-PNG? Implement `ISvgRenderer` for raster output

**Adding features:**
- New image format? Just extend `Folly.Images`
- New hyphenation language? Just extend `Folly.Typography`
- New PDF feature? Just extend `Folly.Pdf.Core`
- Changes don't ripple across monolithic Core

**Example - Adding WebP support:**
```csharp
// In Folly.Images package only
public class WebPParser : IImageParser
{
    public ImageInfo Parse(byte[] data) { ... }
}

// Register in ImageFormatDetector
// No changes needed anywhere else!
```

---

## Migration Strategy

### Phase 1: Extract Independent Libraries (4-6 weeks)
**Low Risk | No Behavior Changes**

#### 1.1 Create Folly.Typography
- Move BiDi/ → Folly.Typography/BiDi/
- Move Hyphenation/ → Folly.Typography/Hyphenation/
- Move KnuthPlassLineBreaker.cs → Folly.Typography/LineBreaking/
- Update Folly.Core to reference Folly.Typography
- All existing tests pass unchanged

**Deliverables:**
- [x] New Folly.Typography.csproj
- [x] BiDi, Hyphenation, Knuth-Plass moved
- [x] Folly.Core references Folly.Typography
- [x] All tests passing
- [x] Zero warnings, zero errors
- [ ] NuGet package published

**Success Metrics:**
- Independent Folly.Typography package on NuGet
- Can be used without any Folly references
- All 485 existing tests pass
- Zero breaking changes for users

---

#### 1.2 Create Folly.Images
- Move Images/ → Folly.Images/
- Update Folly.Core to reference Folly.Images
- All existing tests pass unchanged

**Deliverables:**
- [x] New Folly.Images.csproj
- [x] All image parsers moved
- [x] IImageParser interface moved
- [x] ImageFormatDetector moved
- [x] Folly.Core references Folly.Images
- [x] All tests passing
- [x] Zero warnings, zero errors
- [ ] NuGet package published

**Success Metrics:**
- Independent Folly.Images package on NuGet
- Can parse images without Folly.Core
- All 485 existing tests pass
- Zero breaking changes for users

---

### Phase 2: Abstract SVG Rendering (6-8 weeks)
**Medium Risk | Requires Interface Design**

#### 2.1 Create ISvgRenderer Interface
```csharp
namespace Folly.Svg
{
    public interface ISvgRenderer
    {
        void BeginDocument();
        void BeginGroup(SvgTransform? transform);
        void EndGroup();
        void RenderPath(SvgPath path, SvgStyle style);
        void RenderRect(SvgRect rect, SvgStyle style);
        void RenderCircle(SvgCircle circle, SvgStyle style);
        void RenderText(SvgText text, SvgStyle style);
        // ... other shapes
        void EndDocument();
    }
}
```

**Deliverables:**
- [ ] Design ISvgRenderer interface
- [ ] Refactor SvgToPdf to use interface
- [ ] Create SvgToPdfRenderer implementation
- [ ] All SVG tests pass
- [ ] Zero behavior changes

---

#### 2.2 Extract Folly.Svg Package
- Move Svg/ → Folly.Svg/
- Include ISvgRenderer interface
- Include SvgToPdfRenderer implementation
- Update Folly.Core to reference Folly.Svg
- All existing tests pass unchanged

**Deliverables:**
- [ ] New Folly.Svg.csproj
- [ ] ISvgRenderer abstraction
- [ ] SvgToPdfRenderer implementation
- [ ] All SVG tests passing
- [ ] NuGet package published

**Success Metrics:**
- Independent Folly.Svg package
- Can parse SVG without Folly.Core
- Can implement custom renderers
- All tests pass
- Zero breaking changes

---

### Phase 3: Split FO Components (10-12 weeks)
**Higher Risk | Major Structural Change**

#### 3.1 Extract Folly.Layout
- Create AreaTree.cs → Folly.Layout/AreaTree.cs
- Create Area hierarchy → Folly.Layout/Areas/
- Move layout primitives
- All packages reference Folly.Layout

**Deliverables:**
- [ ] New Folly.Layout.csproj
- [ ] AreaTree and Area hierarchy moved
- [ ] All packages updated
- [ ] All tests passing
- [ ] NuGet package published

---

#### 3.2 Extract Folly.Xslfo.Model
- Move Dom/ → Folly.Xslfo.Model/
- Move FoParser → Folly.Xslfo.Model/
- Move property system → Folly.Xslfo.Model/
- Zero dependencies (pure FO DOM)

**Deliverables:**
- [ ] New Folly.Xslfo.Model.csproj
- [ ] FO DOM classes moved
- [ ] FoParser moved
- [ ] Property system moved
- [ ] All tests passing
- [ ] NuGet package published

---

#### 3.3 Extract Folly.Xslfo.Layout
- Move Layout/LayoutEngine.cs → Folly.Xslfo.Layout/
- Reference: Folly.Xslfo.Model, Folly.Layout, Folly.Typography, Folly.Fonts, Folly.Images, Folly.Svg
- All layout tests pass

**Deliverables:**
- [ ] New Folly.Xslfo.Layout.csproj
- [ ] LayoutEngine moved
- [ ] All dependencies referenced
- [ ] All tests passing
- [ ] NuGet package published

---

#### 3.4 Split Folly.Pdf into Folly.Pdf.Core
- Extract core PDF writing to Folly.Pdf.Core
- Keep FO-specific extensions in Folly (composition package)
- Reference: Folly.Layout, Folly.Fonts, Folly.Images, Folly.Svg

**Deliverables:**
- [ ] New Folly.Pdf.Core.csproj
- [ ] PdfWriter, PdfRenderer moved
- [ ] All PDF tests passing
- [ ] NuGet package published

---

#### 3.5 Create Folly Composition Package
- Create Folly.csproj (or Folly.Xslfo.csproj)
- Reference: Folly.Xslfo.Model, Folly.Xslfo.Layout, Folly.Pdf.Core
- FoDocument API (high-level orchestration)
- Extension methods (SavePdf)

**Deliverables:**
- [ ] New Folly.csproj (composition package)
- [ ] FoDocument API orchestration
- [ ] All packages referenced
- [ ] All tests passing
- [ ] **API 100% identical to current Folly**
- [ ] NuGet package published

**Success Metrics:**
- Existing user code works unchanged
- `dotnet add package Folly` pulls in all necessary dependencies
- All 485+ tests pass
- Zero breaking changes

---

### Phase 4: Verify and Document (2-3 weeks)

#### 4.1 Comprehensive Testing
- [ ] All existing tests pass (100%)
- [ ] Add new tests for independent packages
- [ ] Add integration tests for composition
- [ ] Performance benchmarks unchanged
- [ ] Memory usage unchanged

#### 4.2 Update Documentation
- [ ] README.md - architecture diagram
- [ ] README.md - package descriptions
- [ ] Update examples to show new packages
- [ ] Migration guide (even though no changes needed)
- [ ] Architecture documentation

#### 4.3 NuGet Publishing
- [ ] All packages published to NuGet
- [ ] Version numbers consistent
- [ ] Package descriptions clear
- [ ] Dependencies correct

---

## Success Metrics

### Per-Phase Metrics

**Phase 1:**
- ✅ Folly.Typography, Folly.Images published to NuGet
- ✅ Can be used independently
- ✅ All 485+ tests passing
- ✅ Zero breaking changes

**Phase 2:**
- ✅ Folly.Svg published to NuGet
- ✅ ISvgRenderer interface defined
- ✅ Can implement custom renderers
- ✅ All tests passing
- ✅ Zero breaking changes

**Phase 3:**
- ✅ 10 packages published to NuGet
- ✅ Clear dependency graph
- ✅ Composition package maintains API compatibility
- ✅ All tests passing
- ✅ Zero breaking changes

**Phase 4:**
- ✅ Documentation complete
- ✅ Examples updated
- ✅ Performance unchanged
- ✅ Memory usage unchanged

### Overall Success Metrics

**Architecture:**
- ✅ 10 focused packages vs 1 monolithic core
- ✅ Clear tier structure (1: Foundation, 2: Layout, 3: Format-specific, 4: Composition)
- ✅ Explicit dependencies (no circular references)
- ✅ Each package < 10K LOC

**Reusability:**
- ✅ 5+ packages useful outside XSL-FO context
- ✅ BiDi, Hyphenation, Images, SVG, Fonts independently usable
- ✅ Layout abstraction enables new input/output formats

**Testability:**
- ✅ Components testable in isolation
- ✅ Simple unit tests for typography, images, SVG
- ✅ Mock interfaces for renderer testing

**User Experience:**
- ✅ Zero breaking changes (100% API compatibility)
- ✅ Existing code works unchanged
- ✅ Performance unchanged (zero regression)
- ✅ Same zero-dependency guarantee for core packages

**Evolution:**
- ✅ Easy to add new formats (HTML, ePub, markdown)
- ✅ Easy to add new renderers (Canvas, Image, etc.)
- ✅ Easy to extend individual packages

---

## Potential Concerns & Mitigations

### Concern: "More projects = harder to maintain"
**Mitigation:**
- Each project is smaller, easier to understand
- Clear boundaries reduce cognitive load
- Better than one 25K line monolith
- Focused responsibility per package

### Concern: "Circular dependencies"
**Mitigation:**
- Careful tier design (Tier 1 has zero deps)
- Tier 2 depends only on Tier 1
- Tier 3 depends on Tier 1 + Tier 2
- Tier 4 composition layer
- Use interfaces for plugin patterns (ISvgRenderer, IImageParser)

### Concern: "Package proliferation for consumers"
**Mitigation:**
- Composition package `Folly` references everything needed
- Users wanting full XSL-FO→PDF: just reference `Folly`
- Users wanting specific components: reference granularly
- NuGet resolves dependencies automatically

### Concern: "Breaking existing code"
**Mitigation:**
- Keep `Folly` package API identical
- Migration is internal restructuring only
- Public API unchanged
- Existing projects reference `Folly` and continue working
- Comprehensive tests verify behavior unchanged

### Concern: "Performance overhead from package boundaries"
**Mitigation:**
- No runtime overhead (just different namespaces)
- Compiler optimizations work across assemblies
- Benchmark suite verifies no regression
- If needed, can use InternalsVisibleTo for hot paths

---

## Risk Management

### Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Circular dependency emerges | Medium | High | Strict tier structure, design review before implementation |
| Performance regression | Low | High | Comprehensive benchmarks, verify no regression |
| Breaking changes slip through | Medium | High | Exhaustive testing, API compatibility checks |
| SVG abstraction too complex | Medium | Medium | Incremental design, prototype first |
| Package dependency hell | Low | Medium | Careful version management, semantic versioning |

### Project Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Scope creep (add too many features) | Medium | Medium | Stick to refactoring plan, defer new features |
| Timeline exceeds estimate | High | Low | Phase-by-phase approach, can pause anytime |
| User confusion about packages | Medium | Medium | Clear documentation, composition package |
| Maintenance burden increases | Low | Medium | Automation (CI/CD), clear ownership |

---

## Timeline Estimate

**Phase 1: Extract Independent Libraries**
- 4-6 weeks
- Low risk
- Immediate value (Folly.Typography, Folly.Images usable)

**Phase 2: Abstract SVG Rendering**
- 6-8 weeks
- Medium risk
- Moderate complexity (interface design)

**Phase 3: Split FO Components**
- 10-12 weeks
- Higher risk
- Major restructuring (but tests protect against breakage)

**Phase 4: Verify and Document**
- 2-3 weeks
- Low risk
- Polishing, documentation

**Total: 22-29 weeks (5-7 months)**

Can pause between phases for:
- User feedback
- Bug fixes
- New feature requests
- Market validation

---

## Package Descriptions (for NuGet)

### Folly.Typography
> Text layout primitives: Unicode BiDi (UAX#9), hyphenation (Liang's algorithm), optimal line breaking (Knuth-Plass). Zero dependencies. Pure .NET 8.

### Folly.Images
> Image format parsers for JPEG, PNG, BMP, GIF, TIFF with DPI extraction and ICC profile support. Plugin architecture for extensibility. Zero dependencies. Pure .NET 8.

### Folly.Svg
> Complete SVG 1.1 parser with CSS support. Includes abstraction for multiple rendering backends. Zero dependencies. Pure .NET 8.

### Folly.Fonts
> TrueType/OpenType font parser with subsetting, embedding, and OpenType shaping (GPOS/GSUB). System font discovery. Zero dependencies. Pure .NET 8.

### Folly.Layout
> Format-agnostic layout abstraction (Area Tree). Intermediate representation for document layout engines. Zero dependencies. Pure .NET 8.

### Folly.Pdf.Core
> Low-level PDF 1.7 writer with font embedding, image support, metadata, and PDF/A compliance. Depends on: Folly.Layout, Folly.Fonts, Folly.Images, Folly.Svg.

### Folly.Xslfo.Model
> XSL-FO 1.1 document object model with XML parser and property inheritance system. Zero dependencies. Pure .NET 8.

### Folly.Xslfo.Layout
> XSL-FO layout engine converting FO documents to Area Trees. Supports tables, lists, footnotes, markers, multi-column layout. Depends on: Folly.Xslfo.Model, Folly.Layout, Folly.Typography, Folly.Fonts, Folly.Images, Folly.Svg.

### Folly
> Complete XSL-FO 1.1 to PDF 1.7 renderer. High-level API orchestrating all Folly packages. Zero runtime dependencies beyond .NET 8.

### Folly.Fluent
> Fluent API for programmatic XSL-FO document construction. Alternative to XML authoring. Depends on: Folly.

---

## Next Steps

### Immediate Actions (Week 1)
1. **Decision:** Review this plan with stakeholders
2. **Approval:** Get buy-in for phased approach
3. **Branch:** Create `refactor/phase-1` branch
4. **Start:** Begin Phase 1.1 (Folly.Typography extraction)

### Week 2-6: Phase 1 Execution
1. Extract Folly.Typography
2. Extract Folly.Images
3. Update Folly.Core references
4. Verify all tests pass
5. Publish to NuGet

### Review After Phase 1
- Assess benefits realized
- User feedback on independent packages
- Decide whether to continue to Phase 2
- Adjust plan based on learnings

---

## Conclusion

This refactoring plan transforms Folly from a monolithic XSL-FO renderer into a **suite of composable, reusable libraries**. The phased approach ensures:

**Safety:**
- Zero breaking changes for existing users
- Comprehensive testing throughout
- Can pause/adjust between phases

**Value:**
- 5+ libraries useful beyond XSL-FO
- Easier testing and reasoning
- Enables format expansion (HTML, ePub, markdown)

**Pragmatism:**
- Incremental delivery (Phase 1 delivers value in 6 weeks)
- Low-risk phases first (independent libraries)
- Higher-risk work later (with safety net of tests)

**Maintaining Core Values:**
- ✅ Zero runtime dependencies (Tier 1 + Tier 2)
- ✅ Excellent performance (no regression)
- ✅ Production quality (all tests pass)
- ✅ API compatibility (existing code works)

The current codebase **already has excellent separation internally**. This refactoring just makes that separation **explicit** via project boundaries, unlocking testability and reusability benefits.

**Ready to Begin:** Phase 1 can start immediately. Extract Folly.Typography as proof of concept.
