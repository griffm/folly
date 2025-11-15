namespace Folly.Svg;

/// <summary>
/// Parses SVG color values in various formats.
/// Supports: hex (#RGB, #RRGGBB), rgb(), rgba(), named colors.
/// </summary>
public static class SvgColorParser
{
    /// <summary>
    /// Parses a hex color (#RGB or #RRGGBB).
    /// Returns RGB values in range [0, 1] for PDF.
    /// </summary>
    public static (double r, double g, double b)? ParseHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex) || !hex.StartsWith('#'))
            return null;

        hex = hex[1..]; // Remove #

        if (hex.Length == 3)
        {
            // #RGB -> #RRGGBB
            var r = Convert.ToInt32(hex[0].ToString() + hex[0].ToString(), 16);
            var g = Convert.ToInt32(hex[1].ToString() + hex[1].ToString(), 16);
            var b = Convert.ToInt32(hex[2].ToString() + hex[2].ToString(), 16);
            return (r / 255.0, g / 255.0, b / 255.0);
        }
        else if (hex.Length == 6)
        {
            var r = Convert.ToInt32(hex[0..2], 16);
            var g = Convert.ToInt32(hex[2..4], 16);
            var b = Convert.ToInt32(hex[4..6], 16);
            return (r / 255.0, g / 255.0, b / 255.0);
        }

        return null;
    }

    /// <summary>
    /// Parses rgb() or rgba() color.
    /// Examples: "rgb(255, 0, 0)", "rgb(100%, 0%, 0%)"
    /// </summary>
    public static (double r, double g, double b)? ParseRgb(string rgb)
    {
        if (string.IsNullOrWhiteSpace(rgb))
            return null;

        rgb = rgb.Trim().ToLowerInvariant();

        // Remove "rgb(" or "rgba(" prefix and ")" suffix
        var start = rgb.IndexOf('(');
        var end = rgb.IndexOf(')');
        if (start == -1 || end == -1)
            return null;

        var content = rgb.Substring(start + 1, end - start - 1);
        var parts = content.Split(',', StringSplitOptions.TrimEntries);

        if (parts.Length < 3)
            return null;

        double r = 0, g = 0, b = 0;

        // Parse R
        if (parts[0].EndsWith('%'))
        {
            if (double.TryParse(parts[0].TrimEnd('%'), out var rPct))
                r = rPct / 100.0;
        }
        else
        {
            if (double.TryParse(parts[0], out var rVal))
                r = rVal / 255.0;
        }

        // Parse G
        if (parts[1].EndsWith('%'))
        {
            if (double.TryParse(parts[1].TrimEnd('%'), out var gPct))
                g = gPct / 100.0;
        }
        else
        {
            if (double.TryParse(parts[1], out var gVal))
                g = gVal / 255.0;
        }

        // Parse B
        if (parts[2].EndsWith('%'))
        {
            if (double.TryParse(parts[2].TrimEnd('%'), out var bPct))
                b = bPct / 100.0;
        }
        else
        {
            if (double.TryParse(parts[2], out var bVal))
                b = bVal / 255.0;
        }

        return (r, g, b);
    }

    /// <summary>
    /// Parses a named SVG color.
    /// </summary>
    public static (double r, double g, double b)? ParseNamed(string colorName)
    {
        if (string.IsNullOrWhiteSpace(colorName))
            return null;

        colorName = colorName.Trim().ToLowerInvariant();

        if (NamedColors.TryGetValue(colorName, out var hex))
        {
            return ParseHex(hex);
        }

        return null;
    }

    /// <summary>
    /// SVG/CSS named colors (147 standard colors).
    /// </summary>
    private static readonly Dictionary<string, string> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aliceblue"] = "#F0F8FF",
        ["antiquewhite"] = "#FAEBD7",
        ["aqua"] = "#00FFFF",
        ["aquamarine"] = "#7FFFD4",
        ["azure"] = "#F0FFFF",
        ["beige"] = "#F5F5DC",
        ["bisque"] = "#FFE4C4",
        ["black"] = "#000000",
        ["blanchedalmond"] = "#FFEBCD",
        ["blue"] = "#0000FF",
        ["blueviolet"] = "#8A2BE2",
        ["brown"] = "#A52A2A",
        ["burlywood"] = "#DEB887",
        ["cadetblue"] = "#5F9EA0",
        ["chartreuse"] = "#7FFF00",
        ["chocolate"] = "#D2691E",
        ["coral"] = "#FF7F50",
        ["cornflowerblue"] = "#6495ED",
        ["cornsilk"] = "#FFF8DC",
        ["crimson"] = "#DC143C",
        ["cyan"] = "#00FFFF",
        ["darkblue"] = "#00008B",
        ["darkcyan"] = "#008B8B",
        ["darkgoldenrod"] = "#B8860B",
        ["darkgray"] = "#A9A9A9",
        ["darkgrey"] = "#A9A9A9",
        ["darkgreen"] = "#006400",
        ["darkkhaki"] = "#BDB76B",
        ["darkmagenta"] = "#8B008B",
        ["darkolivegreen"] = "#556B2F",
        ["darkorange"] = "#FF8C00",
        ["darkorchid"] = "#9932CC",
        ["darkred"] = "#8B0000",
        ["darksalmon"] = "#E9967A",
        ["darkseagreen"] = "#8FBC8F",
        ["darkslateblue"] = "#483D8B",
        ["darkslategray"] = "#2F4F4F",
        ["darkslategrey"] = "#2F4F4F",
        ["darkturquoise"] = "#00CED1",
        ["darkviolet"] = "#9400D3",
        ["deeppink"] = "#FF1493",
        ["deepskyblue"] = "#00BFFF",
        ["dimgray"] = "#696969",
        ["dimgrey"] = "#696969",
        ["dodgerblue"] = "#1E90FF",
        ["firebrick"] = "#B22222",
        ["floralwhite"] = "#FFFAF0",
        ["forestgreen"] = "#228B22",
        ["fuchsia"] = "#FF00FF",
        ["gainsboro"] = "#DCDCDC",
        ["ghostwhite"] = "#F8F8FF",
        ["gold"] = "#FFD700",
        ["goldenrod"] = "#DAA520",
        ["gray"] = "#808080",
        ["grey"] = "#808080",
        ["green"] = "#008000",
        ["greenyellow"] = "#ADFF2F",
        ["honeydew"] = "#F0FFF0",
        ["hotpink"] = "#FF69B4",
        ["indianred"] = "#CD5C5C",
        ["indigo"] = "#4B0082",
        ["ivory"] = "#FFFFF0",
        ["khaki"] = "#F0E68C",
        ["lavender"] = "#E6E6FA",
        ["lavenderblush"] = "#FFF0F5",
        ["lawngreen"] = "#7CFC00",
        ["lemonchiffon"] = "#FFFACD",
        ["lightblue"] = "#ADD8E6",
        ["lightcoral"] = "#F08080",
        ["lightcyan"] = "#E0FFFF",
        ["lightgoldenrodyellow"] = "#FAFAD2",
        ["lightgray"] = "#D3D3D3",
        ["lightgrey"] = "#D3D3D3",
        ["lightgreen"] = "#90EE90",
        ["lightpink"] = "#FFB6C1",
        ["lightsalmon"] = "#FFA07A",
        ["lightseagreen"] = "#20B2AA",
        ["lightskyblue"] = "#87CEFA",
        ["lightslategray"] = "#778899",
        ["lightslategrey"] = "#778899",
        ["lightsteelblue"] = "#B0C4DE",
        ["lightyellow"] = "#FFFFE0",
        ["lime"] = "#00FF00",
        ["limegreen"] = "#32CD32",
        ["linen"] = "#FAF0E6",
        ["magenta"] = "#FF00FF",
        ["maroon"] = "#800000",
        ["mediumaquamarine"] = "#66CDAA",
        ["mediumblue"] = "#0000CD",
        ["mediumorchid"] = "#BA55D3",
        ["mediumpurple"] = "#9370DB",
        ["mediumseagreen"] = "#3CB371",
        ["mediumslateblue"] = "#7B68EE",
        ["mediumspringgreen"] = "#00FA9A",
        ["mediumturquoise"] = "#48D1CC",
        ["mediumvioletred"] = "#C71585",
        ["midnightblue"] = "#191970",
        ["mintcream"] = "#F5FFFA",
        ["mistyrose"] = "#FFE4E1",
        ["moccasin"] = "#FFE4B5",
        ["navajowhite"] = "#FFDEAD",
        ["navy"] = "#000080",
        ["oldlace"] = "#FDF5E6",
        ["olive"] = "#808000",
        ["olivedrab"] = "#6B8E23",
        ["orange"] = "#FFA500",
        ["orangered"] = "#FF4500",
        ["orchid"] = "#DA70D6",
        ["palegoldenrod"] = "#EEE8AA",
        ["palegreen"] = "#98FB98",
        ["paleturquoise"] = "#AFEEEE",
        ["palevioletred"] = "#DB7093",
        ["papayawhip"] = "#FFEFD5",
        ["peachpuff"] = "#FFDAB9",
        ["peru"] = "#CD853F",
        ["pink"] = "#FFC0CB",
        ["plum"] = "#DDA0DD",
        ["powderblue"] = "#B0E0E6",
        ["purple"] = "#800080",
        ["red"] = "#FF0000",
        ["rosybrown"] = "#BC8F8F",
        ["royalblue"] = "#4169E1",
        ["saddlebrown"] = "#8B4513",
        ["salmon"] = "#FA8072",
        ["sandybrown"] = "#F4A460",
        ["seagreen"] = "#2E8B57",
        ["seashell"] = "#FFF5EE",
        ["sienna"] = "#A0522D",
        ["silver"] = "#C0C0C0",
        ["skyblue"] = "#87CEEB",
        ["slateblue"] = "#6A5ACD",
        ["slategray"] = "#708090",
        ["slategrey"] = "#708090",
        ["snow"] = "#FFFAFA",
        ["springgreen"] = "#00FF7F",
        ["steelblue"] = "#4682B4",
        ["tan"] = "#D2B48C",
        ["teal"] = "#008080",
        ["thistle"] = "#D8BFD8",
        ["tomato"] = "#FF6347",
        ["turquoise"] = "#40E0D0",
        ["violet"] = "#EE82EE",
        ["wheat"] = "#F5DEB3",
        ["white"] = "#FFFFFF",
        ["whitesmoke"] = "#F5F5F5",
        ["yellow"] = "#FFFF00",
        ["yellowgreen"] = "#9ACD32",
    };
}
