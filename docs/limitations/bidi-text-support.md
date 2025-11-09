# BiDi (Bidirectional Text) Support Limitations

## Overview

Folly implements a simplified bidirectional text support through the `fo:bidi-override` element. While basic right-to-left (RTL) text rendering works, the implementation does not follow the full Unicode Bidirectional Algorithm (UBA), which limits its ability to handle complex mixed-direction content.

## Current Implementation

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:693-767, 1575-1583`

The current BiDi implementation uses simple character reversal:

```csharp
/// <summary>
/// Reverses text for RTL (right-to-left) rendering.
/// This is a simplified implementation that reverses character order.
/// For proper BiDi support, a full Unicode BiDi algorithm implementation would be needed.
/// </summary>
private string ReverseText(string text)
{
    if (string.IsNullOrEmpty(text))
        return text;

    var chars = text.ToCharArray();
    Array.Reverse(chars);
    return new string(chars);
}
```

When `fo:bidi-override` has `direction="rtl"`, the text is reversed:
```csharp
var processedText = bidiDirection == "rtl" ? ReverseText(bidiText) : bidiText;
```

## Limitations

### 1. Not a Full Unicode BiDi Algorithm

**Severity**: Critical for RTL languages
**Standard**: Unicode Standard Annex #9 (UAX#9)

**Description**:
- Does not implement the Unicode Bidirectional Algorithm
- No support for bidirectional character types (L, R, AL, EN, etc.)
- No support for embedding levels
- No support for neutral characters
- No support for paired bracket handling (since Unicode 6.3)

**Impact**:
- Complex mixed LTR/RTL text renders incorrectly
- Numbers in RTL text may appear in wrong order
- Punctuation may appear on wrong side
- Nested BiDi contexts fail completely

**Example Problems**:

**Problem 1: Numbers in RTL text**
```xml
<fo:bidi-override direction="rtl">
  <fo:inline>العدد 123 هنا</fo:inline>
</fo:bidi-override>
```
**Expected**: "انه 123 ددعلا" (number stays LTR within RTL text)
**Actual**: Complete reversal including number digits

**Problem 2: Mixed English and Arabic**
```xml
<fo:bidi-override direction="rtl">
  <fo:inline>مرحبا Hello العالم</fo:inline>
</fo:bidi-override>
```
**Expected**: Proper bidirectional ordering per UAX#9
**Actual**: Simple character reversal, producing gibberish

**Problem 3: Punctuation**
```xml
<fo:bidi-override direction="rtl">
  <fo:inline>السلام عليكم!</fo:inline>
</fo:bidi-override>
```
**Expected**: Exclamation mark at visual end (logical start) of RTL text
**Actual**: Exclamation mark reversed with text

### 2. No Support for Explicit Directional Formatting

**Severity**: High
**Unicode Characters**: LRM, RLM, LRE, RLE, LRO, RLO, PDF, LRI, RLI, FSI, PDI

**Description**:
- Unicode BiDi control characters are not recognized
- Cannot use Left-to-Right Mark (U+200E) or Right-to-Left Mark (U+200F)
- Cannot use embedding controls (LRE, RLE, etc.)
- Cannot use directional isolates (LRI, RLI, FSI)

**Impact**:
- No fine-grained control over bidirectional ordering
- Cannot fix edge cases with control characters
- Cannot isolate bidirectional runs

**Example**:
```xml
<!-- LRM (U+200E) should force LTR ordering -->
<fo:bidi-override direction="rtl">
  <fo:inline>file&#x200E;.txt</fo:inline>
