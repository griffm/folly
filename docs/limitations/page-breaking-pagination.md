# Page Breaking & Pagination Limitations

## Overview

While Folly implements solid basic pagination with multi-page layout and conditional page masters, several advanced page breaking features are not yet supported. These limitations affect professional document typesetting, particularly for long-form content like books and academic papers.

## Current Implementation

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:261-463`

The `LayoutFlowWithPagination()` method handles:
- Multi-page flow with automatic page creation
- Break-before and break-after constraints
- Basic keep-together support
- Multi-column layout with column breaking
- Conditional page master selection

## Limitations

### 1. No Widow/Orphan Control

**Severity**: High for professional publishing
**XSL-FO Properties**: `widows`, `orphans`

**Description**:
- **Widow**: Single line at top of page (from previous paragraph)
- **Orphan**: Single line at bottom of page (from next paragraph)
- No detection or prevention of widows/orphans
- No configurable threshold (e.g., minimum 2 lines)

**Impact**:
- Unprofessional appearance in typeset documents
- Poor reading experience
- Books and academic papers look amateur
- Violates traditional typography rules

**Example Problem**:
```xml
<fo:block>
  This is a long paragraph that spans multiple lines.
  If the last line falls at the top of the next page,
  it creates a widow. This looks terrible in print.
</fo:block>
```

If the page break occurs before the last line, that line appears alone at the top of the next page (widow).

**Expected Behavior**:
```xml
<fo:block widows="2" orphans="2">
  <!-- At least 2 lines must remain together at top/bottom -->
</fo:block>
```

**Proposed Algorithm**:
1. When nearing page bottom, count lines in current paragraph
2. If remaining lines < `widows` value, push entire paragraph to next page
3. If lines at bottom < `orphans` value, push last few lines to next page

**Complexity**: Medium - requires lookahead in line breaking

### 2. Limited Keep Constraints

**Severity**: Medium
**XSL-FO Properties**: `keep-with-next`, `keep-with-previous`, `keep-together.within-page`

**Current Support**: Only `keep-together="always"` on blocks

**Code** (`LayoutEngine.cs:326-336`):
```csharp
var mustKeepTogether = foBlock.KeepTogether == "always";
var blockFitsInColumn = currentY + blockTotalHeight <=
    currentPageMaster.PageHeight - bodyMarginBottom;

if (!blockFitsInColumn)
{
    if (currentY > bodyMarginTop || mustKeepTogether)
    {
        // Move block to next page
    }
}
```

**Not Implemented**:

#### `keep-with-next`
```xml
<fo:block keep-with-next="always">
  Chapter 1
</fo:block>
<fo:block>
  First paragraph of chapter...
</fo:block>
```
**Expected**: Chapter heading and first paragraph stay together
**Actual**: May break between them

#### `keep-with-previous`
```xml
<fo:block>
  See figure below.
</fo:block>
<fo:block keep-with-previous="always">
  <fo:external-graphic src="figure.png"/>
</fo:block>
```
**Expected**: Text and figure stay together
**Actual**: May break between them

#### Fractional Keep Strengths
```xml
<fo:block keep-together.within-page="5">
  <!-- Strength 5 = prefer to keep together -->
</fo:block>
```
**XSL-FO supports**: `always` or integer values 1-999
**Folly supports**: Only `always` (binary decision)

**Impact**:
- Cannot keep headings with following content
- Figures may separate from captions
- List items may break awkwardly
- No priority system for competing constraints

**Proposed Solution**:
1. Track keep relationships between blocks
2. Calculate "cost" of breaking at each position
3. Choose lowest-cost break point
4. Consider keep strength as cost multiplier

### 3. Tables Don't Support Page Breaking

**Severity**: Critical for large tables
**Location**: `LayoutEngine.cs:392-421`

**Current Behavior**:
```csharp
foreach (var foTable in flow.Tables)
{
    var tableArea = LayoutTable(foTable, bodyMarginLeft, currentY, bodyWidth);

    // Check if table fits on current page
    if (currentY + tableArea.Height > currentPageMaster.PageHeight - bodyMarginBottom)
    {
        // Table doesn't fit - add current page and create new one
        areaTree.AddPage(currentPage);
        currentPage = CreatePage(currentPageMaster, pageSequence, pageNumber);
        // Re-position the table for the new page
    }
}
```

**Problem**: Entire table is moved to next page if it doesn't fit.

**Impact**:
- Large tables that exceed one page height cannot be rendered properly
- Table may overflow page boundaries if taller than a single page
- No row-by-row pagination

**Example That Fails**:
```xml
<fo:table>
  <fo:table-body>
    <!-- 50 rows, total height = 1500pt -->
    <!-- Page height = 842pt (A4) -->
    <!-- Table exceeds page height! -->
  </fo:table-body>
