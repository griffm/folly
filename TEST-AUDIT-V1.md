# Folly v1.0 Testing Audit

## Executive Summary

This document provides a comprehensive audit of all deferred tests from Phases 7-13, organized by priority and test category. The goal is to add 110 high-quality tests to reach our v1.0 target of 595 passing tests.

**Current Status:**
- **Baseline:** 485 passing tests (364 unit + 20 spec + 101 font tests)
- **Target:** 595 passing tests
- **Delta:** +110 new tests needed

**Test Distribution:**
- Priority 1 (Critical): 50 tests
- Priority 2 (Important): 44 tests
- Priority 3 (Nice to Have): 16 tests

---

## Test Inventory by Phase

### Phase 7: Critical Issues ✅

**Status:** ALL TESTS COMPLETED

Phase 7 testing is complete:
- ✅ Image error handling tests (ImageDecodingException)
- ✅ Font memory management tests (MaxFontMemory quota)
- ✅ Configurable error behavior tests (ThrowException, UsePlaceholder, SkipImage)

**Tests Added:** Already included in baseline (485 tests)

---

### Phase 8: Font System

#### 8.1 OpenType Features (GPOS/GSUB)

**Deferred from PLAN.md:**
- "Add 20+ tests with real OpenType fonts (deferred)"

**Priority:** 2 (Important)

**Reduced Scope for v1.0:** Focus on common features only (10 tests instead of 20+)

**Test Cases (10 tests):**

1. **OpenType_Ligature_fi** - Test "fi" ligature substitution
2. **OpenType_Ligature_fl** - Test "fl" ligature substitution
3. **OpenType_Ligature_ffi** - Test "ffi" ligature substitution
4. **OpenType_Ligature_ffl** - Test "ffl" ligature substitution
5. **OpenType_Ligature_ff** - Test "ff" ligature substitution
6. **OpenType_Ligature_Disabled** - Test that disabling liga feature prevents substitution
7. **OpenType_CommonLigatures_AllInOne** - Test all common ligatures together
8. **OpenType_Kerning_GPOS** - Test kerning pair adjustment (e.g., "AV")
9. **OpenType_Kerning_Disabled** - Test that disabling kern feature removes adjustment
10. **OpenType_Shaper_Integration** - Test OpenTypeShaper integration with layout engine

**Test Resources Needed:**
- OpenType font with ligatures (e.g., Libertinus Serif, EB Garamond, or similar open-source font)
- Font with GPOS kerning table

**Test File Location:** `tests/Folly.Fonts.Tests/OpenTypeFeatureTests.cs`

**Implementation Notes:**
- Focus on liga (common ligatures) and kern (kerning) features only
- Defer Arabic contextual forms (init, medi, fina, isol) to v1.1
- Use open-source test fonts (SIL Open Font License or similar)

---

#### 8.2 CFF/OpenType Font Support

**Deferred from PLAN.md:**
- "Add 10+ tests with real CFF fonts (deferred)"

**Priority:** 3 (Nice to Have)

**Reduced Scope for v1.0:** Basic parsing only (4 tests instead of 10+)

**Test Cases (4 tests):**

1. **CFF_DetectCffFont** - Test detection of CFF vs TrueType fonts
2. **CFF_ParseBasicStructure** - Test parsing of CFF header, INDEX, Top DICT
3. **CFF_StoreRawData** - Test that raw CFF data is stored for embedding
4. **CFF_FontTypeProperty** - Test FontFile.Type property (TrueType vs CFF)

**Test Resources Needed:**
- CFF-based OpenType font (.otf file)

**Test File Location:** `tests/Folly.Fonts.Tests/CffParserTests.cs`

**Implementation Notes:**
- Only test basic infrastructure, not full CharString parsing
- Full CFF subsetting and embedding deferred to v1.1

---

#### 8.3 Font Metadata Accuracy ✅

**Status:** TESTS COMPLETED

- ✅ Font metadata tests already passing (included in baseline)

---

#### 8.4 Kerning Pair Remapping ✅

**Status:** TESTS COMPLETED

- ✅ 8 kerning tests passing (included in baseline)

---

#### 8.5 Font Performance Optimizations ✅

**Status:** TESTS COMPLETED

- ✅ 16 font cache tests passing (included in baseline)

**Note:** Benchmark suite deferred to v1.1 (not critical for v1.0)

---

**Phase 8 Total:** 14 new tests (10 OpenType + 4 CFF)

---

### Phase 9: Image Format Completion

#### 9.1 Interlaced Image Support ⏸️

**Status:** FEATURE DEFERRED TO v1.1

- Interlaced PNG/GIF support not implemented yet
- No tests needed for v1.0

---

