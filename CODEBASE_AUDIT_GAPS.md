# Codebase Audit Report: Remaining Gaps and Simplifications

**Date:** 2025-11-17
**Audit Scope:** Complete codebase analysis for over-simplifications, assumptions, and missing functionality

---

## Executive Summary

This audit identified **critical gaps** in core functionality that require attention before production deployment. The most significant issues are:

1. **CFF Font Parser** - Drastically simplified with missing glyph extraction, metrics, and charset mapping
2. **Font Subsetting** - Limited to TrueType fonts only; CFF/OpenType fonts not supported
3. **SVG Rendering** - Missing tspan, textPath, and several clipping/filter features
4. **Error Handling** - Multiple bare catch blocks that silently fail without logging
5. **Mac Roman Encoding** - Asymmetric implementation (decode only, no encode)

---

## 1. CRITICAL GAPS (Severity: CRITICAL)

### 1.1 CFF Font Parser - Incomplete Implementation
**File:** `src/Folly.Fonts/Tables/CffTableParser.cs:19-97`

**Issue:** The CFF (Compact Font Format) parser is explicitly labeled as "simplified" with extensive comments listing what's NOT implemented:

- **Missing Features:**
  - CharStrings parsing (Type 2 CharStrings decompilation)
  - Charset mapping (glyph IDs to SIDs)
  - Encoding mapping (char codes to glyph IDs)
  - CIDFont support (FDArray/FDSelect)
  - Width metrics extraction
  - DICT bytecode parsing (TopDict returns default values)

**Lines 70-82 Comment:**
```csharp
// Note: We're implementing a simplified parser here.
// Full CFF parsing requires:
// 1. Parsing CharStrings INDEX
// 2. Parsing Charset
// 3. Parsing Encoding
// 4. For CIDFonts: parsing FDArray, FDSelect
//
// For Phase 8.2, we store the raw CFF data which can be:
// - Embedded directly in PDFs (for non-subset fonts)
// - Used as a base for subsetting operations
//
// CharString parsing (Type 2 CharStrings) is complex and not
// strictly necessary for basic PDF embedding.
```

**Impact:**
- Fonts with CFF outlines may load with incorrect or missing glyph metrics
- Layout errors for CFF-based fonts (common in modern OpenType fonts)
- Cannot properly subset CFF fonts
- Silent failures due to bare catch block (lines 93-97)

**Recommendation:** Complete CFF parser implementation or clearly document limitations

---

### 1.2 Font Subsetting - TrueType Only
**File:** `src/Folly.Fonts/FontSubsetter.cs:29-30`

**Issue:**
```csharp
if (!font.IsTrueType)
    throw new NotSupportedException("Font subsetting is currently only supported for TrueType fonts. OpenType/CFF support coming soon.");
```

**Impact:**
- Cannot subset CFF/OpenType fonts
- PDF files with OpenType fonts will be significantly larger (full font embedding required)
- Combined with incomplete CFF parser, this severely limits OpenType font support

**Recommendation:** Implement CFF subsetting or document workaround for OpenType fonts

---

### 1.3 Silent Error Handling - Bare Catch Blocks
**Files:** Multiple locations identified

**Critical Instances:**

1. **CFF Parser** (`src/Folly.Fonts/Tables/CffTableParser.cs:93-97`):
   ```csharp
   catch
   {
       // If CFF parsing fails, don't crash - just don't populate CFF data
       // This allows fonts with malformed CFF tables to potentially still work
   }
   ```
   **Impact:** CFF parsing failures completely silent; fonts may load with missing data

2. **GSUB Parser** (`src/Folly.Fonts/Tables/GsubTableParser.cs:67-70`):
   ```csharp
   catch
   {
       // If parsing fails, don't crash - just don't populate GSUB data
       // This allows fonts with malformed GSUB tables to still be used
   }
   ```
   **Impact:** Advanced typography features (ligatures, substitutions) silently fail

3. **Font Discovery** (`src/Folly.Fonts/PlatformFontDiscovery.cs:86-89, 116-119`):
   ```csharp
   catch
   {
       // Skip invalid fonts
   }
   ```
   **Impact:** Font loading errors not reported to user

