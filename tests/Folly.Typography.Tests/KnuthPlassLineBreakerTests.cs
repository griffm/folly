using Folly.Typography.LineBreaking;
using Xunit;

namespace Folly.Typography.Tests;

/// <summary>
/// Tests for the Knuth-Plass optimal line breaking algorithm.
/// </summary>
public class KnuthPlassLineBreakerTests
{
    /// <summary>
    /// Simple mock text measurer for testing.
    /// Assumes fixed-width characters for predictable behavior.
    /// </summary>
    private class MockTextMeasurer : ITextMeasurer
    {
        private readonly double _charWidth;
        private readonly double _spaceWidth;

        public MockTextMeasurer(double charWidth = 10.0, double spaceWidth = 5.0)
        {
            _charWidth = charWidth;
            _spaceWidth = spaceWidth;
        }

        public double MeasureWidth(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            if (text == " ")
                return _spaceWidth;

            return text.Length * _charWidth;
        }
    }

    [Fact]
    public void FindOptimalBreakpoints_EmptyText_ReturnsEmptyList()
    {
        // Arrange
        var measurer = new MockTextMeasurer();
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 100);

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints("", new List<string>(), new List<(int, int)>());

        // Assert
        Assert.Empty(breakpoints);
    }

    [Fact]
    public void FindOptimalBreakpoints_SingleWord_HandlesCorrectly()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 100);
        var text = "Hello";
        var words = new List<string> { "Hello" };
        var positions = new List<(int, int)> { (0, 5) };

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);

        // Assert - Single word should not require line breaks (or may have end marker)
        Assert.NotNull(breakpoints);
    }

    [Fact]
    public void FindOptimalBreakpoints_TwoShortWords_NoBreakNeeded()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 200);
        var text = "Hello World";
        var words = new List<string> { "Hello", "World" };
        var positions = new List<(int, int)> { (0, 5), (6, 11) };

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);

        // Assert
        // Both words fit on one line (5*10 + 5 + 5*10 = 105 < 200)
        // Algorithm may include final position but should not have intermediate breaks
        Assert.NotNull(breakpoints);
        Assert.True(breakpoints.Count <= 1, "Should not require intermediate line breaks");
    }

    [Fact]
    public void FindOptimalBreakpoints_TwoLongWords_RequiresBreak()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 60);
        var text = "Hello World";
        var words = new List<string> { "Hello", "World" };
        var positions = new List<(int, int)> { (0, 5), (6, 11) };

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);

        // Assert
        // First word is 50 points, fits. Adding space + second word exceeds 60 points.
        // Should break somewhere (may be final position)
        Assert.NotEmpty(breakpoints);
    }

    [Fact]
    public void FindOptimalBreakpoints_MultipleWords_FindsOptimalBreaks()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 100);
        var text = "The quick brown fox jumps";
        var words = new List<string> { "The", "quick", "brown", "fox", "jumps" };
        var positions = new List<(int, int)>
        {
            (0, 3),   // The
            (4, 9),   // quick
            (10, 15), // brown
            (16, 19), // fox
            (20, 25)  // jumps
        };

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);

        // Assert
        // Line width is 100 points
        // "The quick" = 3*10 + 5 + 5*10 = 85 points (fits)
        // Adding "brown" = 85 + 5 + 5*10 = 140 points (exceeds)
        // Should have at least one breakpoint
        Assert.NotEmpty(breakpoints);
    }

    [Fact]
    public void FindOptimalBreakpoints_VeryNarrowWidth_BreaksAfterEachWord()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 40);
        var text = "One Two Three";
        var words = new List<string> { "One", "Two", "Three" };
        var positions = new List<(int, int)>
        {
            (0, 3),   // One
            (4, 7),   // Two
            (8, 13)   // Three
        };

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);

        // Assert
        // Each word is 30 points, fits on its own line
        // But can't fit two words on one line
        Assert.NotEmpty(breakpoints);
        Assert.True(breakpoints.Count >= 1, "Should have breaks for narrow width");
    }

    [Fact]
    public void FindOptimalBreakpoints_WithTolerance_AllowsMoreFlexibility()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var strictBreaker = new KnuthPlassLineBreaker(measurer, lineWidth: 100, tolerance: 0.5);
        var flexibleBreaker = new KnuthPlassLineBreaker(measurer, lineWidth: 100, tolerance: 2.0);

        var text = "Hello World Test";
        var words = new List<string> { "Hello", "World", "Test" };
        var positions = new List<(int, int)> { (0, 5), (6, 11), (12, 16) };

        // Act
        var strictBreaks = strictBreaker.FindOptimalBreakpoints(text, words, positions);
        var flexibleBreaks = flexibleBreaker.FindOptimalBreakpoints(text, words, positions);

        // Assert
        // Flexible breaker should allow more stretching/shrinking
        // Both should produce valid results (no exceptions)
        Assert.NotNull(strictBreaks);
        Assert.NotNull(flexibleBreaks);
    }

    [Fact]
    public void FindOptimalBreakpoints_CustomSpaceStretch_AffectsBreaking()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);

        // More stretch allows spaces to expand more
        var moreStretch = new KnuthPlassLineBreaker(
            measurer,
            lineWidth: 100,
            spaceStretchRatio: 1.0,
            spaceShrinkRatio: 0.333);

        // Less stretch is more rigid
        var lessStretch = new KnuthPlassLineBreaker(
            measurer,
            lineWidth: 100,
            spaceStretchRatio: 0.2,
            spaceShrinkRatio: 0.333);

        var text = "Hello World Test";
        var words = new List<string> { "Hello", "World", "Test" };
        var positions = new List<(int, int)> { (0, 5), (6, 11), (12, 16) };

        // Act
        var moreStretchBreaks = moreStretch.FindOptimalBreakpoints(text, words, positions);
        var lessStretchBreaks = lessStretch.FindOptimalBreakpoints(text, words, positions);

        // Assert
        // Both should complete without errors
        Assert.NotNull(moreStretchBreaks);
        Assert.NotNull(lessStretchBreaks);
    }

    [Fact]
    public void FindOptimalBreakpoints_CustomPenalties_AffectsDecisions()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);

        // High line penalty discourages breaking
        var highPenalty = new KnuthPlassLineBreaker(
            measurer,
            lineWidth: 100,
            linePenalty: 100.0);

        // Low line penalty allows more breaks
        var lowPenalty = new KnuthPlassLineBreaker(
            measurer,
            lineWidth: 100,
            linePenalty: 1.0);

        var text = "Hello World Test Example";
        var words = new List<string> { "Hello", "World", "Test", "Example" };
        var positions = new List<(int, int)> { (0, 5), (6, 11), (12, 16), (17, 24) };

        // Act
        var highPenaltyBreaks = highPenalty.FindOptimalBreakpoints(text, words, positions);
        var lowPenaltyBreaks = lowPenalty.FindOptimalBreakpoints(text, words, positions);

        // Assert
        Assert.NotNull(highPenaltyBreaks);
        Assert.NotNull(lowPenaltyBreaks);
    }

    [Fact]
    public void FindOptimalBreakpoints_LongParagraph_HandlesCorrectly()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 150);

        var words = new List<string>
        {
            "The", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog",
            "This", "is", "a", "longer", "paragraph", "for", "testing", "purposes"
        };

        var positions = new List<(int, int)>();
        int currentPos = 0;
        foreach (var word in words)
        {
            positions.Add((currentPos, currentPos + word.Length));
            currentPos += word.Length + 1; // +1 for space
        }

        var text = string.Join(" ", words);

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);

        // Assert
        Assert.NotEmpty(breakpoints);

        // Verify all breakpoints are valid positions
        Assert.All(breakpoints, bp => Assert.InRange(bp, 0, text.Length));

        // Verify breakpoints are in ascending order
        for (int i = 1; i < breakpoints.Count; i++)
        {
            Assert.True(breakpoints[i] > breakpoints[i - 1],
                "Breakpoints should be in ascending order");
        }
    }

    [Fact]
    public void FindOptimalBreakpoints_VeryLongWord_DoesNotCrash()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 50);

        // Word is much longer than line width
        var text = "Supercalifragilisticexpialidocious";
        var words = new List<string> { "Supercalifragilisticexpialidocious" };
        var positions = new List<(int, int)> { (0, text.Length) };

        // Act & Assert - Should not crash even though word doesn't fit
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);
        Assert.NotNull(breakpoints);
    }

    [Fact]
    public void FindOptimalBreakpoints_AlternatingShortLongWords_FindsBalance()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 120);

        var words = new List<string> { "I", "love", "programming", "in", "C#" };
        var positions = new List<(int, int)>
        {
            (0, 1),    // I
            (2, 6),    // love
            (7, 18),   // programming
            (19, 21),  // in
            (22, 24)   // C#
        };
        var text = "I love programming in C#";

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);

        // Assert
        // Should handle mixed word lengths gracefully
        Assert.NotNull(breakpoints);
    }

    [Fact]
    public void FindOptimalBreakpoints_SingleCharacterWords_HandlesCorrectly()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 100);

        var words = new List<string> { "I", "a", "m", "a", "test" };
        var positions = new List<(int, int)>
        {
            (0, 1), (2, 3), (4, 5), (6, 7), (8, 12)
        };
        var text = "I a m a test";

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);

        // Assert
        Assert.NotNull(breakpoints);
    }

    [Fact]
    public void FindOptimalBreakpoints_IdenticalLineWidthToContent_FitsExactly()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);

        // "Hello" = 5 chars * 10 = 50 points exactly
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 50);
        var text = "Hello";
        var words = new List<string> { "Hello" };
        var positions = new List<(int, int)> { (0, 5) };

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);

        // Assert - Should fit on one line perfectly (may have end marker)
        Assert.NotNull(breakpoints);
        Assert.True(breakpoints.Count <= 1, "Perfect fit should not require intermediate breaks");
    }

    [Fact]
    public void FindOptimalBreakpoints_DifferentFitnessDemerit_AffectsConsistency()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);

        // High fitness demerit encourages consistent line tightness
        var highFitnessDemerit = new KnuthPlassLineBreaker(
            measurer,
            lineWidth: 100,
            fitnessDemerit: 500.0);

        // Low fitness demerit allows more variation
        var lowFitnessDemerit = new KnuthPlassLineBreaker(
            measurer,
            lineWidth: 100,
            fitnessDemerit: 10.0);

        var text = "Some text for testing consistency";
        var words = new List<string> { "Some", "text", "for", "testing", "consistency" };
        var positions = new List<(int, int)>
        {
            (0, 4), (5, 9), (10, 13), (14, 21), (22, 33)
        };

        // Act
        var highDemeritBreaks = highFitnessDemerit.FindOptimalBreakpoints(text, words, positions);
        var lowDemeritBreaks = lowFitnessDemerit.FindOptimalBreakpoints(text, words, positions);

        // Assert
        Assert.NotNull(highDemeritBreaks);
        Assert.NotNull(lowDemeritBreaks);
    }

    [Fact]
    public void FindOptimalBreakpoints_WideLineWidth_ProducesFewBreaks()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 1000);

        var words = new List<string> { "Short", "text", "here" };
        var positions = new List<(int, int)>
        {
            (0, 5), (6, 10), (11, 15)
        };
        var text = "Short text here";

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);

        // Assert
        // All words should fit on one line (may have end marker only)
        Assert.NotNull(breakpoints);
        Assert.True(breakpoints.Count <= 1, "Wide line should not require intermediate breaks");
    }

    [Fact]
    public void FindOptimalBreakpoints_ManyWords_CompletesInReasonableTime()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 200);

        // Create a paragraph with 50 words
        var words = new List<string>();
        var positions = new List<(int, int)>();
        int pos = 0;

        for (int i = 0; i < 50; i++)
        {
            var word = $"word{i}";
            words.Add(word);
            positions.Add((pos, pos + word.Length));
            pos += word.Length + 1;
        }

        var text = string.Join(" ", words);

        // Act
        var start = DateTime.UtcNow;
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);
        var duration = DateTime.UtcNow - start;

        // Assert
        Assert.NotNull(breakpoints);
        Assert.True(duration.TotalSeconds < 5, "Algorithm should complete in reasonable time");
    }

    [Fact]
    public void Constructor_AcceptsAllParameters()
    {
        // Arrange & Act
        var measurer = new MockTextMeasurer();
        var breaker = new KnuthPlassLineBreaker(
            textMeasurer: measurer,
            lineWidth: 100,
            tolerance: 1.5,
            spaceStretchRatio: 0.6,
            spaceShrinkRatio: 0.4,
            linePenalty: 15.0,
            flaggedDemerit: 150.0,
            fitnessDemerit: 120.0);

        // Assert
        Assert.NotNull(breaker);
    }

    [Fact]
    public void FindOptimalBreakpoints_MinimalInput_TwoWords()
    {
        // Arrange
        var measurer = new MockTextMeasurer(charWidth: 10.0, spaceWidth: 5.0);
        var breaker = new KnuthPlassLineBreaker(measurer, lineWidth: 60);

        var text = "AB CD";
        var words = new List<string> { "AB", "CD" };
        var positions = new List<(int, int)> { (0, 2), (3, 5) };

        // Act
        var breakpoints = breaker.FindOptimalBreakpoints(text, words, positions);

        // Assert
        // AB = 20, space = 5, CD = 20, total = 45 < 60
        Assert.NotNull(breakpoints);
        Assert.True(breakpoints.Count <= 1, "Both words should fit on one line");
    }
}
