# Folly Library Limitations and Assumptions

This document catalogs all known limitations, assumptions, and "simplest thing that could possibly work" implementations in the Folly library. These items represent technical debt and areas where the library may fail or produce unexpected results for certain inputs.

**Status**: Last updated 2025-11-13
**Purpose**: Help developers understand edge cases and prioritize improvements

---

## 1. Font Handling

### 1.1 OpenType Features Not Implemented

**Location**: `src/Folly.Fonts/FontParser.cs:127-129`

```csharp
// TODO: Parse 'GPOS' table for advanced positioning (OpenType)
// TODO: Parse 'GSUB' table for glyph substitution (OpenType)
// TODO: Parse 'CFF ' table for OpenType/CFF fonts
```

**Impact**:
- No support for advanced OpenType features like ligatures, contextual alternates, or positional variants
- CFF (Compact Font Format) fonts cannot be parsed or embedded
- Arabic, Devanagari, and other complex scripts may render incorrectly

**Workaround**: Convert fonts to TrueType format before use

---

### 1.2 Font Subsetting Only for TrueType

**Location**: `src/Folly.Fonts/FontSubsetter.cs:30`

```csharp
throw new NotSupportedException("Font subsetting is currently only supported for TrueType fonts. OpenType/CFF support coming soon.");
```

**Impact**:
- CFF/OpenType fonts must be embedded in full, increasing PDF file size
- Large CJK fonts (15MB+) will bloat PDF files significantly

**Workaround**: Use TrueType versions of fonts when subsetting is required

---

### 1.3 Incomplete Font Metadata

**Location**: `src/Folly.Fonts/TrueTypeFontSerializer.cs:193-219`

Multiple font table fields use placeholder or default values:

```csharp
writer.WriteUInt32(0x00010000); // fontRevision - TODO: parse from font
long macEpochSeconds = 0;       // TODO: Use proper timestamps
ushort macStyle = 0;            // TODO: Derive from font properties (bold, italic)
```

**Impact**:
- Font metadata may be incorrect in generated PDFs
- Font matching and substitution may fail in some PDF readers
- Timestamps will be incorrect (1904-01-01)
- Font style detection may not work properly

---

### 1.4 Incorrect hhea Table Calculations

**Location**: `src/Folly.Fonts/TrueTypeFontSerializer.cs:252-256`

```csharp
// minRightSideBearing - TODO: Calculate properly
writer.WriteInt16(0);

// xMaxExtent - TODO: Calculate properly from glyph data
writer.WriteInt16(font.XMax);
```

**Impact**:
- Font metrics may be incorrect
- Text clipping or overflow in some PDF readers
- Incorrect caret positioning

---

### 1.5 Font Table References Not Cloned

**Location**: `src/Folly.Fonts/FontSubsetter.cs:93-94`

```csharp
OS2 = originalFont.OS2,  // TODO: Clone instead of reference
Post = originalFont.Post, // TODO: Clone instead of reference
```

**Impact**:
- Shared references mean modifications to subset fonts affect original
- Potential memory issues if original font is disposed
- Thread safety concerns with concurrent subsetting

---

### 1.6 Missing PDF Subset Naming Convention

**Location**: `src/Folly.Fonts/FontSubsetter.cs:79`

```csharp
// TODO: Add subset tag to PostScript name (e.g., "ABCDEF+FontName")
PostScriptName = GenerateSubsetPostScriptName(originalFont.PostScriptName),
```

**Impact**:
- Subset fonts don't follow PDF/A conventions
- PDF validators may flag fonts as non-conformant
- Font deduplication across PDFs won't work properly

---

### 1.7 Kerning Pairs Not Remapped in Subsets

**Location**: `src/Folly.Fonts/FontSubsetter.cs:145`

```csharp
// TODO: Remap kerning pair indices to new glyph indices
```

**Impact**:
- Kerning may be incorrect or missing in subset fonts
- Text spacing will be wrong for character pairs with kerning

**Current Status**: The code appears to remap kerning pairs (lines 147-155), but the TODO suggests this is incomplete or incorrect

---

### 1.8 Glyph Outline Data Not Parsed

