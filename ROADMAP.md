# Folly Roadmap

## Current Status (January 2025)

### Completed Milestones

**Milestone M0: Foundation** ✅ (Completed)
- XSL-FO 1.1 XML parsing with namespace support
- Immutable FO DOM representation
- Complete property inheritance system
- Layout master sets and page masters

**Milestone M1: Basic Layout** ✅ (Completed)
- Multi-page layout with automatic pagination
- Line breaking algorithms (Greedy and Knuth-Plass)
- Professional hyphenation (Liang's algorithm, 4 languages)
- Text alignment and justification
- Font metrics and text measurement

**Milestone M2: Tables, Images, Lists** ✅ (Completed)
- Complex table layouts with automatic page breaking
- Table header and footer repetition
- Row and column spanning
- Image embedding (JPEG, PNG, BMP, GIF, TIFF) with zero dependencies
- List formatting with keep-together support

**Milestone M3: Pagination Mastery** ✅ (Completed)
- Keep constraints (keep-together, keep-with-next/previous)
- Widow and orphan control
- Markers for dynamic content
- Footnotes with separators
- Multi-column layout
- Conditional page masters

### Current Milestone

**Milestone M4: Full Spec & Polish** 🚧 (In Progress)
- Advanced formatting objects (floats, absolute positioning)
- Index generation
- Unicode BiDi (UAX#9) for RTL languages
- SVG embedding and rendering
- Rounded corners (border-radius)
- Performance optimization and comprehensive testing

---

## Package Architecture

Folly has been successfully refactored into a modular suite of composable libraries:

### Tier 1: Foundation Libraries (Zero Dependencies)
- **Folly.Typography** - Text layout primitives (BiDi, hyphenation, line breaking)
- **Folly.Images** - Image format parsers (JPEG, PNG, BMP, GIF, TIFF)
- **Folly.Svg** - Complete SVG 1.1 parser with CSS support
- **Folly.Fonts** - TrueType/OpenType font parsing and embedding

### Tier 2: Layout Abstraction
- **Folly.Layout** - Format-agnostic Area Tree intermediate representation

### Tier 3: Format-Specific Libraries
- **Folly.Xslfo.Model** - XSL-FO DOM and property system
- **Folly.Xslfo.Layout** - Layout engine (FO DOM → Area Tree)
- **Folly.Pdf.Core** - PDF 1.7 generation and rendering

### Tier 4: Composition
- **Folly.Core** - High-level API orchestrating all components
- **Folly.Fluent** - Fluent API for programmatic document construction

**Benefits:**
- Independent packages usable outside XSL-FO context
- Clear separation of concerns with explicit dependencies
- Improved testability (components tested in isolation)
- Zero breaking changes for existing users

---

## Upcoming Work

### High Priority: NuGet Publishing

**Status:** All packages ready, pending publication

**Packages to publish:**
1. Folly.Typography
2. Folly.Images
3. Folly.Svg
4. Folly.Fonts (update)
5. Folly.Layout
6. Folly.Xslfo.Model
7. Folly.Xslfo.Layout
8. Folly.Pdf.Core
9. Folly.Core
10. Folly.Fluent

**Blockers:** None - all tests passing, zero warnings, zero errors

### M4 Completion Items

**High Priority:**
- [ ] Additional XSL-FO 1.1 specification coverage
- [ ] Performance optimization for very large documents (1000+ pages)
- [ ] Enhanced error messages with XPath locations for debugging
- [ ] Comprehensive documentation review and updates

**Medium Priority:**
- [ ] Advanced SVG features (masks, advanced filters)
- [ ] Additional hyphenation languages beyond current 4
- [ ] Font loading and caching optimizations
- [ ] Memory profiling for large document generation

**Low Priority:**
- [ ] API documentation generation (DocFX or similar)
- [ ] Additional working examples
- [ ] Video tutorials and expanded guides

---

## Future Directions

### Milestone M5: Production Hardening (Proposed)

**Goals:**
- Production-grade error handling and diagnostics
- Extensive logging with configurable levels
- Performance monitoring and profiling hooks
- Stress testing with extremely large documents (10k+ pages)
- Thread safety analysis and concurrent generation support

**Estimated Timeline:** Q2 2025

### Beyond M5: Ecosystem Expansion

**New Input Formats:**
- HTML to PDF (using Folly.Layout + HTML parser)
- Markdown to PDF converter
- ePub generation support
- RTF document support

**Advanced PDF Features:**
- Interactive PDF forms
- Digital signatures
- PDF/A-2 and PDF/A-3 compliance
- Tagged PDF for accessibility (Section 508, WCAG)
- PDF/X support for print production

**Typography Enhancements:**
- OpenType feature support (GPOS, GSUB tables)
- Complex script shaping (Arabic, Hebrew, Indic scripts)
- Variable font support
- Color fonts (COLR/CPAL tables)
- Ligatures and contextual alternates

**Developer Experience:**
- Visual XSL-FO editor
- PDF inspection and debugging tools
- Performance profiler and analyzer
- Live preview capabilities for rapid development
- VS Code extension for XSL-FO authoring

### Long-Term Vision

Transform Folly from an XSL-FO renderer into a **comprehensive document generation platform** where:
- Multiple input formats can be processed
- Layout engine serves as universal intermediate representation
- Multiple output formats can be generated (PDF, HTML, ePub, images)
- All components remain zero-dependency and reusable

---

## Version History and Timeline

- **v0.1** - M0/M1 complete (Basic rendering) - 2024 Q3
- **v0.2** - M2 complete (Tables, images, lists) - 2024 Q4
- **v0.3** - M3 complete (Pagination mastery) - 2024 Q4
- **v0.4** - M4 in progress (Full spec & polish) - 2025 Q1
- **v1.0** - Target: M4 complete + NuGet published - 2025 Q1/Q2

---

## Contributing to the Roadmap

We welcome community input on priorities and features:

1. **Check existing issues** on GitHub before proposing new features
2. **Discuss major features** via issues before implementing
3. **Follow contribution guidelines** in README.md
4. **Ensure backward compatibility** for all changes
5. **Maintain zero-dependency principle** for core packages

---

## Performance Goals

Folly aims to maintain excellent performance characteristics:

- **Throughput:** Excellent rendering speed for complex documents
- **Scaling:** Sub-linear O(n) performance for large documents
- **Memory:** Minimal footprint with efficient memory management
- **Latency:** Fast startup with lazy initialization where appropriate

See [docs/guides/performance.md](docs/guides/performance.md) for detailed benchmarks and optimization strategies.

---

## Quality Metrics

- **Test Coverage:** Comprehensive test suite across all packages
- **Zero Warnings:** Clean builds with no compiler warnings
- **PDF Validation:** All output validated with qpdf
- **Specification Compliance:** Strong adherence to XSL-FO 1.1 and PDF 1.7 specs
- **Real-World Testing:** Extensive working examples demonstrating all features

---

## Resources

- **[Main README](README.md)** - Project overview and quick start
- **[Documentation Hub](docs/README.md)** - Comprehensive documentation
- **[Architecture Overview](docs/architecture/overview.md)** - System design and components
- **[Getting Started Guide](docs/guides/getting-started.md)** - Installation and tutorials
- **[Performance Guide](docs/guides/performance.md)** - Performance characteristics
- **[Examples](examples/README.md)** - Extensive working examples

---

## Questions or Suggestions?

- Open an issue on GitHub
- Contribute to discussions
- Submit pull requests

We're committed to making Folly the best zero-dependency document generation library for .NET.
