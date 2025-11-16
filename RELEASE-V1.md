# Folly v1.0 Release Preparation Plan

## Executive Summary

This document outlines the testing and documentation work required to prepare Folly for its first production release (v1.0). Rather than adding new features, we focus on ensuring all completed features (Phases 7-13) are thoroughly tested, well-documented, and production-ready.

**Current Status:**
- 485 passing tests (364 unit + 20 spec + 101 font tests)
- ~80-85% XSL-FO 1.1 compliance
- Excellent performance (~150ms for 200 pages)
- Zero dependencies beyond .NET 8
- Phases 7, 8, 9 (partial), 10 (partial), 11, 12 (partial), 13 completed

**Release Goal:** Ship a high-quality, production-ready v1.0 with comprehensive testing and documentation

**Timeline:** 4-6 weeks

---

## Phase 1: Testing Audit & Gap Analysis (Week 1)

### 1.1 Inventory Deferred Tests

Review PLAN.md and catalog all deferred tests from completed phases:

**Phase 7: Critical Issues**
- ✅ All tests completed (image error handling, font memory management)

**Phase 8: Font System**
- ❌ Phase 8.1: 20+ tests for OpenType features (ligatures, contextual alternates, Arabic features)
- ❌ Phase 8.2: 10+ tests for CFF font parsing and embedding
- ✅ Phase 8.3: Font metadata tests (completed)
- ✅ Phase 8.4: Kerning tests (8/8 passing)
- ✅ Phase 8.5: Font performance tests (16 tests completed)
- ⚠️  Phase 8.5: Benchmark suite deferred

**Phase 9: Image Formats**
- ❌ Phase 9.3: DPI detection tests with various DPI values
- ❌ Phase 9.4: CMYK JPEG tests with ICC profiles
- ⏸️ Phase 9.1: Interlaced images (feature deferred)
- ⏸️ Phase 9.2: Indexed PNG transparency (feature deferred)

**Phase 10: Text Layout & Typography**
- ❌ Phase 10.2: 10+ tests for BiDi paired brackets in RTL
- ❌ Phase 10.3: Tests for Knuth-Plass configurable parameters
- ⏸️ Phase 10.1: CJK line breaking (feature deferred)
- ⏸️ Phase 10.4: Additional hyphenation languages (feature deferred)

**Phase 11: Layout Engine**
- ❌ Phase 11.1: 10+ tests for advanced marker retrieval positions
- ❌ Phase 11.2: 5+ tests for proportional and percentage column widths
- ❌ Phase 11.3: 5+ tests for content-based float sizing
- ❌ Phase 11.4: 10+ tests for keep/break controls

**Phase 12: PDF Generation**
- ❌ Phase 12.1: 10+ tests for PDF/A compliance validation
- ⏸️ Phase 12.2: PDF 2.0 (feature deferred)
- ⏸️ Phase 12.3: Digital signatures (feature deferred)
- ⏸️ Phase 12.4: Streaming PDF (feature deferred)

**Phase 13: XSL-FO Features**
- ❌ Phase 13.1: 5+ tests for table captions (before/after/start/end positioning)
- ❌ Phase 13.2: 3+ tests for retrieve-table-marker
- ❌ Phase 13.3: 3+ tests for multi-property elements
- ❌ Phase 13.4: 5+ tests for index generation
- ❌ Phase 13.5: 10+ tests for visibility/clip/overflow

**Total Deferred Tests:** ~110+ tests needed

### 1.2 Prioritize Test Coverage

**Priority 1 (Critical - Must Have for v1.0):**
- Phase 9.3: DPI detection tests
- Phase 9.4: CMYK/ICC profile tests
- Phase 11.2: Column width tests (percentage, proportional, auto)
- Phase 12.1: PDF/A compliance tests
- Phase 13.5: Visibility/clip/overflow tests

**Priority 2 (Important - Should Have):**
- Phase 8.1: OpenType ligature tests (common features only)
- Phase 10.2: BiDi bracket tests
- Phase 10.3: Knuth-Plass parameter tests
- Phase 11.1: Marker retrieval tests
- Phase 13.1: Table caption tests

**Priority 3 (Nice to Have):**
- Phase 8.2: CFF font tests (basic parsing only)
- Phase 11.3: Float sizing tests
- Phase 11.4: Keep/break tests
- Phase 13.2-13.4: Advanced XSL-FO features

### 1.3 Test Infrastructure Assessment

**Existing Test Infrastructure:**
- ✅ Unit test framework (xUnit)
- ✅ XSL-FO conformance tests
- ✅ AreaTree snapshot testing
- ✅ PDF validation (qpdf)
- ✅ Fuzzing/stress tests

**Needed Additions:**
- Visual regression testing (compare PDF renders)
- Font test resources (OpenType, CFF fonts)
- Image test resources (various DPI, CMYK JPEGs)
- Performance benchmark harness

---

## Phase 2: Critical Test Implementation (Weeks 2-3)

### 2.1 Image Format Tests (Priority 1)

**Phase 9.3: DPI Detection Tests**

Test files needed:
- JPEG at 72, 96, 150, 300 DPI
- PNG with pHYs chunk at various DPIs
- BMP with DPI metadata
- GIF with DPI metadata
- TIFF with DPI in IFD

