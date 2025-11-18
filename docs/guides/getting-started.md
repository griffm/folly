# Getting Started with Folly

This guide will help you get started with Folly, a zero-dependency XSL-FO to PDF rendering library for .NET 8.

## Installation

### Prerequisites

- .NET 8.0 SDK or later
- Any OS supported by .NET 8 (Windows, macOS, Linux)

### NuGet Packages

```bash
# Core library (required)
dotnet add package Folly.Core

# Optional: Fluent API for programmatic document construction
dotnet add package Folly.Fluent
```

### Building from Source

```bash
git clone https://github.com/folly/folly.git
cd folly
dotnet restore
dotnet build
```

## Quick Start

### Load and Render XSL-FO

The simplest way to convert an XSL-FO document to PDF:

```csharp
using Folly;
using Folly.Pdf;

// Load FO document from file
var fo = FoDocument.Load("input.fo");

// Render to PDF
using var pdf = File.Create("output.pdf");
fo.SavePdf(pdf);
```

### From XML String

```csharp
var foXml = @"<?xml version='1.0' encoding='UTF-8'?>
<fo:root xmlns:fo='http://www.w3.org/1999/XSL/Format'>
  <fo:layout-master-set>
    <fo:simple-page-master master-name='A4'
                           page-width='210mm' page-height='297mm'>
      <fo:region-body margin='1in'/>
    </fo:simple-page-master>
  </fo:layout-master-set>
  <fo:page-sequence master-reference='A4'>
    <fo:flow flow-name='xsl-region-body'>
      <fo:block>Hello Folly!</fo:block>
    </fo:flow>
  </fo:page-sequence>
</fo:root>";

var fo = FoDocument.Parse(foXml);
using var pdf = File.Create("hello.pdf");
fo.SavePdf(pdf);
```

## Using the Fluent API

The Fluent API provides a programmatic way to build FO documents:

```csharp
using Folly.Fluent;

// Create document using fluent API
Fo.Document(doc => doc
    .Metadata(meta => meta
        .Title("My First Document")
        .Author("Your Name"))
    .LayoutMasters(lm => lm
        .SimplePageMaster("A4", "210mm", "297mm", spm => spm
            .RegionBody(rb => rb.Margin("1in"))
            .RegionBefore("0.5in")
            .RegionAfter("0.5in")))
    .PageSequence("A4", ps => ps
        .Flow(flow => flow
            .Block("Welcome to Folly!")
            .Block(b => b
                .Text("This is a paragraph with ")
                .Inline(i => i.FontWeight("bold").Text("bold"))
                .Text(" text.")))))
.SavePdf("fluent-example.pdf");
```

## Basic Concepts

### FO Document Structure

An XSL-FO document has three main parts:

1. **Layout Master Set** - Defines page templates
2. **Page Sequences** - Content organized into pages
3. **Flow** - The actual content

```xml
<fo:root>
  <fo:layout-master-set>
    <!-- Page templates -->
  </fo:layout-master-set>

  <fo:page-sequence master-reference="...">
    <fo:flow flow-name="xsl-region-body">
      <!-- Content -->
    </fo:flow>
  </fo:page-sequence>
</fo:root>
```

### Page Masters

Define page dimensions and margins:

```xml
<fo:simple-page-master master-name="A4"
                       page-width="210mm"
                       page-height="297mm">
  <fo:region-body margin="1in"/>
  <fo:region-before extent="0.5in"/>
  <fo:region-after extent="0.5in"/>
</fo:simple-page-master>
```

### Blocks and Inlines

Content is composed of blocks (paragraphs) and inlines (styled text):

```xml
<fo:block font-size="14pt" space-after="12pt">
  This is a paragraph with
  <fo:inline font-weight="bold">bold text</fo:inline>.
</fo:block>
```

## Common Tasks

### Creating a Table

```xml
<fo:table width="100%">
  <fo:table-column column-width="33%"/>
  <fo:table-column column-width="33%"/>
  <fo:table-column column-width="34%"/>

  <fo:table-header>
    <fo:table-row font-weight="bold">
      <fo:table-cell border="1pt solid black" padding="2pt">
        <fo:block>Header 1</fo:block>
      </fo:table-cell>
      <fo:table-cell border="1pt solid black" padding="2pt">
        <fo:block>Header 2</fo:block>
      </fo:table-cell>
      <fo:table-cell border="1pt solid black" padding="2pt">
        <fo:block>Header 3</fo:block>
      </fo:table-cell>
    </fo:table-row>
  </fo:table-header>

  <fo:table-body>
    <fo:table-row>
      <fo:table-cell border="1pt solid black" padding="2pt">
        <fo:block>Cell 1</fo:block>
      </fo:table-cell>
      <fo:table-cell border="1pt solid black" padding="2pt">
        <fo:block>Cell 2</fo:block>
      </fo:table-cell>
      <fo:table-cell border="1pt solid black" padding="2pt">
        <fo:block>Cell 3</fo:block>
      </fo:table-cell>
    </fo:table-row>
  </fo:table-body>
</fo:table>
```

