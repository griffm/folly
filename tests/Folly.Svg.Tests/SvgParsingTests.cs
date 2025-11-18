using Folly.Svg;
using Xunit;

namespace Folly.Svg.Tests;

/// <summary>
/// Tests for SVG parsing functionality.
/// These tests prove that Folly.Svg can be used independently without any dependencies on Folly.Core or Folly.Pdf.
/// </summary>
public class SvgParsingTests
{
    [Fact]
    public void Parse_SimpleRect_ParsesSuccessfully()
    {
        // Arrange
        var svg = @"<svg width=""100"" height=""100"" xmlns=""http://www.w3.org/2000/svg"">
                      <rect x=""10"" y=""10"" width=""80"" height=""80"" fill=""blue""/>
                    </svg>";

        // Act
        var document = SvgDocument.Parse(svg);

        // Assert
        Assert.NotNull(document);
        Assert.NotNull(document.Root);
        Assert.Equal("svg", document.Root.ElementType);
        Assert.Single(document.Root.Children);

        var rect = document.Root.Children[0];
        Assert.Equal("rect", rect.ElementType);
        Assert.Equal("10", rect.GetAttribute("x"));
        Assert.Equal("10", rect.GetAttribute("y"));
        Assert.Equal("80", rect.GetAttribute("width"));
        Assert.Equal("80", rect.GetAttribute("height"));
    }

    [Fact]
    public void Parse_WithViewBox_ParsesViewBox()
    {
        // Arrange
        var svg = @"<svg viewBox=""0 0 200 200"" xmlns=""http://www.w3.org/2000/svg"">
                      <circle cx=""100"" cy=""100"" r=""50""/>
                    </svg>";

        // Act
        var document = SvgDocument.Parse(svg);

        // Assert
        Assert.NotNull(document.ViewBox);
        Assert.Equal(0, document.ViewBox.MinX);
        Assert.Equal(0, document.ViewBox.MinY);
        Assert.Equal(200, document.ViewBox.Width);
        Assert.Equal(200, document.ViewBox.Height);
    }

    [Fact]
    public void Parse_Path_ParsesPathData()
    {
        // Arrange
        var svg = @"<svg xmlns=""http://www.w3.org/2000/svg"">
                      <path d=""M 10 10 L 90 90"" stroke=""black""/>
                    </svg>";

        // Act
        var document = SvgDocument.Parse(svg);

        // Assert
        var path = document.Root.Children[0];
        Assert.Equal("path", path.ElementType);
        Assert.Equal("M 10 10 L 90 90", path.GetAttribute("d"));
    }

    [Fact]
    public void Parse_Group_ParsesHierarchy()
    {
        // Arrange
        var svg = @"<svg xmlns=""http://www.w3.org/2000/svg"">
                      <g id=""group1"">
                        <rect x=""0"" y=""0"" width=""50"" height=""50""/>
                        <circle cx=""75"" cy=""25"" r=""25""/>
                      </g>
                    </svg>";

        // Act
        var document = SvgDocument.Parse(svg);

        // Assert
        var group = document.Root.Children[0];
        Assert.Equal("g", group.ElementType);
        Assert.Equal("group1", group.Id);
        Assert.Equal(2, group.Children.Count);
        Assert.Equal("rect", group.Children[0].ElementType);
        Assert.Equal("circle", group.Children[1].ElementType);
    }

    [Fact]
    public void ParseColor_Hex_ParsesCorrectly()
    {
        // Arrange & Act
        var color3 = SvgColorParser.ParseHex("#F00");
        var color6 = SvgColorParser.ParseHex("#FF0000");

        // Assert
        Assert.NotNull(color3);
        Assert.Equal(1.0, color3.Value.r, precision: 3);
        Assert.Equal(0.0, color3.Value.g, precision: 3);
        Assert.Equal(0.0, color3.Value.b, precision: 3);

        Assert.NotNull(color6);
        Assert.Equal(1.0, color6.Value.r, precision: 3);
        Assert.Equal(0.0, color6.Value.g, precision: 3);
        Assert.Equal(0.0, color6.Value.b, precision: 3);
    }

    [Fact]
    public void ParseColor_Rgb_ParsesCorrectly()
    {
        // Arrange & Act
        var color = SvgColorParser.ParseRgb("rgb(255, 128, 0)");

        // Assert
        Assert.NotNull(color);
        Assert.Equal(1.0, color.Value.r, precision: 3);
        Assert.Equal(0.502, color.Value.g, precision: 3);
        Assert.Equal(0.0, color.Value.b, precision: 3);
    }

    [Fact]
    public void ParseTransform_Matrix_ParsesCorrectly()
    {
        // Arrange
        var transformStr = "matrix(1, 0, 0, 1, 10, 20)";

        // Act
        var transform = SvgTransformParser.Parse(transformStr);

        // Assert
        Assert.NotNull(transform);
        Assert.Equal(1, transform.A);
        Assert.Equal(0, transform.B);
        Assert.Equal(0, transform.C);
        Assert.Equal(1, transform.D);
        Assert.Equal(10, transform.E);
        Assert.Equal(20, transform.F);
    }

    [Fact]
    public void ParseTransform_Translate_ParsesCorrectly()
    {
        // Arrange
        var transformStr = "translate(10, 20)";

        // Act
        var transform = SvgTransformParser.Parse(transformStr);

        // Assert
        Assert.NotNull(transform);
        Assert.Equal(10, transform.E);
        Assert.Equal(20, transform.F);
    }

    [Fact]
    public void ToPdf_SimpleRect_GeneratesPdfCommands()
    {
        // Arrange
        var svg = @"<svg width=""100"" height=""100"" xmlns=""http://www.w3.org/2000/svg"">
                      <rect x=""10"" y=""10"" width=""80"" height=""80"" fill=""blue""/>
                    </svg>";
        var document = SvgDocument.Parse(svg);

        // Act
        var converter = new SvgToPdfConverter(document);
        var result = converter.Convert();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ContentStream);
        Assert.Contains("q", result.ContentStream); // Save state
        Assert.Contains("Q", result.ContentStream); // Restore state
        Assert.Contains("cm", result.ContentStream); // Coordinate transformation
        Assert.Contains("re", result.ContentStream); // Rectangle
    }
}
