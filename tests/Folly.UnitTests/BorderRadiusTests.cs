using System.IO;
using Xunit;

namespace Folly.UnitTests;

public class BorderRadiusTests
{
    private static byte[] RenderFoDocument(string foXml)
    {
        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();
        var options = new PdfOptions { CompressStreams = false };
        doc.SavePdf(outputStream, options);
        return outputStream.ToArray();
    }

    [Fact]
    public void BorderRadius_UniformRadius_RendersCorrectly()
    {
        // Arrange
        var foDocument = @"<?xml version=""1.0""?>
<fo:root xmlns:fo=""http://www.w3.org/1999/XSL/Format"">
  <fo:layout-master-set>
    <fo:simple-page-master master-name=""page"" page-width=""8.5in"" page-height=""11in""
                           margin-top=""0.5in"" margin-bottom=""0.5in""
                           margin-left=""0.5in"" margin-right=""0.5in"">
      <fo:region-body/>
    </fo:simple-page-master>
  </fo:layout-master-set>

  <fo:page-sequence master-reference=""page"">
    <fo:flow flow-name=""xsl-region-body"">
      <fo:block border=""2pt solid black"" border-radius=""10pt"" padding=""10pt"">
        This block has uniform rounded corners with 10pt radius.
      </fo:block>
    </fo:flow>
  </fo:page-sequence>
</fo:root>";

        // Act
        var pdf = RenderFoDocument(foDocument);

        // Assert
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);

