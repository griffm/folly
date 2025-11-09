# PDF Rendering Limitations

## Overview

Folly renders to PDF 1.7 with good support for basic graphics, text, and images. However, several advanced PDF features and visual effects are not yet implemented.

## Current Rendering Capabilities

**Location**: `src/Folly.Pdf/PdfRenderer.cs`

**Implemented**:
- PDF 1.7 structure
- Standard Type 1 font embedding
- Font subsetting (only used glyphs)
- Text positioning with baseline alignment
- Border rendering (solid, dashed, dotted)
- Background colors (solid fills)
- Image embedding (JPEG passthrough, PNG decoding)
- Flate (zlib) compression
- Hyperlinks (internal and external)
- Bookmarks (PDF outline)
- Metadata (Document Information Dictionary)

## Limitations

### 1. No Gradients

**Severity**: Medium
**PDF Feature**: Shading patterns (Type 2, 3)

**Description**:
- Only solid colors supported
- No linear gradients
- No radial gradients
- No mesh gradients

**Impact**:
- Cannot create smooth color transitions
- Limited visual effects
- Modern UI designs impossible

**Example Not Supported**:
```xml
<fo:block background-color="linear-gradient(red, blue)">
  <!-- Gradient background -->
</fo:block>
```

**PDF Implementation** (not done):
```
/Shading << /ShadingType 2  % Linear gradient
            /ColorSpace /DeviceRGB
            /Coords [0 0 100 0]  % x0 y0 x1 y1
            /Function <<...>>
>>
```

**Complexity**: Medium

### 2. No Rounded Corners

**Severity**: Low
**PDF Feature**: Bezier curves for rounded rectangles

**Description**:
- All borders are sharp rectangles
- No border-radius support
- Cannot create rounded boxes

**Current**:
```csharp
// Render rectangle
stream.WriteLine($"{x} {y} {width} {height} re");
stream.WriteLine("S");  // Stroke
```

**With Rounded Corners** (not implemented):
```csharp
// Would use Bezier curves to approximate rounded corners
// m, l, c, l, c, ... (moveto, lineto, curveto)
```

**Example Not Supported**:
```xml
<fo:block border="1pt solid black" border-radius="5pt">
  Rounded box
</fo:block>
```

**Complexity**: Low to Medium

### 3. Limited Border Styles

**Severity**: Low
**Supported**: solid, dashed, dotted

**Not Supported**:
- `double` - Two parallel lines
- `groove` - 3D grooved border
- `ridge` - 3D ridged border
- `inset` - 3D inset border
- `outset` - 3D outset border

**Current Implementation** (`PdfRenderer.cs`):
```csharp
switch (borderStyle)
{
    case "dashed":
        stream.WriteLine("[3 2] 0 d");  // Dash pattern
        break;
    case "dotted":
        stream.WriteLine("[1 1] 0 d");  // Dot pattern
        break;
    default:  // solid
        stream.WriteLine("[] 0 d");
        break;
}
```

**Impact**: Minor - most borders use solid style

**Complexity**: Low (double), Medium (3D effects)

### 4. No Transparency/Opacity

**Severity**: Medium
**PDF Feature**: Extended graphics state (alpha channel)

**Description**:
- No alpha transparency
- No opacity control
- All elements fully opaque

**Example Not Supported**:
```xml
<fo:block background-color="rgba(255, 0, 0, 0.5)">
  <!-- 50% transparent red background -->
</fo:block>
```

**PDF Implementation** (not done):
```
/ExtGState << /ca 0.5 >>  % Fill opacity
q  % Save graphics state
/GS1 gs  % Set graphics state
% ... render with opacity ...
Q  % Restore
```

**Impact**: Cannot create overlays, watermarks, fading effects

**Complexity**: Low to Medium

### 5. No Clipping Paths

**Severity**: Low
**PDF Feature**: Clipping operations

**Description**:
- No clipping to arbitrary shapes
- No text clipping
- Cannot mask content

**Use Cases**:
- Clip image to circular shape
- Text as clipping mask
- Complex shape masking

**PDF Operations** (not used):
```
W  % Clip to current path
W* % Clip to current path (even-odd rule)
```

**Example Not Supported**:
```xml
<fo:block clip-path="circle(50%)">
  <fo:external-graphic src="image.jpg"/>
  <!-- Image clipped to circle -->
</fo:block>
```

**Complexity**: Medium

### 6. No Filters/Effects

**Severity**: Low
**Effects**: blur, drop shadow, etc.

**Description**:
- No CSS-style filters
- No blur effects
- No drop shadows
- No color adjustments

**Example Not Supported**:
```xml
<fo:block filter="drop-shadow(2px 2px 5px black)">
  Text with shadow
</fo:block>
```

**Reality**: PDF has limited built-in filter support
- Would need pre-rendering effects to raster
- Or implement as PDF soft mask/transparency group

**Complexity**: Very High

### 7. No Patterns/Textures

**Severity**: Low
**PDF Feature**: Tiling patterns

**Description**:
- No repeating patterns for fills
- No hatching
- No texture fills

**Example Not Supported**:
```xml
<fo:block background-pattern="diagonal-stripe">
  Striped background
</fo:block>
```

**PDF Implementation** (not done):
```
/Pattern << /PatternType 1  % Tiling pattern
            /PaintType 1
            /TilingType 1
            /BBox [0 0 10 10]
            /XStep 10
            /YStep 10
>>
```

**Use Cases**:
- Hatched backgrounds
- Engineering drawings
- Decorative patterns

**Complexity**: Medium

### 8. No Color Spaces Beyond RGB

**Severity**: Medium for professional printing
**Supported**: DeviceRGB only

