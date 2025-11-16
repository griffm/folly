# Fonts & Typography Limitations

## Overview

Folly currently supports only the 14 standard PDF Type 1 fonts with accurate Adobe Font Metrics (AFM). While these fonts cover basic document needs, professional typography and international character support require TrueType/OpenType font embedding, which is not yet implemented.

## Current Implementation

**Supported Fonts** (14 standard PDF fonts):
- **Helvetica**: Helvetica, Helvetica-Bold, Helvetica-Oblique, Helvetica-BoldOblique
- **Times**: Times-Roman, Times-Bold, Times-Italic, Times-BoldItalic
- **Courier**: Courier, Courier-Bold, Courier-Oblique, Courier-BoldOblique
- **Symbol**: Symbol
- **ZapfDingbats**: ZapfDingbats

**Font Metrics**: Adobe Font Metrics (AFM) files provide accurate character widths for ~200+ characters per font.

**Location**:
- AFM files: `src/Folly.Pdf/Fonts/AFM/*.afm`
- Font metrics: `src/Folly.Pdf/Fonts/FontMetrics.cs`

## Limitations

### 1. No TrueType/OpenType Font Support

**Severity**: Critical for custom branding and international content
**File Formats**: `.ttf`, `.otf`, `.ttc`, `.woff`

**Description**:
- Cannot embed custom TrueType or OpenType fonts
- Cannot use system fonts beyond the 14 standards
- No support for font files specified in XSL-FO

**Impact**:
- Very limited font selection (only 3 families)
- Cannot match corporate branding guidelines
- Cannot use modern fonts
- Professional documents look generic

**Example That Doesn't Work**:
```xml
<fo:block font-family="Roboto">
  Text in Roboto font
</fo:block>
```
**Current**: Falls back to Helvetica
**Expected**: Loads and embeds Roboto.ttf

**Proposed Implementation**:
1. Parse TTF/OTF font files to extract:
   - Glyph outlines (CFF or glyf tables)
   - Character metrics (hmtx, hhea tables)
   - Character-to-glyph mapping (cmap table)
   - Font metadata (name table)
2. Embed fonts in PDF as TrueType or CIDFont
3. Implement font subsetting to include only used glyphs
4. Generate ToUnicode CMap for text extraction

**Libraries to Consider**:
- **SharpFont** (FreeType wrapper): Parse TTF/OTF
- **Typography** (Pure .NET): TTF/OTF parsing
- **SkiaSharp**: Font rendering (heavier dependency)

### 2. No Font Fallback Mechanism

**Severity**: High for international text
**Standard**: CSS font-family stacks

**Description**:
- If specified font not available, no automatic substitution
- Hardcoded fallback mapping in code
- No font stack support (e.g., "Arial, Helvetica, sans-serif")

**Current Code** (`LayoutEngine.cs:596-624`):
```csharp
// Hardcoded font variant selection
if (inlineFontWeight == "bold" || weight >= 700)
{
    inlineFontFamily = inlineFontFamily switch
    {
        "Helvetica" => "Helvetica-Bold",
        "Times" or "Times-Roman" => "Times-Bold",
        "Courier" => "Courier-Bold",
        _ => inlineFontFamily + "-Bold"  // May not exist!
    };
}
```

**Impact**:
- Specifying unsupported fonts results in default Helvetica
- No graceful degradation
- International characters may not render (if not in standard fonts)

**Proposed Solution**:
```xml
<fo:block font-family="Roboto, Arial, Helvetica, sans-serif">
  Try fonts in order until one is available
</fo:block>
```

### 3. Limited Character Coverage

**Severity**: Critical for international documents
**Encoding**: Standard fonts use WinAnsiEncoding (256 characters)

**Description**:
- Standard PDF fonts support limited character sets
- No Chinese, Japanese, Korean (CJK) characters
- No Arabic, Hebrew (with proper shaping)
- No Cyrillic beyond basic
- No emoji or special symbols (beyond Symbol/ZapfDingbats)

**Character Coverage**:
- **Helvetica/Times/Courier**: ~220 characters (WinAnsiEncoding)
  - Basic Latin (A-Z, a-z, 0-9)
  - Common punctuation
  - Western European accents (é, ñ, ü)
  - Some symbols (©, ®, €)
