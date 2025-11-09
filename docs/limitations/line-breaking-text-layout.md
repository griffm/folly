# Line Breaking & Text Layout Limitations

## Overview

The Folly layout engine uses a simplified greedy word-based line breaking algorithm that prioritizes performance over optimal typography. While this approach is fast and predictable, it has several limitations for professional publishing.

## Current Implementation

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:871-921`

The `BreakLines()` method implements a simple greedy algorithm:

```csharp
private List<string> BreakLines(string text, double availableWidth, Fonts.FontMetrics fontMetrics)
{
    var lines = new List<string>();
    var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

    // Greedy algorithm: add words until line is full, then start new line
    foreach (var word in words)
    {
        var wordWidth = fontMetrics.MeasureWidth(word);
        var spaceWidth = fontMetrics.MeasureWidth(" ");
        var widthWithWord = currentWidth + (currentLine.Length > 0 ? spaceWidth : 0) + wordWidth;

        if (widthWithWord > availableWidth && currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
            currentLine.Clear();
            // ...
        }
    }
}
```

## Limitations

### 1. No Hyphenation Support

**Severity**: High
**XSL-FO Property**: `hyphenate`, `hyphenation-character`, `hyphenation-push-character-count`, `hyphenation-remain-character-count`

**Description**:
- Words are never broken across lines, even when they exceed line width
- Long words that don't fit on a line are placed on a new line, potentially leaving large gaps
- No support for language-specific hyphenation dictionaries

**Impact**:
- Poor line utilization in narrow columns
- Uneven "ragged right" edges in left-aligned text
- Professionally typeset documents (books, magazines) look amateur
- Multi-column layouts suffer from very uneven line lengths

**Example Problem**:
```xml
<fo:block font-size="12pt" text-align="start">
  The extraterritoriality of internationalization is incomprehensible.
