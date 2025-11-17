# Codebase Gaps and Over-Simplifications Audit

**Date:** 2025-11-17
**Auditor:** Claude Code Agent
**Scope:** Complete codebase audit for over-simplifications, assumptions, and missing functionality

---

## Executive Summary

This audit identified **52 distinct gaps** across the Folly PDF library codebase, categorized by severity:

- **🔴 CRITICAL (P0):** 9 issues - Data loss, corruption, or crashes
- **🟠 HIGH (P1):** 14 issues - Missing features with significant impact
- **🟡 MEDIUM (P2):** 17 issues - Incorrect output or limitations
- **🟢 LOW (P3):** 12 issues - Nice-to-have enhancements

**Most Critical Areas:**
1. **Font Subsetting** - Composite glyphs not remapped (breaks accented characters)
2. **Image Parsing** - Multiple critical format gaps (PNG alpha, JPEG orientation, TIFF multi-strip)
3. **Layout Engine** - Array bounds and infinite loop risks
4. **BiDi Implementation** - Simplified isolate handling causes incorrect rendering

---

## 🔴 CRITICAL ISSUES (P0) - Fix Immediately

### FONT SUBSETTING

#### 1. Composite Glyph Component Remapping Not Implemented
**File:** `src/Folly.Fonts/FontSubsetter.cs:132-144`
**Impact:** Accented characters (é, ñ, ü), ligatures corrupted in subset fonts
**Description:** When subsetting fonts, composite glyphs reference other glyphs by index. The subsetter copies raw glyph data without remapping component indices. If glyph 250 becomes glyph 5 in the subset, composite glyphs still reference old index 250.

**Current Code:**
```csharp
subsetFont.Glyphs[newIndex] = originalFont.Glyphs[oldIndex];
// ^^^ Copies RawGlyphData without remapping component indices!
```

**Fix Required:** Parse composite glyph data, identify component indices, remap using `glyphMapping` dictionary, rewrite glyph data.

---

#### 2. Composite Glyph Dependency Collection Missing
**File:** `src/Folly.Fonts/FontSubsetter.cs:47-67`
**Impact:** Subset fonts may have composite glyphs referencing missing component glyphs
**Description:** When collecting glyphs for subsetting, the code doesn't follow composite glyph references. If 'é' uses components 'e' and acute accent, but 'e' isn't in used characters, component won't be included.

**Fix Required:** Recursive traversal of composite glyph components.

---

#### 3. CFF/OpenType Font Subsetting Not Implemented
**File:** `src/Folly.Fonts/FontSubsetter.cs:29-30`
**Impact:** OpenType fonts with CFF outlines (.otf files) cannot be subset or embedded
**Description:** Any .otf file throws `NotSupportedException`. Affects many modern fonts (Adobe fonts, Google Fonts .otf versions).

**Current Code:**
```csharp
if (!font.IsTrueType)
    throw new NotSupportedException("Font subsetting is currently only supported for TrueType fonts.");
```

**Missing:** CharStrings parsing, Charset parsing, Encoding parsing, CFF subsetting algorithm.

---

### IMAGE PARSING

#### 4. PNG RGBA Alpha Channel Not Extracted
**File:** `src/Folly.Core/Images/Parsers/PngParser.cs:223`
**Impact:** PNG RGBA images lose alpha channel at parse time, must be re-extracted during PDF writing (inefficient and error-prone)

**Current Code:**
```csharp
AlphaData = null, // Will be extracted during PDF writing if needed
```

**Fix Required:** Extract alpha channel during parsing and store in `ImageInfo.AlphaData`.

---

#### 5. JPEG EXIF Orientation Not Parsed
**File:** `src/Folly.Core/Images/Parsers/JpegParser.cs:145-148`
**Impact:** Rotated phone photos display incorrectly in PDFs
**Description:** EXIF orientation metadata (APP1 marker) is acknowledged but not extracted. `ImageInfo.Orientation` property exists but never set.

