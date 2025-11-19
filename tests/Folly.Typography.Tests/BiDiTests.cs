using Folly.Typography.BiDi;
using Xunit;

namespace Folly.Typography.Tests;

/// <summary>
/// Tests for the Unicode Bidirectional Algorithm (UAX#9) implementation.
/// </summary>
public class BiDiTests
{
    [Fact]
    public void GetCharacterType_LatinLetters_ReturnsL()
    {
        // ASCII letters
        Assert.Equal(BidiCharacterType.L, UnicodeBidiData.GetCharacterType('A'));
        Assert.Equal(BidiCharacterType.L, UnicodeBidiData.GetCharacterType('z'));

        // Latin Extended
        Assert.Equal(BidiCharacterType.L, UnicodeBidiData.GetCharacterType('é'));
        Assert.Equal(BidiCharacterType.L, UnicodeBidiData.GetCharacterType('ñ'));
    }

    [Fact]
    public void GetCharacterType_ArabicLetters_ReturnsAL()
    {
        // Arabic letters (U+0600..U+06FF)
        Assert.Equal(BidiCharacterType.AL, UnicodeBidiData.GetCharacterType('\u0627')); // Arabic Letter Alef
        Assert.Equal(BidiCharacterType.AL, UnicodeBidiData.GetCharacterType('\u0628')); // Arabic Letter Beh
        Assert.Equal(BidiCharacterType.AL, UnicodeBidiData.GetCharacterType('\u062A')); // Arabic Letter Teh
    }

    [Fact]
    public void GetCharacterType_HebrewLetters_ReturnsR()
    {
        // Hebrew letters (U+0590..U+05FF)
        Assert.Equal(BidiCharacterType.R, UnicodeBidiData.GetCharacterType('\u05D0')); // Hebrew Letter Alef
        Assert.Equal(BidiCharacterType.R, UnicodeBidiData.GetCharacterType('\u05D1')); // Hebrew Letter Bet
        Assert.Equal(BidiCharacterType.R, UnicodeBidiData.GetCharacterType('\u05E9')); // Hebrew Letter Shin
    }

    [Fact]
    public void GetCharacterType_Digits_ReturnsEN()
    {
        // ASCII digits
        Assert.Equal(BidiCharacterType.EN, UnicodeBidiData.GetCharacterType('0'));
        Assert.Equal(BidiCharacterType.EN, UnicodeBidiData.GetCharacterType('5'));
        Assert.Equal(BidiCharacterType.EN, UnicodeBidiData.GetCharacterType('9'));
    }

    [Fact]
    public void GetCharacterType_ArabicIndicDigits_ReturnsAN()
    {
        // Arabic-Indic digits (U+0660..U+0669)
        Assert.Equal(BidiCharacterType.AN, UnicodeBidiData.GetCharacterType('\u0660')); // Arabic-Indic Digit Zero
        Assert.Equal(BidiCharacterType.AN, UnicodeBidiData.GetCharacterType('\u0665')); // Arabic-Indic Digit Five
    }

    [Fact]
    public void GetCharacterType_Whitespace_ReturnsWS()
    {
        Assert.Equal(BidiCharacterType.WS, UnicodeBidiData.GetCharacterType(' '));
        Assert.Equal(BidiCharacterType.WS, UnicodeBidiData.GetCharacterType('\u00A0')); // Non-breaking space
    }

    [Fact]
    public void GetCharacterType_ExplicitFormatting_ReturnsCorrectTypes()
    {
        Assert.Equal(BidiCharacterType.LRE, UnicodeBidiData.GetCharacterType('\u202A'));
        Assert.Equal(BidiCharacterType.RLE, UnicodeBidiData.GetCharacterType('\u202B'));
        Assert.Equal(BidiCharacterType.PDF, UnicodeBidiData.GetCharacterType('\u202C'));
        Assert.Equal(BidiCharacterType.LRO, UnicodeBidiData.GetCharacterType('\u202D'));
        Assert.Equal(BidiCharacterType.RLO, UnicodeBidiData.GetCharacterType('\u202E'));
    }

