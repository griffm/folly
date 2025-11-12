# Folly Development Roadmap: Path to Best-in-Class Layout Engine

## Executive Summary

This roadmap outlines Folly's evolution from a solid XSL-FO foundation (~70% spec compliance) to a best-in-class layout engine with professional-grade typography, advanced pagination, and comprehensive font support.

**Current Status:** Excellent performance (66x faster than target), solid foundation, ~70% XSL-FO 1.1 compliance
**Target:** Best-in-class layout engine with ~95% spec compliance, professional typography, zero runtime dependencies
**Timeline:** 6 phases over 12-18 months

## Philosophy & Constraints

- **Zero Dependencies**: No runtime dependencies beyond System.* (dev/test dependencies allowed)
- **Performance First**: Maintain excellent performance (current: 150ms for 200 pages)
- **Incremental Enhancement**: Each phase delivers production value
- **Backward Compatible**: Existing functionality never breaks
- **Well-Tested**: Every feature has comprehensive test coverage

## Architecture Assessment

### Current Strengths ✅

**Layout Engine (1,679 lines, 20+ methods)**
- Clean, modular design with clear separation of concerns
- Single-pass greedy algorithm (fast, predictable)
- Multi-column layout, markers, footnotes, floats already working
- Excellent performance foundation
- Well-structured for enhancement

**Foundation Already in Place**
- ✅ Multi-page pagination with conditional page masters
- ✅ Block and inline formatting
- ✅ Tables with column spanning
- ✅ Images (JPEG/PNG)
- ✅ Links and bookmarks
- ✅ Font subsetting and compression
- ✅ Static content (headers/footers)
- ✅ Property inheritance system

### Critical Gaps to Address ⚠️

1. **Tables don't break across pages** - Blocks ~40% of real-world documents
2. **Only 14 standard fonts** - No TrueType/OpenType support
3. **No text justification** - Only left/center/right alignment
4. **No hyphenation** - Poor line breaking in narrow columns
5. **Greedy line breaking** - Suboptimal typography
6. **No widow/orphan control** - Unprofessional page breaks
7. **Limited keep constraints** - No keep-with-next/previous
8. **Simplified BiDi** - Not full UAX#9 algorithm
9. **No row spanning** - Complex table layouts impossible
10. **No absolute positioning** - Limited layout flexibility

### Layout Manager Capability Assessment

**High Confidence (Can Implement with Moderate Effort):**
- ✅ Table page breaking - Row-by-row iteration straightforward
- ✅ Text justification - Inter-word spacing adjustment
- ✅ Keep-with-next/previous - Track relationships, minor refactor
- ✅ List page breaking - Similar pattern to tables
- ✅ Widow/orphan control - Lookahead in line breaking
- ✅ Row spanning - Cell grid tracking
- ✅ Region-start/end - Similar to before/after (already parsed)

**Medium Confidence (Significant Effort, Zero-Deps Solutions):**
- ✅ TrueType/OpenType fonts - Implement parser ourselves (substantial but doable)
- ✅ Hyphenation - Implement Liang algorithm + embed patterns
- ✅ Knuth-Plass line breaking - Dynamic programming (complex but well-documented)
- ✅ Absolute positioning - Block-container enhancement

**Lower Confidence (Very High Effort, Consider Scope):**
- ⚠️ Full BiDi UAX#9 - Complex algorithm with many edge cases
- ⚠️ SVG support - Requires vector graphics engine
- ⚠️ Complex script shaping - Arabic/Indic shaping (HarfBuzz-like complexity)

## Phased Roadmap

---

## Phase 1: Critical Pagination & Typography (8-10 weeks)

**Goal:** Fix the most impactful limitations that block real-world document generation

**Completion Criteria:** Multi-page tables, justified text, professional page breaks

### 1.1 Table Page Breaking ⭐ CRITICAL ✅ COMPLETED

**Impact:** Enables multi-page data tables (40% of documents currently blocked)

**Implementation:**
Row-by-row table layout with page breaking has been successfully implemented in `LayoutEngine.LayoutTableWithPageBreaking()`. The implementation supports:
- Automatic page breaks between table rows
- Header repetition on new pages (default behavior)
- `table-omit-header-at-break` property to control header repetition
- `table-omit-footer-at-break` property for footer handling
- `keep-together` constraint on table rows (basic support)

**Deliverables:**
- [x] Refactor `LayoutTable()` to support row-by-row iteration
- [x] Add page break logic within table layout
- [x] Implement table header repetition on page breaks
- [x] Support `table-omit-header-at-break` property
- [x] Handle keep-together on table rows (basic implementation)
- [x] Add tests for tables spanning multiple pages
- [x] Update examples with large table demo (Example 7.5: 100-row table spanning 4 pages)

**Results:**
- ✅ Tables now break cleanly across pages
- ✅ Headers automatically repeat on each page
- ✅ 4 new comprehensive tests added covering multi-page tables
- ✅ Working demonstration with 100-row table in examples
- ✅ All existing tests pass without regression

**Complexity:** Medium (Completed)

### 1.2 Text Justification ✅ COMPLETED

**Impact:** Professional documents require justified text

**Implementation:**
```csharp
private LineArea CreateJustifiedLine(
    string text,
    double availableWidth,
    Fonts.FontMetrics fontMetrics)
{
    var words = text.Split(' ');
    var textWidth = MeasureText(text, fontMetrics);
    var extraSpace = availableWidth - textWidth;
    var gaps = words.Length - 1;

    if (gaps > 0)
    {
        var spaceAdjustment = extraSpace / gaps;
        // Apply inter-word spacing adjustment
    }
}
```

**Deliverables:**
- [x] Implement inter-word spacing adjustment
- [x] Support `text-align="justify"`
- [x] Support `text-align-last` property
- [x] Handle edge cases (single word, last line)
- [x] Add tests for justified paragraphs
- [x] Update examples

