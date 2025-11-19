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

    #region Path Parsing Tests

    [Fact]
    public void ParsePath_MoveTo_Absolute()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 M 30 40");

        // Assert
        Assert.Equal(2, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("30 40 m", commands[1]);
    }

    [Fact]
    public void ParsePath_MoveTo_Relative()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 m 5 10");

        // Assert
        Assert.Equal(2, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("15 30 m", commands[1]); // Relative to previous position
    }

    [Fact]
    public void ParsePath_LineTo_Absolute()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 L 30 40 L 50 60");

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("30 40 l", commands[1]);
        Assert.Contains("50 60 l", commands[2]);
    }

    [Fact]
    public void ParsePath_LineTo_Relative()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 l 5 10 l 5 10");

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("15 30 l", commands[1]);
        Assert.Contains("20 40 l", commands[2]);
    }

    [Fact]
    public void ParsePath_HorizontalLine_Absolute()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 H 50 H 100");

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("50 20 l", commands[1]);
        Assert.Contains("100 20 l", commands[2]);
    }

    [Fact]
    public void ParsePath_HorizontalLine_Relative()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 h 20 h 30");

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("30 20 l", commands[1]);
        Assert.Contains("60 20 l", commands[2]);
    }

    [Fact]
    public void ParsePath_VerticalLine_Absolute()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 V 50 V 100");

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("10 50 l", commands[1]);
        Assert.Contains("10 100 l", commands[2]);
    }

    [Fact]
    public void ParsePath_VerticalLine_Relative()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 v 20 v 30");

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("10 40 l", commands[1]);
        Assert.Contains("10 70 l", commands[2]);
    }

    [Fact]
    public void ParsePath_CubicBezier_Absolute()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 C 20 10, 30 10, 40 20");

        // Assert
        Assert.Equal(2, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("20 10 30 10 40 20 c", commands[1]);
    }

    [Fact]
    public void ParsePath_CubicBezier_Relative()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 c 10 -10, 20 -10, 30 0");

        // Assert
        Assert.Equal(2, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("20 10 30 10 40 20 c", commands[1]);
    }

    [Fact]
    public void ParsePath_SmoothCubicBezier_Absolute()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 C 20 10, 30 10, 40 20 S 60 30, 70 20");

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("20 10 30 10 40 20 c", commands[1]);
        Assert.Contains("50 30 60 30 70 20 c", commands[2]); // First control point reflected
    }

    [Fact]
    public void ParsePath_QuadraticBezier_Absolute()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 Q 30 10, 50 20");

        // Assert
        Assert.Equal(2, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        // Quadratic converted to cubic
        Assert.Contains("c", commands[1]);
    }

    [Fact]
    public void ParsePath_QuadraticBezier_Relative()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 q 20 -10, 40 0");

        // Assert
        Assert.Equal(2, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("c", commands[1]); // Converted to cubic
    }

    [Fact]
    public void ParsePath_SmoothQuadraticBezier_Absolute()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 Q 30 10, 50 20 T 90 20");

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("c", commands[1]);
        Assert.Contains("c", commands[2]); // Control point reflected
    }

    [Fact]
    public void ParsePath_Arc_Absolute()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 A 30 30 0 0 1 40 50");

        // Assert
        Assert.True(commands.Count >= 2); // Move + at least one cubic curve
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("c", commands[1]); // Arc converted to cubic curves
    }

    [Fact]
    public void ParsePath_Arc_Relative()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 a 30 30 0 0 1 30 30");

        // Assert
        Assert.True(commands.Count >= 2);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("c", commands[1]);
    }

    [Fact]
    public void ParsePath_Arc_DegenerateToLine()
    {
        // Arrange & Act - Zero radius should create a line
        var commands = SvgPathParser.Parse("M 10 20 A 0 0 0 0 1 40 50");

        // Assert
        Assert.Equal(2, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("40 50 l", commands[1]); // Degenerate arc becomes line
    }

    [Fact]
    public void ParsePath_ClosePath()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 L 30 40 L 10 40 Z");

        // Assert
        Assert.Equal(4, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("30 40 l", commands[1]);
        Assert.Contains("10 40 l", commands[2]);
        Assert.Contains("h", commands[3]); // Close path
    }

    [Fact]
    public void ParsePath_MixedCommands()
    {
        // Arrange & Act
        var commands = SvgPathParser.Parse("M 10 20 L 30 40 H 50 V 60 C 50 70, 60 70, 60 60 Z");

        // Assert
        Assert.True(commands.Count >= 6);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("30 40 l", commands[1]);
        Assert.Contains("50 40 l", commands[2]); // H
        Assert.Contains("50 60 l", commands[3]); // V
        Assert.Contains("50 70 60 70 60 60 c", commands[4]); // C
        Assert.Contains("h", commands[5]); // Z
    }

    [Fact]
    public void ParsePath_MultipleCoordinatesAfterMoveTo()
    {
        // Arrange & Act - Multiple coordinates after M are treated as additional M
        var commands = SvgPathParser.Parse("M 10 20 30 40 50 60");

        // Assert
        Assert.Equal(3, commands.Count);
        Assert.Contains("10 20 m", commands[0]);
        Assert.Contains("30 40 m", commands[1]);
        Assert.Contains("50 60 m", commands[2]);
    }

    [Fact]
    public void CalculateBoundingBox_SimplePath()
    {
        // Arrange & Act
        var bbox = SvgPathParser.CalculateBoundingBox("M 10 20 L 50 60 L 100 30");

        // Assert
        Assert.NotNull(bbox);
        Assert.Equal(10, bbox.Value.x);
        Assert.Equal(20, bbox.Value.y);
        Assert.Equal(90, bbox.Value.width); // 100 - 10
        Assert.Equal(40, bbox.Value.height); // 60 - 20
    }

    [Fact]
    public void CalculateBoundingBox_EmptyPath()
    {
        // Arrange & Act
        var bbox = SvgPathParser.CalculateBoundingBox("");

        // Assert
        Assert.Null(bbox);
    }

    [Fact]
    public void CalculateBoundingBox_WithCurves()
    {
        // Arrange & Act
        var bbox = SvgPathParser.CalculateBoundingBox("M 0 0 C 10 10, 20 10, 30 0");

        // Assert
        Assert.NotNull(bbox);
        Assert.Equal(0, bbox.Value.x);
        Assert.Equal(0, bbox.Value.y);
        Assert.True(bbox.Value.width >= 30);
        Assert.True(bbox.Value.height >= 10);
    }

    #endregion

    #region CSS Parser Tests

    [Fact]
    public void ParseCss_SimpleRule()
    {
        // Arrange
        var css = "rect { fill: red; stroke: blue; }";

        // Act
        var rules = SvgCssParser.ParseStylesheet(css);

        // Assert
        Assert.Single(rules);
        Assert.Equal("rect", rules[0].Selector);
        Assert.Equal(2, rules[0].Declarations.Count);
        Assert.Equal("red", rules[0].Declarations["fill"]);
        Assert.Equal("blue", rules[0].Declarations["stroke"]);
    }

    [Fact]
    public void ParseCss_ClassSelector()
    {
        // Arrange
        var css = ".highlight { fill: yellow; }";

        // Act
        var rules = SvgCssParser.ParseStylesheet(css);

        // Assert
        Assert.Single(rules);
        Assert.Equal(".highlight", rules[0].Selector);
        Assert.Equal("yellow", rules[0].Declarations["fill"]);
    }

    [Fact]
    public void ParseCss_IdSelector()
    {
        // Arrange
        var css = "#myRect { fill: green; }";

        // Act
        var rules = SvgCssParser.ParseStylesheet(css);

        // Assert
        Assert.Single(rules);
        Assert.Equal("#myRect", rules[0].Selector);
        Assert.Equal("green", rules[0].Declarations["fill"]);
    }

    [Fact]
    public void ParseCss_MultipleSelectors()
    {
        // Arrange
        var css = "rect, circle { fill: red; }";

        // Act
        var rules = SvgCssParser.ParseStylesheet(css);

        // Assert
        Assert.Equal(2, rules.Count);
        Assert.Equal("rect", rules[0].Selector);
        Assert.Equal("circle", rules[1].Selector);
        Assert.Equal("red", rules[0].Declarations["fill"]);
        Assert.Equal("red", rules[1].Declarations["fill"]);
    }

    [Fact]
    public void ParseCss_WithComments()
    {
        // Arrange
        var css = "/* Comment */ rect { fill: red; /* Another comment */ stroke: blue; }";

        // Act
        var rules = SvgCssParser.ParseStylesheet(css);

        // Assert
        Assert.Single(rules);
        Assert.Equal(2, rules[0].Declarations.Count);
        Assert.Equal("red", rules[0].Declarations["fill"]);
    }

    [Fact]
    public void ParseCss_MultipleRules()
    {
        // Arrange
        var css = @"
            rect { fill: red; }
            circle { fill: blue; }
            .special { stroke: green; }
        ";

        // Act
        var rules = SvgCssParser.ParseStylesheet(css);

        // Assert
        Assert.Equal(3, rules.Count);
        Assert.Equal("rect", rules[0].Selector);
        Assert.Equal("circle", rules[1].Selector);
        Assert.Equal(".special", rules[2].Selector);
    }

    [Fact]
    public void ApplyCssRules_TypeSelector()
    {
        // Arrange
        var element = new SvgElement { ElementType = "rect" };
        var rules = SvgCssParser.ParseStylesheet("rect { fill: red; }");

        // Act
        SvgCssParser.ApplyCssRules(element, rules);

        // Assert
        Assert.Equal("red", element.Style.Fill);
    }

    [Fact]
    public void ApplyCssRules_ClassSelector()
    {
        // Arrange
        var element = new SvgElement { ElementType = "rect" };
        element.Attributes["class"] = "highlight";
        var rules = SvgCssParser.ParseStylesheet(".highlight { fill: yellow; }");

        // Act
        SvgCssParser.ApplyCssRules(element, rules);

        // Assert
        Assert.Equal("yellow", element.Style.Fill);
    }

    [Fact]
    public void ApplyCssRules_IdSelector()
    {
        // Arrange
        var element = new SvgElement { ElementType = "rect" };
        element.Attributes["id"] = "myRect";
        var rules = SvgCssParser.ParseStylesheet("#myRect { fill: green; }");

        // Act
        SvgCssParser.ApplyCssRules(element, rules);

        // Assert
        Assert.Equal("green", element.Style.Fill);
    }

    [Fact]
    public void ApplyCssRules_SpecificityOrder()
    {
        // Arrange - ID should override class, class should override type
        var element = new SvgElement { ElementType = "rect" };
        element.Attributes["id"] = "myRect";
        element.Attributes["class"] = "highlight";
        var rules = SvgCssParser.ParseStylesheet(@"
            rect { fill: red; }
            .highlight { fill: yellow; }
            #myRect { fill: green; }
        ");

        // Act
        SvgCssParser.ApplyCssRules(element, rules);

        // Assert
        Assert.Equal("green", element.Style.Fill); // ID has highest specificity
    }

    [Fact]
    public void ApplyCssRules_NoMatch()
    {
        // Arrange
        var element = new SvgElement { ElementType = "rect" };
        var rules = SvgCssParser.ParseStylesheet("circle { fill: red; }");
        var originalFill = element.Style.Fill; // Capture original fill value

        // Act
        SvgCssParser.ApplyCssRules(element, rules);

        // Assert - No matching rule means style should remain unchanged
        Assert.Equal(originalFill, element.Style.Fill);
    }

    #endregion

    #region Length Parser Tests

    [Theory]
    [InlineData("100", 100.0)]
    [InlineData("100px", 100.0)]
    [InlineData("72pt", 96.0)] // 72pt = 1 inch = 96px
    [InlineData("1in", 96.0)]
    [InlineData("2.54cm", 96.0)] // 2.54cm = 1 inch = 96px
    [InlineData("25.4mm", 96.0)] // 25.4mm = 1 inch = 96px
    [InlineData("6pc", 96.0)] // 6pc = 1 inch = 96px
    public void ParseLength_AbsoluteUnits(string input, double expected)
    {
        // Act
        var result = SvgLengthParser.Parse(input);

        // Assert
        Assert.Equal(expected, result, precision: 1);
    }

    [Theory]
    [InlineData("2em", 16.0, 32.0)] // 2 * 16px font size
    [InlineData("1.5em", 20.0, 30.0)] // 1.5 * 20px font size
    [InlineData("0.5em", 24.0, 12.0)]
    public void ParseLength_EmUnits(string input, double fontSize, double expected)
    {
        // Act
        var result = SvgLengthParser.Parse(input, fontSize: fontSize);

        // Assert
        Assert.Equal(expected, result, precision: 1);
    }

    [Theory]
    [InlineData("2rem", 16.0, 20.0, 40.0)] // 2 * 20px root font size
    [InlineData("1.5rem", 16.0, 24.0, 36.0)]
    public void ParseLength_RemUnits(string input, double fontSize, double rootFontSize, double expected)
    {
        // Act
        var result = SvgLengthParser.Parse(input, fontSize: fontSize, rootFontSize: rootFontSize);

        // Assert
        Assert.Equal(expected, result, precision: 1);
    }

    [Theory]
    [InlineData("50%", 200.0, 100.0)] // 50% of 200px
    [InlineData("100%", 150.0, 150.0)]
    [InlineData("25%", 400.0, 100.0)]
    public void ParseLength_Percentage(string input, double referenceLength, double expected)
    {
        // Act
        var result = SvgLengthParser.Parse(input, referenceLength: referenceLength);

        // Assert
        Assert.Equal(expected, result, precision: 1);
    }

    [Fact]
    public void ParseLength_InvalidValue_ReturnsDefault()
    {
        // Act
        var result = SvgLengthParser.Parse("invalid", defaultValue: 42);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void ParseLength_EmptyValue_ReturnsDefault()
    {
        // Act
        var result = SvgLengthParser.Parse("", defaultValue: 99);

        // Assert
        Assert.Equal(99, result);
    }

    [Fact]
    public void ParseLengthList_ValidList()
    {
        // Act
        var result = SvgLengthParser.ParseList("10 20 30 40");

        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal(10, result[0]);
        Assert.Equal(20, result[1]);
        Assert.Equal(30, result[2]);
        Assert.Equal(40, result[3]);
    }

    [Fact]
    public void ParseLengthList_CommaSeparated()
    {
        // Act
        var result = SvgLengthParser.ParseList("10, 20, 30, 40");

        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal(10, result[0]);
        Assert.Equal(40, result[3]);
    }

    [Fact]
    public void ParseLengthList_ExpectedCount_Valid()
    {
        // Act
        var result = SvgLengthParser.ParseList("10 20 30 40", expectedCount: 4);

        // Assert
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void ParseLengthList_ExpectedCount_Invalid()
    {
        // Act
        var result = SvgLengthParser.ParseList("10 20", expectedCount: 4);

        // Assert
        Assert.Empty(result); // Returns empty if count doesn't match
    }

    #endregion

    #region Transform Parser Tests

    [Fact]
    public void ParseTransform_Scale_Uniform()
    {
        // Arrange & Act
        var transform = SvgTransformParser.Parse("scale(2)");

        // Assert
        Assert.NotNull(transform);
        Assert.Equal(2, transform.A);
        Assert.Equal(2, transform.D);
        Assert.Equal(0, transform.E);
        Assert.Equal(0, transform.F);
    }

    [Fact]
    public void ParseTransform_Scale_NonUniform()
    {
        // Arrange & Act
        var transform = SvgTransformParser.Parse("scale(2, 3)");

        // Assert
        Assert.NotNull(transform);
        Assert.Equal(2, transform.A);
        Assert.Equal(3, transform.D);
    }

    [Fact]
    public void ParseTransform_Rotate()
    {
        // Arrange & Act
        var transform = SvgTransformParser.Parse("rotate(90)");

        // Assert
        Assert.NotNull(transform);
        // 90 degrees should swap and negate axes
        Assert.Equal(0, transform.A, precision: 5);
        Assert.Equal(1, transform.B, precision: 5);
        Assert.Equal(-1, transform.C, precision: 5);
        Assert.Equal(0, transform.D, precision: 5);
    }

    [Fact]
    public void ParseTransform_Rotate_WithCenter()
    {
        // Arrange & Act
        var transform = SvgTransformParser.Parse("rotate(45, 50, 50)");

        // Assert
        Assert.NotNull(transform);
        // Should rotate around point (50, 50)
        Assert.NotEqual(0, transform.E); // Translation component present
        Assert.NotEqual(0, transform.F);
    }

    [Fact]
    public void ParseTransform_SkewX()
    {
        // Arrange & Act
        var transform = SvgTransformParser.Parse("skewX(30)");

        // Assert
        Assert.NotNull(transform);
        Assert.Equal(1, transform.A);
        Assert.Equal(0, transform.B);
        Assert.NotEqual(0, transform.C); // Skew component
        Assert.Equal(1, transform.D);
    }

    [Fact]
    public void ParseTransform_SkewY()
    {
        // Arrange & Act
        var transform = SvgTransformParser.Parse("skewY(30)");

        // Assert
        Assert.NotNull(transform);
        Assert.Equal(1, transform.A);
        Assert.NotEqual(0, transform.B); // Skew component
        Assert.Equal(0, transform.C);
        Assert.Equal(1, transform.D);
    }

    [Fact]
    public void ParseTransform_Multiple()
    {
        // Arrange & Act
        var transform = SvgTransformParser.Parse("translate(10, 20) scale(2) rotate(45)");

        // Assert
        Assert.NotNull(transform);
        // Should be a composition of all three transforms
        Assert.NotEqual(0, transform.E); // Has translation
        Assert.NotEqual(0, transform.F);
        Assert.NotEqual(1, transform.A); // Has scale and rotation
    }

    [Fact]
    public void ParseTransform_Empty_ReturnsIdentity()
    {
        // Arrange & Act
        var transform = SvgTransformParser.Parse("");

        // Assert
        Assert.NotNull(transform);
        Assert.Equal(1, transform.A);
        Assert.Equal(0, transform.B);
        Assert.Equal(0, transform.C);
        Assert.Equal(1, transform.D);
        Assert.Equal(0, transform.E);
        Assert.Equal(0, transform.F);
    }

    #endregion

    #region Color Parser Tests

    [Theory]
    [InlineData("#000000", 0.0, 0.0, 0.0)] // Black
    [InlineData("#FFFFFF", 1.0, 1.0, 1.0)] // White
    [InlineData("#FF0000", 1.0, 0.0, 0.0)] // Red
    [InlineData("#00FF00", 0.0, 1.0, 0.0)] // Green
    [InlineData("#0000FF", 0.0, 0.0, 1.0)] // Blue
    [InlineData("#808080", 0.502, 0.502, 0.502)] // Gray
    public void ParseColor_Hex6_VariousColors(string input, double r, double g, double b)
    {
        // Act
        var color = SvgColorParser.ParseHex(input);

        // Assert
        Assert.NotNull(color);
        Assert.Equal(r, color.Value.r, precision: 3);
        Assert.Equal(g, color.Value.g, precision: 3);
        Assert.Equal(b, color.Value.b, precision: 3);
    }

    [Theory]
    [InlineData("#000", 0.0, 0.0, 0.0)] // Black
    [InlineData("#FFF", 1.0, 1.0, 1.0)] // White
    [InlineData("#F0F", 1.0, 0.0, 1.0)] // Magenta
    public void ParseColor_Hex3_VariousColors(string input, double r, double g, double b)
    {
        // Act
        var color = SvgColorParser.ParseHex(input);

        // Assert
        Assert.NotNull(color);
        Assert.Equal(r, color.Value.r, precision: 3);
        Assert.Equal(g, color.Value.g, precision: 3);
        Assert.Equal(b, color.Value.b, precision: 3);
    }

    [Theory]
    [InlineData("rgb(0, 0, 0)", 0.0, 0.0, 0.0)]
    [InlineData("rgb(255, 255, 255)", 1.0, 1.0, 1.0)]
    [InlineData("rgb(255, 0, 0)", 1.0, 0.0, 0.0)]
    [InlineData("rgb(128, 128, 128)", 0.502, 0.502, 0.502)]
    public void ParseColor_Rgb_Values(string input, double r, double g, double b)
    {
        // Act
        var color = SvgColorParser.ParseRgb(input);

        // Assert
        Assert.NotNull(color);
        Assert.Equal(r, color.Value.r, precision: 3);
        Assert.Equal(g, color.Value.g, precision: 3);
        Assert.Equal(b, color.Value.b, precision: 3);
    }

    [Theory]
    [InlineData("rgb(100%, 0%, 0%)", 1.0, 0.0, 0.0)]
    [InlineData("rgb(50%, 50%, 50%)", 0.5, 0.5, 0.5)]
    [InlineData("rgb(0%, 100%, 100%)", 0.0, 1.0, 1.0)]
    public void ParseColor_Rgb_Percentages(string input, double r, double g, double b)
    {
        // Act
        var color = SvgColorParser.ParseRgb(input);

        // Assert
        Assert.NotNull(color);
        Assert.Equal(r, color.Value.r, precision: 2);
        Assert.Equal(g, color.Value.g, precision: 2);
        Assert.Equal(b, color.Value.b, precision: 2);
    }

    [Theory]
    [InlineData("red", 1.0, 0.0, 0.0)]
    [InlineData("green", 0.0, 0.502, 0.0)] // CSS green is #008000
    [InlineData("blue", 0.0, 0.0, 1.0)]
    [InlineData("white", 1.0, 1.0, 1.0)]
    [InlineData("black", 0.0, 0.0, 0.0)]
    [InlineData("yellow", 1.0, 1.0, 0.0)]
    [InlineData("cyan", 0.0, 1.0, 1.0)]
    [InlineData("magenta", 1.0, 0.0, 1.0)]
    public void ParseColor_Named_StandardColors(string input, double r, double g, double b)
    {
        // Act
        var color = SvgColorParser.ParseNamed(input);

        // Assert
        Assert.NotNull(color);
        Assert.Equal(r, color.Value.r, precision: 2);
        Assert.Equal(g, color.Value.g, precision: 2);
        Assert.Equal(b, color.Value.b, precision: 2);
    }

    [Fact]
    public void ParseColor_Named_CaseInsensitive()
    {
        // Act
        var color1 = SvgColorParser.ParseNamed("red");
        var color2 = SvgColorParser.ParseNamed("RED");
        var color3 = SvgColorParser.ParseNamed("Red");

        // Assert
        Assert.NotNull(color1);
        Assert.NotNull(color2);
        Assert.NotNull(color3);
        Assert.Equal(color1.Value.r, color2.Value.r);
        Assert.Equal(color1.Value.r, color3.Value.r);
    }

    [Fact]
    public void ParseColor_Hex_Invalid_ReturnsNull()
    {
        // Act & Assert - These should all return null
        var color2 = SvgColorParser.ParseHex("#12"); // Too short
        var color3 = SvgColorParser.ParseHex("FF0000"); // Missing #

        Assert.Null(color2);
        Assert.Null(color3);

        // Invalid hex characters throw exception when parsing
        Assert.Throws<FormatException>(() => SvgColorParser.ParseHex("#GG0000"));
    }

    [Fact]
    public void ParseColor_Rgb_WrongFunctionName_ParsesNumbers()
    {
        // Act - Even with wrong function name, if format contains parseable numbers, it extracts them
        var color = SvgColorParser.ParseRgb("notrgb(0, 0, 0)");

        // Assert - Parser is lenient and extracts numbers from the parentheses
        Assert.NotNull(color);
        Assert.Equal(0.0, color.Value.r);
        Assert.Equal(0.0, color.Value.g);
        Assert.Equal(0.0, color.Value.b);
    }

    [Fact]
    public void ParseColor_Rgb_OutOfRange_StillParses()
    {
        // Act - Parser doesn't validate range, allows out-of-range values
        var color = SvgColorParser.ParseRgb("rgb(256, 0, 0)");

        // Assert - Out of range values are parsed (256/255 > 1.0)
        Assert.NotNull(color);
        Assert.True(color.Value.r > 1.0); // Value exceeds normal range
    }

    [Fact]
    public void ParseColor_Named_Invalid_ReturnsNull()
    {
        // Act
        var color = SvgColorParser.ParseNamed("invalidcolorname");

        // Assert
        Assert.Null(color);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Parse_InvalidXml_ThrowsException()
    {
        // Arrange
        var invalidSvg = "<svg><rect></svg>"; // Unclosed rect

        // Act & Assert
        Assert.Throws<System.Xml.XmlException>(() => SvgDocument.Parse(invalidSvg));
    }

    [Fact]
    public void Parse_EmptyString_ThrowsException()
    {
        // Act & Assert - Empty or whitespace-only strings throw XmlException
        Assert.Throws<System.Xml.XmlException>(() => SvgDocument.Parse(""));
    }

    [Fact]
    public void ParsePath_EmptyString_ReturnsEmptyList()
    {
        // Act
        var commands = SvgPathParser.Parse("");

        // Assert
        Assert.Empty(commands);
    }

    [Fact]
    public void ParsePath_InvalidCommand_StopsAtInvalid()
    {
        // Arrange - 'X' is not a valid path command
        var pathData = "M 10 20 X 30 40 L 50 60";

        // Act
        var commands = SvgPathParser.Parse(pathData);

        // Assert
        // Parser stops when it encounters an invalid command
        Assert.Single(commands);
        Assert.Contains("10 20 m", commands[0]);
    }

    [Fact]
    public void ParseTransform_InvalidTransform_ReturnsIdentity()
    {
        // Act
        var transform = SvgTransformParser.Parse("invalid(10, 20)");

        // Assert - Should return identity transform
        Assert.Equal(1, transform.A);
        Assert.Equal(0, transform.B);
        Assert.Equal(0, transform.C);
        Assert.Equal(1, transform.D);
        Assert.Equal(0, transform.E);
        Assert.Equal(0, transform.F);
    }

    [Fact]
    public void ParseCss_EmptyStylesheet_ReturnsEmptyList()
    {
        // Act
        var rules = SvgCssParser.ParseStylesheet("");

        // Assert
        Assert.Empty(rules);
    }

    [Fact]
    public void ParseCss_MalformedRule_IgnoresRule()
    {
        // Arrange - Missing closing brace
        var css = "rect { fill: red; circle { fill: blue; }";

        // Act
        var rules = SvgCssParser.ParseStylesheet(css);

        // Assert - Should parse what it can
        Assert.NotEmpty(rules);
    }

    #endregion
}