### Adding Images

```xml
<fo:block text-align="center">
  <fo:external-graphic src="url('image.png')"
                       content-width="200pt"/>
</fo:block>
```

Supported formats: JPEG, PNG, BMP, GIF, TIFF (zero dependencies).

### Headers and Footers

```xml
<fo:page-sequence master-reference="A4">
  <!-- Header -->
  <fo:static-content flow-name="xsl-region-before">
    <fo:block text-align="center" font-size="10pt">
      Document Header
    </fo:block>
  </fo:static-content>

  <!-- Footer with page numbers -->
  <fo:static-content flow-name="xsl-region-after">
    <fo:block text-align="center" font-size="10pt">
      Page <fo:page-number/>
    </fo:block>
  </fo:static-content>

  <!-- Main content -->
  <fo:flow flow-name="xsl-region-body">
    <fo:block>Content...</fo:block>
  </fo:flow>
</fo:page-sequence>
```

## Configuration Options

### Layout Options

```csharp
var options = new LayoutOptions
{
    // Line breaking algorithm
    LineBreaking = LineBreakingAlgorithm.Greedy,  // or Optimal (Knuth-Plass)

    // Hyphenation
    EnableHyphenation = false,
    HyphenationLanguage = "en-US",
    MinWordLength = 5,
    MinLeftChars = 2,
    MinRightChars = 3,
};

var fo = FoDocument.Load("input.fo");
fo.SavePdf(pdfStream, options);
```

### PDF Options

```csharp
var pdfOptions = new PdfOptions
{
    // Font subsetting
    SubsetFonts = true,

    // Stream compression
    CompressStreams = true,

    // Metadata
    Title = "My Document",
    Author = "Your Name",
    Subject = "Document Subject",
    Keywords = "keyword1, keyword2",
};

var fo = FoDocument.Load("input.fo");
fo.SavePdf(pdfStream, pdfOptions: pdfOptions);
```

## Running Examples

Folly includes extensive working examples demonstrating its capabilities:

```bash
cd examples
dotnet run --project Folly.Examples
```

Generated PDFs will be in `examples/output/`.

### Validating PDFs

Install qpdf and validate generated PDFs:

```bash
# Install qpdf
sudo apt-get install qpdf  # Linux
brew install qpdf          # macOS

# Validate PDFs
cd examples
./validate-pdfs.sh
```

## Next Steps

- **Explore Examples**: Review the extensive examples in `examples/Folly.Examples/`
- **Read the Architecture Guide**: Understand how Folly works in [Architecture Overview](../architecture/overview.md)
- **Review Performance**: Learn about performance in [Performance Guide](performance.md)
- **Check Limitations**: Review [Limitations](limitations.md) for current constraints
- **Contribute**: See the main [README](../../README.md) for contribution guidelines

## Getting Help

- **Documentation**: Browse the `docs/` folder
- **Examples**: Study the `examples/` folder
- **Issues**: Report issues on GitHub
- **CLAUDE.md**: See agent execution guidelines for development setup

## Common Pitfalls

### 1. Missing Namespace

Always include the XSL-FO namespace:

```xml
<fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
  <!-- ... -->
</fo:root>
```

### 2. Incorrect Units

Specify units explicitly:

```xml
<!-- Good -->
<fo:block font-size="12pt" space-after="6pt">

<!-- Bad (unitless values assume points but can be ambiguous) -->
<fo:block font-size="12" space-after="6">
```

### 3. Missing Page Master Reference

Always reference a defined page master:

```xml
<fo:layout-master-set>
  <fo:simple-page-master master-name="A4" ...>
    <!-- ... -->
  </fo:simple-page-master>
</fo:layout-master-set>

<fo:page-sequence master-reference="A4">
  <!-- ... -->
</fo:page-sequence>
```

### 4. Image Path Issues

Use absolute paths or `url()` syntax:

```xml
<!-- Good -->
<fo:external-graphic src="url('images/logo.png')"/>

<!-- Also good -->
<fo:external-graphic src="url('/absolute/path/to/image.png')"/>
```

## Best Practices

1. **Use semantic markup** - Leverage blocks, inlines, tables, and lists appropriately
2. **Reuse page masters** - Define once, reference multiple times
3. **Enable hyphenation** - For narrow columns and justified text
4. **Subset fonts** - Keep enabled (default) for smaller PDFs
5. **Validate PDFs** - Use qpdf to ensure correctness

## Troubleshooting

### Build Errors

Ensure you have .NET 8.0 SDK:

```bash
dotnet --version
# Should show 8.0.xxx
```

### PDF Validation Errors

```bash
qpdf --check output.pdf
```

Common issues:
- Missing fonts → Ensure font files are accessible
- Invalid images → Check image format and path
- Malformed FO → Validate XSL-FO XML structure

### Performance Issues

See [Performance Guide](performance.md) for optimization strategies.

## Resources

- [XSL-FO 1.1 Specification](https://www.w3.org/TR/xsl11/)
- [PDF 1.7 Reference](https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf)
- [Folly Documentation](../README.md)