**Complexity:** Low (1-2 weeks)

### 1.3 Keep-With-Next/Previous ✅ COMPLETED

**Impact:** Keeps headings with content, figures with captions

**Implementation:**
The implementation tracks previous blocks during layout and checks keep constraints before page/column breaks. When a block doesn't fit and has a keep relationship with the previous block (either via `keep-with-next` on the previous block or `keep-with-previous` on the current block), both blocks are moved together to the next page/column.

```csharp
// Check for keep-with-next/previous constraints
var mustKeepWithPrevious = (previousBlock != null && GetKeepStrength(previousBlock.KeepWithNext) > 0) ||
                          GetKeepStrength(foBlock.KeepWithPrevious) > 0;

// If block doesn't fit and must keep with previous, move both blocks together
if (!blockFitsInColumn && mustKeepWithPrevious && previousBlockArea != null && currentY > bodyMarginTop)
{
    // Remove previous block from current page
    currentPage.RemoveArea(previousBlockArea);
    // Move to next page/column
    // Re-layout both blocks together
}
```

**Deliverables:**
- [x] Track keep relationships between blocks
- [x] Implement keep-with-next logic
- [x] Implement keep-with-previous logic
- [x] Support integer keep strength values (1-999)
- [x] Add tests for heading + paragraph scenarios (4 comprehensive tests added)
- [x] Update examples (Example 10 enhanced with keep-with-next/previous demonstrations)

**Results:**
- ✅ Headings stay with their following paragraphs (no orphaned headings)
- ✅ Figure titles stay with their captions
- ✅ Integer keep strength values (1-999) work correctly
- ✅ break-before/after correctly take precedence over keep constraints
- ✅ 4 new tests added: KeepWithNext_KeepsHeadingWithParagraph, KeepWithPrevious_KeepsBlocksWithPrevious, KeepWithNext_IntegerStrength_Works, KeepWithNext_WithBreakBefore_BreakTakesPrecedence
- ✅ Example 10 updated with keep-with-next/previous demonstrations
- ✅ All existing tests pass without regression

**Complexity:** Medium (Completed)

### 1.4 Widow/Orphan Control

**Impact:** Professional typography, no lonely lines

**Implementation:**
```csharp
private bool WouldCreateWidow(
    LineArea[] lines,
    int breakAtLine,
    int widowThreshold)
{
    var linesAfterBreak = lines.Length - breakAtLine;
    return linesAfterBreak < widowThreshold;
}
```

**Deliverables:**
- [x] Implement widow detection (last line alone on new page)
- [x] Implement orphan detection (first line alone on old page)
- [x] Support `widows` and `orphans` properties
- [x] Add lookahead to avoid widow/orphan situations
- [x] Add tests (4 comprehensive tests added)
- [x] Update examples

**Results:**
- ✅ Widows constraint prevents too few lines at top of new page (minimum 2 lines by default)
- ✅ Orphans constraint prevents too few lines at bottom of old page (minimum 2 lines by default)
- ✅ Both constraints work together to find optimal split points
- ✅ keep-together constraint correctly overrides widow/orphan control
- ✅ Block splitting preserves formatting (borders, padding, margins split appropriately)
- ✅ 4 new tests added: Widows_PreventsLonelyLinesAtTopOfPage, Orphans_PreventsLonelyLinesAtBottomOfPage, WidowsAndOrphans_RespectsBothConstraints, KeepTogether_OverridesWidowOrphanControl
- ✅ All existing tests pass without regression

**Complexity:** Medium (Completed)

**Phase 1 Success Metrics:**
- ✅ Tables of 100+ rows render correctly across pages (Completed in 1.1)
- ✅ Justified text looks professional with even spacing (Completed in 1.2)
- ✅ No headings orphaned at bottom of page (Completed in 1.3)
- ⏳ No widow/orphan lines in paragraphs (Pending - Phase 1.4)
- ⏳ 95% of documents that currently fail now succeed (Pending - Full Phase 1 completion)
- ✅ Performance: Still under 300ms for 200 pages (Maintained)
- ✅ 15+ new passing tests (Achieved: 4 new tests from 1.1 + 4 new tests from 1.3 = 8+ tests so far)

---

## Phase 2: Professional Typography (10-12 weeks)

**Goal:** Implement professional-grade typography and line breaking

**Completion Criteria:** Hyphenation working, Knuth-Plass optional, better line breaking

### 2.1 Hyphenation Engine (Zero Dependencies)

**Impact:** Better line breaking, professional appearance

**Zero-Deps Strategy:** Implement Frank Liang's TeX hyphenation algorithm ourselves

**Implementation:**
```csharp
// Pure .NET implementation of Liang's algorithm
public class HyphenationEngine
{
    private Dictionary<string, int[]> _patterns;

    // Load patterns from embedded resources
    public void LoadPatterns(string language)
    {
        // Patterns embedded as resources (no external deps)
        var patterns = LoadEmbeddedResource($"Patterns.{language}.txt");
        ParsePatterns(patterns);
    }

    public List<int> FindHyphenationPoints(string word)
    {
        // Apply Liang's algorithm
        // Returns valid hyphenation positions
    }
}
```

**Pattern Files (Embedded Resources):**
- English (en_US): ~5,000 patterns, 350KB
- German (de_DE): ~12,000 patterns, 800KB
- French (fr_FR): ~8,000 patterns, 600KB
- Spanish (es_ES): ~4,000 patterns, 300KB

**Deliverables:**
- [ ] Implement Liang's TeX hyphenation algorithm (pure .NET)
- [ ] Embed hyphenation pattern files as resources
- [ ] Support `hyphenate` property
- [ ] Support `hyphenation-character` property
- [ ] Support language-specific patterns (en, de, fr, es)
- [ ] Add hyphenation to `BreakLines()` method
- [ ] Configurable min word length for hyphenation
- [ ] Add tests with narrow columns requiring hyphenation
- [ ] Update examples with multi-column documents