#### 9.2 Indexed PNG Transparency ⏸️

**Status:** FEATURE DEFERRED TO v1.1

- Indexed PNG with tRNS chunk not implemented yet
- No tests needed for v1.0

---

#### 9.3 DPI Detection and Scaling

**Deferred from PLAN.md:**
- "Add tests with various DPI values (deferred to future testing phase)"

**Priority:** 1 (Critical)

**Test Cases (10 tests):**

1. **DpiDetection_Jpeg_72Dpi** - JFIF with 72 DPI
2. **DpiDetection_Jpeg_96Dpi** - JFIF with 96 DPI
3. **DpiDetection_Jpeg_150Dpi** - JFIF with 150 DPI
4. **DpiDetection_Jpeg_300Dpi** - JFIF with 300 DPI
5. **DpiDetection_Png_pHYs** - PNG with pHYs chunk at 300 DPI
6. **DpiDetection_DefaultDpi_NoDpiMetadata** - Image without DPI defaults to 72
7. **DpiDetection_ConfigurableDefault** - Test LayoutOptions.DefaultImageDpi
8. **ImageSizing_RespectsDpi_300Dpi** - 300x300px at 300 DPI = 72pt (1 inch)
9. **ImageSizing_RespectsDpi_96Dpi** - 96x96px at 96 DPI = 72pt
10. **ImageSizing_DpiInPdf** - Verify PDF XObject has correct dimensions

**Test Resources Needed:**
- JPEG images at 72, 96, 150, 300 DPI (can create with ImageMagick)
- PNG with pHYs chunk at various DPIs
- Simple test pattern (e.g., 100x100px solid color)

**Test File Location:** `tests/Folly.Pdf.Tests/ImageDpiTests.cs`

**Image Creation Scripts:**
```bash
# Create test images with ImageMagick (if available in environment)
convert -size 300x300 xc:blue -density 72 test-72dpi.jpg
convert -size 300x300 xc:red -density 96 test-96dpi.jpg
convert -size 300x300 xc:green -density 150 test-150dpi.jpg
convert -size 300x300 xc:yellow -density 300 test-300dpi.jpg

# PNG with pHYs chunk
convert -size 300x300 xc:cyan -density 300 test-300dpi.png
```

---

#### 9.4 CMYK Color Support

**Deferred from PLAN.md:**
- "Add 5+ tests with CMYK images (deferred to future testing phase)"

**Priority:** 1 (Critical)

**Test Cases (10 tests):**

1. **Cmyk_DetectCmykJpeg** - Detect 4-component CMYK JPEG
2. **Cmyk_DeviceCmykColorSpace** - PDF uses /DeviceCMYK for CMYK images
3. **Cmyk_ExtractIccProfile_Jpeg** - Extract ICC from JPEG APP2 marker
4. **Cmyk_ExtractIccProfile_Png** - Extract ICC from PNG iCCP chunk
5. **Cmyk_EmbedIccProfile_InPdf** - PDF contains /ICCBased color space
6. **Cmyk_IccProfile_ZlibDecompression** - PNG iCCP zlib decompression works
7. **Cmyk_RgbJpeg_NoIccProfile** - RGB JPEG without ICC uses DeviceRGB
8. **Cmyk_IccProfile_StreamObject** - ICC profile embedded as stream in PDF
9. **Cmyk_MultipleImages_CmykAndRgb** - Document with both CMYK and RGB images
10. **Cmyk_PrintWorkflow** - Integration test with CMYK image in print layout

**Test Resources Needed:**
- CMYK JPEG image (can create or find CC0/public domain)
- RGB JPEG with ICC profile
- PNG with iCCP chunk
- Simple test patterns in CMYK color space

**Test File Location:** `tests/Folly.Pdf.Tests/ImageCmykTests.cs`

**Resource Acquisition:**
- Search for CC0/public domain CMYK test images
- Or create with GIMP (supports CMYK via decompose plugin)
- PNG with iCCP: can embed sRGB profile in test PNG

---

**Phase 9 Total:** 20 new tests (10 DPI + 10 CMYK)

---

### Phase 10: Text Layout & Typography

#### 10.1 CJK Line Breaking ⏸️

**Status:** FEATURE DEFERRED TO v1.1

- UAX#14 and kinsoku shori not implemented yet
- Very complex feature, not blocking v1.0

---

#### 10.2 BiDi Paired Bracket Algorithm

**Deferred from PLAN.md:**
- "Add 10+ tests with nested brackets in RTL (deferred to future testing phase)"

**Priority:** 2 (Important)

**Test Cases (10 tests):**

