namespace Folly.Svg;

/// <summary>
/// Parses SVG transform attributes.
/// Supports: translate, scale, rotate, skewX, skewY, matrix.
/// Example: "translate(10, 20) rotate(45) scale(2)"
/// </summary>
public static class SvgTransformParser
{
    /// <summary>
    /// Parses a transform attribute value and returns the composed transformation.
    /// </summary>
    public static SvgTransform Parse(string transformAttr)
    {
        if (string.IsNullOrWhiteSpace(transformAttr))
            return SvgTransform.Identity();

        var result = SvgTransform.Identity();
        var commands = ExtractTransformCommands(transformAttr);

        foreach (var (command, args) in commands)
        {
            var transform = ParseTransformCommand(command, args);
            if (transform != null)
            {
                result = result.Multiply(transform);
            }
        }

        return result;
    }

    private static List<(string command, double[] args)> ExtractTransformCommands(string transformAttr)
    {
        var commands = new List<(string, double[])>();
        var remaining = transformAttr.Trim();

        while (!string.IsNullOrWhiteSpace(remaining))
        {
            // Find next command
            var openParen = remaining.IndexOf('(');
            if (openParen == -1) break;

            var command = remaining[..openParen].Trim();
            var closeParen = remaining.IndexOf(')', openParen);
            if (closeParen == -1) break;

            var argsStr = remaining.Substring(openParen + 1, closeParen - openParen - 1);
            var args = ParseArgs(argsStr);

            commands.Add((command, args));

            remaining = remaining[(closeParen + 1)..].Trim();
        }

        return commands;
    }

    private static double[] ParseArgs(string argsStr)
    {
        var parts = argsStr.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var args = new List<double>();

        foreach (var part in parts)
        {
            if (double.TryParse(part.Trim(), out var value))
                args.Add(value);
        }

        return args.ToArray();
    }

    private static SvgTransform? ParseTransformCommand(string command, double[] args)
    {
        return command.ToLowerInvariant() switch
        {
            "translate" => ParseTranslate(args),
            "scale" => ParseScale(args),
            "rotate" => ParseRotate(args),
            "skewx" => ParseSkewX(args),
            "skewy" => ParseSkewY(args),
            "matrix" => ParseMatrix(args),
            _ => null
        };
    }

    private static SvgTransform? ParseTranslate(double[] args)
    {
        if (args.Length == 0) return null;

        var tx = args[0];
        var ty = args.Length > 1 ? args[1] : 0;

        return SvgTransform.Translate(tx, ty);
    }

    private static SvgTransform? ParseScale(double[] args)
    {
        if (args.Length == 0) return null;

        var sx = args[0];
        var sy = args.Length > 1 ? args[1] : sx; // Uniform if sy not specified

        return SvgTransform.Scale(sx, sy);
    }

    private static SvgTransform? ParseRotate(double[] args)
    {
        if (args.Length == 0) return null;

        var angle = args[0];

        if (args.Length >= 3)
        {
            var cx = args[1];
            var cy = args[2];
            return SvgTransform.Rotate(angle, cx, cy);
        }

        return SvgTransform.Rotate(angle);
    }

    private static SvgTransform? ParseSkewX(double[] args)
    {
        if (args.Length == 0) return null;
        return SvgTransform.SkewX(args[0]);
    }

    private static SvgTransform? ParseSkewY(double[] args)
    {
        if (args.Length == 0) return null;
        return SvgTransform.SkewY(args[0]);
    }

    private static SvgTransform? ParseMatrix(double[] args)
    {
        if (args.Length < 6) return null;
        return SvgTransform.Matrix(args[0], args[1], args[2], args[3], args[4], args[5]);
    }
}