**Complexity:** High (4-5 weeks)

**References:**
- Frank Liang's thesis: "Word Hy-phen-a-tion by Com-put-er" (1983)
- TeX hyphenation patterns (public domain)
- Pure .NET implementation, no native dependencies

### 2.2 Emergency Line Breaking

**Impact:** Handles overflow gracefully

**Deliverables:**
- [ ] Character-level breaking for overflow words
- [ ] Support `wrap-option="no-wrap"`
- [ ] Support `wrap-option="wrap"`
- [ ] Ellipsis for truncated text (optional)
- [ ] Add tests with very narrow columns
- [ ] Update examples

**Complexity:** Low (1-2 weeks)

### 2.3 Knuth-Plass Line Breaking (Optional Quality Mode)

**Impact:** Optimal paragraph layout (like TeX)

**Implementation:**
```csharp
public enum LineBreakingAlgorithm
{
    Greedy,   // Fast, single-pass (current - default)
    Optimal   // Knuth-Plass, slower but better quality
}

public class LayoutOptions
{
    // Keep greedy as default for performance
    public LineBreakingAlgorithm LineBreaking { get; set; }
        = LineBreakingAlgorithm.Greedy;
}

private List<int> FindOptimalBreakpoints(
    string text,
    double availableWidth,
    Fonts.FontMetrics fontMetrics)
{
    // Dynamic programming implementation
    // Minimize "badness" across entire paragraph
    // O(n²) complexity vs O(n) for greedy
}
```

**Deliverables:**
- [ ] Implement Knuth-Plass algorithm (pure .NET)
- [ ] Make it opt-in via `LayoutOptions.LineBreaking`
- [ ] Calculate badness/penalty for each break
- [ ] Minimize total badness via dynamic programming
- [ ] Keep greedy as default (performance)
- [ ] Add benchmarks comparing both algorithms
- [ ] Add tests for optimal line breaking
- [ ] Update documentation

**Complexity:** Very High (4-5 weeks)

**References:**
- Knuth & Plass: "Breaking Paragraphs into Lines" (1981)
- TeX implementation reference
- Pure .NET implementation

### 2.4 List Page Breaking

**Impact:** Long lists can span pages

**Implementation:**
```csharp
// Similar pattern to table page breaking
private void LayoutListItemsWithPageBreaking(...)
{
    foreach (var item in list.Items)
    {
        if (currentY + itemHeight > pageBottom)
        {
            // Create new page
            // Continue list on new page
        }
        RenderListItem(item);
    }
}
```

**Deliverables:**
- [ ] Refactor list layout for item-by-item breaking
- [ ] Support keep-together on list items
- [ ] Add tests for long lists
- [ ] Update examples

**Complexity:** Medium (2-3 weeks)

**Phase 2 Success Metrics:**
- ✅ Text in narrow columns (3-column layout) looks professional
- ✅ Hyphenation reduces ragged edges by 60%+
- ✅ Knuth-Plass (opt-in) produces TeX-quality output
- ✅ Long lists (100+ items) span multiple pages correctly
- ✅ Performance: Greedy still <300ms, Knuth-Plass <2s for 200 pages
- ✅ 20+ new passing tests

---

## Phase 3: TrueType/OpenType Font Support (12-14 weeks)

**Goal:** Support custom fonts while maintaining zero dependencies

**Completion Criteria:** Load and embed TTF/OTF fonts, basic kerning

### 3.1 TTF/OTF Parser (Zero Dependencies Implementation)

**Zero-Deps Strategy:** Implement our own TrueType/OpenType parser using published specs

**Why No External Library:**
- Most font libraries (FreeType, HarfBuzz) are native C/C++ with P/Invoke
- Pure .NET libraries exist but add dependencies
- Font parsing is well-documented in published specs
- We control the implementation quality and features

**Implementation:**
```csharp
namespace Folly.Fonts
{
    /// <summary>
    /// Pure .NET TrueType/OpenType font parser.
    /// Zero runtime dependencies.
    /// </summary>
    public class TrueTypeFontParser
    {
        public FontData ParseFont(Stream fontStream)
        {
            // Parse TTF/OTF according to spec
            // https://docs.microsoft.com/en-us/typography/opentype/spec/

            // 1. Read font header (offset table)
            // 2. Parse required tables:
            //    - 'head' - Font header
            //    - 'hhea' - Horizontal header
            //    - 'hmtx' - Horizontal metrics
            //    - 'maxp' - Maximum profile
            //    - 'name' - Font name
            //    - 'cmap' - Character to glyph mapping
            //    - 'loca' - Glyph location
            //    - 'glyf' - Glyph data (TrueType)
            //    - 'CFF ' - Compact Font Format (OpenType)
            //    - 'post' - PostScript info
            //    - 'OS/2' - OS/2 metrics

            return new FontData
            {
                FamilyName = ...,
                Glyphs = ...,
                Metrics = ...,
                CmapTable = ...
            };
        }
    }

    public class FontData
    {
        public string FamilyName { get; set; }
        public Dictionary<char, GlyphInfo> CharToGlyph { get; set; }
        public Dictionary<int, GlyphMetrics> GlyphMetrics { get; set; }
        // Add kerning pairs later
    }
}
```

**Tables to Parse (Phase 3.1):**

**Required Tables:**
- `head` - Font header (units per em, bounding box)
- `hhea` - Horizontal header (ascent, descent, line gap)
- `hmtx` - Horizontal metrics (character widths)
- `maxp` - Maximum profile (glyph count)
- `name` - Font naming (family, style)
- `cmap` - Character-to-glyph mapping (Unicode support)
- `loca` - Glyph data location index
- `glyf` - Glyph outlines (TrueType format)
- `CFF ` - Glyph outlines (OpenType/CFF format)
- `post` - PostScript information

