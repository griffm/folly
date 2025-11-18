# Font Architecture Roadmap (Historical)

> **Note:** This is a historical planning document. The font system described in this roadmap has been implemented. See the code in `src/Folly.Fonts/` for the current implementation.

This document outlines the multi-phase approach that was used to evolve Folly's font system from Base 14 Type 1 fonts to supporting TrueType and OpenType fonts with zero external dependencies.

## Current State (Phase 1 - Completed)

### Architecture
- **Centralized Resolution**: `FontResolver` handles all font family normalization and variant selection
- **Data-Driven Registry**: `FontVariantRegistry` maps font families to variants without hardcoded logic
- **Base 14 Fonts**: AFM-based metrics for standard PDF fonts compiled at build time
- **Single Source of Truth**: All font resolution goes through `FontResolver`, eliminating duplication

### Files
- `FontResolver.cs` - Font resolution service
- `FontVariantRegistry.cs` - Variant name mappings
- `StandardFonts.cs` - Base 14 font management
- `FontMetrics.cs` - Text measurement
- `Base14FontsGenerator.cs` - Build-time AFM parsing

### Strengths
- Zero runtime overhead for Base 14 fonts
- Type-safe font properties
- Well-tested (82 unit tests)
- Clean separation of concerns
- Easy to extend with new font families

### Limitations
- Only supports Base 14 PDF fonts
- No custom font loading
- No advanced typography features
- Hardcoded to Type 1 font format

---

## Phase 2: Provider Abstraction Layer

**Goal**: Introduce abstraction layer to support multiple font sources while maintaining backward compatibility.

### Design Principles
1. **Backward Compatibility**: Existing code continues to work unchanged
2. **Lazy Loading**: Font providers initialize only when needed
3. **Fallback Chain**: Try custom fonts → fallback to Base 14
4. **Zero Dependencies**: All implementations in-house
5. **Performance**: No significant overhead for existing Base 14 usage

### Core Abstractions

#### IFontProvider Interface
```csharp
/// <summary>
/// Provides access to a collection of fonts.
/// </summary>
public interface IFontProvider
{
    /// <summary>
    /// Gets a font by its exact name.
    /// </summary>
    IFont? GetFont(string fontName);

    /// <summary>
    /// Gets all available font names from this provider.
    /// </summary>
    IEnumerable<string> GetAvailableFonts();

    /// <summary>
    /// Gets the priority of this provider (higher = tried first).
    /// </summary>
    int Priority { get; }
}
```

#### IFont Interface
```csharp
/// <summary>
/// Represents a font with metrics and rendering capabilities.
/// </summary>
public interface IFont
{
    string Name { get; }
    FontFormat Format { get; }

    // Metrics (in font units)
    double UnitsPerEm { get; }
    double Ascender { get; }
    double Descender { get; }
    double LineGap { get; }

    // Character metrics
    double GetCharWidth(char ch);
    bool HasGlyph(char ch);

    // Advanced metrics (for future use)
    double GetKerning(char left, char right);
    GlyphMetrics GetGlyphMetrics(char ch);
}
```

#### FontFormat Enum
```csharp
public enum FontFormat
{
    Type1,      // AFM-based PDF fonts
    TrueType,   // TTF fonts
    OpenType    // OTF fonts (TTF-based or CFF-based)
}
```

#### FontRegistry (Singleton)
```csharp
/// <summary>
/// Central registry for all font providers with fallback chain.
/// </summary>
public sealed class FontRegistry
{
    private static readonly Lazy<FontRegistry> _instance = new(() => new FontRegistry());
    public static FontRegistry Instance => _instance.Value;

    private readonly List<IFontProvider> _providers = new();

    private FontRegistry()
    {
        // Register Base 14 provider by default
        RegisterProvider(new Base14FontProvider());
    }

    public void RegisterProvider(IFontProvider provider)
    {
        _providers.Add(provider);
        _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    public IFont? GetFont(string fontName)
    {
        // Try each provider in priority order
        foreach (var provider in _providers)
        {
            var font = provider.GetFont(fontName);
            if (font != null)
                return font;
        }

        // Ultimate fallback
        return null;
    }

    public IEnumerable<string> GetAllAvailableFonts()
    {
        return _providers.SelectMany(p => p.GetAvailableFonts()).Distinct();
    }
}
```

### Implementation Strategy

#### Step 1: Create Abstractions
- Define `IFontProvider` and `IFont` interfaces
- Create `FontRegistry` class
- Add `FontFormat` enum
- Define `GlyphMetrics` struct for future use

#### Step 2: Wrap Existing Code
```csharp
/// <summary>
/// Provider for Base 14 PDF Type 1 fonts.
/// </summary>
internal sealed class Base14FontProvider : IFontProvider
{
    public int Priority => 0; // Lowest priority (fallback)

    public IFont? GetFont(string fontName)
    {
        var standardFont = StandardFonts.GetFont(fontName);
        return new Type1Font(standardFont);
    }

    public IEnumerable<string> GetAvailableFonts()
    {
        return new[]
        {
            "Helvetica", "Helvetica-Bold", "Helvetica-Oblique", "Helvetica-BoldOblique",
            "Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic",
            "Courier", "Courier-Bold", "Courier-Oblique", "Courier-BoldOblique",
            "Symbol", "ZapfDingbats"
        };
    }
}

/// <summary>
/// Adapter wrapping StandardFont as IFont.
/// </summary>
internal sealed class Type1Font : IFont
{
    private readonly StandardFont _font;

    public Type1Font(StandardFont font)
    {
        _font = font;
    }

    public string Name => _font.Name;
    public FontFormat Format => FontFormat.Type1;
    public double UnitsPerEm => 1000; // Type 1 fonts use 1000 units per em
    public double Ascender => _font.Ascent;
    public double Descender => _font.Descent;
    public double LineGap => 0; // Type 1 fonts don't have line gap

    public double GetCharWidth(char ch) => _font.GetCharWidth(ch);
    public bool HasGlyph(char ch) => GetCharWidth(ch) > 0;
    public double GetKerning(char left, char right) => 0; // Type 1 fonts in Folly don't have kerning

    public GlyphMetrics GetGlyphMetrics(char ch)
    {
        return new GlyphMetrics
        {
            AdvanceWidth = GetCharWidth(ch),
            LeftSideBearing = 0,
            RightSideBearing = 0
        };
    }
}
```

