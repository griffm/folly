using System.Text;

namespace Folly.Svg;

/// <summary>
/// Parses CSS stylesheets from SVG &lt;style&gt; tags and applies rules to elements.
/// Supports class selectors (.class), type selectors (rect), and ID selectors (#id).
/// </summary>
public static class SvgCssParser
{
    /// <summary>
    /// Parses CSS stylesheet content and returns a list of CSS rules.
    /// </summary>
    public static List<CssRule> ParseStylesheet(string css)
    {
        var rules = new List<CssRule>();

        if (string.IsNullOrWhiteSpace(css))
            return rules;

        // Remove comments (/* ... */)
        css = RemoveComments(css);

        // Split by } to find rule blocks
        var ruleBlocks = css.Split('}', StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in ruleBlocks)
        {
            if (string.IsNullOrWhiteSpace(block))
                continue;

            // Split selector from declarations
            var parts = block.Split('{', 2);
            if (parts.Length != 2)
                continue;

            var selectorText = parts[0].Trim();
            var declarationsText = parts[1].Trim();

            // Parse selectors (comma-separated)
            var selectors = selectorText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            // Parse declarations
            var declarations = ParseDeclarations(declarationsText);

            if (declarations.Count > 0)
            {
                foreach (var selector in selectors)
                {
                    rules.Add(new CssRule
                    {
                        Selector = selector,
                        Declarations = new Dictionary<string, string>(declarations)
                    });
                }
            }
        }

        return rules;
    }

    /// <summary>
    /// Applies CSS rules to an element's style based on selector matching.
    /// </summary>
    public static void ApplyCssRules(SvgElement element, List<CssRule> rules)
    {
        var applicableRules = new List<(int specificity, Dictionary<string, string> declarations)>();

        foreach (var rule in rules)
        {
            if (SelectorMatches(rule.Selector, element))
            {
                var specificity = CalculateSpecificity(rule.Selector);
                applicableRules.Add((specificity, rule.Declarations));
            }
        }

        // Sort by specificity (lowest first, so higher specificity overwrites)
        applicableRules.Sort((a, b) => a.specificity.CompareTo(b.specificity));

        // Apply declarations in order of specificity
        foreach (var (_, declarations) in applicableRules)
        {
            foreach (var (property, value) in declarations)
            {
                ApplyDeclarationToStyle(element.Style, property, value);
            }
        }
    }

    private static string RemoveComments(string css)
    {
        var result = new StringBuilder();
        int i = 0;

        while (i < css.Length)
        {
            if (i + 1 < css.Length && css[i] == '/' && css[i + 1] == '*')
            {
                // Skip until end of comment
                i += 2;
                while (i + 1 < css.Length)
                {
                    if (css[i] == '*' && css[i + 1] == '/')
                    {
                        i += 2;
                        break;
                    }
                    i++;
                }
            }
            else
            {
                result.Append(css[i]);
                i++;
            }
        }

        return result.ToString();
    }

    private static Dictionary<string, string> ParseDeclarations(string declarationsText)
    {
        var declarations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var parts = declarationsText.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            var colonIndex = part.IndexOf(':');
            if (colonIndex == -1)
                continue;

            var property = part.Substring(0, colonIndex).Trim();
            var value = part.Substring(colonIndex + 1).Trim();

            if (!string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(value))
            {
                declarations[property] = value;
            }
        }

        return declarations;
    }

    private static bool SelectorMatches(string selector, SvgElement element)
    {
        selector = selector.Trim();

        // Class selector: .class-name
        if (selector.StartsWith('.'))
        {
            var className = selector.Substring(1);
            var elementClasses = element.GetAttribute("class")?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            return elementClasses.Contains(className, StringComparer.OrdinalIgnoreCase);
        }

        // ID selector: #id
        if (selector.StartsWith('#'))
        {
            var id = selector.Substring(1);
            return element.GetAttribute("id")?.Equals(id, StringComparison.OrdinalIgnoreCase) == true;
        }

        // Type selector: rect, circle, path, etc.
        if (char.IsLetter(selector[0]))
        {
            return element.ElementType.Equals(selector, StringComparison.OrdinalIgnoreCase);
        }

        // Universal selector: *
        if (selector == "*")
        {
            return true;
        }

        return false;
    }

    private static int CalculateSpecificity(string selector)
    {
        // Simple specificity calculation:
        // ID selector: 100
        // Class selector: 10
        // Type selector: 1
        // Universal selector: 0

        selector = selector.Trim();

        if (selector.StartsWith('#'))
            return 100;

        if (selector.StartsWith('.'))
            return 10;

        if (selector == "*")
            return 0;

        // Type selector
        return 1;
    }

    private static void ApplyDeclarationToStyle(SvgStyle style, string property, string value)
    {
        // Map CSS property names to SvgStyle properties
        switch (property.ToLowerInvariant())
        {
            case "fill":
                style.Fill = value;
                break;
            case "fill-opacity":
                if (double.TryParse(value, out var fillOpacity))
                    style.FillOpacity = fillOpacity;
                break;
            case "fill-rule":
                style.FillRule = value;
                break;
            case "stroke":
                style.Stroke = value;
                break;
            case "stroke-width":
                if (double.TryParse(value, out var strokeWidth))
                    style.StrokeWidth = strokeWidth;
                break;
            case "stroke-opacity":
                if (double.TryParse(value, out var strokeOpacity))
                    style.StrokeOpacity = strokeOpacity;
                break;
            case "stroke-linecap":
                style.StrokeLineCap = value;
                break;
            case "stroke-linejoin":
                style.StrokeLineJoin = value;
                break;
            case "stroke-miterlimit":
                if (double.TryParse(value, out var miterLimit))
                    style.StrokeMiterLimit = miterLimit;
                break;
            case "stroke-dasharray":
                style.StrokeDashArray = value;
                break;
            case "stroke-dashoffset":
                if (double.TryParse(value, out var dashOffset))
                    style.StrokeDashOffset = dashOffset;
                break;
            case "opacity":
                if (double.TryParse(value, out var opacity))
                    style.Opacity = opacity;
                break;
            case "font-family":
                style.FontFamily = value;
                break;
            case "font-size":
                if (double.TryParse(value.Replace("px", "").Replace("pt", ""), out var fontSize))
                    style.FontSize = fontSize;
                break;
            case "font-weight":
                style.FontWeight = value;
                break;
            case "font-style":
                style.FontStyle = value;
                break;
            case "clip-path":
                style.ClipPath = value;
                break;
            case "mask":
                style.Mask = value;
                break;
            case "filter":
                style.Filter = value;
                break;
            case "marker-start":
                style.MarkerStart = value;
                break;
            case "marker-mid":
                style.MarkerMid = value;
                break;
            case "marker-end":
                style.MarkerEnd = value;
                break;
        }
    }
}

/// <summary>
/// Represents a CSS rule with a selector and declarations.
/// </summary>
public class CssRule
{
    /// <summary>
    /// Gets the CSS selector (e.g., ".class-name", "#id", "rect").
    /// </summary>
    public required string Selector { get; init; }

    /// <summary>
    /// Gets the CSS declarations (property-value pairs).
    /// </summary>
    public required Dictionary<string, string> Declarations { get; init; }
}