Test cases (8 tests):
```csharp
[Fact]
public void DetectJpegDpi_JFIF_72Dpi()
{
    var info = JpegParser.Parse("test-72dpi.jpg");
    Assert.Equal(72, info.DpiX);
    Assert.Equal(72, info.DpiY);
}

[Fact]
public void DetectJpegDpi_JFIF_300Dpi()
{
    var info = JpegParser.Parse("test-300dpi.jpg");
    Assert.Equal(300, info.DpiX);
    Assert.Equal(300, info.DpiY);
}

[Fact]
public void DetectPngDpi_pHYs()
{
    var info = PngParser.Parse("test-300dpi.png");
    Assert.Equal(300, info.DpiX);
    Assert.Equal(300, info.DpiY);
}

[Fact]
public void ImageSizing_RespectsDpi()
{
    // 300x300px image at 300 DPI should be 72pt x 72pt (1 inch)
    var fo = CreateImageFo("test-300dpi.jpg");
    var area = LayoutEngine.LayoutImage(fo);
    Assert.Equal(72.0, area.Width, 0.1);
    Assert.Equal(72.0, area.Height, 0.1);
}

[Fact]
public void DefaultDpi_WhenNoDpiMetadata()
{
    var info = JpegParser.Parse("no-dpi-metadata.jpg");
    Assert.Equal(72, info.DpiX); // Default
    Assert.Equal(72, info.DpiY);
}

[Fact]
public void ConfigurableDefaultDpi()
{
    var options = new LayoutOptions { DefaultImageDpi = 96 };
    var engine = new LayoutEngine(options);
    // ... assert 96 DPI used as default
}
```

**Phase 9.4: CMYK and ICC Profile Tests**

Test files needed:
- CMYK JPEG (Adobe RGB, CMYK colorspace)
- RGB JPEG with ICC profile
- PNG with iCCP chunk

Test cases (10 tests):
```csharp
[Fact]
public void DetectCmykJpeg()
{
    var info = JpegParser.Parse("cmyk-test.jpg");
    Assert.Equal(ImageColorSpace.CMYK, info.ColorSpace);
    Assert.Equal(4, info.Components);
}

[Fact]
public void EmbedCmykJpeg_UsesDeviceCMYK()
{
    var fo = CreateImageFo("cmyk-test.jpg");
    var pdf = RenderPdf(fo);

    // Verify PDF contains /DeviceCMYK color space
    Assert.Contains("/DeviceCMYK", GetPdfContent(pdf));
}

[Fact]
public void ExtractIccProfile_FromJpeg()
{
    var info = JpegParser.Parse("jpeg-with-icc.jpg");
    Assert.NotNull(info.IccProfile);
    Assert.True(info.IccProfile.Length > 0);
}

[Fact]
public void ExtractIccProfile_FromPng()
{
    var info = PngParser.Parse("png-with-iccp.png");
    Assert.NotNull(info.IccProfile);
    Assert.True(info.IccProfile.Length > 0);
}

[Fact]
public void EmbedIccProfile_InPdf()
{
    var fo = CreateImageFo("jpeg-with-icc.jpg");
    var pdf = RenderPdf(fo);

    // Verify PDF contains ICCBased color space
    Assert.Contains("/ICCBased", GetPdfContent(pdf));
}
```

**Test Resources:** Need to create or source test images (licensed appropriately)

### 2.2 Table Layout Tests (Priority 1)

**Phase 11.2: Column Width Tests**

Test cases (8 tests):
```csharp
[Fact]
public void PercentageColumnWidth_SingleColumn()
{
    var fo = CreateTableFo(new[] { "25%" }, 400.0);
    var table = LayoutEngine.LayoutTable(fo, 400.0);
    Assert.Equal(100.0, table.Columns[0].Width, 0.1);
}

[Fact]
public void PercentageColumnWidth_MultipleColumns()
{
    var fo = CreateTableFo(new[] { "25%", "50%", "25%" }, 400.0);
    var table = LayoutEngine.LayoutTable(fo, 400.0);
    Assert.Equal(100.0, table.Columns[0].Width, 0.1);
    Assert.Equal(200.0, table.Columns[1].Width, 0.1);
    Assert.Equal(100.0, table.Columns[2].Width, 0.1);
}

[Fact]
public void ProportionalColumnWidth_Equal()
{
    var fo = CreateTableFo(new[] { "1*", "1*", "1*" }, 300.0);
    var table = LayoutEngine.LayoutTable(fo, 300.0);
    Assert.All(table.Columns, col => Assert.Equal(100.0, col.Width, 0.1));
}

[Fact]
public void ProportionalColumnWidth_Weighted()
{
    var fo = CreateTableFo(new[] { "1*", "2*", "1*" }, 400.0);
    var table = LayoutEngine.LayoutTable(fo, 400.0);
    Assert.Equal(100.0, table.Columns[0].Width, 0.1);
    Assert.Equal(200.0, table.Columns[1].Width, 0.1);
    Assert.Equal(100.0, table.Columns[2].Width, 0.1);
}

[Fact]
public void AutoColumnWidth_ContentBased()
{
    var fo = CreateTableFo(new[] { "auto", "auto" }, 400.0);
    // Add content: "Short" and "Very Long Content Text"
    var table = LayoutEngine.LayoutTable(fo, 400.0);

    // Second column should be wider due to content
    Assert.True(table.Columns[1].Width > table.Columns[0].Width);
}

[Fact]
public void MixedColumnWidths()
{
    var fo = CreateTableFo(new[] { "100pt", "25%", "2*", "auto" }, 800.0);
    var table = LayoutEngine.LayoutTable(fo, 800.0);
    Assert.Equal(100.0, table.Columns[0].Width, 0.1); // Fixed
    Assert.Equal(200.0, table.Columns[1].Width, 0.1); // 25% of 800
    // Columns 2-3 share remaining 500pt proportionally
}
```

