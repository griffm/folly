# XSL:FO Coverage Validation Plan

## Overview
This document outlines strategies to validate that Folly covers a comprehensive subset of XSL:FO 1.1 needs for production use.

## 1. Specification Conformance Testing

### 1.1 XSL-FO 1.1 Element Coverage Matrix

**Status**: To be created

Create a comprehensive matrix tracking all XSL-FO 1.1 elements:

| Category | Total Elements | Implemented | Tested | Coverage % |
|----------|---------------|-------------|---------|------------|
| Document Structure | 8 | TBD | TBD | TBD |
| Page Layout | 12 | TBD | TBD | TBD |
| Block-level | 15 | TBD | TBD | TBD |
| Inline-level | 12 | TBD | TBD | TBD |
| Tables | 10 | TBD | TBD | TBD |
| Lists | 4 | TBD | TBD | TBD |
| Links & Pagination | 8 | TBD | TBD | TBD |
| Out-of-line | 6 | TBD | TBD | TBD |
| **Total** | **~75** | **~65** | **~55** | **~85%** |

**Action Items:**
1. Parse XSL-FO 1.1 specification XML/HTML
2. Extract all element and property definitions
3. Map to Folly's implementation in `src/Folly.Xslfo.Model/Dom/`
4. Generate automated coverage report
5. Identify gaps in core vs extended conformance

### 1.2 Property Coverage Matrix

**Status**: Partial (see `limitations.md`)

Track all ~200+ XSL-FO properties:

**Currently Documented:**
- ✅ 50+ inheritable properties implemented
- ✅ Font properties (complete)
- ✅ Text properties (comprehensive)
- ✅ Border/padding/margin (complete)
- ✅ Keep/break constraints (partial)

**Need Systematic Tracking:**
- [ ] Catalog all properties from spec section 7
- [ ] Mark implementation status (full/partial/none)
- [ ] Note limitations and workarounds
- [ ] Track property inheritance behavior
- [ ] Test computed values vs specified values

### 1.3 W3C Test Suite Integration

**Goal**: Run W3C XSL-FO conformance tests

**Approach:**
1. Research availability of W3C XSL-FO test suite
2. If available, integrate into CI pipeline
3. If not available, create equivalent based on spec examples
4. Document pass/fail rates
5. Investigate failures and document intentional deviations

**Estimated Coverage Target**: 90%+ of core conformance tests

---

## 2. Real-World Document Corpus

### 2.1 Industry Document Categories

Build test corpus representing production XSL-FO usage:

#### Business Documents
- [x] Invoices (example 06)
- [x] Letterheads (example 31)
- [ ] Purchase orders with complex tables
- [ ] Multi-page statements with running totals
- [ ] Business reports with embedded charts
- [ ] Financial statements (balance sheets, P&L)
- [ ] Contracts with signature blocks

#### Publishing
- [x] Books with chapters (Flatland 21a/21b)
- [x] Table of contents with leaders (example 44 - index)
- [ ] Academic papers with footnotes/endnotes
- [ ] Technical documentation with cross-references
- [ ] Magazines with complex layouts
- [ ] Conference proceedings
- [ ] Catalogs with product listings

#### Forms and Templates
- [ ] Government tax forms (IRS 1040 equivalent)
- [ ] Medical records with privacy footers
- [ ] Insurance claim forms
- [ ] Legal contracts with numbered clauses
- [ ] Application forms with checkboxes
- [ ] Survey forms with tables

#### Localization
- [x] Arabic/Hebrew RTL text (example 35)
- [ ] Chinese technical manual (vertical text)
- [ ] Japanese business card (mixed orientation)
- [ ] Mixed LTR/RTL document (English + Arabic)
- [ ] Thai text with complex scripts
- [ ] Multi-language product manual

#### Technical Documents
- [x] Software documentation with code blocks
- [ ] API reference with tables
- [ ] User manuals with diagrams
- [ ] Safety data sheets (MSDS)
- [ ] Product specifications

### 2.2 Document Complexity Metrics

For each document in the corpus, track:

| Metric | Description | Target Coverage |
|--------|-------------|-----------------|
| Page count | Single to 1000+ pages | All ranges |
| Table complexity | Rows, columns, spanning | Up to 50x50 |
| Image count | Embedded graphics | 0-100+ images |
| Font diversity | Number of fonts used | 1-10 fonts |
| Formatting objects | Unique FO elements used | All implemented |
| RTL content | Percentage RTL text | 0-100% |
| List depth | Nested list levels | Up to 5 levels |
| Footnote count | Footnotes per page | 0-20 |
| Cross-references | Internal links | 0-100+ |