**Location**: `src/Folly.Fonts/Tables/GlyfTableParser.cs:17`

```csharp
/// Full outline parsing (contours and points) is not yet implemented.
```

**Impact**:
- Cannot analyze or modify glyph shapes
- Font validation is incomplete
- Cannot generate font previews or thumbnails

---

### 1.9 Macintosh Font Encoding Assumption

**Location**: `src/Folly.Fonts/Tables/NameTableParser.cs:107`

```csharp
// TODO: Macintosh encoding varies by script (Roman, Japanese, etc.)
// For now, assume ASCII/Roman (encoding 0)
return ReadNameString(stream, tableStart, storageOffset, record, Encoding.ASCII);
```

**Impact**:
- Non-Roman Mac fonts will have garbled font names
- Japanese, Chinese, Korean font names will be corrupted
- Font matching may fail for CJK fonts

---

### 1.10 Variable Fonts Not Supported

**Location**: Referenced in `docs/limitations/fonts-typography.md:253`

**Impact**:
- Cannot use variable fonts (OpenType Font Variations)
- Must use discrete font files for each weight/style
- Larger file sizes and more font files to manage

---

### 1.11 Simplified cmap Segment Building

**Location**: `src/Folly.Fonts/TrueTypeFontSerializer.cs:520`

```csharp
// Build segments - simplified implementation: one segment per contiguous range
```

**Impact**:
- cmap tables may be larger than optimal
- Potential compatibility issues with some PDF readers
- Slower font parsing in some scenarios

---

## 2. Image Handling

### 2.1 Simplified PNG Decoder

**Location**: `src/Folly.Pdf/PdfWriter.cs:297`

```csharp
// Simplified PNG decoder - extract IDAT chunks and parse IHDR for metadata
// For production use, consider using a PNG library like SixLabors.ImageSharp
```

**Impact**:
- Missing support for advanced PNG features
- May fail on valid but complex PNG files
- No validation of CRC checksums

---

### 2.2 Interlaced PNG Not Supported

**Location**: `src/Folly.Pdf/PdfWriter.cs:357`

```csharp
throw new NotSupportedException($"Interlaced PNG images (Adam7) are not supported. Please convert to non-interlaced format.");
```

**Impact**:
- Progressive PNGs will cause errors
- Common web optimization technique not supported

**Workaround**: Pre-process images to remove interlacing

---

### 2.3 Indexed PNG Transparency Not Fully Supported

**Location**: `src/Folly.Pdf/PdfWriter.cs:235`

```csharp
// TODO: Handle indexed color with tRNS (requires SMask or palette expansion)
```

**Impact**:
- Palette-based PNGs with transparency may render incorrectly
- Common for optimized web graphics
- Transparency will be lost

---

### 2.4 Image Decoding Error Fallback

**Location**: `src/Folly.Pdf/PdfWriter.cs:514-516`

```csharp
// Fallback for unexpected decoding errors: create a placeholder image (1x1 white pixel)
byte[] fallback = new byte[] { 255, 255, 255 };
return (fallback, 8, "DeviceRGB", 3, null, null, null);
```

**Impact**:
- **Silent failure**: Corrupted images render as white pixels
- No error reported to user
- Document appears correct but images are missing

**Risk**: High - users won't know their images failed to load

---

### 2.5 JPEG DPI Assumption

**Location**: Referenced in `docs/limitations/images.md:207`

```
- Assumes 72 DPI for all JPEG images
```

**Impact**:
- Images without DPI metadata will be sized incorrectly
- High-resolution images may appear too large
- Print layouts will be wrong

---

### 2.6 Color Space Assumptions

**Location**: Referenced in `docs/limitations/images.md:244`

```
- All images assumed sRGB
```

**Impact**:
- No ICC profile support
- CMYK images will render incorrectly
- Color-managed workflows not supported
- Print PDFs may have wrong colors

---

### 2.7 Limited Image Format Support

**Location**: Referenced in `docs/limitations/images.md`

**Not Supported**:
- GIF
- WebP
- TIFF
- BMP (except through conversion)
- SVG
- HEIF/HEIC

**Workaround**: Pre-convert all images to PNG or JPEG