### 2.3 PDF/A Compliance Tests (Priority 1)

**Phase 12.1: PDF/A Tests**

Test cases (12 tests):
```csharp
[Fact]
public void PdfA2b_XmpMetadata_Included()
{
    var options = new PdfOptions { PdfACompliance = PdfALevel.PdfA2b };
    var pdf = RenderPdf(simpleFo, options);

    Assert.Contains("<?xpacket", GetPdfContent(pdf));
    Assert.Contains("pdfaid:part", GetPdfContent(pdf));
    Assert.Contains("pdfaid:conformance", GetPdfContent(pdf));
}

[Fact]
public void PdfA2b_OutputIntent_Included()
{
    var options = new PdfOptions { PdfACompliance = PdfALevel.PdfA2b };
    var pdf = RenderPdf(simpleFo, options);

    Assert.Contains("/OutputIntents", GetPdfContent(pdf));
    Assert.Contains("/ICCBased", GetPdfContent(pdf));
}

[Fact]
public void PdfA2b_Version_Correct()
{
    var options = new PdfOptions { PdfACompliance = PdfALevel.PdfA2b };
    var pdf = RenderPdf(simpleFo, options);

    Assert.StartsWith("%PDF-1.7", GetPdfHeader(pdf));
}

[Fact]
public void PdfA2b_Validation_FontsEmbedded()
{
    var options = new PdfOptions {
        PdfACompliance = PdfALevel.PdfA2b,
        EmbedFonts = false // Should throw
    };

    Assert.Throws<PdfAComplianceException>(() =>
        RenderPdf(simpleFo, options));
}

[Fact]
public void PdfA_DisabledByDefault()
{
    var options = new PdfOptions(); // Default
    Assert.Equal(PdfALevel.None, options.PdfACompliance);

    var pdf = RenderPdf(simpleFo, options);
    Assert.DoesNotContain("pdfaid:part", GetPdfContent(pdf));
}

[Fact]
public void PdfA1b_Metadata()
{
    var options = new PdfOptions { PdfACompliance = PdfALevel.PdfA1b };
    var pdf = RenderPdf(simpleFo, options);

    Assert.Contains("<pdfaid:part>1</pdfaid:part>", GetPdfContent(pdf));
    Assert.Contains("<pdfaid:conformance>B</pdfaid:conformance>", GetPdfContent(pdf));
}

[Fact]
public void PdfA3b_Metadata()
{
    var options = new PdfOptions { PdfACompliance = PdfALevel.PdfA3b };
    var pdf = RenderPdf(simpleFo, options);

    Assert.Contains("<pdfaid:part>3</pdfaid:part>", GetPdfContent(pdf));
}
```

### 2.4 Visibility/Clip/Overflow Tests (Priority 1)

**Phase 13.5: Rendering Tests**

Test cases (12 tests):
```csharp
[Fact]
public void Visibility_Hidden_NotRendered()
{
    var fo = CreateBlockFo("Hidden Text", visibility: "hidden");
    var pdf = RenderPdf(fo);

    // Hidden content should not appear in PDF stream
    Assert.DoesNotContain("Hidden Text", GetPageContent(pdf));
}

[Fact]
public void Visibility_Visible_Rendered()
{
    var fo = CreateBlockFo("Visible Text", visibility: "visible");
    var pdf = RenderPdf(fo);

    Assert.Contains("Visible Text", GetPageContent(pdf));
}

[Fact]
public void Clip_Rect_Applied()
{
    var fo = CreateBlockFo("Content", clip: "rect(10pt, 100pt, 50pt, 10pt)");
    var pdf = RenderPdf(fo);

    // Should contain PDF clipping operators
    Assert.Contains("W", GetPageContent(pdf)); // Clip operator
    Assert.Contains("n", GetPageContent(pdf)); // End path without stroke
}

[Fact]
public void Clip_WithPercentages()
{
    var fo = CreateBlockFo("Content", clip: "rect(0%, 100%, 50%, 0%)");
    var pdf = RenderPdf(fo);

    // Verify clipping applied (percentages converted to absolute)
    Assert.Contains("W", GetPageContent(pdf));
}

[Fact]
public void Overflow_Hidden_ClipsContent()
{
    var fo = CreateBlockContainerFo(
        width: "100pt",
        height: "50pt",
        overflow: "hidden",
        content: CreateLongText(1000) // Overflowing content
    );
    var pdf = RenderPdf(fo);

    // Should contain clipping rectangle
    Assert.Contains("re", GetPageContent(pdf)); // Rectangle path
    Assert.Contains("W", GetPageContent(pdf));  // Clip
}

[Fact]
public void Overflow_Visible_NoClipping()
{
    var fo = CreateBlockContainerFo(
        width: "100pt",
        overflow: "visible"
    );
    var pdf = RenderPdf(fo);

    var clipCount = CountOccurrences(GetPageContent(pdf), "W");
    Assert.Equal(0, clipCount); // No clipping for overflow:visible
}
```

---

## Phase 3: Important Test Implementation (Week 3-4)

### 3.1 OpenType Feature Tests (Priority 2)

**Phase 8.1: Ligature and Feature Tests**

Focus on common features only (defer complex Arabic shaping):

Test files needed:
- OpenType font with ligatures (fi, fl, ffi, ffl)
- Font with kerning via GPOS

