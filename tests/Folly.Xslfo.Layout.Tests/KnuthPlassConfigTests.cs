using Xunit;

namespace Folly.Xslfo.Layout.Tests;

/// <summary>
/// Tests for configurable Knuth-Plass parameters (Phase 10.3).
/// Priority 2 (Important) - 8 tests.
/// </summary>
public class KnuthPlassConfigTests
{
    [Fact]
    public void KnuthPlass_DefaultParameters()
    {
        // Arrange
        var options = new LayoutOptions();

        // Assert
        Assert.Equal(0.5, options.KnuthPlassSpaceStretchRatio);
        Assert.Equal(0.333, options.KnuthPlassSpaceShrinkRatio, precision: 3);
        Assert.Equal(1.0, options.KnuthPlassTolerance);
    }

    [Fact]
    public void KnuthPlass_CustomStretchRatio()
    {
        // Arrange: Create layout options with custom stretch ratio
        var options = new LayoutOptions
        {
            KnuthPlassSpaceStretchRatio = 0.8  // Allow more stretching than default 0.5
        };

        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="300pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block text-align="justify">
                    This is a test paragraph with several words that will be laid out using the Knuth-Plass algorithm with custom space stretch ratio.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree with custom stretch ratio
        var areaTree = doc.BuildAreaTree(options);

        // Assert: Layout should complete successfully with custom stretch ratio
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        Assert.Equal(0.8, options.KnuthPlassSpaceStretchRatio);
    }

    [Fact]
    public void KnuthPlass_CustomShrinkRatio()
    {
        // Arrange: Create layout options with custom shrink ratio
        var options = new LayoutOptions
        {
            KnuthPlassSpaceShrinkRatio = 0.5  // Allow more shrinking than default 0.333
        };

        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="250pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block text-align="justify">
                    This is a paragraph with many words that will require space shrinking to fit properly on narrow lines with justified text alignment.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree with custom shrink ratio
        var areaTree = doc.BuildAreaTree(options);

        // Assert: Layout should complete successfully with custom shrink ratio
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        Assert.Equal(0.5, options.KnuthPlassSpaceShrinkRatio);
    }

    [Fact]
    public void KnuthPlass_CustomTolerance()
    {
        // Arrange: Create layout options with custom tolerance
        var options = new LayoutOptions
        {
            KnuthPlassTolerance = 1.5  // More tolerant than default 1.0
        };

        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="280pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block text-align="justify">
                    This paragraph tests the tolerance parameter which controls how much variation from ideal line width is acceptable in the Knuth-Plass algorithm.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree with custom tolerance
        var areaTree = doc.BuildAreaTree(options);

        // Assert: Layout should complete successfully with custom tolerance
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        Assert.Equal(1.5, options.KnuthPlassTolerance);
    }

    [Fact]
    public void KnuthPlass_TightTolerance()
    {
        // Arrange: Create layout options with tight tolerance
        // Tight tolerance means stricter justification requirements
        var options = new LayoutOptions
        {
            KnuthPlassTolerance = 0.5  // Tighter than default 1.0
        };

        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="320pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block text-align="justify">
                    With tight tolerance the Knuth-Plass algorithm is more selective about which line breaks are acceptable. This results in more consistent line spacing but may produce overfull or underfull boxes in difficult situations.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree with tight tolerance
        var areaTree = doc.BuildAreaTree(options);

        // Assert: Layout should complete successfully even with tight tolerance
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        Assert.Equal(0.5, options.KnuthPlassTolerance);
    }

    [Fact]
    public void KnuthPlass_LooseTolerance()
    {
        // Arrange: Create layout options with loose tolerance
        // Loose tolerance allows more variation from ideal line width
        var options = new LayoutOptions
        {
            KnuthPlassTolerance = 2.0  // Looser than default 1.0
        };

        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="290pt" page-height="400pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block text-align="justify">
                    Loose tolerance allows the Knuth-Plass algorithm to accept a wider range of line breaks including lines with less ideal spacing. This produces more feasible break points and reduces the risk of overfull boxes.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree with loose tolerance
        var areaTree = doc.BuildAreaTree(options);

        // Assert: Layout should complete successfully with loose tolerance
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        Assert.Equal(2.0, options.KnuthPlassTolerance);
    }

