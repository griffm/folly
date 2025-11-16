# PDF Generation Architecture

Folly generates PDF 1.7 files with comprehensive feature support and zero external dependencies.

## Overview

The PDF generation subsystem converts the area tree into valid PDF 1.7 documents with:

- Font embedding and subsetting
- Image embedding (JPEG, PNG, BMP, GIF, TIFF)
- SVG rendering to native PDF operations
- Stream compression (Flate/zlib)
- Metadata and document properties
- Hyperlinks and bookmarks
- PDF structure and cross-reference tables

## PDF Structure

```
PDF File
├─ Header (%PDF-1.7)
├─ Objects
│  ├─ Catalog (Document root)
│  ├─ Pages (Page tree)
│  ├─ Page Objects
│  │  ├─ Content Streams
│  │  └─ Resources
│  │     ├─ Fonts
│  │     ├─ Images (XObjects)
│  │     ├─ Shadings (Gradients)
│  │     └─ Patterns
│  ├─ Font Objects
│  ├─ Image Objects
│  └─ Metadata
├─ Cross-Reference Table
└─ Trailer
```

## Rendering Pipeline

```
Area Tree
  ↓
PdfRenderer
  ├─ Generate content streams
  ├─ Embed fonts (with subsetting)
  ├─ Embed images
  └─ Convert SVG
  ↓
PdfWriter
  ├─ Serialize objects
  ├─ Compress streams
  ├─ Build xref table
  └─ Write trailer
  ↓
PDF 1.7 Document
```

## Key Components

### Font Embedding

Folly supports Type 1 (standard) and TrueType fonts:

- **Type 1**: Standard 14 PDF fonts with AFM metrics
- **TrueType**: Custom fonts with full parsing and subsetting
- **Subsetting**: Only used glyphs embedded (60-90% size reduction)
- **ToUnicode CMaps**: Enable text extraction from PDFs

See [Font System Architecture](font-system.md) for details.

### Image Embedding

Zero-dependency image decoders for all major formats:

- **JPEG**: DCTDecode passthrough (no recompression)
- **PNG**: Flate decoding with PNG predictors, alpha channel (SMask)
- **BMP**: 24/32-bit RGB with alpha support
- **GIF**: Custom LZW decoder, transparency support
- **TIFF**: Baseline uncompressed RGB

Images are embedded as PDF XObjects for efficient reuse.

### SVG Rendering

SVG graphics are converted to native PDF operations:

- Shapes → PDF path operators (m, l, c, S, f)
- Gradients → PDF shadings (Type 2/3)
- Patterns → PDF tiling patterns (Type 1)
- Text → PDF text operators (BT, Tj, ET)

See [SVG Support](svg-support.md) for complete details.

### Stream Compression

All content streams and images use Flate (zlib) compression:

- **Text content**: 40-60% size reduction
- **Images**: Varies by format (JPEG already compressed)
- **Fonts**: 50-70% size reduction

## PDF Operators Used

### Graphics State
- `q/Q` - Save/restore graphics state
- `cm` - Concatenate transformation matrix
- `w` - Set line width
- `J` - Set line cap style
- `j` - Set line join style
- `d` - Set dash pattern
- `gs` - Set graphics state (ExtGState)

### Path Construction
- `m` - Move to
- `l` - Line to
- `c` - Cubic Bézier curve
- `v` - Bézier curve (current point as first control)
- `y` - Bézier curve (current point as second control)
- `h` - Close path
- `re` - Rectangle

### Path Painting
- `S` - Stroke
- `s` - Close and stroke
- `f` - Fill (nonzero winding)
- `f*` - Fill (even-odd)
- `B` - Fill and stroke
- `W` - Clip (nonzero winding)
- `W*` - Clip (even-odd)
- `n` - End path (no-op)

### Color
- `rg/RG` - Set RGB fill/stroke color
- `g/G` - Set gray fill/stroke color
- `sc/SC` - Set color (general)
- `scn/SCN` - Set color (with pattern/shading)

### Text
- `BT/ET` - Begin/end text object
- `Tf` - Set font and size
- `Td` - Move text position
- `Tj` - Show text string
- `TJ` - Show text with positioning
- `Tc` - Set character spacing
- `Tz` - Set horizontal scaling

### Shadings
- `sh` - Paint shading (gradients)

### XObjects
- `Do` - Paint XObject (images, patterns)

## PDF Structure Details

### Object Model

PDF uses indirect objects referenced by ID:

```
1 0 obj
<<
  /Type /Page
  /Parent 2 0 R
  /Resources 3 0 R
  /Contents 4 0 R
>>
endobj
```

Folly manages object IDs automatically and builds the cross-reference table.

### Cross-Reference Table

Maps object IDs to byte offsets:

```
xref
0 5
0000000000 65535 f
0000000015 00000 n
0000000100 00000 n
0000000250 00000 n
0000000500 00000 n
```

### Trailer

Points to root catalog and info dictionary:

```
trailer
<<
  /Size 5
  /Root 1 0 R
  /Info 2 0 R
>>
startxref
1234
%%EOF
```

## Performance Optimizations

- **Binary writing**: Direct byte-level serialization
- **Stream buffering**: Minimize I/O operations
- **Font caching**: Reuse font objects across documents
- **Lazy resource creation**: Resources created only when used

See [Performance Guide](../guides/performance.md) for benchmarks.

## PDF Compliance

- **Version**: PDF 1.7 (ISO 32000-1:2008)
- **Validation**: Verified with qpdf
- **Structure**: Conforms to PDF specification
- **Fonts**: Proper encoding and metrics
- **Images**: Correct color spaces and compression

## Limitations

Current limitations:

- No PDF 2.0 features
- No PDF/A compliance
- No encryption/digital signatures
- No interactive forms (AcroForms)
- No JavaScript
- Limited CMYK support

See [Limitations](../guides/limitations.md) for complete list.

## Further Reading

- [Font System](font-system.md) - Font parsing and embedding
- [SVG Support](svg-support.md) - SVG to PDF conversion
- [PDF 1.7 Reference](https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf)