---

### 2.8 Image Dimensions Fallback

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:2299`

```csharp
return (100, 100); // Default fallback
```

**Impact**:
- Corrupted or unrecognized image formats default to 100x100 points
- May cause layout issues with broken images
- No error reported to user

---

### 2.9 pHYs Chunk Not Applied

**Location**: `docs/limitations/images.md:203`

```
- **Note**: Currently extracted but not yet applied to image scaling (TODO)
```

**Impact**:
- PNG resolution metadata ignored
- Images may be scaled incorrectly
- Physical size specifications in PNGs not honored

---

## 3. Text Layout and Typography

### 3.1 Simplified BiDi Algorithm

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:2567`

```csharp
/// This is a simplified implementation that reverses character order.
/// For proper BiDi support, a full Unicode BiDi algorithm implementation would be needed.
```

**Impact**:
- **Critical for RTL languages**: Hebrew, Arabic, Persian render incorrectly
- Mixed LTR/RTL text (e.g., English within Arabic) will be wrong
- Numbers in RTL text positioned incorrectly
- BiDi brackets and punctuation not handled

**Status**: See `docs/limitations/bidi-text-support.md` for details

---

### 3.2 Text Justification Not Implemented

**Location**: Referenced in `docs/limitations/line-breaking-text-layout.md:118`

```
**Workaround**: None. Justified text will appear left-aligned.
```

**Impact**:
- `text-align="justify"` is ignored
- Professional publishing layouts not achievable
- Books, newspapers, magazines won't render correctly

---

### 3.3 Hyphenation Not Implemented

**Location**: Referenced in `docs/limitations/line-breaking-text-layout.md:235-243`

**Properties Not Supported**:
- `hyphenate`
- `hyphenation-character`
- `hyphenation-push-character-count`
- `hyphenation-remain-character-count`

**Impact**:
- Ragged right margins on narrow columns
- Poor text distribution
- Manual hyphenation required

**Note**: Hyphenation patterns exist in source generators but aren't used

---

### 3.4 Hardcoded Knuth-Plass Parameters

**Location**: `src/Folly.Core/Layout/KnuthPlassLineBreaker.cs:114-117`

```csharp
// TODO: Make stretch and shrink configurable
// TeX uses stretch = 0.5 * spaceWidth, shrink = 0.333 * spaceWidth
var spaceStretch = spaceWidth * 0.5;
var spaceShrink = spaceWidth * 0.333;
```

**Impact**:
- Cannot customize line breaking behavior
- Different fonts/sizes may need different parameters
- No per-language customization

---

### 3.5 No CJK Line Breaking

**Location**: Referenced in `docs/limitations/line-breaking-text-layout.md:243`

```
- `line-break` - Not implemented (for CJK)
```

**Impact**:
- Chinese, Japanese, Korean text breaks incorrectly
- Line breaks may occur at prohibited positions
- Punctuation handling wrong

---

## 4. Layout Engine

### 4.1 Simplified Marker Retrieval

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:152`

```csharp
// Simplified implementation: support first-starting-within-page and last-ending-within-page
```

**Impact**:
- Limited header/footer marker support
- Some XSL-FO marker positions not implemented
- Complex running headers won't work

**See**: `docs/limitations/advanced-features.md:276` for details

---

### 4.2 Simplified Table Column Width

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:2035`

```csharp
// Handle column width (simplified - support pt values)
```

**Impact**:
- Proportional column widths not fully supported
- Percentage widths may not work
- Auto-sizing limited

---