**Deliverables:**
- [ ] Implement TrueType table parser (pure .NET)
- [ ] Parse required tables: head, hhea, hmtx, maxp, name, cmap, loca, glyf, CFF
- [ ] Build character-to-glyph mapping
- [ ] Extract glyph metrics (width, height, bearings)
- [ ] Support Unicode cmap (format 4, format 12)
- [ ] Add font caching mechanism
- [ ] Add tests with real TTF/OTF files
- [ ] Support Windows, macOS, Linux font directories

**Complexity:** Very High (6-7 weeks)

**References:**
- OpenType Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/
- TrueType Reference: https://developer.apple.com/fonts/TrueType-Reference-Manual/
- Pure .NET implementation

### 3.2 Font Embedding & Subsetting

**Implementation:**
```csharp
public class TrueTypeFontEmbedder
{
    public byte[] CreateSubset(
        FontData font,
        HashSet<char> usedCharacters)
    {
        // Create minimal font with only used glyphs
        // 1. Identify required glyphs
        // 2. Extract glyph outlines
        // 3. Rebuild font tables with subset
        // 4. Generate valid TTF/OTF output
    }

    public void EmbedInPdf(
        PdfWriter writer,
        FontData font,
        byte[] subsetBytes)
    {
        // Embed as TrueType or CIDFont (for CJK)
        // Generate ToUnicode CMap
        // Create font descriptor
    }
}
```

**Deliverables:**
- [ ] Implement font subsetting (extract used glyphs)
- [ ] Rebuild font tables for subset
- [ ] Embed TrueType fonts in PDF
- [ ] Generate ToUnicode CMap for text extraction
- [ ] Support CIDFont for large character sets (CJK)
- [ ] Add tests for embedding and subsetting
- [ ] Update examples with custom fonts

**Complexity:** High (4-5 weeks)

### 3.3 Font Fallback & Family Stacks

**Implementation:**
```csharp
public class FontResolver
{
    private Dictionary<string, FontData> _loadedFonts;

    public FontData ResolveFontFamily(string fontFamilyStack)
    {
        // Parse: "Roboto, Arial, Helvetica, sans-serif"
        var families = fontFamilyStack.Split(',');

        foreach (var family in families)
        {
            if (_loadedFonts.TryGetValue(family.Trim(), out var font))
                return font;
        }

        // Fall back to generic family
        return ResolveGenericFamily("sans-serif");
    }
}
```

**Deliverables:**
- [ ] Support font family stacks
- [ ] Font fallback mechanism
- [ ] Generic family mapping (serif, sans-serif, monospace)
- [ ] System font discovery (Windows, macOS, Linux)
- [ ] Add tests for font fallback
- [ ] Update examples

**Complexity:** Medium (2-3 weeks)

### 3.4 Basic Kerning

**Implementation:**
```csharp
// Parse 'kern' table from font
private Dictionary<(int, int), int> LoadKerningPairs(FontData font)
{
    // Read kern table from TTF/OTF
    // Build dictionary of glyph pair adjustments
}

private double ApplyKerning(char prev, char curr, FontData font)
{
    var prevGlyph = font.CharToGlyph[prev];
    var currGlyph = font.CharToGlyph[curr];

    if (font.KerningPairs.TryGetValue((prevGlyph, currGlyph), out var adjustment))
        return adjustment;

    return 0;
}
```

**Deliverables:**
- [ ] Parse `kern` table from fonts
- [ ] Apply kerning during text measurement
- [ ] Apply kerning during PDF rendering
- [ ] Add tests for kerned text
- [ ] Update examples

**Complexity:** Medium (2-3 weeks)

**Phase 3 Success Metrics:**
- ✅ Can load any TTF/OTF font file
- ✅ Custom fonts embed correctly in PDF
- ✅ Font subsetting reduces file size by 90%+
- ✅ Font fallback works (try list, use first available)
- ✅ Basic kerning improves appearance
- ✅ No external dependencies added
- ✅ 25+ new passing tests
- ✅ Examples showcase 10+ custom fonts

**Note:** All font parsing is pure .NET, zero dependencies

---

## Phase 4: Advanced Table Features (6-8 weeks)

**Goal:** Complete table implementation with row spanning and advanced features

**Completion Criteria:** Row spanning, proportional widths, content-based sizing

### 4.1 Row Spanning Implementation

**Impact:** Complex table layouts (merged cells vertically)

**Implementation:**
```csharp
private class TableCellGrid
{
    private Dictionary<(int row, int col), TableCellPlacement> _occupied;

    public int GetNextAvailableColumn(int row)
    {
        int col = 0;
        while (_occupied.ContainsKey((row, col)))
            col++;
        return col;
    }

    public void ReserveCells(int row, int col, int rowSpan, int colSpan)
    {
        for (int r = 0; r < rowSpan; r++)
            for (int c = 0; c < colSpan; c++)
                _occupied[(row + r, col + c)] = ...;
    }
}

private TableArea? LayoutTableWithSpanning(Dom.FoTable table, ...)
{
    var grid = new TableCellGrid();

    foreach (var row in table.Body.Rows)
    {
        int colIndex = 0;
        foreach (var cell in row.Cells)
        {
            colIndex = grid.GetNextAvailableColumn(rowIndex);
            grid.ReserveCells(rowIndex, colIndex, cell.RowSpan, cell.ColSpan);

            // Calculate cell height to span multiple rows
            var cellHeight = rowHeights[rowIndex];
            for (int i = 1; i < cell.RowSpan; i++)
                cellHeight += rowHeights[rowIndex + i];

            LayoutCell(cell, colIndex, cellHeight);
            colIndex += cell.ColSpan;
        }
        rowIndex++;
    }
}
```

