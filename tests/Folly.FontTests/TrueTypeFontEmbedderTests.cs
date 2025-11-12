using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Folly.Pdf;
using Xunit;

namespace Folly.Fonts.Tests;

public class TrueTypeFontEmbedderTests
{
    [Fact]
    public void GenerateToUnicodeCMap_WithBasicCharacters_CreatesValidCMap()
    {
        // Arrange
        var charToGlyph = new Dictionary<char, ushort>
        {
            { 'A', 1 },
            { 'B', 2 },
            { 'C', 3 }
        };

        // Act
        string cmap = TrueTypeFontEmbedder.GenerateToUnicodeCMapString(charToGlyph);

        // Assert
        Assert.NotNull(cmap);
        Assert.Contains("/CIDInit /ProcSet findresource begin", cmap);
        Assert.Contains("begincmap", cmap);
        Assert.Contains("endcmap", cmap);
        Assert.Contains("/CMapName /Adobe-Identity-UCS def", cmap);
    }

    [Fact]
    public void GenerateToUnicodeCMap_MapsCharactersToUnicode()
    {
        // Arrange
        var charToGlyph = new Dictionary<char, ushort>
        {
            { 'A', 1 },  // Unicode 0x0041
            { 'Z', 2 },  // Unicode 0x005A
        };

        // Act
        string cmap = TrueTypeFontEmbedder.GenerateToUnicodeCMapString(charToGlyph);

        // Assert
        // Check for character code to Unicode mappings
        // 'A' = char code 0x41, Unicode 0x0041
        Assert.Contains("<41> <0041>", cmap);
        // 'Z' = char code 0x5A, Unicode 0x005A
        Assert.Contains("<5A> <005A>", cmap);
    }

    [Fact]
    public void GenerateToUnicodeCMap_WithEmptyMap_CreatesValidCMapWithNoMappings()
    {
        // Arrange
        var charToGlyph = new Dictionary<char, ushort>();

        // Act
        string cmap = TrueTypeFontEmbedder.GenerateToUnicodeCMapString(charToGlyph);

        // Assert
        Assert.NotNull(cmap);
        Assert.Contains("begincmap", cmap);
        Assert.Contains("endcmap", cmap);
        // Should not contain beginbfchar since no characters
        Assert.DoesNotContain("beginbfchar", cmap);
    }

    [Fact]
    public void GenerateToUnicodeCMap_WithSpecialCharacters_CreatesValidMappings()
    {
        // Arrange
        var charToGlyph = new Dictionary<char, ushort>
        {
            { ' ', 1 },   // Space (0x0020)
            { '!', 2 },   // Exclamation (0x0021)
            { '@', 3 },   // At sign (0x0040)
            { '#', 4 },   // Hash (0x0023)
        };

        // Act
        string cmap = TrueTypeFontEmbedder.GenerateToUnicodeCMapString(charToGlyph);

        // Assert
        Assert.Contains("<20> <0020>", cmap); // Space
        Assert.Contains("<21> <0021>", cmap); // !
        Assert.Contains("<40> <0040>", cmap); // @
        Assert.Contains("<23> <0023>", cmap); // #
    }

    [Fact]
    public void GenerateToUnicodeCMap_WithHighUnicodeCharacters_CreatesValidMappings()
    {
        // Arrange
        var charToGlyph = new Dictionary<char, ushort>
        {
            { 'é', 1 },   // e-acute (0x00E9)
            { 'ñ', 2 },   // n-tilde (0x00F1)
            { '€', 3 },   // Euro sign (0x20AC)
        };

        // Act
        string cmap = TrueTypeFontEmbedder.GenerateToUnicodeCMapString(charToGlyph);

        // Assert
        // Character codes wrap to 0-255 (modulo 256)
        // But Unicode values remain correct
        Assert.Contains("<00E9>", cmap); // é Unicode value
        Assert.Contains("<00F1>", cmap); // ñ Unicode value
        Assert.Contains("<20AC>", cmap); // € Unicode value
    }

