using Folly.Images;
using Folly.Images.Parsers;
using Folly.Images.Tests.Helpers;
using Xunit;

namespace Folly.Images.Tests;

/// <summary>
/// Tests for DPI detection and image scaling (Phase 9.3).
/// Priority 1 (Critical) - 10 tests.
/// </summary>
public class ImageDpiTests
{
    [Fact]
    public void DetectJpegDpi_JFIF_72Dpi()
    {
        // Arrange
        var imageBytes = TestResourceLocator.LoadImage("test-72dpi.jpg");
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(imageBytes);

        // Assert
        Assert.Equal(72, info.HorizontalDpi);
        Assert.Equal(72, info.VerticalDpi);
    }

    [Fact]
    public void DetectJpegDpi_JFIF_96Dpi()
    {
        // Arrange
        var imageBytes = TestResourceLocator.LoadImage("test-96dpi.jpg");
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(imageBytes);

        // Assert
        Assert.Equal(96, info.HorizontalDpi);
        Assert.Equal(96, info.VerticalDpi);
    }

    [Fact]
    public void DetectJpegDpi_JFIF_150Dpi()
    {
        // Arrange
        var imageBytes = TestResourceLocator.LoadImage("test-150dpi.jpg");
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(imageBytes);

        // Assert
        Assert.Equal(150, info.HorizontalDpi);
        Assert.Equal(150, info.VerticalDpi);
    }

    [Fact]
    public void DetectJpegDpi_JFIF_300Dpi()
    {
        // Arrange
        var imageBytes = TestResourceLocator.LoadImage("test-300dpi.jpg");
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(imageBytes);

        // Assert
        Assert.Equal(300, info.HorizontalDpi);
        Assert.Equal(300, info.VerticalDpi);
    }

    [Fact]
    public void DetectPngDpi_pHYs()
    {
        // Arrange
        var imageBytes = TestResourceLocator.LoadImage("test-300dpi.png");
        var parser = new PngParser();

        // Act
        var info = parser.Parse(imageBytes);

        // Assert
        Assert.Equal(300, info.HorizontalDpi, precision: 1);
        Assert.Equal(300, info.VerticalDpi, precision: 1);
    }

    [Fact]
    public void DefaultDpi_WhenNoDpiMetadata()
    {
        // Arrange
        var imageBytes = TestResourceLocator.LoadImage("no-dpi-metadata.jpg");
        var parser = new JpegParser();

        // Act
        var info = parser.Parse(imageBytes);

        // Assert
        Assert.Equal(72, info.HorizontalDpi); // Default DPI
        Assert.Equal(72, info.VerticalDpi);
    }

    [Fact(Skip = "Implementation pending")]
    public void ConfigurableDefaultDpi()
    {
        // TODO: Test that LayoutOptions.DefaultImageDpi is respected
        // when image has no DPI metadata
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Implementation pending")]
    public void ImageSizing_RespectsDpi_300Dpi()
    {
        // TODO: Test that 300x300px at 300 DPI = 72pt x 72pt (1 inch)
        // Requires integration with layout engine
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Implementation pending")]
    public void ImageSizing_RespectsDpi_96Dpi()
    {
        // TODO: Test that 96x96px at 96 DPI = 72pt (1 inch)
        Assert.True(true, "Not yet implemented");
    }

    [Fact(Skip = "Implementation pending")]
    public void ImageSizing_DpiInPdf()
    {
        // TODO: Verify PDF XObject has correct dimensions based on DPI
        Assert.True(true, "Not yet implemented");
    }
}