#### Step 3: Update FontMetrics
```csharp
public double MeasureWidth(string text)
{
    if (string.IsNullOrEmpty(text))
        return 0;

    // Get font from registry instead of StandardFonts
    var font = FontRegistry.Instance.GetFont(
        FontResolver.ResolveFont(FamilyName, IsBold, IsItalic));

    if (font == null)
        return 0; // or use fallback

    var width = 0.0;
    foreach (var ch in text)
    {
        width += font.GetCharWidth(ch);
    }

    // Scale by font size
    return width * Size / font.UnitsPerEm;
}
```

#### Step 4: Maintain Backward Compatibility
- Keep `StandardFonts` class public for existing consumers
- Make `Base14FontProvider` internal
- Ensure all existing tests pass
- No changes required to user code

### Testing Strategy
- Unit tests for each abstraction
- Integration tests for provider chain
- Performance benchmarks (should be ≈ same as current)
- Backward compatibility tests

### Benefits of Phase 2
✅ Clean abstraction for multiple font sources
✅ Zero breaking changes
✅ Foundation for TrueType/OpenType support
✅ Testable in isolation
✅ Easy to add new providers (system fonts, embedded fonts, etc.)

---

## Phase 3: TrueType/OpenType Font Parsing (Zero Dependencies)

**Goal**: Implement native TTF/OTF parsing without external libraries.

### Why Zero Dependencies?
1. **Control**: Full control over parsing logic and performance
2. **Size**: No large dependency trees
3. **Compatibility**: Works everywhere .NET 8 works
4. **Learning**: Deep understanding of font formats
5. **Customization**: Can optimize for PDF-specific needs

### TrueType Font Format Overview

#### File Structure
TrueType fonts are binary files with a table-based structure:

```
[Offset Table]
  - scaler type (4 bytes)
  - numTables (2 bytes)
  - searchRange (2 bytes)
  - entrySelector (2 bytes)
  - rangeShift (2 bytes)

[Table Directory]
  For each table:
    - tag (4 bytes, e.g., "head", "hhea", "maxp")
    - checkSum (4 bytes)
    - offset (4 bytes)
    - length (4 bytes)

[Tables]
  - Multiple tables at various offsets
```

#### Required Tables for PDF Embedding

**Essential Tables:**
1. **`head`** - Font header (font bounding box, units per em, etc.)
2. **`hhea`** - Horizontal header (ascender, descender, line gap)
3. **`hmtx`** - Horizontal metrics (advance widths for each glyph)
4. **`maxp`** - Maximum profile (number of glyphs)
5. **`name`** - Naming table (font name, family, etc.)
6. **`cmap`** - Character to glyph index mapping
7. **`post`** - PostScript information (glyph names)
8. **`glyf`** - Glyph data (TrueType outlines)
9. **`loca`** - Index to location (glyph offsets in glyf table)

**Optional but Useful:**
10. **`kern`** - Kerning table (character pair adjustments)
11. **`OS/2`** - OS/2 and Windows specific metrics
12. **`cvt `** - Control value table (for hinting)
13. **`fpgm`** - Font program (TrueType instructions)
14. **`prep`** - Control value program

### Implementation Strategy

#### Architecture
```
TrueType Parsing Layers:

┌─────────────────────────────────────┐
│   TrueTypeFontProvider              │  ← IFontProvider implementation
│   - Scans directories for TTF files │
│   - Caches parsed fonts             │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│   TrueTypeFont                      │  ← IFont implementation
│   - Exposes metrics and glyphs      │
│   - Handles character → glyph       │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│   TrueTypeParser                    │  ← Core parsing logic
│   - Reads binary font file          │
│   - Parses tables                   │
│   - Validates checksums             │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│   TrueTypeTable classes             │  ← Table-specific parsers
│   - HeadTable, HheaTable, etc.      │
│   - Each knows its own structure    │
└─────────────────────────────────────┘
```

#### Core Classes

##### TrueTypeReader (Binary Reader Helper)
```csharp
/// <summary>
/// Helper for reading TrueType binary data with big-endian byte order.
/// </summary>
internal sealed class TrueTypeReader
{
    private readonly BinaryReader _reader;

    public TrueTypeReader(Stream stream)
    {
        _reader = new BinaryReader(stream);
    }

    public byte ReadByte() => _reader.ReadByte();

    public ushort ReadUInt16()
    {
        // TrueType is big-endian, .NET is little-endian
        var bytes = _reader.ReadBytes(2);
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }

    public short ReadInt16()
    {
        var bytes = _reader.ReadBytes(2);
        return (short)((bytes[0] << 8) | bytes[1]);
    }

    public uint ReadUInt32()
    {
        var bytes = _reader.ReadBytes(4);
        return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
    }

    public int ReadInt32()
    {
        var bytes = _reader.ReadBytes(4);
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }

    public long ReadInt64()
    {
        var bytes = _reader.ReadBytes(8);
        return ((long)bytes[0] << 56) | ((long)bytes[1] << 48) |
               ((long)bytes[2] << 40) | ((long)bytes[3] << 32) |
               ((long)bytes[4] << 24) | ((long)bytes[5] << 16) |
               ((long)bytes[6] << 8) | bytes[7];
    }

    public string ReadTag()
    {
        var bytes = _reader.ReadBytes(4);
        return Encoding.ASCII.GetString(bytes);
    }

    public void Seek(long offset)
    {
        _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
    }

    public long Position => _reader.BaseStream.Position;
}
```

