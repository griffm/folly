using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Folly.Fonts.Tests;

public class FontSubsetterTests
{
    private static string GetTestFontPath(string fontName)
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var testFontsDir = Path.Combine(assemblyDir, "..", "..", "..", "TestFonts");
        var fontPath = Path.Combine(testFontsDir, fontName);

        if (!File.Exists(fontPath))
        {
            throw new FileNotFoundException($"Test font not found: {fontPath}");
        }

        return fontPath;
    }

    [Fact]
    public void CreateSubset_WithBasicCharacters_CreatesValidSubset()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        var usedCharacters = new HashSet<char> { 'H', 'e', 'l', 'o', ' ', 'W', 'r', 'd' };

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);

        // Assert
        Assert.NotNull(subsetData);
        Assert.NotEmpty(subsetData);

        // Verify it's a valid TrueType font by checking the signature
        Assert.True(subsetData.Length >= 12, "Subset data should be at least 12 bytes (offset table)");

        // Check sfntVersion (0x00010000 for TrueType)
        uint sfntVersion = ((uint)subsetData[0] << 24)
                         | ((uint)subsetData[1] << 16)
                         | ((uint)subsetData[2] << 8)
                         | subsetData[3];
        Assert.Equal(0x00010000u, sfntVersion);
    }

    [Fact]
    public void CreateSubset_ReducesGlyphCount()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        var usedCharacters = new HashSet<char> { 'A', 'B', 'C' };

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);

        // Assert
        // Parse the subset font to verify it has fewer glyphs
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        // Subset should have: .notdef + 3 characters = 4 glyphs
        Assert.True(subsetFont.GlyphCount <= 4, $"Expected 4 glyphs or fewer, but got {subsetFont.GlyphCount}");
        Assert.True(subsetFont.GlyphCount < font.GlyphCount, "Subset should have fewer glyphs than original");
    }

    [Fact]
    public void CreateSubset_PreservesCharacterMapping()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        var usedCharacters = new HashSet<char> { 'X', 'Y', 'Z' };

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);

        // Parse the subset
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        // Assert - all used characters should be present in the subset
        foreach (char c in usedCharacters)
        {
            Assert.True(subsetFont.HasCharacter(c), $"Character '{c}' should be in the subset");
        }
    }

    [Fact]
    public void CreateSubset_AlwaysIncludesNotDefGlyph()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        var usedCharacters = new HashSet<char> { 'A' };

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        // Assert - .notdef is always glyph 0 and should be present
        Assert.True(subsetFont.GlyphCount >= 2, "Subset should have at least .notdef + one character");
    }

    [Fact]
    public void CreateSubset_PreservesMetrics()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        var usedCharacters = new HashSet<char> { 'M' };

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        // Assert - basic metrics should be preserved
        Assert.Equal(font.UnitsPerEm, subsetFont.UnitsPerEm);
        Assert.Equal(font.Ascender, subsetFont.Ascender);
        Assert.Equal(font.Descender, subsetFont.Descender);
    }

    [Fact]
    public void CreateSubset_PreservesFontNames()
    {
        // Arrange
        var fontPath = GetTestFontPath("Roboto-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        var usedCharacters = new HashSet<char> { 'T', 'e', 's', 't' };

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        // Assert
        Assert.Equal(font.FamilyName, subsetFont.FamilyName);
        Assert.Equal(font.SubfamilyName, subsetFont.SubfamilyName);

        // PostScript name should have a subset prefix
        Assert.Contains("+", subsetFont.PostScriptName);
    }

    [Fact]
    public void CreateSubset_WithEmptyCharacterSet_ThrowsException()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        var usedCharacters = new HashSet<char>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            FontSubsetter.CreateSubset(font, usedCharacters));
    }

    [Fact]
    public void CreateSubset_WithNullCharacterSet_ThrowsException()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            FontSubsetter.CreateSubset(font, null!));
    }

    [Fact]
    public void CreateSubset_WithNullFont_ThrowsException()
    {
        // Arrange
        var usedCharacters = new HashSet<char> { 'A' };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FontSubsetter.CreateSubset(null!, usedCharacters));
    }

    [Fact]
    public void CreateSubset_WithComplexText_CreatesValidSubset()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        var text = "The quick brown fox jumps over the lazy dog.";
        var usedCharacters = new HashSet<char>(text);

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        // Assert - all characters from the text should be present
        foreach (char c in text)
        {
            if (c != ' ')
            {
                Assert.True(subsetFont.HasCharacter(c),
                    $"Character '{c}' should be in the subset");
            }
        }
    }

    [Fact]
    public void CreateSubset_PreservesAdvanceWidths()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        char testChar = 'W';
        var usedCharacters = new HashSet<char> { testChar };

        // Get advance width from original font
        ushort originalWidth = font.GetAdvanceWidth(testChar);

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        // Assert - advance width should be the same in subset
        ushort subsetWidth = subsetFont.GetAdvanceWidth(testChar);
        Assert.Equal(originalWidth, subsetWidth);
    }

    [Fact]
    public void CreateSubset_CanBeReparsed()
    {
        // Arrange
        var fontPath = GetTestFontPath("Roboto-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        var usedCharacters = new HashSet<char> { 'F', 'o', 'l', 'y' };

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);

        // Assert - should be able to parse the subset without errors
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        Assert.NotNull(subsetFont);
        Assert.True(subsetFont.GlyphCount > 0);
        Assert.NotEmpty(subsetFont.CharacterToGlyphIndex);
    }

    [Fact]
    public void CreateSubset_GeneratesUniqueSubsetName()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        var usedCharacters = new HashSet<char> { 'A' };

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        // Assert - PostScript name should have a 6-character prefix + "+"
        var parts = subsetFont.PostScriptName.Split('+');
        Assert.Equal(2, parts.Length);
        Assert.Equal(6, parts[0].Length);
        Assert.All(parts[0], c => Assert.True(char.IsLetter(c) && char.IsUpper(c)));
    }

    [Fact]
    public void CreateSubset_RemapsKerningPairs()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Use characters that have kerning pairs in LiberationSans (e.g., "AV", "WA", "To")
        var usedCharacters = new HashSet<char> { 'A', 'V', 'W', 'T', 'o' };

        // Get original kerning values for reference
        var origAGlyphId = font.GetGlyphIndex('A');
        var origVGlyphId = font.GetGlyphIndex('V');

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        // Assert - subset should have kerning pairs
        Assert.NotEmpty(subsetFont.KerningPairs);

        // The glyph indices will be different in the subset, but kerning pairs should still exist
        var subsetAGlyphId = subsetFont.GetGlyphIndex('A');
        var subsetVGlyphId = subsetFont.GetGlyphIndex('V');

        Assert.True(subsetAGlyphId.HasValue, "Character 'A' should be in subset");
        Assert.True(subsetVGlyphId.HasValue, "Character 'V' should be in subset");

        // If the original font had a kerning pair for A-V, the subset should too
        // (using the new glyph indices)
        if (origAGlyphId.HasValue && origVGlyphId.HasValue)
        {
            var origPair = (origAGlyphId.Value, origVGlyphId.Value);
            var subsetPair = (subsetAGlyphId.Value, subsetVGlyphId.Value);

            if (font.KerningPairs.TryGetValue(origPair, out short origKerning))
            {
                // The subset should have the same kerning value for the remapped pair
                Assert.True(subsetFont.KerningPairs.TryGetValue(subsetPair, out short subsetKerning),
                    $"Subset should have kerning pair for A-V (glyphs {subsetPair.Item1}-{subsetPair.Item2})");
                Assert.Equal(origKerning, subsetKerning);
            }
        }
    }

    [Fact]
    public void CreateSubset_ExcludesKerningPairsForMissingGlyphs()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Only include 'A' and 'B' - this means kerning pairs like 'AV' or 'To' won't be in the subset
        var usedCharacters = new HashSet<char> { 'A', 'B' };

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        // Assert - Check that kerning pairs only exist for glyphs in the subset
        var subsetAGlyphId = subsetFont.GetGlyphIndex('A');
        var subsetBGlyphId = subsetFont.GetGlyphIndex('B');

        foreach (var kvp in subsetFont.KerningPairs)
        {
            var (left, right) = kvp.Key;

            // Both glyph IDs should be valid (within the subset's glyph count)
            Assert.True(left < subsetFont.GlyphCount,
                $"Left glyph ID {left} should be less than glyph count {subsetFont.GlyphCount}");
            Assert.True(right < subsetFont.GlyphCount,
                $"Right glyph ID {right} should be less than glyph count {subsetFont.GlyphCount}");
        }
    }

    [Fact]
    public void CreateSubset_PreservesKerningValues()
    {
        // Arrange
        var fontPath = GetTestFontPath("Roboto-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Use common kerning pairs
        var usedCharacters = new HashSet<char> { 'T', 'o', 'Y', 'A', 'V', 'W' };

        // Collect original kerning information
        var originalKerningByCharPair = new Dictionary<(char, char), short>();
        foreach (var kvp in font.KerningPairs)
        {
            var (leftGlyph, rightGlyph) = kvp.Key;

            // Find which characters map to these glyphs
            foreach (var c1 in usedCharacters)
            {
                var g1 = font.GetGlyphIndex(c1);
                if (g1 == leftGlyph)
                {
                    foreach (var c2 in usedCharacters)
                    {
                        var g2 = font.GetGlyphIndex(c2);
                        if (g2 == rightGlyph)
                        {
                            originalKerningByCharPair[(c1, c2)] = kvp.Value;
                        }
                    }
                }
            }
        }

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        // Assert - Verify kerning values are preserved
        foreach (var kvp in originalKerningByCharPair)
        {
            var (c1, c2) = kvp.Key;
            var originalKerning = kvp.Value;

            var subsetG1 = subsetFont.GetGlyphIndex(c1);
            var subsetG2 = subsetFont.GetGlyphIndex(c2);

            Assert.True(subsetG1.HasValue, $"Character '{c1}' should be in subset");
            Assert.True(subsetG2.HasValue, $"Character '{c2}' should be in subset");

            var subsetPair = (subsetG1.Value, subsetG2.Value);

            if (subsetFont.KerningPairs.TryGetValue(subsetPair, out short subsetKerning))
            {
                Assert.Equal(originalKerning, subsetKerning);
            }
        }
    }

    [Fact]
    public void CreateSubset_WithKerningPairs_ProducesValidFont()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Use the classic kerning test string
        var text = "AV WA To Ty";
        var usedCharacters = new HashSet<char>(text);

        // Act
        byte[] subsetData = FontSubsetter.CreateSubset(font, usedCharacters);

        // Assert - Verify it's a valid TrueType font
        Assert.NotNull(subsetData);
        Assert.True(subsetData.Length > 0);

        // Parse the subset to ensure it's valid
        using var ms = new MemoryStream(subsetData);
        var subsetFont = FontParser.Parse(ms);

        Assert.NotNull(subsetFont);
        Assert.True(subsetFont.GlyphCount > 0);

        // Verify all characters are present
        foreach (char c in usedCharacters)
        {
            if (c != ' ')
            {
                Assert.True(subsetFont.HasCharacter(c), $"Character '{c}' should be in subset");
            }
        }

        // The subset should have some kerning pairs (LiberationSans has kerning)
        Assert.NotEmpty(subsetFont.KerningPairs);
    }
}