Test cases (10 tests - reduced from 20+):
```csharp
[Fact]
public void OpenType_Ligature_fi()
{
    var font = LoadFont("LibertinusSerif-Regular.otf");
    var shaper = new OpenTypeShaper();

    var glyphs = shaper.Shape("fi", font, new[] { "liga" });

    // "fi" should be replaced with ligature glyph
    Assert.Single(glyphs); // One ligature instead of two characters
}

[Fact]
public void OpenType_Ligature_Disabled()
{
    var font = LoadFont("LibertinusSerif-Regular.otf");
    var shaper = new OpenTypeShaper();

    var glyphs = shaper.Shape("fi", font, Array.Empty<string>()); // No features

    Assert.Equal(2, glyphs.Count); // Two separate glyphs
}

[Fact]
public void OpenType_CommonLigatures()
{
    var font = LoadFont("LibertinusSerif-Regular.otf");
    var shaper = new OpenTypeShaper();

    // Test fi, fl, ffi, ffl, ff
    var testCases = new[] { "fi", "fl", "ffi", "ffl", "ff" };

    foreach (var test in testCases)
    {
        var glyphs = shaper.Shape(test, font, new[] { "liga" });
        Assert.Single(glyphs); // Each should become single ligature
    }
}

[Fact]
public void OpenType_Kerning_GPOS()
{
    var font = LoadFont("font-with-gpos-kern.otf");
    var shaper = new OpenTypeShaper();

    var glyphs = shaper.Shape("AV", font, new[] { "kern" });

    // "AV" pair should have negative kerning (tighter spacing)
    Assert.True(glyphs[0].XAdvance + glyphs[0].XOffset < GetNominalWidth(font, 'A'));
}
```

### 3.2 BiDi and Typography Tests (Priority 2)

**Phase 10.2: BiDi Paired Brackets**

Test cases (8 tests):
```csharp
[Fact]
public void BiDi_Brackets_ASCII_InRtl()
{
    var text = "text (inside) more";
    var levels = BiDiAlgorithm.ComputeLevels(text, isRtl: true);

    // Verify brackets handled correctly in RTL context
    Assert.True(levels['('] >= 1);
}

[Fact]
public void BiDi_Brackets_Nested()
{
    var text = "outer (inner [nested] inner) outer";
    var levels = BiDiAlgorithm.ComputeLevels(text, isRtl: true);

    // Verify nested brackets maintain proper pairing
    // Implementation test - verify algorithm completes without errors
    Assert.NotNull(levels);
}

[Fact]
public void BiDi_Brackets_CJK()
{
    var text = "text 「inside」 more";
    var levels = BiDiAlgorithm.ComputeLevels(text, isRtl: false);

    // CJK brackets should be recognized
    Assert.NotNull(levels);
}
```

**Phase 10.3: Knuth-Plass Configuration**

Test cases (6 tests):
```csharp
[Fact]
public void KnuthPlass_DefaultParameters()
{
    var options = new LayoutOptions();

    Assert.Equal(0.5, options.KnuthPlassSpaceStretchRatio);
    Assert.Equal(0.333, options.KnuthPlassShrinkRatio);
    Assert.Equal(1.0, options.KnuthPlassTolerance);
}

[Fact]
public void KnuthPlass_CustomStretch()
{
    var options = new LayoutOptions {
        KnuthPlassSpaceStretchRatio = 0.7
    };
    var breaker = new KnuthPlassLineBreaker(options);

    // Verify custom ratio used in break calculations
    // (Implementation detail test - verify it doesn't crash)
    var breaks = breaker.FindBreakpoints(CreateTestParagraph(), 300.0);
    Assert.NotEmpty(breaks);
}

[Fact]
public void KnuthPlass_TightTolerance()
{
    var options = new LayoutOptions { KnuthPlassTolerance = 0.5 };
    var breaker = new KnuthPlassLineBreaker(options);

    // Tight tolerance should produce fewer feasible breakpoints
    var breaks = breaker.FindBreakpoints(CreateTestParagraph(), 300.0);
    Assert.NotEmpty(breaks);
}
```

### 3.3 Layout Engine Tests (Priority 2)

**Phase 11.1: Advanced Marker Retrieval**

Test cases (8 tests):
```csharp
[Fact]
public void Marker_FirstStartingWithinPage()
{
    var fo = CreateMarkerFo(
        markers: new[] {
            ("chapter", "Chapter 1", position: "top"),
            ("chapter", "Chapter 2", position: "middle")
        },
        retrieve: "first-starting-within-page"
    );

    var result = LayoutEngine.RetrieveMarker(fo, "chapter");
    Assert.Equal("Chapter 1", result.Content);
}

[Fact]
public void Marker_LastEndingWithinPage()
{
    var fo = CreateMarkerFo(
        markers: new[] {
            ("chapter", "Chapter 1", position: "top"),
            ("chapter", "Chapter 2", position: "bottom")
        },
        retrieve: "last-ending-within-page"
    );

    var result = LayoutEngine.RetrieveMarker(fo, "chapter");
    Assert.Equal("Chapter 2", result.Content);
}

[Fact]
public void Marker_FirstIncludingCarryover()
{
    // Test carryover from previous page
    var fo = CreateMultiPageMarkerFo(
        page1Markers: new[] { ("section", "Intro") },
        page2Markers: new[] { ("section", "Content") },
        page2Retrieve: "first-including-carryover"
    );

    var result = LayoutEngine.RetrieveMarker(fo, "section", page: 2);
    Assert.Equal("Intro", result.Content); // Carried over from page 1
}

[Fact]
public void Marker_LastStartingWithinPage()
{
    var fo = CreateMarkerFo(
        markers: new[] {
            ("title", "Title 1", starts: true),
            ("title", "Title 2", starts: true),
            ("title", "Title 3", starts: false) // Continues from previous page
        },
        retrieve: "last-starting-within-page"
    );

    var result = LayoutEngine.RetrieveMarker(fo, "title");
    Assert.Equal("Title 2", result.Content); // Last one that starts on page
}
```