**Recommendation:** Add logging/diagnostics for all catch blocks; expose warnings to users

---

## 2. MAJOR GAPS (Severity: MAJOR)

### 2.1 SVG Rendering - Missing Text Features
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:643-644`

**TODOs:**
```csharp
// TODO: Support tspan elements for multi-line text
// TODO: Support textPath for text on curves
```

**Impact:**
- Multi-line SVG text not rendered
- Text-on-path (common SVG feature) not supported
- Incomplete SVG text rendering capabilities

---

### 2.2 SVG - Incomplete Clipping Support
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1051, 1106`

**Issues:**
1. **clipPathUnits** (line 1051):
   - Only `userSpaceOnUse` supported
   - `objectBoundingBox` not implemented (TODO)

2. **Clipping shapes** (line 1106):
   - Only rect, circle, ellipse, path supported
   - Polygon and polyline clipping missing (TODO)

**Impact:** SVG files using unsupported clipping modes will render incorrectly

---

### 2.3 SVG - Simplified Drop Shadow Filter
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:125, 1120-1191`

**Issue:**
```csharp
// NOTE: This is a simplified implementation without blur - just offset + opacity
```

**Lines 1120-1166:**
- Drop shadows rendered as offset copy with opacity
- Blur filters (`feGaussianBlur`) explicitly ignored
- Text and other elements not supported for shadows (line 1191)

**Impact:** Visual quality degradation for SVG files with drop shadows

---

### 2.4 SVG - Simplified Font Mapping
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1568`

**Issue:**
```csharp
// TODO: This is a simplified mapping. A full implementation would use FontMetrics and PdfBaseFontMapper
```

**Impact:**
- Only basic family/weight/style matching
- Complex font resolution may fail
- Font variants may not map correctly

---

### 2.5 Mac Roman Encoding - Asymmetric Implementation
**File:** `src/Folly.Fonts/Tables/NameTableParser.cs:162-167`

**Issue:**
```csharp
public override int GetByteCount(char[] chars, int index, int count)
{
    throw new NotImplementedException("Mac Roman encoding only supports GetString (decoding)");
}

public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
{
    throw new NotImplementedException("Mac Roman encoding only supports GetString (decoding)");
}
```

**Impact:**
- Can decode Mac Roman but cannot encode it
- Will crash if code attempts to write Mac Roman encoded strings
- Asymmetric capability may cause unexpected failures

**Recommendation:** Implement encoding or document read-only limitation

---

### 2.6 PNG Decoder - Simplified Implementation
**File:** `src/Folly.Pdf/PdfWriter.cs:706-707`

**Issue:**
```csharp
// Simplified PNG decoder - extract IDAT chunks and parse IHDR for metadata
// For production use, consider using a PNG library like SixLabors.ImageSharp
```

**Additional Limitation (line 764-767):**
- Adam7 interlaced PNG throws error (not supported)

**Impact:**
- Manual PNG parsing may have edge case bugs
- Interlaced PNGs not supported
- Comment suggests this is not production-ready

---

### 2.7 Image Error Handling - Silent Placeholder
**File:** `src/Folly.Pdf/PdfWriter.cs:933-945`

**Issue:**
```csharp
else if (_options.ImageErrorBehavior == ImageErrorBehavior.UsePlaceholder)
{
    // Return a 1x1 white pixel placeholder (backward compatibility mode)
    byte[] fallback = new byte[] { 255, 255, 255 };
    return (fallback, 8, "DeviceRGB", 3, null, null, null);
}
```

**Impact:**
- Failed images silently replaced with 1x1 white pixel
- Users may not notice missing content
- Silent data loss in documents

**Recommendation:** Add warning/logging when placeholder used

---

### 2.8 TIFF Parser - Incomplete Array Handling
**File:** `src/Folly.Core/Images/Parsers/TiffParser.cs:209-211`

**Issue:**
```csharp
// For now, assume simple case: single value stored directly
// TODO: Handle arrays properly by reading from offset
return new[] { offsetOrValue };
```

**Impact:**
- TIFF files with proper array properties may fail to parse
- Only handles simplified case where value stored directly

---