**Fix Required:** Parse EXIF data, extract Orientation tag (0x0112), set `ImageInfo.Orientation`.

---

#### 6. JPEG ICC Multi-Segment Profiles Incomplete
**File:** `src/Folly.Core/Images/Parsers/JpegParser.cs:163-174`
**Impact:** Large ICC profiles (>64KB) corrupted - only first chunk stored
**Description:** Code reads sequence number and total count but only stores first chunk, ignores remaining chunks.

**Current Code:**
```csharp
byte sequenceNumber = data[offset + 12];
byte totalCount = data[offset + 13];
// ... but then only stores first chunk at line 170
```

**Fix Required:** Accumulate all chunks in order, concatenate to form complete ICC profile.

---

#### 7. TIFF Multi-Strip Arrays Not Handled
**File:** `src/Folly.Core/Images/Parsers/TiffParser.cs:210`
**Impact:** Large TIFF files with multiple strips will be corrupted
**Description:** IFD tag array reading is incomplete - assumes single value stored directly.

**Current Code:**
```csharp
// TODO: Handle arrays properly by reading from offset
return new[] { offsetOrValue };
```

**Fix Required:** When tag count > 1, read array from offset specified in offsetOrValue.

---

### LAYOUT ENGINE

#### 8. Image Byte Array Access Without Bounds Checking
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:3400-3438`
**Impact:** IndexOutOfRangeException on malformed images
**Description:** Direct array indexing without validating array length.

**Current Code:**
```csharp
if (data[offset] != 0xFF)  // No check if offset < data.Length
int width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];  // No check if data.Length >= 24
```

**Fix Required:**
```csharp
if (data.Length < 24) return null;
if (offset >= data.Length) return null;
```

---

#### 9. TableCellGrid Infinite Loop Risk
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:5054`
**Impact:** Infinite loop if table row has more cells than declared columns
**Description:** `GetNextAvailableColumn` has no maximum column limit.

**Current Code:**
```csharp
while (_occupied.ContainsKey((row, col)))
    col++;
return col;
```

**Fix Required:**
```csharp
public int GetNextAvailableColumn(int row, int startColumn = 0, int maxColumns = 1000)
{
    int col = startColumn;
    while (_occupied.ContainsKey((row, col)) && col < maxColumns)
        col++;
    return col;
}
```

---

## 🟠 HIGH SEVERITY (P1) - Fix Soon

### FONT HANDLING

#### 10. Font Metrics Validation Missing
**File:** `src/Folly.Fonts/Tables/HeadTableParser.cs`, `src/Folly.Fonts/Tables/MaxpTableParser.cs`
**Impact:** Malformed fonts can crash or produce invalid PDFs

**Missing Validations:**
- Ascender > 0, Descender < 0 sanity checks
- XMax > XMin, YMax > YMin bounding box validation
- Glyph count > 0
- numberOfHMetrics ≤ glyphCount (could cause IndexOutOfRangeException)

---

#### 11. Font Metadata Uses Hard-Coded Approximations
**File:** `src/Folly.Pdf/TrueTypeFontEmbedder.cs:118-124`
**Impact:** Incorrect PDF metadata, PDF/A validators may flag errors

**Hard-Coded Values:**
- `ItalicAngle`: Always 0 (should come from Post table)
- `CapHeight`: Uses ascender approximation (should use OS/2 sCapHeight)
- `StemV`: Hard-coded to 80 (should calculate from font weight)
- `Flags`: Always 32 (symbolic), ignores serif/script/italic/fixed-pitch flags

---

#### 12. Division by Zero in Font Width Scaling
**File:** `src/Folly.Pdf/TrueTypeFontEmbedder.cs:313`
**Impact:** Division by zero if font has malformed unitsPerEm

**Current Code:**
```csharp
int scaledWidth = width * 1000 / unitsPerEm;
```

**Fix Required:** Validate `unitsPerEm > 0` or use safe default (1000).

---

### IMAGE PARSING