### 4.3 Float Width Hardcoded Defaults

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:2516`

```csharp
// Calculate float width (default to 200pt, or 1/3 of body width)
var floatWidth = Math.Min(200, bodyWidth / 3);
```

**Impact**:
- Floats without explicit width may be wrong size
- Layout breaks with narrow body widths
- No content-based sizing

---

### 4.4 Writing Mode Fallback

**Location**: `src/Folly.Core/Dom/WritingModeHelper.cs:84`

```csharp
// Unknown writing mode, assume lr-tb
```

**Impact**:
- Unsupported writing modes silently fallback to left-to-right
- Vertical text (tb-rl, tb-lr) may partially work but with issues
- No error or warning to user

---

### 4.5 Fixed Column Width Assumption in Page Breaking

**Location**: Referenced in `docs/limitations/page-breaking-pagination.md:218`

```
// Note: For simplicity, we assume body dimensions are consistent across all page masters
```

**Impact**:
- Different page sizes within same document may have layout issues
- First/last page templates with different widths may break

---

### 4.6 Missing Keep/Break Controls

**Location**: Referenced in `docs/limitations/page-breaking-pagination.md:380-390`

**Not Implemented**:
- `widows`
- `orphans`
- `keep-with-next`
- `keep-with-previous`
- `keep-together` with integer values
- `force-page-count`
- `span` (column balancing)

**Impact**:
- Cannot prevent orphan/widow lines
- Table rows may break awkwardly
- Lists may split poorly across pages

---

## 5. PDF Generation

### 5.1 PDF Version Locked to 1.7

**Location**: `src/Folly.Core/PdfOptions.cs:9`

```csharp
/// Gets or sets the PDF version. Currently only 1.7 is supported.
```

**Impact**:
- Cannot use PDF 2.0 features
- Cannot generate PDF/A-1, PDF/A-2 (require older versions)
- No access to newer compression or security features

---

### 5.2 Large Fonts Loaded into Memory

**Location**: `src/Folly.Pdf/PdfWriter.cs:865-867`

```csharp
// TODO: For very large fonts (e.g., CJK fonts >15MB), consider streaming instead
// of loading entire file. Requires refactoring to support two-pass writing
// (calculate length first, then stream data) or buffering approach.
fontData = File.ReadAllBytes(fontPath);
```

**Impact**:
- **Memory exhaustion** with large CJK fonts
- Multiple large fonts can cause OutOfMemoryException
- Server scenarios with many concurrent PDFs may crash

**Risk**: High for CJK languages

---

### 5.3 Character Encoding Fallback

**Location**: `src/Folly.Pdf/PdfWriter.cs:1062-1064`

```csharp
// Fallback for unmapped characters: use uniXXXX format
// This is a standard PDF convention for characters without standard glyph names
return $"uni{codePoint:X4}";
```

**Impact**:
- Some rare Unicode characters may not render
- Private Use Area characters will use fallback naming
- Font subsetting may be less efficient

**Status**: This is generally acceptable per PDF standards

---

### 5.4 Page Number Placeholder Replacement

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:987`

```csharp
// Replace page number placeholder with actual page number
```

**Impact**:
- Potential issues with total page count in headers/footers

---

## 6. Units and Parsing

### 6.1 Unitless Values Assume Points

**Location**: `src/Folly.Core/Dom/LengthParser.cs:39`

```csharp
"pt" or "" => number, // Points or unitless (assume points)
```

**Impact**:
- XSL-FO files with unitless values get interpreted as points
- May not match other processors' behavior
- Different from HTML/CSS conventions

---

### 6.2 Pixels Treated as Points

**Location**: `src/Folly.Core/Dom/LengthParser.cs:40`

```csharp
"px" => number, // Pixels (treat as points for now)
```

**Impact**:
- Screen resolution (96 DPI) vs print resolution (72 DPI) confusion
- 96px at 96 DPI should equal 72pt (1 inch), not 96pt (1.33 inches)
- Layouts sized in pixels will be ~33% too large

**Risk**: Medium - common in web-to-PDF scenarios

---

### 6.3 Unknown Units Default to Points

**Location**: `src/Folly.Core/Dom/LengthParser.cs:45`

```csharp
_ => number // Default to points
```

**Impact**:
- Typos in unit names silently accepted
- No error for unsupported units (e.g., "em", "rem", "%")
- Debugging difficulty when values are wrong

---

## 7. Color and Graphics

### 7.1 Default Color Fallback

**Location**: `src/Folly.Pdf/PdfRenderer.cs:422, 690`

```csharp
: (0.0, 0.0, 0.0); // Default to black
```

**Impact**:
- Invalid color values default to black
- No error reported
- May hide issues in input documents

---

### 7.2 Color Space Fallback

