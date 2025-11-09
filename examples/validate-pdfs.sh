#!/bin/bash

# Validate Folly-generated PDFs with qpdf
# Usage: ./validate-pdfs.sh

set -e

echo "Folly PDF Validation Script"
echo "============================"
echo ""

# Check if qpdf is installed
if ! command -v qpdf &> /dev/null; then
    echo "Error: qpdf is not installed"
    echo "Install with: sudo apt-get install qpdf"
    exit 1
fi

# Output directory
OUTPUT_DIR="output"

if [ ! -d "$OUTPUT_DIR" ]; then
    echo "Error: Output directory not found: $OUTPUT_DIR"
    echo "Run 'dotnet run --project Folly.Examples' first"
    exit 1
fi

# Count PDFs
PDF_COUNT=$(find "$OUTPUT_DIR" -name "*.pdf" | wc -l)

if [ "$PDF_COUNT" -eq 0 ]; then
    echo "Error: No PDF files found in $OUTPUT_DIR"
    exit 1
fi

echo "Found $PDF_COUNT PDF file(s) to validate"
echo ""

# Validate each PDF
PASS_COUNT=0
FAIL_COUNT=0

for pdf in "$OUTPUT_DIR"/*.pdf; do
    filename=$(basename "$pdf")
    printf "Validating %-30s ... " "$filename"

    if qpdf --check "$pdf" &> /dev/null; then
        echo "✓ PASS"
        ((PASS_COUNT++))
    else
        echo "✗ FAIL"
        ((FAIL_COUNT++))
        echo "  Running detailed check:"
        qpdf --check "$pdf" || true
    fi
done

echo ""
echo "=============================="
echo "Validation Summary"
echo "=============================="
echo "Total:  $PDF_COUNT"
echo "Passed: $PASS_COUNT"
echo "Failed: $FAIL_COUNT"
echo ""

if [ "$FAIL_COUNT" -eq 0 ]; then
    echo "✓ All PDFs are valid!"
    exit 0
else
    echo "✗ Some PDFs failed validation"
    exit 1
fi
