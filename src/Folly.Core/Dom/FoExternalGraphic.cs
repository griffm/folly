namespace Folly.Dom;

/// <summary>
/// Represents an fo:external-graphic element for embedding images.
/// </summary>
public sealed class FoExternalGraphic : FoElement
{
    /// <inheritdoc/>
    public override string Name => "external-graphic";

    /// <summary>
    /// Gets the source URI for the image.
    /// </summary>
    public string Src => Properties.GetString("src", "");

    /// <summary>
    /// Gets the content width.
    /// </summary>
    public string ContentWidth => Properties.GetString("content-width", "auto");

    /// <summary>
    /// Gets the content height.
    /// </summary>
    public string ContentHeight => Properties.GetString("content-height", "auto");

    /// <summary>
    /// Gets the scaling method.
    /// </summary>
    public string Scaling => Properties.GetString("scaling", "uniform");

    /// <summary>
    /// Gets the scaling method for uniform scaling.
    /// </summary>
    public string ScalingMethod => Properties.GetString("scaling-method", "auto");

    /// <summary>
    /// Gets the inline progression dimension (width).
    /// </summary>
    public string InlineProgressionDimension => Properties.GetString("inline-progression-dimension", "auto");

    /// <summary>
    /// Gets the block progression dimension (height).
    /// </summary>
    public string BlockProgressionDimension => Properties.GetString("block-progression-dimension", "auto");

    /// <summary>
    /// Gets the text alignment.
    /// </summary>
    public string TextAlign => Properties.GetString("text-align", "start");
}