### 2.9 GIF Parser - First Frame Only
**File:** `src/Folly.Core/Images/Parsers/GifParser.cs:55-96`

**Issue:**
- Only extracts first image from GIF
- Animation, disposal methods, metadata not supported
- Extension blocks for transparency not fully parsed

**Impact:** Multi-frame GIFs render as static (first frame only)

---

### 2.10 sRGB ICC Profile - Minimal/Simplified
**File:** `src/Folly.Pdf/SrgbIccProfile.cs:11, 38, 140`

**Issues:**
1. Labeled as "simplified sRGB profile" (line 11)
2. Placeholder checksums (line 38): `// Profile size (placeholder, will update at end)`
3. Simplified gamma 2.2 (line 140) instead of proper tone curve

**Impact:**
- May not meet strict color management requirements
- Simplified gamma curve vs. actual sRGB transfer function

---

### 2.11 BiDi - Simplified Isolate Handling
**File:** `src/Folly.Core/BiDi/UnicodeBidiAlgorithm.cs:183`

**Issue:**
```csharp
// Simplified isolate handling (full implementation would be more complex)
levels[i] = currentLevel;
```

**Impact:**
- Directional isolates (LRI, RLI, FSI, PDI) don't fully isolate context
- RTL/mixed direction text may not reorder correctly in complex cases

---

### 2.12 XObject Storage Inconsistency
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:2452`

**TODO:**
```csharp
// TODO: Store as binary bytes instead of converting to UTF8 string for efficiency
```

**Issue:** Form XObjects stored as UTF8 strings instead of binary bytes

**Impact:** Memory inefficiency and potential encoding issues

---

### 2.13 Layout Engine - Leader Workaround
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:1231-1236`

**Issue:**
```csharp
// Add as child of line area (note: LineArea.AddInline only accepts InlineArea)
// We need to track leaders separately or modify LineArea
// For now, let's add it as a child of the block area directly
```

**Impact:**
- Leaders not properly integrated into line layout
- Workaround may cause positioning issues

---

## 3. HARD-CODED VALUES & MAGIC NUMBERS (Severity: MINOR)

### 3.1 Bezier Circle Approximation Constant
**Files:**
- `src/Folly.Core/Svg/SvgToPdf.cs:245, 313` - `0.5522847498`
- `src/Folly.Pdf/PdfRenderer.cs:1280, 1351, 1375` - `0.552284749831`

**Issue:** Magic number used for Bezier approximation of quarter circle; should be named constant

**Recommendation:**
```csharp
private const double BEZIER_CIRCLE_KAPPA = 0.5522847498; // (4/3) * tan(π/8)
```

---

