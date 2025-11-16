# Table Layout Limitations

## Overview

Folly implements comprehensive table support with column spanning, header/body/footer sections, and border control. However, several advanced features like row spanning across pages, header repetition, and content-based column sizing are not yet implemented.

## Current Implementation

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:962-1166`

**Supported Features**:
- Table structure: header, body, footer
- Column width specification (`fo:table-column`)
- Column spanning (`number-columns-spanned`)
- Cell padding and borders
- Border collapse model
- Auto-width column distribution

**Code Structure**:
```csharp
LayoutTable() → LayoutTableRow() → LayoutTableCell()
```

## Limitations

### 1. Tables Don't Break Across Pages ⚠️ CRITICAL

**Severity**: Critical for real-world documents
**Location**: `LayoutEngine.cs:392-421`

**Current Behavior**:
```csharp
// Table treated as single atomic unit
var tableArea = LayoutTable(foTable, bodyMarginLeft, currentY, bodyWidth);

// If doesn't fit, move entire table to next page
if (currentY + tableArea.Height > pageBottom)
{
    // Move to next page, place table there
}
```

**Problem**: Tables larger than one page cannot render properly.

**Impact**:
- Large tables overflow page boundaries
- Multi-page reports impossible
- Data tables with many rows unusable

**Example That Fails**:
```xml
<fo:table>
  <fo:table-body>
    <fo:table-row><fo:table-cell>Row 1</fo:table-cell></fo:table-row>
    <!-- ...100 more rows... -->
    <fo:table-row><fo:table-cell>Row 100</fo:table-cell></fo:table-row>
  </fo:table-body>
</fo:table>
```

If total height > page height, table either:
- Gets moved to next page (if currently mid-page)
- Overflows page boundaries (if at top of page)

**Proposed Solution**:
1. Layout rows incrementally
2. Track cumulative height
3. When approaching page bottom:
   - Add page break
   - Continue table on next page
   - Optionally repeat header

### 2. No Table Header Repetition

**Severity**: High for multi-page tables
**XSL-FO Element**: `fo:table-header`

**Current Behavior**: Header rendered once at top of table

**Expected Behavior**: Header repeats on each page when table spans multiple pages

**Example**:
```xml
<fo:table>
  <fo:table-header>
    <fo:table-row>
      <fo:table-cell>Column 1</fo:table-cell>
      <fo:table-cell>Column 2</fo:table-cell>
    </fo:table-row>
  </fo:table-header>
  <fo:table-body>
    <!-- Many rows spanning multiple pages -->
  </fo:table-body>
</fo:table>
```

**Current**: Header appears only on first page
**Expected**: Header appears at top of table on each page

**Related**: `table-omit-header-at-break` property (not implemented)

### 3. No Row Spanning Implementation

**Severity**: Medium
**XSL-FO Property**: `number-rows-spanned` on `fo:table-cell`

**Current Status**: Property parsed but not rendered

**Code** (`AreaTree.cs:378-465`):
```csharp
public sealed class TableCellArea : Area
{
    public int NumberColumnsSpanned { get; set; } = 1;  // Works
    public int NumberRowsSpanned { get; set; } = 1;     // Parsed but not used
}
```

**Impact**:
- Cells with `rowspan > 1` don't merge properly
- Complex table layouts impossible
- No vertical cell merging

**Example That Doesn't Work**:
```xml
<fo:table>
  <fo:table-body>
    <fo:table-row>
      <fo:table-cell number-rows-spanned="2">Spans 2 rows</fo:table-cell>
      <fo:table-cell>Row 1, Cell 2</fo:table-cell>
    </fo:table-row>
    <fo:table-row>
      <!-- First cell should be occupied by rowspan from above -->
      <fo:table-cell>Row 2, Cell 2</fo:table-cell>
    </fo:table-row>
  </fo:table-body>