</fo:block>
```
In a narrow column, words like "extraterritoriality" and "internationalization" will be placed on new lines, leaving large white spaces.

**Workaround**: None available. Users must manually insert soft hyphens or reword content.

### 2. No Knuth-Plass Algorithm

**Severity**: Medium
**Related**: TeX line breaking algorithm, optimal paragraph layout

**Description**:
- Uses first-fit greedy algorithm instead of total-fit optimization
- Each line is broken independently without considering the paragraph as a whole
- Does not minimize "badness" across all lines in a paragraph

**Impact**:
- Suboptimal line breaks with uneven spacing
- No lookahead to prevent poor breaks in subsequent lines
- Rivers of whitespace may appear in paragraphs
- Inconsistent visual density

**Comparison**:

| Greedy (Current) | Knuth-Plass (Optimal) |
|------------------|----------------------|
| O(n) complexity  | O(n²) complexity     |
| First valid break | Global optimization |
| Fast, simple     | Slower, complex      |
| Uneven quality   | Consistent quality   |

**Academic Reference**:
- Knuth, D.E., and Plass, M.F. "Breaking Paragraphs into Lines." *Software: Practice and Experience* 11.11 (1981): 1119-1184.

### 3. No Text Justification

**Severity**: High
**XSL-FO Property**: `text-align="justify"`, `text-align-last`

**Description**:
- Only supports `text-align`: start, center, end
- No space adjustment to justify text to both margins
- No tracking/kerning adjustment for justification

**Impact**:
- Cannot produce fully justified paragraphs (common in books, newspapers)
- Professional documents look unprofessional
- Many XSL-FO documents specifying `text-align="justify"` will be rendered as left-aligned

**Current Code** (`LayoutEngine.cs:936-943`):
```csharp
var textX = foBlock.TextAlign.ToLowerInvariant() switch
{
    "center" => (availableWidth - textWidth) / 2,
    "end" or "right" => availableWidth - textWidth,
    _ => 0 // start/left
};
```
No case for "justify".

**Workaround**: None. Justified text will appear left-aligned.

### 4. Word-Based Breaking Only

**Severity**: Medium

**Description**:
- Text is split only on whitespace characters (space, tab, newline)
- No character-level breaking for CJK languages
- No emergency breaking for overflowing words
- `StringSplitOptions.RemoveEmptyEntries` removes all whitespace-only tokens

**Impact**:
- Chinese, Japanese, Korean text may not break correctly
- Long URLs or code snippets cannot be broken
- Words wider than available width overflow or disappear

**Example Problem**:
```xml
<fo:block>https://example.com/very/long/url/path/that/exceeds/line/width</fo:block>
```
This URL would either overflow the margin or be placed entirely on a new line.

### 5. No Emergency Breaking

**Severity**: Medium
**XSL-FO Property**: `wrap-option="wrap"` vs `wrap-option="no-wrap"`

**Description**:
- If a single word exceeds `availableWidth`, it's still added to the line
- No character-level breaking as a last resort
- No indication to the user that overflow has occurred

**Current Behavior**:
- Word is placed on a new line by itself
- If still too wide, it's rendered anyway, potentially overflowing page margins
- PDF renderer will draw text outside the block boundaries

**Proposed Emergency Strategy**:
1. Try word-level breaking (current)
2. If word > availableWidth, try hyphenation
3. If no hyphenation, break at character level
4. If still too long, truncate with ellipsis or overflow

### 6. No Support for `wrap-option` Property

**Severity**: Low
**XSL-FO Property**: `wrap-option="no-wrap" | "wrap"`

**Description**:
- Always wraps text, regardless of `wrap-option` property
- No support for preformatted text that should not wrap
- `white-space` property not fully implemented

**Impact**:
- Cannot render code listings without line breaking
- Preformatted text (like ASCII art) will be reformatted

## Proposed Solutions

### Short Term (Easy Wins)

1. **Add `text-align="justify"` support**
   - Implement inter-word space adjustment
   - Distribute extra space evenly across spaces
   - Complexity: Medium, Impact: High

2. **Emergency breaking for overflow words**
   - Character-level breaking when word exceeds line width
   - Complexity: Low, Impact: Medium

3. **Respect `wrap-option="no-wrap"`**
   - Skip line breaking when `wrap-option="no-wrap"`
   - Complexity: Low, Impact: Low

### Long Term (Significant Effort)

1. **Implement Knuth-Plass algorithm**
   - Paragraph-level optimization
   - Minimize badness across all lines
   - Complexity: Very High, Impact: High

2. **Add hyphenation support**
   - Load language-specific hyphenation dictionaries
   - Integrate TeX hyphenation patterns
   - Respect `hyphenate` property and related settings
   - Complexity: Very High, Impact: Very High

3. **CJK line breaking**
   - Implement UAX #14 (Unicode Line Breaking Algorithm)
   - Support character-based breaking for CJK
   - Handle prohibited breaks (e.g., before closing punctuation)
   - Complexity: High, Impact: Medium

## Performance Considerations

Current greedy algorithm: **O(n)** where n = word count
- Very fast: processes thousands of words per millisecond
- Single pass, no backtracking
- Minimal memory allocation

Knuth-Plass algorithm: **O(n²)** to **O(n³)**
- Requires dynamic programming table
- Multiple passes to find optimal breaks
- Significantly slower but produces better results

**Recommendation**: Keep greedy as default, add Knuth-Plass as optional setting:
```csharp
public class LayoutOptions
{
    public LineBreakingAlgorithm Algorithm { get; set; } = LineBreakingAlgorithm.Greedy;
    // LineBreakingAlgorithm.Greedy | LineBreakingAlgorithm.Optimal
}
```

## XSL-FO Specification Compliance

**Properties Not Supported**:
- `hyphenate` (no, yes) - Not implemented
- `hyphenation-character` - Not implemented
- `hyphenation-push-character-count` - Not implemented
- `hyphenation-remain-character-count` - Not implemented
- `text-align="justify"` - Not implemented
- `text-align-last` - Not implemented
- `wrap-option` - Not implemented
- `line-break` - Not implemented (for CJK)
- `white-space-treatment` - Partially implemented
- `white-space-collapse` - Partially implemented
- `linefeed-treatment` - Not implemented

**Compliance Level**: ~30% for line breaking properties

## References

1. **XSL-FO 1.1 Specification**: https://www.w3.org/TR/xsl11/
   - Section 7.16: Area Alignment Properties
   - Section 7.17: Character Properties

2. **Knuth-Plass Algorithm**:
   - Original paper: *Breaking Paragraphs into Lines* (1981)
   - TeX implementation reference

3. **Unicode Line Breaking**:
   - UAX #14: Unicode Line Breaking Algorithm
   - https://www.unicode.org/reports/tr14/

4. **Hyphenation**:
   - TeX hyphenation patterns
   - Liang, F.M. *Word Hy-phen-a-tion by Com-put-er* (1983)

## See Also

- [fonts-typography.md](fonts-typography.md) - Related font metrics issues
- [rendering.md](rendering.md) - How text is rendered to PDF
- [performance.md](performance.md) - Performance trade-offs
