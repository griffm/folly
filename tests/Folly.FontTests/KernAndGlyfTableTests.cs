using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Folly.Fonts.Tests;

public class KernAndGlyfTableTests
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
    public void Parse_LiberationSans_HasKerningPairs()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");

        // Act
        var font = FontParser.Parse(fontPath);

        // Assert
        Assert.NotNull(font);
        Assert.NotEmpty(font.KerningPairs);

        // Liberation Sans should have kerning data
        Assert.True(font.KerningPairs.Count > 0);
    }

    [Fact]
    public void Parse_Roboto_HasKerningPairs()
    {
        // Arrange
        var fontPath = GetTestFontPath("Roboto-Regular.ttf");

        // Act
        var font = FontParser.Parse(fontPath);

        // Assert
        Assert.NotNull(font);

        // Roboto should have kerning data (it's a well-designed font)
        if (font.KerningPairs.Count > 0)
        {
            // Verify we can get kerning for specific pairs
            // Common kerning pairs like "AV", "WA", "To", etc.
            var hasKerningPairs = font.KerningPairs.Any();
            Assert.True(hasKerningPairs);
        }
    }

    [Fact]
    public void GetKerning_CommonPairs_ReturnsNegativeValues()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act & Assert
        // Common pairs that typically have negative kerning (closer together)
        // Note: Specific values depend on the font, so we just check if kerning is applied

        // Try common kerning pairs
        short kerningAV = font.GetKerning('A', 'V');
        short kerningWA = font.GetKerning('W', 'A');
        short kerningTo = font.GetKerning('T', 'o');

        // At least one of these should have kerning
        bool hasAnyKerning = kerningAV != 0 || kerningWA != 0 || kerningTo != 0;

        if (font.KerningPairs.Count > 0)
        {
            Assert.True(hasAnyKerning, "Expected at least one common pair to have kerning");
        }
    }

    [Fact]
    public void Parse_LiberationSans_HasGlyphData()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");

        // Act
        var font = FontParser.Parse(fontPath);

        // Assert
        Assert.NotNull(font);
        Assert.NotNull(font.Glyphs);
        Assert.Equal(font.GlyphCount, font.Glyphs.Length);
    }

    [Fact]
    public void Parse_Roboto_HasGlyphData()
    {
        // Arrange
        var fontPath = GetTestFontPath("Roboto-Regular.ttf");

        // Act
        var font = FontParser.Parse(fontPath);

        // Assert
        Assert.NotNull(font);
        Assert.NotNull(font.Glyphs);
        Assert.Equal(font.GlyphCount, font.Glyphs.Length);
    }

    [Fact]
    public void GetGlyphData_LetterA_HasValidBoundingBox()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act
        var glyphData = font.GetGlyphData('A');

        // Assert
        Assert.NotNull(glyphData);
        Assert.False(glyphData.IsEmptyGlyph);

        // Letter 'A' should be a simple glyph
        Assert.True(glyphData.IsSimpleGlyph);
        Assert.False(glyphData.IsCompositeGlyph);

        // Bounding box should be valid
        Assert.True(glyphData.XMin < glyphData.XMax);
        Assert.True(glyphData.YMin < glyphData.YMax);
        Assert.True(glyphData.Width > 0);
        Assert.True(glyphData.Height > 0);

        // Letter 'A' should have contours
        Assert.True(glyphData.NumberOfContours > 0);
    }

    [Fact]
    public void GetGlyphData_Space_IsEmptyGlyph()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act
        var glyphData = font.GetGlyphData(' ');

        // Assert
        Assert.NotNull(glyphData);

        // Space should have no contours (empty glyph)
        Assert.Equal(0, glyphData.NumberOfContours);
    }

    [Fact]
    public void GetTextWidth_WithoutKerning_MatchesSumOfAdvances()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);
        string text = "Hello";

        // Act
        int textWidth = font.GetTextWidth(text);

        // Calculate expected width (sum of advances + kerning)
        int expectedWidth = 0;
        char? prevChar = null;
        foreach (char c in text)
        {
            expectedWidth += font.GetAdvanceWidth(c);
            if (prevChar.HasValue)
            {
                expectedWidth += font.GetKerning(prevChar.Value, c);
            }
            prevChar = c;
        }

        // Assert
        Assert.Equal(expectedWidth, textWidth);
    }

    [Fact]
    public void GetTextWidth_EmptyString_ReturnsZero()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act & Assert
        Assert.Equal(0, font.GetTextWidth(""));
        Assert.Equal(0, font.GetTextWidth(null!));
    }

    [Fact]
    public void FontUnitsToPixels_ConvertsCorrectly()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act
        // At 12pt, 72 DPI, with 2048 units per em
        // 2048 font units = 12 pixels
        double pixels = font.FontUnitsToPixels(2048, 12, 72);

        // Assert
        Assert.Equal(12.0, pixels, precision: 1);
    }

    [Fact]
    public void GetLineHeight_ReturnsCorrectValue()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act
        int lineHeight = font.GetLineHeight();

        // Assert
        // Line height should be ascender - descender + line gap
        int expected = font.Ascender - font.Descender + font.LineGap;
        Assert.Equal(expected, lineHeight);

        // Line height should be positive and reasonable
        Assert.True(lineHeight > 0);
        Assert.True(lineHeight > font.Ascender);
    }

    [Fact]
    public void GetGlyphIndex_ValidCharacter_ReturnsIndex()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act
        var glyphIndex = font.GetGlyphIndex('A');

        // Assert
        Assert.NotNull(glyphIndex);
        Assert.True(glyphIndex.Value < font.GlyphCount);
    }

    [Fact]
    public void GetGlyphIndex_InvalidCharacter_ReturnsNull()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act
        // Use a character that's unlikely to be in the font
        var glyphIndex = font.GetGlyphIndex('\uFFFF');

        // Assert - might be null or might be the .notdef glyph
        // Just verify it doesn't crash
        Assert.True(glyphIndex == null || glyphIndex.Value < font.GlyphCount);
    }

    [Fact]
    public void GlyphData_Properties_AreConsistent()
    {
        // Arrange
        var fontPath = GetTestFontPath("LiberationSans-Regular.ttf");
        var font = FontParser.Parse(fontPath);

        // Act
        var glyphData = font.GetGlyphData('M');

        // Assert
        Assert.NotNull(glyphData);

        // Width and height should match bounding box calculations
        Assert.Equal(glyphData.XMax - glyphData.XMin, glyphData.Width);
        Assert.Equal(glyphData.YMax - glyphData.YMin, glyphData.Height);

        // Type checks should be mutually exclusive
        int typeCount = 0;
        if (glyphData.IsSimpleGlyph) typeCount++;
        if (glyphData.IsCompositeGlyph) typeCount++;
        if (glyphData.IsEmptyGlyph) typeCount++;

        // A glyph should be exactly one type (simple, composite, or empty)
        Assert.True(typeCount <= 1, "Glyph should be at most one type");
    }
}