    [Fact]
    public void GenerateToUnicodeCMap_SortsCharactersCorrectly()
    {
        // Arrange
        var charToGlyph = new Dictionary<char, ushort>
        {
            { 'Z', 1 },
            { 'A', 2 },
            { 'M', 3 },
        };

        // Act
        string cmap = TrueTypeFontEmbedder.GenerateToUnicodeCMapString(charToGlyph);

        // Assert
        // Find positions of each mapping
        int posA = cmap.IndexOf("<41> <0041>");  // A
        int posM = cmap.IndexOf("<4D> <004D>");  // M
        int posZ = cmap.IndexOf("<5A> <005A>");  // Z

        // They should appear in sorted order: A, M, Z
        Assert.True(posA >= 0, "A mapping should be present");
        Assert.True(posM >= 0, "M mapping should be present");
        Assert.True(posZ >= 0, "Z mapping should be present");
        Assert.True(posA < posM, "A should come before M");
        Assert.True(posM < posZ, "M should come before Z");
    }

    [Fact]
    public void GenerateToUnicodeCMap_WithNumbersAndLetters_CreatesCompleteCMap()
    {
        // Arrange
        var charToGlyph = new Dictionary<char, ushort>
        {
            { '0', 1 },
            { '5', 2 },
            { '9', 3 },
            { 'a', 4 },
            { 'z', 5 },
        };

        // Act
        string cmap = TrueTypeFontEmbedder.GenerateToUnicodeCMapString(charToGlyph);

        // Assert
        Assert.Contains("<30> <0030>", cmap); // 0
        Assert.Contains("<35> <0035>", cmap); // 5
        Assert.Contains("<39> <0039>", cmap); // 9
        Assert.Contains("<61> <0061>", cmap); // a
        Assert.Contains("<7A> <007A>", cmap); // z
    }

    [Fact]
    public void GenerateToUnicodeCMap_CountsCharactersCorrectly()
    {
        // Arrange
        var charToGlyph = new Dictionary<char, ushort>
        {
            { 'A', 1 },
            { 'B', 2 },
            { 'C', 3 },
            { 'D', 4 },
            { 'E', 5 },
        };

        // Act
        string cmap = TrueTypeFontEmbedder.GenerateToUnicodeCMapString(charToGlyph);

        // Assert
        // Should have "5 beginbfchar" for 5 characters
        Assert.Contains("5 beginbfchar", cmap);
    }

    [Fact]
    public void GenerateToUnicodeCMap_HasCorrectStructure()
    {
        // Arrange
        var charToGlyph = new Dictionary<char, ushort>
        {
            { 'X', 1 }
        };

        // Act
        string cmap = TrueTypeFontEmbedder.GenerateToUnicodeCMapString(charToGlyph);

        // Assert
        var lines = cmap.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Check required structure elements in order
        Assert.Contains(lines, l => l.Contains("/ProcSet findresource begin"));
        Assert.Contains(lines, l => l.Contains("begincmap"));
        Assert.Contains(lines, l => l.Contains("/CIDSystemInfo"));
        Assert.Contains(lines, l => l.Contains("begincodespacerange"));
        Assert.Contains(lines, l => l.Contains("<00> <FF>"));
        Assert.Contains(lines, l => l.Contains("endcodespacerange"));
        Assert.Contains(lines, l => l.Contains("beginbfchar"));
        Assert.Contains(lines, l => l.Contains("endbfchar"));
        Assert.Contains(lines, l => l.Contains("endcmap"));
    }

    [Fact]
    public void GenerateToUnicodeCMap_WithFullAlphabet_CreatesAllMappings()
    {
        // Arrange
        var charToGlyph = new Dictionary<char, ushort>();
        ushort glyphIndex = 1;

        // Add all uppercase letters
        for (char c = 'A'; c <= 'Z'; c++)
        {
            charToGlyph[c] = glyphIndex++;
        }

        // Act
        string cmap = TrueTypeFontEmbedder.GenerateToUnicodeCMapString(charToGlyph);

        // Assert
        Assert.Contains("26 beginbfchar", cmap);  // 26 letters

        // Verify first and last letters
        Assert.Contains("<41> <0041>", cmap);  // A
        Assert.Contains("<5A> <005A>", cmap);  // Z
    }
}
