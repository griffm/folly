using System.Globalization;
using System.Text;

namespace Folly.Svg;

/// <summary>
/// Parses SVG path data (the 'd' attribute) and converts to PDF path commands.
/// Supports all SVG path commands: M, L, H, V, C, S, Q, T, A, Z.
/// </summary>
public static class SvgPathParser
{
    /// <summary>
    /// Parses SVG path data and returns PDF path commands.
    /// </summary>
    public static List<string> Parse(string pathData)
    {
        var pdfCommands = new List<string>();
        var parser = new PathDataParser(pathData);

        double currentX = 0, currentY = 0;      // Current point
        double startX = 0, startY = 0;          // Start of current subpath
        double lastControlX = 0, lastControlY = 0; // Last control point (for S, T commands)
        char lastCommand = ' ';

        while (parser.HasMore())
        {
            var command = parser.ReadCommand();
            if (command == ' ') break;

            var isRelative = char.IsLower(command);
            var commandUpper = char.ToUpper(command);

            switch (commandUpper)
            {
                case 'M': // Move to
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x += currentX;
                            y += currentY;
                        }

                        pdfCommands.Add($"{x} {y} m");
                        currentX = x;
                        currentY = y;
                        startX = x;
                        startY = y;

                        // After first coordinate pair, treat as lineto
                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'L': // Line to
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x += currentX;
                            y += currentY;
                        }

                        pdfCommands.Add($"{x} {y} l");
                        currentX = x;
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'H': // Horizontal line
                {
                    while (parser.TryReadNumber(out var x))
                    {
                        if (isRelative)
                            x += currentX;

                        pdfCommands.Add($"{x} {currentY} l");
                        currentX = x;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'V': // Vertical line
                {
                    while (parser.TryReadNumber(out var y))
                    {
                        if (isRelative)
                            y += currentY;

                        pdfCommands.Add($"{currentX} {y} l");
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'C': // Cubic Bézier
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x1)) break;
                        if (!parser.TryReadNumber(out var y1)) break;
                        if (!parser.TryReadNumber(out var x2)) break;
                        if (!parser.TryReadNumber(out var y2)) break;
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x1 += currentX; y1 += currentY;
                            x2 += currentX; y2 += currentY;
                            x += currentX; y += currentY;
                        }

                        pdfCommands.Add($"{x1} {y1} {x2} {y2} {x} {y} c");
                        lastControlX = x2;
                        lastControlY = y2;
                        currentX = x;
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'S': // Smooth cubic Bézier
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x2)) break;
                        if (!parser.TryReadNumber(out var y2)) break;
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        // First control point is reflection of last control point
                        double x1, y1;
                        if (lastCommand == 'C' || lastCommand == 'S' || lastCommand == 'c' || lastCommand == 's')
                        {
                            x1 = 2 * currentX - lastControlX;
                            y1 = 2 * currentY - lastControlY;
                        }
                        else
                        {
                            x1 = currentX;
                            y1 = currentY;
                        }

                        if (isRelative)
                        {
                            x2 += currentX; y2 += currentY;
                            x += currentX; y += currentY;
                        }