**Phase 13.1: Table Caption Tests**

Test cases (6 tests):
```csharp
[Fact]
public void TableCaption_Before()
{
    var fo = CreateTableWithCaption(captionSide: "before");
    var areas = LayoutEngine.LayoutTableAndCaption(fo, 400.0);

    Assert.IsType<BlockArea>(areas[0]); // Caption first
    Assert.IsType<TableArea>(areas[1]); // Table second
}

[Fact]
public void TableCaption_After()
{
    var fo = CreateTableWithCaption(captionSide: "after");
    var areas = LayoutEngine.LayoutTableAndCaption(fo, 400.0);

    Assert.IsType<TableArea>(areas[0]); // Table first
    Assert.IsType<BlockArea>(areas[1]); // Caption second
}

[Fact]
public void TableCaption_MultiPage()
{
    var fo = CreateLargeTableWithCaption(rows: 100, captionSide: "before");
    var pages = LayoutEngine.LayoutPages(fo);

    // Caption should only appear on first page
    Assert.Contains("Table Caption", GetPageContent(pages[0]));
    Assert.DoesNotContain("Table Caption", GetPageContent(pages[1]));
}
```

---

## Phase 4: Example Showcase Updates (Week 4)

### 4.1 New Examples Needed

**Phase 8: Font System Examples**

1. **Example 44: OpenType Ligatures**
   - Demonstrate fi, fl, ffi, ffl ligatures
   - Side-by-side comparison with/without ligatures
   - Professional font embedding

2. **Example 45: Advanced Typography**
   - OpenType features showcase
   - Kerning demonstration
   - Font fallback chains

**Phase 9: Image Examples**

3. **Example 46: High-DPI Images**
   - Same image at 72, 150, 300 DPI
   - Demonstrate correct sizing
   - Print vs screen DPI

4. **Example 47: CMYK Printing**
   - CMYK JPEG embedding
   - ICC profile demonstration
   - Color space comparison

**Phase 10: Typography Examples**

5. **Example 48: BiDi Advanced**
   - Paired brackets in RTL
   - Mixed nested content
   - Complex Arabic/Hebrew text

6. **Example 49: Line Breaking Control**
   - Knuth-Plass parameter tuning
   - Tight vs loose justification
   - Custom typography settings

**Phase 11: Layout Examples**

7. **Example 50: Advanced Tables**
   - Percentage column widths
   - Proportional columns (1*, 2*, etc.)
   - Auto-sized columns
   - Mixed column width types

8. **Example 51: Running Headers**
   - All 4 marker retrieve positions
   - Chapter/section markers
   - First-including-carryover demo

9. **Example 52: Advanced Floats**
   - Content-based float sizing
   - Auto-width floats
   - Complex float layouts

**Phase 12: PDF Examples**

10. **Example 53: PDF/A Archival**
    - PDF/A-2b generation
    - XMP metadata
    - Long-term preservation demo

**Phase 13: XSL-FO Examples**

11. **Example 54: Table Captions**
    - Caption positioning (before/after/start/end)
    - Styled captions
    - Multi-page tables with captions

12. **Example 55: Advanced Indexing**
    - Index generation
    - Page ranges
    - Custom separators

13. **Example 56: Visibility and Clipping**
    - Hidden content
    - Clip rectangles
    - Overflow handling

**Total New Examples:** 13 examples (bringing total from 63 to 76)

### 4.2 Update Existing Examples

**Update Examples README:**
- Add descriptions for new examples
- Update feature matrix
- Add cross-references

**Update validate-pdfs.sh:**
- Include new examples in validation

---

## Phase 5: Documentation Writing (Week 5)

### 5.1 User Guides

**New Documentation Files:**

1. **docs/guides/fonts.md** - Comprehensive font guide
   - TrueType/OpenType embedding
   - Font subsetting
   - System font discovery
   - Font fallback chains
   - Kerning
   - OpenType features
   - CFF fonts (basic support)
   - Memory management
   - Performance tips

2. **docs/guides/images.md** - Image handling guide
   - Supported formats (JPEG, PNG, BMP, GIF, TIFF)
   - DPI detection and scaling
   - CMYK and ICC profiles
   - Alpha transparency
   - Image sizing
   - Performance considerations
   - Deferred features (interlaced, indexed transparency)

3. **docs/guides/tables.md** - Advanced table layouts
   - Column width types (fixed, percentage, proportional, auto)
   - Row and column spanning
   - Header/footer repetition
   - Page breaking behavior
   - Table captions
   - Retrieve-table-marker
   - Styling and borders

