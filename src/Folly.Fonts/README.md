# Folly.Fonts

Pure .NET TrueType and OpenType font parser with **zero runtime dependencies**.

## Overview

Folly.Fonts is a standalone library for parsing TrueType (.ttf) and OpenType (.otf) font files. It provides comprehensive font metrics, character-to-glyph mapping, and font metadata without requiring any external dependencies.

This library is designed to be generic and reusable - it has no dependencies on the Folly layout engine and can be used in any .NET project that needs to work with fonts.

## Features

- ✅ **Zero Dependencies**: Pure .NET implementation, no native libraries required
- ✅ **TrueType Support**: Full parsing of .ttf font files
- ✅ **OpenType Support**: Parsing of OpenType fonts (CFF outlines not yet fully implemented)
- ✅ **Comprehensive Metrics**: Font metrics (ascender, descender, line gap, units per em)
- ✅ **Character Mapping**: Unicode character-to-glyph index mapping
- ✅ **Glyph Metrics**: Advance widths and left side bearings for all glyphs
- ✅ **Font Metadata**: Family name, style, version, PostScript name
- ✅ **Multiple cmap Formats**: Supports formats 0, 4, and 12 (full Unicode range)
- ✅ **Table Parsing**: head, maxp, hhea, hmtx, name, cmap, loca, post, OS/2

## Installation

```bash
dotnet add package Folly.Fonts
```

## Usage

### Basic Font Loading

```csharp
using Folly.Fonts;

// Load a font file
var font = FontParser.Parse("path/to/font.ttf");

// Access font metadata
Console.WriteLine($"Font: {font.FamilyName} {font.SubfamilyName}");
Console.WriteLine($"Version: {font.Version}");
Console.WriteLine($"Glyphs: {font.GlyphCount}");
```

### Font Metrics

```csharp
// Get font units per em (typically 1000 or 2048)
int unitsPerEm = font.UnitsPerEm;

// Get vertical metrics
int ascender = font.Ascender;   // Distance from baseline to top
int descender = font.Descender; // Distance from baseline to bottom (negative)
int lineGap = font.LineGap;     // Additional space between lines

// Calculate line height in font units
int lineHeight = ascender - descender + lineGap;
```

### Character Mapping

```csharp
// Check if a character is supported
bool hasCharacter = font.HasCharacter('A');

// Get glyph index for a character
if (font.CharacterToGlyphIndex.TryGetValue('A', out ushort glyphIndex))
{
    Console.WriteLine($"Character 'A' maps to glyph index {glyphIndex}");
}

// Get advance width for a character
ushort width = font.GetAdvanceWidth('A');
Console.WriteLine($"Character 'A' width: {width} font units");
```

### Font Validation

```csharp
// Quick check if a file is a valid font
bool isValid = FontParser.IsValidFontFile("path/to/file.ttf");

if (isValid)
{
    var font = FontParser.Parse("path/to/file.ttf");
}
```

## Supported Tables

| Table | Description | Status |
|-------|-------------|--------|
| `head` | Font header | ✅ Complete |
| `maxp` | Maximum profile | ✅ Complete |
| `hhea` | Horizontal header | ✅ Complete |
| `hmtx` | Horizontal metrics | ✅ Complete |
| `name` | Font naming | ✅ Complete |
| `cmap` | Character to glyph mapping | ✅ Complete (formats 0, 4, 12) |
| `loca` | Glyph locations | ✅ Complete |
| `post` | PostScript information | ✅ Complete |
| `OS/2` | Windows metrics | ✅ Complete |
| `glyf` | Glyph data (TrueType) | 🔄 Planned (outline parsing) |
| `CFF ` | Glyph data (OpenType) | 🔄 Planned |
| `kern` | Kerning pairs | 🔄 Planned |
| `GPOS` | Glyph positioning | 🔄 Planned |
| `GSUB` | Glyph substitution | 🔄 Planned |

## Architecture

### Core Components

- **`BigEndianBinaryReader`**: Utility for reading big-endian binary data (fonts use network byte order)
- **`FontFileReader`**: Low-level reader for font file structure and table directory
- **`FontParser`**: High-level API for loading and parsing font files
- **`Tables/*`**: Individual parsers for each font table (HeadTableParser, CmapTableParser, etc.)
- **`Models/*`**: Data models for font data (FontFile, TableDirectory, OS2Table, PostTable)

### Design Principles

1. **Zero Dependencies**: Only System.* libraries are used
2. **Spec Compliance**: Follows OpenType and TrueType specifications exactly
3. **Error Handling**: Comprehensive validation and clear error messages
4. **Performance**: Efficient parsing with minimal allocations
5. **Testability**: Comprehensive test suite with real public domain fonts

## Testing

The library includes comprehensive tests using real public domain fonts:

- **Liberation Sans** (SIL OFL): Widely used, excellent metrics
- **Roboto** (Apache 2.0): Modern font with comprehensive Unicode coverage

Run tests:

```bash
dotnet test tests/Folly.FontTests/Folly.FontTests.csproj
```

## Roadmap

### Phase 3.1 (Current)
- ✅ Parse required tables (head, maxp, hhea, hmtx, name, cmap, loca, post, OS/2)
- ✅ Character-to-glyph mapping
- ✅ Basic font metrics
- ✅ Comprehensive test suite

### Phase 3.2 (Next)
- ⏳ Parse `kern` table for basic kerning pairs
- ⏳ Parse `glyf` table for TrueType glyph outlines
- ⏳ Parse `CFF ` table for OpenType glyph outlines

### Phase 3.3 (Future)
- ⏳ Font subsetting (extract only used glyphs)
- ⏳ Font embedding (generate subset fonts)
- ⏳ Advanced OpenType features (GPOS, GSUB tables)

### Phase 3.4 (Future)
- ⏳ Font fallback and family stacks
- ⏳ System font discovery (Windows, macOS, Linux)
- ⏳ Font collection (.ttc) support

## Specifications

This library follows these official specifications:

- [OpenType Specification](https://docs.microsoft.com/en-us/typography/opentype/spec/) (Microsoft)
- [TrueType Reference Manual](https://developer.apple.com/fonts/TrueType-Reference-Manual/) (Apple)

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please ensure:

1. Zero runtime dependencies are maintained
2. All new code has XML documentation comments
3. Comprehensive tests are added for new features
4. Code follows existing patterns and conventions

## Credits

- OpenType specification: Microsoft Typography
- TrueType specification: Apple Inc.
- Test fonts: Liberation Fonts project, Google Fonts (Roboto)
