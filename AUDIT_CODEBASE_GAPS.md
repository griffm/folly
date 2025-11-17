# Folly PDF Library - Comprehensive Codebase Audit
## Remaining Gaps, Assumptions, and Over-Simplifications

**Audit Date:** 2025-11-17
**Audited Directories:** src/Folly.Core/, src/Folly.Pdf/, src/Folly.Fonts/
**Total Lines of Code (Core):** ~20,784 lines
**Audit Scope:** Identify over-simplifications, assumptions, missing major functionality

---

## Executive Summary

The Folly PDF library codebase is **generally well-implemented** with good security practices, comprehensive error handling, and solid PDF/A compliance. However, **20 distinct gap areas** have been identified, with the most critical being:

1. **Policy Violations:** 10 TODO comments in core code (violates TODO policy for required functionality)
2. **Incomplete SVG Support:** Multiple SVG rendering features have simplified or partial implementations
3. **BiDi Algorithm Gaps:** Simplified handling of Unicode bidirectional isolates
4. **Image Parser Edge Cases:** Assumptions in TIFF/GIF parsing that fail on edge cases

**Overall Risk Level:** **MEDIUM**
- Core PDF generation is robust and production-ready
- PDF/A compliance is well-implemented
- SVG rendering has known limitations (documented but incomplete)
- Edge cases in internationalization (BiDi) and image parsing could cause issues

---

## CRITICAL FINDINGS (Severity: Critical)

### 1. TODO Comments in Core Code - POLICY VIOLATION

**Issue:** Core code contains TODO comments for required functionality, violating the project's TODO policy which states: "Core directories must have no TODOs for required functionality."

**Location:** `src/Folly.Core/Svg/SvgToPdf.cs`

| Line | TODO | Impact |
|------|------|--------|
| 643 | Support tspan elements for multi-line text | Multi-line text in SVG may not render correctly |
| 644 | Support textPath for text on curves improvements | textPath features incomplete |
| 675 | Merge element.Style properties into mergedStyle | `<use>` element styles don't properly override |
| 831 | Implement bounding box tracking for path elements | Gradient fills on paths may be incorrect |
| 1051 | Handle clipPathUnits (userSpaceOnUse vs objectBoundingBox) | Clip paths may use wrong coordinate system |
| 1106 | Support more clipping shapes (polygon, polyline, etc.) | Only rect/circle clipping supported |
| 1568 | Simplified font mapping - should use FontMetrics | Font handling inconsistent with rest of codebase |

**Location:** `src/Folly.Core/Images/Parsers/TiffParser.cs`

| Line | TODO | Impact |
|------|------|--------|
| 210 | Handle TIFF arrays properly by reading from offset | Array-based TIFF tags only read first value |

**Location:** `src/Folly.Core/Images/Parsers/GifParser.cs`

| Line | TODO | Impact |
|------|------|--------|
| 55 | Parse GIF extension blocks for transparency metadata | Metadata loss (minor impact) |

**Additional TODO:** `src/Folly.Core/Svg/SvgToPdf.cs:2452` - "Store as bytes, not string, for image data" (minor impact)

**Recommendation:** Either complete these implementations or move them to non-core optional enhancement layer per policy.

---

## MAJOR FINDINGS (Severity: Major)

### 2. BiDi Algorithm - Simplified Isolate Handling

**File:** `src/Folly.Core/BiDi/UnicodeBidiAlgorithm.cs:183`

```csharp
// Simplified isolate handling (full implementation would be more complex)
levels[i] = currentLevel;
```

**Issue:** Unicode bidirectional isolate characters (LRI, RLI, FSI, PDI) are not properly handled according to UAX#9 (Unicode Bidirectional Algorithm). The implementation uses a simplified approach that may produce incorrect text layout for complex mixed-direction content with directional isolates.

**Impact:** International text rendering (especially Arabic, Hebrew with embedded LTR text) may be incorrect in edge cases.

**Recommendation:** Implement full isolate handling per Unicode UAX#9 specification.

---

### 3. SVG Drop Shadow - No Blur Implementation