##### TrueTypeParser
```csharp
/// <summary>
/// Parses TrueType font files.
/// </summary>
internal sealed class TrueTypeParser
{
    public static TrueTypeFont Parse(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new TrueTypeReader(stream);

        // Parse offset table
        var offsetTable = ParseOffsetTable(reader);

        // Parse table directory
        var tableDirectory = ParseTableDirectory(reader, offsetTable.NumTables);

        // Parse required tables
        var tables = ParseTables(reader, tableDirectory);

        return new TrueTypeFont(tables);
    }

    private static OffsetTable ParseOffsetTable(TrueTypeReader reader)
    {
        return new OffsetTable
        {
            ScalerType = reader.ReadUInt32(),
            NumTables = reader.ReadUInt16(),
            SearchRange = reader.ReadUInt16(),
            EntrySelector = reader.ReadUInt16(),
            RangeShift = reader.ReadUInt16()
        };
    }

    private static Dictionary<string, TableRecord> ParseTableDirectory(
        TrueTypeReader reader, int numTables)
    {
        var tables = new Dictionary<string, TableRecord>();

        for (int i = 0; i < numTables; i++)
        {
            var tag = reader.ReadTag();
            var record = new TableRecord
            {
                Tag = tag,
                CheckSum = reader.ReadUInt32(),
                Offset = reader.ReadUInt32(),
                Length = reader.ReadUInt32()
            };
            tables[tag] = record;
        }

        return tables;
    }

    private static TrueTypeTables ParseTables(
        TrueTypeReader reader,
        Dictionary<string, TableRecord> directory)
    {
        var tables = new TrueTypeTables();

        // Parse required tables
        if (directory.TryGetValue("head", out var headRecord))
            tables.Head = HeadTable.Parse(reader, headRecord);

        if (directory.TryGetValue("hhea", out var hheaRecord))
            tables.Hhea = HheaTable.Parse(reader, hheaRecord);

        if (directory.TryGetValue("maxp", out var maxpRecord))
            tables.Maxp = MaxpTable.Parse(reader, maxpRecord);

        if (directory.TryGetValue("hmtx", out var hmtxRecord))
            tables.Hmtx = HmtxTable.Parse(reader, hmtxRecord,
                tables.Hhea.NumberOfHMetrics,
                tables.Maxp.NumGlyphs);

        if (directory.TryGetValue("cmap", out var cmapRecord))
            tables.Cmap = CmapTable.Parse(reader, cmapRecord);

        if (directory.TryGetValue("name", out var nameRecord))
            tables.Name = NameTable.Parse(reader, nameRecord);

        if (directory.TryGetValue("post", out var postRecord))
            tables.Post = PostTable.Parse(reader, postRecord);

        // Optional tables
        if (directory.TryGetValue("kern", out var kernRecord))
            tables.Kern = KernTable.Parse(reader, kernRecord);

        if (directory.TryGetValue("OS/2", out var os2Record))
            tables.OS2 = OS2Table.Parse(reader, os2Record);

        return tables;
    }
}
```

#### Table Implementations

##### HeadTable (Font Header)
```csharp
/// <summary>
/// Parses the 'head' table (font header).
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/head
/// </summary>
internal sealed class HeadTable
{
    public ushort MajorVersion { get; set; }
    public ushort MinorVersion { get; set; }
    public int FontRevision { get; set; }
    public uint CheckSumAdjustment { get; set; }
    public uint MagicNumber { get; set; } // Should be 0x5F0F3CF5
    public ushort Flags { get; set; }
    public ushort UnitsPerEm { get; set; }
    public long Created { get; set; }
    public long Modified { get; set; }
    public short XMin { get; set; }
    public short YMin { get; set; }
    public short XMax { get; set; }
    public short YMax { get; set; }
    public ushort MacStyle { get; set; }
    public ushort LowestRecPPEM { get; set; }
    public short FontDirectionHint { get; set; }
    public short IndexToLocFormat { get; set; } // 0=short, 1=long
    public short GlyphDataFormat { get; set; }

    public static HeadTable Parse(TrueTypeReader reader, TableRecord record)
    {
        reader.Seek(record.Offset);

        var table = new HeadTable
        {
            MajorVersion = reader.ReadUInt16(),
            MinorVersion = reader.ReadUInt16(),
            FontRevision = reader.ReadInt32(),
            CheckSumAdjustment = reader.ReadUInt32(),
            MagicNumber = reader.ReadUInt32(),
            Flags = reader.ReadUInt16(),
            UnitsPerEm = reader.ReadUInt16(),
            Created = reader.ReadInt64(),
            Modified = reader.ReadInt64(),
            XMin = reader.ReadInt16(),
            YMin = reader.ReadInt16(),
            XMax = reader.ReadInt16(),
            YMax = reader.ReadInt16(),
            MacStyle = reader.ReadUInt16(),
            LowestRecPPEM = reader.ReadUInt16(),
            FontDirectionHint = reader.ReadInt16(),
            IndexToLocFormat = reader.ReadInt16(),
            GlyphDataFormat = reader.ReadInt16()
        };

        // Validate magic number
        if (table.MagicNumber != 0x5F0F3CF5)
            throw new InvalidDataException("Invalid head table magic number");

        return table;
    }
}
```