**Deliverables:**
- [ ] Implement cell grid tracking
- [ ] Support `number-rows-spanned` property
- [ ] Calculate merged cell dimensions correctly
- [ ] Handle row spanning with page breaks
- [ ] Add tests for complex row/column spanning
- [ ] Update examples with merged cells

**Complexity:** Medium-High (3-4 weeks)

### 4.2 Proportional Column Widths

**Impact:** Better control over column sizing

**Implementation:**
```csharp
private List<double> CalculateProportionalWidths(
    Dom.FoTable table,
    double availableWidth)
{
    var columns = table.Columns;
    var fixedWidth = 0.0;
    var proportionalSum = 0.0;

    // First pass: sum fixed widths and proportional values
    foreach (var col in columns)
    {
        if (col.ColumnWidth.EndsWith("pt"))
            fixedWidth += ParseLength(col.ColumnWidth);
        else if (col.ColumnWidth.StartsWith("proportional-column-width("))
            proportionalSum += ParseProportional(col.ColumnWidth);
    }

    // Second pass: distribute remaining width proportionally
    var remainingWidth = availableWidth - fixedWidth;
    var widths = new List<double>();
    foreach (var col in columns)
    {
        if (col.ColumnWidth.StartsWith("proportional-column-width("))
        {
            var proportion = ParseProportional(col.ColumnWidth);
            widths.Add(remainingWidth * (proportion / proportionalSum));
        }
        else
        {
            widths.Add(ParseLength(col.ColumnWidth));
        }
    }

    return widths;
}
```

**Deliverables:**
- [ ] Support `proportional-column-width()` function
- [ ] Mix fixed and proportional widths
- [ ] Add tests for proportional layouts
- [ ] Update examples

**Complexity:** Medium (2-3 weeks)

### 4.3 Content-Based Column Sizing

**Impact:** Auto columns sized to content

**Implementation:**
```csharp
private double CalculateContentBasedWidth(Dom.FoTableColumn column)
{
    var maxWidth = 0.0;

    // Measure all cells in this column
    foreach (var row in table.AllRows)
    {
        var cell = row.GetCellInColumn(columnIndex);
        var cellMinWidth = MeasureCellMinimumWidth(cell);
        var cellMaxWidth = MeasureCellMaximumWidth(cell);
        maxWidth = Math.Max(maxWidth, cellMinWidth);
    }

    return maxWidth;
}
```

**Deliverables:**
- [ ] Measure cell content to determine minimum width
- [ ] Two-pass layout: measure, then render
- [ ] Balance content-based and auto columns
- [ ] Add tests for auto-sized columns
- [ ] Update examples

**Complexity:** High (3-4 weeks)

### 4.4 Table Footer Repetition

**Implementation:**
```csharp
private void RenderTableWithFooter(
    Dom.FoTable table,
    bool repeatFooterOnBreak)
{
    // Render header
    // Render body rows
    // When page break occurs:
    if (repeatFooterOnBreak)
    {
        RenderTableFooter();  // On current page
        // New page
        RenderTableHeader();  // On new page
    }
    else
    {
        // Footer only at end
    }
}
```

**Deliverables:**
- [ ] Support `table-omit-footer-at-break` property
- [ ] Render footer at page breaks (if enabled)
- [ ] Add tests
- [ ] Update examples

**Complexity:** Low (1-2 weeks)

**Phase 4 Success Metrics:**
- ✅ Row spanning works correctly (including with page breaks)
- ✅ Proportional column widths distribute space correctly
- ✅ Content-based columns sized optimally
- ✅ Complex tables (10x10 with spanning) render correctly
- ✅ 20+ new passing tests
- ✅ Examples showcase advanced table features

---

## Phase 5: Advanced Layout & Positioning (8-10 weeks)

**Goal:** Absolute positioning, region-start/end, advanced layout features

**Completion Criteria:** Absolute positioning works, all regions render

### 5.1 Absolute Positioning

**Impact:** Enables letterheads, watermarks, complex forms

**Implementation:**
```csharp
public class AbsolutePositionedArea : Area
{
    public string Position { get; set; }  // "absolute" | "fixed"
    public double Top { get; set; }
    public double Left { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
    public int ZIndex { get; set; } = 0;
}

private BlockArea? LayoutAbsoluteBlockContainer(
    Dom.FoBlockContainer container,
    Dom.FoSimplePageMaster pageMaster)
{
    if (container.AbsolutePosition == "absolute")
    {
        var area = new AbsolutePositionedArea();

        // Calculate position relative to page
        area.X = ResolvePosition(container.Left, pageMaster.PageWidth);
        area.Y = ResolvePosition(container.Top, pageMaster.PageHeight);

        // Layout content within container
        LayoutBlockContent(container, area);

        return area;
    }

    return LayoutBlockContainerNormal(container);
}
```

**Deliverables:**
- [ ] Implement `fo:block-container` with `absolute-position="absolute"`
- [ ] Support `top`, `left`, `right`, `bottom` properties
- [ ] Support `z-index` for layering
- [ ] Render absolute elements after flow (correct z-order)
- [ ] Add tests for overlapping content
- [ ] Add examples: letterhead, watermark, form

**Complexity:** High (4-5 weeks)

### 5.2 Region Start/End (Left/Right Sidebars)

**Impact:** Enables margin notes, sidebars

**Implementation:**
```csharp
private void AddRegionStartContent(
    PageViewport page,
    Dom.FoSimplePageMaster pageMaster,
    Dom.FoPageSequence pageSequence)
{
    foreach (var staticContent in pageSequence.StaticContents)
    {
        if (staticContent.FlowName == "xsl-region-start"
            && pageMaster.RegionStart != null)
        {
            var extent = pageMaster.RegionStart.Extent;
            var x = pageMaster.MarginLeft;
            var y = pageMaster.MarginTop + regionBeforeExtent;
            var width = extent;
            var height = pageMaster.PageHeight - y - bodyMarginBottom;

            // Layout content in left sidebar region
            foreach (var block in staticContent.Blocks)
            {
                var blockArea = LayoutBlock(block, x, y, width);
                page.AddArea(blockArea);
                y += blockArea.Height;
            }
        }
    }
}
```

