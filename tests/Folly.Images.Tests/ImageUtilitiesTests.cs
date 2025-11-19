using Folly.Images;
using Xunit;

namespace Folly.Images.Tests;

public class ImageUtilitiesTests
{
    [Fact]
    public void PixelsToPoints_At72Dpi_ReturnsEqualValue()
    {
        // Arrange
        double pixels = 72;
        double dpi = 72;

        // Act
        double points = ImageUtilities.PixelsToPoints(pixels, dpi);

        // Assert
        Assert.Equal(72, points);
    }

    [Fact]
    public void PixelsToPoints_At144Dpi_ReturnsHalfValue()
    {
        // Arrange
        double pixels = 144;
        double dpi = 144;

        // Act
        double points = ImageUtilities.PixelsToPoints(pixels, dpi);

        // Assert
        Assert.Equal(72, points);
    }

    [Fact]
    public void PixelsToPoints_At300Dpi_ConvertsCorrectly()
    {
        // Arrange
        double pixels = 300;
        double dpi = 300;

        // Act
        double points = ImageUtilities.PixelsToPoints(pixels, dpi);

        // Assert
        Assert.Equal(72, points, precision: 5);
    }

    [Fact]
    public void PixelsToPoints_WithZeroDpi_UsesDefaultDpi()
    {
        // Arrange
        double pixels = 100;
        double dpi = 0;
        double defaultDpi = 72;

        // Act
        double points = ImageUtilities.PixelsToPoints(pixels, dpi, defaultDpi);

        // Assert
        Assert.Equal(100, points);
    }

    [Fact]
    public void PixelsToPoints_WithNegativeDpi_UsesDefaultDpi()
    {
        // Arrange
        double pixels = 100;
        double dpi = -10;
        double defaultDpi = 96;

        // Act
        double points = ImageUtilities.PixelsToPoints(pixels, dpi, defaultDpi);

        // Assert
        Assert.Equal(75, points);
    }

    [Fact]
    public void PixelsToPoints_WithCustomDefaultDpi_UsesCustomDefault()
    {
        // Arrange
        double pixels = 96;
        double dpi = 0;
        double defaultDpi = 96;

        // Act
        double points = ImageUtilities.PixelsToPoints(pixels, dpi, defaultDpi);

        // Assert
        Assert.Equal(72, points);
    }

    [Theory]
    [InlineData(72, 72, 72)]
    [InlineData(144, 144, 72)]
    [InlineData(300, 300, 72)]
    [InlineData(600, 600, 72)]
    public void PixelsToPoints_VariousDpi_ConvertsToPointsCorrectly(double pixels, double dpi, double expectedPoints)
    {
        // Act
        double points = ImageUtilities.PixelsToPoints(pixels, dpi);

        // Assert
        Assert.Equal(expectedPoints, points, precision: 5);
    }

    [Fact]
    public void GetIntrinsicSizeInPoints_At72Dpi_ReturnsEqualDimensions()
    {
        // Arrange
        var imageInfo = new ImageInfo
        {
            Format = "TEST",
            Width = 144,
            Height = 72,
            HorizontalDpi = 72,
            VerticalDpi = 72
        };

        // Act
        var (width, height) = ImageUtilities.GetIntrinsicSizeInPoints(imageInfo);

        // Assert
        Assert.Equal(144, width);
        Assert.Equal(72, height);
    }

    [Fact]
    public void GetIntrinsicSizeInPoints_At300Dpi_ConvertsCorrectly()
    {
        // Arrange
        var imageInfo = new ImageInfo
        {
            Format = "TEST",
            Width = 300,
            Height = 600,
            HorizontalDpi = 300,
            VerticalDpi = 300
        };

        // Act
        var (width, height) = ImageUtilities.GetIntrinsicSizeInPoints(imageInfo);

        // Assert
        Assert.Equal(72, width, precision: 5);
        Assert.Equal(144, height, precision: 5);
    }

    [Fact]
    public void GetIntrinsicSizeInPoints_WithDifferentDpi_ConvertsEachAxisCorrectly()
    {
        // Arrange
        var imageInfo = new ImageInfo
        {
            Format = "TEST",
            Width = 300,
            Height = 144,
            HorizontalDpi = 300,
            VerticalDpi = 72
        };

        // Act
        var (width, height) = ImageUtilities.GetIntrinsicSizeInPoints(imageInfo);

        // Assert
        Assert.Equal(72, width, precision: 5);
        Assert.Equal(144, height, precision: 5);
    }

