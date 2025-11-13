using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Folly.Pdf;

/// <summary>
/// Handles embedding TrueType fonts in PDF documents.
/// Creates font descriptors, font streams, and ToUnicode CMaps.
/// </summary>
internal class TrueTypeFontEmbedder
{
    private readonly PdfWriter _writer;

    public TrueTypeFontEmbedder(PdfWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    /// <summary>
    /// Embeds a TrueType font subset in the PDF using Type 0 composite font with Identity-H encoding.
    /// This supports full Unicode (not limited to 0-255) by using glyph IDs as character codes.
    /// Returns the font dictionary object ID.
    /// </summary>
    public int EmbedTrueTypeFont(
        string fontName,
        byte[] fontData,
        Dictionary<char, ushort> characterToGlyphIndex,
        int unitsPerEm,
        short ascender,
        short descender,
        short xMin,
        short yMin,
        short xMax,
        short yMax,
        ushort[]? glyphAdvanceWidths = null)
    {
        // Step 1: Write the font stream (compressed TrueType data)
        int fontStreamId = WriteFontStream(fontData);

        // Step 2: Write the font descriptor
        int fontDescriptorId = WriteFontDescriptor(
            fontName,
            fontStreamId,
            unitsPerEm,
            ascender,
            descender,
            xMin,
            yMin,
            xMax,
            yMax);

        // Step 3: Write the CIDFont Type 2 dictionary (descendant font)
        int cidFontId = WriteCIDFontType2Dictionary(
            fontName,
            fontDescriptorId,
            characterToGlyphIndex,
            glyphAdvanceWidths,
            unitsPerEm);

        // Step 4: Write the ToUnicode CMap (multi-byte character codes)
        int toUnicodeId = WriteToUnicodeCMap(characterToGlyphIndex);

        // Step 5: Write the Type 0 composite font dictionary
        int fontDictId = WriteType0FontDictionary(
            fontName,
            cidFontId,
            toUnicodeId);

        return fontDictId;
    }

    /// <summary>
    /// Writes a compressed TrueType font stream.
    /// </summary>
    private int WriteFontStream(byte[] fontData)
    {
        // Compress the font data
        byte[] compressedData;
        using (var ms = new MemoryStream())
        {
            using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal))
            {
                deflate.Write(fontData, 0, fontData.Length);
            }
            compressedData = ms.ToArray();
        }

        var streamId = _writer.BeginObject();
        _writer.WriteLine("<<");
        _writer.WriteLine($"  /Length {compressedData.Length}");
        _writer.WriteLine($"  /Length1 {fontData.Length}"); // Uncompressed length
        _writer.WriteLine("  /Filter /FlateDecode");
        _writer.WriteLine(">>");
        _writer.WriteLine("stream");

        // Write compressed font data
        var stream = _writer.GetStream();
        stream.Write(compressedData, 0, compressedData.Length);
        _writer.UpdatePosition(compressedData.Length);

        _writer.WriteLine("");
        _writer.WriteLine("endstream");
        _writer.EndObject();

