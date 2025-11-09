namespace Folly;

/// <summary>
/// Options for the layout engine.
/// </summary>
public sealed class LayoutOptions
{
    /// <summary>
    /// Gets or sets whether to enable strict layout validation.
    /// Default is true.
    /// </summary>
    public bool StrictLayout { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of layout iterations for complex cases.
    /// Default is 10.
    /// </summary>
    public int MaxIterations { get; set; } = 10;
}
