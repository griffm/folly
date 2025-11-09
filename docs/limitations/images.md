# Image Support Limitations

## Overview

Folly supports basic image embedding for JPEG (passthrough) and PNG (with decoding). However, many modern image formats, advanced features, and transformations are not yet implemented.

## Current Implementation

**Location**: `src/Folly.Core/Layout/LayoutEngine.cs:1168-1370`

**Supported Formats**:
- **JPEG**: Passthrough (binary data embedded directly)
- **PNG**: Basic decoding with dimension detection

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

### 3. No Image Transformations

**Severity**: Medium
**XSL-FO Properties**: Various image manipulation properties

**Not Supported**:
- **Rotation**: Cannot rotate images
- **Flipping**: No horizontal/vertical flip
- **Cropping**: Cannot crop to viewport
- **Filters**: No brightness, contrast, blur, etc.
- **Masking**: No alpha masks or clipping paths
- **Color adjustments**: No grayscale, sepia, etc.

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

### 5. Basic Dimension Detection

**Severity**: Low to Medium

**Current Implementation** (`LayoutEngine.cs:1269-1317`):
- Manual byte-level parsing of JPEG markers
- Manual PNG IHDR chunk parsing
- May fail on malformed images
- No DPI/resolution detection

**Limitations**:
- Assumes 72 DPI for all images
- No scaling based on actual image resolution
- Manual parsing brittle for edge cases

**Example Issue**:
Image is 300 DPI (high-res print):
- Physical size: 2" × 3"
- Pixel size: 600px × 900px
- Current: Renders at (600/72)" × (900/72)" = 8.3" × 12.5" (wrong!)
- Expected: Renders at 2" × 3" (correct)

**Solution**: Parse JFIF resolution or PNG pHYs chunk

### 6. No Image Compression Options

**Severity**: Low
**PDF Capabilities**: JPEG, Flate, JPEG2000, JBIG2

**Current Behavior**:
- JPEG: Passthrough (good - preserves compression)
- PNG: Decompresses, then re-compresses with Flate

**Not Supported**:
- JPEG2000 compression (better than JPEG)
- JBIG2 for monochrome images (excellent for text)
- Custom quality settings
- Lossless vs lossy choice

**Impact**: Larger PDF file sizes for some image types

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

### 8. No Image Optimization

**Severity**: Low
**Optimization Techniques**:

**Not Implemented**:
- Downsampling high-res images to screen resolution
- Converting palette-based PNG to indexed color in PDF
- Detecting and reusing duplicate images
- Progressive JPEG to standard JPEG
- Stripping metadata (EXIF, XMP) to reduce size

**Impact**: Larger PDF files than necessary

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

## Proposed Solutions

### Phase 1: Format Support (High Priority)
1. **Add TIFF support** - Important for scanning workflows
2. **Add WebP support** - Modern efficient format
3. **Add GIF support** - Common web format

**Libraries**:
- **SkiaSharp**: Supports all major formats
- **ImageSharp**: Pure .NET, many formats
- **System.Drawing** (Windows only): Legacy option

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
2. DPI/resolution detection
3. Color space management
4. Automatic downsampling

**Libraries**:
- **ImageSharp**: Rich image processing
- **MetadataExtractor**: EXIF parsing

### Phase 4: Optimization (Low Priority)
1. Image deduplication
2. Intelligent downsampling
3. Metadata stripping
4. Format conversion (if beneficial)

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