- **Symbol**: Greek letters, math symbols
- **ZapfDingbats**: Decorative symbols

**Missing**:
- CJK: 中文, 日本語, 한국어
- Arabic: العربية
- Hebrew: עברית
- Extended Cyrillic: Русский (beyond basic)
- Emoji: 😀, 🎉, etc.
- Mathematical symbols beyond Symbol font

**Impact**:
- Cannot create documents in most non-European languages
- International content appears as missing glyphs or boxes
- Multilingual documents impossible

**Workaround**: None for current implementation

**Solution**: Requires Unicode font support with:
- CIDFont for CJK (thousands of glyphs)
- Composite fonts with multiple subfonts
- Unicode cmap tables

### 4. No Advanced OpenType Features

**Severity**: Medium for professional typography
**Features**: Ligatures, kerning, contextual alternates, small caps

**Description**:
- No ligature substitution (fi → fi, fl → fl)
- No kerning pairs (AV, To, We should be closer)
- No contextual alternates
- No small capitals
- No old-style numerals
- No swashes or stylistic sets

**Impact**:
- Text lacks professional polish
- Uneven letter spacing in some combinations
- No access to advanced typographic features

**Examples**:

**Ligatures**:
- Input: "difficult"
- With ligatures: "difﬁcult" (fi is single glyph)
- Current: "difficult" (f and i separate)

**Kerning**:
- Without kerning: `T o` (large gap)
- With kerning: `To` (visually balanced)

**OpenType Features** (not supported):
- `liga` - Standard ligatures
- `kern` - Kerning
- `smcp` - Small capitals
- `onum` - Old-style numerals
- `swsh` - Swashes
- `calt` - Contextual alternates
- `ss01-ss20` - Stylistic sets

**Implementation Requirements**:
1. Parse OpenType GSUB table (glyph substitution)
2. Parse OpenType GPOS table (glyph positioning)
3. Apply features during layout
4. Subset embedded fonts preserve features

**Complexity**: Very High

### 5. No Font Subsetting for Custom Fonts

**Severity**: Medium (file size)

**Current Subsetting**: Only for standard fonts (font subsetting is implemented)

**Description**:
- When TrueType/OpenType support is added, need subsetting
- Embedding full fonts increases PDF size dramatically
- Chinese fonts can be 10+ MB

**Example**:
- Full Adobe Song Std (Simplified Chinese): ~12 MB
- Subset with 500 glyphs: ~200 KB

**Current Status**:
- Font subsetting infrastructure exists for standard fonts
- Will need extension for TTF/OTF

### 6. No Web Font Support

**Severity**: Low
**Formats**: WOFF, WOFF2

**Description**:
- Cannot use web fonts directly
- Must convert to TTF/OTF first

**Impact**: Minor - web fonts are primarily for browsers

### 7. No Font Hinting

**Severity**: Low for PDF (high for screen rendering)

**Description**:
- Font hinting improves rendering at small sizes on screen
- Less important for PDF (typically printed)
- Still affects readability of on-screen PDF viewing

**Implementation**: Would require:
- Parse TrueType instructions (glyf table)
- Execute font program
- Adjust glyph outlines

**Recommendation**: Low priority - PDF rendering less affected

### 8. No Font Synthesis

**Severity**: Medium
**Description**: Cannot synthesize bold/italic if font variants missing

**Example**:
```xml
<fo:block font-family="CustomFont" font-weight="bold">
  Bold text
</fo:block>
```

If `CustomFont-Bold.ttf` doesn't exist:
- **Current**: Falls back to regular weight
- **Ideal**: Synthesize bold by stroking glyphs (low quality)
- **Best**: Warn user and use regular weight

**CSS Approach**: `font-synthesis: weight | style | none`

### 9. Variable Fonts Not Supported

**Severity**: Low (emerging technology)
**Format**: OpenType Variable Fonts (.ttf, .otf with `fvar` table)

**Description**:
- Variable fonts allow continuous weight/width/slant adjustment
- Single file contains multiple font weights
- Efficient but complex to implement

**Impact**: Cannot use modern variable fonts

