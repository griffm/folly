# Positioning & Layout Limitations

## Overview

Folly implements a flow-based layout model suitable for documents with natural content flow (top-to-bottom, column-based). However, advanced positioning features like absolute positioning, z-ordering, and complex overlapping layouts are not supported.

## Current Implementation

The layout engine uses relative positioning within the flow:
- Block areas positioned sequentially (top-to-bottom)
- Inline areas positioned within lines (left-to-right)
- Tables and lists positioned as atomic blocks
- Floats positioned separately in margins

## Limitations

### 1. No Absolute Positioning

**Severity**: High for complex layouts
**XSL-FO Elements**: `fo:block-container` with `absolute-position`

**Description**:
- `fo:block-container` is parsed but not fully implemented
- Cannot position blocks at specific (x, y) coordinates
- Cannot create overlapping content
- Cannot position elements relative to page edges

**Current Status**:
```csharp
// fo:block-container exists in DOM but lacks layout support
// See: src/Folly.Core/Dom/FoBlockContainer.cs
```

**Impact**:
- Cannot create letterheads with positioned logos
- Cannot create watermarks
- Cannot create complex form layouts
- Cannot position signatures at exact locations
- No magazine-style overlapping layouts

**Example That Doesn't Work**:
```xml
<fo:block-container absolute-position="absolute" top="2in" left="3in">
  <fo:block>This text should be at exactly (3in, 2in)</fo:block>
</fo:block-container>
```
**Current**: Block appears in flow, position properties ignored
**Expected**: Block appears at specified coordinates

**XSL-FO Properties Not Supported**:
- `absolute-position="absolute | fixed"`
- `top`, `bottom`, `left`, `right` on block-containers
- `z-index` for layering

### 2. No Fixed Positioning

**Severity**: Medium
**XSL-FO**: `absolute-position="fixed"`

**Description**:
- Cannot create elements fixed to page viewport
- Cannot create repeating background elements
- Cannot create page overlays

**Use Cases**:
- Watermarks that appear on every page
- Background graphics
- Page borders with decorative elements
- "Confidential" stamps

**Example**:
```xml
<fo:block-container absolute-position="fixed" top="0" left="0">
  <fo:block font-size="72pt" color="#CCCCCC">DRAFT</fo:block>
</fo:block-container>
```
**Expected**: "DRAFT" appears at same position on every page
**Actual**: Not rendered

### 3. No Background Images

**Severity**: Medium
**XSL-FO Property**: `background-image`

**Current Support**: Only solid `background-color`

**Code** (`AreaTree.cs:169`):
```csharp
public string BackgroundColor { get; set; } = "transparent";
// No BackgroundImage property
```

**Impact**:
- Cannot use images as block backgrounds
- Cannot create textured backgrounds
- Cannot create letterheads with background graphics
- Cannot implement watermarks via background

**Example Not Supported**:
```xml
<fo:block background-image="url('logo.png')"
          background-repeat="no-repeat"
          background-position="center">
  Content with background image
</fo:block>
```

**Related Properties Not Supported**:
- `background-image`
- `background-repeat`
- `background-position`
- `background-attachment`

### 4. Simplified Float Implementation

**Severity**: Medium for magazine layouts
**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:1498-1553`

**Current Behavior**:
- Floats rendered separately after main content
- Fixed width (200pt or 1/3 body width)
- No text wrapping around floats
- Floats don't affect flow positioning

**Code**:
```csharp
// Calculate float width (default to 200pt, or 1/3 of body width)
var floatWidth = Math.Min(200, bodyWidth / 3);

// Floats are positioned but don't displace main content
```

**Impact**:
- Cannot create magazine-style layouts with wrapped text
- Floats appear in margins but don't integrate with flow
- Cannot control float width dynamically
- Float positioning is simplistic

**Example**:
```xml
<fo:float float="start">
  <fo:block>
    <fo:external-graphic src="photo.jpg"/>
    <fo:block>Caption text</fo:block>
  </fo:block>
</fo:float>
<fo:block>
  This text should wrap around the floated image, but currently
  it doesn't. The image appears in the margin separately.
</fo:block>
```

**Proposed Enhancement**:
1. Reserve space in flow for float
2. Shorten line length for lines adjacent to float
3. Support dynamic float sizing
4. Implement proper clearing

### 5. No Z-Index / Layering

**Severity**: Low to Medium
**XSL-FO Property**: `z-index`

**Description**:
- No control over rendering order
- Cannot create overlapping elements with controlled stacking
- Content renders in DOM order only

**Impact**:
- Cannot place background behind foreground
- Cannot create layered designs
- Cannot implement visual depth

**Example**:
```xml
<fo:block-container z-index="1">Background</fo:block-container>
<fo:block-container z-index="10">Foreground</fo:block-container>
```
**Expected**: Foreground renders on top regardless of source order
**Actual**: z-index ignored

### 6. No Vertical Alignment in Block Containers

**Severity**: Low
**XSL-FO Property**: `display-align` on `fo:block-container`

**Description**:
- Cannot vertically center content in fixed-height containers
- No support for `display-align="before | center | after | auto"`

**Use Cases**:
- Center text in fixed-height table cells (partially supported via `vertical-align` on cells)
- Center content in full-page blocks
- Align content to bottom of region

**Example**:
```xml
<fo:block-container height="3in" display-align="center">
  <fo:block>This should be vertically centered</fo:block>
