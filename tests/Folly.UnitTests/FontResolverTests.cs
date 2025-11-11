using Folly.Fonts;
using Xunit;

namespace Folly.UnitTests;

/// <summary>
/// Unit tests for FontResolver and font variant resolution logic.
/// </summary>
public class FontResolverTests
{
    #region Generic Family Normalization Tests

    [Theory]
    [InlineData("serif", "Times-Roman")]
    [InlineData("sans-serif", "Helvetica")]
    [InlineData("monospace", "Courier")]
    [InlineData("times", "Times-Roman")]
    [InlineData("times new roman", "Times-Roman")]
    [InlineData("helvetica", "Helvetica")]
    [InlineData("arial", "Helvetica")]
    [InlineData("courier", "Courier")]
    [InlineData("courier new", "Courier")]
    public void NormalizeFamily_GenericFamilies_ReturnCorrectBaseFont(string input, string expected)
    {
        var result = FontResolver.NormalizeFamily(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("SERIF", "Times-Roman")]
    [InlineData("Sans-Serif", "Helvetica")]
    [InlineData("MONOSPACE", "Courier")]
    [InlineData("Helvetica", "Helvetica")]
    [InlineData("TIMES-ROMAN", "Times-Roman")]
    public void NormalizeFamily_CaseInsensitive_ReturnsCorrectFont(string input, string expected)
    {
        var result = FontResolver.NormalizeFamily(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("UnknownFont", "UnknownFont")]
    [InlineData("CustomFamily", "CustomFamily")]
    public void NormalizeFamily_UnknownFamilies_ReturnsInputAsIs(string input, string expected)
    {
        var result = FontResolver.NormalizeFamily(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeFamily_EmptyString_ReturnsHelvetica()
    {
        var result = FontResolver.NormalizeFamily("");
        Assert.Equal("Helvetica", result);
    }

    [Fact]
    public void NormalizeFamily_Null_ReturnsHelvetica()
    {
        var result = FontResolver.NormalizeFamily(null!);
        Assert.Equal("Helvetica", result);
    }

    #endregion

    #region Font Weight Tests

    [Theory]
    [InlineData("bold", true)]
    [InlineData("BOLD", true)]
    [InlineData("Bold", true)]
    [InlineData("700", true)]
    [InlineData("800", true)]
    [InlineData("900", true)]
    public void IsBoldWeight_BoldWeights_ReturnsTrue(string weight, bool expected)
    {
        var result = FontResolver.IsBoldWeight(weight);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("normal", false)]
    [InlineData("400", false)]
    [InlineData("500", false)]
    [InlineData("600", false)]
    [InlineData("100", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsBoldWeight_NormalWeights_ReturnsFalse(string? weight, bool expected)
    {
        var result = FontResolver.IsBoldWeight(weight);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Font Style Tests

    [Theory]
    [InlineData("italic", true)]
    [InlineData("oblique", true)]
    [InlineData("ITALIC", true)]
    [InlineData("Oblique", true)]
    public void IsItalicStyle_ItalicStyles_ReturnsTrue(string style, bool expected)
    {
        var result = FontResolver.IsItalicStyle(style);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("normal", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("unknown", false)]
    public void IsItalicStyle_NonItalicStyles_ReturnsFalse(string? style, bool expected)
    {
        var result = FontResolver.IsItalicStyle(style);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Helvetica Font Resolution Tests

    [Fact]
    public void ResolveFont_HelveticaNormal_ReturnsHelvetica()
    {
        var result = FontResolver.ResolveFont("Helvetica", false, false);
        Assert.Equal("Helvetica", result);
    }

    [Fact]
    public void ResolveFont_HelveticaBold_ReturnsHelveticaBold()
    {
        var result = FontResolver.ResolveFont("Helvetica", true, false);
        Assert.Equal("Helvetica-Bold", result);
    }

    [Fact]
    public void ResolveFont_HelveticaItalic_ReturnsHelveticaOblique()
    {
        var result = FontResolver.ResolveFont("Helvetica", false, true);
        Assert.Equal("Helvetica-Oblique", result);
    }

    [Fact]
    public void ResolveFont_HelveticaBoldItalic_ReturnsHelveticaBoldOblique()
    {
        var result = FontResolver.ResolveFont("Helvetica", true, true);
        Assert.Equal("Helvetica-BoldOblique", result);
    }

    #endregion

    #region Times Font Resolution Tests

    [Fact]
    public void ResolveFont_TimesNormal_ReturnsTimesRoman()
    {
        var result = FontResolver.ResolveFont("Times-Roman", false, false);
        Assert.Equal("Times-Roman", result);
    }

    [Fact]
    public void ResolveFont_TimesBold_ReturnsTimesBold()
    {
        var result = FontResolver.ResolveFont("Times-Roman", true, false);
        Assert.Equal("Times-Bold", result);
    }

    [Fact]
    public void ResolveFont_TimesItalic_ReturnsTimesItalic()
    {
        var result = FontResolver.ResolveFont("Times-Roman", false, true);
        Assert.Equal("Times-Italic", result);
    }

    [Fact]
    public void ResolveFont_TimesBoldItalic_ReturnsTimesBoldItalic()
    {
        var result = FontResolver.ResolveFont("Times-Roman", true, true);
        Assert.Equal("Times-BoldItalic", result);
    }

    #endregion

    #region Courier Font Resolution Tests

    [Fact]
    public void ResolveFont_CourierNormal_ReturnsCourier()
    {
        var result = FontResolver.ResolveFont("Courier", false, false);
        Assert.Equal("Courier", result);
    }

    [Fact]
    public void ResolveFont_CourierBold_ReturnsCourierBold()
    {
        var result = FontResolver.ResolveFont("Courier", true, false);
        Assert.Equal("Courier-Bold", result);
    }

    [Fact]
    public void ResolveFont_CourierItalic_ReturnsCourierOblique()
    {
        var result = FontResolver.ResolveFont("Courier", false, true);
        Assert.Equal("Courier-Oblique", result);
    }

    [Fact]
    public void ResolveFont_CourierBoldItalic_ReturnsCourierBoldOblique()
    {
        var result = FontResolver.ResolveFont("Courier", true, true);
        Assert.Equal("Courier-BoldOblique", result);
    }

    #endregion

    #region CSS-Style Weight/Style String Tests

    [Theory]
    [InlineData("Helvetica", "bold", "italic", "Helvetica-BoldOblique")]
    [InlineData("Times-Roman", "700", "italic", "Times-BoldItalic")]
    [InlineData("Courier", "normal", "oblique", "Courier-Oblique")]
    [InlineData("Helvetica", "400", "normal", "Helvetica")]
    [InlineData("Times-Roman", "800", null, "Times-Bold")]
    [InlineData("Courier", null, "italic", "Courier-Oblique")]
    public void ResolveFont_WithCssStrings_ReturnsCorrectVariant(
        string family, string? weight, string? style, string expected)
    {
        var result = FontResolver.ResolveFont(family, weight, style);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Generic Family with Variants Tests

    [Theory]
    [InlineData("serif", "bold", "italic", "Times-BoldItalic")]
    [InlineData("sans-serif", "700", "oblique", "Helvetica-BoldOblique")]
    [InlineData("monospace", "bold", null, "Courier-Bold")]
    [InlineData("times", null, "italic", "Times-Italic")]
    [InlineData("helvetica", "400", "oblique", "Helvetica-Oblique")]
    [InlineData("courier new", "800", "italic", "Courier-BoldOblique")]
    public void ResolveFont_GenericFamiliesWithVariants_ReturnsCorrectFont(
        string family, string? weight, string? style, string expected)
    {
        var result = FontResolver.ResolveFont(family, weight, style);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Edge Cases and Fallbacks Tests

    [Fact]
    public void ResolveFont_UnknownFamily_AppliesVariantSyntax()
    {
        // For unknown families, the system should still attempt to apply variant suffixes
        var result = FontResolver.ResolveFont("UnknownFont", true, true);
        // Should get some variant name with Bold and Italic/Oblique
        Assert.Contains("Bold", result);
    }

    [Theory]
    [InlineData("", false, false)]
    [InlineData(null, true, false)]
    [InlineData("", false, true)]
    public void ResolveFont_EmptyOrNullFamily_ReturnsFallback(string? family, bool bold, bool italic)
    {
        var result = FontResolver.ResolveFont(family ?? "", bold, italic);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // Should fallback to some default (likely Helvetica variant)
        Assert.StartsWith("Helvetica", result);
    }

    #endregion

    #region Already Resolved Font Name Tests

    [Theory]
    [InlineData("times-italic", "Times-Italic")]
    [InlineData("HELVETICA-BOLD", "Helvetica-Bold")]
    [InlineData("Courier-BoldOblique", "Courier-BoldOblique")]
    public void NormalizeFamily_AlreadyResolvedNames_ReturnsCanonicalName(string input, string expected)
    {
        var result = FontResolver.NormalizeFamily(input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Font Name Format Tests

    [Theory]
    [InlineData("serif", false, false, "Times-Roman")]
    [InlineData("sans-serif", true, false, "Helvetica-Bold")]
    [InlineData("monospace", false, true, "Courier-Oblique")]
    [InlineData("Times-Roman", true, true, "Times-BoldItalic")]
    [InlineData("Helvetica", false, false, "Helvetica")]
    public void ResolveFont_VariousInputs_ReturnsWellFormedFontName(
        string family, bool bold, bool italic, string expected)
    {
        var fontName = FontResolver.ResolveFont(family, bold, italic);

        Assert.NotNull(fontName);
        Assert.NotEmpty(fontName);
        Assert.Equal(expected, fontName);
    }

    [Theory]
    [InlineData("serif", "700", "italic", "Times-BoldItalic")]
    [InlineData("sans-serif", "bold", null, "Helvetica-Bold")]
    [InlineData("monospace", null, "oblique", "Courier-Oblique")]
    public void ResolveFont_CssStrings_ReturnsWellFormedFontName(
        string family, string? weight, string? style, string expected)
    {
        var fontName = FontResolver.ResolveFont(family, weight, style);

        Assert.NotNull(fontName);
        Assert.NotEmpty(fontName);
        Assert.Equal(expected, fontName);
    }

    #endregion

    #region Consistency Tests

    [Fact]
    public void ResolveFont_BooleanAndStringOverloads_ProduceSameResults()
    {
        // Test that both overloads produce the same results
        var result1 = FontResolver.ResolveFont("Helvetica", true, true);
        var result2 = FontResolver.ResolveFont("Helvetica", "bold", "italic");

        Assert.Equal(result1, result2);
    }

    [Theory]
    [InlineData("Times-Roman", false, false, "400", "normal")]
    [InlineData("Courier", true, false, "700", null)]
    [InlineData("Helvetica", false, true, null, "italic")]
    public void ResolveFont_OverloadConsistency_ProducesSameResult(
        string family, bool bold, bool italic, string? weight, string? style)
    {
        var result1 = FontResolver.ResolveFont(family, bold, italic);
        var result2 = FontResolver.ResolveFont(family, weight, style);

        Assert.Equal(result1, result2);
    }

    #endregion
}
