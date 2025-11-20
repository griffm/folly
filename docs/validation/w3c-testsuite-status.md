# W3C XSL-FO Test Suite - Status and Access

## Executive Summary

**Status**: W3C XSL-FO 1.1 Test Suite identified but not automatically downloadable

**Alternative**: Apache FOP test suite successfully downloaded (50 representative tests from 768 total)

**Recommendation**: Use Apache FOP tests as primary validation corpus, supplement with manual W3C test suite download if needed

## W3C Test Suite Information

### Official Location

- **Homepage**: https://www.w3.org/Style/XSL/TestSuite1.1/
- **Test ToC**: https://www.w3.org/Style/XSL/TestSuite1.1/testsuite-toc.htm
- **Testing Guide**: https://www.w3.org/Style/XSL/TestSuite1.1/w3cxsl-fo-1_1-test.html

### Access Challenges

**Issue**: Direct download attempts return 403 Forbidden errors

```bash
curl https://www.w3.org/Style/XSL/TestSuite1.1/xsl11-testsuite.zip
# HTTP/2 403
```

**Possible Reasons**:
1. W3C may require browser-based access with cookies/session
2. May need to accept terms of service
3. May require W3C member access
4. Bot protection may be blocking automated downloads
5. File may have been moved or archived

### How to Access W3C Test Suite

**Option 1: Manual Browser Download** (Recommended)
1. Visit https://www.w3.org/Style/XSL/TestSuite1.1/ in a web browser
2. Look for download link or ZIP file
3. Download manually
4. Extract to `tests/w3c-testsuite/w3c-official/`

**Option 2: Contact W3C**
- Email: www-archive@w3.org or xsl-fo-comments@w3.org
- Request access to test suite
- Inquire about access procedures

**Option 3: Academic/Institutional Access**
- Check if your institution has W3C member access
- Some universities and research institutions have privileged access

**Option 4: Alternative Archives**
- Check Internet Archive (archive.org) for cached versions
- Search for mirrors on GitHub or other repositories
- Look for third-party distributions (with proper licensing)

## Apache FOP Test Suite (Successfully Downloaded)

### Overview

**Source**: https://github.com/apache/xmlgraphics-fop/tree/main/fop/test/layoutengine/standard-testcases

**Status**: ✅ Successfully downloaded 50 representative test cases

**Total Available**: 768 test cases in FOP repository

### What We Downloaded

**Location**: `tests/w3c-testsuite/apache-fop/`

**Test Categories** (50 files):
- Block-level elements (8 tests)
- Inline elements (4 tests)
- Tables (6 tests)
- Lists (3 tests)
- Markers (3 tests)
- Page breaking (5 tests)
- Keep/break constraints (5 tests)
- Footnotes (2 tests)
- Images (2 tests)
- Links (2 tests)
- Leaders (2 tests)
- Writing modes (2 tests)

**Example Files** (3 files):
- `simple.fo` - Basic XSL-FO structure
- `normal.fo` - Standard document example
- `extensive.fo` - Comprehensive feature demonstration

### Apache FOP Test Format

FOP tests use a custom XML format:

```xml
<testcase>
  <info>
    <p>Test description</p>
  </info>
  <fo>
    <!-- XSL-FO content -->
    <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
      ...
    </fo:root>
  </fo>
  <checks>
    <!-- XPath assertions for validation -->
    <eval expected="value" xpath="..."/>
  </checks>
</testcase>
```

**Advantages**:
- ✅ Freely available on GitHub
- ✅ Well-documented format
- ✅ Comprehensive coverage (768 tests)
- ✅ Apache 2.0 license (permissive)
- ✅ Actively maintained
- ✅ Includes validation checks (XPath assertions)

**To Use**:
1. Extract `<fo>` content from test files
2. Process through Folly
3. Validate output (manually or via XPath checks)

## NIST Test Suite

### Information

**Reference**: https://www.nist.gov/publications/conformance-test-suite-xsl-fo

**Status**: ❌ Also returns 403 Forbidden on automated access

**Description**: NIST developed a conformance test suite for XSL-FO validation

**Access**:
- May require institutional access
- Contact NIST directly for availability
- Check if available through academic channels

## Recommendation: Use Apache FOP Tests

### Rationale

1. **Accessibility**: FOP tests are freely available on GitHub
2. **Comprehensive**: 768 tests covering all major XSL-FO features
3. **Authoritative**: From the most widely-used open-source XSL-FO processor
4. **Real-world**: Tests based on actual implementation experience
5. **Maintained**: Actively updated with bug fixes and new features
6. **Documented**: Clear format and validation approach

### Why FOP Tests Are Sufficient

**Apache FOP is the de facto standard**:
- Most widely deployed XSL-FO processor
- Used in DocBook, DITA, and countless commercial applications
- 20+ years of development and refinement
- If Folly matches FOP behavior, it's compatible with the ecosystem

**FOP test coverage**:
- All XSL-FO 1.1 elements
- Edge cases and corner cases
- Real-world scenarios
- Performance and stress tests

