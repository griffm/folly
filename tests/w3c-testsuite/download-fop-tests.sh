#!/bin/bash
# Download Apache FOP Test Suite
# This script downloads test files from Apache FOP's GitHub repository
# to create a comprehensive XSL-FO test corpus for Folly validation

set -e

REPO_BASE="https://raw.githubusercontent.com/apache/xmlgraphics-fop/main"
TEST_DIR="tests/w3c-testsuite/apache-fop"
EXAMPLES_DIR="tests/w3c-testsuite/apache-fop-examples"

echo "Creating test suite directories..."
mkdir -p "$TEST_DIR"
mkdir -p "$EXAMPLES_DIR"/{basic,tables,lists,graphics,markers,pagination,svg,keeps_and_breaks,footnotes}

echo "Downloading Apache FOP test metadata..."
curl -s "$REPO_BASE/fop/test/layoutengine/README" > "$TEST_DIR/README" || echo "README not found"

echo ""
echo "Apache FOP has 768 layout engine test cases."
echo "Downloading a representative sample covering major XSL-FO features..."
echo ""

# Function to download a test file
download_test() {
    local filename=$1
    local category=$2
    echo "  - $filename"
    curl -s -f "$REPO_BASE/fop/test/layoutengine/standard-testcases/$filename" \
        > "$TEST_DIR/$filename" 2>/dev/null || echo "    (failed to download)"
}

# Function to download example file
download_example() {
    local subdir=$1
    local filename=$2
    echo "  - $subdir/$filename"
    curl -s -f "$REPO_BASE/fop/examples/fo/$subdir/$filename" \
        > "$EXAMPLES_DIR/$subdir/$filename" 2>/dev/null || echo "    (failed to download)"
}

echo "Downloading block-level element tests..."
download_test "block_absolute-position.xml" "blocks"
download_test "block_background-image.xml" "blocks"
download_test "block_border_padding.xml" "blocks"
download_test "block_font-family.xml" "blocks"
download_test "block_line-height.xml" "blocks"
download_test "block_space-before_space-after_1.xml" "blocks"
download_test "block_space-before_space-after_2.xml" "blocks"
download_test "block_white-space_1.xml" "blocks"

echo "Downloading inline-level element tests..."
download_test "inline_block_nested_3.xml" "inlines"
download_test "inline-container_border_padding.xml" "inlines"
download_test "inline_letter-spacing.xml" "inlines"
download_test "inline_word-spacing.xml" "inlines"

echo "Downloading table tests..."
download_test "table_border-collapse_separate.xml" "tables"
download_test "table_border-width.xml" "tables"
download_test "table-cell_number-columns-spanned.xml" "tables"
download_test "table-cell_number-rows-spanned.xml" "tables"
download_test "table-header_marker_bug39443.xml" "tables"
download_test "table_width_100_large-content.xml" "tables"

echo "Downloading list tests..."
download_test "list-block_1.xml" "lists"
download_test "list-block_space-after.xml" "lists"
download_test "list-block_space-before.xml" "lists"

echo "Downloading marker tests..."
download_test "marker_1.xml" "markers"
download_test "marker_2.xml" "markers"
download_test "marker_bug36724.xml" "markers"

echo "Downloading page break tests..."
download_test "page-breaking_1.xml" "pagination"
download_test "page-breaking_2.xml" "pagination"
download_test "page-breaking_3.xml" "pagination"
download_test "page-number_1.xml" "pagination"
download_test "page-number-citation_1.xml" "pagination"

echo "Downloading keep/break tests..."
download_test "keep-together_1.xml" "keeps"
download_test "keep-with-next_1.xml" "keeps"
download_test "keep-with-previous_1.xml" "keeps"
download_test "break-before_1.xml" "keeps"
download_test "break-after_1.xml" "keeps"

echo "Downloading footnote tests..."
download_test "footnote_1.xml" "footnotes"
download_test "footnote_2.xml" "footnotes"

echo "Downloading image tests..."
download_test "external-graphic_1.xml" "images"
download_test "external-graphic_background-image.xml" "images"

echo "Downloading link tests..."
download_test "basic-link_external-destination.xml" "links"
download_test "basic-link_internal-desination-same-page-after.xml" "links"

echo "Downloading leader tests..."
download_test "leader_1.xml" "leaders"
download_test "leader_2.xml" "leaders"

echo "Downloading writing mode tests..."
download_test "writing-mode_rl_1.xml" "writing-modes"
download_test "writing-mode_tb_1.xml" "writing-modes"

echo ""
echo "Downloading Apache FOP examples..."

# Download basic examples
echo "  Basic examples:"
download_example "basic" "simple.fo"
download_example "basic" "normal.fo"
download_example "basic" "extensive.fo"

# Download table examples
echo "  Table examples:"
download_example "tables" "border.fo"
download_example "tables" "cell.fo"
download_example "tables" "list-block.fo"

echo ""
echo "========================================"
echo "Download complete!"
echo "========================================"
echo ""
echo "Test files location: $TEST_DIR"
echo "Example files location: $EXAMPLES_DIR"
echo ""
echo "Summary:"
echo "- Apache FOP has 768 layout engine test cases total"
echo "- Downloaded ~50 representative test cases covering major features"
echo "- Downloaded example files organized by category"
echo ""
echo "To run these tests against Folly:"
echo "  1. Convert .xml test cases to .fo files (they use a test format)"
echo "  2. Process through Folly and compare outputs"
echo "  3. Document compatibility and any failures"
echo ""
echo "Next steps:"
echo "  - Review test file format (see $TEST_DIR/README)"
echo "  - Create test harness to process FOP test cases"
echo "  - Add to validation plan corpus"
echo ""