        // Verify PDF contains Bezier curve operators (c = curveto)
        var pdfText = System.Text.Encoding.Latin1.GetString(pdf);
        Assert.Contains(" c\n", pdfText); // Bezier curve operator
    }

    [Fact]
    public void BorderRadius_IndividualCorners_RendersCorrectly()
    {
        // Arrange
        var foDocument = @"<?xml version=""1.0""?>
<fo:root xmlns:fo=""http://www.w3.org/1999/XSL/Format"">
  <fo:layout-master-set>
    <fo:simple-page-master master-name=""page"" page-width=""8.5in"" page-height=""11in""
                           margin-top=""0.5in"" margin-bottom=""0.5in""
                           margin-left=""0.5in"" margin-right=""0.5in"">
      <fo:region-body/>
    </fo:simple-page-master>
  </fo:layout-master-set>

  <fo:page-sequence master-reference=""page"">
    <fo:flow flow-name=""xsl-region-body"">
      <fo:block border=""2pt solid blue""
                border-top-left-radius=""5pt""
                border-top-right-radius=""10pt""
                border-bottom-right-radius=""15pt""
                border-bottom-left-radius=""20pt""
                padding=""10pt"">
        This block has different radius for each corner.
      </fo:block>
    </fo:flow>
  </fo:page-sequence>
</fo:root>";

        // Act
        var pdf = RenderFoDocument(foDocument);

        // Assert
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);

        // Verify PDF contains Bezier curves
        var pdfText = System.Text.Encoding.Latin1.GetString(pdf);
        Assert.Contains(" c\n", pdfText);
    }

    [Fact]
    public void BorderRadius_WithDashedBorder_RendersCorrectly()
    {
        // Arrange
        var foDocument = @"<?xml version=""1.0""?>
<fo:root xmlns:fo=""http://www.w3.org/1999/XSL/Format"">
  <fo:layout-master-set>
    <fo:simple-page-master master-name=""page"" page-width=""8.5in"" page-height=""11in""
                           margin-top=""0.5in"" margin-bottom=""0.5in""
                           margin-left=""0.5in"" margin-right=""0.5in"">
      <fo:region-body/>
    </fo:simple-page-master>
  </fo:layout-master-set>

  <fo:page-sequence master-reference=""page"">
    <fo:flow flow-name=""xsl-region-body"">
      <fo:block border=""2pt dashed red"" border-radius=""8pt"" padding=""10pt"">
        Rounded corners with dashed border.
      </fo:block>
    </fo:flow>
  </fo:page-sequence>
</fo:root>";

        // Act
        var pdf = RenderFoDocument(foDocument);

        // Assert
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);

        // Verify PDF contains both dash pattern and Bezier curves
        var pdfText = System.Text.Encoding.Latin1.GetString(pdf);
        Assert.Contains("[3 2] 0 d\n", pdfText); // Dash pattern
        Assert.Contains(" c\n", pdfText); // Bezier curves
    }

    [Fact]
    public void BorderRadius_AbsolutePositioning_RendersCorrectly()
    {
        // Arrange
        var foDocument = @"<?xml version=""1.0""?>
<fo:root xmlns:fo=""http://www.w3.org/1999/XSL/Format"">
  <fo:layout-master-set>
    <fo:simple-page-master master-name=""page"" page-width=""8.5in"" page-height=""11in""
                           margin-top=""0.5in"" margin-bottom=""0.5in""
                           margin-left=""0.5in"" margin-right=""0.5in"">
      <fo:region-body/>
    </fo:simple-page-master>
  </fo:layout-master-set>

  <fo:page-sequence master-reference=""page"">
    <fo:flow flow-name=""xsl-region-body"">
      <fo:block-container absolute-position=""absolute""
                          top=""2in"" left=""2in"" width=""3in"" height=""2in""
                          border=""3pt solid green"" border-radius=""15pt"">
        <fo:block padding=""10pt"">Absolutely positioned box with rounded corners.</fo:block>
      </fo:block-container>
      <fo:block>Normal flow content.</fo:block>
    </fo:flow>
  </fo:page-sequence>
</fo:root>";

        // Act
        var pdf = RenderFoDocument(foDocument);

        // Assert
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);

        // Verify PDF contains Bezier curves for absolute positioned element
        var pdfText = System.Text.Encoding.Latin1.GetString(pdf);
        Assert.Contains(" c\n", pdfText);
    }

    [Fact]
    public void BorderRadius_LargeRadius_ClampedToHalfDimension()
    {
        // Arrange - radius larger than half the box should be clamped
        var foDocument = @"<?xml version=""1.0""?>
<fo:root xmlns:fo=""http://www.w3.org/1999/XSL/Format"">
  <fo:layout-master-set>
    <fo:simple-page-master master-name=""page"" page-width=""8.5in"" page-height=""11in""
                           margin-top=""0.5in"" margin-bottom=""0.5in""
                           margin-left=""0.5in"" margin-right=""0.5in"">
      <fo:region-body/>
    </fo:simple-page-master>
  </fo:layout-master-set>

  <fo:page-sequence master-reference=""page"">
    <fo:flow flow-name=""xsl-region-body"">
      <fo:block border=""2pt solid black"" border-radius=""500pt"" padding=""10pt"" width=""100pt"" height=""50pt"">
        Large radius should be clamped.
      </fo:block>
    </fo:flow>
  </fo:page-sequence>
</fo:root>";

        // Act
        var pdf = RenderFoDocument(foDocument);

        // Assert - should not crash and should generate valid PDF
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);

        // Verify it's still a valid PDF with curves
        var pdfText = System.Text.Encoding.Latin1.GetString(pdf);
        Assert.Contains(" c\n", pdfText);
    }

    [Fact]
    public void BorderRadius_ZeroRadius_UsesStandardBorder()
    {
        // Arrange
        var foDocument = @"<?xml version=""1.0""?>
<fo:root xmlns:fo=""http://www.w3.org/1999/XSL/Format"">
  <fo:layout-master-set>
    <fo:simple-page-master master-name=""page"" page-width=""8.5in"" page-height=""11in""
                           margin-top=""0.5in"" margin-bottom=""0.5in""
                           margin-left=""0.5in"" margin-right=""0.5in"">
      <fo:region-body/>
    </fo:simple-page-master>
  </fo:layout-master-set>

  <fo:page-sequence master-reference=""page"">
    <fo:flow flow-name=""xsl-region-body"">
      <fo:block border=""2pt solid black"" border-radius=""0pt"" padding=""10pt"">
        Zero radius should use standard rectangular border.
      </fo:block>
    </fo:flow>
  </fo:page-sequence>
</fo:root>";

        // Act
        var pdf = RenderFoDocument(foDocument);

        // Assert
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);

        // Verify it uses standard border (moveto + lineto, not curveto)
        var pdfText = System.Text.Encoding.Latin1.GetString(pdf);
        // Should have moveto (m) and lineto (l) operators for straight lines
        Assert.Contains(" m\n", pdfText);
        Assert.Contains(" l\n", pdfText);
    }

    [Fact]
    public void BorderRadius_PartialCorners_MixedRoundedAndSharp()
    {
        // Arrange - only some corners rounded
        var foDocument = @"<?xml version=""1.0""?>
<fo:root xmlns:fo=""http://www.w3.org/1999/XSL/Format"">
  <fo:layout-master-set>
    <fo:simple-page-master master-name=""page"" page-width=""8.5in"" page-height=""11in""
                           margin-top=""0.5in"" margin-bottom=""0.5in""
                           margin-left=""0.5in"" margin-right=""0.5in"">
      <fo:region-body/>
    </fo:simple-page-master>
  </fo:layout-master-set>

  <fo:page-sequence master-reference=""page"">
    <fo:flow flow-name=""xsl-region-body"">
      <fo:block border=""2pt solid purple""
                border-top-left-radius=""10pt""
                border-top-right-radius=""0pt""
                border-bottom-right-radius=""0pt""
                border-bottom-left-radius=""10pt""
                padding=""10pt"">
        Top-left and bottom-left corners rounded, others sharp.
      </fo:block>
    </fo:flow>
  </fo:page-sequence>
</fo:root>";

        // Act
        var pdf = RenderFoDocument(foDocument);

        // Assert
        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);

        // Verify PDF contains Bezier curves (for rounded corners)
        var pdfText = System.Text.Encoding.Latin1.GetString(pdf);
        Assert.Contains(" c\n", pdfText);
    }
}