#### 13. GIF Animated Frames Ignored
**File:** `src/Folly.Core/Images/Parsers/GifParser.cs:126`
**Impact:** Multi-frame GIFs lose animation, only first frame extracted

**Current Code:**
```csharp
break; // Stop after first image
```

**Fix Required:** Either extract all frames or document limitation clearly.

---

#### 14. TIFF CMYK Not Supported
**File:** `src/Folly.Core/Images/Parsers/TiffParser.cs:60`
**Impact:** TIFF CMYK images cannot be used

**Current Code:**
```csharp
throw new NotSupportedException($"TIFF photometric interpretation {photometricInterpretation} not supported.");
```

**Fix Required:** Add CMYK support (photometric interpretation 5).

---

#### 15. TIFF 16-Bit Samples Downsampled to 8-Bit
**File:** `src/Folly.Core/Images/Parsers/TiffParser.cs:284-291`
**Impact:** Loss of color depth and precision in high-quality images

**Fix Required:** Preserve 16-bit samples or provide option for quality mode.

---

#### 16. TIFF Alpha Channel Not Supported
**File:** `src/Folly.Core/Images/Parsers/TiffParser.cs`
**Impact:** TIFF with alpha channel will fail or render incorrectly

**Fix Required:** Detect samples per pixel = 4 (RGBA), separate alpha channel.

---

#### 17. BMP 16-Bit Color Not Supported
**File:** `src/Folly.Core/Images/Parsers/BmpParser.cs:61`
**Impact:** 16-bit BMP files rejected

**Current Code:**
```csharp
throw new NotSupportedException($"BMP with {bitsPerPixel} bits per pixel not supported.");
```

**Fix Required:** Add 16-bit RGB555/RGB565 support.

---

#### 18. No Image Size Limits (OOM Vulnerability)
**File:** All image parsers
**Impact:** 10000×10000 pixel image = 300MB uncompressed RGB - could cause OOM crashes

**Current Protection:**
- PNG: 100MB chunk size limit
- Others: No limits

**Fix Required:**
```csharp
const int MAX_IMAGE_PIXELS = 100_000_000; // 100 megapixels
const int MAX_FILE_SIZE = 100 * 1024 * 1024; // 100MB
```

---

### SVG CONVERSION

#### 19. SVG Arc Commands in TextPath Ignored
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1854-1866`
**Impact:** Text on arc-based paths will be incorrect

**Current Code:**
```csharp
// Arc approximation requires complex calculations
// For textPath, arcs are less common and can be approximated
// For now, skip arc commands (read and discard 7 parameters)
```

**Fix Required:** Implement arc to cubic Bezier approximation.

---

#### 20. SVG Smooth Bezier Commands Not Supported
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1848-1851`
**Impact:** Smooth path commands (S, s, T, t) in textPath not supported

**Current Code:**
```csharp
// S, s, T, t require tracking previous control points
// For textPath approximation, we can skip these for now
```

**Fix Required:** Track previous control points and implement smooth Bezier.

---

#### 21. SVG Filter Effects Minimal
**File:** `src/Folly.Core/Svg/SvgToPdf.cs`, `src/Folly.Core/Svg/SvgParser.cs:783-785`
**Impact:** Complex SVG filters completely unsupported

**Currently Supported:**
- feDropShadow (simplified, without blur)

**Not Supported:**
- feGaussianBlur (defined but not implemented)
- feBlend, feOffset, feColorMatrix, feComposite, feMerge, feFlood, feTurbulence, feDisplacementMap, feMorphology, feConvolveMatrix, feImage, feTile, feDiffuseLighting, feSpecularLighting

**Fix Required:** Implement core filter primitives or clearly document limitations.

---

### LAYOUT ENGINE

#### 22. Dictionary KeyNotFoundException Risk
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:308, 391`
**Impact:** Runtime exception if marker class name not in dictionary

**Current Code:**
```csharp
var markersForClass = _markers[className];
```

**Fix Required:**
```csharp
if (!_markers.TryGetValue(className, out var markersForClass))
    return null;
