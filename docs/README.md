# Folly Documentation

This directory contains comprehensive documentation for the Folly XSL-FO to PDF rendering library.

## Documentation Structure

### Core Documentation

- **[Architecture Overview](architecture/overview.md)** - High-level system design and component overview
- **[Getting Started Guide](guides/getting-started.md)** - Quick start guide for new users
- **[Performance Guide](guides/performance.md)** - Performance characteristics and optimization strategies
- **[Limitations](guides/limitations.md)** - Known limitations and workarounds

### Architecture Documentation

The `architecture/` directory contains detailed information about internal system design:

- **[Font System](../src/Folly.Fonts/README.md)** - TrueType/OpenType font parsing, subsetting, and embedding
- **[Layout Engine](architecture/layout-engine.md)** - Text layout, line breaking, and pagination
- **[SVG Rendering](architecture/svg-support.md)** - SVG parsing and PDF conversion
- **[PDF Generation](architecture/pdf-generation.md)** - PDF structure and rendering

## Quick Links

- [Main README](../README.md) - Project overview and quick start
- [Roadmap](../ROADMAP.md) - Current status, upcoming features, and future plans
- [Agent Guidelines](../CLAUDE.md) - Development environment setup and guidelines

## For Contributors

When adding new features or making changes, please:

1. Update relevant documentation in this directory
2. Update `guides/limitations.md` to reflect current limitations and completed features
3. Update examples in `../examples/` to demonstrate new features
4. Keep documentation professional and concise