4. **docs/guides/typography.md** - Typography guide
   - Line breaking algorithms (Greedy vs Knuth-Plass)
   - Hyphenation (4 languages)
   - BiDi and RTL text (UAX#9)
   - Paired brackets
   - Emergency line breaking
   - Text alignment and justification
   - Configurable parameters

5. **docs/guides/pdf-generation.md** - PDF output guide
   - PDF 1.7 structure
   - Font embedding
   - Image embedding
   - Metadata
   - PDF/A compliance (PDF/A-1b, 2b, 3b)
   - Compression
   - Validation tools

6. **docs/guides/markers.md** - Marker and retrieval guide
   - Marker basics (fo:marker)
   - All 4 retrieve positions
   - Marker scoping
   - Carryover behavior
   - Table markers
   - Running headers/footers
   - Best practices

7. **docs/guides/migration-to-v1.md** - Migration guide
   - Breaking changes from pre-v1
   - New features summary
   - API changes
   - Deprecated features
   - Upgrade checklist

### 5.2 API Documentation

**XML Documentation Comments:**

Audit all public APIs and ensure comprehensive XML comments:

```csharp
/// <summary>
/// Configures the PDF/A compliance level for generated PDF documents.
/// </summary>
/// <value>
/// The PDF/A compliance level. Default is <see cref="PdfALevel.None"/> (no PDF/A compliance).
/// </value>
/// <remarks>
/// <para>
/// PDF/A is an ISO standard for long-term archival of electronic documents. When enabled,
/// Folly will generate PDFs that conform to the specified PDF/A level:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="PdfALevel.PdfA1b"/> - PDF/A-1 Level B (basic, based on PDF 1.4)</description></item>
/// <item><description><see cref="PdfALevel.PdfA2b"/> - PDF/A-2 Level B (based on PDF 1.7, recommended)</description></item>
/// <item><description><see cref="PdfALevel.PdfA3b"/> - PDF/A-3 Level B (allows file attachments)</description></item>
/// </list>
/// <para>
/// PDF/A compliance requires:
/// </para>
/// <list type="bullet">
/// <item><description>All fonts must be embedded (<see cref="EmbedFonts"/> must be true)</description></item>
/// <item><description>No encryption</description></item>
/// <item><description>XMP metadata must be present</description></item>
/// <item><description>OutputIntent with ICC profile</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var options = new PdfOptions
/// {
///     PdfACompliance = PdfALevel.PdfA2b,
///     EmbedFonts = true // Required for PDF/A
/// };
/// document.SavePdf("archival.pdf", options);
/// </code>
/// </example>
public PdfALevel PdfACompliance { get; set; } = PdfALevel.None;
```

**Priority Classes for Documentation:**
- `PdfOptions` - All properties
- `LayoutOptions` - All properties (esp. new Knuth-Plass params)
- `FoDocument` - Public methods
- `LayoutEngine` - Public methods
- Font-related classes (`FontResolver`, `FontSubsetter`)
- Image parsers (`JpegParser`, `PngParser`, etc.)

### 5.3 README Updates

**Update README.md:**

1. Update feature list with Phases 7-13 additions
2. Add new example count (76 examples)
3. Update test count (target: 595 tests)
4. Add "What's New in v1.0" section
5. Link to new documentation guides
6. Update performance metrics if changed
7. Add PDF/A badge
8. Add OpenType support badge

**Add Badges:**
```markdown
[![Build Status](https://github.com/folly/folly/workflows/CI/badge.svg)](https://github.com/folly/folly/actions)
[![NuGet](https://img.shields.io/nuget/v/Folly.Core.svg)](https://www.nuget.org/packages/Folly.Core/)
[![PDF/A Support](https://img.shields.io/badge/PDF%2FA-1b%2C%202b%2C%203b-blue)](docs/guides/pdf-generation.md)
[![OpenType](https://img.shields.io/badge/OpenType-Ligatures%2C%20Kerning-green)](docs/guides/fonts.md)
[![Tests](https://img.shields.io/badge/Tests-595%20passing-brightgreen)](tests/)
```

---

## Phase 6: Release Preparation (Week 6)

### 6.1 Version and Release Notes

**Version Number:** v1.0.0

**Semantic Versioning Justification:**
- First production-ready release
- Comprehensive feature set (~85% XSL-FO 1.1)
- Stable API
- Extensive testing (595 tests)
- Production-quality code

**Release Notes (RELEASE-NOTES-v1.0.md):**

```markdown
# Folly v1.0.0 Release Notes

**Release Date:** [DATE]

## Overview

Folly v1.0.0 is the first production-ready release of the XSL-FO 1.1 to PDF renderer for .NET 8. This release represents months of development and includes ~85% XSL-FO 1.1 specification compliance, 595 passing tests, and 76 comprehensive examples.

## Highlights

### Zero Dependencies
- Pure .NET 8 implementation
- No external runtime dependencies
- Self-contained font handling
- Native image parsing (JPEG, PNG, BMP, GIF, TIFF)

### Excellent Performance
- 200-page document in ~150ms (66x faster than target)
- ~22MB memory footprint for 200 pages
- Linear to sub-linear scaling
- Optimized font caching

### Professional Typography
- Knuth-Plass line breaking (TeX-quality)
- Hyphenation (English, German, French, Spanish)
- OpenType ligatures and kerning
- Full Unicode BiDi (UAX#9) for RTL languages
- Emergency line breaking for overflow

### Complete Image Support
- JPEG (including CMYK), PNG (with alpha), BMP, GIF, TIFF
- DPI detection and proper scaling
- ICC profile embedding
- Zero dependencies for image decoding

### Advanced Layout
- Multi-page tables with header/footer repetition
- Column width control (fixed, percentage, proportional, auto)
- Row and column spanning
- Table captions
- Content-based float sizing
- Advanced marker retrieval (4 positions)
- Footnotes with separators
- Multi-column layout

### PDF Generation
- PDF 1.7 output
- PDF/A-2b compliance for archival
- Font embedding and subsetting
- Metadata and bookmarks
- Compression (Flate)
- Validation (qpdf clean)

### World-Class SVG Support
- 5,500+ lines of production SVG code
- All basic shapes, paths (14 commands), transforms
- Text on curves (textPath), vertical text
- Gradients, patterns, clipping, markers
- CSS stylesheet support
- 26 comprehensive SVG examples

## What's New in v1.0

### Font System (Phase 8)
- ✅ OpenType GPOS/GSUB parsing (ligatures, kerning, positioning)
- ✅ CFF font foundation (basic table parsing)
- ✅ Accurate font metadata (revision, timestamps, macStyle)
- ✅ Kerning pair remapping in subset fonts
- ✅ Font performance optimizations (LRU cache, persistent cache, thread-safe)
- ✅ Configurable memory limits (MaxFontMemory) to prevent OOM with large fonts

### Image Support (Phase 9)
- ✅ DPI detection for all formats (JPEG, PNG, BMP, GIF, TIFF)
- ✅ CMYK JPEG support with DeviceCMYK color space
- ✅ ICC profile extraction and embedding (JPEG, PNG)
- ✅ Configurable default DPI

### Typography (Phase 10)
- ✅ BiDi paired bracket algorithm (UAX#9 BD16)
- ✅ Configurable Knuth-Plass parameters (7 new options)

### Layout Engine (Phase 11)
- ✅ Advanced marker retrieval (all 4 XSL-FO positions)
- ✅ Percentage column widths in tables
- ✅ Content-based float sizing
- ✅ force-page-count (even, odd, end-on-even, end-on-odd)

### PDF Generation (Phase 12)
- ✅ PDF/A-2b compliance
- ✅ XMP metadata generation
- ✅ sRGB ICC profile embedding
- ✅ Validation for PDF/A requirements

### XSL-FO Features (Phase 13)
- ✅ Table captions (fo:table-and-caption, fo:table-caption)
- ✅ Retrieve-table-marker for table-scoped markers
- ✅ Multi-property elements (fo:multi-switch, fo:multi-case, etc.)
- ✅ Index generation (fo:index-range-begin, fo:index-key-reference, etc.)
- ✅ Visibility, clip, and overflow properties

### Error Handling (Phase 7)
- ✅ ImageDecodingException with detailed diagnostics (no more silent failures)
- ✅ Configurable error behavior (ThrowException, UsePlaceholder, SkipImage)
- ✅ Font memory quota to prevent OutOfMemoryException

## Test Coverage

**595 Passing Tests:**
- 364 unit tests
- 20 XSL-FO conformance tests
- 101 font system tests
- 110 new feature tests (Phases 8-13)

**76 Working Examples:**
- 37 XSL-FO examples
- 26 SVG examples
- 13 new examples showcasing v1.0 features

## Breaking Changes

None - this is the first production release.

## Known Limitations

Deferred to future releases:
- Interlaced PNG/GIF images (Phase 9.1)
- Indexed PNG transparency (Phase 9.2)
- CJK line breaking (Phase 10.1)
- Additional hyphenation languages beyond 4 (Phase 10.4)
- PDF 2.0 features (Phase 12.2)
- Digital signatures (Phase 12.3)
- Streaming PDF generation (Phase 12.4)
- Performance optimizations (Phase 14)

See [docs/guides/limitations.md](docs/guides/limitations.md) for full details.

## Documentation

New documentation guides:
- [Font Handling Guide](docs/guides/fonts.md)
- [Image Handling Guide](docs/guides/images.md)
- [Table Layouts Guide](docs/guides/tables.md)
- [Typography Guide](docs/guides/typography.md)
- [PDF Generation Guide](docs/guides/pdf-generation.md)
- [Markers and Retrieval Guide](docs/guides/markers.md)

## Installation

```bash
dotnet add package Folly.Core
dotnet add package Folly.Pdf
dotnet add package Folly.Fluent  # Optional
```

## Requirements

- .NET 8.0 or later
- No other dependencies

## Acknowledgments

Folly implements:
- XSL-FO 1.1 W3C Recommendation
- Unicode BiDi Algorithm (UAX#9)
- Knuth-Plass line breaking algorithm
- Liang's hyphenation algorithm
- OpenType specification (GPOS/GSUB)
- PDF 1.7 and PDF/A-2 ISO standards

## Contributors

[List contributors]

## License

MIT License - see LICENSE file for details.
```

### 6.2 NuGet Package Preparation

**Package Metadata Updates:**

Update `Folly.Core.csproj`, `Folly.Pdf.csproj`, `Folly.Fluent.csproj`:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <PackageVersion>1.0.0</PackageVersion>
  <PackageReleaseNotes>
    First production release of Folly XSL-FO renderer.
    - 85% XSL-FO 1.1 compliance
    - 595 passing tests
    - 76 comprehensive examples
    - OpenType support (ligatures, kerning)
    - PDF/A-2b archival compliance
    - CMYK and ICC profile support
    - Advanced table layouts
    - Full Unicode BiDi (RTL languages)
    - Zero runtime dependencies
    - Excellent performance (150ms for 200 pages)

    See https://github.com/folly/folly/releases/tag/v1.0.0 for full release notes.
  </PackageReleaseNotes>
  <PackageTags>xsl-fo;pdf;pdf-generation;xslfo;formatting-objects;layout;typography;opentype;pdf-a;cmyk;bidi</PackageTags>
  <Description>
    XSL-FO 1.1 to PDF renderer for .NET 8. Pure .NET implementation with zero runtime dependencies, excellent performance, and professional typography. Supports OpenType features, PDF/A archival, CMYK images, BiDi text, and advanced table layouts.
  </Description>
</PropertyGroup>
```

### 6.3 GitHub Release

**Release Checklist:**

- [ ] All 595 tests passing
- [ ] All 76 examples generating clean PDFs
- [ ] qpdf validation passing for all examples
- [ ] Build passing on all platforms (Windows, Linux, macOS)
- [ ] Documentation complete and reviewed
- [ ] README.md updated
- [ ] PLAN.md updated with v1.0 completion status
- [ ] RELEASE-NOTES-v1.0.md created
- [ ] CHANGELOG.md updated
- [ ] Version numbers updated in all .csproj files
- [ ] NuGet packages built and tested locally
- [ ] Git tag created: `v1.0.0`
- [ ] GitHub release created with:
  - Release notes
  - Pre-built example PDFs (zip)
  - NuGet package links
  - Documentation links

---

## Success Metrics

### Test Coverage Targets

**Baseline:** 485 tests
**Target:** 595 tests
**Delta:** +110 tests

**Coverage by Phase:**
- Phase 7: ✅ Complete (tests already done)
- Phase 8: +18 tests (OpenType, CFF basics)
- Phase 9: +18 tests (DPI, CMYK)
- Phase 10: +14 tests (BiDi, Knuth-Plass)
- Phase 11: +24 tests (markers, columns, floats, keep/break)
- Phase 12: +12 tests (PDF/A)
- Phase 13: +24 tests (captions, table markers, multi-*, index, clip/overflow)

**Total:** 110 new tests

### Documentation Targets

**New Documentation:**
- 7 user guides (~50 pages total)
- API XML comments (all public APIs)
- 13 new examples
- Updated README
- Migration guide
- Release notes

**Metrics:**
- All public APIs documented with XML comments
- Every feature has at least one example
- User guides cover all major features
- Migration path clear for future versions

### Quality Targets

**Build:**
- ✅ Zero build warnings
- ✅ Zero build errors
- ✅ All platforms supported (Windows, Linux, macOS)

**Tests:**
- ✅ 100% test pass rate (595/595)
- ✅ qpdf validation clean for all examples

**Performance:**
- ✅ No regressions from current baseline
- ✅ 200-page document < 500ms
- ✅ Memory < 100MB for 200 pages

---

## Timeline Summary

**Week 1: Testing Audit & Gap Analysis**
- Inventory deferred tests
- Prioritize test coverage
- Set up test infrastructure

**Weeks 2-3: Critical Test Implementation**
- Image tests (DPI, CMYK)
- Table tests (column widths)
- PDF/A tests
- Visibility/clip/overflow tests

**Weeks 3-4: Important Test Implementation**
- OpenType tests (ligatures)
- BiDi tests
- Typography tests
- Layout engine tests

**Week 4: Example Showcase Updates**
- Create 13 new examples
- Update examples README
- Update validation scripts

**Week 5: Documentation Writing**
- Write 7 user guides
- Add XML API documentation
- Update README and PLAN.md

**Week 6: Release Preparation**
- Finalize version number
- Write release notes
- Prepare NuGet packages
- Create GitHub release

**Total:** 6 weeks to v1.0 release

---

## Risk Mitigation

### Risk: Test Creation Takes Longer Than Expected

**Mitigation:**
- Prioritize critical tests (Priority 1) first
- Can defer Priority 3 tests to v1.1 if needed
- Focus on breadth over depth initially

### Risk: Documentation Scope Too Large

**Mitigation:**
- Start with skeleton docs, fill in details iteratively
- Reuse content from PLAN.md where applicable
- Community can contribute via PRs after release

### Risk: Example Creation Reveals Bugs

**Mitigation:**
- This is actually good! Better to find bugs now
- Allocate buffer time in timeline for bug fixes
- May need to extend Week 4 if major issues found

### Risk: Performance Regression Discovered

**Mitigation:**
- Run benchmarks early (Week 1)
- If regression found, may need to investigate/fix
- Could delay release if severe

---

## Post-Release (v1.1 Planning)

### Deferred Features for v1.1

**From Phase 9:**
- Interlaced PNG/GIF support (Medium complexity)
- Indexed PNG transparency (Medium complexity)

**From Phase 10:**
- Additional hyphenation languages (10+ languages)

**From Phase 12:**
- PDF 2.0 support (if demand exists)
- Digital signatures (if demand exists)
- Streaming PDF (for 1000+ page documents)

**Phase 14:**
- Performance optimizations
- Memory pooling
- Parallel page layout

### v1.1 Timeline

Target: 3-4 months after v1.0 release

---

## Conclusion

This plan focuses on **quality over quantity** - ensuring all completed features (Phases 7-13) are thoroughly tested, well-documented, and production-ready. By deferring new features to v1.1 and beyond, we can ship a solid, reliable v1.0 that users can trust.

**Key Principles:**
1. **Comprehensive Testing** - 110 new tests covering all v1.0 features
2. **Excellent Documentation** - 7 user guides + full API docs
3. **Working Examples** - 13 new examples (76 total)
4. **Quality First** - No regressions, clean validation
5. **Clear Communication** - Detailed release notes, migration guide

**Target:** Production-ready v1.0 in 6 weeks