</fo:table>
```

**Expected XSL-FO Behavior**:
- Table header repeats on each page
- Table rows split across pages
- Table footer appears on last page only
- Respect `fo:table-row` `keep-together` property

**Proposed Solution**:
1. Layout table row-by-row instead of as single unit
2. Track cumulative height
3. When approaching page bottom:
   - Check if next row fits
   - If not, create page break
   - Repeat table header on new page
4. Handle `keep-together` on rows
5. Handle `keep-with-next`/`keep-with-previous` between rows

**Complexity**: High - requires restructuring table layout algorithm

### 4. Lists Don't Support Page Breaking

**Severity**: High for long lists
**Location**: `LayoutEngine.cs:423-455`

**Current Behavior**: Same as tables - entire list moved if doesn't fit

**Impact**:
- Long lists cannot span multiple pages
- List may overflow if taller than one page
- No item-by-item pagination

**Example That Fails**:
```xml
<fo:list-block>
  <!-- 100 list items, total height exceeds page -->
</fo:list-block>
```

**Expected Behavior**:
- List items break across pages individually
- Respect `keep-together` on list items
- Optionally keep label with body

**Proposed Solution**:
Similar to tables - layout item-by-item and break when necessary.

**Complexity**: Medium - simpler than tables (no header repetition)

### 5. Fixed Column Width Assumption

**Severity**: Low
**Location**: `LayoutEngine.cs:266-267`

**Current Code**:
```csharp
// Get first page master to determine body dimensions
// Note: For simplicity, we assume body dimensions are consistent across all page masters
var firstPageMaster = SelectPageMaster(foRoot, pageSequence, pageNumber: 1, totalPages: 999);
```

**Problem**: Uses dimensions from first page master for entire sequence.

**Impact**:
- Cannot change page size mid-sequence
- Cannot have different margins on different pages
- Conditional page masters with different dimensions don't work properly

**Example That Fails**:
```xml
<fo:page-sequence-master master-name="sequence">
  <fo:single-page-master-reference master-reference="first-page"/>
  <!-- first-page: wide margins, narrow body -->
  <fo:repeatable-page-master-reference master-reference="normal-page"/>
  <!-- normal-page: narrow margins, wide body -->
</fo:page-sequence-master>
```
Content would be laid out for first page's narrow body width on all pages.

**Proposed Solution**:
- Re-layout content for each page master's dimensions
- Cache layout if page masters are identical
- Handle reflow when dimensions change

**Complexity**: High - may require re-layouting content

### 6. No Column Balancing Control

**Severity**: Low
**XSL-FO Property**: `span` on blocks (for multi-column)

**Current Behavior**: Fills columns left-to-right, no balancing

**Impact**:
- Last page of multi-column content may have uneven columns
- Cannot control column balancing strategy

**Example**:
```
Page with 3 columns:
[Full]  [Full]  [1/4 full]  ← Unbalanced
```

**Desired**:
```
[2/3]   [2/3]   [2/3]       ← Balanced
```

**Complexity**: Medium - requires lookahead to distribute content evenly

### 7. No Intelligent Page Break Selection

**Severity**: Medium

**Current Behavior**: Greedy algorithm - breaks when content doesn't fit

**Impact**:
- Page breaks may occur at awkward positions
- No cost function to prefer better break points
- Cannot avoid breaking inside certain constructs

**Better Approach**:
1. Identify potential break points (between paragraphs, after headings, etc.)
2. Assign cost to each break point
3. Choose lowest-cost break near page bottom

**Break Point Costs** (example):
- Between paragraphs: Cost = 1 (good)
- After heading: Cost = 100 (bad - orphan heading)
- Inside table: Cost = 500 (very bad)
- Inside paragraph: Cost = 10 (acceptable if needed)

### 8. No Support for Forced Page Masters

**Severity**: Low
**XSL-FO Property**: `force-page-count` on `fo:page-sequence`

**Description**:
- Cannot force even/odd page count
- Cannot insert blank pages automatically
- Useful for double-sided printing (chapters start on odd page)

**Example**:
```xml
<fo:page-sequence force-page-count="even">
  <!-- If content is odd number of pages, add blank page -->