</fo:block-container>
```
**Current**: Content appears at top
**Expected**: Content centered vertically

### 7. No Writing Mode Variations

**Severity**: Medium for CJK languages
**XSL-FO Property**: `writing-mode`

**Supported**: Only `lr-tb` (left-to-right, top-to-bottom)

**Not Supported**:
- `rl-tb` - Right-to-left, top-to-bottom (for RTL languages)
- `tb-rl` - Top-to-bottom, right-to-left (traditional CJK)
- `tb-lr` - Top-to-bottom, left-to-right
- `lr` - Left-to-right
- `rl` - Right-to-left
- `tb` - Top-to-bottom

**Impact**:
- Cannot render traditional vertical Chinese/Japanese text
- Cannot render Mongolian script (vertical)
- Cannot create rotated text effects

**Example**:
```xml
<fo:block-container writing-mode="tb-rl">
  <fo:block>日本語の縦書き</fo:block>
</fo:block-container>
```
**Expected**: Text flows top-to-bottom, right-to-left
**Actual**: Text flows normally (left-to-right)

### 8. No Reference Orientation / Rotation

**Severity**: Low
**XSL-FO Property**: `reference-orientation`

**Description**:
- Cannot rotate blocks
- No support for landscape orientation within portrait pages
- Cannot create rotated labels/headers

**Values**: `0 | 90 | 180 | 270 | -90 | -180 | -270`

**Use Cases**:
- Rotate table headers for narrow columns
- Create spine labels
- Rotate page content for wide tables

**Example**:
```xml
<fo:block-container reference-orientation="90">
  <fo:block>Rotated 90 degrees clockwise</fo:block>
</fo:block-container>
```

### 9. No Inline Container Positioning

**Severity**: Low
**XSL-FO Element**: `fo:inline-container`

**Description**:
- `fo:inline-container` parsed but not fully implemented
- Cannot embed block-level content within inline flow with special positioning
- Cannot create inline diagrams or special layouts

### 10. No Regions Beyond Before/After/Body

**Severity**: Medium
**XSL-FO Elements**: `fo:region-start`, `fo:region-end`

**Current Status**:
- `fo:region-start` and `fo:region-end` parsed
- Not rendered in layout engine
- Cannot create left/right sidebars

**Impact**:
- Cannot create margin notes
- Cannot create sidebars
- No support for running sidebars with content

**Example**:
```xml
<fo:simple-page-master master-name="with-sidebar">
  <fo:region-body margin-left="3in" margin-right="1in"/>
  <fo:region-start extent="2.5in"/>  <!-- Left sidebar -->
  <fo:region-end extent="0.75in"/>   <!-- Right margin notes -->
</fo:simple-page-master>

<fo:page-sequence>
  <fo:static-content flow-name="xsl-region-start">
    <fo:block>Sidebar content</fo:block>
  </fo:static-content>
  <!-- ... -->
</fo:page-sequence>
```

**Proposed Implementation**: Relatively straightforward, similar to region-before/after

## Workarounds

### For Positioned Elements
**Current**: Not possible
**Workaround**: Use tables with empty cells to create spacing

### For Background Images
**Current**: Not supported
**Workaround**: Embed image as regular content, position with padding/margins

### For Overlapping Content
**Current**: Not possible
**Workaround**: None - design must avoid overlaps

### For Vertical Centering
**Current**: Limited support
**Workaround**: Use calculated padding to simulate centering

## Proposed Solutions

### Phase 1: Basic Absolute Positioning
1. Implement `absolute-position="absolute"` on `fo:block-container`
2. Support `top`, `left`, `right`, `bottom` properties
3. Allow positioning relative to page viewport
**Complexity**: Medium
**Impact**: High

### Phase 2: Background Images
1. Add `background-image` property parsing
2. Load and embed background images
3. Support `background-repeat` and `background-position`
4. Render backgrounds before content
**Complexity**: Medium
**Impact**: Medium

### Phase 3: Region Start/End
1. Implement `fo:region-start` and `fo:region-end` rendering
2. Layout static-content for these regions
3. Support extent and margin properties
**Complexity**: Low
**Impact**: Medium

### Phase 4: Advanced Features
1. Z-index support for layering
2. Reference-orientation for rotation
3. Writing-mode variations
4. Display-align for vertical alignment
**Complexity**: High
**Impact**: Medium

## XSL-FO Specification Compliance

**Properties Implemented**:
- Basic flow layout - Yes
- `fo:region-body` - Yes
- `fo:region-before` / `fo:region-after` - Yes
- Simple float placement - Partial

**Properties Not Supported**:
- `absolute-position` - Not implemented
- `top`, `left`, `right`, `bottom` - Not implemented
- `z-index` - Not implemented
- `background-image` - Not implemented
- `background-repeat`, `background-position` - Not implemented
- `writing-mode` (beyond lr-tb) - Not implemented
- `reference-orientation` - Not implemented
- `display-align` - Not implemented
- `fo:region-start` / `fo:region-end` rendering - Not implemented
- Full `fo:block-container` support - Partial
- Full `fo:inline-container` support - Partial

**Compliance Level**: ~25% for positioning properties

## References

1. **XSL-FO 1.1 Specification**:
   - Section 6.5.3: `fo:block-container`
   - Section 6.6.11: `fo:float`
   - Section 7.21: Position Properties
   - https://www.w3.org/TR/xsl11/

2. **CSS Positioning**:
   - CSS 2.1 Section 9.3: Positioning schemes
   - https://www.w3.org/TR/CSS2/visuren.html
   - Similar concepts to XSL-FO

## See Also

- [rendering.md](rendering.md) - How positioned content would be rendered
- [page-breaking-pagination.md](page-breaking-pagination.md) - Page-level layout
- [advanced-features.md](advanced-features.md) - Other missing features
