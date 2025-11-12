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
}
