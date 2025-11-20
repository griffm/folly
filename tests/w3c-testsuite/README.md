# XSL-FO Test Suites

This directory contains test suites from various sources for validating Folly's XSL-FO implementation.

## Test Suite Sources

### 1. Apache FOP Test Suite

**Source**: https://github.com/apache/xmlgraphics-fop

**Location**: `apache-fop/` and `apache-fop-examples/`

**Contents**:
- **768 layout engine test cases** (total in FOP repository)
- **50 downloaded test cases** covering major features (representative sample)
- **3 example FO files** (simple.fo, normal.fo, extensive.fo)

**Test Format**:

Apache FOP uses a custom XML test format:

```xml
<testcase>
  <info>
    <p>Description of what this test validates</p>
  </info>
  <fo>
    <!-- Standard XSL-FO content here -->
    <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
      ...
    </fo:root>
  </fo>
  <checks>
    <!-- XPath assertions to validate output -->
    <eval expected="value" xpath="//flow/block[1]/..."/>
  </checks>
</testcase>
```

To use these tests with Folly:
1. Extract the `<fo>` section content
2. Process through Folly's layout engine
3. Validate output against `<checks>` assertions (if implementing test runner)
4. Or manually inspect PDF output for correctness

**Coverage Areas**:

The downloaded test sample includes:
- ✅ Block-level elements (absolute-position, background-image, borders, fonts, line-height, spacing, white-space)
- ✅ Inline elements (nested blocks, containers, letter-spacing, word-spacing)
- ✅ Tables (border-collapse, border-width, column spanning, row spanning, headers)
- ✅ Lists (list-block with spacing)
- ✅ Markers (basic marker tests)
- ✅ Page breaking (multiple scenarios)
- ✅ Page numbers and citations
- ✅ Keep and break constraints (keep-together, keep-with-next/previous, break-before/after)
- ✅ Footnotes
- ✅ Images (external-graphic)
- ✅ Links (external and internal destinations)
- ✅ Leaders
- ✅ Writing modes (right-to-left, top-to-bottom)

**Statistics**:
- Total FOP test cases: 768
- Downloaded samples: 50
- Example files: 3
- Categories covered: 15+

### 2. W3C XSL-FO 1.1 Test Suite (Attempted)

**Official Location**: https://www.w3.org/Style/XSL/TestSuite1.1/

**Status**: ❌ Not successfully downloaded

**Issue**: W3C website returns 403 Forbidden errors when attempting to download the test suite ZIP file directly.

**Alternative Access Methods**:
1. **Manual download**: Visit the W3C test suite page in a browser and download manually
2. **NIST Test Suite**: The NIST (National Institute of Standards and Technology) published a "Conformance Test Suite for XSL-FO" which may be available through academic or government channels
3. **Browser-based download**: Use a web browser to access and download (may require accepting terms of service)

**Known Information**:
- The W3C test suite was contributed by the Working Group and third parties
- Available as a ZIP file from the W3C website
- Includes table of contents at: https://www.w3.org/Style/XSL/TestSuite1.1/testsuite-toc.htm
- Testing procedures documented at: https://www.w3.org/Style/XSL/TestSuite1.1/w3cxsl-fo-1_1-test.html

**To obtain the W3C test suite**:
```bash
# Visit in browser and download manually
open https://www.w3.org/Style/XSL/TestSuite1.1/

# Or if you have the direct ZIP URL, try wget with user agent:
wget --user-agent="Mozilla/5.0" https://www.w3.org/Style/XSL/TestSuite1.1/[filename].zip
```

### 3. NIST XSL-FO Conformance Test Suite

**Reference**: https://www.nist.gov/publications/conformance-test-suite-xsl-fo

**Status**: ❌ Not successfully downloaded (403 Forbidden)

**Description**: The National Institute of Standards and Technology (NIST) developed a conformance test suite for XSL-FO to help verify processor implementations.

**Access**: May require institutional access or contacting NIST directly.

## Using These Tests with Folly

### Quick Start

1. **Extract XSL-FO from test files**:
```bash
# For FOP test files, extract the <fo> section
xmllint --xpath "//fo:root" apache-fop/block_font-family.xml
```

2. **Process through Folly**:
```bash
# Save extracted FO content to a file, then:
dotnet run --project examples/Folly.Examples/Folly.Examples.csproj
```

