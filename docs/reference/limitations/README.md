# Folly Layout Engine Limitations

## Overview

This directory contains detailed documentation of known limitations in the Folly XSL-FO layout engine. While Folly implements a comprehensive subset of XSL-FO 1.1 with excellent performance, several advanced features are not yet supported.

**Overall XSL-FO 1.1 Compliance**: ~65-70%

## Quick Summary

### What Works Well ✅

- **Basic document layout** - Multi-page pagination, margins, padding
- **Text formatting** - Fonts (14 standard), sizes, colors, alignment (start/center/end)
- **Tables** - Column spanning, headers, borders, backgrounds
- **Lists** - Ordered and unordered lists with labels
- **Images** - JPEG and PNG embedding with scaling
- **Links** - Internal cross-references and external URLs
- **Bookmarks** - PDF outline/table of contents
- **Static content** - Headers and footers with page numbers
- **Markers** - Running headers with dynamic content
- **Multi-column layout** - Newspaper-style columns
- **Footnotes** - Bottom-of-page notes with separators
- **Performance** - 66x faster than target (150ms for 200 pages)

### Critical Limitations ⚠️

1. **Tables don't break across pages** - Major limitation for long tables
2. **No text justification** - Only left/center/right alignment
3. **Only 14 standard fonts** - No TrueType/OpenType support
4. **Simplified BiDi** - RTL languages may render incorrectly
5. **No row spanning in tables** - Complex table layouts impossible
6. **No hyphenation** - Poor line breaking in narrow columns
7. **No absolute positioning** - Limited layout flexibility
8. **Greedy line breaking** - Produces suboptimal typography
9. **No widow/orphan control** - Unprofessional page breaks
10. **No keep-with-next/previous** - Cannot keep headings with content

## Documentation Index

### Core Layout Issues

#### [Line Breaking & Text Layout](line-breaking-text-layout.md)
**Severity**: High

- ❌ No hyphenation support
- ❌ Greedy line breaking (not Knuth-Plass)
- ❌ No text justification
- ❌ Word-based breaking only (no CJK support)
- ❌ No emergency breaking for overflow

**Impact**: Suboptimal typography, poor line utilization, uneven spacing

**Use Case Affected**: Professional publishing, books, magazines

---

#### [Page Breaking & Pagination](page-breaking-pagination.md)
**Severity**: High

- ❌ No widow/orphan control
- ❌ Tables don't break across pages ⚠️ CRITICAL
- ❌ Lists don't break across pages
- ❌ No keep-with-next/previous
- ❌ No table header repetition
- 🟡 Limited keep-together (binary only)

**Impact**: Large tables unusable, poor professional typography

**Use Case Affected**: Reports, data tables, long documents

---

#### [Positioning & Layout](positioning-layout.md)
**Severity**: Medium

- ❌ No absolute positioning
- ❌ No fixed positioning
- ❌ No background images
- ❌ No z-index/layering
- ❌ No rotation (reference-orientation)
- ❌ No vertical alignment in containers
- 🟡 Simplified floats (no text wrapping)
- 🟡 Region start/end parsed but not rendered

**Impact**: Cannot create complex layouts, letterheads, overlays

**Use Case Affected**: Forms, certificates, magazine layouts

---

### Typography & Internationalization

#### [Fonts & Typography](fonts-typography.md)
**Severity**: Critical for custom branding

- ❌ No TrueType/OpenType font support ⚠️ CRITICAL
- ❌ No font fallback mechanism
- ❌ Limited character coverage (WinAnsiEncoding only)
- ❌ No ligatures or kerning
- ❌ No OpenType features
- ❌ No font subsetting for custom fonts
- 🟡 Only 14 standard PDF fonts

**Impact**: Cannot use custom fonts, no international characters (CJK, Arabic, etc.)

**Use Case Affected**: Corporate branding, multilingual documents

---

#### [BiDi (Bidirectional Text)](bidi-text-support.md)
**Severity**: Critical for RTL languages

