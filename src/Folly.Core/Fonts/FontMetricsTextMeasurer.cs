namespace Folly.Fonts;

/// <summary>
/// Adapter that wraps FontMetrics to implement ITextMeasurer for line breaking.
/// </summary>
internal sealed class FontMetricsTextMeasurer : Typography.LineBreaking.ITextMeasurer
{
    private readonly Fonts.FontMetrics _fontMetrics;

    public FontMetricsTextMeasurer(Fonts.FontMetrics fontMetrics)
    {
        _fontMetrics = fontMetrics;
    }

    public double MeasureWidth(string text)
    {
        return _fontMetrics.MeasureWidth(text);
    }
}
