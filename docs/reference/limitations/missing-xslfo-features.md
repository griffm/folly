# Missing XSL-FO Features - Complete List

## Overview

This document provides a comprehensive checklist of XSL-FO 1.1 features and their implementation status in Folly. For detailed information about specific limitations, see the individual topic documents.

## Legend

- ✅ **Fully Implemented** - Complete support
- 🟡 **Partially Implemented** - Basic support with known limitations
- ❌ **Not Implemented** - Not supported
- ⏳ **Planned** - On roadmap

## Formatting Objects

### Document Structure

| Element | Status | Notes |
|---------|--------|-------|
| `fo:root` | ✅ | Fully supported |
| `fo:declarations` | ✅ | Metadata support |
| `fo:color-profile` | ❌ | No ICC color profiles |
| `fo:page-sequence` | ✅ | Fully supported |
| `fo:layout-master-set` | ✅ | Fully supported |
| `fo:page-sequence-master` | ✅ | Conditional masters supported |
| `fo:single-page-master-reference` | ✅ | Supported |
| `fo:repeatable-page-master-reference` | ✅ | Supported |
| `fo:repeatable-page-master-alternatives` | ✅ | Supported |
| `fo:conditional-page-master-reference` | ✅ | Supported |
| `fo:simple-page-master` | ✅ | Fully supported |
| `fo:region-body` | ✅ | Fully supported |
| `fo:region-before` | ✅ | Headers supported |
| `fo:region-after` | ✅ | Footers supported |
| `fo:region-start` | 🟡 | Parsed, not rendered |
| `fo:region-end` | 🟡 | Parsed, not rendered |
| `fo:flow` | ✅ | Fully supported |
| `fo:static-content` | ✅ | Fully supported |
| `fo:title` | ✅ | Metadata support |

### Block-Level

| Element | Status | Notes |
|---------|--------|-------|
| `fo:block` | ✅ | Fully supported |
| `fo:block-container` | 🟡 | Parsed, no absolute positioning |

### Inline-Level

| Element | Status | Notes |
|---------|--------|-------|
| `fo:bidi-override` | 🟡 | Simple reversal, not full UAX#9 |
| `fo:character` | 🟡 | Parsed, basic support |
| `fo:initial-property-set` | 🟡 | Parsed, not applied |
| `fo:external-graphic` | 🟡 | JPEG/PNG only |
| `fo:instream-foreign-object` | ❌ | No SVG/MathML support |
| `fo:inline` | ✅ | Fully supported |
| `fo:inline-container` | 🟡 | Parsed, limited support |
| `fo:leader` | ✅ | Dots, rules, spaces |
| `fo:page-number` | ✅ | Fully supported |
| `fo:page-number-citation` | ✅ | Fully supported |
| `fo:page-number-citation-last` | ✅ | Fully supported |
| `fo:wrapper` | ✅ | Property inheritance |

### Tables

| Element | Status | Notes |
|---------|--------|-------|
| `fo:table-and-caption` | ❌ | Not implemented |
| `fo:table` | ✅ | Fully supported |
| `fo:table-column` | ✅ | Width, repeat supported |
| `fo:table-caption` | ❌ | Not implemented |
| `fo:table-header` | ✅ | No repetition on page break |
| `fo:table-footer` | ✅ | Basic support |
| `fo:table-body` | ✅ | Fully supported |
| `fo:table-row` | ✅ | Fully supported |
| `fo:table-cell` | ✅ | Column span yes, row span no |

### Lists

| Element | Status | Notes |
|---------|--------|-------|
| `fo:list-block` | ✅ | Fully supported |
| `fo:list-item` | ✅ | Fully supported |
| `fo:list-item-body` | ✅ | Fully supported |
| `fo:list-item-label` | ✅ | Fully supported |

### Links & Bookmarks

| Element | Status | Notes |
|---------|--------|-------|
| `fo:basic-link` | ✅ | Internal and external links |
| `fo:bookmark-tree` | ✅ | PDF outline |
| `fo:bookmark` | ✅ | Nested bookmarks |
| `fo:bookmark-title` | ✅ | Bookmark text |