### 2.3 Benchmark Suite Structure

```
tests/Folly.BenchmarkCorpus/
├── business/
│   ├── invoice-simple.fo
│   ├── invoice-complex.fo
│   ├── purchase-order.fo
│   ├── financial-statement.fo
│   └── contract.fo
├── publishing/
│   ├── book-chapter.fo
│   ├── academic-paper.fo
│   ├── magazine-article.fo
│   └── technical-manual.fo
├── forms/
│   ├── tax-form.fo
│   ├── medical-record.fo
│   └── application.fo
├── localization/
│   ├── chinese-vertical.fo
│   ├── arabic-rtl.fo
│   ├── mixed-direction.fo
│   └── thai-complex.fo
└── stress/
    ├── 1000-page-report.fo
    ├── 500-page-table.fo
    └── 100-images.fo
```

**Validation Criteria:**
- PDF validates with `qpdf --check`
- Visual inspection for correctness
- Performance within acceptable bounds
- Memory usage reasonable
- No crashes or exceptions

---

## 3. Comparative Analysis

### 3.1 Benchmark Against Other Processors

Compare Folly against established XSL-FO processors:

**Commercial Processors:**
- Apache FOP (open source, widely used)
- RenderX XEP
- Antenna House Formatter
- Ibex PDF Creator

**Comparison Methodology:**

1. **Feature Parity Matrix:**
   - Process same document through multiple processors
   - Compare supported features
   - Document behavioral differences
   - Note Folly's unique strengths (zero dependencies, performance)

2. **Output Quality Comparison:**
   - Visual diff of PDFs
   - Text extraction accuracy
   - Font rendering quality
   - Table layout precision
   - Image embedding fidelity

3. **Performance Benchmarks:**
   - Processing time for identical documents
   - Memory consumption
   - Scalability to large documents
   - Startup overhead

4. **Compliance Comparison:**
   - How each handles edge cases
   - Spec interpretation differences
   - Error handling approaches

### 3.2 Document Compatibility Testing

**Goal**: Process real-world XSL-FO documents created for other processors

**Sources:**
- DocBook XSL-FO stylesheets output
- DITA Open Toolkit FO output
- Commercial CMS-generated documents
- Open-source project documentation

**Success Metrics:**
- Percentage of documents that render without errors
- Visual similarity to reference output
- Required modifications for compatibility

---

## 4. Specification Edge Case Testing

### 4.1 XSL-FO 1.1 Specification Examples

**Action**: Extract all examples from XSL-FO 1.1 specification

The spec contains numerous illustrative examples. Each should:
1. Be extracted into separate test files
2. Processed through Folly
3. Validated against expected behavior
4. Documented in test suite

**Example Sections to Cover:**
- Section 6.2: Formatting Objects (all element examples)
- Section 6.4: Flow Objects
- Section 6.6: Block-level Objects
- Section 6.7: Inline-level Objects
- Section 6.8: Table Objects
- Section 6.9: List Objects
- Section 6.11: Link and Multi Objects

### 4.2 Edge Case Test Suite

Create tests for pathological cases:

**Layout Edge Cases:**
```
tests/Folly.EdgeCases/
├── LayoutEdgeCases/
│   ├── EmptyBlocks.fo          # Empty elements
│   ├── ZeroWidthTable.fo       # Degenerate tables
│   ├── InfiniteLoop.fo         # Circular references
│   ├── ExtremeNesting.fo       # 100-level deep nesting
│   └── MixedWritingModes.fo    # LR + RL + TB in one doc
├── PropertyEdgeCases/
│   ├── ConflictingProperties.fo # Contradictory settings
│   ├── InvalidUnits.fo          # Malformed length values
│   ├── ExtremeFontSizes.fo      # 1pt and 1000pt fonts
│   └── NegativeDimensions.fo    # Negative margins/padding
├── PaginationEdgeCases/
│   ├── SingleLinePages.fo       # Forced tiny pages
│   ├── UnbreakableContent.fo    # Content larger than page
│   ├── AllKeepTogether.fo       # Everything marked keep
│   └── ConflictingBreaks.fo     # Keep vs force-break
└── ContentEdgeCases/
    ├── UnicodeExtreme.fo        # Surrogate pairs, combining
    ├── EmptyPageSequence.fo     # No content
    ├── ThousandTables.fo        # Extreme element count
    └── MalformedXml.fo          # Invalid XSL-FO
```

---

## 5. Automated Regression Testing

### 5.1 Visual Regression Testing

**Goal**: Detect unintended rendering changes

