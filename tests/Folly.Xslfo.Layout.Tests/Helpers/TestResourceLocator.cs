namespace Folly.Xslfo.Layout.Tests.Helpers;

/// <summary>
/// Helper utilities for locating and loading test resources (fonts, images).
/// </summary>
public static class TestResourceLocator
{
    private static readonly string TestImagesPath = Path.Combine(
        AppContext.BaseDirectory,
        "TestResources",
        "Images");

    private static readonly string LegacyTestImagesPath = Path.Combine(
        AppContext.BaseDirectory,
        "test-images");

    /// <summary>
    /// Gets the path to a test image file.
    /// </summary>
    /// <param name="imageName">The image file name (e.g., "test-72dpi.jpg").</param>
    /// <returns>The full path to the image file.</returns>
    /// <exception cref="FileNotFoundException">If the image file does not exist.</exception>
    public static string GetImagePath(string imageName)
    {
        // Try new location first
        var newPath = Path.Combine(TestImagesPath, imageName);
        if (File.Exists(newPath))
            return newPath;

        // Fall back to legacy location
        var legacyPath = Path.Combine(LegacyTestImagesPath, imageName);
        if (File.Exists(legacyPath))
            return legacyPath;

        throw new FileNotFoundException($"Test image not found: {imageName}", imageName);
    }

    /// <summary>
    /// Loads a test image file as a byte array.
    /// </summary>
    /// <param name="imageName">The image file name.</param>
    /// <returns>The image file bytes.</returns>
    public static byte[] LoadImage(string imageName)
    {
        var path = GetImagePath(imageName);
        return File.ReadAllBytes(path);
    }

    /// <summary>
    /// Checks if a test image exists.
    /// </summary>
    /// <param name="imageName">The image file name.</param>
    /// <returns>True if the image file exists.</returns>
    public static bool ImageExists(string imageName)
    {
        try
        {
            GetImagePath(imageName);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the test resources directory path.
    /// </summary>
    public static string GetTestResourcesPath()
    {
        return TestImagesPath;
    }

    /// <summary>
    /// Gets the legacy test images directory path.
    /// </summary>
    public static string GetLegacyTestImagesPath()
    {
        return LegacyTestImagesPath;
    }

    /// <summary>
    /// Lists all available test images.
    /// </summary>
    /// <returns>An array of test image file names.</returns>
    public static string[] ListTestImages()
    {
        var images = new List<string>();

        // Check new location
        if (Directory.Exists(TestImagesPath))
        {
            images.AddRange(Directory.GetFiles(TestImagesPath)
                .Select(Path.GetFileName)
                .Where(name => name != null)!);
        }

        // Check legacy location
        if (Directory.Exists(LegacyTestImagesPath))
        {
            images.AddRange(Directory.GetFiles(LegacyTestImagesPath)
                .Select(Path.GetFileName)
                .Where(name => name != null && !images.Contains(name))!);
        }

        return images.ToArray();
    }
}