##### HheaTable (Horizontal Header)
```csharp
/// <summary>
/// Parses the 'hhea' table (horizontal header).
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/hhea
/// </summary>
internal sealed class HheaTable
{
    public ushort MajorVersion { get; set; }
    public ushort MinorVersion { get; set; }
    public short Ascender { get; set; }
    public short Descender { get; set; }
    public short LineGap { get; set; }
    public ushort AdvanceWidthMax { get; set; }
    public short MinLeftSideBearing { get; set; }
    public short MinRightSideBearing { get; set; }
    public short XMaxExtent { get; set; }
    public short CaretSlopeRise { get; set; }
    public short CaretSlopeRun { get; set; }
    public short CaretOffset { get; set; }
    public short Reserved1 { get; set; }
    public short Reserved2 { get; set; }
    public short Reserved3 { get; set; }
    public short Reserved4 { get; set; }
    public short MetricDataFormat { get; set; }
    public ushort NumberOfHMetrics { get; set; }

    public static HheaTable Parse(TrueTypeReader reader, TableRecord record)
    {
        reader.Seek(record.Offset);

        return new HheaTable
        {
            MajorVersion = reader.ReadUInt16(),
            MinorVersion = reader.ReadUInt16(),
            Ascender = reader.ReadInt16(),
            Descender = reader.ReadInt16(),
            LineGap = reader.ReadInt16(),
            AdvanceWidthMax = reader.ReadUInt16(),
            MinLeftSideBearing = reader.ReadInt16(),
            MinRightSideBearing = reader.ReadInt16(),
            XMaxExtent = reader.ReadInt16(),
            CaretSlopeRise = reader.ReadInt16(),
            CaretSlopeRun = reader.ReadInt16(),
            CaretOffset = reader.ReadInt16(),
            Reserved1 = reader.ReadInt16(),
            Reserved2 = reader.ReadInt16(),
            Reserved3 = reader.ReadInt16(),
            Reserved4 = reader.ReadInt16(),
            MetricDataFormat = reader.ReadInt16(),
            NumberOfHMetrics = reader.ReadUInt16()
        };
    }
}
```

##### HmtxTable (Horizontal Metrics)
```csharp
/// <summary>
/// Parses the 'hmtx' table (horizontal metrics).
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/hmtx
/// </summary>
internal sealed class HmtxTable
{
    public struct LongHorMetric
    {
        public ushort AdvanceWidth;
        public short LeftSideBearing;
    }

    public LongHorMetric[] HMetrics { get; set; } = Array.Empty<LongHorMetric>();
    public short[] LeftSideBearings { get; set; } = Array.Empty<short>();

    public static HmtxTable Parse(
        TrueTypeReader reader,
        TableRecord record,
        int numberOfHMetrics,
        int numGlyphs)
    {
        reader.Seek(record.Offset);

        var table = new HmtxTable();

        // Read long horizontal metrics
        table.HMetrics = new LongHorMetric[numberOfHMetrics];
        for (int i = 0; i < numberOfHMetrics; i++)
        {
            table.HMetrics[i] = new LongHorMetric
            {
                AdvanceWidth = reader.ReadUInt16(),
                LeftSideBearing = reader.ReadInt16()
            };
        }

        // Read left side bearings for remaining glyphs
        int numLeftSideBearings = numGlyphs - numberOfHMetrics;
        if (numLeftSideBearings > 0)
        {
            table.LeftSideBearings = new short[numLeftSideBearings];
            for (int i = 0; i < numLeftSideBearings; i++)
            {
                table.LeftSideBearings[i] = reader.ReadInt16();
            }
        }

        return table;
    }

    public ushort GetAdvanceWidth(int glyphId, int numberOfHMetrics)
    {
        if (glyphId < numberOfHMetrics)
            return HMetrics[glyphId].AdvanceWidth;

        // For glyphs beyond numberOfHMetrics, use the last metric's advance width
        return HMetrics[numberOfHMetrics - 1].AdvanceWidth;
    }
}
```

