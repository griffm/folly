namespace Folly.Svg;

/// <summary>
/// Represents an SVG transformation matrix.
/// SVG transforms include: translate, scale, rotate, skewX, skewY, matrix.
/// This class maintains a 3x3 transformation matrix (using 2x3 affine transform).
/// </summary>
public sealed class SvgTransform
{
    // Affine transformation matrix:
    // | a  c  e |
    // | b  d  f |
    // | 0  0  1 |
    //
    // [x']   [a c e]   [x]
    // [y'] = [b d f] * [y]
    // [1 ]   [0 0 1]   [1]

    /// <summary>
    /// Gets or sets the A component of the transformation matrix (Scale X / Matrix[0,0]).
    /// </summary>
    public double A { get; set; } = 1;

    /// <summary>
    /// Gets or sets the B component of the transformation matrix (Skew Y / Matrix[1,0]).
    /// </summary>
    public double B { get; set; } = 0;

    /// <summary>
    /// Gets or sets the C component of the transformation matrix (Skew X / Matrix[0,1]).
    /// </summary>
    public double C { get; set; } = 0;

    /// <summary>
    /// Gets or sets the D component of the transformation matrix (Scale Y / Matrix[1,1]).
    /// </summary>
    public double D { get; set; } = 1;

    /// <summary>
    /// Gets or sets the E component of the transformation matrix (Translate X / Matrix[0,2]).
    /// </summary>
    public double E { get; set; } = 0;

    /// <summary>
    /// Gets or sets the F component of the transformation matrix (Translate Y / Matrix[1,2]).
    /// </summary>
    public double F { get; set; } = 0;

    /// <summary>
    /// Creates an identity transform (no transformation).
    /// </summary>
    public static SvgTransform Identity()
    {
        return new SvgTransform { A = 1, B = 0, C = 0, D = 1, E = 0, F = 0 };
    }

    /// <summary>
    /// Creates a translation transform.
    /// </summary>
    public static SvgTransform Translate(double tx, double ty = 0)
    {
        return new SvgTransform { A = 1, B = 0, C = 0, D = 1, E = tx, F = ty };
    }

    /// <summary>
    /// Creates a scale transform.
    /// </summary>
    public static SvgTransform Scale(double sx, double sy = 0)
    {
        if (sy == 0) sy = sx; // Uniform scaling if sy not specified
        return new SvgTransform { A = sx, B = 0, C = 0, D = sy, E = 0, F = 0 };
    }

    /// <summary>
    /// Creates a rotation transform (angle in degrees).
    /// If cx, cy are provided, rotates around that point.
    /// </summary>
    public static SvgTransform Rotate(double angle, double cx = 0, double cy = 0)
    {
        var radians = angle * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);

        if (cx == 0 && cy == 0)
        {
            return new SvgTransform { A = cos, B = sin, C = -sin, D = cos, E = 0, F = 0 };
        }
        else
        {
            // Rotate around (cx, cy): translate(-cx,-cy) * rotate * translate(cx,cy)
            return new SvgTransform
            {
                A = cos,
                B = sin,
                C = -sin,
                D = cos,
                E = -cx * cos + cy * sin + cx,
                F = -cx * sin - cy * cos + cy
            };
        }
    }

    /// <summary>
    /// Creates a skewX transform (angle in degrees).
    /// </summary>
    public static SvgTransform SkewX(double angle)
    {
        var radians = angle * Math.PI / 180.0;
        var tan = Math.Tan(radians);
        return new SvgTransform { A = 1, B = 0, C = tan, D = 1, E = 0, F = 0 };
    }

    /// <summary>
    /// Creates a skewY transform (angle in degrees).
    /// </summary>
    public static SvgTransform SkewY(double angle)
    {
        var radians = angle * Math.PI / 180.0;
        var tan = Math.Tan(radians);
        return new SvgTransform { A = 1, B = tan, C = 0, D = 1, E = 0, F = 0 };
    }

    /// <summary>
    /// Creates a transform from a matrix.
    /// </summary>
    public static SvgTransform Matrix(double a, double b, double c, double d, double e, double f)
    {
        return new SvgTransform { A = a, B = b, C = c, D = d, E = e, F = f };
    }

    /// <summary>
    /// Multiplies this transform by another (composes transformations).
    /// Result = this * other
    /// </summary>
    public SvgTransform Multiply(SvgTransform other)
    {
        return new SvgTransform
        {
            A = A * other.A + C * other.B,
            B = B * other.A + D * other.B,
            C = A * other.C + C * other.D,
            D = B * other.C + D * other.D,
            E = A * other.E + C * other.F + E,
            F = B * other.E + D * other.F + F
        };
    }

    /// <summary>
    /// Transforms a point using this transformation matrix.
    /// </summary>
    public (double x, double y) TransformPoint(double x, double y)
    {
        return (
            A * x + C * y + E,
            B * x + D * y + F
        );
    }

    /// <summary>
    /// Returns true if this is the identity transform (no transformation).
    /// </summary>
    public bool IsIdentity()
    {
        return A == 1 && B == 0 && C == 0 && D == 1 && E == 0 && F == 0;
    }

    /// <summary>
    /// Converts to PDF transformation matrix format (a b c d e f).
    /// </summary>
    public string ToPdfMatrix()
    {
        return $"{A} {B} {C} {D} {E} {F}";
    }

    /// <summary>
    /// Returns a string representation of the transformation matrix.
    /// </summary>
    public override string ToString()
    {
        return $"matrix({A}, {B}, {C}, {D}, {E}, {F})";
    }
}