    [Fact]
    public void GetIntrinsicSizeInPoints_WithZeroHorizontalDpi_UsesDefaultDpi()
    {
        // Arrange
        var imageInfo = new ImageInfo
        {
            Format = "TEST",
            Width = 100,
            Height = 100,
            HorizontalDpi = 0,
            VerticalDpi = 72
        };

        // Act
        var (width, height) = ImageUtilities.GetIntrinsicSizeInPoints(imageInfo, defaultDpi: 72);

        // Assert
        Assert.Equal(100, width);
        Assert.Equal(100, height);
    }

    [Fact]
    public void GetIntrinsicSizeInPoints_WithZeroVerticalDpi_UsesHorizontalDpi()
    {
        // Arrange
        var imageInfo = new ImageInfo
        {
            Format = "TEST",
            Width = 144,
            Height = 144,
            HorizontalDpi = 144,
            VerticalDpi = 0
        };

        // Act
        var (width, height) = ImageUtilities.GetIntrinsicSizeInPoints(imageInfo);

        // Assert
        Assert.Equal(72, width);
        Assert.Equal(72, height); // Should use HorizontalDpi for vertical as well
    }

    [Fact]
    public void GetIntrinsicSizeInPoints_WithBothDpiZero_UsesDefaultDpi()
    {
        // Arrange
        var imageInfo = new ImageInfo
        {
            Format = "TEST",
            Width = 96,
            Height = 96,
            HorizontalDpi = 0,
            VerticalDpi = 0
        };

        // Act
        var (width, height) = ImageUtilities.GetIntrinsicSizeInPoints(imageInfo, defaultDpi: 96);

        // Assert
        Assert.Equal(72, width);
        Assert.Equal(72, height);
    }

    [Fact]
    public void GetIntrinsicSizeInPoints_WithCustomDefaultDpi_UsesCustomDefault()
    {
        // Arrange
        var imageInfo = new ImageInfo
        {
            Format = "TEST",
            Width = 100,
            Height = 100,
            HorizontalDpi = 0,
            VerticalDpi = 0
        };

        // Act
        var (width, height) = ImageUtilities.GetIntrinsicSizeInPoints(imageInfo, defaultDpi: 100);

        // Assert
        Assert.Equal(72, width);
        Assert.Equal(72, height);
    }

    [Theory]
    [InlineData(300, 300, 300, 300, 72, 72)]
    [InlineData(600, 400, 300, 200, 144, 144)]
    [InlineData(1200, 1200, 600, 600, 144, 144)]
    public void GetIntrinsicSizeInPoints_VariousInputs_ConvertsCorrectly(
        int widthPx, int heightPx, double dpiX, double dpiY, double expectedWidthPt, double expectedHeightPt)
    {
        // Arrange
        var imageInfo = new ImageInfo
        {
            Format = "TEST",
            Width = widthPx,
            Height = heightPx,
            HorizontalDpi = dpiX,
            VerticalDpi = dpiY
        };

        // Act
        var (width, height) = ImageUtilities.GetIntrinsicSizeInPoints(imageInfo);

        // Assert
        Assert.Equal(expectedWidthPt, width, precision: 5);
        Assert.Equal(expectedHeightPt, height, precision: 5);
    }

    [Fact]
    public void PixelsToPoints_WithLargePixelValue_HandlesCorrectly()
    {
        // Arrange
        double pixels = 10000;
        double dpi = 300;

        // Act
        double points = ImageUtilities.PixelsToPoints(pixels, dpi);

        // Assert
        Assert.Equal(2400, points);
    }

    [Fact]
    public void PixelsToPoints_WithSmallPixelValue_HandlesCorrectly()
    {
        // Arrange
        double pixels = 1;
        double dpi = 72;

        // Act
        double points = ImageUtilities.PixelsToPoints(pixels, dpi);

        // Assert
        Assert.Equal(1, points);
    }

    [Fact]
    public void PixelsToPoints_WithFractionalPixels_HandlesCorrectly()
    {
        // Arrange
        double pixels = 150.5;
        double dpi = 150;

        // Act
        double points = ImageUtilities.PixelsToPoints(pixels, dpi);

        // Assert
        Assert.Equal(72.24, points, precision: 2);
    }
}