##### CmapTable (Character to Glyph Mapping)
```csharp
/// <summary>
/// Parses the 'cmap' table (character to glyph mapping).
/// This is the most complex table - maps Unicode code points to glyph IDs.
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/cmap
/// </summary>
internal sealed class CmapTable
{
    public Dictionary<int, int> CharacterToGlyphIndex { get; set; } = new();

    public static CmapTable Parse(TrueTypeReader reader, TableRecord record)
    {
        reader.Seek(record.Offset);

        var version = reader.ReadUInt16();
        var numTables = reader.ReadUInt16();

        // Find the best subtable (prefer Format 4 for BMP, Format 12 for full Unicode)
        EncodingRecord? bestRecord = null;
        long bestOffset = 0;

        for (int i = 0; i < numTables; i++)
        {
            var platformId = reader.ReadUInt16();
            var encodingId = reader.ReadUInt16();
            var offset = reader.ReadUInt32();

            // Prefer Windows Unicode BMP (3,1) or Unicode Full (3,10)
            if (platformId == 3 && (encodingId == 1 || encodingId == 10))
            {
                bestRecord = new EncodingRecord
                {
                    PlatformId = platformId,
                    EncodingId = encodingId,
                    Offset = offset
                };
                bestOffset = record.Offset + offset;
            }
            // Fallback to Unicode (0,3) or (0,4)
            else if (platformId == 0 && (encodingId == 3 || encodingId == 4) && bestRecord == null)
            {
                bestRecord = new EncodingRecord
                {
                    PlatformId = platformId,
                    EncodingId = encodingId,
                    Offset = offset
                };
                bestOffset = record.Offset + offset;
            }
        }

        if (bestRecord == null)
            throw new InvalidDataException("No suitable cmap subtable found");

        // Parse the subtable
        reader.Seek(bestOffset);
        var format = reader.ReadUInt16();

        return format switch
        {
            4 => ParseFormat4(reader),
            12 => ParseFormat12(reader),
            _ => throw new NotSupportedException($"cmap format {format} not supported")
        };
    }

    private static CmapTable ParseFormat4(TrueTypeReader reader)
    {
        // Format 4: Segment mapping to delta values
        // Most common for BMP characters (U+0000 to U+FFFF)

        var length = reader.ReadUInt16();
        var language = reader.ReadUInt16();
        var segCountX2 = reader.ReadUInt16();
        var segCount = segCountX2 / 2;
        var searchRange = reader.ReadUInt16();
        var entrySelector = reader.ReadUInt16();
        var rangeShift = reader.ReadUInt16();

        // Read parallel arrays
        var endCode = new ushort[segCount];
        for (int i = 0; i < segCount; i++)
            endCode[i] = reader.ReadUInt16();

        var reservedPad = reader.ReadUInt16(); // Should be 0

        var startCode = new ushort[segCount];
        for (int i = 0; i < segCount; i++)
            startCode[i] = reader.ReadUInt16();

        var idDelta = new short[segCount];
        for (int i = 0; i < segCount; i++)
            idDelta[i] = reader.ReadInt16();

        var idRangeOffsetStart = reader.Position;
        var idRangeOffset = new ushort[segCount];
        for (int i = 0; i < segCount; i++)
            idRangeOffset[i] = reader.ReadUInt16();

        // Build character to glyph mapping
        var table = new CmapTable();

        for (int i = 0; i < segCount; i++)
        {
            var start = startCode[i];
            var end = endCode[i];

            if (start == 0xFFFF && end == 0xFFFF)
                break; // End marker

            for (int c = start; c <= end; c++)
            {
                int glyphId;

                if (idRangeOffset[i] == 0)
                {
                    // Simple delta mapping
                    glyphId = (c + idDelta[i]) & 0xFFFF;
                }
                else
                {
                    // Indirect mapping through glyphIdArray
                    var offset = idRangeOffsetStart + i * 2 + idRangeOffset[i] + (c - start) * 2;
                    reader.Seek(offset);
                    glyphId = reader.ReadUInt16();

                    if (glyphId != 0)
                        glyphId = (glyphId + idDelta[i]) & 0xFFFF;
                }

                if (glyphId != 0)
                    table.CharacterToGlyphIndex[c] = glyphId;
            }
        }

        return table;
    }

    private static CmapTable ParseFormat12(TrueTypeReader reader)
    {
        // Format 12: Segmented coverage
        // For full Unicode range including supplementary planes

        var reserved = reader.ReadUInt16(); // Should be 0
        var length = reader.ReadUInt32();
        var language = reader.ReadUInt32();
        var numGroups = reader.ReadUInt32();

        var table = new CmapTable();

        for (uint i = 0; i < numGroups; i++)
        {
            var startCharCode = reader.ReadUInt32();
            var endCharCode = reader.ReadUInt32();
            var startGlyphId = reader.ReadUInt32();

            for (uint c = startCharCode; c <= endCharCode; c++)
            {
                var glyphId = startGlyphId + (c - startCharCode);
                table.CharacterToGlyphIndex[(int)c] = (int)glyphId;
            }
        }

        return table;
    }

    private struct EncodingRecord
    {
        public ushort PlatformId;
        public ushort EncodingId;
        public uint Offset;
    }
}
```

##### NameTable (Font Names)
```csharp
/// <summary>
/// Parses the 'name' table (font naming information).
/// Spec: https://docs.microsoft.com/en-us/typography/opentype/spec/name
/// </summary>
internal sealed class NameTable
{
    public Dictionary<int, string> Names { get; set; } = new();

    // Name IDs
    public const int COPYRIGHT = 0;
    public const int FONT_FAMILY = 1;
    public const int FONT_SUBFAMILY = 2;
    public const int UNIQUE_IDENTIFIER = 3;
    public const int FULL_NAME = 4;
    public const int VERSION = 5;
    public const int POSTSCRIPT_NAME = 6;

    public static NameTable Parse(TrueTypeReader reader, TableRecord record)
    {
        reader.Seek(record.Offset);

        var format = reader.ReadUInt16();
        var count = reader.ReadUInt16();
        var stringOffset = reader.ReadUInt16();

        var nameRecords = new List<NameRecord>();
        for (int i = 0; i < count; i++)
        {
            nameRecords.Add(new NameRecord
            {
                PlatformId = reader.ReadUInt16(),
                EncodingId = reader.ReadUInt16(),
                LanguageId = reader.ReadUInt16(),
                NameId = reader.ReadUInt16(),
                Length = reader.ReadUInt16(),
                Offset = reader.ReadUInt16()
            });
        }

        var table = new NameTable();
        var stringBase = record.Offset + stringOffset;

        // Prefer Windows Unicode names (platform 3, encoding 1)
        foreach (var nr in nameRecords.Where(r => r.PlatformId == 3 && r.EncodingId == 1))
        {
            if (!table.Names.ContainsKey(nr.NameId))
            {
                reader.Seek(stringBase + nr.Offset);
                var bytes = new byte[nr.Length];
                for (int i = 0; i < nr.Length; i++)
                    bytes[i] = reader.ReadByte();

                // Windows names are UTF-16 BE
                table.Names[nr.NameId] = Encoding.BigEndianUnicode.GetString(bytes);
            }
        }

        return table;
    }

    public string? GetFontFamily() => Names.GetValueOrDefault(FONT_FAMILY);
    public string? GetFullName() => Names.GetValueOrDefault(FULL_NAME);
    public string? GetPostScriptName() => Names.GetValueOrDefault(POSTSCRIPT_NAME);

    private struct NameRecord
    {
        public ushort PlatformId;
        public ushort EncodingId;
        public ushort LanguageId;
        public ushort NameId;
        public ushort Length;
        public ushort Offset;
    }
}
```