</fo:table>
```

**Expected**: First cell in row 1 extends down into row 2
**Actual**: Both rows render independently, overlap possible

**Implementation Challenge**:
- Need to track which cells are occupied by spans from previous rows
- Adjust column index when layouting cells
- Calculate cell height to cover multiple rows

### 4. Simplified Column Width Calculation

**Severity**: Medium
**Location**: `LayoutEngine.cs:1028-1082`

**Current Algorithm**:
```csharp
// If column specifies width, use it
if (column.ColumnWidth > 0)
    columnWidths.Add(column.ColumnWidth);
else
    columnWidths.Add(0);  // Auto

// Distribute remaining width equally among auto columns
var autoWidth = remainingWidth / autoCount;
```

**Limitations**:

#### No Content-Based Sizing
- Auto-width columns get equal distribution
- Doesn't measure cell content to determine optimal width
- May result in poor column proportions

**Example**:
```
| Short | This is a very long cell content that should get more space |
```
With current algorithm, both columns might get 50% width each.

#### No Proportional Widths
- Cannot specify "column 1 is 2x wider than column 2"
- Percentage widths not fully supported
- No relative sizing (e.g., `2*` notation)

**XSL-FO allows**:
```xml
<fo:table-column column-width="proportional-column-width(1)"/>
<fo:table-column column-width="proportional-column-width(2)"/>
<!-- Second column is 2x wider -->
```

**Folly limitation**: Not implemented

### 5. No Table Footer Positioning Control

**Severity**: Low
**XSL-FO Element**: `fo:table-footer`

**Current Behavior**: Footer rendered after all body rows

**Missing**: `table-omit-footer-at-break` property
- Footer should appear at bottom of each page (when table breaks)
- Or only at end of table

**Example Use Case**:
```xml
<fo:table table-omit-footer-at-break="false">
  <fo:table-footer>
    <fo:table-row>
      <fo:table-cell>Total: $1,234.56</fo:table-cell>
    </fo:table-row>
  </fo:table-footer>
  <!-- Multi-page table body -->
</fo:table>
```

**Expected**: "Total" row appears at bottom of each page
**Current**: Appears only at end of table

### 6. No Minimum/Maximum Column Width

**Severity**: Low
**CSS Properties**: `min-width`, `max-width`

**Description**:
- Cannot specify minimum column width
- Cannot specify maximum column width
- Auto-sizing may produce very narrow or very wide columns

**Example Not Supported**:
```xml
<fo:table-column column-width="auto" min-width="1in" max-width="3in"/>
```

### 7. Border Collapse Edge Cases

**Severity**: Low
**XSL-FO Property**: `border-collapse="collapse" | separate"`

**Current Support**: Basic implementation

**Edge Cases Not Handled**:
- Conflicting border widths (cell vs table)
- Border priority (table > row > cell?)
- Corner rendering when borders differ

**Code** (`AreaTree.cs:319-354`):
```csharp
public sealed class TableArea : Area
{
    public string BorderCollapse { get; set; } = "separate";
    public double BorderSpacing { get; set; } = 2;
}
```

Basic support exists but edge cases may not render correctly per spec.

### 8. No Vertical Alignment per Row

**Severity**: Low
**XSL-FO Property**: `display-align` on `fo:table-row`

**Current Support**: Only on individual cells (`vertical-align`)

**Impact**: Cannot set alignment for entire row, must set on each cell

### 9. No Empty Cell Behavior Control

**Severity**: Very Low
**XSL-FO Property**: `empty-cells="show | hide"`

**Description**: Cannot control whether borders/backgrounds show for empty cells

**CSS Equivalent**: `empty-cells: show | hide`

### 10. No Table as Block-Level Container

**Severity**: Low

**Description**:
- Tables can only contain fo:table-column, fo:table-header, fo:table-body, fo:table-footer
- Cannot embed other block elements in table structure

**Impact**: Limited table composition flexibility

## Additional Limitations

### Cell Content Overflow
- No control over overflow behavior
- Content may overflow cell boundaries
- No text truncation or ellipsis