**Deliverables:**
- [ ] Implement `fo:region-start` layout (already parsed)
- [ ] Implement `fo:region-end` layout (already parsed)
- [ ] Support `extent` property for region width
- [ ] Layout static-content for xsl-region-start/end
- [ ] Add tests for sidebars
- [ ] Add examples with margin notes

**Complexity:** Medium (2-3 weeks)

### 5.3 Background Images

**Implementation:**
```csharp
public class BackgroundImageArea : Area
{
    public string ImagePath { get; set; }
    public string Repeat { get; set; }  // "repeat" | "no-repeat" | "repeat-x" | "repeat-y"
    public string Position { get; set; }  // "center" | "top" | "bottom" | etc.
}

private void ApplyBackgroundImage(
    BlockArea block,
    Dom.FoBlock foBlock)
{
    if (!string.IsNullOrEmpty(foBlock.BackgroundImage))
    {
        block.BackgroundImage = new BackgroundImageArea
        {
            ImagePath = foBlock.BackgroundImage,
            Repeat = foBlock.BackgroundRepeat ?? "repeat",
            Position = foBlock.BackgroundPosition ?? "0% 0%"
        };
    }
}
```

**Deliverables:**
- [ ] Support `background-image` property
- [ ] Support `background-repeat` property
- [ ] Support `background-position` property
- [ ] Render backgrounds in PDF (tiled or positioned)
- [ ] Add tests for background images
- [ ] Add examples with letterhead backgrounds

**Complexity:** Medium (2-3 weeks)

### 5.4 Reference Orientation (Rotation)

**Implementation:**
```csharp
private void ApplyReferenceOrientation(
    BlockArea area,
    int orientation)
{
    // orientation is 0, 90, 180, or 270 degrees
    // Apply transformation matrix in PDF
    area.ReferenceOrientation = orientation;
}
```

**Deliverables:**
- [ ] Support `reference-orientation` property
- [ ] Rotate block containers (0, 90, 180, 270 degrees)
- [ ] Adjust dimensions for rotated content
- [ ] Render rotated content in PDF
- [ ] Add tests for rotated blocks
- [ ] Add examples with rotated table headers

**Complexity:** Medium (2-3 weeks)

### 5.5 Display-Align (Vertical Alignment)

**Deliverables:**
- [ ] Support `display-align` property on block containers
- [ ] Implement vertical centering ("center")
- [ ] Implement bottom alignment ("after")
- [ ] Add tests
- [ ] Update examples

**Complexity:** Low (1-2 weeks)

**Phase 5 Success Metrics:**
- ✅ Can position blocks at exact (x,y) coordinates
- ✅ Z-index controls rendering order
- ✅ Region-start/end (sidebars) render correctly
- ✅ Background images tile/position correctly
- ✅ Content can be rotated 90/180/270 degrees
- ✅ Vertical alignment works in containers
- ✅ 25+ new passing tests
- ✅ Examples: letterhead, form, sidebar document

---

## Phase 6: Internationalization & Advanced Features (10-12 weeks)

**Goal:** Full BiDi support, additional image formats, polish

**Completion Criteria:** UAX#9 BiDi, SVG support, accessibility foundations

### 6.1 Full Unicode BiDi Algorithm (UAX#9)

**Impact:** Correct rendering of RTL languages (Arabic, Hebrew)

**Zero-Deps Strategy:** Implement UAX#9 ourselves (substantial but doable)

**Implementation:**
```csharp
namespace Folly.BiDi
{
    /// <summary>
    /// Pure .NET implementation of Unicode Bidirectional Algorithm (UAX#9).
    /// Zero runtime dependencies.
    /// </summary>
    public class UnicodeBidiAlgorithm
    {
        public string ReorderText(string text, Direction baseDirection)
        {
            // Phase 1: Resolve character types (L, R, AL, EN, ES, etc.)
            var types = ResolveCharacterTypes(text);

            // Phase 2: Resolve explicit embeddings and overrides
            var levels = ResolveExplicitLevels(types);

            // Phase 3: Resolve weak types
            ResolveWeakTypes(types, levels);

            // Phase 4: Resolve neutral types
            ResolveNeutralTypes(types, levels);

            // Phase 5: Resolve implicit levels
            ResolveImplicitLevels(types, levels);

            // Phase 6: Reorder text by levels
            return ReorderByLevels(text, levels);
        }

        private CharacterType[] ResolveCharacterTypes(string text)
        {
            // Classify each character as L, R, AL, EN, etc.
            // Use Unicode character database data
        }
    }

    public enum CharacterType
    {
        L,   // Left-to-Right
        R,   // Right-to-Left
        AL,  // Right-to-Left Arabic
        EN,  // European Number
        ES,  // European Separator
        ET,  // European Terminator
        AN,  // Arabic Number
        CS,  // Common Separator
        NSM, // Non-Spacing Mark
        BN,  // Boundary Neutral
        B,   // Paragraph Separator
        S,   // Segment Separator
        WS,  // Whitespace
        ON   // Other Neutral
    }
}
```

**Unicode Data (Embedded Resources):**
- Bidi character types: ~30KB embedded data
- Bidi mirroring pairs: ~5KB embedded data
- No external dependencies