**Location**: `src/Folly.Pdf/PdfWriter.cs:755`

```csharp
_ => "DeviceRGB"      // Default fallback
```

**Impact**:
- Unknown color spaces default to RGB
- CMYK workflows may break
- Spot colors not supported

---

## 8. Missing XSL-FO Features

**Location**: See `docs/limitations/missing-xslfo-features.md`

Major unimplemented elements:
- ❌ `fo:table-and-caption`
- ❌ `fo:table-caption`
- ❌ `fo:retrieve-table-marker`
- ❌ `fo:multi-*` (all interactive elements)
- ❌ `fo:index-*` (all indexing elements)
- ❌ Background images
- ❌ Absolute positioning
- ❌ `z-index`
- ❌ `visibility`, `clip`, `overflow`

---

## 9. Security and Validation

### 9.1 Limited Security Policy Enforcement

**Location**: Referenced in `docs/limitations/security-validation.md:258-264`

**Not Implemented**:
- `AllowJavaScript`
- `AllowEmbeddedFiles`
- Disk quotas for temporary files
- JavaScript validation in input

**Impact**:
- Cannot enforce comprehensive security policies
- PDF encryption not available
- Digital signatures not supported

---

## 10. Performance

### 10.1 No Streaming PDF Generation

**Location**: Referenced in `docs/limitations/performance.md:79`

```
**Streaming Approach** (not implemented):
```

**Impact**:
- Entire PDF built in memory before writing
- Large documents (1000+ pages) may cause memory issues
- Cannot start sending PDF before complete generation

---

### 10.2 No Memory Pooling

**Location**: `PERFORMANCE.md:79`

```
2. **Memory Pooling**: Use `ArrayPool<T>` for temporary buffers
```

**Impact**:
- High GC pressure with many allocations
- Slower performance for high-throughput scenarios
- Server environments may see memory fragmentation

---

## Summary Statistics

**Total TODOs Found**: 15 in source code
**Total Simplified Implementations**: 8
**Total Assumptions**: 12
**Total Not Implemented Features**: 30+

**Severity Breakdown**:
- 🔴 **Critical** (will fail for common use cases): 5
  - BiDi text rendering
  - Interlaced PNG
  - Large CJK fonts (memory)
  - Silent image failures
  - Pixel unit interpretation

- 🟡 **High** (missing features for professional use): 15
  - Text justification
  - Hyphenation
  - OpenType features
  - CFF fonts
  - Variable fonts
  - CMYK color
  - PDF 2.0

- 🟢 **Medium** (edge cases, optimizations): 30+
  - Font metadata accuracy
  - Performance optimizations
  - Advanced XSL-FO features

---

## Recommendations

### Immediate Priorities (Critical Bugs)

1. **Fix silent image failures** - Should throw errors, not render white pixels
2. **Fix pixel unit conversion** - px should be 96 DPI, not 72 DPI
3. **Add memory streaming for large fonts** - Prevent OOM with CJK fonts
4. **Warn on BiDi text** - Current implementation is incorrect, should warn users
5. **Support interlaced PNG** - Very common format

### Next Phase (Professional Features)

1. Text justification
2. Hyphenation (patterns already exist!)
3. Full CMYK color support
4. OpenType GPOS/GSUB tables
5. PDF/A compliance

### Long Term (Optimization)

1. Streaming PDF generation
2. Memory pooling with ArrayPool
3. Font caching across documents
4. Incremental PDF updates

---

## Testing Recommendations

Create tests for each limitation to:
1. **Document expected behavior** (pass/fail/warning)
2. **Prevent regressions** when fixing issues
3. **Track progress** as items are addressed

Example test structure:
```csharp
[Fact]
public void InterlacedPng_ShouldThrowNotSupportedException()
{
    // KNOWN LIMITATION: docs/limitations.md#2.2
    var ex = Assert.Throws<NotSupportedException>(...);
    Assert.Contains("interlaced", ex.Message, StringComparison.OrdinalIgnoreCase);
}
```

---

**Last Updated**: 2025-11-13
**Document Version**: 1.0
**Contributing**: When adding TODO comments to code, update this document
