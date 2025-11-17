using System.Text;

namespace Folly.Svg.Gradients;

/// <summary>
/// Converts SVG gradients to PDF shading patterns.
/// PDF supports Type 2 (axial/linear) and Type 3 (radial) shading patterns.
/// </summary>
public static class SvgGradientToPdf
{
    /// <summary>
    /// Generates a PDF shading dictionary for a gradient.
    /// This returns the shading dictionary object that can be added to PDF resources.
    /// </summary>
    /// <param name="gradient">The SVG gradient to convert.</param>
    /// <param name="boundingBox">The bounding box of the shape being filled (for objectBoundingBox units).</param>
    /// <returns>PDF shading dictionary as a string.</returns>
    public static string GenerateShadingDictionary(SvgGradient gradient, (double x, double y, double width, double height) boundingBox)
    {
        return gradient switch
        {
            SvgLinearGradient linear => GenerateLinearShading(linear, boundingBox),
            SvgRadialGradient radial => GenerateRadialShading(radial, boundingBox),
            _ => throw new NotSupportedException($"Gradient type '{gradient.Type}' is not supported")
        };
    }

    private static string GenerateLinearShading(SvgLinearGradient gradient, (double x, double y, double width, double height) bbox)
    {
        var sb = new StringBuilder();

        // Calculate coordinates based on gradient units
        double x1, y1, x2, y2;

        if (gradient.GradientUnits == "objectBoundingBox")
        {
            // Coordinates are fractions (0-1) of the bounding box
            x1 = bbox.x + gradient.X1 * bbox.width;
            y1 = bbox.y + gradient.Y1 * bbox.height;
            x2 = bbox.x + gradient.X2 * bbox.width;
            y2 = bbox.y + gradient.Y2 * bbox.height;
        }
        else
        {
            // userSpaceOnUse: coordinates are absolute
            x1 = gradient.X1;
            y1 = gradient.Y1;
            x2 = gradient.X2;
            y2 = gradient.Y2;
        }

        // Apply gradient transform if present
        if (gradient.GradientTransform != null)
        {
            var t = gradient.GradientTransform;
            (x1, y1) = t.TransformPoint(x1, y1);
            (x2, y2) = t.TransformPoint(x2, y2);
        }

        // Build PDF Type 2 (Axial) shading dictionary
        sb.AppendLine("<<");
        sb.AppendLine("  /ShadingType 2"); // Axial shading (linear gradient)
        sb.AppendLine("  /ColorSpace /DeviceRGB");
        sb.AppendLine($"  /Coords [{x1} {y1} {x2} {y2}]"); // Start and end points

        // Generate function for color interpolation
        var function = GenerateGradientFunction(gradient.Stops);
        sb.AppendLine($"  /Function {function}");

        // Handle extend (spreadMethod)
        var extend = gradient.SpreadMethod == "pad" ? "[true true]" : "[false false]";
        sb.AppendLine($"  /Extend {extend}");

        sb.Append(">>");

        return sb.ToString();
    }

    private static string GenerateRadialShading(SvgRadialGradient gradient, (double x, double y, double width, double height) bbox)
    {
        var sb = new StringBuilder();

        // Calculate coordinates based on gradient units
        double cx, cy, r, fx, fy;

        if (gradient.GradientUnits == "objectBoundingBox")
        {
            // Coordinates are fractions (0-1) of the bounding box
            cx = bbox.x + gradient.Cx * bbox.width;
            cy = bbox.y + gradient.Cy * bbox.height;
            // Use minimum dimension for radius to ensure gradient fits within bounding box
            // NOTE: For non-square boxes, this approximates elliptical gradients as circular.
            // PDF Type 3 shading supports only circular radial gradients, not elliptical.
            r = gradient.R * Math.Min(bbox.width, bbox.height);
            fx = bbox.x + gradient.Fx * bbox.width;
            fy = bbox.y + gradient.Fy * bbox.height;
        }
        else
        {
            // userSpaceOnUse: coordinates are absolute
            cx = gradient.Cx;
            cy = gradient.Cy;
            r = gradient.R;
            fx = gradient.Fx;
            fy = gradient.Fy;
        }

        // Apply gradient transform if present
        if (gradient.GradientTransform != null)
        {
            var t = gradient.GradientTransform;
            (cx, cy) = t.TransformPoint(cx, cy);
            (fx, fy) = t.TransformPoint(fx, fy);
            // NOTE: Radius is not transformed for non-uniform scaling.
            // PDF Type 3 shading only supports circular gradients. Proper handling of
            // non-uniform scale transforms would require elliptical gradients or
            // approximation via pattern fills, neither of which is currently implemented.
        }

        // Build PDF Type 3 (Radial) shading dictionary
        sb.AppendLine("<<");
        sb.AppendLine("  /ShadingType 3"); // Radial shading
        sb.AppendLine("  /ColorSpace /DeviceRGB");
        sb.AppendLine($"  /Coords [{fx} {fy} {gradient.Fr} {cx} {cy} {r}]"); // Focal point and circle

        // Generate function for color interpolation
        var function = GenerateGradientFunction(gradient.Stops);
        sb.AppendLine($"  /Function {function}");

        // Handle extend
        var extend = gradient.SpreadMethod == "pad" ? "[true true]" : "[false false]";
        sb.AppendLine($"  /Extend {extend}");

        sb.Append(">>");

        return sb.ToString();
    }