    [Fact]
    public void ReorderText_LTRText_NoChange()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "Hello World";
        var result = algorithm.ReorderText(text, 0); // LTR

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void ReorderText_RTLText_Reversed()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Hebrew: שלום (Shalom)
        var text = "\u05E9\u05DC\u05D5\u05DD";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Hebrew text should be reversed for display
        Assert.Equal("\u05DD\u05D5\u05DC\u05E9", result);
    }

    [Fact]
    public void ReorderText_ArabicText_Reversed()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Arabic: سلام (Salam)
        var text = "\u0633\u0644\u0627\u0645";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Arabic text should be reversed for display
        Assert.Equal("\u0645\u0627\u0644\u0633", result);
    }

    [Fact]
    public void ReorderText_MixedLTRInRTL_NumbersStayLTR()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Hebrew with numbers: שלום 123
        var text = "\u05E9\u05DC\u05D5\u05DD 123";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Numbers should remain LTR (not reversed)
        // Expected: "123 םולש" (123 followed by reversed Hebrew)
        Assert.Equal("123 \u05DD\u05D5\u05DC\u05E9", result);
    }

    [Fact]
    public void ReorderText_RTLWithLTRWords_MixedCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Hebrew: שלום World
        var text = "\u05E9\u05DC\u05D5\u05DD World";
        var result = algorithm.ReorderText(text, 1); // RTL base direction

        // Expected: "World םולש" (LTR word followed by reversed Hebrew)
        Assert.Equal("World \u05DD\u05D5\u05DC\u05E9", result);
    }

    [Fact]
    public void ReorderText_LTRWithRTLWords_MixedCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // English with Hebrew: Hello שלום
        var text = "Hello \u05E9\u05DC\u05D5\u05DD";
        var result = algorithm.ReorderText(text, 0); // LTR base direction

        // Expected: "Hello םולש" (LTR text followed by reversed Hebrew)
        Assert.Equal("Hello \u05DD\u05D5\u05DC\u05E9", result);
    }

    [Fact]
    public void ReorderText_ArabicWithNumbers_NumbersStayLTR()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Arabic with European digits: مرحبا 2025
        var text = "\u0645\u0631\u062D\u0628\u0627 2025";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Numbers should remain LTR
        // Expected: "2025 ابحرم" (2025 followed by reversed Arabic)
        Assert.Equal("2025 \u0627\u0628\u062D\u0631\u0645", result);
    }

    [Fact]
    public void ReorderText_ArabicWithArabicDigits_DigitsHandledCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Arabic with Arabic-Indic digits: مرحبا ٠١٢٣
        var text = "\u0645\u0631\u062D\u0628\u0627 \u0660\u0661\u0662\u0663";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Arabic-Indic digits (AN type) - in modern usage, these are displayed LTR even in RTL context
        // Expected: "٠١٢٣ ابحرم" (LTR digits followed by space and reversed Arabic)
        Assert.Equal("\u0660\u0661\u0662\u0663 \u0627\u0628\u062D\u0631\u0645", result);
    }

    [Fact]
    public void ReorderText_Punctuation_HandledCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Hebrew with punctuation: שלום!
        var text = "\u05E9\u05DC\u05D5\u05DD!";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Punctuation should be after the reversed Hebrew
        // Expected: "!םולש"
        Assert.Equal("!\u05DD\u05D5\u05DC\u05E9", result);
    }

    [Fact]
    public void ReorderText_ComplexMixed_HandledCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Complex: "Hello שלום World 123"
        var text = "Hello \u05E9\u05DC\u05D5\u05DD World 123";
        var result = algorithm.ReorderText(text, 0); // LTR base

        // With LTR base, expect: "Hello םולש World 123"
        Assert.Equal("Hello \u05DD\u05D5\u05DC\u05E9 World 123", result);
    }

    [Fact]
    public void ReorderText_EmptyString_ReturnsEmpty()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var result = algorithm.ReorderText("", 0);
        Assert.Equal("", result);
    }

    [Fact]
    public void ReorderText_NullString_ReturnsNull()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var result = algorithm.ReorderText(null!, 0);
        Assert.Null(result);
    }

    [Fact]
    public void ReorderText_AutoDetectLTR_DetectsCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "Hello World";
        var result = algorithm.ReorderText(text, -1); // Auto-detect

        // Should detect as LTR
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void ReorderText_AutoDetectRTL_DetectsCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Hebrew: שלום
        var text = "\u05E9\u05DC\u05D5\u05DD";
        var result = algorithm.ReorderText(text, -1); // Auto-detect

        // Should detect as RTL and reverse
        Assert.Equal("\u05DD\u05D5\u05DC\u05E9", result);
    }

    [Fact]
    public void ReorderText_WithExplicitLRE_EmbeddingWorks()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Hebrew with LTR embedding: "שלום<LRE>World<PDF>"
        var text = "\u05E9\u05DC\u05D5\u05DD\u202AWorld\u202C";
        var result = algorithm.ReorderText(text, 1); // RTL base

        // The embedded "World" should stay LTR within RTL context
        Assert.Contains("World", result);
    }

    [Fact]
    public void ReorderText_MultipleRuns_HandledCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Multiple alternating runs: "English עברית English עברית"
        var text = "English \u05E2\u05D1\u05E8\u05D9\u05EA English \u05E2\u05D1\u05E8\u05D9\u05EA";
        var result = algorithm.ReorderText(text, 0); // LTR base

        // Each Hebrew section should be reversed independently
        Assert.Contains("English", result);
        // Hebrew "עברית" reversed is "תירבע"
        Assert.Contains("\u05EA\u05D9\u05E8\u05D1\u05E2", result);
    }

    [Fact]
    public void ReorderText_Parentheses_Mirrored()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Hebrew with parentheses: "(שלום)"
        var text = "(\u05E9\u05DC\u05D5\u05DD)";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Parentheses positions change but content is reversed
        // Expected: ")םולש("
        Assert.Equal(")\u05DD\u05D5\u05DC\u05E9(", result);
    }

    [Fact]
    public void IsMirroredBracket_CommonBrackets_ReturnsTrue()
    {
        Assert.True(UnicodeBidiData.IsMirroredBracket('('));
        Assert.True(UnicodeBidiData.IsMirroredBracket(')'));
        Assert.True(UnicodeBidiData.IsMirroredBracket('['));
        Assert.True(UnicodeBidiData.IsMirroredBracket(']'));
        Assert.True(UnicodeBidiData.IsMirroredBracket('{'));
        Assert.True(UnicodeBidiData.IsMirroredBracket('}'));
    }

    [Fact]
    public void GetMirroredBracket_ReturnsCounterpart()
    {
        Assert.Equal(')', UnicodeBidiData.GetMirroredBracket('('));
        Assert.Equal('(', UnicodeBidiData.GetMirroredBracket(')'));
        Assert.Equal(']', UnicodeBidiData.GetMirroredBracket('['));
        Assert.Equal('[', UnicodeBidiData.GetMirroredBracket(']'));
    }

    [Fact]
    public void ReorderText_MultipleSpaces_PreservesSpacing()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "Hello  World"; // Two spaces
        var result = algorithm.ReorderText(text, 0); // LTR

        Assert.Equal("Hello  World", result);
    }

    [Fact]
    public void ReorderText_LeadingWhitespace_HandledCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "   Hello";
        var result = algorithm.ReorderText(text, 0); // LTR

        Assert.Equal("   Hello", result);
    }

    [Fact]
    public void ReorderText_TrailingWhitespace_HandledCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "Hello   ";
        var result = algorithm.ReorderText(text, 0); // LTR

        Assert.Equal("Hello   ", result);
    }

    [Fact]
    public void ReorderText_OnlyWhitespace_ReturnsUnchanged()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "     ";
        var result = algorithm.ReorderText(text, 0);

        Assert.Equal("     ", result);
    }

    [Fact]
    public void ReorderText_SingleCharacter_RTL()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "\u05D0"; // Hebrew Alef
        var result = algorithm.ReorderText(text, 1); // RTL

        Assert.Equal("\u05D0", result);
    }

    [Fact]
    public void ReorderText_SingleCharacter_LTR()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "A";
        var result = algorithm.ReorderText(text, 0); // LTR

        Assert.Equal("A", result);
    }

    [Fact]
    public void ReorderText_NumbersOnly_RemainsLTR()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "123456";
        var result = algorithm.ReorderText(text, 1); // RTL base

        // Numbers always stay LTR even in RTL context
        Assert.Equal("123456", result);
    }

    [Fact]
    public void ReorderText_MixedPunctuationAndNumbers_RTL()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // RTL text with numbers and punctuation
        var text = "\u05E9\u05DC\u05D5\u05DD 123!";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Numbers stay LTR, punctuation follows neutral behavior
        Assert.Contains("123", result);
        Assert.Contains("\u05DD\u05D5\u05DC\u05E9", result); // Reversed Hebrew
    }

    [Fact]
    public void ReorderText_NestedDirectionalText_HandlesCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // English with Hebrew with English inside: "Hello עברית English"
        var text = "Hello \u05E2\u05D1\u05E8\u05D9\u05EA English";
        var result = algorithm.ReorderText(text, 0); // LTR base

        // Should have both "Hello" and "English" in LTR order
        // Hebrew section should be reversed
        Assert.Contains("Hello", result);
        Assert.Contains("English", result);
        Assert.Contains("\u05EA\u05D9\u05E8\u05D1\u05E2", result); // Reversed Hebrew
    }

    [Fact]
    public void ReorderText_ConsecutiveRTLWords_GroupedCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Two Hebrew words: "שלום עולם" (Shalom Olam / Hello World)
        var text = "\u05E9\u05DC\u05D5\u05DD \u05E2\u05D5\u05DC\u05DD";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Both words should be reversed and right-to-left
        Assert.Contains("\u05DD\u05D5\u05DC\u05E9", result); // Reversed שלום
        Assert.Contains("\u05DD\u05DC\u05D5\u05E2", result); // Reversed עולם
    }

    [Fact]
    public void ReorderText_ArabicWithComma_HandledCorrectly()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Arabic with comma: "مرحبا، عالم"
        var text = "\u0645\u0631\u062D\u0628\u0627\u060C \u0639\u0627\u0644\u0645";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Arabic text should be reversed, comma is neutral
        Assert.NotEmpty(result);
    }

    [Fact]
    public void ReorderText_QuotationMarks_HandleBracketMirroring()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Hebrew with quotes: "שלום"
        var text = "\"\u05E9\u05DC\u05D5\u05DD\"";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Quotes and Hebrew should be handled
        Assert.Contains("\u05DD\u05D5\u05DC\u05E9", result); // Reversed Hebrew
    }

    [Fact]
    public void GetCharacterType_CommonPunctuation_ReturnsCorrectTypes()
    {
        // Period
        Assert.Equal(BidiCharacterType.CS, UnicodeBidiData.GetCharacterType('.'));

        // Comma
        Assert.Equal(BidiCharacterType.CS, UnicodeBidiData.GetCharacterType(','));

        // Exclamation mark
        Assert.Equal(BidiCharacterType.ON, UnicodeBidiData.GetCharacterType('!'));

        // Question mark
        Assert.Equal(BidiCharacterType.ON, UnicodeBidiData.GetCharacterType('?'));
    }

    [Fact]
    public void GetCharacterType_MathSymbols_ReturnsCorrectTypes()
    {
        Assert.Equal(BidiCharacterType.ES, UnicodeBidiData.GetCharacterType('+')); // Plus is ES (European Separator)
        Assert.Equal(BidiCharacterType.ES, UnicodeBidiData.GetCharacterType('-')); // Minus is ES
        Assert.Equal(BidiCharacterType.ON, UnicodeBidiData.GetCharacterType('=')); // Equals is ON
    }

    [Fact]
    public void ReorderText_VeryLongText_HandlesEfficiently()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Create a long mixed text
        var parts = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            parts.Add("English");
            parts.Add("\u05E2\u05D1\u05E8\u05D9\u05EA"); // Hebrew
        }
        var text = string.Join(" ", parts);

        // Act
        var start = DateTime.UtcNow;
        var result = algorithm.ReorderText(text, 0);
        var duration = DateTime.UtcNow - start;

        // Assert
        Assert.NotNull(result);
        Assert.True(duration.TotalSeconds < 2, "BiDi algorithm should complete efficiently");
    }

    [Fact]
    public void ReorderText_TabCharacters_HandledAsWhitespace()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "Hello\tWorld";
        var result = algorithm.ReorderText(text, 0); // LTR

        Assert.Equal("Hello\tWorld", result);
    }

    [Fact]
    public void ReorderText_NewlineCharacters_PreservedInText()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "Hello\nWorld";
        var result = algorithm.ReorderText(text, 0); // LTR

        Assert.Contains("\n", result);
    }

    [Fact]
    public void ReorderText_MixedArabicAndHebrew_BothHandledAsRTL()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        // Arabic followed by Hebrew
        var text = "\u0645\u0631\u062D\u0628\u0627 \u05E9\u05DC\u05D5\u05DD";
        var result = algorithm.ReorderText(text, 1); // RTL

        // Both should be reversed
        Assert.Contains("\u0627\u0628\u062D\u0631\u0645", result); // Reversed Arabic
        Assert.Contains("\u05DD\u05D5\u05DC\u05E9", result); // Reversed Hebrew
    }

    [Fact]
    public void GetCharacterType_CurrencySymbols_ReturnsET()
    {
        // Dollar sign
        Assert.Equal(BidiCharacterType.ET, UnicodeBidiData.GetCharacterType('$'));

        // Euro sign (if supported)
        // Note: This might vary by Unicode version
    }

    [Fact]
    public void ReorderText_SlashCharacter_HandleAsNeutral()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "\u05E9\u05DC\u05D5\u05DD/Hello";
        var result = algorithm.ReorderText(text, 1); // RTL

        Assert.NotEmpty(result);
    }

    [Fact]
    public void ReorderText_AllCapsEnglishInRTL_RemainsLTR()
    {
        var algorithm = new UnicodeBidiAlgorithm();
        var text = "\u05E9\u05DC\u05D5\u05DD HELLO";
        var result = algorithm.ReorderText(text, 1); // RTL

        // HELLO should remain in LTR order
        Assert.Contains("HELLO", result);
        // Hebrew should be reversed
        Assert.Contains("\u05DD\u05D5\u05DC\u05E9", result);
    }
}
