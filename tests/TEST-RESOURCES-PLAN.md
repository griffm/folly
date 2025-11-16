# Test Resource Acquisition Plan

## Overview
Generate test resources (images and fonts) needed for v1.0 test suite (110 tests).

## Strategy
Since ImageMagick is not available, we'll create test images **programmatically using C#** with zero external dependencies. This approach:
- ✅ Works in any environment
- ✅ Reproducible and version-controlled
- ✅ No external dependencies
- ✅ Easy for other developers to regenerate

## Resources Needed

### Images (14 files, ~800 KB total)

**DPI Test Images (6 files):**
- `test-72dpi.jpg` - 100×100px JPEG at 72 DPI
- `test-96dpi.jpg` - 100×100px JPEG at 96 DPI
- `test-150dpi.jpg` - 100×100px JPEG at 150 DPI
- `test-300dpi.jpg` - 100×100px JPEG at 300 DPI
- `test-300dpi.png` - 100×100px PNG with pHYs chunk at 300 DPI
- `no-dpi-metadata.jpg` - 100×100px JPEG without DPI metadata

**CMYK Test Images (4 files):**
- `test-cmyk.jpg` - 200×200px CMYK JPEG (Adobe CMYK colorspace)
- `jpeg-with-icc.jpg` - 200×200px RGB JPEG with embedded sRGB ICC profile
- `png-with-iccp.png` - 200×200px PNG with iCCP chunk
- `test-rgb.jpg` - 200×200px RGB JPEG without ICC profile (control)

### Fonts (2 files, ~400 KB total)

**OpenType Font with Ligatures:**
- Font: **Libertinus Serif Regular**
- Source: Google Fonts / GitHub
- License: SIL Open Font License 1.1
- Features: liga (fi, fl, ffi, ffl), kern
- URL: https://github.com/alerque/libertinus/releases

**CFF-based OpenType Font:**
- Font: **Source Serif Pro** or **EB Garamond**
- Source: Adobe Fonts / Google Fonts
- License: SIL Open Font License 1.1
- Format: .otf with CFF outlines
- URL: https://github.com/adobe-fonts/source-serif-pro

## Implementation Plan

### Phase 1: Create Test Image Generator (C# Console App)

Create `tests/TestResourceGenerator/` project that:
1. Generates simple test images with specified DPI
2. Embeds JFIF APP0 markers with DPI metadata
3. Generates PNG with pHYs chunks
4. Creates CMYK JPEGs with Adobe markers
5. Embeds ICC profiles (sRGB)

### Phase 2: Download Fonts

Use curl/wget to download open-source fonts:
1. Libertinus Serif Regular.otf
2. Source Serif Pro or EB Garamond .otf

### Phase 3: Documentation

Create `tests/Folly.UnitTests/TestResources/Images/LICENSES.txt`:
- Document image generation method (programmatic, CC0)
- List font licenses (SIL OFL 1.1)
- Provide source URLs

## Test Image Generation Approach

We'll create minimal valid images in pure C#:

**JPEG with DPI:**
```
[JPEG SOI]
[JFIF APP0 with DPI metadata]
[JPEG data]
[JPEG EOI]
```

**PNG with DPI:**
```
PNG signature
IHDR chunk (width, height, bit depth)
pHYs chunk (pixels per meter → DPI)
IDAT chunk (compressed image data)
IEND chunk
```

**CMYK JPEG:**
```
[JPEG SOI]
[Adobe APP14 marker with CMYK flag]
[SOF0 with 4 components]
[JPEG data]
[JPEG EOI]
```

## Advantages of This Approach

1. **Zero Dependencies** - Pure C# using System.IO
2. **Reproducible** - Any developer can regenerate
3. **Version Controlled** - Generator code in repository
4. **Fast** - Generates all images in <1 second
5. **Minimal File Sizes** - Only what's needed for tests

## Timeline

- **Step 1:** Create TestResourceGenerator project (30 min)
- **Step 2:** Implement JPEG DPI generator (20 min)
- **Step 3:** Implement PNG DPI generator (20 min)
- **Step 4:** Implement CMYK generators (30 min)
- **Step 5:** Download fonts (10 min)
- **Step 6:** Create LICENSES.txt (10 min)
- **Step 7:** Verify tests work (20 min)

**Total:** ~2.5 hours

## Next Steps

1. Create TestResourceGenerator console project
2. Implement image generators
3. Run generator to create test images
4. Download fonts
5. Document licenses
6. Update test files to remove [Skip] attributes
7. Verify tests pass

---

Ready to proceed with implementation!
