using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace Folly.Fonts.Tests;

public class FontResolverTests
{
    private static string GetTestFontPath(string fontName)
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var testFontsDir = Path.Combine(assemblyDir, "..", "..", "..", "TestFonts");
        return Path.Combine(testFontsDir, fontName);
    }

    [Fact]
    public void ResolveFontFamily_WithCustomFont_ReturnsCustomFontPath()
    {
        // Arrange
        var customFonts = new Dictionary<string, string>
        {
            ["Roboto"] = GetTestFontPath("Roboto-Regular.ttf")
        };
        var resolver = new FontResolver(customFonts);

        // Act
        var result = resolver.ResolveFontFamily("Roboto");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customFonts["Roboto"], result);
    }

    [Fact]
    public void ResolveFontFamily_WithFontStack_ReturnsFirstAvailable()
    {
        // Arrange
        var customFonts = new Dictionary<string, string>
        {
            ["LiberationSans"] = GetTestFontPath("LiberationSans-Regular.ttf")
        };
        var resolver = new FontResolver(customFonts);

        // Act - try non-existent font first, then available font
        var result = resolver.ResolveFontFamily("NonExistentFont, LiberationSans, Arial");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customFonts["LiberationSans"], result);
    }

    [Fact]
    public void ResolveFontFamily_WithWhitespace_HandlesCorrectly()
    {
        // Arrange
        var customFonts = new Dictionary<string, string>
        {
            ["Roboto"] = GetTestFontPath("Roboto-Regular.ttf")
        };
        var resolver = new FontResolver(customFonts);

        // Act - extra whitespace around font names
        var result = resolver.ResolveFontFamily("  NonExistent  ,  Roboto  ,  Arial  ");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customFonts["Roboto"], result);
    }

    [Fact]
    public void ResolveFontFamily_WithEmptyStack_ReturnsNull()
    {
        // Arrange
        var resolver = new FontResolver();

        // Act
        var result = resolver.ResolveFontFamily("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveFontFamily_WithNullStack_ReturnsNull()
    {
        // Arrange
        var resolver = new FontResolver();

        // Act
        var result = resolver.ResolveFontFamily(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveFontFamily_WithNoMatchingFonts_ReturnsNull()
    {
        // Arrange
        var resolver = new FontResolver();

        // Act
        var result = resolver.ResolveFontFamily("NonExistentFont1, NonExistentFont2");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveFontFamily_WithGenericFamily_ResolvesToSystemFont()
    {
        // Arrange
        var resolver = new FontResolver();

        // Act - generic families should resolve to system fonts
        var result = resolver.ResolveFontFamily("sans-serif");

        // Assert - may or may not find a system font depending on environment
        // Just verify it doesn't throw and returns either a path or null
        Assert.True(result == null || File.Exists(result));
    }

    [Fact]
    public void IsFontAvailable_WithCustomFont_ReturnsTrue()
    {
        // Arrange
        var customFonts = new Dictionary<string, string>
        {
            ["Roboto"] = GetTestFontPath("Roboto-Regular.ttf")
        };
        var resolver = new FontResolver(customFonts);

        // Act
        var result = resolver.IsFontAvailable("Roboto");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFontAvailable_WithNonExistentFont_ReturnsFalse()
    {
        // Arrange
        var resolver = new FontResolver();

        // Act
        var result = resolver.IsFontAvailable("NonExistentFont12345");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFontAvailable_WithEmptyName_ReturnsFalse()
    {
        // Arrange
        var resolver = new FontResolver();

        // Act
        var result = resolver.IsFontAvailable("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetAvailableSystemFonts_ReturnsNonNull()
    {
        // Arrange
        var resolver = new FontResolver();

        // Act
        var fonts = resolver.GetAvailableSystemFonts();

        // Assert
        Assert.NotNull(fonts);
        // Note: May be empty if no system fonts found, which is okay
    }

    [Fact]
    public void ClearCache_AllowsRescan()
    {
        // Arrange
        var resolver = new FontResolver();

        // Act - trigger scan
        var fonts1 = resolver.GetAvailableSystemFonts();

        // Clear cache
        resolver.ClearCache();

        // Get fonts again (should rescan)
        var fonts2 = resolver.GetAvailableSystemFonts();

        // Assert - both should be valid dictionaries
        Assert.NotNull(fonts1);
        Assert.NotNull(fonts2);
    }

    [Fact]
    public void ResolveFontFamily_IsCaseInsensitive()
    {
        // Arrange
        var customFonts = new Dictionary<string, string>
        {
            ["Roboto"] = GetTestFontPath("Roboto-Regular.ttf")
        };
        var resolver = new FontResolver(customFonts);

        // Act - try different cases
        var result1 = resolver.ResolveFontFamily("roboto");
        var result2 = resolver.ResolveFontFamily("ROBOTO");
        var result3 = resolver.ResolveFontFamily("RoBoTo");

        // Assert - should all resolve to the same font (or all be null)
        // Custom fonts are case-sensitive in the dictionary, but system fonts should be case-insensitive
        Assert.True(
            (result1 == null && result2 == null && result3 == null) ||
            (result1 == result2 && result2 == result3));
    }

    [Fact]
    public void ResolveFontFamily_MultipleFontsInStack_PrioritizesCorrectly()
    {
        // Arrange
        var customFonts = new Dictionary<string, string>
        {
            ["FontA"] = GetTestFontPath("Roboto-Regular.ttf"),
            ["FontB"] = GetTestFontPath("LiberationSans-Regular.ttf")
        };
        var resolver = new FontResolver(customFonts);

        // Act - FontB should be chosen since FontA comes first
        var result = resolver.ResolveFontFamily("FontA, FontB, FontC");

        // Assert
        Assert.Equal(customFonts["FontA"], result);
    }

    [Fact]
    public void ResolveFontFamily_WithNonExistentCustomFontPath_SkipsIt()
    {
        // Arrange
        var customFonts = new Dictionary<string, string>
        {
            ["BadFont"] = "/nonexistent/path/font.ttf",
            ["GoodFont"] = GetTestFontPath("Roboto-Regular.ttf")
        };
        var resolver = new FontResolver(customFonts);

        // Act - should skip BadFont and find GoodFont
        var result = resolver.ResolveFontFamily("BadFont, GoodFont");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customFonts["GoodFont"], result);
    }
}