1. **BiDi_Brackets_Ascii_Parentheses** - Test () in RTL context
2. **BiDi_Brackets_Ascii_Square** - Test [] in RTL context
3. **BiDi_Brackets_Ascii_Curly** - Test {} in RTL context
4. **BiDi_Brackets_Ascii_Angle** - Test <> in RTL context
5. **BiDi_Brackets_Unicode_Quotes** - Test "", '', ‹›, «»
6. **BiDi_Brackets_Cjk** - Test 「」, 『』, 【】, etc.
7. **BiDi_Brackets_Nested_TwoLevels** - Test (inner [nested])
8. **BiDi_Brackets_Nested_ThreeLevels** - Test (a [b {c} b] a)
9. **BiDi_Brackets_Mixed_LtrRtl** - Test brackets in mixed LTR/RTL text
10. **BiDi_Brackets_Integration** - Integration test with Hebrew/Arabic text

**Test Resources Needed:**
- None (pure Unicode text strings)

**Test File Location:** `tests/Folly.Core.Tests/BiDi/PairedBracketTests.cs`

**Implementation Notes:**
- Focus on algorithm correctness, not visual output
- Test that paired brackets get correct directionality
- Verify embedding levels are computed correctly

---

#### 10.3 Configurable Knuth-Plass Parameters

**Deferred from PLAN.md:**
- "Add tests with various parameter values (deferred to future testing phase)"

**Priority:** 2 (Important)

**Test Cases (8 tests):**

1. **KnuthPlass_DefaultParameters** - Verify default values
2. **KnuthPlass_CustomStretchRatio** - Test custom SpaceStretchRatio
3. **KnuthPlass_CustomShrinkRatio** - Test custom SpaceShrinkRatio
4. **KnuthPlass_CustomTolerance** - Test custom Tolerance
5. **KnuthPlass_TightTolerance** - Test tight tolerance (0.5) produces fewer breaks
6. **KnuthPlass_LooseTolerance** - Test loose tolerance (2.0) produces more breaks
7. **KnuthPlass_CustomPenalties** - Test custom LinePenalty, FlaggedDemerit, etc.
8. **KnuthPlass_Integration** - Integration test with actual paragraph layout

**Test Resources Needed:**
- None (use test paragraphs)

**Test File Location:** `tests/Folly.Core.Tests/Layout/KnuthPlassConfigTests.cs`

**Implementation Notes:**
- Test that parameters are passed through to KnuthPlassLineBreaker
- Compare break quality with different parameters
- Verify no crashes with extreme parameter values

---

#### 10.4 Additional Hyphenation Languages ⏸️

**Status:** FEATURE DEFERRED TO v1.1

- Only 4 languages currently (en, de, fr, es)
- Adding 10+ more languages is nice-to-have, not critical

---

**Phase 10 Total:** 18 new tests (10 BiDi + 8 Knuth-Plass)

---

### Phase 11: Layout Engine Enhancements

#### 11.1 Advanced Marker Retrieval

**Deferred from PLAN.md:**
- "Add 10+ tests for marker retrieval - deferred"

**Priority:** 2 (Important)

**Test Cases (10 tests):**

1. **Marker_FirstStartingWithinPage** - Retrieve first marker that starts on page
2. **Marker_LastEndingWithinPage** - Retrieve last marker on page
3. **Marker_FirstIncludingCarryover** - Retrieve from previous page if none on current
4. **Marker_LastStartingWithinPage** - Last marker that starts (not continues)
5. **Marker_Carryover_AcrossPages** - Test carryover from page N to page N+1
6. **Marker_MultipleMarkers_SamePage** - Multiple markers with same class name
7. **Marker_SequenceNumbers** - Verify sequence number tracking
8. **Marker_NoMarker_ReturnsNull** - No marker found returns null
9. **Marker_MarkerScope_PageSequence** - Markers scoped to page sequence
10. **Marker_Integration_RunningHeader** - Integration test with running header

**Test Resources Needed:**
- None (create XSL-FO snippets)

**Test File Location:** `tests/Folly.Core.Tests/Layout/MarkerRetrievalTests.cs`

**Implementation Notes:**
- Test all 4 retrieve positions
- Verify carryover logic works correctly
- Test edge cases (no markers, first page, last page)

---

#### 11.2 Proportional and Auto Column Widths

**Deferred from PLAN.md:**
- "Add 5+ tests for complex column scenarios - deferred"

**Priority:** 1 (Critical)

**Test Cases (8 tests):**