#### TrueTypeFont Implementation
```csharp
/// <summary>
/// Represents a TrueType font implementing IFont.
/// </summary>
internal sealed class TrueTypeFont : IFont
{
    private readonly TrueTypeTables _tables;

    public TrueTypeFont(TrueTypeTables tables)
    {
        _tables = tables;
        Name = _tables.Name?.GetFullName() ?? "Unknown";
    }

    public string Name { get; }
    public FontFormat Format => FontFormat.TrueType;
    public double UnitsPerEm => _tables.Head.UnitsPerEm;
    public double Ascender => _tables.Hhea.Ascender;
    public double Descender => _tables.Hhea.Descender;
    public double LineGap => _tables.Hhea.LineGap;

    public double GetCharWidth(char ch)
    {
        // Map character to glyph ID
        if (!_tables.Cmap.CharacterToGlyphIndex.TryGetValue(ch, out var glyphId))
            return 0; // Glyph not found

        // Get advance width for glyph
        return _tables.Hmtx.GetAdvanceWidth(glyphId, _tables.Hhea.NumberOfHMetrics);
    }

    public bool HasGlyph(char ch)
    {
        return _tables.Cmap.CharacterToGlyphIndex.ContainsKey(ch);
    }

    public double GetKerning(char left, char right)
    {
        if (_tables.Kern == null)
            return 0;

        // Get glyph IDs
        if (!_tables.Cmap.CharacterToGlyphIndex.TryGetValue(left, out var leftGlyph))
            return 0;
        if (!_tables.Cmap.CharacterToGlyphIndex.TryGetValue(right, out var rightGlyph))
            return 0;

        return _tables.Kern.GetKerning(leftGlyph, rightGlyph);
    }

    public GlyphMetrics GetGlyphMetrics(char ch)
    {
        if (!_tables.Cmap.CharacterToGlyphIndex.TryGetValue(ch, out var glyphId))
        {
            return new GlyphMetrics();
        }

        var advanceWidth = _tables.Hmtx.GetAdvanceWidth(glyphId, _tables.Hhea.NumberOfHMetrics);
        var lsb = glyphId < _tables.Hhea.NumberOfHMetrics
            ? _tables.Hmtx.HMetrics[glyphId].LeftSideBearing
            : (glyphId - _tables.Hhea.NumberOfHMetrics < _tables.Hmtx.LeftSideBearings.Length
                ? _tables.Hmtx.LeftSideBearings[glyphId - _tables.Hhea.NumberOfHMetrics]
                : (short)0);

        return new GlyphMetrics
        {
            AdvanceWidth = advanceWidth,
            LeftSideBearing = lsb,
            RightSideBearing = (short)(advanceWidth - lsb) // Approximate
        };
    }
}
```

#### TrueTypeFontProvider Implementation
```csharp
/// <summary>
/// Provider for TrueType fonts from a directory.
/// </summary>
public sealed class TrueTypeFontProvider : IFontProvider
{
    private readonly Dictionary<string, Lazy<TrueTypeFont>> _fonts = new();

    public int Priority => 10; // Higher than Base 14

    public TrueTypeFontProvider(string fontsDirectory)
    {
        if (!Directory.Exists(fontsDirectory))
            throw new DirectoryNotFoundException($"Font directory not found: {fontsDirectory}");

        // Scan for TTF files
        var ttfFiles = Directory.GetFiles(fontsDirectory, "*.ttf", SearchOption.AllDirectories);

        foreach (var file in ttfFiles)
        {
            // Lazy load fonts on demand
            var lazyFont = new Lazy<TrueTypeFont>(() => TrueTypeParser.Parse(file));

            // Use filename as initial key (will be updated on first access)
            var fontName = Path.GetFileNameWithoutExtension(file);
            _fonts[fontName] = lazyFont;
        }
    }

    public IFont? GetFont(string fontName)
    {
        // Try exact match first
        if (_fonts.TryGetValue(fontName, out var lazyFont))
            return lazyFont.Value;

        // Try case-insensitive match
        var key = _fonts.Keys.FirstOrDefault(k =>
            k.Equals(fontName, StringComparison.OrdinalIgnoreCase));

        if (key != null)
            return _fonts[key].Value;

        return null;
    }

    public IEnumerable<string> GetAvailableFonts()
    {
        // Force load all fonts to get actual names
        return _fonts.Values.Select(f => f.Value.Name);
    }

    public void AddFont(string filePath)
    {
        var lazyFont = new Lazy<TrueTypeFont>(() => TrueTypeParser.Parse(filePath));
        var fontName = Path.GetFileNameWithoutExtension(filePath);
        _fonts[fontName] = lazyFont;
    }
}
```

### OpenType Considerations

OpenType fonts (.otf) come in two flavors:
1. **TrueType-based**: Same structure as TTF, can reuse parser
2. **CFF-based**: Uses PostScript outlines instead of TrueType outlines

#### Detecting Font Type
```csharp
public static FontFormat DetectFormat(string filePath)
{
    using var stream = File.OpenRead(filePath);
    using var reader = new TrueTypeReader(stream);

    var scalerType = reader.ReadUInt32();

    return scalerType switch
    {
        0x00010000 or 0x74727565 => FontFormat.TrueType,  // 'true' or version 1.0
        0x4F54544F => FontFormat.OpenType,                 // 'OTTO' (CFF-based)
        _ => throw new InvalidDataException("Unknown font format")
    };
}
```

For CFF-based OpenType:
- Requires parsing the 'CFF ' (Compact Font Format) table
- More complex than TrueType outlines
- Can be deferred to later phase if needed

### PDF Embedding Strategy

#### Font Subsetting
For PDF generation, we need to embed only the glyphs actually used:

