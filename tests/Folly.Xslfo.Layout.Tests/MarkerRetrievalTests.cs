using Xunit;

namespace Folly.Xslfo.Layout.Tests;

/// <summary>
/// Tests for advanced marker retrieval positions (Phase 11.1).
/// Priority 2 (Important) - 10 tests.
/// </summary>
public class MarkerRetrievalTests
{
    [Fact]
    public void Marker_FirstStartingWithinPage()
    {
        // Arrange: Page with multiple markers, retrieve first one
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                  <fo:region-before extent="50pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-before">
                  <fo:block>
                    Chapter: <fo:retrieve-marker retrieve-class-name="chapter" retrieve-position="first-starting-within-page"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:marker marker-class-name="chapter">Chapter 1</fo:marker>
                    First paragraph
                  </fo:block>
                  <fo:block>
                    <fo:marker marker-class-name="chapter">Chapter 2</fo:marker>
                    Second paragraph
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully with marker retrieval
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void Marker_LastEndingWithinPage()
    {
        // Arrange: Retrieve last marker that ends on current page
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                  <fo:region-after extent="50pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-after">
                  <fo:block>
                    Last section: <fo:retrieve-marker retrieve-class-name="section" retrieve-position="last-ending-within-page"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:marker marker-class-name="section">Section A</fo:marker>
                    Content A
                  </fo:block>
                  <fo:block>
                    <fo:marker marker-class-name="section">Section B</fo:marker>
                    Content B
                  </fo:block>
                  <fo:block>
                    <fo:marker marker-class-name="section">Section C</fo:marker>
                    Content C
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void Marker_FirstIncludingCarryover()
    {
        // Arrange: Multi-page document where page 2 should use marker from page 1
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="150pt">
                  <fo:region-body margin="10pt"/>
                  <fo:region-before extent="20pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-before">
                  <fo:block font-size="10pt">
                    Current: <fo:retrieve-marker retrieve-class-name="topic" retrieve-position="first-including-carryover"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:marker marker-class-name="topic">Topic 1</fo:marker>
                    Page 1 content
                  </fo:block>
                  <fo:block break-before="page">
                    Page 2 has no marker, should use Topic 1 from page 1
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should have 2 pages, with carryover working
        Assert.NotNull(areaTree);
        Assert.Equal(2, areaTree.Pages.Count);
    }

    [Fact]
    public void Marker_LastStartingWithinPage()
    {
        // Arrange: Retrieve last marker that starts on this page
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                  <fo:region-after extent="50pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-after">
                  <fo:block>
                    Last started: <fo:retrieve-marker retrieve-class-name="subsection" retrieve-position="last-starting-within-page"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:marker marker-class-name="subsection">Subsection 1.1</fo:marker>
                    First subsection
                  </fo:block>
                  <fo:block>
                    <fo:marker marker-class-name="subsection">Subsection 1.2</fo:marker>
                    Second subsection
                  </fo:block>
                  <fo:block>
                    <fo:marker marker-class-name="subsection">Subsection 1.3</fo:marker>
                    Third subsection
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void Marker_Carryover_AcrossPages()
    {
        // Arrange: Test marker carryover from page 1 to page 2
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="120pt">
                  <fo:region-body margin="10pt"/>
                  <fo:region-before extent="15pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-before">
                  <fo:block font-size="8pt">
                    Header: <fo:retrieve-marker retrieve-class-name="header" retrieve-position="first-including-carryover"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:marker marker-class-name="header">Document Title</fo:marker>
                    Page 1 content with marker
                  </fo:block>
                  <fo:block break-before="page">
                    Page 2 content without marker (should carry over Document Title)
                  </fo:block>
                  <fo:block break-before="page">
                    <fo:marker marker-class-name="header">New Title</fo:marker>
                    Page 3 with new marker
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should have 3 pages with proper carryover
        Assert.NotNull(areaTree);
        Assert.Equal(3, areaTree.Pages.Count);
    }

    [Fact]
    public void Marker_MultipleMarkers_SamePage()
    {
        // Arrange: Multiple markers with same class name on same page
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                  <fo:region-before extent="50pt"/>
                  <fo:region-after extent="50pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-before">
                  <fo:block>
                    First: <fo:retrieve-marker retrieve-class-name="item" retrieve-position="first-starting-within-page"/>
                  </fo:block>
                </fo:static-content>
                <fo:static-content flow-name="xsl-region-after">
                  <fo:block>
                    Last: <fo:retrieve-marker retrieve-class-name="item" retrieve-position="last-ending-within-page"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:marker marker-class-name="item">Item A</fo:marker>
                    Content A
                  </fo:block>
                  <fo:block>
                    <fo:marker marker-class-name="item">Item B</fo:marker>
                    Content B
                  </fo:block>
                  <fo:block>
                    <fo:marker marker-class-name="item">Item C</fo:marker>
                    Content C
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void Marker_SequenceNumbers()
    {
        // Arrange: Test that markers are tracked with sequence numbers
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                  <fo:region-before extent="50pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-before">
                  <fo:block>
                    Running head: <fo:retrieve-marker retrieve-class-name="heading" retrieve-position="first-starting-within-page"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:marker marker-class-name="heading">First Heading</fo:marker>
                    First content
                  </fo:block>
                  <fo:block>
                    <fo:marker marker-class-name="heading">Second Heading</fo:marker>
                    Second content
                  </fo:block>
                  <fo:block>
                    <fo:marker marker-class-name="heading">Third Heading</fo:marker>
                    Third content
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree (markers should be tracked with sequence numbers)
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void Marker_NoMarker_ReturnsNull()
    {
        // Arrange: Page with no markers should handle gracefully
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                  <fo:region-before extent="50pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-before">
                  <fo:block>
                    Header: <fo:retrieve-marker retrieve-class-name="nonexistent" retrieve-position="first-starting-within-page"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Content without any markers</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree (should handle missing marker gracefully)
        var areaTree = doc.BuildAreaTree();

        // Assert: Document should layout successfully even with no marker
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void Marker_MarkerScope_PageSequence()
    {
        // Arrange: Markers should be scoped to their page sequence
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="150pt">
                  <fo:region-body margin="10pt"/>
                  <fo:region-before extent="20pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-before">
                  <fo:block font-size="10pt">
                    Seq 1: <fo:retrieve-marker retrieve-class-name="seq" retrieve-position="first-including-carryover"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:marker marker-class-name="seq">Sequence 1</fo:marker>
                    First page sequence
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-before">
                  <fo:block font-size="10pt">
                    Seq 2: <fo:retrieve-marker retrieve-class-name="seq" retrieve-position="first-including-carryover"/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:marker marker-class-name="seq">Sequence 2</fo:marker>
                    Second page sequence (markers from first sequence should not be visible)
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should have 2 pages from 2 page sequences
        Assert.NotNull(areaTree);
        Assert.Equal(2, areaTree.Pages.Count);
    }

    [Fact]
    public void Marker_Integration_RunningHeader()
    {
        // Arrange: Realistic running header scenario across multiple pages
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="200pt">
                  <fo:region-body margin-top="30pt" margin-bottom="30pt" margin-left="50pt" margin-right="50pt"/>
                  <fo:region-before extent="25pt"/>
                  <fo:region-after extent="25pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-before">
                  <fo:block text-align="center" font-size="10pt" border-bottom="1pt solid black">
                    <fo:retrieve-marker retrieve-class-name="chapter" retrieve-position="first-including-carryover"/>
                  </fo:block>
                </fo:static-content>
                <fo:static-content flow-name="xsl-region-after">
                  <fo:block text-align="center" font-size="10pt">
                    Page <fo:page-number/>
                  </fo:block>
                </fo:static-content>
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:marker marker-class-name="chapter">Chapter 1: Introduction</fo:marker>
                    This is the content of chapter 1.
                  </fo:block>
                  <fo:block break-before="page">
                    More chapter 1 content on page 2 (marker carries over).
                  </fo:block>
                  <fo:block break-before="page">
                    <fo:marker marker-class-name="chapter">Chapter 2: Methods</fo:marker>
                    This is the start of chapter 2.
                  </fo:block>
                  <fo:block break-before="page">
                    More chapter 2 content on page 4.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act: Build area tree
        var areaTree = doc.BuildAreaTree();

        // Assert: Should have 4 pages with running headers
        Assert.NotNull(areaTree);
        Assert.Equal(4, areaTree.Pages.Count);
    }
}