### Out-of-Line

| Element | Status | Notes |
|---------|--------|-------|
| `fo:float` | 🟡 | Simplified, no text wrap |
| `fo:footnote` | ✅ | Basic support |
| `fo:footnote-body` | ✅ | Supported |

### Other

| Element | Status | Notes |
|---------|--------|-------|
| `fo:retrieve-marker` | 🟡 | Simplified retrieval |
| `fo:retrieve-table-marker` | ❌ | Not implemented |
| `fo:marker` | ✅ | Basic support |

### Multi-Property

| Element | Status | Notes |
|---------|--------|-------|
| `fo:multi-switch` | ❌ | Not implemented |
| `fo:multi-case` | ❌ | Not implemented |
| `fo:multi-toggle` | ❌ | Not implemented |
| `fo:multi-properties` | ❌ | Not implemented |
| `fo:multi-property-set` | ❌ | Not implemented |

### Index

| Element | Status | Notes |
|---------|--------|-------|
| `fo:index-page-citation` | ❌ | Not implemented |
| `fo:index-page-number-prefix` | ❌ | Not implemented |
| `fo:index-page-number-suffix` | ❌ | Not implemented |
| `fo:index-range-begin` | ❌ | Not implemented |
| `fo:index-range-end` | ❌ | Not implemented |

## Properties by Category

### Pagination and Layout

| Property | Status | Notes |
|----------|--------|-------|
| `page-height`, `page-width` | ✅ | Supported |
| `margin-*` | ✅ | All margins |
| `padding-*` | ✅ | All padding |
| `border-*-width` | ✅ | All borders |
| `border-*-style` | 🟡 | solid, dashed, dotted only |
| `border-*-color` | ✅ | Supported |
| `space-before`, `space-after` | ✅ | Supported |
| `start-indent`, `end-indent` | ✅ | Supported |
| `extent` | ✅ | Region sizes |
| `column-count` | ✅ | Multi-column |
| `column-gap` | ✅ | Multi-column |
| `span` | ❌ | Column spanning control |

### Keeps and Breaks

| Property | Status | Notes |
|----------|--------|-------|
| `break-before` | ✅ | always, page |
| `break-after` | ✅ | always, page |
| `keep-together` | 🟡 | always only, no integers |
| `keep-with-next` | ❌ | Not implemented |
| `keep-with-previous` | ❌ | Not implemented |
| `widows` | ❌ | Not implemented |
| `orphans` | ❌ | Not implemented |

### Fonts

| Property | Status | Notes |
|----------|--------|-------|
| `font-family` | 🟡 | Standard fonts only (14) |
| `font-size` | ✅ | Supported |
| `font-weight` | 🟡 | bold mapping only |
| `font-style` | 🟡 | italic/oblique mapping |
| `font-variant` | ❌ | No small-caps |
| `font-stretch` | ❌ | Not implemented |
| `font-selection-strategy` | ❌ | Not implemented |

### Text

| Property | Status | Notes |
|----------|--------|-------|
| `text-align` | 🟡 | start, center, end (no justify) |
| `text-align-last` | ❌ | Not implemented |
| `text-indent` | ✅ | Supported |
| `white-space-collapse` | 🟡 | Partial |
| `white-space-treatment` | 🟡 | Partial |
| `wrap-option` | ❌ | Not implemented |
| `hyphenate` | ❌ | No hyphenation |
| `hyphenation-*` | ❌ | All hyphenation props |
| `line-height` | ✅ | Supported |
| `text-decoration` | ✅ | Underline, etc. |
| `color` | ✅ | Text color |

### BiDi

| Property | Status | Notes |
|----------|--------|-------|
| `direction` | 🟡 | On bidi-override only |
| `writing-mode` | 🟡 | lr-tb only |
| `unicode-bidi` | ❌ | Not implemented |

### Positioning

| Property | Status | Notes |
|----------|--------|-------|
| `absolute-position` | ❌ | Not implemented |
| `top`, `right`, `bottom`, `left` | ❌ | Not implemented |
| `z-index` | ❌ | Not implemented |
| `reference-orientation` | ❌ | No rotation |
| `display-align` | ❌ | No vertical centering |