</fo:bidi-override>
```
Current implementation ignores the LRM character.

### 3. No Automatic Direction Detection

**Severity**: Medium
**XSL-FO Property**: `writing-mode`, `direction`, `unicode-bidi`

**Description**:
- No automatic detection of text direction from content
- Requires explicit `fo:bidi-override` wrapper
- Does not detect strong RTL characters (Arabic, Hebrew)
- No support for `unicode-bidi="embed"` or `"bidi-override"`

**Impact**:
- Users must manually wrap all RTL content
- Error-prone for mixed content
- Verbose XSL-FO markup required

**Current Workaround**: Must explicitly use:
```xml
<fo:bidi-override direction="rtl">مرحبا</fo:bidi-override>
```

**Desired Behavior**:
```xml
<!-- Should auto-detect Arabic as RTL -->
<fo:block>مرحبا</fo:block>
```

### 4. Block-Level Direction Not Supported

**Severity**: High
**XSL-FO Property**: `writing-mode`, `direction` on `fo:block`

**Description**:
- BiDi only works at inline level via `fo:bidi-override`
- No block-level direction property
- No support for `writing-mode="rl-tb"` (right-to-left, top-to-bottom)
- No support for `direction="rtl"` on `fo:block`

**Impact**:
- Cannot set paragraph direction
- Cannot align RTL paragraphs to the right by default
- Cannot handle RTL page flow

**Example That Doesn't Work**:
```xml
<fo:block direction="rtl" text-align="start">
  مرحبا العالم
