# Folly Examples

This directory contains runnable examples that demonstrate Folly's XSL-FO to PDF rendering capabilities.

## Running the Examples

Generate all example PDFs:

```bash
cd examples
dotnet run --project Folly.Examples
```

The examples will be generated in `examples/Folly.Examples/output/`:

- `01-hello-world.pdf` - Simple "Hello World" document
- `02-multiple-blocks.pdf` - Multiple blocks with different fonts
- `03-text-alignment.pdf` - Start, center, and end text alignment
- `04-borders-backgrounds.pdf` - Various border styles and background colors
- `05-multi-page.pdf` - Multi-page document with automatic pagination
- `06-invoice.pdf` - Sample invoice demonstrating a real-world use case

## Validating PDFs

Validate the generated PDFs using qpdf:

```bash
# Install qpdf if not already installed
sudo apt-get install qpdf

# Run validation script
./validate-pdfs.sh
```

The validation script checks each PDF for:
- Structural integrity
- Valid PDF syntax
- Cross-reference table correctness
- Proper encoding

## Example Features

### 01: Hello World
- Basic document structure
- Simple text rendering
- Multiple font families (Helvetica, Times)

### 02: Multiple Blocks
- Sequential block layout
- Font family variations (Helvetica, Times, Courier)
- Font size variations

### 03: Text Alignment
- Left/start alignment
- Center alignment
- Right/end alignment

### 04: Borders and Backgrounds
- Background colors (named and hex)
- Border styles (solid, dashed, dotted)
- Border colors
- Combining borders and backgrounds

### 05: Multi-Page Document
- Automatic page breaking
- Multi-chapter document
- Consistent styling across pages

### 06: Invoice
- Real-world document example
- Table-like layout using blocks
- Mixed styling and alignment
- Professional formatting

## Viewing PDFs

Open the generated PDFs with any PDF viewer:

```bash
# Linux
xdg-open Folly.Examples/output/01-hello-world.pdf

# macOS
open Folly.Examples/output/01-hello-world.pdf

# Windows
start Folly.Examples/output/01-hello-world.pdf
```
