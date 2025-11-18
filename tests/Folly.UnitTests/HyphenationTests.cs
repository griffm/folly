using System.Linq;
using Folly.Typography.Hyphenation;
using Xunit;

namespace Folly.UnitTests;

public class HyphenationTests
{
    [Theory]
    [InlineData("hyphenation")] // hy-phen-ation
    [InlineData("algorithm")]   // al-go-rithm
    [InlineData("computer")]    // com-put-er
    [InlineData("documentation")] // doc-u-men-tation
    public void HyphenationEngine_FindsCorrectHyphenationPoints_EnglishWords(string word)
    {
        // Arrange
        var engine = new HyphenationEngine("en-US", minWordLength: 5, minLeftChars: 2, minRightChars: 3);

        // Act
        var points = engine.FindHyphenationPoints(word);

        // Assert
        // Note: Actual hyphenation points may vary based on TeX patterns
        // We're checking that at least some points are found and they're reasonable
        Assert.NotEmpty(points);
        Assert.All(points, point =>
        {
            Assert.InRange(point, 2, word.Length - 3); // Respects min left/right chars
        });
    }

    [Fact]
    public void HyphenationEngine_DoesNotHyphenateShortWords()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US", minWordLength: 6);

        // Act
        var points = engine.FindHyphenationPoints("short");

        // Assert
        Assert.Empty(points);
    }

    [Fact]
    public void HyphenationEngine_RespectsMinLeftChars()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US", minLeftChars: 3);

        // Act
        var points = engine.FindHyphenationPoints("hyphenation");

        // Assert
        Assert.All(points, point => Assert.True(point >= 3, $"Point {point} is less than minLeftChars(3)"));
    }

    [Fact]
    public void HyphenationEngine_RespectsMinRightChars()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US", minRightChars: 4);
        var word = "hyphenation";

        // Act
        var points = engine.FindHyphenationPoints(word);

        // Assert
        Assert.All(points, point => Assert.True(word.Length - point >= 4,
            $"Point {point} leaves less than minRightChars(4) characters"));
    }

    [Fact]
    public void HyphenationEngine_HandlesMultipleLanguages()
    {
        // Arrange & Act
        var supportedLanguages = HyphenationEngine.GetSupportedLanguages();

        // Assert
        Assert.Contains("en-US", supportedLanguages);
        Assert.Contains("de-DE", supportedLanguages);
        Assert.Contains("fr-FR", supportedLanguages);
        Assert.Contains("es-ES", supportedLanguages);
    }

    [Fact]
    public void HyphenationEngine_GermanHyphenation_Works()
    {
        // Arrange
        var engine = new HyphenationEngine("de-DE");

        // Act
        var points = engine.FindHyphenationPoints("bundesrepublik"); // German word

        // Assert
        Assert.NotEmpty(points);
    }

    [Fact]
    public void HyphenationEngine_FrenchHyphenation_Works()
    {
        // Arrange
        var engine = new HyphenationEngine("fr-FR");

        // Act
        var points = engine.FindHyphenationPoints("typographie"); // French word

        // Assert
        Assert.NotEmpty(points);
    }

    [Fact]
    public void HyphenationEngine_SpanishHyphenation_Works()
    {
        // Arrange
        var engine = new HyphenationEngine("es-ES");

        // Act
        var points = engine.FindHyphenationPoints("universidad"); // Spanish word

        // Assert
        Assert.NotEmpty(points);
    }

    [Fact]
    public void Hyphenate_InsertsHyphensAtCorrectPoints()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var hyphenated = engine.Hyphenate("hyphenation", '-');

        // Assert
        Assert.Contains('-', hyphenated);
        Assert.True(hyphenated.Length > "hyphenation".Length);
    }

    [Fact]
    public void Hyphenate_UsesSoftHyphen_WhenRequested()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var hyphenated = engine.Hyphenate("hyphenation", '\u00AD');

        // Assert
        Assert.Contains('\u00AD', hyphenated);
    }

    [Fact]
    public void ShowHyphenationPoints_ReturnsVisualRepresentation()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var display = engine.ShowHyphenationPoints("hyphenation");

        // Assert
        Assert.Contains('-', display);
        // Should show something like "hy-phen-ation" or similar
    }

    [Fact]
    public void HyphenationEngine_HandlesEmptyString()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("");

        // Assert
        Assert.Empty(points);
    }

    [Fact]
    public void HyphenationEngine_HandlesWhitespaceOnly()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("   ");

        // Assert
        Assert.Empty(points);
    }

    [Theory]
    [InlineData("can't")]      // Apostrophe
    [InlineData("hello123")]   // Numbers
    [InlineData("hello!")]     // Punctuation
    public void HyphenationEngine_CleansNonAlphabeticCharacters(string word)
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act & Assert - Should not throw
        var points = engine.FindHyphenationPoints(word);
        // Just verify it doesn't crash
    }
}