```csharp
public class TrueTypeFontSubsetter
{
    public static byte[] CreateSubset(TrueTypeFont font, HashSet<char> usedChars)
    {
        // 1. Determine which glyphs are needed
        var glyphIds = new HashSet<int> { 0 }; // Always include .notdef
        foreach (var ch in usedChars)
        {
            if (font.TryGetGlyphId(ch, out var glyphId))
                glyphIds.Add(glyphId);
        }

        // 2. Create new glyph ID mapping (old → new)
        var glyphMapping = new Dictionary<int, int>();
        var newGlyphId = 0;
        foreach (var oldId in glyphIds.OrderBy(id => id))
        {
            glyphMapping[oldId] = newGlyphId++;
        }

        // 3. Build new tables with subset glyphs
        var subsetTables = new TrueTypeTables
        {
            Head = CloneHeadTable(font),
            Hhea = BuildSubsetHheaTable(font, glyphIds),
            Maxp = BuildSubsetMaxpTable(glyphIds.Count),
            Hmtx = BuildSubsetHmtxTable(font, glyphIds, glyphMapping),
            Cmap = BuildSubsetCmapTable(font, usedChars, glyphMapping),
            Name = CloneNameTable(font),
            Post = BuildSubsetPostTable(font, glyphIds, glyphMapping),
            Glyf = BuildSubsetGlyfTable(font, glyphIds, glyphMapping),
            Loca = BuildSubsetLocaTable(font, glyphIds, glyphMapping)
        };

        // 4. Serialize to bytes
        return SerializeTrueTypeFont(subsetTables);
    }
}
```

#### PDF Font Dictionary
```csharp
public class TrueTypePdfWriter
{
    public void WriteTrueTypeFont(PdfWriter writer, TrueTypeFont font, HashSet<char> usedChars)
    {
        // Create font subset
        var subsetBytes = TrueTypeFontSubsetter.CreateSubset(font, usedChars);

        // Compress font data
        var compressedBytes = CompressWithFlate(subsetBytes);

        // Create font stream object
        var fontStreamId = writer.WriteStream(compressedBytes, new Dictionary<string, object>
        {
            ["Length1"] = subsetBytes.Length
        });

        // Create FontDescriptor
        var descriptorId = writer.WriteObject(new Dictionary<string, object>
        {
            ["Type"] = "/FontDescriptor",
            ["FontName"] = $"/{font.Name}",
            ["Flags"] = 32, // Symbolic
            ["FontBBox"] = $"[{font.BoundingBox}]",
            ["ItalicAngle"] = 0,
            ["Ascent"] = font.Ascender,
            ["Descent"] = font.Descender,
            ["CapHeight"] = font.CapHeight,
            ["StemV"] = 80, // Approximate
            ["FontFile2"] = $"{fontStreamId} 0 R"
        });

        // Create Font dictionary
        var fontId = writer.WriteObject(new Dictionary<string, object>
        {
            ["Type"] = "/Font",
            ["Subtype"] = "/TrueType",
            ["BaseFont"] = $"/{font.Name}",
            ["FirstChar"] = 32,
            ["LastChar"] = 255,
            ["Widths"] = BuildWidthsArray(font, usedChars),
            ["FontDescriptor"] = $"{descriptorId} 0 R"
        });

        return fontId;
    }
}
```

### Performance Optimizations

#### Caching Strategy
```csharp
public sealed class FontCache
{
    private static readonly ConcurrentDictionary<string, IFont> _cache = new();

    public static IFont GetOrLoadFont(string path)
    {
        return _cache.GetOrAdd(path, p => TrueTypeParser.Parse(p));
    }

    public static void Clear() => _cache.Clear();
}
```

#### Memory-Mapped Files
For large font files, use memory mapping:
```csharp
public static TrueTypeFont ParseMemoryMapped(string filePath)
{
    using var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
    using var stream = mmf.CreateViewStream();
    using var reader = new TrueTypeReader(stream);

    return ParseFromReader(reader);
}
```

#### Lazy Table Loading
Only parse tables when needed:
```csharp
public sealed class LazyTrueTypeTables
{
    private readonly TrueTypeReader _reader;
    private readonly Dictionary<string, TableRecord> _directory;

    private HeadTable? _head;
    public HeadTable Head => _head ??= HeadTable.Parse(_reader, _directory["head"]);

    // ... similar for other tables
}
```

### Testing Strategy

#### Unit Tests
- Test each table parser independently
- Test with known font files (e.g., Arial, Times New Roman)
- Test error handling for malformed fonts
- Test character-to-glyph mapping accuracy

#### Integration Tests
- Load real TTF files and measure text
- Compare metrics with known values
- Test font subsetting
- Test PDF embedding

#### Performance Tests
- Benchmark font loading time
- Benchmark character width lookups
- Test with large documents (1000+ fonts)
- Memory usage profiling

### Implementation Phases

#### Phase 3a: Basic TrueType Support
1. Implement TrueTypeReader
2. Implement core table parsers (head, hhea, maxp, hmtx)
3. Implement cmap parser (Format 4 only)
4. Implement name parser
5. Create TrueTypeFont implementing IFont
6. Create TrueTypeFontProvider
7. Integration tests with simple TTF fonts

#### Phase 3b: Advanced Features
1. Implement cmap Format 12 (full Unicode)
2. Implement kern table (kerning)
3. Implement OS/2 table (additional metrics)
4. Add font validation and error recovery

#### Phase 3c: PDF Integration
1. Implement font subsetting
2. Implement glyf and loca table parsing (for subsetting)
3. Create TrueType font stream writer for PDF
4. Update PdfWriter to use TrueType fonts
5. Test with various fonts in PDFs