3. **Validate output**:
```bash
# Check PDF structure
qpdf --check output.pdf

# Visual inspection
pdftoppm -png -r 150 output.pdf page
```

### Creating a Test Runner

To systematically process all test files:

```csharp
using Folly;
using Folly.Pdf;
using System.Xml.Linq;

// Load FOP test case
var testDoc = XDocument.Load("apache-fop/block_font-family.xml");
var foElement = testDoc.Descendants()
    .First(e => e.Name.LocalName == "root" &&
                e.Name.Namespace == "http://www.w3.org/1999/XSL/Format");

// Process through Folly
var foDoc = FoDocument.Parse(foElement.ToString());
using var pdf = File.Create("output.pdf");
foDoc.SavePdf(pdf);

// Validate checks (future enhancement)
var checks = testDoc.Descendants("checks").FirstOrDefault();
// ... implement XPath validation against Folly's area tree
```

## Test Coverage Goals

### Phase 1: Apache FOP Tests (Current)
- [x] Download representative FOP test sample (50 files)
- [ ] Create test runner to extract and process FO content
- [ ] Run all 50 tests through Folly
- [ ] Document pass/fail rate
- [ ] Investigate failures

### Phase 2: Expand FOP Coverage
- [ ] Download all 768 FOP test cases
- [ ] Categorize by XSL-FO feature
- [ ] Prioritize based on Folly's feature support
- [ ] Run comprehensive test suite
- [ ] Target: 90%+ pass rate

### Phase 3: W3C Test Suite
- [ ] Obtain W3C XSL-FO 1.1 test suite (manual download)
- [ ] Understand W3C test format
- [ ] Create test runner for W3C format
- [ ] Process all W3C tests
- [ ] Document conformance level

### Phase 4: Real-World Corpus
- [ ] Collect 50+ real-world XSL-FO documents
- [ ] Process through Folly
- [ ] Measure success rate (target: 95%+)
- [ ] Document compatibility issues

## Test Categories

Based on FOP test coverage, here are the major test categories:

| Category | FOP Tests | Description |
|----------|-----------|-------------|
| Blocks | 100+ | Block-level formatting objects |
| Inlines | 80+ | Inline-level formatting |
| Tables | 150+ | Table layout and properties |
| Lists | 30+ | List formatting |
| Page Layout | 60+ | Page masters and sequences |
| Pagination | 50+ | Page breaking and keeps |
| Markers | 20+ | Dynamic content retrieval |
| Footnotes | 15+ | Footnote handling |
| Links | 40+ | Hyperlinks and destinations |
| Images | 30+ | External graphics |
| SVG | 25+ | SVG integration |
| Leaders | 15+ | Leader elements |
| Writing Modes | 30+ | BiDi and vertical text |
| Keeps/Breaks | 50+ | Keep and break constraints |
| Other | 60+ | Edge cases and advanced features |

## Contributing

To add more test files:

1. Identify test sources (W3C, FOP, DocBook, DITA, etc.)
2. Download test files
3. Document format and structure
4. Update this README
5. Create test runner if needed
6. Add to validation plan

## Resources

- **Apache FOP**: https://xmlgraphics.apache.org/fop/
- **FOP Test Suite**: https://github.com/apache/xmlgraphics-fop/tree/main/fop/test
- **W3C XSL-FO 1.1**: https://www.w3.org/Style/XSL/TestSuite1.1/
- **XSL-FO 1.1 Specification**: https://www.w3.org/TR/xsl11/
- **Folly Validation Plan**: `docs/validation/xslfo-coverage-validation-plan.md`

## Next Steps

1. **Manual W3C download**: Visit W3C test suite page in browser and download the ZIP file
2. **Create test runner**: Build a tool to process FOP test format
3. **Run test suite**: Process all 50 downloaded tests
4. **Document results**: Create coverage matrix showing pass/fail
5. **Expand coverage**: Download remaining FOP tests (718 more)
6. **Comparative analysis**: Run same tests through Apache FOP and compare

---

**Last Updated**: 2025-11-20
**Test Files Downloaded**: 50 (Apache FOP)
**Total Available**: 768+ (FOP), 100+ (W3C est.)
**Next Action**: Create test runner for automated processing