### 3.2 Default Page Size Assumption
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:471-472`

**Issue:**
```csharp
595pt / 842pt  // A4 dimensions hard-coded as default
```

**Impact:**
- Assumes A4 as default (European standard)
- US users might expect Letter size (612x792pt)

**Recommendation:** Make default page size configurable

---

### 3.3 PNG Parsing Magic Offsets
**File:** `src/Folly.Pdf/PdfWriter.cs:726, 962`

**Issue:** Hard-coded offsets `8`, `2`, `4` for PNG signature and zlib headers

**Recommendation:** Use named constants for readability

---

## 4. ASSUMPTIONS IN CORE LOGIC

### 4.1 Critical Assumptions

| Location | Assumption | Risk |
|----------|-----------|------|
| `CffTableParser.cs:86-91` | CFF parsing failures are acceptable | Fonts may load with missing glyph data silently |
| `TiffParser.cs:109, 209` | TIFF values stored directly (not as offsets) | Complex TIFFs may fail |
| `TiffParser.cs:272` | 8-bit palette size | May fail on non-8-bit palettes |
| `GifParser.cs:203` | 72 DPI for GIF images | Incorrect DPI for high-res displays |
| `PdfWriter.cs:933-945` | Acceptable to replace failed images with 1x1 white pixel | Silent data loss |
| `WritingModeHelper.cs:84` | Unknown writing modes default to lr-tb | May misrender vertical text |
| `LengthParser.cs:40` | Unitless values are points | May misinterpret user intent |

---

## 5. MISSING MAJOR FUNCTIONALITY SUMMARY

### PDF-Level Features
- [ ] Full CFF font parsing with CharString decompilation
- [ ] CFF font subsetting
- [ ] Complete Charset and Encoding mapping for CFF fonts
- [ ] CIDFont (composite font) support
- [ ] Full ICC profile generation (currently minimal sRGB)

### SVG Features
- [ ] tspan elements for multi-line/styled text
- [ ] textPath for text on curves (partial implementation exists)
- [ ] All clipping shapes (polygon, polyline missing)
- [ ] clipPathUnits=objectBoundingBox
- [ ] Drop shadow with blur filters
- [ ] Per-character text transformations/rotations
- [ ] Shadow support for text elements

### Image Handling
- [ ] Multi-frame GIF support
- [ ] GIF disposal methods and animation
- [ ] Full TIFF array properties
- [ ] GIF extension block metadata (transparency, etc.)
- [ ] Adam7 interlaced PNG support
- [ ] Production-grade PNG decoder (currently simplified)

### Typography
- [ ] Mac Roman encoding (write direction)
- [ ] Full Unicode BiDi isolate handling
- [ ] Per-character rotation in tspan

---

## 6. RECOMMENDATIONS BY PRIORITY

### Priority 1 (Critical - Blocks Core Functionality)
1. **Complete CFF Parser** - Implement CharStrings, Charset, Encoding parsing
2. **Add CFF Font Subsetting** - Essential for modern OpenType fonts
3. **Fix Silent Error Handling** - Add logging/diagnostics to all bare catch blocks
4. **Implement Mac Roman Encoding** - Make encoding symmetric with decoding

### Priority 2 (Major - Missing Common Features)
1. **Implement SVG tspan/textPath** - Very common SVG features
2. **Add clipPathUnits Support** - Required for SVG spec compliance
3. **Complete BiDi Isolate Handling** - Needed for proper RTL text
4. **Improve Image Error Reporting** - Log/warn instead of silent placeholders

### Priority 3 (Quality - Production Readiness)
1. **Replace Simplified PNG Decoder** - Use proper library or complete implementation
2. **Complete TIFF Array Handling** - Support full TIFF specification
3. **Add Multi-frame GIF Support** - Or document single-frame limitation
4. **Complete sRGB ICC Profile** - Use proper tone curve representation

### Priority 4 (Enhancement - Code Quality)
1. **Named Constants for Magic Numbers** - Improve code readability
2. **Configurable Default Page Size** - Better UX for different regions
3. **Fix Leader Layout Integration** - Remove workaround
4. **XObject Binary Storage** - Improve efficiency

---

## 7. COMPLIANCE & DOCUMENTATION GAPS

### Areas Needing Documentation
1. **CFF Font Limitations** - Document what CFF features are not supported
2. **Font Subsetting Constraints** - Clearly state TrueType-only limitation
3. **SVG Feature Support Matrix** - Document supported/unsupported SVG features
4. **Image Format Limitations** - Document GIF single-frame, PNG interlacing, TIFF constraints
5. **Error Handling Behavior** - Document what errors are silently handled vs. thrown

### Zero-Dependency Compliance
All core directories (`src/Folly.Core/`, `src/Folly.Pdf/`) maintain zero external dependencies ✅

---

## 8. CONCLUSION

The codebase has **solid foundational architecture** but contains significant gaps in three critical areas:

1. **Font Handling** - CFF parser incomplete; subsetting limited to TrueType
2. **SVG Rendering** - Missing text features, clipping modes, and filter effects
3. **Error Handling** - Too many silent failures without user visibility

**Production Readiness Assessment:**
- **TrueType fonts:** Production ready ✅
- **CFF/OpenType fonts:** Limited support ⚠️ (no subsetting, simplified parsing)
- **SVG rendering:** Good for basic SVGs ✅, limited for advanced features ⚠️
- **Image handling:** Basic formats work ✅, advanced features missing ⚠️

**Recommended Actions Before Production:**
1. Complete or document CFF font limitations
2. Add comprehensive error logging/reporting
3. Document all feature limitations in user-facing documentation
4. Consider adding integration tests for edge cases (interlaced PNG, CFF fonts, complex SVGs)