    private static string GenerateGradientFunction(List<SvgGradientStop> stops)
    {
        if (stops.Count == 0)
        {
            // No stops - return black function
            return "<< /FunctionType 2 /Domain [0 1] /C0 [0 0 0] /C1 [0 0 0] /N 1 >>";
        }

        if (stops.Count == 1)
        {
            // Single stop - constant color
            var color = ParseStopColor(stops[0]);
            return $"<< /FunctionType 2 /Domain [0 1] /C0 [{color.r} {color.g} {color.b}] /C1 [{color.r} {color.g} {color.b}] /N 1 >>";
        }

        if (stops.Count == 2)
        {
            // Two stops - simple linear interpolation (exponential with N=1)
            var color0 = ParseStopColor(stops[0]);
            var color1 = ParseStopColor(stops[1]);

            return $"<< /FunctionType 2 /Domain [0 1] /C0 [{color0.r} {color0.g} {color0.b}] /C1 [{color1.r} {color1.g} {color1.b}] /N 1 >>";
        }

        // Multiple stops - use stitching function (Type 3)
        var sb = new StringBuilder();
        sb.AppendLine("<<");
        sb.AppendLine("  /FunctionType 3"); // Stitching function
        sb.AppendLine("  /Domain [0 1]");

        // Build sub-functions for each segment
        var functions = new List<string>();
        var bounds = new List<double>();
        var encode = new List<string>();

        for (int i = 0; i < stops.Count - 1; i++)
        {
            var stop0 = stops[i];
            var stop1 = stops[i + 1];

            var color0 = ParseStopColor(stop0);
            var color1 = ParseStopColor(stop1);

            // Create interpolation function for this segment
            var func = $"<< /FunctionType 2 /Domain [0 1] /C0 [{color0.r} {color0.g} {color0.b}] /C1 [{color1.r} {color1.g} {color1.b}] /N 1 >>";
            functions.Add(func);

            // Add boundary (all except last)
            if (i < stops.Count - 2)
            {
                bounds.Add(stop1.Offset);
            }

            encode.Add("0 1");
        }

        // Add functions array
        sb.AppendLine($"  /Functions [");
        foreach (var func in functions)
        {
            sb.AppendLine($"    {func}");
        }
        sb.AppendLine("  ]");

        // Add bounds array
        if (bounds.Count > 0)
        {
            sb.AppendLine($"  /Bounds [{string.Join(" ", bounds)}]");
        }
        else
        {
            sb.AppendLine("  /Bounds []");
        }

        // Add encode array
        sb.AppendLine($"  /Encode [{string.Join(" ", encode)}]");

        sb.Append(">>");

        return sb.ToString();
    }

    private static (double r, double g, double b) ParseStopColor(SvgGradientStop stop)
    {
        // Parse color using SvgColorParser
        var color = SvgColorParser.ParseHex(stop.Color);
        if (color == null)
        {
            color = SvgColorParser.ParseRgb(stop.Color);
        }
        if (color == null)
        {
            color = SvgColorParser.ParseNamed(stop.Color);
        }

        if (color == null)
        {
            // Fallback to black
            return (0, 0, 0);
        }

        // LIMITATION: Per-stop opacity is not applied to gradient colors.
        // PDF shading dictionaries (Type 2/3) do not natively support opacity in color
        // functions - they only support RGB/CMYK/Gray color spaces, not alpha channels.
        //
        // Proper implementation would require one of:
        // 1. SMask (soft mask) with separate opacity gradient
        // 2. Transparency group with alpha channel compositing
        // 3. Pre-multiplying alpha into colors (incorrect color blending)
        //
        // This is a known PDF limitation. Most SVG implementations similarly struggle
        // with gradient opacity when converting to static formats. The gradient colors
        // are rendered correctly; only the per-stop opacity values are ignored.
        //
        // Workaround: Apply uniform opacity to the entire element using fill-opacity
        // instead of per-stop opacity for gradient stops.

        return color.Value;
    }
}