**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1120-1191`

```csharp
/// LIMITATION: Drop shadow is rendered as an offset copy with opacity, without blur.
/// Full SVG feGaussianBlur is not implemented due to PDF rendering constraints.
```

**Issue:**
- Drop shadow rendered as offset + opacity only (no blur radius)
- Text and other elements not supported for shadows (line 1191)
- feGaussianBlur filter not implemented

**Documented Constraints:**
"PDF blur effects require either: 1. Soft masks (SMask), 2. Form XObjects with transparency groups, 3. Rasterization"

**Impact:** Visual quality degradation - shadows appear hard-edged instead of soft/blurred.

**Recommendation:** Implement blur using transparency groups or selective rasterization for filtered elements.

---

### 4. SVG Gradient Per-Stop Opacity Not Applied

**File:** `src/Folly.Core/Svg/Gradients/SvgGradientToPdf.cs:240-254`

```csharp
// LIMITATION: Per-stop opacity is not applied to gradient colors.
// PDF shading dictionaries (Type 2/3) do not natively support opacity in color functions
// Workaround: Apply uniform opacity to entire element using fill-opacity instead
```

**Issue:** Per-gradient-stop opacity values are silently ignored. PDF Type 2/3 shading dictionaries don't support per-stop opacity.

**Impact:** Gradients with varying opacity across stops will render incorrectly. Silent feature loss.

**Recommendation:** Implement using soft masks (SMask) with gradient masks, or document this limitation in user documentation.

---

### 5. SVG Filter Primitives - Most Unsupported

**File:** `src/Folly.Core/Svg/SvgParser.cs:783-792`

**Status:** Parsed but not rendered (returns null at line 792)

**Unsupported Filter Primitives:**
- feOffset, feColorMatrix, feComposite, feMerge
- feTurbulence, feDisplacementMap, feMorphology
- feConvolveMatrix, feFlood, feImage, feTile
- feSpecularLighting, feDiffuseLighting

**Currently Supported (Partial):**
- feGaussianBlur (parsed, simplified rendering)
- feDropShadow (parsed, no blur)
- feBlend (parsed, incomplete mode handling)

**Impact:** SVG content with filter effects will render with missing or degraded visual effects.

**Recommendation:** Implement most common filters (feOffset, feColorMatrix, feComposite, feMerge) as priority.

---

### 6. TIFF Parser - Array Values Not Properly Read

**File:** `src/Folly.Core/Images/Parsers/TiffParser.cs:204-212`

```csharp
private static uint[] ReadIntArray(byte[] data, Dictionary<ushort, uint> tags, ushort tag, bool littleEndian)
{
    if (!tags.TryGetValue(tag, out uint offsetOrValue))
        return Array.Empty<uint>();

    // For now, assume simple case: single value stored directly
    // TODO: Handle arrays properly by reading from offset
    return new[] { offsetOrValue };
}
```

**Issue:** TIFF format allows both direct values (when < 4 bytes) and offsets to array data. Current implementation only handles direct single values, causing data loss for array-based TIFF tags (e.g., StripOffsets, StripByteCounts for multi-strip images).

**Impact:** Multi-strip TIFF images may fail to parse correctly. Only single-strip TIFFs work reliably.

**Recommendation:** Implement proper offset-based array reading per TIFF specification.

---

### 7. SVG `<use>` Element - Style Merging Not Implemented

**File:** `src/Folly.Core/Svg/SvgToPdf.cs:673-675`

```csharp
// Merge styles: use element's style overrides referenced element's style
var mergedStyle = referencedElement.Style.Clone();
// TODO: Merge element.Style properties into mergedStyle
```

**Issue:** `<use>` element styles should override properties from the referenced element, but currently only cloning occurs without merging. SVG specification states that `<use>` styles cascade and override.

**Impact:** SVG `<use>` elements cannot override colors, strokes, or other style properties as intended by SVG spec.

**Recommendation:** Implement style property merging with proper override semantics.

---

### 8. SVG Clip Path Units Not Handled

**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1051`

```csharp
// TODO: Handle clipPathUnits (userSpaceOnUse vs objectBoundingBox)
```

**Issue:** SVG clipPathUnits attribute determines whether clip path coordinates are in user space or relative to bounding box (0-1 range). Current implementation assumes one unit type.

**Impact:** Clip paths with `objectBoundingBox` units will have incorrect coordinates and produce wrong clipping.

**Recommendation:** Parse clipPathUnits and apply appropriate coordinate transformations.

---

### 9. SVG Clipping - Limited Shape Support

