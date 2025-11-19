using Folly.Layout;
using Xunit;

namespace Folly.Xslfo.Layout.Tests;

/// <summary>
/// Tests for content-based float sizing (Phase 11.3).
/// Priority 2 (Important) - 6 tests.
/// </summary>
public class FloatSizingTests
{
    private FloatArea? FindFloatArea(AreaTree areaTree)
    {
        foreach (var page in areaTree.Pages)
        {
            var floatArea = FindFloatAreaInChildren(page.Areas);
            if (floatArea != null)
                return floatArea;
        }
        return null;
    }

    private FloatArea? FindFloatAreaInChildren(IReadOnlyList<Area> areas)
    {
        foreach (var area in areas)
        {
            if (area is FloatArea floatArea)
                return floatArea;

            if (area is BlockArea blockArea && blockArea.Children.Count > 0)
            {
                var found = FindFloatAreaInChildren(blockArea.Children);
                if (found != null)
                    return found;
            }
        }
        return null;
    }

    [Fact]
    public void FloatWidth_Explicit_Absolute()
    {
        // Arrange: Float with explicit absolute width
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="500pt" page-height="300pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:float float="start">
                    <fo:block width="100pt" background-color="lightblue">Float content</fo:block>
                  </fo:float>
                  <fo:block>Main content flows around the float.</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Find float and verify width
        var floatArea = FindFloatArea(areaTree);
        Assert.NotNull(floatArea);
        Assert.Equal(100.0, floatArea.Width, precision: 1);
    }

    [Fact]
    public void FloatWidth_Explicit_Percentage()
    {
        // Arrange: Float with percentage width (25% of 480pt body = 120pt)
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="500pt" page-height="300pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:float float="start">
                    <fo:block width="25%" background-color="lightgreen">Float content</fo:block>
                  </fo:float>
                  <fo:block>Main content flows around the float.</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Find float and verify width
        // 25% of (500pt page - 20pt margin) = 25% of 480pt = 120pt
        var floatArea = FindFloatArea(areaTree);
        Assert.NotNull(floatArea);
        Assert.Equal(120.0, floatArea.Width, precision: 1);
    }

    [Fact]
    public void FloatWidth_Auto_ContentBased()
    {
        // Arrange: Float with auto width should measure content
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="500pt" page-height="300pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:float float="start">
                    <fo:block width="auto" font-size="12pt">Short text</fo:block>
                  </fo:float>
                  <fo:block>Main content flows around the float.</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Float should have width based on content
        var floatArea = FindFloatArea(areaTree);
        Assert.NotNull(floatArea);
        // Auto width should be > 0 and based on content measurement
        Assert.True(floatArea.Width > 0, "Auto width should be positive");
        Assert.True(floatArea.Width < 480, "Auto width should be less than full body width");
    }

    [Fact]
    public void FloatWidth_Auto_MaxConstraint()
    {
        // Arrange: Float with auto width and very long content
        // Should be clamped to 1/3 of body width
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="600pt" page-height="300pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:float float="start">
                    <fo:block width="auto" font-size="12pt">This is a very long text content that would naturally be quite wide if left unconstrained</fo:block>
                  </fo:float>
                  <fo:block>Main content flows around the float.</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Auto width should be clamped to 1/3 of body width
        // Body width = 600 - 20 = 580pt, max float width = 580/3 ≈ 193.33pt
        var floatArea = FindFloatArea(areaTree);
        Assert.NotNull(floatArea);
        Assert.True(floatArea.Width <= 194.0,
            $"Auto width should be clamped to ~193pt (1/3 of body), but was {floatArea.Width:F1}pt");
        Assert.True(floatArea.Width > 0, "Float should have positive width");
    }

    [Fact]
    public void FloatWidth_Auto_MinimumConstraint()
    {
        // Arrange: Float with minimal content should respect MinimumColumnWidth (50pt)
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="500pt" page-height="300pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:float float="start">
                    <fo:block width="auto" font-size="12pt">X</fo:block>
                  </fo:float>
                  <fo:block>Main content flows around the float.</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Float should respect MinimumColumnWidth (50pt)
        var floatArea = FindFloatArea(areaTree);
        Assert.NotNull(floatArea);
        Assert.True(floatArea.Width >= 50.0,
            $"Float width should be at least MinimumColumnWidth (50pt), but was {floatArea.Width:F1}pt");
    }

    [Fact]
    public void FloatWidth_Integration()
    {
        // Arrange: Integration test with multiple floats and different width types
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="600pt" page-height="800pt">
                  <fo:region-body margin="10pt" column-count="2" column-gap="20pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:float float="start">
                    <fo:block width="80pt" background-color="lightblue">Fixed 80pt</fo:block>
                  </fo:float>
                  <fo:block>This is some body text that flows around the float in a multi-column layout. The float should work correctly regardless of column boundaries.</fo:block>
                  <fo:float float="end">
                    <fo:block width="25%" background-color="lightgreen">25% width</fo:block>
                  </fo:float>
                  <fo:block>More content that demonstrates the layout engine can handle multiple floats with different width specifications in a complex layout scenario.</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully without errors
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);

        // Verify at least one float was created
        var floatArea = FindFloatArea(areaTree);
        Assert.NotNull(floatArea);
        Assert.True(floatArea.Width > 0, "Float should have positive width");
    }
}
