# Canonical SVG Test Files

This directory contains canonical SVG test files used for testing and validating SVG rendering engines. These files are widely recognized in the SVG community and serve as benchmarks for correctness.

## Directory Structure

```
canonical-tests/
├── ghostscript/        # The famous Ghostscript Tiger
├── w3c/               # W3C SVG Test Suite style examples
└── README.md          # This file
```

## Contents

### Ghostscript Tiger

**File**: `ghostscript/tiger.svg`

The **Ghostscript Tiger** is the most famous SVG test file in existence. It has been the de facto standard for testing vector graphics software since the early 1990s.

- **Original format**: PostScript (tiger.ps)
- **Created**: ~1990 (CreationDate: 4/12/90 3:20 AM)
- **First appearance**: Sun Microsystems SunOS (`/usr/openwin/share/images/PostScript/tiger.ps`)
- **Attributed to**: Michael Scaramozzino (per various internet sources)
- **File size**: ~66 KB
- **Complexity**: Extensive use of paths, fills, strokes, gradients
- **License**: GPL (as part of Ghostscript distribution) / Public domain debate exists
- **Source**: Unity Technologies vector-graphics-samples repository

**Why it's important**: Virtually every SVG renderer uses this file for testing. It contains a complex mix of:
- Bezier curves and complex paths
- Gradients and fills
- Stroke operations
- Transformations
- Overlapping elements

### W3C SVG Test Suite Style Examples

**Directory**: `w3c/`

These are test files created in the style of the W3C SVG 1.1 Test Suite. They follow W3C conventions and are designed to test specific SVG features in isolation.

**Files included**:

1. **shapes-rect-01.svg** - Basic rectangle rendering with fill and stroke
2. **shapes-circle-01.svg** - Circle rendering with various fills and strokes
3. **paths-cubic-bezier-01.svg** - Cubic Bezier curves and smooth curves
4. **painting-fill-gradient-01.svg** - Linear gradient fills
5. **text-basic-01.svg** - Text rendering with various fonts and attributes
6. **transforms-rotate-01.svg** - Rotation transforms

**License**: Created as examples for the Folly project, following W3C SVG specification patterns. These files are effectively test data and follow the W3C SVG 1.1 specification.

## Licensing Information

### Ghostscript Tiger

The Ghostscript Tiger has complex licensing history:

- **Likely License**: GPL (as Ghostscript is GPL-licensed)
- **Alternative view**: Public domain due to age and unclear provenance
- **Usage**: Widely used for testing without restriction in practice
- **For this project**: Included for testing purposes only

**Attribution**: This file is sourced from the Unity Technologies vector-graphics-samples repository, which itself derives from the original Ghostscript distribution.

### W3C Style Tests

The W3C test suite is dual-licensed:

1. **3-Clause BSD License** - For software development and testing (allows modifications)
2. **W3C Test Suite License** - For authoritative conformance testing (no modifications)

For development and testing purposes (like this project), the **3-Clause BSD License** applies, which permits:
- Copying and distribution
- Modification
- Integration into software
- Commercial use

The tests in `w3c/` are created following W3C SVG specification examples and conventions.

## Usage Guidelines

### For Testing

All files in this directory are suitable for:
- Rendering engine validation
- Performance benchmarking
- Visual regression testing
- SVG feature implementation verification

### For Distribution

- **Ghostscript Tiger**: Include GPL license notice if redistributing
- **W3C Style Tests**: Include BSD license notice if redistributing

## References

- W3C SVG 1.1 Specification: https://www.w3.org/TR/SVG11/
- W3C Test Suite Licenses: https://www.w3.org/copyright/test-suites-licenses/
- Ghostscript Project: https://www.ghostscript.com/
- Web Platform Tests (SVG): https://github.com/web-platform-tests/wpt

## Why These Files Matter

These canonical test files serve several critical purposes:

1. **Industry Standards**: The Tiger is universally recognized; passing it means your renderer works
2. **Regression Testing**: Catch rendering bugs early
3. **Feature Coverage**: W3C tests cover specific SVG features systematically
4. **Benchmarking**: Compare performance and correctness against other renderers
5. **Documentation**: Demonstrate what your renderer can handle

## Additional Test Suites

For comprehensive testing, consider also using:

- **Web Platform Tests**: https://github.com/web-platform-tests/wpt (Official W3C test repository)
- **ThorVG Test Suite**: https://github.com/thorvg/thorvg (Modern SVG rendering tests)
- **resvg Test Suite**: https://github.com/RazrFalcon/resvg-test-suite (Comprehensive SVG tests)