```

---

#### 23. BiDi Isolate Implementation Simplified
**File:** `src/Folly.Core/BiDi/UnicodeBidiAlgorithm.cs:180-189`
**Impact:** Mixed directionality with isolates (e.g., Arabic with embedded numbers in parentheses) will render incorrectly

**Current Code:**
```csharp
case BidiCharacterType.LRI: // Left-to-Right Isolate
case BidiCharacterType.RLI: // Right-to-Left Isolate
case BidiCharacterType.FSI: // First Strong Isolate
    // Simplified isolate handling (full implementation would be more complex)
    levels[i] = currentLevel;
    break;
```

**Missing:** Isolate level stack management, overflow isolation counter, proper isolation of directional runs, FSI auto-detection.

**Fix Required:** Implement full UAX#9 X1-X10 rules for isolating sequences.

---

## 🟡 MEDIUM SEVERITY (P2) - Improve Quality

### PDF STRUCTURE

#### 24. PdfStructureTree Unsafe .First() Call
**File:** `src/Folly.Pdf/PdfStructureTree.cs:263, 329`
**Impact:** InvalidOperationException if dictionary unexpectedly empty

**Current Code:**
```csharp
var pageNum = mcidPages.Values.First();
```

**Fix Required:**
```csharp
var pageNum = mcidPages.Values.FirstOrDefault();
if (pageNum < 0) return; // Handle empty case
```

---

### IMAGE PARSING

#### 25. TIFF Integer Overflow in Array Indexing
**File:** `src/Folly.Core/Images/Parsers/TiffParser.cs:79-80`
**Impact:** If TIFF contains offsets >2GB, silent overflow causes incorrect memory access

**Current Code:**
```csharp
int stripOffset = (int)stripOffsets[i];
int stripByteCount = (int)stripByteCounts[i];
```

**Fix Required:** Add overflow validation before casting.

---

#### 26. PNG ICC Profile Decompression Failures Silent
**File:** `src/Folly.Core/Images/Parsers/PngParser.cs:128-133`
**Impact:** Corrupted ICC profiles silently dropped, no diagnostic

**Current Code:**
```csharp
catch
{
    return null; // Decompression failed
}
```

**Fix Required:** Log warning or provide diagnostic callback.

---

### SVG CONVERSION

#### 27. Gradient Stop Opacity Ignored
**File:** `src/Folly.Core/Svg/Gradients/SvgGradientToPdf.cs:240-255`
**Impact:** Gradient stops with opacity <1.0 render incorrectly

**Description:** PDF shading dictionaries don't support opacity in color functions - only RGB/CMYK/Gray, not alpha.

**Workaround:** Apply uniform opacity to entire element (implemented).

---

#### 28. Elliptical Radial Gradients Approximated as Circular
**File:** `src/Folly.Core/Svg/Gradients/SvgGradientToPdf.cs:92-118`
**Impact:** Non-circular radial gradients on non-square boxes rendered incorrectly

**Description:** PDF Type 3 shading supports only circular radial gradients, not elliptical.

---

#### 29. SVG Path Bounding Box Not Tracked
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:831`
**Impact:** Path elements with gradients may not render correctly

**Current Code:**
```csharp
// TODO: Implement bounding box tracking for path elements
```

**Fix Required:** Calculate path bounding box for objectBoundingBox gradient units.

---

#### 30. SVG Drop Shadow Without Blur
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1120-1191`
**Impact:** Drop shadows lack Gaussian blur effect

**Current Implementation:**
```csharp
// NOTE: This is a simplified implementation without blur - just offset + opacity
```

**Limitation:** PDF rendering constraints prevent full feGaussianBlur implementation.

---

#### 31. SVG Text and Other Elements Not Supported for Shadows
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1191`
**Impact:** Drop shadow only works for rect, circle, ellipse, path