- ❌ Not full Unicode BiDi Algorithm (UAX#9) ⚠️
- ❌ Simple character reversal only
- ❌ No support for BiDi control characters
- ❌ No automatic direction detection
- ❌ No block-level direction
- ❌ No layout mirroring

**Impact**: Arabic, Hebrew, and other RTL languages may render incorrectly

**Use Case Affected**: Middle Eastern documents, international content

---

### Content Types

#### [Tables](tables.md)
**Severity**: Critical for data presentation

- ❌ No page breaking ⚠️ CRITICAL
- ❌ No header repetition on page breaks
- ❌ No row spanning implementation
- ❌ No content-based column sizing
- ❌ No proportional column widths
- 🟡 Simplified width calculation

**Impact**: Multi-page tables impossible, complex layouts limited

**Use Case Affected**: Financial reports, data tables, invoices

---

#### [Images](images.md)
**Severity**: Medium

- ❌ Limited format support (JPEG/PNG only)
- ❌ No SVG support
- ❌ No image transformations (rotation, filters)
- ❌ No EXIF orientation
- ❌ No color space management (CMYK, ICC)
- ❌ No image optimization
- 🟡 Basic dimension detection

**Impact**: Cannot use vector graphics, modern formats (WebP), or professional color

**Use Case Affected**: Print workflows, modern web content

---

### Advanced Features

#### [Advanced XSL-FO Features](advanced-features.md)
**Severity**: Low to Medium

- ❌ No initial property set (drop caps)
- ❌ No multi-* elements (conditional content)
- ❌ No index generation
- ❌ No change bars
- 🟡 Character element (basic)
- 🟡 Inline container (limited)
- 🟡 Wrapper (works via inheritance)

**Impact**: Cannot create decorative typography, advanced layouts

**Use Case Affected**: Books, magazines, revision tracking

---

### Quality & Compliance

#### [Security & Validation](security-validation.md)
**Severity**: Medium

- ✅ XXE prevention (implemented)
- ✅ Resource limits (implemented)
- ✅ Image path validation (implemented)
- ✅ PDF metadata sanitization (implemented)
- ❌ Namespace validation not enforced
- ❌ No schema validation
- ❌ No URL scheme whitelist
- 🟡 Limited property validation

**Impact**: May accept invalid FO, some security gaps

**Use Case Affected**: Security-sensitive deployments

---

#### [Performance](performance.md)
**Severity**: Low (excellent current performance)

- ❌ Single-threaded layout
- ❌ No streaming support
- ❌ No layout caching
- ❌ No incremental layout
- 🟡 Current: 66x faster than target (very good!)

**Impact**: Extreme workloads (10,000+ pages) could be faster

**Use Case Affected**: Batch processing, very large documents

---

#### [PDF Rendering](rendering.md)
**Severity**: Low to Medium

- ❌ No gradients
- ❌ No rounded corners
- ❌ No transparency/opacity
- ❌ No clipping paths
- ❌ No filters/effects
- ❌ No CMYK color (RGB only)
- ❌ No Tagged PDF (accessibility)
- ❌ No PDF/A compliance
- 🟡 Limited border styles (solid, dashed, dotted)

**Impact**: Limited visual effects, no professional print, no accessibility

**Use Case Affected**: Print workflows, accessibility requirements

---

### Reference

#### [Missing XSL-FO Features - Complete Checklist](missing-xslfo-features.md)

Comprehensive checklist of all XSL-FO 1.1 elements and properties with implementation status.

---

## Severity Levels

| Level | Impact | Examples |
|-------|--------|----------|
| **Critical** ⚠️ | Breaks common use cases | Table page breaking, TrueType fonts, BiDi |
| **High** | Significantly limits usability | Text justification, hyphenation, widow/orphan |
| **Medium** | Important for specific workflows | Absolute positioning, image formats, CMYK |
| **Low** | Nice to have, edge cases | Rounded corners, drop caps, performance optimizations |

## Use Case Suitability

### ✅ Well Suited For

- **Business documents** - Reports, invoices, letters
- **Simple publishing** - Documentation, manuals
- **Forms** (non-interactive) - Printable forms, applications
- **Certificates** - Simple layouts without complex positioning
- **Basic reports** - With short tables and simple formatting

### 🟡 Partially Suited For

- **Books** - Limited by typography (no justification/hyphenation)
- **Magazines** - Limited by positioning and advanced layout
- **Data-heavy reports** - Limited by table page breaking
- **Multilingual content** - Limited by font and BiDi support

### ❌ Not Suitable For

- **Professional publishing** - Needs better typography
- **Complex forms** - Needs absolute positioning
- **RTL languages** - Needs full BiDi support
- **Print production** - Needs CMYK, ICC profiles
- **Accessible PDFs** - Needs Tagged PDF
- **CJK documents** - Needs Unicode font support

## Workarounds

Many limitations can be worked around:

1. **Table page breaking** → Split large tables manually in FO
2. **Custom fonts** → Use standard fonts that are similar
3. **Text justification** → Accept left-aligned text
4. **Absolute positioning** → Use tables for layout
5. **Background images** → Embed as regular content
6. **Row spanning** → Restructure table layout
7. **BiDi text** → Pre-process text for RTL
8. **Image formats** → Convert to JPEG/PNG

## Implementation Priorities

Based on user impact, the recommended implementation order is:

### Phase 1: Critical Gaps (High ROI)
1. **Table page breaking** - Enables multi-page tables
2. **Table header repetition** - Professional tables
3. **TrueType/OpenType fonts** - Enables custom fonts

### Phase 2: Professional Typography
4. **Text justification** - Better appearance
5. **Hyphenation** - Better line breaking
6. **Widow/orphan control** - Professional pagination
7. **Keep-with-next/previous** - Better page breaks

### Phase 3: Advanced Layout
8. **Row spanning in tables** - Complex layouts
9. **Absolute positioning** - Forms and certificates
10. **Region start/end** - Sidebars and margin notes

### Phase 4: Internationalization
11. **Full BiDi support (UAX#9)** - RTL languages
12. **CJK font support** - Asian languages
13. **Complex script shaping** - Arabic, Indic

### Phase 5: Quality & Compliance
14. **Tagged PDF** - Accessibility
15. **PDF/A compliance** - Archival
16. **CMYK color** - Professional print

## Contributing

If you're interested in addressing any of these limitations, please:

1. Review the detailed documentation for the specific limitation
2. Check the "Proposed Solution" sections
3. Consider implementation complexity and dependencies
4. Discuss approach in GitHub issues before large changes

## Testing

Each limitation document includes:
- Current behavior examples
- Expected behavior per XSL-FO spec
- Impact assessment
- Complexity estimates

Test cases should cover:
- Basic functionality
- Edge cases mentioned in limitations
- XSL-FO spec compliance
- Regression prevention

## References

1. **XSL-FO 1.1 Specification**:
   - https://www.w3.org/TR/xsl11/
   - Official W3C recommendation

2. **PDF 1.7 Specification**:
   - https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf
   - PDF reference manual

3. **Unicode BiDi Algorithm (UAX#9)**:
   - https://www.unicode.org/reports/tr9/
   - Bidirectional text algorithm

## Questions?

For questions about specific limitations:
- Read the detailed documentation in the relevant `.md` file
- Check the "Workarounds" sections
- Open a GitHub issue for clarification

For feature requests:
- Check if already documented here
- Assess priority based on your use case
- Open a GitHub issue with use case details