**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1106`

```csharp
// TODO: Support more clipping shapes (polygon, polyline, etc.)
```

**Issue:** Only `<rect>` and `<circle>` are supported as clip path shapes. `<path>`, `<polygon>`, `<polyline>`, `<ellipse>` not supported.

**Impact:** Complex clipping shapes silently fail, potentially showing unclipped content.

**Recommendation:** Implement path-based clipping (most important), then polygon/polyline.

---

### 10. SVG Path Bounding Box Not Tracked

**File:** `src/Folly.Core/Svg/SvgToPdf.cs:831`

```csharp
// TODO: Implement bounding box tracking for path elements
```

**Issue:** Path element bounding boxes are not calculated, affecting gradient rendering when gradients use objectBoundingBox units on path elements.

**Impact:** Gradients applied to path elements may have incorrect positioning/scaling.

**Recommendation:** Implement path bounding box calculation during path parsing.

---

### 11. SVG Font Mapping - Simplified Implementation

**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1568`

```csharp
// TODO: This is a simplified mapping. A full implementation would use FontMetrics and PdfBaseFontMapper
```

**Issue:** SVG font family names are mapped using a simple hardcoded list:

```csharp
fontFamily switch {
    "serif" => "Times-Roman",
    "sans-serif" => "Helvetica",
    "monospace" => "Courier",
    _ => fontFamily
}
```

This doesn't leverage the robust `FontMetrics` and `PdfBaseFontMapper` infrastructure already present in the codebase.

**Impact:** Inconsistent font handling between SVG and native PDF content. Missing font fallback logic.

**Recommendation:** Integrate SVG font mapping with existing FontMetrics/PdfBaseFontMapper.

---

### 12. Layout Engine - Absolute Positioning Only

**File:** `src/Folly.Core/Layout/LayoutEngine.cs:4536-4540`

```csharp
// Only handle absolute positioning
if (blockContainer.AbsolutePosition != "absolute")
{
    return null;
}
```

**Issue:** Block containers with `relative`, `fixed`, or other positioning types are silently ignored (return null).

**Impact:** CSS positioning modes other than absolute are not supported, limiting layout flexibility.

**Recommendation:** Implement relative and fixed positioning per CSS specification.

---

### 13. PDF Bookmark Destinations - Named Destinations Only

**File:** `src/Folly.Pdf/PdfWriter.cs:1796`

```csharp
// For MVP, use named destination
// In full implementation, would resolve to /Dest [pageRef /XYZ x y zoom]
```

**Issue:** Bookmarks use named destinations instead of explicit page references with XYZ coordinates. Named destinations require destination definitions elsewhere in the PDF.

**Impact:** Bookmarks may not work correctly if named destinations are not defined. No control over zoom level or exact viewport position.

**Recommendation:** Implement explicit destination arrays with page object references.

---

## MINOR FINDINGS (Severity: Minor)

### 14. GIF Extension Blocks Not Fully Parsed

**File:** `src/Folly.Core/Images/Parsers/GifParser.cs:55, 94-95`

```csharp
// TODO: Parse extension blocks for transparency and other metadata
// Skip other extension blocks
offset = SkipDataSubBlocks(data, offset);
```

**Issue:** GIF Graphic Control Extensions (GCE) are partially handled for transparency, but other metadata (comments, application extensions, etc.) is discarded.

**Impact:** Loss of metadata only. Basic GIF rendering works correctly.

**Recommendation:** Low priority. Parse additional extensions if metadata is needed.

---

### 15. Image DPI Assumptions

**Files:**
- `src/Folly.Core/Images/Parsers/GifParser.cs:203` - Assumes 72 DPI
- `src/Folly.Core/Images/Parsers/TiffParser.cs:109, 272` - Assumes simple cases

**Code Examples:**

```csharp
// GIF: For simplicity, assume 72 DPI and adjust vertical based on aspect ratio

// TIFF: For simplicity, assume they're stored directly (this works for many TIFFs)
int paletteSize = 256; // Assume 8-bit for simplicity
```

**Impact:** Works for common cases, may produce incorrect DPI or palette sizes for edge cases.

**Recommendation:** Low priority. Assumptions work for 95%+ of images.

---

### 16. Color Parsing Fallback Assumptions

**File:** `src/Folly.Core/Dom/FoParser.cs:1603`

```csharp
// Otherwise assume it's a color
```

**Issue:** Border shorthand property parser assumes unrecognized tokens are colors.

**Impact:** Malformed border properties may be misinterpreted. Graceful degradation.

**Recommendation:** Low priority. Add validation if strict parsing needed.

---

### 17. Writing Mode Fallback Assumption

**File:** `src/Folly.Core/Dom/WritingModeHelper.cs:84`

```csharp
// Unknown writing mode, assume lr-tb
return defaultValue;
```