**Not Supported**:
- **DeviceCMYK** - For print (cyan, magenta, yellow, black)
- **DeviceGray** - Grayscale
- **ICC Color Profiles** - Device-independent color
- **Lab Color Space** - Perceptually uniform
- **Separation** - Spot colors (Pantone, etc.)

**Impact**:
- Cannot prepare PDFs for professional printing
- Colors may not match expectations
- No spot color support

**Current**:
```csharp
// Always RGB
stream.WriteLine($"{r} {g} {b} rg");  // Fill
stream.WriteLine($"{r} {g} {b} RG");  // Stroke
```

**For CMYK** (not implemented):
```csharp
stream.WriteLine($"{c} {m} {y} {k} k");  // Fill
stream.WriteLine($"{c} {m} {y} {k} K");  // Stroke
```

**Complexity**: Low (CMYK), High (ICC profiles)

### 9. No Overprint Control

**Severity**: Low (professional printing)
**PDF Feature**: Overprint mode

**Description**:
- No control over ink overlap in printing
- Important for spot colors
- Affects how colors mix on press

**PDF Operators**:
```
/OPM 1  % Overprint mode
true setoverprint  % Enable overprint
```

**Impact**: Professional print workflows only

**Complexity**: Low

### 10. No Soft Masks

**Severity**: Low
**PDF Feature**: Soft mask (alpha channel image)

**Description**:
- Cannot use grayscale image as transparency mask
- No alpha channel masking
- No luminosity masks

**Use Case**:
- Fade image edges
- Complex transparency shapes
- Photographic compositing

**Complexity**: Medium

## Graphics State Management

**Current**:
- Basic save/restore (q/Q operators)
- Line width, dash pattern, color
- Transformation matrices (CTM)

**Not Tracked**:
- Line cap/join styles (always default)
- Miter limit (always default)
- Flatness tolerance
- Rendering intent

**Impact**: Minor - defaults work for most cases

## Text Rendering

**Current**:
- Horizontal text only
- Single-byte encoding (WinAnsiEncoding)
- Kerning not applied
- No ligatures

**Limitations** (from fonts-typography.md):
- No vertical text
- No complex script shaping
- No advanced typography features

## Z-Order / Layering

**Current**: Content rendered in DOM order

**Description**:
- No z-index support
- Cannot reorder rendering independent of DOM
- Overlapping elements render in source order

**Impact**: Limited control over visual stacking

## Annotations

**Implemented**:
- Link annotations (internal/external)

**Not Implemented**:
- Text annotations (sticky notes)
- Highlight annotations
- Ink annotations (freehand drawing)
- File attachment annotations
- Sound/movie annotations
- 3D annotations
- Redaction annotations

**Impact**: Limited PDF interactivity

## Forms

**Severity**: Medium for interactive PDFs
**Description**: No AcroForm or XFA form support

**Not Implemented**:
- Text fields
- Checkboxes
- Radio buttons
- Dropdown lists
- Buttons

**Impact**: Cannot create fillable forms

**Complexity**: Very High

## JavaScript

**Severity**: Low
**Description**: No embedded JavaScript support

**PDF Capability**: PDF supports embedded JavaScript for:
- Form validation
- Calculations
- Interactive behaviors
- Page actions

**Current**: No JavaScript embedding

**Impact**: Purely static documents

**Complexity**: Medium to High

## Tagged PDF / Accessibility

**Severity**: Medium for accessibility
**Description**: No PDF structure tags

**Missing**:
- Structure tree (tagged PDF)
- Role mapping (heading, paragraph, list)
- Alt text for images
- Reading order
- Language tags

**Impact**:
- PDFs not accessible to screen readers
- Cannot meet accessibility requirements (Section 508, WCAG)
- No reflow support

**Complexity**: High

**Related**: PDF/UA (Universal Accessibility) standard

## PDF/A Compliance

**Severity**: Medium for archival
**Description**: No PDF/A (archival format) support

**PDF/A Requirements**:
- All fonts embedded
- No encryption
- No external dependencies
- Specific metadata
- Color profiles included

**Current**: Basic PDF 1.7, not PDF/A compliant

**Use Case**: Long-term archival, legal documents

**Complexity**: Medium

## Optimization

**Implemented**:
- ✅ Flate compression
- ✅ Font subsetting
- ✅ JPEG passthrough (no recompression)

**Not Implemented**:
- Object deduplication
- Image downsampling
- Optimal object ordering
- Linearization (fast web view)
- Cross-reference streams (compressed)

**Impact**: PDFs larger than optimal

**File Size Comparison**:
- With current optimizations: Good
- With all optimizations: Could be 20-30% smaller

## PDF Version

**Current**: PDF 1.7 (2006 standard)

**Later Versions**:
- PDF 1.7 Extension Level 3 (Adobe)
- **PDF 2.0** (ISO 32000-2:2017) - Latest standard

**New Features in PDF 2.0** (not supported):
- Enhanced security
- Better compression
- Improved graphics
- Page piece dictionaries
- New annotation types

**Recommendation**: PDF 1.7 is widely compatible, sufficient for now

## References

1. **PDF Reference 1.7**:
   - Complete PDF specification
   - https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf

2. **PDF 2.0 (ISO 32000-2)**:
   - Latest PDF standard
   - https://www.iso.org/standard/63534.html

3. **PDF/A (ISO 19005)**:
   - Archival PDF standard
   - https://www.pdfa.org/

4. **PDF/UA (ISO 14289)**:
   - Universal Accessibility standard
   - https://www.pdfa.org/pdfua/

## See Also

- [fonts-typography.md](fonts-typography.md) - Font rendering limitations
- [images.md](images.md) - Image rendering
- [positioning-layout.md](positioning-layout.md) - Graphics positioning