1. **ColumnWidth_Percentage_Single** - Single column with 25% width
2. **ColumnWidth_Percentage_Multiple** - Three columns: 25%, 50%, 25%
3. **ColumnWidth_Proportional_Equal** - Three 1* columns (equal width)
4. **ColumnWidth_Proportional_Weighted** - Columns: 1*, 2*, 1* (weighted)
5. **ColumnWidth_Auto_ContentBased** - Auto columns sized to content
6. **ColumnWidth_Mixed** - Mix of fixed, percentage, proportional, auto
7. **ColumnWidth_Percentage_OverConstraint** - Percentages > 100% (clamping)
8. **ColumnWidth_MinimumWidth** - Verify MinimumColumnWidth constraint

**Test Resources Needed:**
- None (create table FO snippets)

**Test File Location:** `tests/Folly.Core.Tests/Layout/ColumnWidthTests.cs`

**Implementation Notes:**
- Test CalculateColumnWidths method
- Verify widths sum to table width
- Test edge cases (over-constrained, under-constrained)

---

#### 11.3 Content-Based Float Sizing

**Deferred from PLAN.md:**
- "Add 5+ tests with auto-sized floats - deferred"

**Priority:** 2 (Important)

**Test Cases (6 tests):**

1. **FloatWidth_Explicit_Absolute** - Float with explicit width="100pt"
2. **FloatWidth_Explicit_Percentage** - Float with width="25%"
3. **FloatWidth_Auto_ContentBased** - Float with width="auto" measures content
4. **FloatWidth_Auto_MaxConstraint** - Auto width clamped to 1/3 body width
5. **FloatWidth_Auto_MinimumConstraint** - Auto width respects MinimumColumnWidth
6. **FloatWidth_Integration** - Integration test with float in multi-column layout

**Test Resources Needed:**
- None (create float FO snippets)

**Test File Location:** `tests/Folly.Core.Tests/Layout/FloatSizingTests.cs`

**Implementation Notes:**
- Test CalculateFloatWidth method
- Test MeasureFloatMinimumWidth
- Verify constraints applied correctly

---

#### 11.4 Additional Keep/Break Controls

**Deferred from PLAN.md:**
- "Add 10+ tests for advanced keep/break scenarios - deferred"

**Priority:** 2 (Important)

**Test Cases (8 tests):**

1. **Keep_IntegerStrength_Basic** - Test keep-together with strength values
2. **Keep_IntegerStrength_Comparison** - Strength 1 vs 999 prioritization
3. **ForcePageCount_Even** - Add blank page if sequence ends on odd page
4. **ForcePageCount_Odd** - Add blank page if sequence ends on even page
5. **ForcePageCount_EndOnEven** - Ensure last page is even-numbered
6. **ForcePageCount_EndOnOdd** - Ensure last page is odd-numbered
7. **ForcePageCount_Auto** - No forced blank pages
8. **ForcePageCount_Integration** - Multi-sequence document with force-page-count

**Test Resources Needed:**
- None (create page sequence FO snippets)

**Test File Location:** `tests/Folly.Core.Tests/Layout/KeepBreakTests.cs`

**Implementation Notes:**
- Test GetKeepStrength method
- Test ApplyForcePageCount method
- Verify blank pages added correctly

**Note:** Span property deferred to v1.1 (requires column layout refactoring)

---

**Phase 11 Total:** 32 new tests (10 markers + 8 columns + 6 floats + 8 keep/break)

---

### Phase 12: PDF Generation Enhancements

#### 12.1 PDF/A Compliance

**Deferred from PLAN.md:**
- "Add 10+ tests for PDF/A compliance (deferred)"

**Priority:** 1 (Critical)

**Test Cases (12 tests):**

1. **PdfA_Disabled_ByDefault** - Default options have PdfACompliance = None
2. **PdfA2b_XmpMetadata_Included** - XMP packet in PDF
3. **PdfA2b_XmpMetadata_Part** - pdfaid:part = 2
4. **PdfA2b_XmpMetadata_Conformance** - pdfaid:conformance = B
5. **PdfA2b_OutputIntent_Included** - /OutputIntents in catalog
6. **PdfA2b_OutputIntent_IccProfile** - ICC profile embedded
7. **PdfA2b_Version_Correct** - PDF version is 1.7
8. **PdfA2b_Validation_FontsEmbedded** - Throws if EmbedFonts=false
9. **PdfA1b_Metadata** - PDF/A-1b produces part=1
10. **PdfA3b_Metadata** - PDF/A-3b produces part=3
11. **PdfA_DublinCore_Metadata** - Title, creator, description in XMP
12. **PdfA_Integration** - Full document with PDF/A-2b enabled

**Test Resources Needed:**
- None (validate PDF structure)

**Test File Location:** `tests/Folly.Pdf.Tests/PdfAComplianceTests.cs`

**Implementation Notes:**
- Parse generated PDF to verify structure
- Check for XMP metadata stream
- Verify OutputIntent object
- Could use external validator (VeraPDF) if available

