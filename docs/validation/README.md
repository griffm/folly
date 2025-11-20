# XSL-FO Coverage Validation

This directory contains documentation and tools for validating Folly's XSL-FO 1.1 specification coverage.

## Contents

- **[xslfo-coverage-validation-plan.md](xslfo-coverage-validation-plan.md)** - Comprehensive plan for validating XSL-FO coverage across multiple dimensions (specification conformance, real-world testing, comparative analysis, etc.)

## Coverage Reports

Generated coverage reports will be placed in this directory:

- `coverage-report.json` - Machine-readable coverage metrics
- `coverage-report.md` - Human-readable coverage summary

## Running Coverage Analysis

To generate a coverage report, run the coverage analyzer tool:

```bash
cd tools/CoverageAnalyzer
dotnet run
```

This will:
1. Analyze implemented XSL-FO elements in `src/Folly.Xslfo.Model/Dom/`
2. Count test coverage in `tests/`
3. Count examples in `examples/`
4. Generate reports in this directory

## Coverage Metrics

### Element Coverage
Percentage of XSL-FO 1.1 formatting objects implemented.

**Categories tracked:**
- Document Structure (root, page-sequence, layout-master-set, etc.)
- Flow Objects (flow, static-content)
- Block-level (block, block-container)
- Inline-level (inline, character, leader, page-number, etc.)
- Tables (table, table-row, table-cell, etc.)
- Lists (list-block, list-item, etc.)
- Links and Multi (basic-link, multi-*, etc.)
- Out-of-Line (float, footnote)
- Markers (marker, retrieve-marker, retrieve-table-marker)
- Indexing (index-range-begin, index-range-end, etc.)
- Bookmarks (bookmark-tree, bookmark)

### Test Coverage
Number of test files and test methods covering XSL-FO features.

### Example Coverage
Number of working examples demonstrating XSL-FO capabilities.

## Validation Strategy

See [xslfo-coverage-validation-plan.md](xslfo-coverage-validation-plan.md) for the complete validation strategy, including:

1. **Specification Conformance Testing** - Element/property matrix, W3C test suites
2. **Real-World Document Corpus** - Industry documents (business, publishing, forms, localization)
3. **Comparative Analysis** - Benchmarking against Apache FOP and other processors
4. **Edge Case Testing** - Pathological inputs and error handling
5. **Regression Testing** - Visual regression and PDF structure validation
6. **Feature Coverage Metrics** - Automated coverage reporting
7. **Industry-Specific Validation** - DocBook, DITA integration
8. **Community Validation** - Beta testing and user feedback
9. **Compliance Certification** - Formal conformance documentation

## Contributing

To improve XSL-FO coverage validation:

1. Add test cases for unimplemented features
2. Contribute real-world XSL-FO documents to the corpus
3. Enhance the coverage analyzer tool
4. Document XSL-FO compatibility issues
5. Compare output with other processors

## Resources

- **XSL-FO 1.1 Specification**: https://www.w3.org/TR/xsl11/
- **Apache FOP** (reference implementation): https://xmlgraphics.apache.org/fop/
- **PDF Validation**: `qpdf --check output.pdf`
- **Visual Inspection**: `pdftoppm -png -r 150 output.pdf page`

## Roadmap

See the [validation plan](xslfo-coverage-validation-plan.md#10-implementation-roadmap) for phased implementation timeline.

**Current Phase**: Foundation (Q1 2025)
- [x] Create validation plan
- [x] Create coverage analyzer tool
- [ ] Generate initial coverage report
- [ ] Document current coverage baseline

**Next Phase**: Corpus Building (Q1-Q2 2025)
- [ ] Collect 50+ real-world XSL-FO documents
- [ ] Create benchmark suite structure
- [ ] Validate all corpus documents
