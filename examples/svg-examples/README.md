# SVG Examples

This directory contains a comprehensive collection of SVG (Scalable Vector Graphics) examples demonstrating various features and capabilities of the SVG specification. These examples are useful for testing SVG rendering implementations, learning SVG syntax, and understanding different SVG features.

## Directory Structure

```
svg-examples/
├── basic-shapes/      # Fundamental SVG shapes
├── paths/             # Path elements and curves
├── text/              # Text rendering and text-on-path
├── gradients/         # Linear and radial gradients
├── transforms/        # Transformations (translate, rotate, scale)
└── complex/           # Advanced features and combinations
```

## Examples Overview

### Basic Shapes (`basic-shapes/`)

Demonstrates the fundamental SVG shape elements:

- **01-rectangle.svg** - Basic rectangle with fill and stroke
- **02-circle.svg** - Simple circle element
- **03-ellipse.svg** - Ellipse with different horizontal and vertical radii
- **04-line.svg** - Line elements with different stroke styles (solid, dashed)
- **05-polygon.svg** - Closed polygons (triangle, pentagon)
- **06-polyline.svg** - Connected line segments (wave pattern)
- **07-rounded-rectangle.svg** - Rectangle with rounded corners using `rx` and `ry`

**SVG Elements Covered:** `<rect>`, `<circle>`, `<ellipse>`, `<line>`, `<polygon>`, `<polyline>`

### Paths (`paths/`)

Demonstrates the powerful path element and various path commands:

- **01-straight-path.svg** - Path with straight lines using M (moveto) and L (lineto)
- **02-curved-path.svg** - Quadratic Bezier curves using Q command
- **03-cubic-bezier.svg** - Cubic Bezier curves using C command
- **04-arc.svg** - Elliptical arcs using A command
- **05-complex-path.svg** - Complex heart shape combining multiple commands

**Path Commands Covered:** M (moveto), L (lineto), Q (quadratic bezier), C (cubic bezier), A (arc), Z (closepath)

### Text (`text/`)

Demonstrates text rendering capabilities:

- **01-basic-text.svg** - Simple text with various styling (bold, italic, outlined)
- **02-text-path.svg** - Text following a curved path using `<textPath>`
- **03-multiline-text.svg** - Multiple lines of text using `<tspan>` elements

**SVG Elements Covered:** `<text>`, `<textPath>`, `<tspan>`

### Gradients (`gradients/`)

Demonstrates gradient fills:

- **01-linear-gradient.svg** - Linear gradients (horizontal, vertical, diagonal)
- **02-radial-gradient.svg** - Radial gradients with different centers and multiple stops

**SVG Elements Covered:** `<linearGradient>`, `<radialGradient>`, `<stop>`

### Transforms (`transforms/`)

Demonstrates coordinate transformations:

- **01-translate.svg** - Translation (moving elements)
- **02-rotate.svg** - Rotation around a point
- **03-scale.svg** - Scaling elements
- **04-combined.svg** - Multiple transformations combined

**Transform Functions Covered:** `translate()`, `rotate()`, `scale()`

### Complex Features (`complex/`)

Demonstrates advanced SVG features and combinations:

- **01-grouped-elements.svg** - Using `<g>` to group and transform multiple elements (house, tree, sun scene)
- **02-clipping-masking.svg** - Clipping paths to restrict rendering regions
- **03-patterns.svg** - Pattern fills (checkerboard, stripes, polka dots)
- **04-filters.svg** - Filter effects (blur, drop shadow, glow)
- **05-viewbox-example.svg** - ViewBox scaling and coordinate systems
- **06-markers.svg** - Markers for arrowheads and decorative endpoints

**SVG Elements Covered:** `<g>`, `<clipPath>`, `<pattern>`, `<filter>`, `<marker>`, `<defs>`, `<use>`

## SVG Features Summary

This collection demonstrates:

- ✅ Basic shapes (rectangles, circles, ellipses, lines, polygons, polylines)
- ✅ Complex paths with Bezier curves and arcs
- ✅ Text rendering and text-on-path
- ✅ Linear and radial gradients
- ✅ Transformations (translate, rotate, scale)
- ✅ Groups and element organization
- ✅ Clipping paths
- ✅ Pattern fills
- ✅ Filter effects
- ✅ Markers
- ✅ ViewBox and coordinate systems
- ✅ Stroke properties (width, dasharray, linecap, linejoin)
- ✅ Fill and stroke styling
- ✅ Opacity and transparency

## Usage

These examples can be:

1. **Viewed in any modern web browser** - Open the SVG files directly
2. **Used for testing** - Validate SVG rendering implementations
3. **Learning resources** - Study the well-commented SVG code
4. **Integration tests** - Automated testing of SVG parsers and renderers

## Testing with Folly

To use these examples with the Folly PDF library:

```bash
# From the project root
dotnet run --project examples/Folly.Examples/Folly.Examples.csproj
```

The examples can be integrated into the Folly.Examples project to test SVG-to-PDF conversion capabilities.

## Technical Details

- **Encoding:** UTF-8
- **SVG Version:** Compatible with SVG 1.1 and SVG 2.0
- **Namespace:** `http://www.w3.org/2000/svg`
- **Structure:** All files include proper XML declaration, title, and description elements

## Color Palette

The examples use a consistent color palette inspired by Flat UI Colors:

- Primary Blue: `#3498DB`
- Primary Red: `#E74C3C`
- Primary Green: `#27AE60`
- Purple: `#9B59B6`
- Orange: `#E67E22`, `#F39C12`
- Teal: `#1ABC9C`
- Dark Gray: `#2C3E50`

## License

These SVG examples were created specifically for the Folly project and are available under the same MIT License as the Folly project itself. They are designed to be simple, well-documented examples suitable for testing and educational purposes.

## Contributing

When adding new SVG examples:

1. Place them in the appropriate category directory
2. Use clear, descriptive filenames with numerical prefixes
3. Include `<title>` and `<desc>` elements
4. Add inline comments explaining key features
5. Update this README with the new example
6. Follow the established color palette and coding style

## References

- [SVG Specification](https://www.w3.org/TR/SVG2/)
- [MDN SVG Documentation](https://developer.mozilla.org/en-US/docs/Web/SVG)
- [SVG Path Commands](https://developer.mozilla.org/en-US/docs/Web/SVG/Tutorial/Paths)

## Total Examples

- **Basic Shapes:** 7 examples
- **Paths:** 5 examples
- **Text:** 3 examples
- **Gradients:** 2 examples
- **Transforms:** 4 examples
- **Complex:** 6 examples

**Total: 27 SVG examples**

---

Created for the Folly PDF Library
Last Updated: 2025-11-15