</fo:block>
```
The `direction="rtl"` is ignored; text aligns to the left instead of right.

### 5. No Mirroring of Layout

**Severity**: Medium
**Scope**: Page layout, table cell order

**Description**:
- RTL languages often require mirrored page layouts
- Table columns should flow right-to-left in RTL documents
- List markers should appear on the right in RTL lists
- No support for mirrored margins/padding

**Impact**:
- RTL documents look wrong structurally
- Tables read in wrong direction
- Lists have markers on wrong side

**Example**:
In a RTL document, a table should have columns ordered right-to-left:
```
| C | B | A |  (RTL)
```
Instead of:
```
| A | B | C |  (LTR)
```

### 6. Character Reversal Breaks Complex Scripts

**Severity**: Critical for some languages
**Affected**: Arabic, Hebrew (with vowel marks), Indic scripts

**Description**:
- Simple character reversal breaks combining marks
- Arabic shaping is not considered
- Contextual forms are not handled
- Vowel marks (diacritics) may separate from base characters

**Impact**:
- Arabic text may not connect properly
- Vowel marks appear on wrong characters
- Text is unreadable

**Example**:
Arabic text with diacritics:
```
كَتَبَ
```
After reversal, the diacritics (َ) may separate from their base letters, producing incorrect rendering.

## Unicode Bidirectional Algorithm Phases

The full UBA consists of several phases that are **not implemented**:

### Phase 1: Paragraph Level Determination
- Determine base paragraph direction
- Not implemented

### Phase 2: Explicit Embeddings and Overrides
- Process LRE, RLE, LRO, RLO, PDF characters
- Process LRI, RLI, FSI, PDI characters
- Build embedding level stack
- Not implemented

### Phase 3: Weak Types Resolution
- Resolve European numbers, separators, terminators
- Resolve neutral characters (whitespace, punctuation)
- Not implemented

### Phase 4: Neutral Character Resolution
- Resolve remaining neutral types
- Not implemented

### Phase 5: Implicit Levels
- Assign levels to characters without explicit levels
- Not implemented

### Phase 6: Reordering
- Reverse character runs based on levels
- **Partially implemented** (simple reversal only)

## Proposed Solutions

### Short Term (Minimal Compliance)

1. **Handle Numbers Separately**
   - Detect number sequences in RTL text
   - Keep number order LTR within RTL text
   - Complexity: Low, Impact: Medium

2. **Support Common Punctuation**
   - Move punctuation to correct visual position
   - Handle quotes, periods, exclamation marks
   - Complexity: Low, Impact: Low

### Medium Term (Basic BiDi)

1. **Implement Simplified BiDi Algorithm**
   - Classify characters as L, R, or neutral
   - Apply basic reordering rules
   - Handle at least Arabic and Hebrew correctly
   - Complexity: Medium, Impact: High

2. **Block-Level Direction Support**
   - Honor `direction` property on `fo:block`
   - Mirror alignment for RTL blocks
   - Complexity: Low, Impact: High

### Long Term (Full Compliance)

1. **Full Unicode BiDi Algorithm (UAX#9)**
   - Implement all phases of UBA
   - Support all character types
   - Support explicit directional formatting
   - Handle nested embeddings
   - Complexity: Very High, Impact: Very High

2. **Arabic Shaping and Contextual Forms**
   - Implement Arabic joining behavior
   - Support contextual glyph substitution
   - Requires OpenType font support
   - Complexity: Very High, Impact: High

3. **RTL Page Layout**
   - Mirror page flow for RTL documents
   - Reverse table column order
   - Position list markers on right
   - Complexity: Medium, Impact: Medium

## Existing BiDi Libraries

Consider integrating existing implementations:

1. **ICU (International Components for Unicode)**
   - C library with .NET wrappers
   - Full UAX#9 implementation
   - License: Unicode License (permissive)
   - URL: https://icu.unicode.org/

2. **Unidecode.NET**
   - Pure .NET BiDi implementation
   - May not be complete UAX#9
   - Check license and completeness

3. **HarfBuzz**
   - Text shaping engine
   - Handles complex scripts
   - C library with .NET bindings
   - License: MIT-style

## Testing Requirements

To properly validate BiDi support, need test cases for:

1. **Arabic text** - Most common RTL script
2. **Hebrew text** - With and without vowel points
3. **Mixed LTR/RTL** - English within Arabic, Arabic within English
4. **Numbers in RTL** - Arabic numerals, European numbers
5. **Punctuation** - Quotes, periods, brackets
6. **Nested embeddings** - Multiple levels of direction changes
7. **Neutral characters** - Whitespace, symbols
8. **BiDi control characters** - LRM, RLM, etc.

## XSL-FO Specification Compliance

**Properties Implemented**:
- `fo:bidi-override` element - Partially (no UAX#9)
- `direction="rtl"` on `fo:bidi-override` - Partially (simple reversal)

**Properties Not Supported**:
- `direction` on `fo:block` - Not implemented
- `writing-mode="rl-tb"` - Not implemented
- `unicode-bidi` - Not implemented
- Automatic BiDi detection - Not implemented

**Compliance Level**: ~20% for BiDi properties

## XSL-FO Specification References

**Section 7.29.1: `direction`**
> Specifies the inline-progression-direction for the context to be either left-to-right or right-to-left.

**Section 7.29.2: `unicode-bidi`**
> This property specifies whether an additional level of embedding (with respect to the Unicode bidirectional algorithm) is opened.

**Section 6.6.2: `fo:bidi-override`**
> The fo:bidi-override formatting object is used where control of the Unicode BIDI algorithm [UNICODE UAX #9] direction is desired.

## References

1. **Unicode Standard Annex #9: Unicode Bidirectional Algorithm**
   - https://www.unicode.org/reports/tr9/
   - Definitive specification

2. **XSL-FO 1.1 Specification**
   - https://www.w3.org/TR/xsl11/
   - Section 6.6.2: `fo:bidi-override`
   - Section 7.29: Writing-mode-related Properties

3. **W3C Internationalization**
   - https://www.w3.org/International/questions/qa-bidi-unicode-controls
   - Practical guidance on BiDi control characters

4. **ICU BiDi Implementation**
   - https://unicode-org.github.io/icu/userguide/transforms/bidi/
   - Reference implementation

## See Also

- [fonts-typography.md](fonts-typography.md) - Arabic shaping requires proper font support
- [rendering.md](rendering.md) - PDF rendering of RTL text
- [line-breaking-text-layout.md](line-breaking-text-layout.md) - Line breaking in RTL text