    [Fact]
    public void KnuthPlass_CustomPenalties()
    {
        // Arrange: Create layout options with custom penalty values
        var options = new LayoutOptions
        {
            KnuthPlassLinePenalty = 15.0,        // Higher than default 10.0 (fewer lines)
            KnuthPlassFlaggedDemerit = 150.0,    // Higher than default 100.0 (discourages consecutive bad lines)
            KnuthPlassFitnessDemerit = 80.0,     // Lower than default 100.0 (more flexible fitness class changes)
            KnuthPlassHyphenPenalty = 75.0       // Higher than default 50.0 (discourages hyphenation)
        };

        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="320pt" page-height="500pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block text-align="justify">
                    This test paragraph verifies that custom penalty values are accepted by the layout engine. The Knuth-Plass algorithm uses penalties to control line breaking behavior including the cost of line breaks, consecutive bad lines, fitness class changes, and hyphenation points.
                  </fo:block>
                  <fo:block text-align="justify" space-before="10pt">
                    Multiple paragraphs help demonstrate how these penalty settings affect overall document layout quality and consistency.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree with custom penalties
        var areaTree = doc.BuildAreaTree(options);

        // Assert: Layout should complete successfully with custom penalties
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
        Assert.Equal(15.0, options.KnuthPlassLinePenalty);
        Assert.Equal(150.0, options.KnuthPlassFlaggedDemerit);
        Assert.Equal(80.0, options.KnuthPlassFitnessDemerit);
        Assert.Equal(75.0, options.KnuthPlassHyphenPenalty);
    }

    [Fact]
    public void KnuthPlass_Integration()
    {
        // Arrange: Integration test with comprehensive Knuth-Plass configuration
        // Tests that all parameters work together in a realistic document
        var options = new LayoutOptions
        {
            KnuthPlassSpaceStretchRatio = 0.6,
            KnuthPlassSpaceShrinkRatio = 0.4,
            KnuthPlassTolerance = 1.2,
            KnuthPlassLinePenalty = 12.0,
            KnuthPlassFlaggedDemerit = 120.0,
            KnuthPlassFitnessDemerit = 90.0,
            KnuthPlassHyphenPenalty = 60.0
        };

        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="400pt" page-height="600pt">
                  <fo:region-body margin="20pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block text-align="justify" space-after="12pt">
                    This integration test demonstrates the Knuth-Plass line breaking algorithm with custom configuration parameters. The algorithm was developed by Donald Knuth and Michael Plass for the TeX typesetting system and provides optimal line breaking by minimizing the total badness of a paragraph.
                  </fo:block>
                  <fo:block text-align="justify" space-after="12pt">
                    The algorithm considers all possible line breaks in a paragraph and uses dynamic programming to find the sequence that minimizes a cost function. This cost includes penalties for stretched or compressed spaces, consecutive hyphens, and fitness class changes between adjacent lines.
                  </fo:block>
                  <fo:block text-align="justify" space-after="12pt">
                    By adjusting parameters like stretch ratio, shrink ratio, tolerance, and various penalties, users can fine-tune the line breaking behavior to match their specific requirements. Higher tolerance values produce looser justification, while lower values enforce stricter spacing at the potential cost of overfull boxes.
                  </fo:block>
                  <fo:block text-align="justify">
                    This multi-paragraph test ensures that custom Knuth-Plass configuration works correctly across an entire document with varied content lengths and breaking challenges. The layout engine should handle all parameter combinations gracefully while producing high-quality justified text.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree with comprehensive custom configuration
        var areaTree = doc.BuildAreaTree(options);

        // Assert: Document should layout successfully with all custom parameters
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);

        // Verify all custom parameters are preserved
        Assert.Equal(0.6, options.KnuthPlassSpaceStretchRatio);
        Assert.Equal(0.4, options.KnuthPlassSpaceShrinkRatio);
        Assert.Equal(1.2, options.KnuthPlassTolerance);
        Assert.Equal(12.0, options.KnuthPlassLinePenalty);
        Assert.Equal(120.0, options.KnuthPlassFlaggedDemerit);
        Assert.Equal(90.0, options.KnuthPlassFitnessDemerit);
        Assert.Equal(60.0, options.KnuthPlassHyphenPenalty);
    }
}