        return streamId;
    }

    /// <summary>
    /// Writes a TrueType font descriptor.
    /// </summary>
    private int WriteFontDescriptor(
        string fontName,
        int fontStreamId,
        int unitsPerEm,
        short ascender,
        short descender,
        short xMin,
        short yMin,
        short xMax,
        short yMax)
    {
        var descriptorId = _writer.BeginObject();
        _writer.WriteLine("<<");
        _writer.WriteLine("  /Type /FontDescriptor");
        _writer.WriteLine($"  /FontName /{fontName}");
        _writer.WriteLine("  /Flags 32"); // Symbolic font (bit 6)
        _writer.WriteLine($"  /FontBBox [{xMin} {yMin} {xMax} {yMax}]");
        _writer.WriteLine($"  /ItalicAngle 0");
        _writer.WriteLine($"  /Ascent {ascender}");
        _writer.WriteLine($"  /Descent {descender}");
        _writer.WriteLine($"  /CapHeight {ascender}"); // Approximation
        _writer.WriteLine($"  /StemV 80"); // Approximation for normal weight
        _writer.WriteLine($"  /FontFile2 {fontStreamId} 0 R"); // TrueType font stream
        _writer.WriteLine(">>");
        _writer.EndObject();

        return descriptorId;
    }

    /// <summary>
    /// Writes a ToUnicode CMap for text extraction.
    /// Maps character codes (glyph IDs, 2 bytes) to Unicode values.
    /// Uses Identity-H encoding (multi-byte) to support full Unicode range.
    /// </summary>
    private int WriteToUnicodeCMap(Dictionary<char, ushort> characterToGlyphIndex)
    {
        var sb = new StringBuilder();

        sb.AppendLine("/CIDInit /ProcSet findresource begin");
        sb.AppendLine("12 dict begin");
        sb.AppendLine("begincmap");
        sb.AppendLine("/CIDSystemInfo");
        sb.AppendLine("<< /Registry (Adobe)");
        sb.AppendLine("/Ordering (UCS)");
        sb.AppendLine("/Supplement 0");
        sb.AppendLine(">> def");
        sb.AppendLine("/CMapName /Adobe-Identity-UCS def");
        sb.AppendLine("/CMapType 2 def");
        sb.AppendLine("1 begincodespacerange");
        sb.AppendLine("<0000> <FFFF>");  // 2-byte character codes
        sb.AppendLine("endcodespacerange");

        // Build character code to Unicode mappings
        // Character code = glyph ID (2 bytes)
        var sortedChars = characterToGlyphIndex.Keys.OrderBy(c => (int)c).ToList();

        if (sortedChars.Count > 0)
        {
            sb.AppendLine($"{sortedChars.Count} beginbfchar");

            foreach (var ch in sortedChars)
            {
                // Character code = glyph ID (2 bytes, no modulo-256!)
                ushort glyphId = characterToGlyphIndex[ch];

                // Unicode value
                int unicode = (int)ch;

                // Write as 2-byte hex codes
                sb.AppendLine($"<{glyphId:X4}> <{unicode:X4}>");
            }

            sb.AppendLine("endbfchar");
        }

        sb.AppendLine("endcmap");
        sb.AppendLine("CMapName currentdict /CMap defineresource pop");
        sb.AppendLine("end");
        sb.AppendLine("end");

        // Convert to bytes
        byte[] cmapData = Encoding.ASCII.GetBytes(sb.ToString());

        // Write CMap stream
        var cmapId = _writer.BeginObject();
        _writer.WriteLine("<<");
        _writer.WriteLine($"  /Length {cmapData.Length}");
        _writer.WriteLine(">>");
        _writer.WriteLine("stream");
        var stream = _writer.GetStream();
        stream.Write(cmapData, 0, cmapData.Length);
        _writer.UpdatePosition(cmapData.Length);
        _writer.WriteLine("");
        _writer.WriteLine("endstream");
        _writer.EndObject();

        return cmapId;
    }

    /// <summary>
    /// Writes a Type 0 composite font dictionary with Identity-H encoding.
    /// This is the top-level font dictionary that references the CIDFont.
    /// </summary>
    private int WriteType0FontDictionary(
        string fontName,
        int cidFontId,
        int toUnicodeId)
    {
        var fontDictId = _writer.BeginObject();
        _writer.WriteLine("<<");
        _writer.WriteLine("  /Type /Font");
        _writer.WriteLine("  /Subtype /Type0");
        _writer.WriteLine($"  /BaseFont /{fontName}");
        _writer.WriteLine("  /Encoding /Identity-H"); // Horizontal identity mapping (2-byte codes)
        _writer.WriteLine($"  /DescendantFonts [{cidFontId} 0 R]");
        _writer.WriteLine($"  /ToUnicode {toUnicodeId} 0 R");
        _writer.WriteLine(">>");
        _writer.EndObject();

        return fontDictId;
    }

    /// <summary>
    /// Writes a CIDFont Type 2 dictionary (for TrueType-based CID fonts).
    /// This is the descendant font that contains the actual font data.
    /// </summary>
    private int WriteCIDFontType2Dictionary(
        string fontName,
        int fontDescriptorId,
        Dictionary<char, ushort> characterToGlyphIndex,
        ushort[]? glyphAdvanceWidths,
        int unitsPerEm)
    {
        var cidFontId = _writer.BeginObject();
        _writer.WriteLine("<<");
        _writer.WriteLine("  /Type /Font");
        _writer.WriteLine("  /Subtype /CIDFontType2");
        _writer.WriteLine($"  /BaseFont /{fontName}");

        // CIDSystemInfo identifies the character collection
        _writer.WriteLine("  /CIDSystemInfo <<");
        _writer.WriteLine("    /Registry (Adobe)");
        _writer.WriteLine("    /Ordering (Identity)");
        _writer.WriteLine("    /Supplement 0");
        _writer.WriteLine("  >>");

        _writer.WriteLine($"  /FontDescriptor {fontDescriptorId} 0 R");

        // Use Identity mapping (glyph ID = CID)
        _writer.WriteLine("  /CIDToGIDMap /Identity");

        // Write width array (W) for CID fonts
        WriteCIDWidthArray(characterToGlyphIndex, glyphAdvanceWidths, unitsPerEm);

        // Default width (fallback if character not in W array)
        _writer.WriteLine("  /DW 1000"); // Default width in font units

        _writer.WriteLine(">>");
        _writer.EndObject();

        return cidFontId;
    }

    /// <summary>
    /// Writes the W (width) array for a CIDFont.
    /// Format: /W [c1 [w1 w2 ... wn] c2 [w1 w2 ... wm] ...]
    /// Where c is the starting CID and following array contains consecutive widths.
    /// </summary>
    private void WriteCIDWidthArray(
        Dictionary<char, ushort> characterToGlyphIndex,
        ushort[]? glyphAdvanceWidths,
        int unitsPerEm)
    {
        if (characterToGlyphIndex.Count == 0)
        {
            _writer.WriteLine("  /W []");
            return;
        }

        // Build a sorted list of (glyphId, width) pairs
        var glyphWidths = new List<(ushort glyphId, int width)>();

        foreach (var kvp in characterToGlyphIndex)
        {
            ushort glyphId = kvp.Value;

            // Get actual width if available, otherwise use default
            int width = 1000; // Default width in font units
            if (glyphAdvanceWidths != null && glyphId < glyphAdvanceWidths.Length)
            {
                width = glyphAdvanceWidths[glyphId];
            }

            glyphWidths.Add((glyphId, width));
        }

        // Sort by glyph ID
        glyphWidths.Sort((a, b) => a.glyphId.CompareTo(b.glyphId));

        // Write W array
        // For simplicity, we'll write individual entries: /W [gid [width] gid [width] ...]
        // A more compact format would group consecutive glyphs, but this is clearer
        _writer.Write("  /W [");

        for (int i = 0; i < glyphWidths.Count; i++)
        {
            var (glyphId, width) = glyphWidths[i];
            _writer.Write($"{glyphId} [{width}]");

            if (i < glyphWidths.Count - 1)
            {
                _writer.Write(" ");
            }
        }

        _writer.WriteLine("]");
    }

    /// <summary>
    /// Generates a ToUnicode CMap as a string.
    /// This is a helper for testing and debugging.
    /// Uses 2-byte glyph IDs as character codes (no modulo-256).
    /// </summary>
    public static string GenerateToUnicodeCMapString(Dictionary<char, ushort> characterToGlyphIndex)
    {
        var sb = new StringBuilder();

        sb.AppendLine("/CIDInit /ProcSet findresource begin");
        sb.AppendLine("12 dict begin");
        sb.AppendLine("begincmap");
        sb.AppendLine("/CIDSystemInfo");
        sb.AppendLine("<< /Registry (Adobe)");
        sb.AppendLine("/Ordering (UCS)");
        sb.AppendLine("/Supplement 0");
        sb.AppendLine(">> def");
        sb.AppendLine("/CMapName /Adobe-Identity-UCS def");
        sb.AppendLine("/CMapType 2 def");
        sb.AppendLine("1 begincodespacerange");
        sb.AppendLine("<0000> <FFFF>");  // 2-byte character codes
        sb.AppendLine("endcodespacerange");

        var sortedChars = characterToGlyphIndex.Keys.OrderBy(c => (int)c).ToList();

        if (sortedChars.Count > 0)
        {
            sb.AppendLine($"{sortedChars.Count} beginbfchar");

            foreach (var ch in sortedChars)
            {
                // Character code = glyph ID (2 bytes)
                ushort glyphId = characterToGlyphIndex[ch];
                int unicode = (int)ch;
                sb.AppendLine($"<{glyphId:X4}> <{unicode:X4}>");
            }

            sb.AppendLine("endbfchar");
        }

        sb.AppendLine("endcmap");
        sb.AppendLine("CMapName currentdict /CMap defineresource pop");
        sb.AppendLine("end");
        sb.AppendLine("end");

        return sb.ToString();
    }
}