</fo:page-sequence>
```

**Values**: `auto | even | odd | end-on-even | end-on-odd | no-force`

**Proposed Solution**: After layout, check page count and insert blank pages if needed.

**Complexity**: Low

### 9. No Page Break Penalties

**Severity**: Low to Medium
**Related**: TeX's `\penalty` system

**Description**:
- No fine-grained control over break desirability
- Cannot specify "prefer not to break here" vs "never break"
- Binary decision (break-before=always or nothing)

**Example Use Cases**:
- Slightly discourage breaking after first line of paragraph
- Strongly discourage breaking before last line
- Mildly prefer breaking after long paragraphs

**TeX Approach**:
```tex
\penalty -100  % Encourage breaking here
\penalty 0     % Neutral
\penalty 100   % Discourage breaking
\penalty 10000 % Forbid breaking
```

**XSL-FO Approach**: Integer values for keep properties (1-999)

**Folly Limitation**: Only `always` supported, no integer values

## Performance Considerations

### Current Performance
- Single-pass layout: O(n) where n = block count
- No backtracking or optimization
- Very fast but produces suboptimal breaks

### With Optimal Page Breaking
- Would require dynamic programming: O(n × p) where p = page count
- Significantly slower
- Much better results

**Recommendation**: Make optimal breaking optional:
```csharp
public class LayoutOptions
{
    public PageBreakingStrategy Strategy { get; set; } = PageBreakingStrategy.Greedy;
    // Greedy: Fast, single-pass
    // Optimal: Slower, best quality
}
```

## XSL-FO Specification Compliance

**Properties Implemented**:
- `break-before="always | page"` - Yes
- `break-after="always | page"` - Yes
- `keep-together="always"` - Partial (blocks only)

**Properties Not Supported**:
- `widows` - Not implemented
- `orphans` - Not implemented
- `keep-with-next` - Not implemented
- `keep-with-previous` - Not implemented
- `keep-together` with integer values - Not implemented
- `keep-together.within-page` vs `.within-column` - Not distinguished
- `force-page-count` - Not implemented
- `span` (for column balancing) - Not implemented
- Table row page breaking - Not implemented
- List item page breaking - Not implemented

**Compliance Level**: ~40% for page breaking properties

## Proposed Implementation Priorities

### High Priority (Critical Gaps)
1. **Table row-by-row pagination** - Critical for real-world use
2. **List item pagination** - Common use case
3. **`keep-with-next`/`keep-with-previous`** - Essential for headings

### Medium Priority (Quality Improvements)
4. **Widow/orphan control** - Professional appearance
5. **Variable page dimensions** - Proper conditional page master support

### Low Priority (Nice to Have)
6. **Column balancing** - Aesthetic improvement
7. **Intelligent break point selection** - Quality improvement
8. **`force-page-count`** - Niche use case

## References

1. **XSL-FO 1.1 Specification**:
   - Section 7.20: Keeps and Breaks Properties
   - https://www.w3.org/TR/xsl11/slice7.html#pr-section

2. **TeX Page Breaking Algorithm**:
   - Knuth, D.E. *The TeXbook* (1986), Chapter 15: How TeX Makes Lines into Pages
   - Demonstrates optimal page breaking with penalties

3. **CSS Fragmentation Module**:
   - https://www.w3.org/TR/css-break-3/
   - Similar concepts for web pagination

## See Also

- [tables.md](tables.md) - Table-specific pagination issues
- [line-breaking-text-layout.md](line-breaking-text-layout.md) - Paragraph-level breaking
- [advanced-features.md](advanced-features.md) - Other missing features