**Example**:
```xml
<fo:block font-family="Roboto VF" font-weight="350">
  Weight 350 (between Light and Regular)
</fo:block>
```

**Priority**: Low - standard fonts sufficient for now

### 10. No Font Feature Control

**Severity**: Low
**CSS Property**: `font-feature-settings`, `font-variant-*`

**Description**:
- Cannot enable/disable specific OpenType features
- No control over ligatures, kerning, alternates

**Example** (not supported):
```xml
<fo:block font-feature-settings="'liga' 0, 'kern' 1">
  Kerning on, ligatures off
</fo:block>
```

## Font Selection Algorithm

**Current** (simplified):
1. Use specified `font-family` if it's a standard font
2. Apply weight/style modifiers to select variant
3. Fall back to Helvetica if unknown

**Proposed** (with TTF/OTF):
1. Parse `font-family` list: "Roboto, Arial, Helvetica, sans-serif"
2. For each font:
   a. Check if standard PDF font → use it
   b. Check if font file available (loaded or system) → embed it
   c. Try next font in list
3. Fall back to generic family (serif/sans-serif/monospace)
4. Ultimate fallback: Helvetica

## Performance Considerations

**Font Parsing**: TTF/OTF parsing can be slow
- Cache parsed font data
- Load fonts once at initialization
- Subset during PDF generation

**Glyph Lookups**: Thousands of glyphs in CJK fonts
- Use hash tables for glyph ID lookups
- Cache character widths
- Optimize cmap table access

**Memory**: Large fonts consume memory
- Load only required glyphs for subsetting
- Share font data across multiple uses

## XSL-FO Specification Compliance

**Properties Implemented**:
- `font-family` - Partial (standard fonts only)
- `font-size` - Yes
- `font-weight` - Partial (bold mapping only)
- `font-style` - Partial (italic/oblique mapping)

**Properties Not Supported**:
- `font-family` with custom fonts - Not implemented
- `font-family` with font stacks - Not implemented
- `font-selection-strategy` - Not implemented
- `font-variant` - Not implemented (small-caps, etc.)
- `font-stretch` - Not implemented
- Font feature controls - Not implemented

**Compliance Level**: ~40% for font properties

## Proposed Implementation Roadmap

### Phase 1: Basic TrueType Support (High Priority)
1. Parse TTF files for metrics and glyphs
2. Embed TrueType fonts in PDF
3. Subset embedded fonts
4. Support standard Latin character sets
**Effort**: High, **Impact**: Very High

### Phase 2: OpenType and Unicode (High Priority)
1. Parse OTF (CFF outlines)
2. Support Unicode cmaps
3. Implement CIDFont for CJK
**Effort**: Very High, **Impact**: Very High

### Phase 3: Font Features (Medium Priority)
1. Basic kerning support
2. Standard ligatures
3. OpenType feature activation
**Effort**: High, **Impact**: Medium

### Phase 4: Advanced Features (Low Priority)
1. Complex script shaping (Arabic, Indic)
2. Variable fonts
3. Advanced OpenType features
**Effort**: Very High, **Impact**: Low

## References

1. **TrueType Specification**:
   - Apple TrueType Reference Manual
   - https://developer.apple.com/fonts/TrueType-Reference-Manual/

2. **OpenType Specification**:
   - Microsoft OpenType Specification
   - https://docs.microsoft.com/en-us/typography/opentype/spec/

3. **PDF Font Embedding**:
   - PDF Reference 1.7, Section 9: Fonts
   - https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf

4. **Adobe Font Metrics**:
   - AFM File Format Specification
   - Used for current standard font metrics

5. **HarfBuzz**:
   - Text shaping engine for complex scripts
   - https://harfbuzz.github.io/

6. **Typography Library (.NET)**:
   - Pure C# TTF/OTF parsing
   - https://github.com/LayoutFarm/Typography

## See Also

- [line-breaking-text-layout.md](line-breaking-text-layout.md) - Text layout with font metrics
- [bidi-text-support.md](bidi-text-support.md) - BiDi and complex scripts
- [rendering.md](rendering.md) - How fonts are rendered to PDF
- [performance.md](performance.md) - Font loading and caching performance