#### Phase 3d: OpenType CFF Support (Optional)
1. Implement CFF table parser
2. Handle PostScript outlines
3. Test with CFF-based OpenType fonts

### File Structure
```
src/Folly.Core/Fonts/
├── FontResolver.cs                  (existing)
├── FontVariantRegistry.cs           (existing)
├── StandardFonts.cs                 (existing)
├── FontMetrics.cs                   (existing)
├── IFontProvider.cs                 (new - Phase 2)
├── IFont.cs                         (new - Phase 2)
├── FontRegistry.cs                  (new - Phase 2)
├── Base14FontProvider.cs            (new - Phase 2)
└── TrueType/                        (new - Phase 3)
    ├── TrueTypeReader.cs
    ├── TrueTypeParser.cs
    ├── TrueTypeFont.cs
    ├── TrueTypeFontProvider.cs
    ├── TrueTypeFontSubsetter.cs
    ├── Tables/
    │   ├── TableRecord.cs
    │   ├── HeadTable.cs
    │   ├── HheaTable.cs
    │   ├── MaxpTable.cs
    │   ├── HmtxTable.cs
    │   ├── CmapTable.cs
    │   ├── NameTable.cs
    │   ├── PostTable.cs
    │   ├── KernTable.cs
    │   ├── OS2Table.cs
    │   ├── GlyfTable.cs (for subsetting)
    │   └── LocaTable.cs (for subsetting)
    └── GlyphMetrics.cs
```

### Resources and References

#### Specifications
- [OpenType Spec (Microsoft)](https://docs.microsoft.com/en-us/typography/opentype/spec/)
- [TrueType Reference Manual (Apple)](https://developer.apple.com/fonts/TrueType-Reference-Manual/)
- [PDF Font Embedding (Adobe)](https://www.adobe.com/content/dam/acom/en/devnet/pdf/pdfs/PDF32000_2008.pdf)

#### Existing Implementations to Study
- [SixLabors.Fonts](https://github.com/SixLabors/Fonts) - Good reference for table parsing
- [Typography](https://github.com/LayoutFarm/Typography) - More comprehensive
- [iText 7](https://github.com/itext/itext7-dotnet) - PDF font embedding approach

#### Test Fonts
- Liberation Fonts (open source, metric-compatible with Arial/Times)
- GNU FreeFont (extensive Unicode coverage)
- Noto Fonts (Google, very comprehensive)

### Estimated Effort

| Phase | Complexity | Lines of Code | Estimated Time |
|-------|-----------|---------------|----------------|
| Phase 2 (Abstraction) | Low | ~500 | 1-2 days |
| Phase 3a (Basic TTF) | Medium | ~1500 | 1 week |
| Phase 3b (Advanced) | Medium | ~800 | 3-4 days |
| Phase 3c (PDF Integration) | High | ~1200 | 1 week |
| Phase 3d (OpenType CFF) | High | ~2000 | 2 weeks |
| **Total** | | **~6000** | **4-5 weeks** |

### Risk Assessment

#### High Risk
- **Complex binary parsing**: TrueType format is intricate, easy to introduce bugs
- **Character encoding edge cases**: Unicode handling, especially supplementary planes
- **Font subsetting correctness**: Critical for PDF compliance

#### Medium Risk
- **Performance**: Parsing large fonts could be slow without optimization
- **Memory usage**: Caching many fonts could consume significant memory
- **Font validation**: Malformed fonts could crash the parser

#### Mitigation Strategies
1. **Extensive testing** with diverse font files
2. **Incremental development** - basic features first
3. **Reference implementation comparison** - validate against known parsers
4. **Performance profiling** early and often
5. **Robust error handling** - fail gracefully on bad fonts

### Success Criteria

✅ Can load and parse common TrueType fonts (Arial, Times, Courier, etc.)
✅ Accurate character width measurements matching reference implementations
✅ Successful PDF generation with embedded TrueType fonts
✅ Font subsetting works correctly and reduces PDF size
✅ Performance acceptable (< 100ms to load a typical font)
✅ No external dependencies
✅ Comprehensive test coverage (>80%)
✅ All existing functionality remains working

---

## Future Enhancements (Phase 4+)

### Advanced Typography
- Ligatures (fi, fl, etc.)
- Contextual alternates
- Small caps
- Old-style numerals

### Complex Scripts
- Right-to-left text (Arabic, Hebrew)
- Indic scripts (Devanagari, Tamil, etc.)
- Thai, Lao, Khmer
- Combining diacritics

### Font Features
- OpenType feature parsing (GSUB, GPOS tables)
- Font variations (variable fonts)
- Color fonts (COLR, CPAL tables)

### Performance
- On-demand glyph loading
- Partial font file reading (don't load entire file)
- Better caching strategies
- Native memory for large buffers

### Developer Experience
- Font preview/inspection tools
- Font fallback chains
- System font discovery
- Font matching algorithm improvements

---

## Conclusion

This roadmap provides a clear path from the current Base 14 font system to full TrueType/OpenType support with zero external dependencies. The phased approach allows for:

1. **Immediate value** from Phase 1 (completed) - cleaner architecture
2. **Flexibility** from Phase 2 - easy to extend with new font sources
3. **Power** from Phase 3 - full TrueType support for custom fonts
4. **Growth** beyond - advanced typography when needed

The zero-dependency approach is ambitious but achievable. The TrueType format, while complex, is well-documented and has been successfully implemented in many projects. By building our own parser, we gain complete control, deep understanding, and the ability to optimize specifically for PDF generation use cases.

The estimated ~6000 lines of code over 4-5 weeks is realistic for a solid, well-tested implementation. This excludes advanced features like complex script shaping, which would add significant complexity.

The key to success is **incremental development** with continuous testing against real-world fonts and PDF validation tools.