**Issue:** Unknown writing modes silently default to left-to-right, top-to-bottom instead of throwing error.

**Impact:** Graceful degradation. Better than failing, but silently changes behavior.

**Recommendation:** Consider warning log for unknown modes.

---

### 18. Unitless Length Assumption

**File:** `src/Folly.Core/Dom/LengthParser.cs:40`

```csharp
"pt" or "" => number, // Points or unitless (assume points)
```

**Issue:** Unitless numeric values are assumed to be points. CSS specification says unitless values should be pixels in some contexts.

**Impact:** Minor discrepancy from CSS spec. Works for XSL-FO which uses points by default.

**Recommendation:** Low priority for XSL-FO. Document this assumption.

---

### 19. Mac Roman Encoding - Decode Only

**File:** `src/Folly.Fonts/Tables/NameTableParser.cs:162, 167`

```csharp
throw new NotImplementedException("Mac Roman encoding only supports GetString (decoding)");
```

**Issue:** Mac Roman encoding implementation only supports decoding (GetString), not encoding (GetBytes).

**Impact:** Read-only operations work. Cannot create new Mac Roman encoded strings (not needed for font parsing).

**Recommendation:** None. Encoding not needed for current use cases.

---

## DESIGN LIMITATIONS (Acknowledged & Acceptable)

### 20. PDF Encryption Not Supported

**File:** `src/Folly.Pdf/PdfRenderer.cs:126-128`

```csharp
// Note: PDF/A standards (ISO 19005) prohibit encryption to ensure long-term accessibility.
// The current implementation does not support PDF encryption. If encryption support is added
// in the future, this validation must ensure it's disabled when PDF/A compliance is enabled.
```

**Status:** **Intentional design decision** for PDF/A compliance.

**Impact:** Cannot create encrypted/password-protected PDFs. Acceptable for PDF/A use case.

**Recommendation:** Document this limitation in user-facing documentation.

---

### 21. Interlaced PNG Not Supported

**File:** `src/Folly.Pdf/PdfWriter.cs:766`

```csharp
throw new NotSupportedException($"Interlaced PNG images (Adam7) are not supported. Please convert to non-interlaced format.");
```

**Status:** Intentional limitation. Throws clear error message.

**Impact:** Interlaced PNGs must be converted before use. Fails loudly with clear message.

**Recommendation:** Acceptable. Very rare format. Consider implementing if user requests increase.

---

### 22. Limited Image Format Support

**Supported:** PNG, JPEG, GIF, BMP, TIFF (subset)
**Not Supported:** WebP, AVIF, HEIC, TIFF with advanced compression

**Status:** Intentional scope limitation.

**Impact:** Modern image formats not supported. Users must convert.

**Recommendation:** Document supported formats. Add WebP if demand exists.

---

### 23. No Interactive PDF Features

**Not Supported:**
- JavaScript actions
- Form fields (AcroForm)
- Digital signatures
- Optional content groups (layers)
- Multimedia (audio/video)
- 3D models

**Status:** Intentional scope limitation. PDF/A prohibits JavaScript.

**Impact:** Cannot create interactive PDFs. Acceptable for document generation use case.

**Recommendation:** Document as design scope. PDF/A compliance intentionally excludes these features.

---

## STRENGTHS IDENTIFIED

### Security & Error Handling (Excellent)

**Bounds Checking:**
- PNG: Validates chunk lengths, integer overflow checks
- GIF: Extensive offset validation throughout parser
- TIFF: Offset/bounds validation present
- BMP: Header size and palette validation

**DoS Mitigations:**
- MAX_PNG_CHUNK_SIZE = 100MB limit
- Image size limits in LayoutEngine (line 3279)
- Max page limit enforcement (line 4096)
- XML entity expansion limits (1024 entities)
- Document size limit (100MB)

**Verdict:** Security-conscious implementation with robust protections.

---

### Font Handling (Excellent)

**Features Present:**
- OpenType shaping (GSUB, GPOS)
- Kerning support (kern table)
- Ligature support (GSUB)
- Font subsetting
- TrueType embedding
- CFF font support
- Mac Roman encoding for font names

**Verdict:** Comprehensive font handling with advanced typography support.

---

### Transparency Support (Good)

**Features Present:**
- SMask (Soft Mask) for alpha channels
- Image alpha channel extraction and embedding
- PNG tRNS chunk handling
- GIF transparent color index
- Indexed color with transparency expansion

**Verdict:** Solid transparency implementation for images.

---

