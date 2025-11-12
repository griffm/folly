# Image Support Limitations

## Overview

Folly provides comprehensive support for JPEG and PNG images, including advanced PNG features like alpha channels, all color types, and all bit depths. However, many modern image formats (WebP, SVG, TIFF) and advanced transformations are not yet implemented.

## Current Implementation

**Location**:
- Layout: `src/Folly.Core/Layout/LayoutEngine.cs:1168-1370`
- PNG Decoder: `src/Folly.Pdf/PdfWriter.cs:234-610`

### JPEG Support

**Format**: Passthrough (binary data embedded directly into PDF)
- **Advantages**: Zero processing overhead, preserves original quality and compression
- **Color spaces**: RGB, CMYK, Grayscale (passed through)
- **Bit depths**: 8-bit typically
- **Metadata**: EXIF data preserved but not interpreted

### PNG Support (Comprehensive)

**Color Types** (All Supported):
- ✅ **Grayscale** (type 0): 1, 2, 4, 8, 16-bit
- ✅ **RGB** (type 2): 8, 16-bit
- ✅ **Indexed/Palette** (type 3): 1, 2, 4, 8-bit
- ✅ **Grayscale + Alpha** (type 4): 8, 16-bit with SMask
- ✅ **RGBA** (type 6): 8, 16-bit with SMask

**Features**:
- ✅ Alpha channel support (SMask for gradual transparency)
- ✅ tRNS transparency (simple color-key masking for RGB/Grayscale)
- ✅ All bit depths (1, 2, 4, 8, 16-bit)
- ✅ Non-interlaced images
- ✅ PNG predictor filters (Sub, Up, Average, Paeth)
- ✅ pHYs chunk parsing (DPI metadata)
- ✅ Comprehensive validation
- ❌ Interlaced (Adam7) - Rejected with clear error
- ❌ Indexed + tRNS - Not yet supported

**Processing**:
- Non-alpha images: Passthrough with PNG predictors (efficient)
- Alpha images: Decompress, separate color/alpha, recompress, create SMask

**Validation**:
- PNG signature verification
- Interlace detection (rejected)
- Bit depth validation per color type
- Missing palette detection
- Invalid color type detection

**Test Coverage**: 25 tests from PngSuite (100% pass rate)
- Location: `tests/Folly.UnitTests/PngSuiteTests.cs`
- Test data: `tests/Folly.UnitTests/TestData/PngSuite/` (176 images)

**Format Detection** (`LayoutEngine.cs:1249-1267`):
```csharp
// JPEG detection
if (data[0] == 0xFF && data[1] == 0xD8) { ... }

// PNG detection
if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) { ... }
```

**Dimension Handling**:
- Manual parsing of JPEG SOF markers
- Manual parsing of PNG IHDR chunk
- pHYs chunk parsing for DPI metadata
- Aspect ratio preservation
- Constraining to available width

## Limitations

### 1. Limited Format Support

**Severity**: Medium
**Supported**: JPEG, PNG only

**Not Supported**:
- **GIF** (.gif) - Legacy web format, includes animation
- **WebP** (.webp) - Modern efficient format from Google
- **TIFF** (.tif, .tiff) - Common in publishing/scanning
- **BMP** (.bmp) - Windows bitmap
- **SVG** (.svg) - Scalable vector graphics
- **PDF** (.pdf) - Embedded PDF pages
- **EPS** (.eps) - Encapsulated PostScript
- **JPEG 2000** (.jp2, .j2k) - Advanced JPEG variant

**Impact**:
- Cannot embed modern web images (WebP)
- Cannot use vector graphics (SVG)
- Cannot include high-quality scans (TIFF)
- Limited format flexibility

**Example That Doesn't Work**:
```xml
<fo:external-graphic src="logo.svg"/>
<fo:external-graphic src="photo.webp"/>
<fo:external-graphic src="scan.tiff"/>
```

**Workaround**: Convert images to JPEG or PNG before processing

### 2. No SVG Support

**Severity**: High for modern documents
**Format**: Scalable Vector Graphics

**Description**:
- No SVG parsing or rendering
- Cannot embed vector illustrations
- Lose scalability benefits

**Impact**:
- Logos and diagrams must be rasterized
- Loss of quality at high resolution/zoom
- Larger file sizes (raster vs vector)
- No text selection in embedded diagrams

**Use Cases Affected**:
- Company logos (should be vector)
- Technical diagrams
- Charts and graphs
- Icons

**Implementation Requirements**:
1. Parse SVG XML
2. Convert SVG paths to PDF graphics operators
3. Handle SVG text, transforms, styles
4. Support gradients, filters, clipping

**Complexity**: Very High

**Alternative**: Render SVG to raster at high DPI before embedding

### 3. Limited Image Transformations

**Severity**: Medium
**XSL-FO Properties**: Various image manipulation properties

**Not Supported**:
- **Rotation**: Cannot rotate images
- **Flipping**: No horizontal/vertical flip
- **Cropping**: Cannot crop to viewport
- **Filters**: No brightness, contrast, blur, etc.
- **Color adjustments**: No grayscale, sepia, etc.