                        pdfCommands.Add($"{x1} {y1} {x2} {y2} {x} {y} c");
                        lastControlX = x2;
                        lastControlY = y2;
                        currentX = x;
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'Q': // Quadratic Bézier
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x1)) break;
                        if (!parser.TryReadNumber(out var y1)) break;
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x1 += currentX; y1 += currentY;
                            x += currentX; y += currentY;
                        }

                        // Convert quadratic to cubic Bézier
                        var cx1 = currentX + (2.0 / 3.0) * (x1 - currentX);
                        var cy1 = currentY + (2.0 / 3.0) * (y1 - currentY);
                        var cx2 = x + (2.0 / 3.0) * (x1 - x);
                        var cy2 = y + (2.0 / 3.0) * (y1 - y);

                        pdfCommands.Add($"{cx1} {cy1} {cx2} {cy2} {x} {y} c");
                        lastControlX = x1;
                        lastControlY = y1;
                        currentX = x;
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'T': // Smooth quadratic Bézier
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        // Control point is reflection of last control point
                        double x1, y1;
                        if (lastCommand == 'Q' || lastCommand == 'T' || lastCommand == 'q' || lastCommand == 't')
                        {
                            x1 = 2 * currentX - lastControlX;
                            y1 = 2 * currentY - lastControlY;
                        }
                        else
                        {
                            x1 = currentX;
                            y1 = currentY;
                        }

                        if (isRelative)
                        {
                            x += currentX;
                            y += currentY;
                        }

                        // Convert quadratic to cubic
                        var cx1 = currentX + (2.0 / 3.0) * (x1 - currentX);
                        var cy1 = currentY + (2.0 / 3.0) * (y1 - currentY);
                        var cx2 = x + (2.0 / 3.0) * (x1 - x);
                        var cy2 = y + (2.0 / 3.0) * (y1 - y);

                        pdfCommands.Add($"{cx1} {cy1} {cx2} {cy2} {x} {y} c");
                        lastControlX = x1;
                        lastControlY = y1;
                        currentX = x;
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'A': // Elliptical arc (MOST COMPLEX!)
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var rx)) break;
                        if (!parser.TryReadNumber(out var ry)) break;
                        if (!parser.TryReadNumber(out var xAxisRotation)) break;
                        if (!parser.TryReadNumber(out var largeArcFlag)) break;
                        if (!parser.TryReadNumber(out var sweepFlag)) break;
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x += currentX;
                            y += currentY;
                        }

                        // Convert elliptical arc to cubic Bézier curves
                        var arcCommands = ConvertArcToCubic(
                            currentX, currentY, x, y,
                            rx, ry, xAxisRotation,
                            largeArcFlag != 0, sweepFlag != 0
                        );

                        pdfCommands.AddRange(arcCommands);
                        currentX = x;
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'Z': // Close path
                {
                    pdfCommands.Add("h");
                    currentX = startX;
                    currentY = startY;
                    break;
                }
            }

            lastCommand = command;
        }

        return pdfCommands;
    }

    /// <summary>
    /// Converts an elliptical arc to cubic Bézier curves.
    /// This is the most complex SVG path operation!
    /// Based on SVG spec: https://www.w3.org/TR/SVG/implnotes.html#ArcImplementationNotes
    /// </summary>
    private static List<string> ConvertArcToCubic(
        double x1, double y1, double x2, double y2,
        double rx, double ry, double phi,
        bool largeArc, bool sweep)
    {
        var commands = new List<string>();

        // Handle degenerate cases
        if (x1 == x2 && y1 == y2)
            return commands; // Zero-length arc

        if (rx == 0 || ry == 0)
        {
            // Degenerate to line
            commands.Add($"{x2} {y2} l");
            return commands;
        }

        rx = Math.Abs(rx);
        ry = Math.Abs(ry);

        var phiRad = phi * Math.PI / 180.0;
        var cosPhi = Math.Cos(phiRad);
        var sinPhi = Math.Sin(phiRad);

        // Step 1: Compute center point
        var dx = (x1 - x2) / 2.0;
        var dy = (y1 - y2) / 2.0;
        var x1p = cosPhi * dx + sinPhi * dy;
        var y1p = -sinPhi * dx + cosPhi * dy;

        // Correct radii if needed
        var lambda = (x1p * x1p) / (rx * rx) + (y1p * y1p) / (ry * ry);
        if (lambda > 1)
        {
            rx *= Math.Sqrt(lambda);
            ry *= Math.Sqrt(lambda);
        }

        // Compute center
        var sign = largeArc != sweep ? 1 : -1;
        var sq = Math.Max(0, (rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p) /
                              (rx * rx * y1p * y1p + ry * ry * x1p * x1p));
        var coef = sign * Math.Sqrt(sq);
        var cxp = coef * rx * y1p / ry;
        var cyp = -coef * ry * x1p / rx;

        var cx = cosPhi * cxp - sinPhi * cyp + (x1 + x2) / 2.0;
        var cy = sinPhi * cxp + cosPhi * cyp + (y1 + y2) / 2.0;

        // Compute angles
        var theta1 = Math.Atan2((y1p - cyp) / ry, (x1p - cxp) / rx);
        var theta2 = Math.Atan2((-y1p - cyp) / ry, (-x1p - cxp) / rx);

        var dTheta = theta2 - theta1;

        if (sweep && dTheta < 0)
            dTheta += 2 * Math.PI;
        else if (!sweep && dTheta > 0)
            dTheta -= 2 * Math.PI;

        // Convert to cubic Bézier curves (use multiple curves for large arcs)
        var segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(dTheta) / (Math.PI / 2.0)));
        var deltaTheta = dTheta / segments;

        for (int i = 0; i < segments; i++)
        {
            var t1 = theta1 + i * deltaTheta;
            var t2 = t1 + deltaTheta;

            // Convert arc segment to cubic Bézier
            var alpha = Math.Sin(t2 - t1) * (Math.Sqrt(4 + 3 * Math.Tan((t2 - t1) / 2) * Math.Tan((t2 - t1) / 2)) - 1) / 3;

            var cosT1 = Math.Cos(t1);
            var sinT1 = Math.Sin(t1);
            var cosT2 = Math.Cos(t2);
            var sinT2 = Math.Sin(t2);

            var q1x = cosT1;
            var q1y = sinT1;
            var q2x = cosT2;
            var q2y = sinT2;

            var cp1x = q1x - q1y * alpha;
            var cp1y = q1y + q1x * alpha;
            var cp2x = q2x + q2y * alpha;
            var cp2y = q2y - q2x * alpha;

            // Transform back to original coordinate system
            var p1x = cosPhi * rx * cp1x - sinPhi * ry * cp1y + cx;
            var p1y = sinPhi * rx * cp1x + cosPhi * ry * cp1y + cy;
            var p2x = cosPhi * rx * cp2x - sinPhi * ry * cp2y + cx;
            var p2y = sinPhi * rx * cp2x + cosPhi * ry * cp2y + cy;
            var px = cosPhi * rx * q2x - sinPhi * ry * q2y + cx;
            var py = sinPhi * rx * q2x + cosPhi * ry * q2y + cy;

            commands.Add($"{p1x} {p1y} {p2x} {p2y} {px} {py} c");
        }

        return commands;
    }

    /// <summary>
    /// Calculates the bounding box of an SVG path by tracking min/max coordinates.
    /// Returns (minX, minY, width, height) or null if path is empty.
    /// </summary>
    public static (double x, double y, double width, double height)? CalculateBoundingBox(string pathData)
    {
        var parser = new PathDataParser(pathData);

        double currentX = 0, currentY = 0;
        double startX = 0, startY = 0;
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        bool hasPoints = false;

        void TrackPoint(double x, double y)
        {
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
            hasPoints = true;
        }

        while (parser.HasMore())
        {
            var command = parser.ReadCommand();
            if (command == ' ') break;

            var isRelative = char.IsLower(command);
            var commandUpper = char.ToUpper(command);

            switch (commandUpper)
            {
                case 'M': // Move to
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x += currentX;
                            y += currentY;
                        }

                        currentX = x;
                        currentY = y;
                        startX = x;
                        startY = y;
                        TrackPoint(x, y);

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'L': // Line to
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x += currentX;
                            y += currentY;
                        }

                        currentX = x;
                        currentY = y;
                        TrackPoint(x, y);

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'H': // Horizontal line
                {
                    while (parser.TryReadNumber(out var x))
                    {
                        if (isRelative)
                            x += currentX;

                        currentX = x;
                        TrackPoint(x, currentY);

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'V': // Vertical line
                {
                    while (parser.TryReadNumber(out var y))
                    {
                        if (isRelative)
                            y += currentY;

                        currentY = y;
                        TrackPoint(currentX, y);

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'C': // Cubic Bézier
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x1)) break;
                        if (!parser.TryReadNumber(out var y1)) break;
                        if (!parser.TryReadNumber(out var x2)) break;
                        if (!parser.TryReadNumber(out var y2)) break;
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x1 += currentX; y1 += currentY;
                            x2 += currentX; y2 += currentY;
                            x += currentX; y += currentY;
                        }

                        // Track all points (conservative bbox)
                        TrackPoint(x1, y1);
                        TrackPoint(x2, y2);
                        TrackPoint(x, y);

                        currentX = x;
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'S': // Smooth cubic Bézier
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x2)) break;
                        if (!parser.TryReadNumber(out var y2)) break;
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x2 += currentX; y2 += currentY;
                            x += currentX; y += currentY;
                        }

                        TrackPoint(x2, y2);
                        TrackPoint(x, y);

                        currentX = x;
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'Q': // Quadratic Bézier
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x1)) break;
                        if (!parser.TryReadNumber(out var y1)) break;
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x1 += currentX; y1 += currentY;
                            x += currentX; y += currentY;
                        }

                        TrackPoint(x1, y1);
                        TrackPoint(x, y);

                        currentX = x;
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'T': // Smooth quadratic Bézier
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x += currentX;
                            y += currentY;
                        }

                        TrackPoint(x, y);

                        currentX = x;
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'A': // Elliptical arc
                {
                    while (true)
                    {
                        if (!parser.TryReadNumber(out var rx)) break;
                        if (!parser.TryReadNumber(out var ry)) break;
                        if (!parser.TryReadNumber(out var angle)) break;
                        if (!parser.TryReadNumber(out var largeArcFlag)) break;
                        if (!parser.TryReadNumber(out var sweepFlag)) break;
                        if (!parser.TryReadNumber(out var x)) break;
                        if (!parser.TryReadNumber(out var y)) break;

                        if (isRelative)
                        {
                            x += currentX;
                            y += currentY;
                        }

                        // Track start and end points (conservative bbox)
                        TrackPoint(currentX, currentY);
                        TrackPoint(x, y);

                        // For better accuracy, we could also track the arc extrema
                        // but this conservative bbox is sufficient for gradients

                        currentX = x;
                        currentY = y;

                        if (!parser.PeekNumber()) break;
                    }
                    break;
                }

                case 'Z': // Close path
                {
                    currentX = startX;
                    currentY = startY;
                    break;
                }
            }
        }

        if (!hasPoints)
            return null;

        return (minX, minY, maxX - minX, maxY - minY);
    }
}

