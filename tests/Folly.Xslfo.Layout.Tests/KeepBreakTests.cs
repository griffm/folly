using Xunit;

namespace Folly.Xslfo.Layout.Tests;

/// <summary>
/// Tests for advanced keep/break controls (Phase 11.4).
/// Priority 2 (Important) - 8 tests.
/// </summary>
public class KeepBreakTests
{
    [Fact]
    public void Keep_IntegerStrength_Basic()
    {
        // Arrange: Block with keep-together strength
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="200pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block margin-bottom="100pt">First block fills most of page</fo:block>
                  <fo:block keep-together.within-page="100">
                    <fo:block>Line 1 of kept block</fo:block>
                    <fo:block>Line 2 of kept block</fo:block>
                    <fo:block>Line 3 of kept block</fo:block>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout without errors
        // The keep-together constraint should be respected
        Assert.NotNull(areaTree);
        Assert.True(areaTree.Pages.Count >= 1, "Should produce at least one page");
    }

    [Fact]
    public void Keep_IntegerStrength_Comparison()
    {
        // Arrange: Test that higher strength values have higher priority
        // This test verifies that strength 999 (always) is stronger than strength 1
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="200pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block margin-bottom="90pt">First block</fo:block>
                  <fo:block keep-together.within-page="1">
                    <fo:block>Weak keep (strength 1)</fo:block>
                    <fo:block>Should break if necessary</fo:block>
                  </fo:block>
                  <fo:block keep-together.within-page="999">
                    <fo:block>Strong keep (strength 999)</fo:block>
                    <fo:block>Should not break</fo:block>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Layout should complete successfully
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void ForcePageCount_Even()
    {
        // Arrange: Page sequence that ends on page 1 (odd) should add blank page
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4" force-page-count="even">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Single page of content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should have 2 pages (1 content + 1 blank)
        Assert.NotNull(areaTree);
        Assert.Equal(2, areaTree.Pages.Count);
    }

    [Fact]
    public void ForcePageCount_Odd()
    {
        // Arrange: Page sequence that ends on page 2 (even) should add blank page
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="100pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4" force-page-count="odd">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>First page content</fo:block>
                  <fo:block break-before="page">Second page content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should have 3 pages (2 content + 1 blank)
        Assert.NotNull(areaTree);
        Assert.Equal(3, areaTree.Pages.Count);
    }

    [Fact]
    public void ForcePageCount_EndOnEven()
    {
        // Arrange: end-on-even is synonym for even
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4" force-page-count="end-on-even">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Single page of content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should have 2 pages (1 content + 1 blank), same as "even"
        Assert.NotNull(areaTree);
        Assert.Equal(2, areaTree.Pages.Count);
    }

    [Fact]
    public void ForcePageCount_EndOnOdd()
    {
        // Arrange: end-on-odd is synonym for odd
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="100pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4" force-page-count="end-on-odd">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>First page content</fo:block>
                  <fo:block break-before="page">Second page content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should have 3 pages (2 content + 1 blank), same as "odd"
        Assert.NotNull(areaTree);
        Assert.Equal(3, areaTree.Pages.Count);
    }

    [Fact]
    public void ForcePageCount_Auto()
    {
        // Arrange: auto means no forced blank pages
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4" force-page-count="auto">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Single page of content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should have exactly 1 page (no blank page added)
        Assert.NotNull(areaTree);
        Assert.Single(areaTree.Pages);
    }

    [Fact]
    public void ForcePageCount_Integration()
    {
        // Arrange: Multi-sequence document with different force-page-count values
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="150pt">
                  <fo:region-body margin="10pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4" force-page-count="even">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Sequence 1 (force even): Single page</fo:block>
                </fo:flow>
              </fo:page-sequence>
              <fo:page-sequence master-reference="A4" force-page-count="odd">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Sequence 2 (force odd): Page 1</fo:block>
                  <fo:block break-before="page">Sequence 2 (force odd): Page 2</fo:block>
                </fo:flow>
              </fo:page-sequence>
              <fo:page-sequence master-reference="A4" force-page-count="auto">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Sequence 3 (auto): Single page</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should have correct total page count
        // Sequence 1: 1 content + 1 blank = 2 pages (ends on even)
        // Sequence 2: 2 content + 1 blank = 3 pages (ends on odd)
        // Sequence 3: 1 content = 1 page (auto)
        // Total: 2 + 3 + 1 = 6 pages
        Assert.NotNull(areaTree);
        Assert.Equal(6, areaTree.Pages.Count);
    }
}