**Validation strategy**:
- Pass rate against FOP tests = market compatibility
- W3C tests would add specification conformance validation
- But FOP tests provide practical validation

## Implementation Plan

### Phase 1: Current FOP Sample (Complete)
- [x] Downloaded 50 representative FOP tests
- [x] Downloaded 3 example FO files
- [x] Documented test format
- [ ] Create test runner to extract and process tests

### Phase 2: Expand FOP Coverage
- [ ] Download all 768 FOP test cases
- [ ] Categorize by feature area
- [ ] Create automated test harness
- [ ] Run full suite through Folly
- [ ] Document pass/fail rate and incompatibilities

### Phase 3: W3C Test Suite (Optional)
- [ ] Attempt manual download from W3C
- [ ] If successful, integrate W3C tests
- [ ] Compare W3C vs FOP test coverage
- [ ] Document specification conformance level

### Phase 4: Real-World Corpus
- [ ] Collect 50+ production XSL-FO documents
- [ ] Process through Folly
- [ ] Measure success rate (target: 95%+)
- [ ] Document compatibility

## Downloads Available

### Apache FOP Tests (768 total, 50 downloaded)

To download more FOP tests:

```bash
# Clone FOP repository
git clone https://github.com/apache/xmlgraphics-fop.git

# Find all test files
find xmlgraphics-fop/fop/test/layoutengine/standard-testcases -name "*.xml"

# Copy to Folly test suite
cp xmlgraphics-fop/fop/test/layoutengine/standard-testcases/*.xml \
   tests/w3c-testsuite/apache-fop/
```

Or use the provided download script:
```bash
./tests/w3c-testsuite/download-fop-tests.sh
```

### W3C Test Suite (Manual Download Required)

If you can access the W3C test suite:

1. Visit https://www.w3.org/Style/XSL/TestSuite1.1/ in browser
2. Download the test suite ZIP file
3. Extract to `tests/w3c-testsuite/w3c-official/`
4. Update this document with details about test format and contents

## Test Suite Comparison

| Feature | Apache FOP | W3C Official | NIST |
|---------|-----------|--------------|------|
| **Accessibility** | ✅ GitHub | ⚠️ Manual DL | ❌ 403 |
| **Test Count** | 768 | ~100 (est.) | Unknown |
| **Format** | XML + checks | Unknown | Unknown |
| **License** | Apache 2.0 | W3C terms | Public domain? |
| **Maintained** | ✅ Active | ⚠️ Static | ❌ Archived |
| **Coverage** | Comprehensive | Specification | Conformance |
| **Validation** | XPath checks | Unknown | Unknown |
| **Documentation** | Excellent | Good | Limited |

**Verdict**: Apache FOP tests are the best available option for practical validation.

## Next Steps

1. **Create FOP test runner**
   - Extract `<fo>` content from XML
   - Process through Folly
   - Compare output with `<checks>` assertions
   - Generate pass/fail report

2. **Run current 50 tests**
   - Process all downloaded FOP tests
   - Document initial pass rate
   - Investigate failures
   - Fix or document incompatibilities

3. **Expand to full FOP suite**
   - Download remaining 718 FOP tests
   - Run comprehensive suite
   - Target: 90%+ pass rate
   - Document coverage matrix

4. **Attempt W3C download** (optional)
   - Try manual browser download
   - If successful, integrate W3C tests
   - Compare with FOP results
   - Document specification conformance

5. **Build real-world corpus**
   - Collect production XSL-FO documents
   - Add DocBook/DITA examples
   - Process through Folly
   - Measure practical compatibility

## Resources

### Apache FOP
- **Repository**: https://github.com/apache/xmlgraphics-fop
- **Test Suite**: https://github.com/apache/xmlgraphics-fop/tree/main/fop/test
- **Documentation**: https://xmlgraphics.apache.org/fop/dev/testing.html
- **License**: Apache License 2.0

### W3C
- **Test Suite**: https://www.w3.org/Style/XSL/TestSuite1.1/
- **Specification**: https://www.w3.org/TR/xsl11/
- **Working Group**: https://www.w3.org/Style/XSL/

### Tools
- **qpdf**: PDF validation
- **xmllint**: XML/XPath processing
- **pdftoppm**: PDF rendering for visual inspection

## Conclusion

While the official W3C XSL-FO test suite is not easily accessible via automated downloads, **Apache FOP's comprehensive test suite provides an excellent alternative** for validating Folly's XSL-FO implementation.

**Key Points**:
- ✅ 768 FOP tests available (50 downloaded so far)
- ✅ Comprehensive coverage of XSL-FO features
- ✅ Authoritative source (most widely-used processor)
- ✅ Freely available and well-documented
- ⚠️ W3C test suite requires manual download
- 📋 Next step: Create test runner and process FOP tests

**Recommendation**: Proceed with Apache FOP test suite as primary validation corpus. This provides practical, real-world validation that ensures Folly is compatible with the XSL-FO ecosystem.

---

**Last Updated**: 2025-11-20
**Status**: Apache FOP tests downloaded successfully
**Next Action**: Create test runner for automated FOP test processing
**Priority**: High (essential for validation plan Phase 2)