**Current Code:**
```csharp
// NOTE: Text and other elements not supported for shadows yet
```

**Fix Required:** Extend shadow support to all element types.

---

#### 32. SVG ClipPath Units Not Handled
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1051`
**Impact:** Clipping paths with objectBoundingBox units may be incorrect

**Current Code:**
```csharp
// TODO: Handle clipPathUnits (userSpaceOnUse vs objectBoundingBox)
```

**Fix Required:** Implement coordinate system transformation for objectBoundingBox.

---

#### 33. SVG Limited Clipping Shapes
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1106`
**Impact:** Only rect, circle, ellipse, path supported in clipPath

**Current Code:**
```csharp
// TODO: Support more clipping shapes (polygon, polyline, etc.)
```

**Fix Required:** Add polygon, polyline, line clipping shape support.

---

#### 34. SVG Font Mapping Simplified
**File:** `src/Folly.Core/Svg/SvgToPdf.cs:1568`
**Impact:** SVG font selection may not match PDF font rendering

**Current Code:**
```csharp
// TODO: This is a simplified mapping. A full implementation would use FontMetrics and PdfBaseFontMapper
```

**Fix Required:** Integrate with FontMetrics and PdfBaseFontMapper.

---

### LAYOUT ENGINE

#### 35. Division by Zero in SVG Path Arc Conversion
**File:** `src/Folly.Core/Svg/SvgPathParser.cs:353-354, 360-361`
**Impact:** If rx/ry become zero through lambda correction, division by zero

**Description:** Although checked at lines 320-325, if rx/ry become zero through lambda correction (line 344-345), division could fail.

**Fix Required:** Add validation after radius correction.

---

#### 36. Division by Zero in Layout Engine
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:4168`
**Impact:** If lineHeight is 0 or negative, division by zero or incorrect calculations

**Current Code:**
```csharp
var maxLinesThatFit = (int)Math.Floor(availableForLines / lineHeight);
```

**Fix Required:** Validate `lineHeight > 0` before division.

---

#### 37. Empty Table with Zero Cells Edge Case
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:2835`
**Impact:** If table row exists with zero cells, defaults to 1 column (may be unexpected)

**Current Code:**
```csharp
var cellCount = foTable.Body?.Rows.FirstOrDefault()?.Cells.Count ?? 1;
```

**Fix Required:**
```csharp
var cellCount = Math.Max(1, foTable.Body?.Rows.FirstOrDefault()?.Cells.Count ?? 1);
```

---

#### 38. Negative Column Width Possible
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:550-551`
**Impact:** If columnGap too large, columnWidth becomes negative

**Current Code:**
```csharp
var columnWidth = columnCount > 1
    ? (bodyWidth - (columnCount - 1) * columnGap) / columnCount
    : bodyWidth;
```

**Fix Required:** Validate result is positive, clamp to minimum.

---

#### 39. Table Fixed Width Overflow Not Handled
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:2793-2825`
**Impact:** If all fixed column widths exceed availableWidth, columns overlap

**Missing:** Proportional shrinking when fixed widths exceed available space.

**Fix Required:** Detect overflow, apply proportional reduction.

---