### Caption Support
- No `fo:table-caption` support
- Cannot add table titles/descriptions
- Must use separate block above/below table

### Accessibility
- No `role` or `scope` attributes for screen readers
- PDF tables not marked up for accessibility
- Missing table structure tags

## Performance Considerations

**Current**: O(rows × cells) layout time
- Each cell laid out independently
- Row height = max cell height
- Simple but may be slow for huge tables

**With Page Breaking**: Would require:
- Row-by-row processing
- Height tracking
- Header storage for repetition
- More complex but necessary

## XSL-FO Specification Compliance

**Properties Implemented**:
- `fo:table`, `fo:table-column`, `fo:table-header`, `fo:table-body`, `fo:table-footer` - Yes
- `fo:table-row`, `fo:table-cell` - Yes
- `column-width` - Yes (explicit values)
- `number-columns-spanned` - Yes
- `number-columns-repeated` - Yes
- `border-collapse` - Yes (basic)
- Cell `padding` - Yes
- Cell `border` - Yes
- Cell `background-color` - Yes
- `vertical-align` on cells - Partial

**Properties Not Supported**:
- Table page breaking - **Not implemented** ⚠️
- Header repetition - **Not implemented** ⚠️
- `number-rows-spanned` - **Not implemented** ⚠️
- `proportional-column-width()` - Not implemented
- `table-omit-header-at-break` - Not implemented
- `table-omit-footer-at-break` - Not implemented
- `fo:table-caption` - Not implemented
- `empty-cells` - Not implemented
- Content-based column sizing - Not implemented
- `min-width` / `max-width` on columns - Not implemented

**Compliance Level**: ~60% for table properties

## Proposed Implementation Priorities

### Critical (Breaks Real Use Cases)
1. **Table page breaking** - Essential for multi-page tables
2. **Header repetition** - Important for readability

### High (Common Requirements)
3. **Row spanning** - Needed for complex layouts
4. **Proportional column widths** - Better layout control

### Medium (Quality Improvements)
5. **Content-based column sizing** - Better auto-layout
6. **Footer repetition control** - Professional tables

### Low (Nice to Have)
7. **Table captions** - Accessibility and clarity
8. **Empty cell control** - Aesthetic fine-tuning

## Implementation Notes

### Row Spanning Algorithm

```csharp
// Track occupied cells in grid
var occupiedCells = new Dictionary<(int row, int col), bool>();

for each row:
    var columnIndex = 0
    for each cell in row:
        // Skip columns occupied by previous rowspans
        while (occupiedCells[(currentRow, columnIndex)])
            columnIndex++

        // Layout cell
        layoutCell(cell, columnIndex)

        // Mark grid positions as occupied
        for r in 0..cell.RowSpan:
            for c in 0..cell.ColSpan:
                occupiedCells[(currentRow + r, columnIndex + c)] = true

        columnIndex += cell.ColSpan
```

### Page Breaking Algorithm

```csharp
var currentY = startY
for each row:
    var rowHeight = calculateRowHeight(row)

    if (currentY + rowHeight > pageBottom):
        // Start new page
        createNewPage()

        // Repeat header
        if (table.HasHeader)
            renderHeader()

        currentY = newPageTop

    renderRow(row, currentY)
    currentY += rowHeight
```

## References

1. **XSL-FO 1.1 Specification**:
   - Section 6.7: Table Formatting Objects
   - Section 7.26: Table Properties
   - https://www.w3.org/TR/xsl11/slice6.html#fo_section

2. **CSS Tables Module**:
   - CSS 2.1 Section 17: Tables
   - https://www.w3.org/TR/CSS2/tables.html
   - Similar concepts

3. **PDF Table Structure**:
   - Tagged PDF for table accessibility
   - PDF Reference 1.7, Section 10.7.4: Table Elements

## See Also

- [page-breaking-pagination.md](page-breaking-pagination.md) - General page breaking issues
- [rendering.md](rendering.md) - How tables are rendered to PDF
- [advanced-features.md](advanced-features.md) - Other missing features
