namespace Folly.Xslfo;

/// <summary>
/// Represents the fo:leader element.
/// Used to generate leader patterns such as dots, rules, or spaces
/// between two text regions (commonly used in tables of contents).
/// </summary>
public sealed class FoLeader : FoElement
{
    /// <inheritdoc/>
    public override string Name => "leader";

    /// <summary>
    /// Gets the leader pattern (space, dots, rule, use-content).
    /// Default is "space".
    /// </summary>
    public string LeaderPattern => Properties.GetString("leader-pattern", "space");

    /// <summary>
    /// Gets the leader pattern width (use-font-metrics, or a length value).
    /// Specifies the inline-progression-dimension of the pattern.
    /// Default is "use-font-metrics".
    /// </summary>
    public string LeaderPatternWidth => Properties.GetString("leader-pattern-width", "use-font-metrics");

    /// <summary>
    /// Gets the leader length minimum.
    /// Default is "0pt".
    /// </summary>
    public double LeaderLengthMinimum => Properties.GetLength("leader-length.minimum", 0);

    /// <summary>
    /// Gets the leader length optimum.
    /// Default is "12pt".
    /// </summary>
    public double LeaderLengthOptimum => Properties.GetLength("leader-length.optimum", 12);

    /// <summary>
    /// Gets the leader length maximum.
    /// Default is "100%".
    /// </summary>
    public string LeaderLengthMaximum => Properties.GetString("leader-length.maximum", "100%");

    /// <summary>
    /// Gets the leader alignment (none, reference-area, page).
    /// Default is "none".
    /// </summary>
    public string LeaderAlignment => Properties.GetString("leader-alignment", "none");

    /// <summary>
    /// Gets the rule thickness for leader-pattern="rule".
    /// Default is "1pt".
    /// </summary>
    public double RuleThickness => Properties.GetLength("rule-thickness", 1);

    /// <summary>
    /// Gets the rule style (none, dotted, dashed, solid, double, groove, ridge).
    /// Default is "solid".
    /// </summary>
    public string RuleStyle => Properties.GetString("rule-style", "solid");

    /// <summary>
    /// Gets the color for the leader pattern.
    /// Inherits from parent if not specified.
    /// </summary>
    public string Color => GetComputedProperty("color", "black") ?? "black";

    /// <summary>
    /// Gets the font family for leader-pattern="dots" or "use-content".
    /// </summary>
    public string FontFamily => GetComputedProperty("font-family", "Helvetica") ?? "Helvetica";

    /// <summary>
    /// Gets the font size in points for leader-pattern="dots" or "use-content".
    /// </summary>
    public double? FontSize
    {
        get
        {
            var value = GetComputedProperty("font-size", "12pt");
            if (string.IsNullOrEmpty(value)) return 12;
            return LengthParser.Parse(value);
        }
    }
}