---

#### 12.2 PDF 2.0 Support ⏸️

**Status:** FEATURE DEFERRED TO v1.1

---

#### 12.3 Digital Signatures ⏸️

**Status:** FEATURE DEFERRED TO v1.1

---

#### 12.4 Streaming PDF Generation ⏸️

**Status:** FEATURE DEFERRED TO v1.1

---

**Phase 12 Total:** 12 new tests (PDF/A only)

---

### Phase 13: Missing XSL-FO Features

#### 13.1 Table Captions

**Deferred from PLAN.md:**
- "Add 5+ tests (deferred to future testing phase)"

**Priority:** 2 (Important)

**Test Cases (6 tests):**

1. **TableCaption_Before** - Caption positioned before table
2. **TableCaption_After** - Caption positioned after table
3. **TableCaption_Start** - Caption at start (same as before)
4. **TableCaption_End** - Caption at end (same as after)
5. **TableCaption_Styling** - Caption with custom font, color, etc.
6. **TableCaption_MultiPage** - Caption only on first page of multi-page table

**Test Resources Needed:**
- None (create table-and-caption FO snippets)

**Test File Location:** `tests/Folly.Core.Tests/Layout/TableCaptionTests.cs`

**Implementation Notes:**
- Test LayoutTableAndCaptionWithPageBreaking method
- Verify caption area order (before vs after)
- Test caption-side property parsing

---

#### 13.2 Retrieve Table Marker

**Deferred from PLAN.md:**
- "Add 3+ tests (deferred to future testing phase)"

**Priority:** 2 (Important)

**Test Cases (4 tests):**

1. **TableMarker_FirstStarting** - Retrieve first marker in table
2. **TableMarker_LastEnding** - Retrieve last marker in table
3. **TableMarker_TableScope** - Markers scoped to table only
4. **TableMarker_Integration** - Integration test with table header retrieval

**Test Resources Needed:**
- None (create table FO with markers)

**Test File Location:** `tests/Folly.Core.Tests/Layout/TableMarkerTests.cs`

**Implementation Notes:**
- Test RetrieveTableMarkerContent method
- Verify table marker tracking separate from page markers
- Test marker clearing at table boundaries

---

#### 13.3 Multi-Property Elements

**Deferred from PLAN.md:**
- "Add 3+ tests (deferred to future testing phase)"

**Priority:** 3 (Nice to Have)

**Test Cases (3 tests):**

1. **MultiSwitch_SelectsFirstCase** - multi-switch selects first case by default
2. **MultiSwitch_StartingState** - Respects starting-state="show" attribute
3. **MultiProperties_StaticRendering** - multi-properties renders wrapper

**Test Resources Needed:**
- None (create multi-* FO snippets)

**Test File Location:** `tests/Folly.Core.Tests/Layout/MultiPropertyTests.cs`

**Implementation Notes:**
- Test static rendering mode only (no interactivity)
- Verify parsing of all multi-* elements
- Test that first case is selected

---

#### 13.4 Index Generation

**Deferred from PLAN.md:**
- "Add 5+ tests (deferred to future testing phase)"

**Priority:** 2 (Important)

**Test Cases (6 tests):**

1. **Index_RangeTracking** - Track index-range-begin to index-range-end
2. **Index_PageNumbers** - Generate correct page numbers for index entries
3. **Index_PageRanges** - Merge sequential pages into ranges (5-8)
4. **Index_MergeSequential_Enabled** - merge-sequential-page-numbers="true"
5. **Index_CustomSeparators** - Custom list and range separators
6. **Index_Sorting** - Index entries sorted by page number

**Test Resources Needed:**
- None (create index FO snippets)

**Test File Location:** `tests/Folly.Core.Tests/Layout/IndexGenerationTests.cs`

**Implementation Notes:**
- Test TrackIndexElements method
- Test GenerateIndexContent method
- Verify sorting and range merging

---

#### 13.5 Visibility, Clip, and Overflow

**Deferred from PLAN.md:**
- "Add 10+ tests (deferred to future testing phase)"

**Priority:** 1 (Critical)

**Test Cases (12 tests):**

1. **Visibility_Visible** - Visible content rendered
2. **Visibility_Hidden** - Hidden content not rendered
3. **Visibility_Collapsed** - Collapsed content not rendered
4. **Visibility_Inheritance** - Visibility inherited from parent
5. **Clip_Rect_Absolute** - Clip with absolute lengths (10pt, 50pt, etc.)
6. **Clip_Rect_Percentage** - Clip with percentages (0%, 50%, 100%)
7. **Clip_Auto** - No clipping when clip="auto"
8. **Clip_PdfOperators** - PDF contains W and n operators
9. **Overflow_Visible** - No clipping for overflow:visible
10. **Overflow_Hidden** - Clipping applied for overflow:hidden
11. **Overflow_Integration** - Block container with overflowing content
12. **Clip_Overflow_Combined** - Both clip and overflow applied