**Deliverables:**
- [ ] Implement UAX#9 algorithm phases 1-6 (pure .NET)
- [ ] Embed Unicode BiDi character data as resources
- [ ] Replace simple character reversal with proper algorithm
- [ ] Support BiDi control characters (LRM, RLM, LRE, RLE, PDF, LRI, RLI, FSI, PDI)
- [ ] Support block-level direction property
- [ ] Support mixed LTR/RTL content
- [ ] Handle numbers in RTL text correctly
- [ ] Handle punctuation in RTL text correctly
- [ ] Add comprehensive BiDi tests (Arabic, Hebrew, mixed)
- [ ] Add examples with RTL documents

**Complexity:** Very High (5-6 weeks)

**References:**
- Unicode Standard Annex #9: https://www.unicode.org/reports/tr9/
- Pure .NET implementation, no native dependencies

### 6.2 Additional Image Formats

**Strategy:** Use System.Drawing.Common where available, or minimal parsers

**Deliverables:**
- [ ] Add GIF support (System.Drawing or custom parser)
- [ ] Add WebP support (System.Drawing.Common on .NET 8+)
- [ ] Add TIFF support (System.Drawing or minimal parser)
- [ ] Add BMP support (trivial format)
- [ ] EXIF orientation support (auto-rotate)
- [ ] DPI/resolution detection (proper sizing)
- [ ] Add tests for each format
- [ ] Update examples

**Complexity:** Medium (3-4 weeks)

**Note:** System.Drawing.Common is available on .NET 8+ with proper warnings

### 6.3 Basic SVG Support

**Impact:** Vector graphics, scalable logos

**Strategy:** Parse SVG, convert to PDF graphics operators

**Implementation:**
```csharp
public class SimpleSvgRenderer
{
    public void RenderSvgToPdf(
        XmlDocument svg,
        PdfContentStream stream,
        double x,
        double y,
        double width,
        double height)
    {
        // Parse SVG elements
        foreach (var element in svg.DocumentElement.ChildNodes)
        {
            switch (element.Name)
            {
                case "rect":
                    RenderRect(element, stream);
                    break;
                case "circle":
                    RenderCircle(element, stream);
                    break;
                case "path":
                    RenderPath(element, stream);
                    break;
                case "text":
                    RenderText(element, stream);
                    break;
            }
        }
    }
}
```

**Deliverables:**
- [ ] Parse basic SVG elements (rect, circle, ellipse, line, polyline, polygon, path)
- [ ] Convert SVG paths to PDF path operators
- [ ] Support basic transforms (translate, scale, rotate)
- [ ] Support fills and strokes
- [ ] Support text elements (basic)
- [ ] Add tests with simple SVG files
- [ ] Add examples with SVG logos
- [ ] Document limitations (no filters, gradients, etc.)

**Complexity:** Very High (4-5 weeks)

**Scope:** Basic SVG only - no gradients, filters, masks, patterns (future)

### 6.4 Tagged PDF Foundations (Accessibility)

**Impact:** Accessibility, screen reader support

**Implementation:**
```csharp
public class PdfStructureTree
{
    public void AddStructureElement(
        string type,  // "Document", "H1", "P", "Table", etc.
        int contentId)
    {
        // Build PDF structure tree
        // Tag content with structure types
    }
}
```

**Deliverables:**
- [ ] Create PDF structure tree
- [ ] Tag basic elements (paragraphs, headings)
- [ ] Tag tables with proper structure
- [ ] Add alt text for images
- [ ] Mark reading order
- [ ] Add language tags
- [ ] Test with screen readers
- [ ] Document accessibility features

**Complexity:** High (3-4 weeks)

**Scope:** Foundation only - full PDF/UA compliance is Phase 7+

**Phase 6 Success Metrics:**
- ✅ Arabic and Hebrew text render correctly
- ✅ Mixed LTR/RTL documents work perfectly
- ✅ WebP, TIFF, GIF images supported
- ✅ Simple SVG files (logos, icons) embed correctly
- ✅ Basic tagged PDF structure created
- ✅ 30+ new passing tests
- ✅ Examples: RTL document, mixed-direction, SVG-based

---

## Phase 7+: Future Enhancements (Post 1.0)

**Goal:** Advanced features for specialized workflows

### Future Considerations

**Performance Optimizations:**
- Multi-threaded layout (page-level parallelization)
- Streaming support (constant memory for huge documents)
- Incremental layout (interactive editors)

**Advanced PDF:**
- PDF/A compliance (archival)
- PDF/UA compliance (full accessibility)
- CMYK color space (professional printing)
- ICC color profiles
- Spot colors (Pantone)

**Advanced Graphics:**
- Gradients (linear, radial)
- Rounded corners
- Transparency/opacity
- Clipping paths
- Filters and effects

**Advanced Typography:**
- OpenType features (liga, smcp, onum, swsh, calt, ss01-ss20)
- Complex script shaping (Arabic contextual forms, Indic scripts)
- Variable fonts
- Advanced kerning (GPOS table)

**Advanced Layout:**
- Drop caps (initial-property-set)
- Multi-switch elements
- Index generation
- Change bars
- Better float text wrapping

**Developer Experience:**
- Visual diff tool
- CLI tool for FO→PDF
- Live preview server
- VS Code extension

---

## Implementation Strategy

### Per-Phase Workflow

**1. Planning (Week 1)**
- Review phase goals and deliverables
- Break down into 2-week sprints
- Identify dependencies and risks
- Set up feature branches

**2. Implementation (Weeks 2-N)**
- TDD: Write tests first
- Implement features incrementally
- Code review before merge
- Document as you go

**3. Testing (Throughout)**
- Unit tests for each method
- Integration tests for features
- Conformance tests against XSL-FO spec
- Performance benchmarks
- Visual inspection of examples

**4. Documentation (Throughout)**
- Update PLAN.md with progress
- Update README.md with new features
- Add examples demonstrating features
- Write API documentation

**5. Release (End of Phase)**
- Merge feature branches
- Update version number
- Create release notes
- Publish to NuGet
- Announce on GitHub

### Testing Strategy

