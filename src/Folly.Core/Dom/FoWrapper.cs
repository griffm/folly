namespace Folly.Dom;

/// <summary>
/// Represents the fo:wrapper element.
/// A non-visual wrapper element used solely for inheriting and passing properties
/// to its child elements. It does not generate any areas itself.
/// </summary>
public sealed class FoWrapper : FoElement
{
    /// <inheritdoc/>
    public override string Name => "wrapper";

    // FoWrapper doesn't have specific properties beyond the inherited ones
    // Its primary purpose is to group content and apply properties that will be inherited
}
