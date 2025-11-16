using Xunit;

namespace Folly.FontTests;

/// <summary>
/// Tests for OpenType GPOS/GSUB features - ligatures and kerning (Phase 8.1).
/// Priority 2 (Important) - 10 tests.
/// </summary>
public class OpenTypeFeatureTests
{
    [Fact(Skip = "Test resource not yet available")]
    public void OpenType_Ligature_fi()
    {
        // TODO: Test "fi" ligature substitution with OpenType font
        // Font needed: Libertinus Serif, EB Garamond, or similar (SIL OFL)
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Test resource not yet available")]
    public void OpenType_Ligature_fl()
    {
        // TODO: Test "fl" ligature substitution
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Test resource not yet available")]
    public void OpenType_Ligature_ffi()
    {
        // TODO: Test "ffi" ligature substitution
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Test resource not yet available")]
    public void OpenType_Ligature_ffl()
    {
        // TODO: Test "ffl" ligature substitution
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Test resource not yet available")]
    public void OpenType_Ligature_ff()
    {
        // TODO: Test "ff" ligature substitution
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Test resource not yet available")]
    public void OpenType_Ligature_Disabled()
    {
        // TODO: Test that disabling liga feature prevents substitution
        // Should return 2 separate glyphs for "fi"
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Test resource not yet available")]
    public void OpenType_CommonLigatures_AllInOne()
    {
        // TODO: Test all common ligatures together in one string
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Test resource not yet available")]
    public void OpenType_Kerning_GPOS()
    {
        // TODO: Test kerning pair adjustment (e.g., "AV")
        // Font needed: Font with GPOS kerning table
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Test resource not yet available")]
    public void OpenType_Kerning_Disabled()
    {
        // TODO: Test that disabling kern feature removes adjustment
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Test resource not yet available")]
    public void OpenType_Shaper_Integration()
    {
        // TODO: Test OpenTypeShaper integration with layout engine
        Assert.True(true, "Not yet implemented");
    }
}