**Supported**:
- ✅ **Alpha masks**: PNG images with alpha channels (SMask)
- ✅ **Transparency**: tRNS color-key masking (RGB/Grayscale)

**Example Not Supported**:
```xml
<fo:external-graphic src="image.jpg"
                     content-width="2in"
                     content-height="3in"
                     scaling="non-uniform"  <!-- Supported -->
                     rotation="90"/>         <!-- NOT supported -->
```

**Workaround**: Pre-process images externally

### 4. No EXIF Orientation Support

**Severity**: Medium
**EXIF Tag**: Orientation (0x0112)

**Description**:
- Many photos include EXIF orientation tag
- Indicates how camera was held (portrait/landscape/rotated)
- Not read or applied during rendering

**Impact**:
- Photos may appear sideways
- User must manually rotate before embedding
- Common issue with phone photos

**Example Problem**:
Camera EXIF says: "This image is rotated 90° clockwise"
Current behavior: Renders image in stored orientation (wrong)
Expected: Auto-rotate according to EXIF

**Solution**:
1. Parse EXIF data from JPEG
2. Read orientation tag
3. Apply rotation during embedding

**Complexity**: Low to Medium

### 5. Dimension and DPI Detection

**Severity**: Low

**Current Implementation**:
- Manual byte-level parsing of JPEG markers
- Manual PNG IHDR chunk parsing
- ✅ PNG pHYs chunk parsing (DPI metadata extracted)
- May fail on malformed images

**PNG Resolution**: ✅ Supported
- pHYs chunk parsed for pixels-per-meter
- Can be converted to DPI
- **Note**: Currently extracted but not yet applied to image scaling (TODO)

**JPEG Resolution**: ⚠️ Partial
- JFIF resolution not parsed
- Assumes 72 DPI for all JPEG images

**Example Issue** (JPEG only):
Image is 300 DPI (high-res print):
- Physical size: 2" × 3"
- Pixel size: 600px × 900px
- Current: Renders at (600/72)" × (900/72)" = 8.3" × 12.5" (wrong!)
- Expected: Renders at 2" × 3" (correct)

**Solution**: Parse JFIF resolution markers

### 6. Limited Compression Options

**Severity**: Low
**PDF Capabilities**: JPEG, Flate, JPEG2000, JBIG2

**Current Behavior**:
- **JPEG**: Passthrough (excellent - preserves original compression)
- **PNG without alpha**: Passthrough with PNG predictors (efficient)
- **PNG with alpha**: Decompress, separate channels, recompress with Flate

**Not Supported**:
- JPEG2000 compression (better than JPEG)
- JBIG2 for monochrome images (excellent for text)
- Custom quality settings
- Lossless vs lossy choice

**Impact**: Minimal - current compression is efficient for most use cases

### 7. No Color Space Management

**Severity**: Medium for professional printing
**Color Spaces**: RGB, CMYK, Grayscale, Lab, ICC profiles

**Description**:
- No ICC color profile support
- No CMYK handling
- All images assumed sRGB
- No color space conversion

**Impact**:
- Colors may not match original
- Professional printing workflows affected
- Cannot prepare PDF for specific output devices

**Example**:
```xml
<!-- Image has embedded CMYK profile for printing -->
<fo:external-graphic src="cmyk-image.jpg"/>
```
Current: Treats as RGB, colors may shift
Expected: Preserve CMYK or convert properly

### 8. Limited Image Optimization

**Severity**: Low
**Optimization Techniques**:

**Implemented**:
- ✅ Indexed PNG → PDF Indexed ColorSpace (preserves palette efficiency)

**Not Implemented**:
- Downsampling high-res images to screen resolution
- Detecting and reusing duplicate images
- Progressive JPEG to standard JPEG
- Stripping metadata (EXIF, XMP) to reduce size

**Impact**: Larger PDF files than necessary for some use cases

**Example**:
User embeds 10 MB, 6000×4000px photo for 2"×1.5" display area:
- Current: Embeds full 10 MB image
- Optimal: Downsample to 300 DPI → 900×675px, ~200 KB

### 9. No Animated Image Support

**Severity**: Very Low
**Formats**: Animated GIF, Animated PNG (APNG), Animated WebP

**Description**:
- PDF doesn't natively support animation
- Could extract first frame

**Impact**: Minimal - PDF is static medium

### 10. No Image Caching/Reuse

**Severity**: Low
**Description**: Same image used multiple times is embedded multiple times

**Example**:
```xml
<fo:block>
  <fo:external-graphic src="logo.png"/>  <!-- Embedded once -->
</fo:block>
<fo:block>
  <fo:external-graphic src="logo.png"/>  <!-- Embedded again! -->
</fo:block>
```

**Current**: Logo embedded twice (duplicate data)
**Optimal**: Embed once, reference twice (PDF XObject reuse)

**Impact**: Larger file sizes for documents with repeated images