### Backgrounds

| Property | Status | Notes |
|----------|--------|-------|
| `background-color` | ✅ | Solid colors |
| `background-image` | ❌ | Not implemented |
| `background-repeat` | ❌ | Not implemented |
| `background-position` | ❌ | Not implemented |
| `background-attachment` | ❌ | Not implemented |

### Tables

| Property | Status | Notes |
|----------|--------|-------|
| `table-layout` | ✅ | auto and fixed |
| `table-omit-header-at-break` | ❌ | No header repeat |
| `table-omit-footer-at-break` | ❌ | Not implemented |
| `border-collapse` | ✅ | collapse and separate |
| `border-spacing` | ✅ | Supported |
| `column-width` | 🟡 | Explicit only |
| `number-columns-repeated` | ✅ | Supported |
| `number-columns-spanned` | ✅ | Supported |
| `number-rows-spanned` | ❌ | Parsed, not rendered |
| `empty-cells` | ❌ | Not implemented |

### Other

| Property | Status | Notes |
|----------|--------|-------|
| `id` | ✅ | Link destinations |
| `visibility` | ❌ | Not implemented |
| `clip` | ❌ | Not implemented |
| `overflow` | ❌ | Not implemented |
| `change-bar-*` | ❌ | All change bar props |

## Feature Completion by Area

| Area | Coverage | Details |
|------|----------|---------|
| **Document Structure** | ~95% | [page-breaking-pagination.md](page-breaking-pagination.md) |
| **Block Layout** | ~85% | [line-breaking-text-layout.md](line-breaking-text-layout.md) |
| **Inline Layout** | ~75% | [fonts-typography.md](fonts-typography.md) |
| **Tables** | ~60% | [tables.md](tables.md) ⚠️ |
| **Lists** | ~90% | Well supported |
| **Images** | ~70% | [images.md](images.md) |
| **Links/Bookmarks** | ~95% | Well supported |
| **Fonts** | ~40% | [fonts-typography.md](fonts-typography.md) ⚠️ |
| **BiDi** | ~20% | [bidi-text-support.md](bidi-text-support.md) ⚠️ |
| **Positioning** | ~25% | [positioning-layout.md](positioning-layout.md) ⚠️ |
| **Page Breaking** | ~40% | [page-breaking-pagination.md](page-breaking-pagination.md) ⚠️ |
| **PDF Rendering** | ~70% | [rendering.md](rendering.md) |

**Overall Compliance**: ~65-70% of XSL-FO 1.1 specification

⚠️ = Significant limitations

## Critical Missing Features

These features significantly limit real-world usability:

1. **Table page breaking** - Cannot render multi-page tables
2. **Text justification** - Professional typography limited
3. **TrueType/OpenType fonts** - Only 14 standard fonts
4. **Row spanning in tables** - Complex table layouts impossible
5. **Full BiDi support** - RTL languages may render incorrectly
6. **Hyphenation** - Poor line breaking in narrow columns
7. **Keep-with-next/previous** - Cannot keep headings with content
8. **Absolute positioning** - No complex layouts
9. **Table header repetition** - Multi-page tables lack context
10. **Widow/orphan control** - Unprofessional typography

See individual limitation documents for details and workarounds.

## References

1. **XSL-FO 1.1 Specification**:
   - https://www.w3.org/TR/xsl11/
   - Complete reference for all features

2. **XSL-FO Conformance**:
   - https://www.w3.org/TR/xsl11/#fo-sec-conformance
   - Defines Basic vs Complete conformance levels

## See Also

- [line-breaking-text-layout.md](line-breaking-text-layout.md)
- [bidi-text-support.md](bidi-text-support.md)
- [page-breaking-pagination.md](page-breaking-pagination.md)
- [positioning-layout.md](positioning-layout.md)
- [fonts-typography.md](fonts-typography.md)
- [images.md](images.md)
- [tables.md](tables.md)
- [advanced-features.md](advanced-features.md)
- [security-validation.md](security-validation.md)
- [performance.md](performance.md)
- [rendering.md](rendering.md)
