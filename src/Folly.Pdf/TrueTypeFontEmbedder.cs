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
    /// Embeds a TrueType font subset in the PDF.
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
        short yMax)
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

        // Step 3: Write the ToUnicode CMap
        int toUnicodeId = WriteToUnicodeCMap(characterToGlyphIndex);

        // Step 4: Write the font dictionary (TrueType simple font)
        int fontDictId = WriteTrueTypeFontDictionary(
            fontName,
            fontDescriptorId,
            toUnicodeId,
            characterToGlyphIndex);

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
    /// Maps character codes (0-255) to Unicode values.
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
        sb.AppendLine("<00> <FF>");
        sb.AppendLine("endcodespacerange");

        // Build character code to Unicode mappings
        var sortedChars = characterToGlyphIndex.Keys.OrderBy(c => (int)c).ToList();

        if (sortedChars.Count > 0)
        {
            sb.AppendLine($"{sortedChars.Count} beginbfchar");

            foreach (var ch in sortedChars)
            {
                // Character code (0-255)
                byte charCode = (byte)((int)ch % 256);

                // Unicode value
                int unicode = (int)ch;

                sb.AppendLine($"<{charCode:X2}> <{unicode:X4}>");
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
    /// Writes a TrueType font dictionary (simple font with standard encoding).
    /// </summary>
    private int WriteTrueTypeFontDictionary(
        string fontName,
        int fontDescriptorId,
        int toUnicodeId,
        Dictionary<char, ushort> characterToGlyphIndex)
    {
        var fontDictId = _writer.BeginObject();
        _writer.WriteLine("<<");
        _writer.WriteLine("  /Type /Font");
        _writer.WriteLine("  /Subtype /TrueType");
        _writer.WriteLine($"  /BaseFont /{fontName}");
        _writer.WriteLine("  /Encoding /WinAnsiEncoding"); // Standard encoding
        _writer.WriteLine($"  /FontDescriptor {fontDescriptorId} 0 R");
        _writer.WriteLine($"  /ToUnicode {toUnicodeId} 0 R");

        // Write FirstChar and LastChar
        if (characterToGlyphIndex.Count > 0)
        {
            int firstChar = characterToGlyphIndex.Keys.Min(c => (int)c);
            int lastChar = characterToGlyphIndex.Keys.Max(c => (int)c);

            // Clamp to 0-255 for simple TrueType font
            firstChar = Math.Max(0, Math.Min(255, firstChar));
            lastChar = Math.Max(0, Math.Min(255, lastChar));

            _writer.WriteLine($"  /FirstChar {firstChar}");
            _writer.WriteLine($"  /LastChar {lastChar}");

            // Write Widths array (placeholder - would need actual glyph widths)
            _writer.Write("  /Widths [");
            for (int i = firstChar; i <= lastChar; i++)
            {
                // TODO: Get actual widths from font metrics
                _writer.Write("500 "); // Placeholder width
            }
            _writer.WriteLine("]");
        }

        _writer.WriteLine(">>");
        _writer.EndObject();

        return fontDictId;
    }

    /// <summary>
    /// Generates a ToUnicode CMap as a string.
    /// This is a helper for testing and debugging.
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
        sb.AppendLine("<00> <FF>");
        sb.AppendLine("endcodespacerange");

        var sortedChars = characterToGlyphIndex.Keys.OrderBy(c => (int)c).ToList();

        if (sortedChars.Count > 0)
        {
            sb.AppendLine($"{sortedChars.Count} beginbfchar");

            foreach (var ch in sortedChars)
            {
                byte charCode = (byte)((int)ch % 256);
                int unicode = (int)ch;
                sb.AppendLine($"<{charCode:X2}> <{unicode:X4}>");
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
