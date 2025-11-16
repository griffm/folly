# Advanced Features Limitations

## Overview

This document covers miscellaneous advanced XSL-FO features that are not yet implemented in Folly, including initial property sets (drop caps), wrappers, characters, and various smaller features.

## Not Implemented Features

### 1. Initial Property Set (Drop Caps)

**Severity**: Medium
**XSL-FO Element**: `fo:initial-property-set`

**Description**:
- No support for styling first letter/line differently
- Cannot create drop caps (large first letter)
- Cannot apply special formatting to paragraph openings

**Current Status**: Element parsed but not applied in layout

**Example Not Supported**:
```xml
<fo:block>
  <fo:initial-property-set font-size="24pt" font-weight="bold"
                           color="red"/>
  This paragraph begins with a large red capital letter.
</fo:initial-property-set>
```

**Expected**: "T" appears at 24pt, bold, red; rest is normal
**Actual**: Entire block uses default properties

**Use Cases**:
- Drop caps in book chapters
- Decorative paragraph openings
- Magazine-style typography

**Implementation Complexity**: Medium
- Apply properties only to first letter or first line
- Handle line breaking around enlarged first letter
- Adjust baseline alignment

### 2. Wrapper Element

**Severity**: Low
**XSL-FO Element**: `fo:wrapper`

**Description**:
- `fo:wrapper` exists and is parsed
- Used for grouping property inheritance
- Should be transparent in layout (contributes no areas)

**Current Status**: Parsed, basic implementation exists

**Use Case**:
```xml
<fo:wrapper color="blue" font-weight="bold">
  <fo:block>This block inherits blue and bold</fo:block>
  <fo:block>So does this one</fo:block>
</fo:wrapper>
```

**Purpose**: Apply properties to multiple children without creating layout structure

**Likely Status**: Probably works since properties inherit, but not explicitly tested

### 3. Character Element

**Severity**: Low
**XSL-FO Element**: `fo:character`

**Description**:
- Represents single character with special properties
- Useful for glyphs, symbols, special formatting

**Current Status**: Parsed but not specially handled in layout

**Example**:
```xml
<fo:block>
  Copyright
  <fo:character character="©" font-family="Symbol"/>
  2024
</fo:block>
```

**Expected**: Renders © symbol from Symbol font
**Current**: Likely ignored or rendered as text node

**Use Cases**:
- Inserting symbols by Unicode code point
- Applying different font to single character
- Special character formatting

### 4. Inline Container

**Severity**: Low
**XSL-FO Element**: `fo:inline-container`

**Description**:
- Embeds block-level content within inline flow
- Creates mini block context inside line

**Current Status**: Parsed but layout support incomplete

**Example**:
```xml
<fo:block>
  This text contains
  <fo:inline-container>
    <fo:block>a block</fo:block>
    <fo:block>inside a line!</fo:block>
  </fo:inline-container>
  and continues.
</fo:block>
```

**Use Cases**:
- Inline diagrams
- Side-by-side content within paragraph
- Complex inline layouts

**Complexity**: High - requires managing block context within inline

### 5. Retrieve Table Marker

**Severity**: Low
**XSL-FO Element**: `fo:retrieve-table-marker`

**Description**:
- Like `fo:retrieve-marker` but specifically for tables
- Retrieves markers from table headers

**Not Implemented**: Table-specific marker retrieval

**Use Case**:
```xml
<fo:table>
  <fo:table-header>
    <fo:marker marker-class-name="chapter">Introduction</fo:marker>
  </fo:table-header>
</fo:table>

<!-- In page header -->
<fo:retrieve-table-marker retrieve-class-name="chapter"/>
```

### 6. Multi-Property Elements

**Severity**: Very Low
**XSL-FO Elements**: `fo:multi-*` family

**Not Implemented**:
- `fo:multi-switch` - Conditional content
- `fo:multi-case` - Switch cases
- `fo:multi-toggle` - Interactive toggling
- `fo:multi-properties` - Multiple property sets
- `fo:multi-property-set` - Property set alternatives

**Description**:
- Advanced conditional rendering
- Multiple versions of content
- Interactive PDF features

**Impact**: Minimal - rarely used, complex to implement

**Example**:
```xml
<fo:multi-switch>
  <fo:multi-case>Print version</fo:multi-case>
  <fo:multi-case>Screen version</fo:multi-case>
</fo:multi-switch>
```

### 7. Index Generation

**Severity**: Medium
**XSL-FO Elements**: `fo:index-*` family