/// <summary>
/// Helper class to parse path data string.
/// </summary>
internal class PathDataParser
{
    private readonly string _data;
    private int _position;

    public PathDataParser(string data)
    {
        _data = data;
        _position = 0;
    }

    public bool HasMore()
    {
        SkipWhitespace();
        return _position < _data.Length;
    }

    public char ReadCommand()
    {
        SkipWhitespace();
        if (_position >= _data.Length) return ' ';

        var ch = _data[_position];
        if (char.IsLetter(ch))
        {
            _position++;
            return ch;
        }

        return ' ';
    }

    public bool TryReadNumber(out double value)
    {
        SkipWhitespace();
        value = 0;

        if (_position >= _data.Length)
            return false;

        var start = _position;
        var hasDecimal = false;
        var hasExponent = false;

        // Handle sign
        if (_data[_position] == '-' || _data[_position] == '+')
            _position++;

        // Read digits
        while (_position < _data.Length)
        {
            var ch = _data[_position];

            if (char.IsDigit(ch))
            {
                _position++;
            }
            else if (ch == '.' && !hasDecimal && !hasExponent)
            {
                hasDecimal = true;
                _position++;
            }
            else if ((ch == 'e' || ch == 'E') && !hasExponent)
            {
                hasExponent = true;
                _position++;

                // Handle exponent sign
                if (_position < _data.Length && (_data[_position] == '-' || _data[_position] == '+'))
                    _position++;
            }
            else
            {
                break;
            }
        }

        if (_position == start)
            return false;

        var str = _data[start.._position];
        return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    public bool PeekNumber()
    {
        SkipWhitespace();
        if (_position >= _data.Length)
            return false;

        var ch = _data[_position];
        return char.IsDigit(ch) || ch == '-' || ch == '+' || ch == '.';
    }

    private void SkipWhitespace()
    {
        while (_position < _data.Length)
        {
            var ch = _data[_position];
            if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n' || ch == ',')
                _position++;
            else
                break;
        }
    }
}