#### 40. Negative Table Spacing Not Validated
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:2744`
**Impact:** If spacing exceeds availableWidth, availableForColumns becomes negative

**Current Code:**
```csharp
var availableForColumns = availableWidth - spacing;
```

**Fix Required:** Validate `availableForColumns > 0`.

---

## 🟢 LOW SEVERITY (P3) - Nice to Have

### IMAGE PARSING

#### 41. PNG Advanced Chunks Not Parsed
**File:** `src/Folly.Core/Images/Parsers/PngParser.cs`
**Impact:** Minor - loss of metadata, gamma correction, color space info

**Missing Chunks:**
- gAMA (gamma correction)
- sRGB (standard RGB color space)
- cHRM (chromatic adaptation)
- bKGD (background color)
- hIST (histogram)
- sPLT (suggested palette)

---

#### 42. JPEG EXIF DPI Not Parsed
**File:** `src/Folly.Core/Images/Parsers/JpegParser.cs:145-148`
**Impact:** JFIF DPI works, but EXIF XResolution/YResolution ignored

**Fix Required:** Parse EXIF resolution tags as fallback.

---

#### 43. GIF Transparency Extension Block TODO
**File:** `src/Folly.Core/Images/Parsers/GifParser.cs:55`
**Impact:** Basic transparency works, advanced features missing

**Current Code:**
```csharp
// TODO: Parse extension blocks for transparency and other metadata
```

**Note:** Basic transparency is implemented, but advanced features (disposal method, delay time, loop count) not extracted.

---

#### 44. TIFF Multi-Page Not Supported
**File:** `src/Folly.Core/Images/Parsers/TiffParser.cs`
**Impact:** Only first page of multi-page TIFF extracted

**Fix Required:** Parse all IFD chains, allow page selection.

---

#### 45. BMP BITMAPV4/V5 Headers Not Supported
**File:** `src/Folly.Core/Images/Parsers/BmpParser.cs:38`
**Impact:** Modern BMP features (ICC profiles, alpha masks) not available

**Fix Required:** Add support for 108-byte and 124-byte headers.

---

#### 46. GIF Aspect Ratio Approximation
**File:** `src/Folly.Core/Images/Parsers/GifParser.cs:199-206`
**Impact:** Converts pixel aspect ratio to DPI assumption (not true DPI)

**Fix Required:** Handle aspect ratio more accurately or default to 72 DPI.

---

### FONT HANDLING

#### 47. Variable Fonts Not Supported
**File:** `src/Folly.Fonts/FontParser.cs`
**Impact:** Variable fonts work only in default instance, no weight/width/slant variations

**Missing:** fvar, avar, gvar, STAT, HVAR, VVAR, MVAR table parsers.

**Risk Level:** Medium - No crashes expected, but only default instance accessible.

---

### LAYOUT ENGINE

#### 48. Hardcoded Default Extent Values
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:146, 179, 212-216, 253-257, 521-524`
**Impact:** Region extent defaults to 36 points (half-inch) if not specified

**Fix Required:** Define constant with clear documentation or make configurable.

---

#### 49. Magic Number for Minimum Column Width
**File:** `src/Folly.Core/Layout/LayoutEngine.cs:9`
**Impact:** Columns narrower than 50pt may not be supported

**Current Code:**
```csharp
private const double MinimumColumnWidth = 50.0;
```

**Fix Required:** Document why 50pt was chosen.

---

#### 50. Keep/Break Property Completeness
**File:** `src/Folly.Core/Dom/FoBlock.cs`
**Impact:** Some XSL-FO keep/break values not implemented

**Missing:**
- keep-together integer values
- break-before/after: "column", "even-page", "odd-page"
- keep-with-next/previous integer strength values

---

### DOM PARSING

#### 51. LengthParser Negative Number Handling
**File:** `src/Folly.Core/Dom/LengthParser.cs:26-28`
**Impact:** "-5pt" could parse incorrectly as minus sign accepted anywhere

**Current Code:**
```csharp
while (numEnd < value.Length && (char.IsDigit(value[numEnd]) || value[numEnd] == '.' || value[numEnd] == '-'))
    numEnd++;
```

**Fix Required:** Only accept minus sign at beginning.

---

### BIDI

#### 52. BiDi Bracket Pairing Incomplete
**File:** `src/Folly.Core/BiDi/UnicodeBidiAlgorithm.cs`
**Impact:** Some Unicode bracket pairs may not be recognized

**Status:** Core brackets implemented, but full Unicode bracket database not exhaustive.

---

## Summary Statistics

