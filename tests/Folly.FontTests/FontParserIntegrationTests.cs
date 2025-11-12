using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Folly.Fonts.Tests;

public class FontParserIntegrationTests
{
    private static string GetTestFontPath(string fontName)
    {
        // Get the directory of the test assembly
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        // Navigate up to the test project root and find TestFonts directory
        var testFontsDir = Path.Combine(assemblyDir, "..", "..", "..", "TestFonts");
        var fontPath = Path.Combine(testFontsDir, fontName);

        if (!File.Exists(fontPath))
        {
            throw new FileNotFoundException($"Test font not found: {fontPath}");
        }

        return fontPath;
    }

    [Fact]
    public void Parse_LiberationSans_SuccessfullyLoadsFont()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");

        // Act
        var font = FontParser.Parse(fontPath);

        // Assert
        Assert.NotNull(font);
        Assert.Equal("Liberation Sans", font.FamilyName);
        Assert.Equal("Regular", font.SubfamilyName);
        Assert.True(font.IsTrueType);
        Assert.True(font.UnitsPerEm > 0);
        Assert.True(font.GlyphCount > 0);
        Assert.NotEmpty(font.CharacterToGlyphIndex);
        Assert.NotEmpty(font.GlyphAdvanceWidths);
    }

    [Fact]
    public void Parse_Roboto_SuccessfullyLoadsFont()
    {
        // Arrange
        var fontPath = GetTestFontPath("Roboto-Regular.ttf");

        // Act
        var font = FontParser.Parse(fontPath);

        // Assert
        Assert.NotNull(font);
        Assert.Equal("Roboto", font.FamilyName);
        Assert.Equal("Regular", font.SubfamilyName);
        Assert.True(font.IsTrueType);
        Assert.True(font.UnitsPerEm > 0);
        Assert.True(font.GlyphCount > 0);
        Assert.NotEmpty(font.CharacterToGlyphIndex);
        Assert.NotEmpty(font.GlyphAdvanceWidths);
    }

    [Fact]
    public void Parse_LiberationSans_HasCorrectMetrics()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");

        // Act
        var font = FontParser.Parse(fontPath);

        // Assert
        Assert.Equal(2048, font.UnitsPerEm);
        Assert.True(font.Ascender > 0);
        Assert.True(font.Descender < 0); // Descender is typically negative
        Assert.NotNull(font.OS2);
        Assert.NotNull(font.Post);
    }

    [Fact]
    public void Parse_LiberationSans_CanMapBasicCharacters()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act & Assert
        // Test basic ASCII characters
        Assert.True(font.HasCharacter('A'));
        Assert.True(font.HasCharacter('a'));
        Assert.True(font.HasCharacter('0'));
        Assert.True(font.HasCharacter(' '));
        Assert.True(font.HasCharacter('!'));

        // Get glyph indices
        Assert.True(font.CharacterToGlyphIndex.ContainsKey('A'));
        Assert.True(font.CharacterToGlyphIndex.ContainsKey('a'));

        // Get advance widths
        ushort widthA = font.GetAdvanceWidth('A');
        ushort widtha = font.GetAdvanceWidth('a');

        Assert.True(widthA > 0);
        Assert.True(widtha > 0);

        // Capital letters are typically wider than lowercase (not always, but for these letters)
        // Just verify they're different widths
        Assert.NotEqual(widthA, widtha);
    }

    [Fact]
    public void Parse_Roboto_SupportsUnicodeCharacters()
    {
        // Arrange
        var fontPath = GetTestFontPath("Roboto-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act & Assert
        // Test various Unicode characters
        Assert.True(font.HasCharacter('A'));
        Assert.True(font.HasCharacter('€')); // Euro sign
        Assert.True(font.HasCharacter('©')); // Copyright symbol

        // Roboto has good Unicode coverage
        Assert.True(font.CharacterToGlyphIndex.Count > 1000);
    }

    [Fact]
    public void Parse_LiberationSans_HasValidFontBoundingBox()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");

        // Act
        var font = FontParser.Parse(fontPath);

        // Assert
        Assert.True(font.XMin < font.XMax);
        Assert.True(font.YMin < font.YMax);

        // Bounding box should be reasonable for a typical font
        Assert.True(font.XMin < 0); // Typically slightly negative
        Assert.True(font.XMax > 0);
        Assert.True(font.YMin < 0);
        Assert.True(font.YMax > 0);
    }

    [Fact]
    public void Parse_LiberationSans_HasGlyphData()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");

        // Act
        var font = FontParser.Parse(fontPath);

        // Assert
        Assert.NotNull(font.GlyphOffsets);
        Assert.Equal(font.GlyphCount + 1, font.GlyphOffsets.Length);

        // Glyph offsets should be monotonically increasing
        for (int i = 0; i < font.GlyphOffsets.Length - 1; i++)
        {
            Assert.True(font.GlyphOffsets[i] <= font.GlyphOffsets[i + 1]);
        }
    }

    [Fact]
    public void Parse_Roboto_HasPostScriptInformation()
    {
        // Arrange
        var fontPath = GetTestFontPath("Roboto-Regular.ttf");

        // Act
        var font = FontParser.Parse(fontPath);

        // Assert
        Assert.NotNull(font.Post);

        // Roboto Regular should have zero italic angle
        Assert.Equal(0.0, font.Post.ItalicAngle, precision: 1);

        // Should not be monospaced
        Assert.False(font.Post.IsMonospaced);

        // Should have underline position and thickness
        Assert.True(font.Post.UnderlinePosition != 0);
        Assert.True(font.Post.UnderlineThickness > 0);
    }

    [Fact]
    public void IsValidFontFile_ValidTrueTypeFont_ReturnsTrue()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");

        // Act
        bool isValid = FontParser.IsValidFontFile(fontPath);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValidFontFile_InvalidFile_ReturnsFalse()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "This is not a font file");

        try
        {
            // Act
            bool isValid = FontParser.IsValidFontFile(tempFile);

            // Assert
            Assert.False(isValid);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_LiberationSans_HasOS2Table()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");

        // Act
        var font = FontParser.Parse(fontPath);

        // Assert
        Assert.NotNull(font.OS2);
        Assert.True(font.OS2.WeightClass > 0);
        Assert.True(font.OS2.WidthClass > 0);
        Assert.True(font.OS2.WinAscent > 0);
        Assert.True(font.OS2.WinDescent > 0);
    }
}