**Implementation:**
1. Generate PDF for each example
2. Convert PDF to images (using poppler-utils)
3. Store reference images
4. Compare new renders against references
5. Flag visual differences for review

**Tools:**
- `pdftoppm` for PDF to image conversion
- Image diff tools (ImageMagick `compare`)
- CI integration to catch regressions

**Example CI Job:**
```bash
#!/bin/bash
# Generate PDFs
dotnet run --project examples/Folly.Examples

# Convert to images
for pdf in examples/output/*.pdf; do
    pdftoppm -png -r 150 "$pdf" "${pdf%.pdf}"
done

# Compare against baseline
for img in examples/output/*.png; do
    compare -metric AE "$img" "baseline/$img" "diff/$img" || echo "Visual diff detected"
done
```

### 5.2 PDF Structure Validation

**Automated Checks:**
- qpdf validation (already implemented)
- PDF/A compliance checking (when implemented)
- Accessibility checker (PDF/UA)
- Text extraction tests (ensure searchability)
- Font embedding verification
- Hyperlink validation
- Bookmark structure validation

**Example Test:**
```csharp
[Theory]
[MemberData(nameof(AllExamplePdfs))]
public void AllExamples_ShouldProduceValidPdf(string pdfPath)
{
    var result = RunQpdfCheck(pdfPath);
    Assert.True(result.IsValid, result.ErrorMessage);
}

[Theory]
[MemberData(nameof(AllExamplePdfs))]
public void AllExamples_ShouldHaveExtractableText(string pdfPath)
{
    var text = ExtractTextFromPdf(pdfPath);
    Assert.NotEmpty(text);
}
```

---

## 6. Feature Coverage Metrics

### 6.1 Automated Coverage Report

**Goal**: Generate machine-readable coverage metrics

**Implementation:**

```csharp
// Coverage analyzer that scans codebase
public class XslFoCoverageAnalyzer
{
    public CoverageReport AnalyzeCoverage()
    {
        var report = new CoverageReport();

        // Scan all FO element classes
        var elementTypes = ScanImplementedElements();
        report.ElementCoverage = CalculateCoverage(elementTypes);

        // Scan property implementations
        var properties = ScanImplementedProperties();
        report.PropertyCoverage = CalculateCoverage(properties);

        // Scan test coverage
        var tests = ScanTestFiles();
        report.TestCoverage = CalculateTestCoverage(tests);

        return report;
    }
}
```

**Output Format (JSON):**
```json
{
  "timestamp": "2025-01-20T12:00:00Z",
  "folly_version": "0.4.0",
  "specification": "XSL-FO 1.1",
  "coverage": {
    "elements": {
      "total": 75,
      "implemented": 65,
      "tested": 55,
      "percentage": 86.7
    },
    "properties": {
      "total": 200,
      "implemented": 150,
      "tested": 120,
      "percentage": 75.0
    },
    "categories": {
      "document_structure": { "coverage": 100.0 },
      "page_layout": { "coverage": 95.0 },
      "block_level": { "coverage": 90.0 },
      "inline_level": { "coverage": 85.0 },
      "tables": { "coverage": 95.0 },
      "lists": { "coverage": 100.0 },
      "links": { "coverage": 80.0 },
      "out_of_line": { "coverage": 70.0 }
    }
  }
}
```

### 6.2 Coverage Visualization

Create visual dashboards:
- GitHub README badges (coverage percentages)
- Documentation site with feature matrix
- Interactive coverage explorer
- Trend charts over time

---

## 7. Industry-Specific Validation

### 7.1 DocBook XSL-FO Integration

**Goal**: Validate compatibility with DocBook toolchain

DocBook is a major XSL-FO use case:

1. Install DocBook XSL stylesheets
2. Generate FO from sample DocBook XML
3. Process through Folly
4. Compare with Apache FOP output
5. Document compatibility issues

**Test Documents:**
- Simple article
- Multi-chapter book
- Reference manual with index
- Technical specification

### 7.2 DITA Open Toolkit Integration

**Goal**: Process DITA-generated XSL-FO

DITA is another major use case:

1. Install DITA-OT
2. Generate FO from DITA topics
3. Process through Folly
4. Validate output
5. Document findings

---

## 8. Community Validation

### 8.1 User Feedback Collection

**Strategies:**
- GitHub issues template: "Does Folly meet your XSL-FO needs?"
- Comparison survey: Users compare Folly to alternatives
- Feature request tracking
- Production deployment case studies

### 8.2 Beta Testing Program

**Approach:**
1. Recruit organizations using XSL-FO in production
2. Provide early access to Folly
3. Collect feedback on missing features
4. Track which features are most requested
5. Document real-world success stories

---

