namespace Folly;

/// <summary>
/// Options for loading XSL-FO documents.
/// </summary>
public sealed class FoLoadOptions
{
    /// <summary>
    /// Gets or sets whether to validate the FO structure against XSL-FO 1.1 spec.
    /// Default is true.
    /// </summary>
    public bool ValidateStructure { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate property values.
    /// Default is true.
    /// </summary>
    public bool ValidateProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets the base URI for resolving relative URIs in the document.
    /// </summary>
    public Uri? BaseUri { get; set; }
}