**Test Coverage Targets:**
- Unit tests: 85%+ code coverage
- Integration tests: All major workflows
- Conformance tests: All XSL-FO features
- Performance tests: No regressions
- Fuzzing tests: Edge cases, malicious input

**Test Categories:**
1. **Unit Tests** - Individual methods
2. **Layout Tests** - AreaTree snapshots
3. **PDF Validation** - Structure, fonts, output
4. **Conformance Tests** - XSL-FO 1.1 spec
5. **Performance Tests** - Speed, memory
6. **Fuzzing Tests** - Malformed input
7. **Visual Tests** - PDF output inspection

### Performance Targets

**Must Maintain:**
- 200-page document: <500ms (currently 150ms)
- Memory: <100MB for 200 pages (currently 22MB)
- Throughput: >400 pages/second (currently 1,333/sec)

**Rationale:** Some features (Knuth-Plass, hyphenation) add overhead, but should remain fast

### Versioning Strategy

**Semantic Versioning:**
- **Phase 1:** v1.1.0 - Critical Pagination & Typography
- **Phase 2:** v1.2.0 - Professional Typography
- **Phase 3:** v2.0.0 - TrueType/OpenType Support (breaking: font API changes)
- **Phase 4:** v2.1.0 - Advanced Table Features
- **Phase 5:** v2.2.0 - Advanced Layout & Positioning
- **Phase 6:** v3.0.0 - Internationalization (breaking: BiDi API changes)

**Release Cadence:** Every 2-3 months

---

## Risk Management

### Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| TrueType parsing complexity exceeds estimate | High | High | Implement in phases, focus on required tables first |
| Knuth-Plass performance unacceptable | Medium | Medium | Make it optional, keep greedy as default |
| BiDi UAX#9 complexity too high | High | Medium | Start with simplified algorithm, iterate |
| Font subsetting bugs corrupt PDFs | Medium | High | Extensive testing, validate output with qpdf |
| Table page breaking breaks existing layouts | Low | High | Comprehensive regression tests |

### Project Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Scope creep (too many features) | Medium | High | Strict phase boundaries, defer non-critical items |
| Zero-deps constraint blocks solutions | Medium | Medium | Research pure .NET alternatives before starting |
| Performance regresses significantly | Low | High | CI performance tests, benchmark every PR |
| Breaking changes disrupt users | Medium | High | Semantic versioning, deprecation warnings |

---

## Success Metrics

### Phase Completion Criteria

Each phase must meet ALL criteria before moving to next:
- ✅ All planned features implemented
- ✅ All tests passing (100% of new tests, 100% of regression tests)
- ✅ Performance targets met
- ✅ Documentation complete (README, examples, API docs)
- ✅ Code review approved
- ✅ No critical bugs
- ✅ Released to NuGet

### Overall Success (End of Phase 6)

**XSL-FO Compliance:**
- Target: 95% of XSL-FO 1.1 specification (up from current 70%)
- Measured by: Conformance test suite passage rate

**Real-World Usability:**
- Target: 95% of common documents render correctly (up from current ~60%)
- Measured by: User-submitted documents, issue reports

**Performance:**
- Target: <500ms for 200-page document (currently 150ms, allow some overhead)
- Target: <200MB memory for 200-page document (currently 22MB)
- Measured by: BenchmarkDotNet suite

**Zero Dependencies:**
- Target: Zero runtime dependencies beyond System.*
- Measured by: Package analysis, dependency graph

**Test Coverage:**
- Target: 200+ passing tests (currently 119)
- Target: 85%+ code coverage
- Measured by: Test suite, coverage tools

**User Adoption:**
- Target: 10,000+ NuGet downloads/month
- Target: 50+ GitHub stars
- Target: Active community (issues, PRs)
- Measured by: NuGet stats, GitHub metrics

---

## Resources & References

### Specifications

- **XSL-FO 1.1:** https://www.w3.org/TR/xsl11/
- **PDF 1.7:** https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf
- **OpenType Spec:** https://docs.microsoft.com/en-us/typography/opentype/spec/
- **TrueType Reference:** https://developer.apple.com/fonts/TrueType-Reference-Manual/
- **Unicode BiDi UAX#9:** https://www.unicode.org/reports/tr9/
- **SVG 1.1:** https://www.w3.org/TR/SVG11/

### Academic Papers

- Knuth & Plass: "Breaking Paragraphs into Lines" (1981)
- Frank Liang: "Word Hy-phen-a-tion by Com-put-er" (1983)
- Unicode Technical Reports (UAX series)

### Implementation References

- TeX source code (line breaking, hyphenation)
- Apache FOP (XSL-FO implementation in Java)
- pdfTeX (PDF generation)

---

## Conclusion

This roadmap transforms Folly from a solid foundation into a best-in-class layout engine while maintaining its core values: **zero dependencies**, **excellent performance**, and **production quality**.

**Key Strengths of This Plan:**
1. **Phased Approach** - Each phase delivers real value
2. **Zero Dependencies** - All implementations are pure .NET
3. **Performance Focus** - Speed remains excellent throughout
4. **Comprehensive** - Addresses all major limitations
5. **Realistic** - Complexity estimates are honest
6. **Tested** - Quality maintained via extensive testing

**After Phase 6 Completion:**
- ~95% XSL-FO 1.1 compliance (up from ~70%)
- Professional typography (justification, hyphenation, Knuth-Plass)
- Custom fonts (TrueType/OpenType)
- Advanced tables (page breaking, row spanning)
- Internationalization (full BiDi UAX#9)
- Absolute positioning
- Basic SVG support
- Accessibility foundations (tagged PDF)
- Still zero runtime dependencies
- Still excellent performance

**Timeline:** 12-18 months for phases 1-6

**Ready to Begin:** Phase 1 can start immediately - the layout engine is well-positioned for these enhancements.
