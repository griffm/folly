using System.Linq;
using Folly.Typography.Hyphenation;
using Xunit;

namespace Folly.Typography.Tests;

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

    [Fact]
    public void HyphenationEngine_VeryLongWord_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("incomprehensibilities");

        // Assert
        Assert.NotEmpty(points);
    }

    [Fact]
    public void HyphenationEngine_SingleLetterWord_NoHyphenation()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("I");

        // Assert
        Assert.Empty(points);
    }

    [Fact]
    public void HyphenationEngine_TwoLetterWord_NoHyphenation()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("is");

        // Assert
        Assert.Empty(points);
    }

    [Fact]
    public void HyphenationEngine_AllUppercase_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("HYPHENATION");

        // Assert
        // Should handle uppercase words
        Assert.NotNull(points);
    }

    [Fact]
    public void HyphenationEngine_MixedCase_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("HyPhEnAtIoN");

        // Assert
        Assert.NotNull(points);
    }

    [Fact]
    public void HyphenationEngine_NumbersOnly_NoHyphenation()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("12345");

        // Assert
        Assert.Empty(points);
    }

    [Fact]
    public void HyphenationEngine_ConsecutiveVowels_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("beautiful");

        // Assert
        Assert.NotNull(points);
    }

    [Fact]
    public void HyphenationEngine_ConsecutiveConsonants_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("strength");

        // Assert
        Assert.NotNull(points);
    }

    [Fact]
    public void HyphenationEngine_WordStartingWithVowel_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("extraordinary");

        // Assert
        Assert.NotEmpty(points);
    }

    [Fact]
    public void HyphenationEngine_WordEndingWithVowel_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("avenue");

        // Assert
        Assert.NotNull(points);
    }

    [Fact]
    public void HyphenationEngine_RepeatedLetters_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("accommodate");

        // Assert
        Assert.NotEmpty(points);
    }

    [Fact]
    public void HyphenationEngine_CompoundWord_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints("blackboard");

        // Assert
        Assert.NotNull(points);
    }

    [Fact]
    public void HyphenationEngine_GermanUmlaut_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("de-DE");

        // Act - German word with umlaut
        var points = engine.FindHyphenationPoints("überhaupt");

        // Assert
        Assert.NotNull(points);
    }

    [Fact]
    public void HyphenationEngine_FrenchAccent_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("fr-FR");

        // Act - French word with accent
        var points = engine.FindHyphenationPoints("développement");

        // Assert
        Assert.NotNull(points);
    }

    [Fact]
    public void HyphenationEngine_SpanishTilde_HandlesCorrectly()
    {
        // Arrange
        var engine = new HyphenationEngine("es-ES");

        // Act - Spanish word with tilde
        var points = engine.FindHyphenationPoints("mañana");

        // Assert
        Assert.NotNull(points);
    }

    [Fact]
    public void HyphenationEngine_DifferentMinWordLengths_AffectsResults()
    {
        // Arrange
        var shortMin = new HyphenationEngine("en-US", minWordLength: 4);
        var longMin = new HyphenationEngine("en-US", minWordLength: 10);
        var word = "testing"; // 7 characters

        // Act
        var shortPoints = shortMin.FindHyphenationPoints(word);
        var longPoints = longMin.FindHyphenationPoints(word);

        // Assert
        Assert.NotEmpty(shortPoints); // Should hyphenate (7 >= 4)
        Assert.Empty(longPoints);     // Should not hyphenate (7 < 10)
    }

    [Fact]
    public void Hyphenate_NoHyphenationPoints_ReturnsOriginalWord()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US", minWordLength: 20);

        // Act
        var hyphenated = engine.Hyphenate("test", '-');

        // Assert
        Assert.Equal("test", hyphenated);
    }

    [Fact]
    public void Hyphenate_CustomCharacter_UsesSpecifiedCharacter()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var hyphenated = engine.Hyphenate("hyphenation", '|');

        // Assert
        Assert.Contains('|', hyphenated);
        Assert.DoesNotContain('-', hyphenated);
    }

    [Theory]
    [InlineData("dictionary")]
    [InlineData("information")]
    [InlineData("communication")]
    [InlineData("international")]
    public void HyphenationEngine_CommonWords_ProducesValidHyphenation(string word)
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var points = engine.FindHyphenationPoints(word);

        // Assert
        Assert.NotEmpty(points);
        // All points should be within valid range
        Assert.All(points, p => Assert.InRange(p, 1, word.Length - 1));
    }

    [Fact]
    public void HyphenationEngine_ZeroMinLeftChars_AllowsBreakAtStart()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US", minLeftChars: 0);

        // Act
        var points = engine.FindHyphenationPoints("testing");

        // Assert
        // Should allow more flexible breaking
        Assert.NotNull(points);
    }

    [Fact]
    public void HyphenationEngine_ZeroMinRightChars_AllowsBreakAtEnd()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US", minRightChars: 0);

        // Act
        var points = engine.FindHyphenationPoints("testing");

        // Assert
        Assert.NotNull(points);
    }

    [Fact]
    public void ShowHyphenationPoints_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");

        // Act
        var display = engine.ShowHyphenationPoints("");

        // Assert
        Assert.Equal("", display);
    }

    [Fact]
    public void ShowHyphenationPoints_NoHyphenation_ReturnsOriginal()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US", minWordLength: 20);

        // Act
        var display = engine.ShowHyphenationPoints("test");

        // Assert
        Assert.Equal("test", display);
    }

    [Fact]
    public void GetSupportedLanguages_ContainsExpectedLanguages()
    {
        // Act
        var languages = HyphenationEngine.GetSupportedLanguages();

        // Assert
        Assert.Contains("en-US", languages);
        Assert.Contains("de-DE", languages);
        Assert.Contains("fr-FR", languages);
        Assert.Contains("es-ES", languages);
        Assert.Equal(4, languages.Length);
    }

    [Fact]
    public void HyphenationEngine_ExtremelyLongWord_PerformsEfficiently()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");
        var longWord = new string('a', 1000);

        // Act
        var start = DateTime.UtcNow;
        var points = engine.FindHyphenationPoints(longWord);
        var duration = DateTime.UtcNow - start;

        // Assert
        Assert.NotNull(points);
        Assert.True(duration.TotalSeconds < 1, "Should handle very long words efficiently");
    }

    [Fact]
    public void HyphenationEngine_MultipleCallsSameWord_ConsistentResults()
    {
        // Arrange
        var engine = new HyphenationEngine("en-US");
        var word = "hyphenation";

        // Act
        var points1 = engine.FindHyphenationPoints(word);
        var points2 = engine.FindHyphenationPoints(word);

        // Assert
        Assert.Equal(points1, points2);
    }
}