**Not Implemented**:
- `fo:index-page-citation` - Cite page number in index
- `fo:index-page-number-prefix` - Index formatting
- `fo:index-page-number-suffix` - Index formatting
- `fo:index-range-begin` - Start of index range
- `fo:index-range-end` - End of index range

**Description**:
- Automatic index generation
- Back-of-book index with page numbers
- Index ranges

**Impact**: Medium - important for books, but can be generated in XSLT preprocessing

**Workaround**: Generate index in XSLT, use `fo:page-number-citation`

### 8. Change Bars

**Severity**: Low
**XSL-FO Properties**: `change-bar-*`

**Not Implemented**:
- `change-bar-class`
- `change-bar-color`
- `change-bar-offset`
- `change-bar-placement`
- `change-bar-style`
- `change-bar-width`

**Description**:
- Vertical bars in margin indicating changed content
- Useful for document revisions

**Example**:
```xml
<fo:block change-bar-class="revision1"
          change-bar-color="red"
          change-bar-placement="start">
  This content changed in revision 1
</fo:block>
```

**Expected**: Red bar in left margin
**Current**: No rendering

### 9. Conditional Sub-Regions

**Severity**: Low
**XSL-FO Elements**: Various region conditionals

**Description**:
- Different region configurations based on conditions
- More advanced than simple conditional page masters

**Current**: Basic conditional page masters work, but no fine-grained conditional regions

### 10. External Destinations with Parameters

**Severity**: Very Low
**Feature**: Named destinations with view parameters

**Description**:
- Links to specific zoom levels, views in PDF
- E.g., "open at 150% zoom, show page 5"

**Current**: Basic links work, no view control

**Example**:
```xml
<fo:basic-link external-destination="file.pdf#page=5&zoom=150">
  Link with zoom
</fo:basic-link>
```

## Partially Implemented

### 1. Regions

**Implemented**:
- `fo:region-body` - Yes
- `fo:region-before` - Yes
- `fo:region-after` - Yes

**Parsed but Not Rendered**:
- `fo:region-start` - Parsed, not laid out
- `fo:region-end` - Parsed, not laid out

**Impact**: Cannot create sidebars or margin notes

### 2. Markers

**Implemented**:
- `fo:marker` - Yes
- `fo:retrieve-marker` - Yes (basic)

**Limitations**:
- Simplified retrieval position logic
- Not all `retrieve-position` values distinguished properly

**Retrieval Positions** (from spec):
- `first-starting-within-page` - Implemented
- `first-including-carryover` - Treated same as above
- `last-starting-within-page` - Implemented
- `last-ending-within-page` - Treated same as above

### 3. Block Container

**Parsed**: Yes
**Absolute Positioning**: Not implemented
**Display Align**: Not implemented
**Reference Orientation**: Not implemented

See [positioning-layout.md](positioning-layout.md) for details.

## XSL-FO Spec Coverage

**Fully Implemented**:
- Core document structure (root, page-sequence, flow)
- Basic blocks and inlines
- Tables (with limitations)
- Lists
- Images (JPEG/PNG)
- Links and bookmarks
- Static content
- Basic markers
- Page numbers

**Partially Implemented**:
- Regions (body/before/after only)
- Block containers (basic, no positioning)
- BiDi (simple reversal)
- Floats (simplified)
- Footnotes (basic)

**Not Implemented**:
- Multi-* elements
- Index generation
- Change bars
- Initial property sets (drop caps)
- Character element
- Inline containers
- Retrieve table marker
- Complex marker retrieval

**Overall Compliance**: ~65-70% of XSL-FO 1.1 specification

## Proposed Priorities

### High Priority
1. Region-start/end implementation - Common use case
2. Drop caps (initial-property-set) - Professional typography

### Medium Priority
3. Full marker retrieval logic - Edge cases
4. Character element - Symbol insertion

### Low Priority
5. Inline containers - Niche use case
6. Change bars - Specific workflow
7. Multi-* elements - Rarely used
8. Index generation - Can be done in preprocessing

## References

1. **XSL-FO 1.1 Specification**:
   - Full formatting object list
   - https://www.w3.org/TR/xsl11/

2. **XSL-FO Conformance Levels**:
   - Basic vs Complete conformance
   - https://www.w3.org/TR/xsl11/#fo-sec-conformance

## See Also

- [missing-xslfo-features.md](missing-xslfo-features.md) - Complete list of missing features
- [positioning-layout.md](positioning-layout.md) - Positioning limitations
- [page-breaking-pagination.md](page-breaking-pagination.md) - Pagination features
