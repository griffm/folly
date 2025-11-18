namespace Folly.Typography.LineBreaking;

/// <summary>
/// Interface for measuring text width.
/// This abstraction allows line breaking to work with any text measurement system.
/// </summary>
public interface ITextMeasurer
{
    /// <summary>
    /// Measures the width of the given text in points.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>The width in points.</returns>
    double MeasureWidth(string text);
}