**Test Resources Needed:**
- None (create block/block-container FO snippets)

**Test File Location:** `tests/Folly.Pdf.Tests/VisibilityClipOverflowTests.cs`

**Implementation Notes:**
- Test PDF rendering (check for PDF operators)
- Test layout (verify areas have properties set)
- Test ParseClipRect helper method

---

**Phase 13 Total:** 31 new tests (6 caption + 4 table marker + 3 multi + 6 index + 12 visibility)

---

## Test Priority Summary

### Priority 1: Critical (Must Have for v1.0)

**Total: 50 tests**

| Category | Tests | Test File |
|----------|-------|-----------|
| DPI Detection (9.3) | 10 | ImageDpiTests.cs |
| CMYK/ICC (9.4) | 10 | ImageCmykTests.cs |
| Column Widths (11.2) | 8 | ColumnWidthTests.cs |
| PDF/A (12.1) | 12 | PdfAComplianceTests.cs |
| Visibility/Clip/Overflow (13.5) | 12 | VisibilityClipOverflowTests.cs |

**Rationale:** These tests validate core features that users will immediately notice. DPI/CMYK are essential for print workflows, column widths are commonly used in tables, PDF/A is required for archival, and visibility/clip/overflow affect visual output.

---

### Priority 2: Important (Should Have)

**Total: 44 tests**

| Category | Tests | Test File |
|----------|-------|-----------|
| OpenType Features (8.1) | 10 | OpenTypeFeatureTests.cs |
| BiDi Brackets (10.2) | 10 | PairedBracketTests.cs |
| Knuth-Plass Config (10.3) | 8 | KnuthPlassConfigTests.cs |
| Marker Retrieval (11.1) | 10 | MarkerRetrievalTests.cs |
| Float Sizing (11.3) | 6 | FloatSizingTests.cs |
| Keep/Break (11.4) | 8 | KeepBreakTests.cs |
| Table Captions (13.1) | 6 | TableCaptionTests.cs |
| Table Markers (13.2) | 4 | TableMarkerTests.cs |
| Index Generation (13.4) | 6 | IndexGenerationTests.cs |

**Rationale:** These tests cover important features that enhance quality and spec compliance. Users may not immediately need them, but they're important for professional documents.

---

### Priority 3: Nice to Have

**Total: 16 tests**

| Category | Tests | Test File |
|----------|-------|-----------|
| CFF Fonts (8.2) | 4 | CffParserTests.cs |
| Multi-Property (13.3) | 3 | MultiPropertyTests.cs |

**Rationale:** These test edge cases and less commonly used features. CFF fonts are rare in typical workflows, and multi-property elements are seldom used in print PDFs. Can defer to v1.1 if time is short.

---

## Test Resource Requirements

### Fonts

**Required:**
- ✅ **Open-source OpenType font with ligatures** (e.g., Libertinus Serif, EB Garamond)
  - License: SIL Open Font License
  - Features needed: liga (fi, fl, ffi, ffl), kern
  - Size: ~200-500 KB

- ⚠️ **CFF-based OpenType font** (.otf file)
  - License: SIL OFL or similar
  - Needed for: CFF parser tests (Priority 3)
  - Size: ~100-300 KB

**Acquisition Plan:**
- Search Google Fonts for suitable open-source fonts
- Download and include in `tests/Folly.Fonts.Tests/TestResources/`
- Document licenses in `tests/Folly.Fonts.Tests/TestResources/LICENSES.txt`

---

### Images

**Required:**

1. **DPI Test Images (10 files)**
   - JPEG at 72, 96, 150, 300 DPI (4 files)
   - PNG with pHYs at 300 DPI (1 file)
   - Images without DPI metadata (1 file)
   - Simple patterns: 100x100px solid colors
   - Total size: ~500 KB

2. **CMYK Test Images (4 files)**
   - CMYK JPEG (1 file)
   - RGB JPEG with ICC profile (1 file)
   - PNG with iCCP chunk (1 file)
   - Simple test pattern: 200x200px
   - Total size: ~300 KB

**Creation Methods:**