**Solution**: Track embedded images by path/hash, reuse XObject references

## Security Considerations

**Current Security** (`LayoutEngine.cs:1604-1646`):

✅ **Implemented**:
- Path traversal prevention
- Image size limit (50 MB default)
- File existence validation
- Optional base path restriction

**Path Validation**:
```csharp
private bool ValidateImagePath(string imagePath)
{
    var fullPath = Path.GetFullPath(imagePath);

    // Check if absolute paths allowed
    if (Path.IsPathRooted(imagePath) && !_options.AllowAbsoluteImagePaths)
        return false;

    // Check if within allowed base path
    if (!string.IsNullOrWhiteSpace(_options.AllowedImageBasePath))
    {
        // Verify path starts with base path
    }
}
```

**Security Options**:
```csharp
public class LayoutOptions
{
    public long MaxImageSizeBytes = 50 * 1024 * 1024;  // 50 MB
    public string? AllowedImageBasePath;
    public bool AllowAbsoluteImagePaths = false;
}
```

## Recent Improvements (2025)

### PNG Support Enhancements ✅ Completed
1. ✅ **All color types**: Grayscale, RGB, Indexed, RGBA, Grayscale+Alpha
2. ✅ **All bit depths**: 1, 2, 4, 8, 16-bit support
3. ✅ **Alpha channels**: SMask implementation for gradual transparency
4. ✅ **tRNS transparency**: Color-key masking for RGB/Grayscale
5. ✅ **Validation**: Comprehensive error detection and clear messages
6. ✅ **Test coverage**: 25 PngSuite tests (100% pass rate)
7. ✅ **Performance**: Optimized bulk copy for IDAT data
8. ✅ **pHYs chunk**: DPI metadata extraction

**Impact**: PNG support is now production-ready and comprehensive

## Proposed Solutions

### Phase 1: Format Support (High Priority)
1. **Add TIFF support** - Important for scanning workflows
2. **Add WebP support** - Modern efficient format
3. **Add GIF support** - Common web format

**Libraries**:
- **SkiaSharp**: Supports all major formats
- **ImageSharp**: Pure .NET, many formats
- **System.Drawing** (Windows only): Legacy option

**Note**: Consider zero-dependency requirement when choosing approach

### Phase 2: SVG Support (High Priority)
1. Parse SVG using XML reader
2. Convert to PDF graphics operators
3. Support basic shapes, paths, text
4. Advanced: gradients, filters, patterns

**Libraries**:
- **Svg.Skia**: SVG to Skia rendering
- **SharpVectors**: Pure .NET SVG

### Phase 3: Image Processing (Medium Priority)
1. EXIF orientation
2. ✅ ~~DPI/resolution detection~~ (PNG pHYs done, JPEG JFIF pending)
3. Color space management
4. Automatic downsampling
5. Apply pHYs data to image scaling

**Libraries**:
- **ImageSharp**: Rich image processing
- **MetadataExtractor**: EXIF parsing

### Phase 4: Optimization (Low Priority)
1. Image deduplication
2. Intelligent downsampling
3. Metadata stripping
4. Format conversion (if beneficial)
5. ✅ ~~Indexed color support~~ (Already implemented)

## XSL-FO Specification Compliance

**Properties Implemented**:
- `src` (image path) - Yes
- `content-width` / `content-height` - Yes
- `scaling` - Yes
- Aspect ratio preservation - Yes

**Properties Not Fully Supported**:
- `scaling="non-uniform"` - Implemented
- `scaling="uniform"` - Implemented
- `content-type` - Not validated
- `scaling-method` - Not implemented

**Compliance Level**: ~70% for image properties

## Performance Considerations

**Current**:
- Images loaded synchronously during layout
- Entire image file read into memory
- PNG decompression can be slow for large images

**Optimizations**:
1. **Lazy loading**: Load images only when needed
2. **Streaming**: Don't load entire file to memory
3. **Parallel loading**: Load images in background
4. **Thumbnail generation**: For very large images

## References

1. **JPEG Specification**:
   - ITU-T T.81 / ISO/IEC 10918-1
   - https://www.w3.org/Graphics/JPEG/

2. **PNG Specification**:
   - W3C PNG Specification
   - https://www.w3.org/TR/PNG/

3. **SVG Specification**:
   - W3C SVG 1.1
   - https://www.w3.org/TR/SVG11/

4. **PDF Image XObjects**:
   - PDF Reference 1.7, Section 8.9: Image Dictionaries
   - https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf

5. **EXIF Specification**:
   - JEITA CP-3451 (EXIF 2.3)
   - http://www.cipa.jp/std/documents/e/DC-008-2012_E.pdf

6. **ImageSharp Library**:
   - SixLabors.ImageSharp
   - https://github.com/SixLabors/ImageSharp

## See Also

- [rendering.md](rendering.md) - How images are rendered in PDF
- [security-validation.md](security-validation.md) - Image path validation
- [performance.md](performance.md) - Image loading performance