## 9. Compliance Certification

### 9.1 Conformance Levels

Define Folly's conformance level:

**XSL-FO 1.1 Conformance Levels:**
- **Basic**: Minimal implementation
- **Extended**: Advanced features
- **Complete**: Full specification

**Folly's Position:**
- Document target conformance level (likely "Extended")
- List which optional features are implemented
- Clearly state which features are out of scope

### 9.2 Certification Document

Create formal conformance document:

```markdown
# Folly XSL-FO 1.1 Conformance Statement

**Version**: 0.4.0
**Date**: 2025-01-20
**Conformance Level**: Extended

## Implemented Features
- ✅ All required elements (Section X.Y)
- ✅ Core property set (Section X.Y)
- ✅ Table formatting (Section X.Y)
- ✅ Page sequencing (Section X.Y)

## Limitations
- ❌ Interactive elements (fo:multi-*)
- ❌ CJK line breaking rules
- ⚠️ Partial: Writing modes (lr-tb only)

## Test Results
- W3C Test Suite: 90% pass rate
- Real-world corpus: 95% success rate
- Performance benchmarks: Exceeds targets
```

---

## 10. Implementation Roadmap

### Phase 1: Foundation (Q1 2025)
- [ ] Create XSL-FO 1.1 element/property matrix
- [ ] Document current coverage percentages
- [ ] Implement automated coverage analyzer
- [ ] Add W3C spec examples to test suite

### Phase 2: Corpus Building (Q1-Q2 2025)
- [ ] Collect 50+ real-world XSL-FO documents
- [ ] Categorize by industry/use case
- [ ] Create benchmark suite structure
- [ ] Validate all corpus documents

### Phase 3: Comparative Analysis (Q2 2025)
- [ ] Process corpus through Apache FOP
- [ ] Visual comparison of outputs
- [ ] Performance benchmarking
- [ ] Document compatibility findings

### Phase 4: Edge Cases (Q2 2025)
- [ ] Create pathological test suite
- [ ] Test error handling robustness
- [ ] Fuzzing with malformed inputs
- [ ] Document all edge case behaviors

### Phase 5: Regression Automation (Q2-Q3 2025)
- [ ] Implement visual regression testing
- [ ] PDF structure validation suite
- [ ] CI integration for all checks
- [ ] Performance regression detection

### Phase 6: Community Validation (Q3 2025)
- [ ] Beta testing program
- [ ] DocBook/DITA integration testing
- [ ] User feedback collection
- [ ] Production deployment case studies

### Phase 7: Certification (Q4 2025)
- [ ] Publish formal conformance statement
- [ ] Create coverage dashboard
- [ ] Generate compliance report
- [ ] Submit to relevant standards bodies (if applicable)

---

## Success Criteria

Folly can confidently claim "comprehensive XSL-FO coverage" when:

1. **Specification Coverage**: ≥85% of XSL-FO 1.1 elements/properties
2. **Test Coverage**: ≥90% of implemented features have automated tests
3. **Real-World Validation**: ≥95% of corpus documents render correctly
4. **Comparative Performance**: Within 2x of established processors
5. **Community Validation**: Positive feedback from production deployments
6. **Edge Case Handling**: Graceful degradation for unsupported features
7. **Documentation**: Every limitation documented with workarounds

---

## Tools and Resources

### Recommended Tools
- **qpdf**: PDF validation (already in use)
- **poppler-utils**: PDF rendering and comparison
- **Apache FOP**: Reference implementation comparison
- **ImageMagick**: Visual diff generation
- **DocBook XSL**: Industry standard toolchain
- **DITA-OT**: Technical documentation toolchain

### Specification Resources
- XSL-FO 1.1 W3C Recommendation: https://www.w3.org/TR/xsl11/
- PDF 1.7 ISO 32000-1:2008
- UAX#9 Unicode BiDi Algorithm
- OpenType specification

### Testing Frameworks
- xUnit (current test framework)
- BenchmarkDotNet (performance testing)
- Verify (snapshot testing)
- Coverage tools (dotcover, coverlet)

---

## Conclusion

This validation plan provides a comprehensive, multi-faceted approach to ensuring Folly covers a robust subset of XSL:FO needs. By combining specification conformance, real-world testing, comparative analysis, and community validation, we can confidently assess and communicate Folly's capabilities.

**Next Steps:**
1. Review and approve this validation plan
2. Prioritize phases based on release timeline
3. Assign resources and timeline
4. Begin Phase 1 implementation

**Maintenance:**
- Update this document as validation progresses
- Track metrics in automated reports
- Publish results in documentation
- Use findings to guide feature prioritization