**Option A: ImageMagick** (if available in environment)
```bash
# Install ImageMagick
apt-get install -y imagemagick

# Create DPI test images
convert -size 100x100 xc:blue -density 72 -quality 90 test-72dpi.jpg
convert -size 100x100 xc:red -density 96 -quality 90 test-96dpi.jpg
convert -size 100x100 xc:green -density 150 -quality 90 test-150dpi.jpg
convert -size 100x100 xc:yellow -density 300 -quality 90 test-300dpi.jpg
convert -size 100x100 xc:cyan -density 300 test-300dpi.png

# Create CMYK JPEG
convert -size 200x200 xc:blue -colorspace CMYK -quality 90 test-cmyk.jpg

# Create RGB JPEG with sRGB ICC profile
convert -size 200x200 xc:magenta -profile sRGB.icc test-rgb-icc.jpg
```

**Option B: Programmatic Creation** (C# code)
- Create minimal test images programmatically in test setup
- Use System.Drawing or SkiaSharp (dev dependency only)
- Embed DPI in JFIF/pHYs chunks manually

**Option C: Public Domain Images**
- Search Wikimedia Commons for CC0 test images
- Ensure proper attribution and licensing

**Storage Location:** `tests/Folly.Pdf.Tests/TestResources/Images/`

---

## Test Infrastructure

### New Test Files to Create

1. `tests/Folly.Fonts.Tests/OpenTypeFeatureTests.cs` (10 tests)
2. `tests/Folly.Fonts.Tests/CffParserTests.cs` (4 tests)
3. `tests/Folly.Pdf.Tests/ImageDpiTests.cs` (10 tests)
4. `tests/Folly.Pdf.Tests/ImageCmykTests.cs` (10 tests)
5. `tests/Folly.Core.Tests/BiDi/PairedBracketTests.cs` (10 tests)
6. `tests/Folly.Core.Tests/Layout/KnuthPlassConfigTests.cs` (8 tests)
7. `tests/Folly.Core.Tests/Layout/MarkerRetrievalTests.cs` (10 tests)
8. `tests/Folly.Core.Tests/Layout/ColumnWidthTests.cs` (8 tests)
9. `tests/Folly.Core.Tests/Layout/FloatSizingTests.cs` (6 tests)
10. `tests/Folly.Core.Tests/Layout/KeepBreakTests.cs` (8 tests)
11. `tests/Folly.Pdf.Tests/PdfAComplianceTests.cs` (12 tests)
12. `tests/Folly.Core.Tests/Layout/TableCaptionTests.cs` (6 tests)
13. `tests/Folly.Core.Tests/Layout/TableMarkerTests.cs` (4 tests)
14. `tests/Folly.Core.Tests/Layout/MultiPropertyTests.cs` (3 tests)
15. `tests/Folly.Core.Tests/Layout/IndexGenerationTests.cs` (6 tests)
16. `tests/Folly.Pdf.Tests/VisibilityClipOverflowTests.cs` (12 tests)

**Total: 16 new test files**

---

### Test Helper Utilities Needed

**1. PDF Content Extraction Helper**

```csharp
// tests/Folly.Pdf.Tests/Helpers/PdfContentHelper.cs
public static class PdfContentHelper
{
    public static string GetPdfHeader(byte[] pdf)
    {
        // Extract "%PDF-1.7" header
    }

    public static string GetPdfContent(byte[] pdf)
    {
        // Extract full PDF as text for searching
    }

    public static string GetPageContent(byte[] pdf, int pageIndex = 0)
    {
        // Extract specific page stream content
    }

    public static int CountOccurrences(string content, string pattern)
    {
        // Count pattern occurrences
    }

    public static bool ContainsPdfOperator(byte[] pdf, string op)
    {
        // Check for PDF operators (W, n, re, etc.)
    }
}
```

**2. FO Snippet Builder Helper**

```csharp
// tests/Folly.Core.Tests/Helpers/FoSnippetBuilder.cs
public static class FoSnippetBuilder
{
    public static FoDocument CreateSimpleDocument(Action<FoFlow> flowBuilder)
    {
        // Create minimal FO document with simple page master
    }

    public static FoBlock CreateBlockWithVisibility(string content, string visibility)
    {
        // Create block with visibility property
    }

    public static FoTable CreateTableWithColumnWidths(string[] widths)
    {
        // Create table with specified column widths
    }

    public static FoFloat CreateFloat(string width, Action<FoBlock> contentBuilder)
    {
        // Create float with specified width
    }
}
```

**3. Test Resource Locator**

```csharp
// tests/TestHelpers/ResourceLocator.cs
public static class TestResourceLocator
{
    public static string GetFontPath(string fontName)
    {
        // Return path to test font resources
    }

    public static string GetImagePath(string imageName)
    {
        // Return path to test image resources
    }

    public static byte[] LoadFont(string fontName)
    {
        // Load font file bytes
    }

    public static byte[] LoadImage(string imageName)
    {
        // Load image file bytes
    }
}
```

---

## Test Execution Plan

### Phase 1: Setup (Days 1-2)

**Day 1:**
- ✅ Create test audit document (this file)
- Create test resource directories
- Set up test helper utilities
- Create skeleton test files (16 files)

**Day 2:**
- Acquire/create test resources (fonts, images)
- Verify test infrastructure compiles
- Run baseline tests (485 should still pass)

---

### Phase 2: Priority 1 Tests (Days 3-8)

**Days 3-4: Image Tests (20 tests)**
- ImageDpiTests.cs (10 tests)
- ImageCmykTests.cs (10 tests)

**Days 5-6: Table Tests (8 tests)**
- ColumnWidthTests.cs (8 tests)

**Day 7: PDF/A Tests (12 tests)**
- PdfAComplianceTests.cs (12 tests)

**Day 8: Visibility Tests (12 tests)**
- VisibilityClipOverflowTests.cs (12 tests)

**Milestone:** 52 Priority 1 tests completed

---

### Phase 3: Priority 2 Tests (Days 9-14)

**Days 9-10: Font & Typography (18 tests)**
- OpenTypeFeatureTests.cs (10 tests)
- PairedBracketTests.cs (10 tests)

**Days 11-12: Layout Engine (24 tests)**
- KnuthPlassConfigTests.cs (8 tests)
- MarkerRetrievalTests.cs (10 tests)
- FloatSizingTests.cs (6 tests)

**Days 13-14: Advanced Features (16 tests)**
- KeepBreakTests.cs (8 tests)
- TableCaptionTests.cs (6 tests)
- TableMarkerTests.cs (4 tests)
- IndexGenerationTests.cs (6 tests)

**Milestone:** 58 Priority 2 tests completed (110 total with Priority 1)

---

### Phase 4: Priority 3 Tests (Optional - Days 15-16)

**Days 15-16: Edge Cases (7 tests)**
- CffParserTests.cs (4 tests)
- MultiPropertyTests.cs (3 tests)

**Milestone:** 117 total tests if all priorities completed

---

### Phase 5: Verification (Days 17-18)

**Day 17:**
- Run full test suite (target: 595+ passing)
- Fix any test failures
- Verify no regressions in existing tests

**Day 18:**
- Code coverage analysis
- Identify any critical gaps
- Document test results

---

## Success Metrics

### Quantitative Metrics

- ✅ **Total tests:** 595+ passing (485 baseline + 110 new)
- ✅ **Test pass rate:** 100% (no failing tests)
- ✅ **Priority 1 coverage:** 100% (all 52 tests implemented)
- ✅ **Priority 2 coverage:** 100% (all 58 tests implemented)
- ✅ **Priority 3 coverage:** Target 50%+ (nice to have)
- ✅ **Code coverage:** Maintain or improve current coverage
- ✅ **No regressions:** All existing 485 tests still pass

### Qualitative Metrics

- ✅ All new features from Phases 7-13 have test coverage
- ✅ Tests are well-documented with clear intent
- ✅ Test resources properly licensed and attributed
- ✅ Test infrastructure reusable for future tests
- ✅ No flaky tests (all tests deterministic)

---

## Risk Assessment

### Risk: Test Resource Acquisition Delays

**Probability:** Medium
**Impact:** Medium

**Mitigation:**
- Start with programmatic image generation (no external dependencies)
- Use open-source fonts from Google Fonts (readily available)
- Can defer CMYK tests if resources not available

---

### Risk: Test Implementation Takes Longer Than Expected

**Probability:** Medium
**Impact:** Medium

**Mitigation:**
- Prioritization allows dropping Priority 3 tests if needed
- Can parallelize test writing (fonts vs images vs layout)
- Focus on breadth over depth initially

---

### Risk: Tests Reveal Bugs in Implementation

**Probability:** High
**Impact:** High (this is actually good!)

**Mitigation:**
- Allocate buffer time for bug fixes
- Tests revealing bugs = quality improvement
- May extend timeline but improves release quality

---

### Risk: Existing Tests Break During Development

**Probability:** Low
**Impact:** High

**Mitigation:**
- Run existing tests after each new test file
- Use CI/CD to catch regressions early
- Don't modify production code unless fixing bugs

---

## Next Steps

1. **Review and approve this audit** - Confirm priorities and scope
2. **Set up test infrastructure** - Create directories, helpers, skeleton files
3. **Acquire test resources** - Download fonts, create/find images
4. **Begin Priority 1 tests** - Start with ImageDpiTests.cs
5. **Daily progress tracking** - Update completion status

**Estimated Timeline:** 18 days for all Priority 1 & 2 tests (110 tests)

**Ready to proceed?** Awaiting approval to begin test infrastructure setup.
