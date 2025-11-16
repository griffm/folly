# Font Test Resources

This directory contains fonts used for testing OpenType features and CFF font parsing in Folly.

## Current Status

⏭️ **OpenType and CFF test fonts are not included in the repository** to keep the repository size minimal.

The following test files are currently **skipped** pending font resource acquisition:
- `OpenTypeFeatureTests.cs` (10 tests) - Requires OpenType font with ligatures
- `CffParserTests.cs` (4 tests) - Requires CFF-based .otf font

## Required Fonts (Not Included)

To run the OpenType and CFF tests, you need to add the following fonts to this directory:

### 1. OpenType Font with Ligatures

**Recommended:** Libertinus Serif Regular
- **Features needed:** liga (fi, fl, ffi, ffl), kern
- **License:** SIL Open Font License 1.1
- **Download:** https://github.com/alerque/libertinus/releases
- **File name:** `LibertinusSerif-Regular.otf`
- **Size:** ~200 KB

**Alternative:** EB Garamond
- **Download:** https://fonts.google.com/specimen/EB+Garamond
- **File name:** `EBGaramond-Regular.otf`

### 2. CFF-Based OpenType Font

**Recommended:** Source Serif Pro
- **License:** SIL Open Font License 1.1
- **Download:** https://github.com/adobe-fonts/source-serif-pro/releases
- **File name:** `SourceSerifPro-Regular.otf`
- **Size:** ~200 KB

**Alternative:** Any .otf font with CFF outlines (not TrueType)

## How to Add Fonts

1. Download the fonts from the links above
2. Place them in this directory (`tests/Folly.FontTests/TestResources/`)
3. Update the test files to reference the correct font file names
4. Remove the `[Skip]` attributes from the tests in:
   - `OpenTypeFeatureTests.cs`
   - `CffParserTests.cs`

## License Compliance

**Important:** Only use fonts with licenses that permit redistribution and modification:
- ✅ SIL Open Font License (OFL) 1.1
- ✅ Apache License 2.0
- ✅ Public Domain / CC0
- ❌ Proprietary/commercial fonts

Always include the font's LICENSE file when adding fonts to this directory.

## Existing Fonts

The `tests/Folly.FontTests/TestFonts/` directory already contains:
- `LiberationSans-Regular.ttf` - Used for basic font tests
- `Roboto-Regular.ttf` - Used for font fallback tests

These fonts **do not** have OpenType ligatures and cannot be used for the OpenType feature tests.

## Why Fonts Are Not Included

Font files are relatively large (200-500 KB each) and including them in the repository would:
1. Increase repository size unnecessarily
2. Require tracking binary files in Git
3. Add maintenance burden for font updates

Instead, developers who want to run these specific tests can download the fonts themselves.

## Alternative: Use Existing System Fonts

If you have fonts installed on your system with ligatures (e.g., from Microsoft Office, Adobe Creative Suite, or Linux font packages), you may be able to use those for testing by updating the font paths in the test files.

Common system fonts with ligatures:
- **macOS:** SF Pro, New York
- **Windows:** Cambria, Calibri (some versions)
- **Linux:** DejaVu Serif, Liberation Serif (some versions)

---

**For questions about font testing, see the [TEST-AUDIT-V1.md](../../TEST-AUDIT-V1.md) document.**