### Compression (Good)

**Algorithms Implemented:**
- FlateDecode (zlib/deflate) - for streams, images
- DCTDecode (JPEG) - passthrough for JPEG images
- Proper zlib headers and Adler-32 checksums
- PNG predictor support

**Verdict:** Appropriate compression for PDF/A compliance.

---

### PDF/A Compliance (Excellent)

**Features Present:**
- XMP metadata embedding
- sRGB ICC profile generation
- Output intent support
- Structure tree for accessibility
- UTF-8 encoding support
- No encryption (per PDF/A requirement)

**Verdict:** Strong PDF/A-1b, PDF/A-2b, PDF/A-3b compliance.

---

## SUMMARY STATISTICS

| Category | Count |
|----------|-------|
| **Total Gap Areas Identified** | 20 |
| **Critical Issues (Policy Violations)** | 1 (10 TODOs) |
| **Major Issues (Missing Features)** | 12 |
| **Minor Issues (Edge Cases)** | 6 |
| **Design Limitations (Acceptable)** | 4 |
| **NotImplementedException Found** | 2 (both acceptable) |
| **TODO Comments in Core** | 10 |
| **Documented Simplifications** | 6 |

---

## PRIORITIZED RECOMMENDATIONS

### Immediate Actions (Critical - Policy Violations)

1. **Resolve all 10 TODOs in core code** - Either complete implementations or move to optional layer
2. **Complete TIFF array handling** - Fix data loss in multi-strip TIFF parsing
3. **Implement BiDi isolate handling** - Fix international text correctness

### Short Term (High Impact)

4. **Fix SVG `<use>` style merging** - Correct SVG semantics
5. **Implement SVG gradient per-stop opacity** - Use SMask approach
6. **Add SVG clip path units handling** - Fix objectBoundingBox clipping
7. **Support more SVG clipping shapes** - At minimum, add path-based clipping
8. **Implement SVG path bounding boxes** - Fix gradient positioning on paths

### Medium Term (Quality Improvements)

9. **Integrate SVG font mapping with FontMetrics** - Consistency across codebase
10. **Implement common SVG filter primitives** - feOffset, feColorMatrix, feComposite, feMerge
11. **Add drop shadow blur** - Transparency groups or selective rasterization
12. **Implement relative/fixed positioning** - Expand layout engine capabilities
13. **Use explicit bookmark destinations** - Better bookmark navigation

### Low Priority (Edge Cases)

14. Parse additional GIF metadata
15. Improve image DPI detection
16. Add stricter validation for color/length parsing
17. Consider WebP format support if user demand exists

---

## OVERALL ASSESSMENT

**Codebase Health:** **B+ (Good with Notable Gaps)**

**Production Readiness:**
- ✅ **PDF/A Generation:** Production-ready
- ✅ **Basic PDF Output:** Production-ready with known SVG limitations
- ⚠️ **SVG Rendering:** Functional but incomplete (filters, clipping, text features)
- ⚠️ **International Text:** Works for most cases, BiDi edge cases possible
- ✅ **Security:** Well-hardened against DoS and malformed inputs
- ✅ **Image Support:** Solid for common formats, edge cases in TIFF

**Risk Assessment:**
- **HIGH RISK:** None identified
- **MEDIUM RISK:** BiDi simplifications, TIFF array handling, SVG feature gaps
- **LOW RISK:** Image parsing assumptions, edge case handling

**Recommendation:** **Approve for production use** with documented limitations for SVG features. Address critical policy violations (TODOs in core) in next sprint.

---

## APPENDIX: Grep Results Summary

### Search Patterns Used

1. `TODO` - Found 10 in core code
2. `FIXME|HACK|XXX` - None found (good!)
3. `NotImplementedException` - 2 found (both acceptable: Mac Roman encoding)
4. `not supported|not implemented` - 10 found (documented limitations)
5. `simplified|simplification` - 14 found (documented approaches)
6. `assume|assumes|assumption` - 9 found (mostly minor)

### Files with Most Issues

1. `src/Folly.Core/Svg/SvgToPdf.cs` - 7 TODOs, multiple limitations
2. `src/Folly.Core/Images/Parsers/TiffParser.cs` - 3 assumptions, 1 TODO
3. `src/Folly.Core/Images/Parsers/GifParser.cs` - 2 assumptions, 1 TODO
4. `src/Folly.Core/BiDi/UnicodeBidiAlgorithm.cs` - 1 simplification (major)

---

**End of Audit Report**