| Category | Critical | High | Medium | Low | Total |
|----------|----------|------|--------|-----|-------|
| Font Subsetting | 3 | 3 | 0 | 1 | 7 |
| Image Parsing | 4 | 6 | 2 | 6 | 18 |
| SVG Conversion | 0 | 3 | 9 | 0 | 12 |
| Layout Engine | 2 | 2 | 6 | 2 | 12 |
| BiDi/Text | 0 | 1 | 0 | 1 | 2 |
| DOM/Parsing | 0 | 0 | 0 | 1 | 1 |
| **Total** | **9** | **15** | **17** | **11** | **52** |

---

## Priority Recommendations

### Immediate Action (Next Sprint)
1. Fix composite glyph remapping in font subsetting
2. Add composite glyph dependency collection
3. Fix PNG alpha channel extraction
4. Fix JPEG EXIF orientation parsing
5. Fix JPEG ICC multi-segment profiles
6. Fix TIFF multi-strip array handling
7. Add image byte array bounds checking
8. Fix TableCellGrid infinite loop risk
9. Add font metrics validation

### High Priority (Within Month)
10. Implement CFF font subsetting or document limitation clearly
11. Fix BiDi isolate implementation
12. Add image size limits (OOM protection)
13. Implement SVG arc commands in textPath
14. Add BMP 16-bit support
15. Add TIFF CMYK support
16. Fix font metadata hard-coded values
17. Implement SVG smooth Bezier commands
18. Add Dictionary TryGetValue pattern

### Medium Priority (Within Quarter)
19-40. Address all medium severity issues listed above

### Low Priority (Backlog)
41-52. Nice-to-have enhancements

---

## Test Coverage Recommendations

Based on gaps found, prioritize tests for:

1. **Font Subsetting:**
   - Composite glyphs (accented characters, ligatures)
   - CFF fonts (expect NotSupportedException)
   - Variable fonts (default instance only)

2. **Image Parsing:**
   - PNG RGBA with alpha channel
   - JPEG with EXIF orientation (8 rotations)
   - JPEG with large ICC profiles (>64KB)
   - TIFF multi-strip images
   - GIF animated (verify only first frame)
   - Malformed images (truncated, zero dimensions, huge sizes)

3. **SVG Conversion:**
   - TextPath with arcs and smooth Beziers
   - Gradients with opacity stops
   - Filter effects (verify limitations)
   - Clipping paths with various shapes

4. **Layout Engine:**
   - Empty tables, zero-cell rows
   - Tables with excessive border spacing
   - Very large column gaps
   - Malformed cell spans

5. **BiDi:**
   - Text with LRI/RLI/FSI isolates
   - Mixed Arabic/English with numbers in parentheses

---

## Documentation Updates Required

1. **docs/guides/limitations.md:**
   - Add all P0/P1 issues not currently documented
   - Update status of recently fixed issues
   - Add severity indicators

2. **README.md:**
   - Update known limitations section
   - Add "Not Supported" subsection for CFF fonts, variable fonts

3. **API Documentation:**
   - Document image size limits
   - Document font subsetting limitations
   - Document SVG feature support matrix

---

## Conclusion

The Folly PDF library has a solid foundation with good defensive coding in many areas. However, this audit identified **52 distinct gaps** requiring attention, with **9 critical issues** that could cause data loss, corruption, or crashes in production use.

**Key Strengths:**
- Comprehensive error handling in most areas
- Good validation for common cases
- Well-structured code with clear separation of concerns

**Key Weaknesses:**
- Font subsetting incomplete for composite glyphs
- Image parsing missing critical features (alpha, orientation, multi-segment profiles)
- Layout engine has edge case vulnerabilities
- BiDi implementation simplified for isolates

**Recommended Next Steps:**
1. Address all 9 critical issues in next sprint
2. Create comprehensive test suite for identified gaps
3. Update documentation to clearly state limitations
4. Implement or document CFF font subsetting
5. Add global image size limits for security

---

**Generated:** 2025-11-17
**Files Analyzed:** 50+ files across src/Folly.Core, src/Folly.Pdf, src/Folly.Fonts
**Analysis Depth:** Complete codebase with deep exploration of critical areas
