using Folly;
using Folly.Pdf;

Console.WriteLine("Folly XSL-FO to PDF Examples");
Console.WriteLine("=============================\n");

// Find the examples directory (where this project lives)
// This works whether run from project root or examples directory
var currentDir = Directory.GetCurrentDirectory();
var examplesDir = currentDir;

// If we're in the project root, navigate to examples
if (Directory.Exists(Path.Combine(currentDir, "examples")))
{
    examplesDir = Path.Combine(currentDir, "examples");
}
// If we're in the Folly.Examples subdirectory, go up one level
else if (currentDir.EndsWith("Folly.Examples"))
{
    examplesDir = Path.GetDirectoryName(currentDir)!;
}

// Create output directory in the examples folder
var outputDir = Path.Combine(examplesDir, "output");
Directory.CreateDirectory(outputDir);

Console.WriteLine($"Output directory: {outputDir}\n");

// Example 1: Hello World
Console.WriteLine("Generating Example 1: Hello World...");
GenerateHelloWorld(Path.Combine(outputDir, "01-hello-world.pdf"));

// Example 2: Multiple Blocks with Styling
Console.WriteLine("Generating Example 2: Multiple Blocks with Styling...");
GenerateMultipleBlocks(Path.Combine(outputDir, "02-multiple-blocks.pdf"));

// Example 3: Text Alignment
Console.WriteLine("Generating Example 3: Text Alignment...");
GenerateTextAlignment(Path.Combine(outputDir, "03-text-alignment.pdf"));

// Example 4: Borders and Backgrounds
Console.WriteLine("Generating Example 4: Borders and Backgrounds...");
GenerateBordersAndBackgrounds(Path.Combine(outputDir, "04-borders-backgrounds.pdf"));

// Example 5: Multi-Page Document
Console.WriteLine("Generating Example 5: Multi-Page Document...");
GenerateMultiPageDocument(Path.Combine(outputDir, "05-multi-page.pdf"));

// Example 6: Invoice
Console.WriteLine("Generating Example 6: Sample Invoice...");
GenerateInvoice(Path.Combine(outputDir, "06-invoice.pdf"));

// Example 7: Table
Console.WriteLine("Generating Example 7: Table Example...");
GenerateTableExample(Path.Combine(outputDir, "07-table.pdf"));

// Example 7.5: Multi-Page Table
Console.WriteLine("Generating Example 7.5: Multi-Page Table...");
GenerateMultiPageTableExample(Path.Combine(outputDir, "07b-multi-page-table.pdf"));

// Example 8: Images
Console.WriteLine("Generating Example 8: Image Example...");
GenerateImageExample(Path.Combine(outputDir, "08-images.pdf"));

// Example 9: Lists
Console.WriteLine("Generating Example 9: List Example...");
GenerateListExample(Path.Combine(outputDir, "09-lists.pdf"));

// Example 10: Keep and Break Constraints
Console.WriteLine("Generating Example 10: Keep and Break Constraints...");
GenerateKeepBreakExample(Path.Combine(outputDir, "10-keep-break.pdf"));

// Example 11: Headers, Footers, and Page Numbers
Console.WriteLine("Generating Example 11: Headers, Footers, and Page Numbers...");
GenerateHeaderFooterExample(Path.Combine(outputDir, "11-headers-footers.pdf"));

// Example 12: Markers for Dynamic Headers
Console.WriteLine("Generating Example 12: Markers for Dynamic Headers...");
GenerateMarkerExample(Path.Combine(outputDir, "12-markers.pdf"));

// Example 13: Conditional Page Masters
Console.WriteLine("Generating Example 13: Conditional Page Masters...");
GenerateConditionalPageMastersExample(Path.Combine(outputDir, "13-conditional-page-masters.pdf"));

// Example 14: Multi-Column Layout
Console.WriteLine("Generating Example 14: Multi-Column Layout...");
GenerateMultiColumnExample(Path.Combine(outputDir, "14-multi-column.pdf"));

// Example 15: Footnotes
Console.WriteLine("Generating Example 15: Footnotes...");
GenerateFootnoteExample(Path.Combine(outputDir, "15-footnotes.pdf"));

// Example 16: External Links
Console.WriteLine("Generating Example 16: External Links...");
GenerateLinksExample(Path.Combine(outputDir, "16-links.pdf"));

// Example 17: Bookmarks (PDF Outline)
Console.WriteLine("Generating Example 17: Bookmarks (PDF Outline)...");
GenerateBookmarksExample(Path.Combine(outputDir, "17-bookmarks.pdf"));

// Example 18: Inline Formatting
Console.WriteLine("Generating Example 18: Inline Formatting...");
GenerateInlineFormattingExample(Path.Combine(outputDir, "18-inline-formatting.pdf"));

// Example 19: BiDi Override (Right-to-Left Text)
Console.WriteLine("Generating Example 19: BiDi Override (Right-to-Left Text)...");
GenerateBidiOverrideExample(Path.Combine(outputDir, "19-bidi-override.pdf"));

// Example 20: PDF Metadata
Console.WriteLine("Generating Example 20: PDF Metadata...");
GenerateMetadataExample(Path.Combine(outputDir, "20-metadata.pdf"));

// Example 21a: Flatland Book (Simple Line Breaking)
Console.WriteLine("Generating Example 21a: Flatland Book (Simple Line Breaking)...");
GenerateFlatlandBookSimple(Path.Combine(outputDir, "21a-flatland-simple.pdf"), examplesDir);

// Example 21b: Flatland Book (Advanced: Knuth-Plass + Hyphenation)
Console.WriteLine("Generating Example 21b: Flatland Book (Advanced: Knuth-Plass + Hyphenation)...");
GenerateFlatlandBookAdvanced(Path.Combine(outputDir, "21b-flatland-advanced.pdf"), examplesDir);

// Example 22: Emergency Line Breaking
Console.WriteLine("Generating Example 22: Emergency Line Breaking...");
GenerateEmergencyLineBreaking(Path.Combine(outputDir, "22-emergency-line-breaking.pdf"));

// Example 23: Multi-Page Lists
Console.WriteLine("Generating Example 23: Multi-Page Lists...");
GenerateMultiPageListExample(Path.Combine(outputDir, "23-multi-page-lists.pdf"));

// Example 24: TrueType Fonts
Console.WriteLine("Generating Example 24: TrueType Fonts...");
GenerateTrueTypeFontsExample(Path.Combine(outputDir, "24-truetype-fonts.pdf"), examplesDir);

// Example 25: Font Fallback and System Fonts
Console.WriteLine("Generating Example 25: Font Fallback and System Fonts...");
GenerateFontFallbackExample(Path.Combine(outputDir, "25-font-fallback.pdf"));

// Example 26: Kerning Demonstration
Console.WriteLine("Generating Example 26: Kerning Demonstration...");
GenerateKerningExample(Path.Combine(outputDir, "26-kerning.pdf"), examplesDir);

// Example 27: Table Row Spanning
Console.WriteLine("Generating Example 27: Table Row Spanning...");
GenerateRowSpanningExample(Path.Combine(outputDir, "27-row-spanning.pdf"));

// Example 28: Proportional Column Widths
Console.WriteLine("Generating Example 28: Proportional Column Widths...");
GenerateProportionalWidthsExample(Path.Combine(outputDir, "28-proportional-widths.pdf"));

// Example 29: Content-Based Column Sizing
Console.WriteLine("Generating Example 29: Content-Based Column Sizing...");
GenerateContentBasedSizingExample(Path.Combine(outputDir, "29-content-based-sizing.pdf"));

// Example 30: Table Footer Repetition
Console.WriteLine("Generating Example 30: Table Footer Repetition...");
GenerateFooterRepetitionExample(Path.Combine(outputDir, "30-footer-repetition.pdf"));

// Example 31: Business Letterhead with Absolute Positioning
Console.WriteLine("Generating Example 31: Business Letterhead...");
GenerateLetterheadExample(Path.Combine(outputDir, "31-letterhead.pdf"));

// Example 32: Sidebars with Margin Notes
Console.WriteLine("Generating Example 32: Sidebars with Margin Notes...");
GenerateSidebarsExample(Path.Combine(outputDir, "32-sidebars.pdf"));

// Example 33: All Image Formats (JPEG, PNG, BMP, GIF, TIFF)
Console.WriteLine("Generating Example 33: All Image Formats...");
GenerateImageFormatsExample(Path.Combine(outputDir, "33-image-formats.pdf"), examplesDir);

// Example 34: Rounded Corners (Border Radius)
Console.WriteLine("Generating Example 34: Rounded Corners (Border Radius)...");
GenerateRoundedCornersExample(Path.Combine(outputDir, "34-rounded-corners.pdf"));

Console.WriteLine("\n✓ All examples generated successfully!");
Console.WriteLine($"\nView PDFs in: {outputDir}");
Console.WriteLine("\nValidate with qpdf:");
Console.WriteLine($"  qpdf --check {Path.Combine(outputDir, "*.pdf")}");

static void GenerateHelloWorld(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-family="Helvetica" font-size="24pt" text-align="center">
                Hello, Folly!
              </fo:block>
              <fo:block font-family="Times" font-size="12pt" margin-top="12pt">
                This is a simple XSL-FO document rendered to PDF by the Folly processor.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateMultipleBlocks(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="20pt" font-family="Helvetica" margin-bottom="12pt">
                Multiple Block Example
              </fo:block>
              <fo:block font-size="14pt" margin-bottom="8pt">
                First paragraph with default styling.
              </fo:block>
              <fo:block font-size="12pt" font-family="Times" margin-bottom="8pt">
                Second paragraph using Times font family.
              </fo:block>
              <fo:block font-size="10pt" font-family="Courier" margin-bottom="8pt">
                Third paragraph using Courier (monospace) font.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateTextAlignment(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" text-align="center" margin-bottom="24pt">
                Text Alignment Examples
              </fo:block>
              <fo:block text-align="start" margin-bottom="12pt">
                This text is aligned to the start (left in LTR languages).
              </fo:block>
              <fo:block text-align="center" margin-bottom="12pt">
                This text is centered.
              </fo:block>
              <fo:block text-align="end" margin-bottom="12pt">
                This text is aligned to the end (right in LTR languages).
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateBordersAndBackgrounds(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" text-align="center" margin-bottom="24pt">
                Borders and Backgrounds
              </fo:block>

              <fo:block background-color="yellow" padding="12pt" margin-bottom="12pt">
                This block has a yellow background.
              </fo:block>

              <fo:block border-width="2pt" border-style="solid" border-color="red"
                        padding="12pt" margin-bottom="12pt">
                This block has a solid red border.
              </fo:block>

              <fo:block border-width="3pt" border-style="dashed" border-color="blue"
                        padding="12pt" margin-bottom="12pt">
                This block has a dashed blue border.
              </fo:block>

              <fo:block background-color="#90EE90" border-width="2pt" border-style="solid"
                        border-color="#006400" padding="12pt" margin-bottom="12pt">
                This block has both a light green background and a dark green border.
              </fo:block>

              <fo:block background-color="silver" border-width="1pt" border-style="dotted"
                        border-color="black" padding="12pt">
                This block has a silver background with a dotted black border.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateMultiPageDocument(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="24pt" text-align="center" margin-bottom="24pt">
                Multi-Page Document Example
              </fo:block>

              <fo:block font-size="18pt" margin-bottom="12pt">Chapter 1: Introduction</fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
                incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud
                exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
              </fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu
                fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in
                culpa qui officia deserunt mollit anim id est laborum.
              </fo:block>

              <fo:block font-size="18pt" margin-top="24pt" margin-bottom="12pt">Chapter 2: Main Content</fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque
                laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi
                architecto beatae vitae dicta sunt explicabo.
              </fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia
                consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt.
              </fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci
                velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam
                aliquam quaerat voluptatem.
              </fo:block>

              <fo:block font-size="18pt" margin-top="24pt" margin-bottom="12pt">Chapter 3: More Details</fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium
                voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint
                occaecati cupiditate non provident.
              </fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Similique sunt in culpa qui officia deserunt mollitia animi, id est laborum et dolorum
                fuga. Et harum quidem rerum facilis est et expedita distinctio.
              </fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Nam libero tempore, cum soluta nobis est eligendi optio cumque nihil impedit quo minus
                id quod maxime placeat facere possimus, omnis voluptas assumenda est, omnis dolor
                repellendus.
              </fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus saepe
                eveniet ut et voluptates repudiandae sint et molestiae non recusandae.
              </fo:block>

              <fo:block font-size="18pt" margin-top="24pt" margin-bottom="12pt">Chapter 4: Continuation</fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Itaque earum rerum hic tenetur a sapiente delectus, ut aut reiciendis voluptatibus
                maiores alias consequatur aut perferendis doloribus asperiores repellat.
              </fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
                incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud
                exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
              </fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu
                fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in
                culpa qui officia deserunt mollit anim id est laborum.
              </fo:block>

              <fo:block font-size="18pt" margin-top="24pt" margin-bottom="12pt">Chapter 5: Final Thoughts</fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque
                laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi
                architecto beatae vitae dicta sunt explicabo.
              </fo:block>
              <fo:block font-size="12pt" margin-bottom="12pt">
                This document demonstrates automatic page breaking when content exceeds the available
                space on a single page. The layout engine will create additional pages as needed.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateInvoice(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="24pt" font-family="Helvetica" text-align="center" margin-bottom="24pt">
                INVOICE
              </fo:block>

              <fo:block font-size="10pt" margin-bottom="8pt">
                Invoice Number: INV-2024-001
              </fo:block>
              <fo:block font-size="10pt" margin-bottom="24pt">
                Date: January 15, 2024
              </fo:block>

              <fo:block font-size="12pt" font-family="Helvetica" margin-bottom="8pt">
                Bill To:
              </fo:block>
              <fo:block font-size="10pt" margin-bottom="4pt">
                Acme Corporation
              </fo:block>
              <fo:block font-size="10pt" margin-bottom="4pt">
                123 Business Street
              </fo:block>
              <fo:block font-size="10pt" margin-bottom="24pt">
                San Francisco, CA 94107
              </fo:block>

              <fo:block background-color="#E0E0E0" border-width="1pt" border-style="solid"
                        border-color="black" padding="8pt" margin-bottom="12pt">
                <fo:block font-size="12pt" font-family="Helvetica">
                  Item Description
                </fo:block>
              </fo:block>

              <fo:block border-width="1pt" border-style="solid" border-color="#CCCCCC"
                        padding="8pt" margin-bottom="8pt">
                <fo:block font-size="10pt" margin-bottom="4pt">
                  XSL-FO Processing Service - Professional License
                </fo:block>
                <fo:block font-size="10pt" text-align="end">
                  $999.00
                </fo:block>
              </fo:block>

              <fo:block border-width="1pt" border-style="solid" border-color="#CCCCCC"
                        padding="8pt" margin-bottom="8pt">
                <fo:block font-size="10pt" margin-bottom="4pt">
                  Technical Support (12 months)
                </fo:block>
                <fo:block font-size="10pt" text-align="end">
                  $299.00
                </fo:block>
              </fo:block>

              <fo:block background-color="#F0F0F0" border-width="2pt" border-style="solid"
                        border-color="black" padding="8pt" margin-top="12pt">
                <fo:block font-size="14pt" font-family="Helvetica" text-align="end">
                  Total: $1,298.00
                </fo:block>
              </fo:block>

              <fo:block font-size="10pt" margin-top="24pt" text-align="center">
                Thank you for your business!
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateTableExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" font-family="Helvetica" text-align="center" margin-bottom="24pt">
                Table Example
              </fo:block>

              <fo:table border-collapse="separate" border-spacing="2pt">
                <fo:table-column column-width="150pt"/>
                <fo:table-column column-width="150pt"/>
                <fo:table-column column-width="150pt"/>

                <fo:table-header>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid"
                                   border-color="black" background-color="#E0E0E0">
                      <fo:block font-family="Helvetica" font-size="12pt">
                        Product
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid"
                                   border-color="black" background-color="#E0E0E0">
                      <fo:block font-family="Helvetica" font-size="12pt">
                        Quantity
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid"
                                   border-color="black" background-color="#E0E0E0">
                      <fo:block font-family="Helvetica" font-size="12pt" text-align="end">
                        Price
                      </fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-header>

                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid" border-color="black">
                      <fo:block font-size="10pt">
                        Widget A
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid" border-color="black">
                      <fo:block font-size="10pt" text-align="center">
                        5
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid" border-color="black">
                      <fo:block font-size="10pt" text-align="end">
                        $25.00
                      </fo:block>
                    </fo:table-cell>
                  </fo:table-row>

                  <fo:table-row>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid" border-color="black">
                      <fo:block font-size="10pt">
                        Widget B
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid" border-color="black">
                      <fo:block font-size="10pt" text-align="center">
                        3
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid" border-color="black">
                      <fo:block font-size="10pt" text-align="end">
                        $45.00
                      </fo:block>
                    </fo:table-cell>
                  </fo:table-row>

                  <fo:table-row>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid" border-color="black">
                      <fo:block font-size="10pt">
                        Widget C
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid" border-color="black">
                      <fo:block font-size="10pt" text-align="center">
                        7
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border-width="1pt" border-style="solid" border-color="black">
                      <fo:block font-size="10pt" text-align="end">
                        $35.00
                      </fo:block>
                    </fo:table-cell>
                  </fo:table-row>

                  <fo:table-row>
                    <fo:table-cell padding="8pt" border-width="2pt" border-style="solid" border-color="black"
                                   background-color="#F0F0F0" number-columns-spanned="2">
                      <fo:block font-size="12pt" font-family="Helvetica" text-align="end">
                        Total:
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border-width="2pt" border-style="solid" border-color="black"
                                   background-color="#F0F0F0">
                      <fo:block font-size="12pt" font-family="Helvetica" text-align="end">
                        $105.00
                      </fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateMultiPageTableExample(string outputPath)
{
    // Generate a table with enough rows to span multiple pages
    var tableRows = new System.Text.StringBuilder();

    // Generate 100 rows of data
    for (int i = 1; i <= 100; i++)
    {
        tableRows.AppendLine($@"
                  <fo:table-row>
                    <fo:table-cell padding=""4pt"" border-width=""0.5pt"" border-style=""solid"" border-color=""#CCCCCC"">
                      <fo:block font-size=""9pt"">
                        Item #{i:D3}
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding=""4pt"" border-width=""0.5pt"" border-style=""solid"" border-color=""#CCCCCC"">
                      <fo:block font-size=""9pt"">
                        Product {(i % 5) + 1}
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding=""4pt"" border-width=""0.5pt"" border-style=""solid"" border-color=""#CCCCCC"">
                      <fo:block font-size=""9pt"" text-align=""center"">
                        {(i % 10) + 1}
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding=""4pt"" border-width=""0.5pt"" border-style=""solid"" border-color=""#CCCCCC"">
                      <fo:block font-size=""9pt"" text-align=""end"">
                        ${(i * 12.50):F2}
                      </fo:block>
                    </fo:table-cell>
                  </fo:table-row>");
    }

    var foXml = $"""
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <!-- First page master with more top margin for title and intro -->
            <fo:simple-page-master master-name="first" page-width="595pt" page-height="842pt">
              <fo:region-body margin-top="72pt" margin-bottom="72pt" margin-left="72pt" margin-right="72pt"/>
              <fo:region-before extent="36pt"/>
              <fo:region-after extent="36pt"/>
            </fo:simple-page-master>

            <!-- Subsequent pages with reduced top margin for better visual balance -->
            <fo:simple-page-master master-name="rest" page-width="595pt" page-height="842pt">
              <fo:region-body margin-top="12pt" margin-bottom="72pt" margin-left="72pt" margin-right="72pt"/>
              <fo:region-before extent="36pt"/>
              <fo:region-after extent="36pt"/>
            </fo:simple-page-master>

            <!-- Page sequence master to alternate between first and rest -->
            <fo:page-sequence-master master-name="pages">
              <fo:repeatable-page-master-alternatives>
                <fo:conditional-page-master-reference master-reference="first" page-position="first"/>
                <fo:conditional-page-master-reference master-reference="rest" page-position="rest"/>
              </fo:repeatable-page-master-alternatives>
            </fo:page-sequence-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="pages">
            <!-- Header with page number -->
            <fo:static-content flow-name="xsl-region-before">
              <fo:block font-size="10pt" font-family="Helvetica" text-align="end" padding-bottom="6pt" border-bottom-width="0.5pt" border-bottom-style="solid" border-bottom-color="black">
                Page <fo:page-number/> - Multi-Page Table Example
              </fo:block>
            </fo:static-content>

            <!-- Footer -->
            <fo:static-content flow-name="xsl-region-after">
              <fo:block font-size="9pt" font-family="Helvetica" text-align="center" padding-top="6pt" border-top-width="0.5pt" border-top-style="solid" border-top-color="black">
                Folly XSL-FO Processor - Table Page Breaking Demo
              </fo:block>
            </fo:static-content>

            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" font-family="Helvetica" text-align="center" margin-bottom="12pt" space-after="12pt">
                Multi-Page Table Example
              </fo:block>

              <fo:block font-size="11pt" margin-bottom="12pt">
                This example demonstrates Phase 1.1 functionality: tables that automatically break across pages
                with header repetition. The table below contains 100 rows and will span multiple pages.
              </fo:block>

              <!-- Table with header repetition (default behavior) -->
              <fo:table border-collapse="separate" border-spacing="0pt" space-before="12pt">
                <fo:table-column column-width="100pt"/>
                <fo:table-column column-width="120pt"/>
                <fo:table-column column-width="80pt"/>
                <fo:table-column column-width="100pt"/>

                <!-- Table header - will be repeated on each page -->
                <fo:table-header>
                  <fo:table-row>
                    <fo:table-cell padding="6pt" border-width="1pt" border-style="solid"
                                   border-color="black" background-color="#4A90E2">
                      <fo:block font-family="Helvetica" font-size="10pt" color="white" font-weight="bold">
                        ID
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt" border-width="1pt" border-style="solid"
                                   border-color="black" background-color="#4A90E2">
                      <fo:block font-family="Helvetica" font-size="10pt" color="white" font-weight="bold">
                        Product Name
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt" border-width="1pt" border-style="solid"
                                   border-color="black" background-color="#4A90E2">
                      <fo:block font-family="Helvetica" font-size="10pt" color="white" font-weight="bold" text-align="center">
                        Quantity
                      </fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt" border-width="1pt" border-style="solid"
                                   border-color="black" background-color="#4A90E2">
                      <fo:block font-family="Helvetica" font-size="10pt" color="white" font-weight="bold" text-align="end">
                        Amount
                      </fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-header>

                <fo:table-body>
                {tableRows}
                </fo:table-body>
              </fo:table>

              <fo:block font-size="10pt" margin-top="12pt" space-before="12pt">
                ✓ Header automatically repeats on each page
              </fo:block>
              <fo:block font-size="10pt">
                ✓ Rows break cleanly across page boundaries
              </fo:block>
              <fo:block font-size="10pt">
                ✓ Table spans {(100 / 30) + 1} pages
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateImageExample(string outputPath)
{
    // Get the path to test images
    var testImagePath = Path.Combine(Path.GetDirectoryName(outputPath)!, "..", "test-images", "test-100x100.jpg");
    testImagePath = Path.GetFullPath(testImagePath);

    var foXml = $"""
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" font-family="Helvetica" text-align="center" margin-bottom="24pt">
                Image Example
              </fo:block>

              <fo:block margin-bottom="12pt">
                <fo:external-graphic src="{testImagePath}" content-width="100pt" content-height="100pt"/>
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This demonstrates image embedding with JPEG format support.
              </fo:block>

              <fo:block margin-bottom="12pt">
                <fo:external-graphic src="{testImagePath}" content-width="200pt"/>
              </fo:block>

              <fo:block font-size="10pt" text-align="center">
                Images are embedded as PDF XObjects with proper scaling.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateListExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" font-family="Helvetica" text-align="center" margin-bottom="24pt">
                List Example
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This example demonstrates XSL-FO list blocks with labels and bodies:
              </fo:block>

              <fo:list-block provisional-distance-between-starts="36pt" provisional-label-separation="6pt" space-before="12pt" space-after="12pt">
                <fo:list-item space-before="6pt">
                  <fo:list-item-label>
                    <fo:block font-family="Helvetica">1.</fo:block>
                  </fo:list-item-label>
                  <fo:list-item-body>
                    <fo:block font-family="Helvetica">First item in the list demonstrates basic list functionality.</fo:block>
                  </fo:list-item-body>
                </fo:list-item>

                <fo:list-item space-before="6pt">
                  <fo:list-item-label>
                    <fo:block font-family="Helvetica">2.</fo:block>
                  </fo:list-item-label>
                  <fo:list-item-body>
                    <fo:block font-family="Helvetica">Second item shows proper spacing between items.</fo:block>
                  </fo:list-item-body>
                </fo:list-item>

                <fo:list-item space-before="6pt">
                  <fo:list-item-label>
                    <fo:block font-family="Helvetica">3.</fo:block>
                  </fo:list-item-label>
                  <fo:list-item-body>
                    <fo:block font-family="Helvetica">Third item demonstrates the provisional distance between the label start and body start.</fo:block>
                  </fo:list-item-body>
                </fo:list-item>
              </fo:list-block>

              <fo:block font-size="12pt" margin-bottom="12pt" margin-top="24pt">
                Bulleted list example:
              </fo:block>

              <fo:list-block provisional-distance-between-starts="24pt" provisional-label-separation="6pt">
                <fo:list-item space-before="6pt">
                  <fo:list-item-label>
                    <fo:block font-family="Helvetica">•</fo:block>
                  </fo:list-item-label>
                  <fo:list-item-body>
                    <fo:block font-family="Helvetica">Bullets can be used as labels</fo:block>
                  </fo:list-item-body>
                </fo:list-item>

                <fo:list-item space-before="6pt">
                  <fo:list-item-label>
                    <fo:block font-family="Helvetica">•</fo:block>
                  </fo:list-item-label>
                  <fo:list-item-body>
                    <fo:block font-family="Helvetica">The provisional-distance-between-starts property controls the indent</fo:block>
                  </fo:list-item-body>
                </fo:list-item>

                <fo:list-item space-before="6pt">
                  <fo:list-item-label>
                    <fo:block font-family="Helvetica">•</fo:block>
                  </fo:list-item-label>
                  <fo:list-item-body>
                    <fo:block font-family="Helvetica">The provisional-label-separation property controls the gap between label and body</fo:block>
                  </fo:list-item-body>
                </fo:list-item>
              </fo:list-block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateKeepBreakExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" font-family="Helvetica" text-align="center" margin-bottom="24pt">
                Keep and Break Constraints
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This example demonstrates XSL-FO keep and break constraints for pagination control, including keep-together, keep-with-next, keep-with-previous, and break-before/after.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="12pt" margin-bottom="6pt">
                Section 1: Normal Flow
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This block flows normally without any constraints. Content will break across pages naturally based on available space.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="12pt" margin-bottom="6pt" break-before="page">
                Section 2: Break Before
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The heading above has break-before="page", which forces it to start on a new page. This is useful for chapter breaks and major section divisions.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="12pt" margin-bottom="6pt">
                Section 3: Keep Together
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt" padding="12pt" background-color="#f0f0f0" border-width="1pt" border-style="solid" border-color="#cccccc" keep-together="always">
                This entire block has keep-together="always", which means it will not be split across pages. If it doesn't fit on the current page, the entire block will move to the next page. This is essential for keeping important content like code blocks, tables, or definitions together.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The block above will always appear as a complete unit on a single page.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="12pt" margin-bottom="6pt">
                Section 4: Break After
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt" break-after="page">
                This block has break-after="page", forcing a page break after its content. Everything following this block will start on a new page.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="12pt" margin-bottom="6pt">
                Section 5: After Break
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This section appears on a new page due to the break-after constraint in the previous section.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="12pt" margin-bottom="6pt" keep-with-next="always">
                Section 6: Keep With Next
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt" padding="8pt" background-color="#e8f4f8" border-width="1pt" border-style="solid" border-color="#7fb3d5">
                The heading above has keep-with-next="always", which ensures it stays together with this following block. This prevents orphaned headings at the bottom of pages - a heading will always have at least its first paragraph with it.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="12pt" margin-bottom="6pt">
                Regular Heading
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt" padding="8pt" background-color="#f8f0e8" border-width="1pt" border-style="solid" border-color="#d5b37f" keep-with-previous="always">
                This block has keep-with-previous="always", which keeps it together with the heading above. This achieves the same effect as keep-with-next but is specified on the following block instead.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="12pt" margin-bottom="6pt" keep-with-next="500">
                Figure 1: Diagram Example
              </fo:block>

              <fo:block font-size="10pt" font-style="italic" margin-bottom="12pt" padding="8pt" background-color="#f0f8f0" border-width="1pt" border-style="solid" border-color="#7fd59f">
                This is a figure caption. The heading above uses keep-with-next="500" (integer strength), which ensures the figure title and caption stay together on the same page. Integer values (1-999) allow fine-tuned control over keep priorities.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                These pagination controls give you precise control over document layout and ensure professional formatting. Keep constraints are essential for maintaining document quality and readability.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateHeaderFooterExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin-top="72pt" margin-bottom="72pt" margin-left="72pt" margin-right="72pt"/>
              <fo:region-before extent="36pt"/>
              <fo:region-after extent="36pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:static-content flow-name="xsl-region-before">
              <fo:block font-size="10pt" font-family="Helvetica" text-align="center" padding-top="12pt">
                Document with Headers and Footers - Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-after">
              <fo:block font-size="9pt" font-family="Helvetica" text-align="center" padding-bottom="12pt">
                Page <fo:page-number/> - Folly XSL-FO Processor
              </fo:block>
            </fo:static-content>

            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" font-family="Helvetica" text-align="center" margin-bottom="24pt">
                Headers, Footers, and Page Numbers
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This example demonstrates static-content for repeating headers and footers with dynamic page numbers.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The fo:page-number element is replaced with the actual page number at layout time. This allows you to create professional documents with running headers and footers.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="24pt" margin-bottom="12pt">
                Page 1 Content
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="24pt" margin-bottom="12pt" break-before="page">
                Page 2 Content
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Notice how the page number in the header and footer updates automatically for each page. This is a fundamental feature for multi-page documents like reports, books, and manuals.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The region-before (header) has an extent of 36pt, and the region-after (footer) also has 36pt. The region-body has margins that account for these regions.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="24pt" margin-bottom="12pt" break-before="page">
                Page 3 Content
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Each page displays its number in both the header and footer. This demonstrates the power of XSL-FO's static-content mechanism combined with dynamic page number generation.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                You can use this pattern to create sophisticated document layouts with consistent branding and navigation elements across all pages.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateMarkerExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin-top="72pt" margin-bottom="72pt" margin-left="72pt" margin-right="72pt"/>
              <fo:region-before extent="36pt"/>
              <fo:region-after extent="36pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:static-content flow-name="xsl-region-before">
              <fo:block font-size="10pt" font-family="Helvetica" text-align="center" padding-top="12pt">
                <fo:retrieve-marker retrieve-class-name="chapter-title"/> - Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-after">
              <fo:block font-size="9pt" font-family="Helvetica" text-align="center" padding-bottom="12pt">
                Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="20pt" font-family="Helvetica" font-weight="bold" margin-bottom="24pt">
                <fo:marker marker-class-name="chapter-title">Chapter 1: Introduction</fo:marker>
                Chapter 1: Introduction
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This example demonstrates the power of markers in XSL-FO. Markers allow you to capture content from the flow and display it in static-content areas like headers.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Notice how the chapter title appears in the header above. The fo:marker element captures the title, and fo:retrieve-marker displays it in the header.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This is essential for creating professional documents like books, reports, and manuals where you want the current chapter or section to appear in running headers.
              </fo:block>

              <fo:block font-size="20pt" font-family="Helvetica" font-weight="bold" margin-top="48pt" margin-bottom="24pt" break-before="page">
                <fo:marker marker-class-name="chapter-title">Chapter 2: Features</fo:marker>
                Chapter 2: Features
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                When this chapter starts on a new page, the header will automatically update to show "Chapter 2: Features".
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This dynamic behavior is achieved through the marker mechanism. Each time a new marker is encountered, it becomes available for retrieval in the static-content.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The retrieve-position property (default: first-starting-within-page) determines which marker to display when multiple markers of the same class appear on a page.
              </fo:block>

              <fo:block font-size="20pt" font-family="Helvetica" font-weight="bold" margin-top="48pt" margin-bottom="24pt" break-before="page">
                <fo:marker marker-class-name="chapter-title">Chapter 3: Conclusion</fo:marker>
                Chapter 3: Conclusion
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The header now shows "Chapter 3: Conclusion", demonstrating how markers adapt to the content on each page.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This feature is one of the most powerful aspects of XSL-FO for creating sophisticated, professional documents with dynamic headers and footers.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateConditionalPageMastersExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <!-- Define page masters for first, odd, and even pages -->
            <fo:simple-page-master master-name="first-page" page-width="595pt" page-height="842pt">
              <fo:region-body margin-top="144pt" margin-bottom="72pt" margin-left="72pt" margin-right="72pt"/>
              <fo:region-before extent="108pt"/>
              <fo:region-after extent="36pt"/>
            </fo:simple-page-master>

            <fo:simple-page-master master-name="odd-page" page-width="595pt" page-height="842pt">
              <fo:region-body margin-top="72pt" margin-bottom="72pt" margin-left="72pt" margin-right="72pt"/>
              <fo:region-before extent="36pt"/>
              <fo:region-after extent="36pt"/>
            </fo:simple-page-master>

            <fo:simple-page-master master-name="even-page" page-width="595pt" page-height="842pt">
              <fo:region-body margin-top="72pt" margin-bottom="72pt" margin-left="72pt" margin-right="72pt"/>
              <fo:region-before extent="36pt"/>
              <fo:region-after extent="36pt"/>
            </fo:simple-page-master>

            <!-- Define conditional page sequence master -->
            <fo:page-sequence-master master-name="document">
              <fo:repeatable-page-master-alternatives>
                <fo:conditional-page-master-reference master-reference="first-page" page-position="first"/>
                <fo:conditional-page-master-reference master-reference="odd-page" odd-or-even="odd"/>
                <fo:conditional-page-master-reference master-reference="even-page" odd-or-even="even"/>
              </fo:repeatable-page-master-alternatives>
            </fo:page-sequence-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="document">
            <!-- Headers for different page types -->
            <fo:static-content flow-name="xsl-region-before">
              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" text-align="center" padding-top="36pt">
                Conditional Page Masters Example
              </fo:block>
              <fo:block font-size="10pt" font-family="Helvetica" text-align="center" margin-top="6pt">
                Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-after">
              <fo:block font-size="9pt" font-family="Helvetica" text-align="center" padding-bottom="12pt">
                Footer - Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="24pt" font-family="Helvetica" font-weight="bold" text-align="center" margin-bottom="24pt">
                First Page Layout
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This is the first page, which uses the "first-page" master. Notice the larger top margin to accommodate a title area.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Conditional page masters allow different layouts for:
              </fo:block>

              <fo:block font-size="12pt" margin-left="24pt" margin-bottom="6pt">
                • First page (page-position="first")
              </fo:block>

              <fo:block font-size="12pt" margin-left="24pt" margin-bottom="6pt">
                • Odd pages (odd-or-even="odd")
              </fo:block>

              <fo:block font-size="12pt" margin-left="24pt" margin-bottom="12pt">
                • Even pages (odd-or-even="even")
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This is commonly used in books where left and right pages have mirrored layouts for binding, or where the first page needs special treatment like a cover or title page.
              </fo:block>

              <fo:block font-size="18pt" font-family="Helvetica" font-weight="bold" margin-top="36pt" margin-bottom="18pt" break-before="page">
                Second Page (Even)
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This is page 2, an even page. It uses the "even-page" master with standard margins.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                In a real book layout, even pages (left-hand pages) might have the page number on the left, while odd pages (right-hand pages) have the page number on the right.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The repeatable-page-master-alternatives element allows the XSL-FO processor to automatically select the appropriate page master based on the page number and position.
              </fo:block>

              <fo:block font-size="18pt" font-family="Helvetica" font-weight="bold" margin-top="36pt" margin-bottom="18pt" break-before="page">
                Third Page (Odd)
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This is page 3, an odd page using the "odd-page" master.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The conditional page master reference checks conditions in order and selects the first match. The conditions checked are:
              </fo:block>

              <fo:block font-size="12pt" margin-left="24pt" margin-bottom="6pt">
                1. page-position (first, last, rest, any)
              </fo:block>

              <fo:block font-size="12pt" margin-left="24pt" margin-bottom="6pt">
                2. odd-or-even (odd, even, any)
              </fo:block>

              <fo:block font-size="12pt" margin-left="24pt" margin-bottom="12pt">
                3. blank-or-not-blank (blank, not-blank, any)
              </fo:block>

              <fo:block font-size="18pt" font-family="Helvetica" font-weight="bold" margin-top="36pt" margin-bottom="18pt" break-before="page">
                Fourth Page (Even)
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Page 4 demonstrates that the pattern continues - this even page again uses the "even-page" master.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This feature is essential for professional publishing where page layout varies based on position in the document.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateMultiColumnExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4-3col" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt" column-count="3" column-gap="12pt"/>
              <fo:region-before extent="36pt"/>
              <fo:region-after extent="36pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="A4-3col">
            <fo:static-content flow-name="xsl-region-before">
              <fo:block font-size="10pt" font-family="Helvetica" text-align="center" padding-top="12pt" font-weight="bold">
                Multi-Column Layout Example
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-after">
              <fo:block font-size="9pt" font-family="Helvetica" text-align="center" padding-bottom="12pt">
                Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" font-family="Helvetica" font-weight="bold" text-align="center" margin-bottom="18pt">
                The Power of Multi-Column Layout
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Multi-column layout is a fundamental feature in professional publishing, particularly for newspapers, magazines, academic journals, and newsletters. XSL-FO provides powerful support for multi-column formatting through the column-count and column-gap properties.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This document demonstrates a three-column layout. Notice how the text flows naturally from one column to the next, just like in a newspaper. When one column fills up, the content automatically continues in the next column on the same page.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="18pt" margin-bottom="12pt">
                Key Features
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The column-count property specifies the number of columns, while column-gap defines the space between columns. These properties work together to create balanced, readable layouts that maximize page real estate while maintaining readability.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Content flows vertically down the first column until it reaches the bottom margin. At that point, it continues at the top of the second column. This process repeats for all columns before moving to the next page.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="18pt" margin-bottom="12pt">
                Common Applications
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Newspapers use multi-column layouts extensively to pack more information into limited space. The narrow columns are easier to read than full-width text blocks, as the eye doesn't have to travel as far horizontally.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Academic journals often use two-column layouts to create a more formal, scholarly appearance. This format has been a standard in academic publishing for decades.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Newsletters benefit from multi-column layouts by creating visual interest and allowing for flexible content organization. Different stories can be placed side by side, making the layout more dynamic.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The column gap should be wide enough to clearly separate columns but not so wide that it wastes valuable page space. A gap of 12 to 18 points is typical for most applications.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Font size also matters in multi-column layouts. With narrower columns, smaller fonts may become difficult to read, so it's important to maintain adequate font sizes for readability.
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="18pt" margin-bottom="12pt">
                Technical Implementation
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                In XSL-FO, multi-column layout is achieved by setting the column-count property on the fo:region-body element within the fo:simple-page-master. The layout engine automatically handles the flow of content between columns.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The Folly XSL-FO processor implements intelligent column balancing, ensuring that content flows smoothly from one column to the next. Block elements that don't fit in the remaining space of a column are moved to the top of the next column.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Multi-column layout is an essential tool in the document designer's toolkit. By understanding how to effectively use column-count and column-gap properties, you can create professional, readable documents that make the best use of available page space.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateFootnoteExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
              <fo:region-before extent="36pt"/>
              <fo:region-after extent="36pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="A4">
            <fo:static-content flow-name="xsl-region-before">
              <fo:block font-size="10pt" font-family="Helvetica" text-align="center" padding-top="12pt" font-weight="bold">
                Footnotes Example
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-after">
              <fo:block font-size="9pt" font-family="Helvetica" text-align="center" padding-bottom="12pt">
                Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-footnote-separator">
              <fo:block>
                <fo:leader leader-pattern="rule" leader-length="2in" rule-thickness="0.5pt"/>
              </fo:block>
            </fo:static-content>

            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" font-family="Helvetica" font-weight="bold" text-align="center" margin-bottom="18pt">
                Footnotes in Academic Publishing
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Footnotes are essential in academic and professional documents. They provide additional context, citations, and explanatory notes without disrupting the main text flow.<fo:footnote><fo:inline>1</fo:inline><fo:footnote-body><fo:block font-size="10pt">This is the first footnote, demonstrating basic footnote functionality.</fo:block></fo:footnote-body></fo:footnote>
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                In XSL-FO, footnotes consist of two parts: the inline reference (typically a superscript number) and the footnote body containing the actual note text.<fo:footnote><fo:inline>2</fo:inline><fo:footnote-body><fo:block font-size="10pt">The footnote body is rendered at the bottom of the page, separated from the main content.</fo:block></fo:footnote-body></fo:footnote>
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="18pt" margin-bottom="12pt">
                Historical Context
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The use of footnotes dates back to ancient manuscripts, where scribes would add marginal notes to clarify or expand upon the main text.<fo:footnote><fo:inline>3</fo:inline><fo:footnote-body><fo:block font-size="10pt">Medieval manuscripts often featured elaborate marginal annotations that evolved into modern footnotes.</fo:block></fo:footnote-body></fo:footnote> This practice evolved over centuries into the standardized footnote system we use today.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Modern academic writing relies heavily on footnotes for citations, particularly in disciplines like history, law, and the humanities.<fo:footnote><fo:inline>4</fo:inline><fo:footnote-body><fo:block font-size="10pt">Some fields prefer endnotes or parenthetical citations, but footnotes remain popular for their immediacy.</fo:block></fo:footnote-body></fo:footnote>
              </fo:block>

              <fo:block font-size="14pt" font-family="Helvetica" font-weight="bold" margin-top="18pt" margin-bottom="12pt">
                Technical Implementation
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                In the Folly XSL-FO processor, footnotes are collected during the layout phase and rendered at the bottom of the page.<fo:footnote><fo:inline>5</fo:inline><fo:footnote-body><fo:block font-size="10pt">The layout engine reserves space at the page bottom and renders footnote bodies there.</fo:block></fo:footnote-body></fo:footnote> This ensures proper pagination and prevents footnotes from being orphaned on different pages.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The footnote reference appears inline with the text, while the footnote body is automatically positioned at the page bottom, maintaining the document's professional appearance and readability.<fo:footnote><fo:inline>6</fo:inline><fo:footnote-body><fo:block font-size="10pt">Multiple footnotes on the same page are stacked vertically in the footnote area.</fo:block></fo:footnote-body></fo:footnote>
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateLinksExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="210mm" page-height="297mm"
                                 margin-top="1in" margin-bottom="1in"
                                 margin-left="1in" margin-right="1in">
              <fo:region-body margin-top="0.5in" margin-bottom="0.5in"/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="24pt" font-weight="bold" margin-bottom="24pt" text-align="center">
                XSL-FO Links Example
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-bottom="12pt">
                External Links (fo:basic-link with external-destination)
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Links allow users to navigate to external URLs or resources. Click the links below to test:
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt" margin-left="20pt">
                • Visit the <fo:basic-link external-destination="https://www.w3.org/TR/xsl/" color="blue" text-decoration="underline">W3C XSL-FO Specification</fo:basic-link> to learn more about formatting objects.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt" margin-left="20pt">
                • Check out <fo:basic-link external-destination="https://github.com/anthropics/folly" color="blue" text-decoration="underline">Folly on GitHub</fo:basic-link> for the source code.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt" margin-left="20pt">
                • Send an email to <fo:basic-link external-destination="mailto:support@example.com" color="blue" text-decoration="underline">support@example.com</fo:basic-link> for assistance.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="24pt" margin-left="20pt">
                • Open a local file: <fo:basic-link external-destination="file:///home/user/document.pdf" color="blue" text-decoration="underline">document.pdf</fo:basic-link>
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-bottom="12pt">
                Link Styling
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Links can be styled with different colors and text decorations:
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt" margin-left="20pt">
                • Blue underlined: <fo:basic-link external-destination="https://example.com" color="blue" text-decoration="underline">Default Style</fo:basic-link>
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt" margin-left="20pt">
                • Red underlined: <fo:basic-link external-destination="https://example.com" color="red" text-decoration="underline">Custom Color</fo:basic-link>
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="24pt" margin-left="20pt">
                • Green no underline: <fo:basic-link external-destination="https://example.com" color="green" text-decoration="none">No Decoration</fo:basic-link>
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-bottom="12pt">
                Implementation Notes
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt">
                • External links use the /URI action in PDF annotations
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt">
                • Links are clickable rectangles overlaid on the text
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt">
                • The border property is set to [0 0 0] for invisible borders
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt">
                • show-destination controls whether links open in the same window (replace) or new window (new)
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt">
                • Internal links (internal-destination) can reference named destinations within the document
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateBookmarksExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="210mm" page-height="297mm"
                                 margin-top="1in" margin-bottom="1in"
                                 margin-left="1in" margin-right="1in">
              <fo:region-body margin-top="0.5in" margin-bottom="0.5in"/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:bookmark-tree>
            <fo:bookmark internal-destination="chapter1" starting-state="show">
              <fo:bookmark-title>Chapter 1: Introduction</fo:bookmark-title>
              <fo:bookmark internal-destination="section1-1">
                <fo:bookmark-title>1.1 Getting Started</fo:bookmark-title>
              </fo:bookmark>
              <fo:bookmark internal-destination="section1-2">
                <fo:bookmark-title>1.2 Basic Concepts</fo:bookmark-title>
              </fo:bookmark>
            </fo:bookmark>

            <fo:bookmark internal-destination="chapter2" starting-state="show">
              <fo:bookmark-title>Chapter 2: Advanced Features</fo:bookmark-title>
              <fo:bookmark internal-destination="section2-1">
                <fo:bookmark-title>2.1 Tables and Lists</fo:bookmark-title>
              </fo:bookmark>
              <fo:bookmark internal-destination="section2-2">
                <fo:bookmark-title>2.2 Images and Graphics</fo:bookmark-title>
              </fo:bookmark>
              <fo:bookmark internal-destination="section2-3">
                <fo:bookmark-title>2.3 Links and Bookmarks</fo:bookmark-title>
              </fo:bookmark>
            </fo:bookmark>

            <fo:bookmark internal-destination="chapter3" starting-state="hide">
              <fo:bookmark-title>Chapter 3: Reference</fo:bookmark-title>
              <fo:bookmark internal-destination="section3-1">
                <fo:bookmark-title>3.1 Property Index</fo:bookmark-title>
              </fo:bookmark>
              <fo:bookmark internal-destination="section3-2">
                <fo:bookmark-title>3.2 Element Index</fo:bookmark-title>
              </fo:bookmark>
            </fo:bookmark>

            <fo:bookmark external-destination="https://www.w3.org/TR/xsl/">
              <fo:bookmark-title>External: XSL-FO Specification</fo:bookmark-title>
            </fo:bookmark>
          </fo:bookmark-tree>

          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="24pt" font-weight="bold" margin-bottom="24pt" text-align="center">
                PDF Bookmarks Example
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="24pt">
                This PDF demonstrates the bookmark (outline) feature. Open the bookmarks panel in your PDF viewer to see the table of contents structure.
              </fo:block>

              <fo:block font-size="18pt" font-weight="bold" margin-top="36pt" margin-bottom="12pt" id="chapter1">
                Chapter 1: Introduction
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="24pt" margin-bottom="8pt" id="section1-1">
                1.1 Getting Started
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The fo:bookmark-tree element contains the hierarchical structure of bookmarks that will appear in the PDF viewer's navigation pane. Each fo:bookmark represents an entry in the outline.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Bookmarks can reference internal destinations (pages or elements within the document) using the internal-destination attribute, which should match an id attribute on a formatting object.
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="24pt" margin-bottom="8pt" id="section1-2">
                1.2 Basic Concepts
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Bookmarks create a navigational structure separate from the document content. They appear in most PDF viewers as a collapsible tree in a sidebar, allowing readers to quickly jump to different sections.
              </fo:block>

              <fo:block font-size="18pt" font-weight="bold" margin-top="36pt" margin-bottom="12pt" id="chapter2">
                Chapter 2: Advanced Features
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="24pt" margin-bottom="8pt" id="section2-1">
                2.1 Tables and Lists
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Bookmarks can have nested child bookmarks, creating a hierarchical structure. The starting-state attribute controls whether child bookmarks are initially expanded ("show") or collapsed ("hide").
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="24pt" margin-bottom="8pt" id="section2-2">
                2.2 Images and Graphics
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                In PDF, the bookmark count determines the visual state: positive counts mean expanded, negative means collapsed. The Folly processor handles this automatically based on the starting-state attribute.
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="24pt" margin-bottom="8pt" id="section2-3">
                2.3 Links and Bookmarks
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Bookmarks are different from links: bookmarks appear in the PDF outline/navigation pane, while links (fo:basic-link) appear as clickable areas in the document content itself.
              </fo:block>

              <fo:block font-size="18pt" font-weight="bold" margin-top="36pt" margin-bottom="12pt" id="chapter3">
                Chapter 3: Reference
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="24pt" margin-bottom="8pt" id="section3-1">
                3.1 Property Index
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt">
                Key bookmark properties:
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt" margin-left="20pt">
                • internal-destination: Links to an id within the document
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt" margin-left="20pt">
                • external-destination: Links to an external URI
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="24pt" margin-left="20pt">
                • starting-state: Controls initial expansion (show/hide)
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="24pt" margin-bottom="8pt" id="section3-2">
                3.2 Element Index
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt">
                Bookmark elements:
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt" margin-left="20pt">
                • fo:bookmark-tree: Root of the bookmark hierarchy
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt" margin-left="20pt">
                • fo:bookmark: Individual bookmark entry (can be nested)
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt" margin-left="20pt">
                • fo:bookmark-title: The text displayed in the outline
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateInlineFormattingExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-family="Helvetica" font-size="20pt" font-weight="bold" margin-bottom="12pt">
                Inline Formatting Examples
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                1. Font Weight and Style
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This is normal text with <fo:inline font-weight="bold">bold text</fo:inline> and <fo:inline font-style="italic">italic text</fo:inline>.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                You can also combine: <fo:inline font-weight="bold" font-style="italic">bold italic text</fo:inline>.
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                2. Text Color
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Text can be <fo:inline color="red">red</fo:inline>, <fo:inline color="blue">blue</fo:inline>, or <fo:inline color="green">green</fo:inline>.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                You can also use hex colors: <fo:inline color="#FF6600">orange</fo:inline> or <fo:inline color="#9933CC">purple</fo:inline>.
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                3. Text Decoration
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This text has <fo:inline text-decoration="underline">underline</fo:inline>, <fo:inline text-decoration="overline">overline</fo:inline>, and <fo:inline text-decoration="line-through">strikethrough</fo:inline>.
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                4. Background Color
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                You can highlight text with <fo:inline background-color="yellow">yellow background</fo:inline> or <fo:inline background-color="#CCFFCC">light green background</fo:inline>.
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                5. Font Size
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Normal text with <fo:inline font-size="16pt">larger text</fo:inline> and <fo:inline font-size="8pt">smaller text</fo:inline> inline.
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                6. Combined Formatting
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                All formatting can be combined: <fo:inline font-weight="bold" color="red" background-color="yellow" text-decoration="underline">bold red underlined text on yellow</fo:inline>.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Create <fo:inline font-family="Courier" background-color="#F0F0F0" color="#000080">code snippets</fo:inline> with monospace font and light background.
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                7. Practical Examples
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                <fo:inline font-weight="bold">Important:</fo:inline> Always validate your input before processing.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                The function <fo:inline font-family="Courier">calculateTotal()</fo:inline> returns a <fo:inline font-family="Courier">decimal</fo:inline> value.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                <fo:inline color="red" font-weight="bold">ERROR:</fo:inline> Connection timeout occurred.
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                <fo:inline color="green" font-weight="bold">SUCCESS:</fo:inline> Operation completed successfully.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateBidiOverrideExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-family="Helvetica" font-size="20pt" font-weight="bold" margin-bottom="12pt">
                BiDi Override Examples (Right-to-Left Text)
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                1. Basic RTL Text
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Normal LTR text: Hello World
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                RTL override: <fo:bidi-override direction="rtl">Hello World</fo:bidi-override>
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                2. RTL with Different Fonts
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                <fo:bidi-override direction="rtl" font-family="Times">Times RTL Text</fo:bidi-override>
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                <fo:bidi-override direction="rtl" font-family="Courier">Courier RTL Text</fo:bidi-override>
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                3. RTL with Styling
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                <fo:bidi-override direction="rtl" font-weight="bold" color="blue">Bold Blue RTL</fo:bidi-override>
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                <fo:bidi-override direction="rtl" font-style="italic" color="red">Italic Red RTL</fo:bidi-override>
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                4. Mixed Directionality
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                LTR text followed by <fo:bidi-override direction="rtl">RTL text</fo:bidi-override> and more LTR text.
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                5. RTL Numbers and Symbols
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                Normal: Price: $123.45
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                RTL: <fo:bidi-override direction="rtl">Price: $123.45</fo:bidi-override>
              </fo:block>

              <fo:block font-size="10pt" color="gray" margin-top="24pt">
                Note: This is a simplified BiDi implementation. For production use with complex scripts
                (Arabic, Hebrew, etc.), a full Unicode BiDi Algorithm implementation would be required.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateMetadataExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:declarations>
            <fo:info>
              <title>PDF Metadata Example Document</title>
              <author>Folly XSL-FO Processor</author>
              <subject>Demonstration of PDF Document Information Dictionary</subject>
              <keywords>PDF, metadata, XSL-FO, document properties, Folly</keywords>
              <creator>Folly Examples Generator</creator>
            </fo:info>
          </fo:declarations>

          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt"
                                   margin-top="72pt" margin-bottom="72pt"
                                   margin-left="72pt" margin-right="72pt">
              <fo:region-body margin-top="36pt" margin-bottom="36pt"/>
              <fo:region-before extent="36pt"/>
              <fo:region-after extent="36pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="A4">
            <fo:static-content flow-name="xsl-region-before">
              <fo:block text-align="center" font-size="10pt" color="#666666">
                Example 20: PDF Metadata
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-after">
              <fo:block text-align="center" font-size="9pt" color="#999999">
                Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="24pt" font-weight="bold" color="#2c3e50"
                       text-align="center" margin-bottom="24pt">
                PDF Metadata Example
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                What is PDF Metadata?
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt" text-align="justify">
                PDF metadata is information about a document that is stored in the PDF's Document
                Information Dictionary. This metadata is displayed in PDF viewers when you view
                document properties (typically under File → Properties).
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                Metadata in This Document
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt">
                This document demonstrates metadata support in Folly. The following metadata fields
                were specified using XSL-FO declarations:
              </fo:block>

              <fo:block font-size="11pt" margin-left="24pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">Title:</fo:inline> PDF Metadata Example Document
              </fo:block>

              <fo:block font-size="11pt" margin-left="24pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">Author:</fo:inline> Folly XSL-FO Processor
              </fo:block>

              <fo:block font-size="11pt" margin-left="24pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">Subject:</fo:inline> Demonstration of PDF Document Information Dictionary
              </fo:block>

              <fo:block font-size="11pt" margin-left="24pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">Keywords:</fo:inline> PDF, metadata, XSL-FO, document properties, Folly
              </fo:block>

              <fo:block font-size="11pt" margin-left="24pt" margin-bottom="12pt">
                • <fo:inline font-weight="bold">Creator:</fo:inline> Folly Examples Generator
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                How to View Metadata
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="8pt">
                To view the metadata in this PDF:
              </fo:block>

              <fo:block font-size="11pt" margin-left="24pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">Adobe Acrobat/Reader:</fo:inline> File → Properties (Ctrl+D)
              </fo:block>

              <fo:block font-size="11pt" margin-left="24pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">macOS Preview:</fo:inline> Tools → Show Inspector (Cmd+I)
              </fo:block>

              <fo:block font-size="11pt" margin-left="24pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">Linux (Evince):</fo:inline> File → Properties
              </fo:block>

              <fo:block font-size="11pt" margin-left="24pt" margin-bottom="12pt">
                • <fo:inline font-weight="bold">Command line:</fo:inline> pdfinfo 20-metadata.pdf
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" margin-top="18pt" margin-bottom="8pt">
                Additional Metadata
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt" text-align="justify">
                In addition to the fields specified above, Folly automatically includes:
              </fo:block>

              <fo:block font-size="11pt" margin-left="24pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">Producer:</fo:inline> The Folly library name and version
              </fo:block>

              <fo:block font-size="11pt" margin-left="24pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">Creation Date:</fo:inline> Timestamp when the PDF was generated
              </fo:block>

              <fo:block font-size="10pt" color="#666666" margin-top="24pt" border-top="1pt solid #cccccc"
                       padding-top="12pt">
                <fo:inline font-style="italic">
                  Note: Metadata can also be specified programmatically using the PdfOptions class
                  when calling SavePdf(). Programmatically-specified metadata takes precedence
                  over XSL-FO declarations.
                </fo:inline>
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateFlatlandBookSimple(string outputPath, string examplesDir)
{
    // Load the Flatland book from the examples/books directory
    var booksDir = Path.Combine(examplesDir, "books", "flatland");
    var foFilePath = Path.Combine(booksDir, "flatland.fo");

    if (!File.Exists(foFilePath))
    {
        Console.WriteLine($"Warning: Flatland FO file not found at {foFilePath}");
        Console.WriteLine("Skipping flatland example.");
        return;
    }

    // Read the FO file
    var foXml = File.ReadAllText(foFilePath);

    // Change to the flatland directory so relative image paths work
    var originalDir = Directory.GetCurrentDirectory();
    try
    {
        Directory.SetCurrentDirectory(booksDir);

        // Use simple/greedy line breaking (default, fast)
        var layoutOptions = new LayoutOptions
        {
            LineBreaking = LineBreakingAlgorithm.Greedy,
            EnableHyphenation = false
        };

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));

        // Build area tree with layout options
        var areaTree = doc.BuildAreaTree(layoutOptions);

        // Render to PDF
        using var stream = File.Create(outputPath);
        using var renderer = new Folly.Pdf.PdfRenderer(stream, new PdfOptions());
        renderer.Render(areaTree, doc.Root.BookmarkTree);
    }
    finally
    {
        // Restore original directory
        Directory.SetCurrentDirectory(originalDir);
    }
}

static void GenerateFlatlandBookAdvanced(string outputPath, string examplesDir)
{
    // Load the Flatland book from the examples/books directory
    var booksDir = Path.Combine(examplesDir, "books", "flatland");
    var foFilePath = Path.Combine(booksDir, "flatland.fo");

    if (!File.Exists(foFilePath))
    {
        Console.WriteLine($"Warning: Flatland FO file not found at {foFilePath}");
        Console.WriteLine("Skipping flatland example.");
        return;
    }

    // Read the FO file
    var foXml = File.ReadAllText(foFilePath);

    // Change to the flatland directory so relative image paths work
    var originalDir = Directory.GetCurrentDirectory();
    try
    {
        Directory.SetCurrentDirectory(booksDir);

        // Use advanced Knuth-Plass line breaking with hyphenation for optimal typography
        var layoutOptions = new LayoutOptions
        {
            LineBreaking = LineBreakingAlgorithm.Optimal,
            EnableHyphenation = true,
            HyphenationLanguage = "en-US",
            HyphenationMinWordLength = 5,
            HyphenationMinLeftChars = 2,
            HyphenationMinRightChars = 3
        };

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));

        // Build area tree with layout options
        var areaTree = doc.BuildAreaTree(layoutOptions);

        // Render to PDF
        using var stream = File.Create(outputPath);
        using var renderer = new Folly.Pdf.PdfRenderer(stream, new PdfOptions());
        renderer.Render(areaTree, doc.Root.BookmarkTree);
    }
    finally
    {
        // Restore original directory
        Directory.SetCurrentDirectory(originalDir);
    }
}

static void GenerateEmergencyLineBreaking(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <!-- Title -->
              <fo:block font-size="24pt" font-weight="bold" text-align="center" margin-bottom="24pt">
                Emergency Line Breaking
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="12pt">
                This example demonstrates Folly's emergency line breaking features for handling overflow text in narrow columns.
              </fo:block>

              <!-- Section 1: Normal Wrapping (default) -->
              <fo:block font-size="16pt" font-weight="bold" margin-top="18pt" margin-bottom="12pt">
                1. Normal Wrapping (wrap-option="wrap")
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="6pt">
                Default behavior wraps text normally at word boundaries:
              </fo:block>

              <fo:block-container width="150pt" border="1pt solid black" padding="6pt" margin-bottom="12pt">
                <fo:block font-size="10pt" wrap-option="wrap">
                  This is a normal paragraph with regular words that will wrap at word boundaries. Very long words like ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 will be broken character-by-character as a last resort.
                </fo:block>
              </fo:block-container>

              <!-- Section 2: No Wrap -->
              <fo:block font-size="16pt" font-weight="bold" margin-top="18pt" margin-bottom="12pt">
                2. No Wrapping (wrap-option="no-wrap")
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="6pt">
                Setting wrap-option="no-wrap" prevents all line breaking (text overflows):
              </fo:block>

              <fo:block-container width="150pt" border="1pt solid black" padding="6pt" margin-bottom="12pt">
                <fo:block font-size="10pt" wrap-option="no-wrap">
                  This text will not wrap even though it is much longer than the container width. It will overflow to the right.
                </fo:block>
              </fo:block-container>

              <!-- Section 3: Emergency Breaking in Very Narrow Columns -->
              <fo:block font-size="16pt" font-weight="bold" margin-top="18pt" margin-bottom="12pt">
                3. Emergency Breaking in Very Narrow Columns
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="6pt">
                When words are too long to fit even on their own line, emergency breaking splits them character-by-character:
              </fo:block>

              <fo:block-container width="80pt" border="1pt solid black" padding="6pt" margin-bottom="12pt">
                <fo:block font-size="10pt">
                  ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ
                </fo:block>
              </fo:block-container>

              <!-- Section 4: Multiple Overflow Words -->
              <fo:block font-size="16pt" font-weight="bold" margin-top="18pt" margin-bottom="12pt">
                4. Multiple Overflow Words
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="6pt">
                Emergency breaking handles multiple overflow words in sequence:
              </fo:block>

              <fo:block-container width="70pt" border="1pt solid black" padding="6pt" margin-bottom="12pt">
                <fo:block font-size="10pt">
                  OVERFLOW ANOTHERLONG YETANOTHER FINALWORD
                </fo:block>
              </fo:block-container>

              <!-- Section 5: Mixed Content -->
              <fo:block font-size="16pt" font-weight="bold" margin-top="18pt" margin-bottom="12pt">
                5. Mixed Content
              </fo:block>

              <fo:block font-size="12pt" margin-bottom="6pt">
                Emergency breaking works alongside normal word wrapping and hyphenation:
              </fo:block>

              <fo:block-container width="120pt" border="1pt solid black" padding="6pt" margin-bottom="12pt">
                <fo:block font-size="10pt">
                  Normal words wrap naturally, but VERYLONGWORDSWITHNOBREAKS are handled by emergency character-level breaking.
                </fo:block>
              </fo:block-container>

              <!-- Notes -->
              <fo:block font-size="14pt" font-weight="bold" margin-top="24pt" margin-bottom="12pt">
                Implementation Notes
              </fo:block>

              <fo:block font-size="11pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">wrap-option="wrap"</fo:inline> (default): Wraps text at word boundaries, uses hyphenation if enabled, and applies emergency character-level breaking as a last resort for overflow words.
              </fo:block>

              <fo:block font-size="11pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">wrap-option="no-wrap"</fo:inline>: Prevents all line breaking. Text will overflow the container boundaries.
              </fo:block>

              <fo:block font-size="11pt" margin-bottom="6pt">
                • <fo:inline font-weight="bold">Emergency Breaking</fo:inline>: Automatically triggered when a word is too long to fit on a line even by itself. Breaks the word character-by-character to fit the available width.
              </fo:block>

              <fo:block font-size="11pt" margin-top="12pt" color="#666666">
                This feature ensures that documents can handle any text input gracefully, even with extremely narrow columns or very long words that cannot be hyphenated.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateMultiPageListExample(string outputPath)
{
    // Generate 100 list items that will span multiple pages
    var listItems = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"""
                <fo:list-item space-before="6pt"{(i % 20 == 0 ? " keep-together=\"always\"" : "")}>
                  <fo:list-item-label end-indent="label-end()">
                    <fo:block font-weight="bold" color="#0066CC">{i}.</fo:block>
                  </fo:list-item-label>
                  <fo:list-item-body start-indent="body-start()">
                    <fo:block>
                      <fo:inline font-weight="bold">List Item {i}:</fo:inline> This is a list item with some content.
                      {(i % 10 == 0 ? "This item has extra content to make it taller and demonstrate the page breaking behavior more effectively." : "")}
                    </fo:block>
                    {(i % 20 == 0 ? "<fo:block space-before=\"3pt\" font-size=\"9pt\" color=\"#666\">This item uses keep-together=\"always\" to prevent breaking across pages.</fo:block>" : "")}
                  </fo:list-item-body>
                </fo:list-item>
                """));

    var foXml = $"""
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="210mm" page-height="297mm" margin="25mm">
              <fo:region-body margin-top="20mm" margin-bottom="20mm"/>
              <fo:region-before extent="15mm"/>
              <fo:region-after extent="15mm"/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="A4">
            <fo:static-content flow-name="xsl-region-before">
              <fo:block text-align="center" font-size="10pt" color="#666666" border-bottom="0.5pt solid #CCCCCC" padding-bottom="3pt">
                Example 23: Multi-Page Lists
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-after">
              <fo:block text-align="center" font-size="9pt" color="#666666" border-top="0.5pt solid #CCCCCC" padding-top="3pt">
                Page <fo:page-number/> — Multi-Page List Example
              </fo:block>
            </fo:static-content>

            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" font-weight="bold" color="#003366" margin-bottom="12pt">
                Multi-Page Lists
              </fo:block>

              <fo:block font-size="11pt" margin-bottom="12pt" color="#666666">
                This example demonstrates list page breaking. The list below contains 100 items that will automatically break across multiple pages. Notice how items marked with keep-together="always" (every 20th item) remain intact on a single page.
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" color="#0066CC" margin-top="12pt" margin-bottom="8pt">
                Features Demonstrated:
              </fo:block>

              <fo:list-block provisional-distance-between-starts="30pt" provisional-label-separation="6pt" margin-bottom="12pt">
                <fo:list-item space-before="4pt">
                  <fo:list-item-label end-indent="label-end()">
                    <fo:block>•</fo:block>
                  </fo:list-item-label>
                  <fo:list-item-body start-indent="body-start()">
                    <fo:block>Automatic page breaks between list items</fo:block>
                  </fo:list-item-body>
                </fo:list-item>
                <fo:list-item space-before="4pt">
                  <fo:list-item-label end-indent="label-end()">
                    <fo:block>•</fo:block>
                  </fo:list-item-label>
                  <fo:list-item-body start-indent="body-start()">
                    <fo:block>keep-together constraint support on list items</fo:block>
                  </fo:list-item-body>
                </fo:list-item>
                <fo:list-item space-before="4pt">
                  <fo:list-item-label end-indent="label-end()">
                    <fo:block>•</fo:block>
                  </fo:list-item-label>
                  <fo:list-item-body start-indent="body-start()">
                    <fo:block>Proper spacing and formatting across page boundaries</fo:block>
                  </fo:list-item-body>
                </fo:list-item>
              </fo:list-block>

              <fo:block font-size="14pt" font-weight="bold" color="#0066CC" margin-top="18pt" margin-bottom="8pt">
                100-Item List (Spanning Multiple Pages):
              </fo:block>

              <fo:list-block provisional-distance-between-starts="30pt" provisional-label-separation="6pt">
                {listItems}
              </fo:list-block>

              <fo:block font-size="11pt" margin-top="18pt" padding="8pt" background-color="#F0F8FF" border="1pt solid #0066CC">
                <fo:inline font-weight="bold">Note:</fo:inline> This list spans multiple pages seamlessly. Items with keep-together="always" (items 20, 40, 60, 80, 100) will not be split across pages, demonstrating the pagination constraint system.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateTrueTypeFontsExample(string outputPath, string examplesDir)
{
    // Try to find the test fonts
    // Look in the tests directory relative to examples directory
    var projectRoot = Path.GetFullPath(Path.Combine(examplesDir, ".."));
    var testFontsDir = Path.Combine(projectRoot, "tests", "Folly.FontTests", "TestFonts");

    // Check if fonts exist
    var robotoPath = Path.Combine(testFontsDir, "Roboto-Regular.ttf");
    var liberationPath = Path.Combine(testFontsDir, "LiberationSans-Regular.ttf");

    if (!File.Exists(robotoPath) || !File.Exists(liberationPath))
    {
        Console.WriteLine($"  ⚠ Skipping TrueType example - test fonts not found at: {testFontsDir}");
        return;
    }

    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-family="Roboto" font-size="24pt" text-align="center" margin-bottom="24pt" color="#2196F3">
                TrueType Font Embedding Demo
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="14pt" margin-bottom="12pt">
                This document demonstrates embedded TrueType fonts with font subsetting for optimal file size.
              </fo:block>

              <fo:block font-family="Roboto" font-size="18pt" margin-top="24pt" margin-bottom="12pt" color="#1976D2">
                Roboto Font Sample
              </fo:block>

              <fo:block font-family="Roboto" font-size="12pt" margin-bottom="12pt">
                The Roboto font family is a sans-serif typeface developed by Google as the system font for Android.
                It features friendly and open curves, providing a comfortable reading experience.
              </fo:block>

              <fo:block font-family="Roboto" font-size="10pt" margin-bottom="12pt" color="#666666">
                ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz 0123456789
              </fo:block>

              <fo:block font-family="Roboto" font-size="12pt" margin-bottom="12pt">
                "The quick brown fox jumps over the lazy dog."
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="18pt" margin-top="24pt" margin-bottom="12pt" color="#1976D2">
                Liberation Sans Font Sample
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="12pt" margin-bottom="12pt">
                Liberation Sans is a font family which aims at metric compatibility with Arial.
                It is designed for on-screen display and document formatting.
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="10pt" margin-bottom="12pt" color="#666666">
                ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz 0123456789
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="12pt" margin-bottom="12pt">
                "Pack my box with five dozen liquor jugs."
              </fo:block>

              <fo:block font-family="Roboto" font-size="16pt" margin-top="24pt" margin-bottom="12pt" color="#1976D2">
                Font Subsetting Benefits
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="12pt" margin-bottom="6pt">
                • Only characters used in the document are embedded
              </fo:block>
              <fo:block font-family="LiberationSans" font-size="12pt" margin-bottom="6pt">
                • Significantly reduces PDF file size
              </fo:block>
              <fo:block font-family="LiberationSans" font-size="12pt" margin-bottom="6pt">
                • Maintains font quality and metrics
              </fo:block>
              <fo:block font-family="LiberationSans" font-size="12pt" margin-bottom="6pt">
                • Supports text extraction via ToUnicode CMap
              </fo:block>

              <fo:block font-family="Roboto" font-size="12pt" margin-top="24pt" padding="12pt" background-color="#E3F2FD" border="1pt solid #2196F3">
                This PDF was generated with TrueType fonts embedded and subsetted using Folly's font infrastructure.
                All characters are selectable and searchable thanks to the ToUnicode CMap.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    // Render to PDF with TrueType fonts
    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));

    // Configure PDF options with TrueType fonts
    var options = new PdfOptions
    {
        SubsetFonts = true,
        CompressStreams = true,
        Metadata = new PdfMetadata
        {
            Title = "TrueType Font Embedding Demo",
            Author = "Folly PDF Engine",
            Subject = "Demonstration of TrueType font embedding with subsetting",
            Keywords = "TrueType, fonts, PDF, embedding, subsetting"
        }
    };

    // Map font families to TrueType font files
    options.TrueTypeFonts["Roboto"] = robotoPath;
    options.TrueTypeFonts["LiberationSans"] = liberationPath;

    doc.SavePdf(outputPath, options);
}

static void GenerateFontFallbackExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
              <fo:region-body margin="72pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-family="Roboto, Arial, Helvetica, sans-serif" font-size="24pt" text-align="center" margin-bottom="24pt" color="#2196F3">
                Font Fallback &amp; System Font Resolution
              </fo:block>

              <fo:block font-family="Arial, Helvetica, sans-serif" font-size="14pt" margin-bottom="12pt">
                This document demonstrates automatic font resolution with fallback support. When a specific font is not available,
                the system will try alternative fonts in the font-family stack.
              </fo:block>

              <fo:block font-family="sans-serif" font-size="18pt" margin-top="24pt" margin-bottom="12pt" color="#1976D2">
                Generic Font Families
              </fo:block>

              <fo:block font-family="sans-serif" font-size="12pt" margin-bottom="12pt">
                Sans-serif: This text uses the generic "sans-serif" family, which resolves to system fonts like Arial,
                Helvetica, Liberation Sans, or DejaVu Sans depending on what's available on your system.
              </fo:block>

              <fo:block font-family="serif" font-size="12pt" margin-bottom="12pt">
                Serif: This text uses the generic "serif" family, which resolves to system fonts like Times New Roman,
                Times, Liberation Serif, or DejaVu Serif.
              </fo:block>

              <fo:block font-family="monospace" font-size="12pt" margin-bottom="12pt">
                Monospace: This text uses the generic "monospace" family, which resolves to system fonts like Courier New,
                Courier, Liberation Mono, or DejaVu Sans Mono.
              </fo:block>

              <fo:block font-family="Arial, Helvetica, sans-serif" font-size="18pt" margin-top="24pt" margin-bottom="12pt" color="#1976D2">
                Font Family Stacks
              </fo:block>

              <fo:block font-family="Roboto, Arial, Helvetica, sans-serif" font-size="12pt" margin-bottom="12pt">
                This paragraph uses a font stack: "Roboto, Arial, Helvetica, sans-serif". If Roboto is available, it will be used.
                Otherwise, the system falls back to Arial, then Helvetica, then any sans-serif font.
              </fo:block>

              <fo:block font-family="Georgia, Times New Roman, serif" font-size="12pt" margin-bottom="12pt">
                This paragraph uses: "Georgia, Times New Roman, serif". The system will try Georgia first, then Times New Roman,
                then fall back to any serif font.
              </fo:block>

              <fo:block font-family="Consolas, Monaco, Courier New, monospace" font-size="12pt" margin-bottom="12pt">
                This paragraph uses: "Consolas, Monaco, Courier New, monospace". Perfect for code snippets and technical content.
              </fo:block>

              <fo:block font-family="sans-serif" font-size="18pt" margin-top="24pt" margin-bottom="12pt" color="#1976D2">
                How Font Resolution Works
              </fo:block>

              <fo:block font-family="sans-serif" font-size="12pt" margin-bottom="6pt">
                1. Check custom font mappings in PdfOptions.TrueTypeFonts
              </fo:block>
              <fo:block font-family="sans-serif" font-size="12pt" margin-bottom="6pt">
                2. Scan system font directories if EnableFontFallback is true
              </fo:block>
              <fo:block font-family="sans-serif" font-size="12pt" margin-bottom="6pt">
                3. Map generic families (sans-serif, serif, monospace) to common fonts
              </fo:block>
              <fo:block font-family="sans-serif" font-size="12pt" margin-bottom="6pt">
                4. Fall back to PDF base fonts (Helvetica, Times-Roman, Courier) if needed
              </fo:block>

              <fo:block font-family="sans-serif" font-size="18pt" margin-top="24pt" margin-bottom="12pt" color="#1976D2">
                Cross-Platform Support
              </fo:block>

              <fo:block font-family="sans-serif" font-size="12pt" margin-bottom="12pt">
                The font resolver automatically discovers system fonts on Windows, macOS, and Linux:
              </fo:block>

              <fo:block font-family="monospace" font-size="10pt" margin-bottom="6pt" margin-left="12pt">
                • Windows: C:\Windows\Fonts, %LOCALAPPDATA%\Microsoft\Windows\Fonts
              </fo:block>
              <fo:block font-family="monospace" font-size="10pt" margin-bottom="6pt" margin-left="12pt">
                • macOS: /Library/Fonts, /System/Library/Fonts, ~/Library/Fonts
              </fo:block>
              <fo:block font-family="monospace" font-size="10pt" margin-bottom="12pt" margin-left="12pt">
                • Linux: /usr/share/fonts, /usr/local/share/fonts, ~/.fonts, ~/.local/share/fonts
              </fo:block>

              <fo:block font-family="sans-serif" font-size="12pt" margin-top="24pt" padding="12pt" background-color="#E3F2FD" border="1pt solid #2196F3">
                This PDF demonstrates automatic font fallback. Set EnableFontFallback=true in PdfOptions to enable this feature.
                The system will automatically discover and use available system fonts.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    // Render to PDF with font fallback enabled
    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));

    // Configure PDF options with font fallback
    var options = new PdfOptions
    {
        EnableFontFallback = true,  // Enable automatic font resolution
        SubsetFonts = true,
        CompressStreams = true,
        Metadata = new PdfMetadata
        {
            Title = "Font Fallback and System Font Resolution",
            Author = "Folly PDF Engine",
            Subject = "Demonstration of automatic font resolution with fallback support",
            Keywords = "fonts, fallback, system fonts, font stacks, cross-platform"
        }
    };

    doc.SavePdf(outputPath, options);
}

static void GenerateKerningExample(string outputPath, string examplesDir)
{
    // Find test fonts directory
    var testFontsDir = Path.Combine(examplesDir, "..", "tests", "Folly.FontTests", "TestFonts");
    var liberationPath = Path.Combine(testFontsDir, "LiberationSans-Regular.ttf");

    // Check if test fonts exist
    if (!File.Exists(liberationPath))
    {
        Console.WriteLine($"  ⚠ Skipping kerning example - test fonts not found at: {testFontsDir}");
        return;
    }

    var foXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="210mm" page-height="297mm" margin="1in">
              <fo:region-body/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-family="LiberationSans" font-size="28pt" font-weight="bold" color="#1976D2" margin-bottom="24pt">
                Kerning Demonstration
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="14pt" margin-bottom="24pt">
                This document demonstrates automatic kerning support for TrueType fonts. Kerning adjusts
                the spacing between specific character pairs to improve visual appearance and readability.
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="18pt" font-weight="bold" color="#424242" margin-bottom="12pt">
                Common Kerning Pairs
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="14pt" margin-bottom="6pt">
                The following character combinations typically benefit from kerning:
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="48pt" margin-left="24pt" margin-bottom="24pt" color="#E91E63">
                AV WA To Ty Va Yo
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="14pt" margin-bottom="12pt">
                Notice how the letters fit together naturally. Without kerning, there would be awkward gaps between these letter pairs.
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="18pt" font-weight="bold" color="#424242" margin-top="24pt" margin-bottom="12pt">
                Real-World Example
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="36pt" margin-left="24pt" margin-bottom="24pt">
                WAVE Technology
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="36pt" margin-left="24pt" margin-bottom="24pt">
                Type Design
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="36pt" margin-left="24pt" margin-bottom="24pt">
                Professional Typography
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="18pt" font-weight="bold" color="#424242" margin-top="24pt" margin-bottom="12pt">
                How Kerning Works
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="14pt" margin-bottom="12pt">
                Folly automatically applies kerning when:
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="12pt" margin-bottom="6pt" margin-left="18pt">
                • TrueType or OpenType fonts are used (via PdfOptions.TrueTypeFonts)
              </fo:block>
              <fo:block font-family="LiberationSans" font-size="12pt" margin-bottom="6pt" margin-left="18pt">
                • The font contains kerning pair data (kern table)
              </fo:block>
              <fo:block font-family="LiberationSans" font-size="12pt" margin-bottom="12pt" margin-left="18pt">
                • Specific character combinations match defined kerning pairs
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="14pt" margin-top="24pt" padding="12pt" background-color="#E3F2FD" border="1pt solid #2196F3">
                <fo:inline font-weight="bold">Technical Note:</fo:inline> Folly uses the PDF TJ operator to apply kerning
                adjustments character-by-character, ensuring precise spacing according to the font designer's intentions.
                Kerning values are automatically converted from font units to PDF units (1000ths of an em).
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="18pt" font-weight="bold" color="#424242" margin-top="24pt" margin-bottom="12pt">
                More Examples
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="24pt" margin-left="24pt" margin-bottom="12pt">
                PA We Yo Lu ff fi fl
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="24pt" margin-left="24pt" margin-bottom="12pt">
                ATTORNEY LAWSUIT
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="24pt" margin-left="24pt" margin-bottom="24pt">
                Flying Away Together
              </fo:block>

              <fo:block font-family="LiberationSans" font-size="12pt" margin-top="24pt" padding="8pt" background-color="#F5F5F5">
                This PDF demonstrates professional typography with automatic kerning.
                Generated with Folly XSL-FO Processor - TrueType font support with kerning Phase 3.4 complete!
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    // Render to PDF with TrueType font and kerning
    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));

    // Configure PDF options with TrueType fonts
    var options = new PdfOptions
    {
        SubsetFonts = true,
        CompressStreams = true,
        Metadata = new PdfMetadata
        {
            Title = "Kerning Demonstration",
            Author = "Folly PDF Engine",
            Subject = "Automatic kerning with TrueType fonts",
            Keywords = "kerning, typography, TrueType, OpenType, PDF"
        }
    };

    // Map font families to TrueType font files
    options.TrueTypeFonts["LiberationSans"] = liberationPath;

    doc.SavePdf(outputPath, options);
}

static void GenerateRowSpanningExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="210mm" page-height="297mm" margin="1in">
              <fo:region-body/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-family="Helvetica" font-size="28pt" font-weight="bold" color="#1976D2" margin-bottom="24pt">
                Table Row Spanning
              </fo:block>

              <fo:block font-family="Helvetica" font-size="14pt" margin-bottom="24pt">
                This document demonstrates row spanning support in tables, allowing cells to span
                multiple rows for complex layouts.
              </fo:block>

              <!-- Example 1: Basic Row Spanning -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-bottom="12pt">
                Example 1: Basic Row Spanning
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="2pt" margin-bottom="24pt">
                <fo:table-column column-width="80pt"/>
                <fo:table-column column-width="120pt"/>
                <fo:table-column column-width="120pt"/>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell number-rows-spanned="3" background-color="#E3F2FD" padding="8pt">
                      <fo:block font-weight="bold">Spans 3 Rows</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Row 1, Col 2</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Row 1, Col 3</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#FFF9C4">
                      <fo:block>Row 2, Col 2</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#FFF9C4">
                      <fo:block>Row 2, Col 3</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt">
                      <fo:block>Row 3, Col 2</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Row 3, Col 3</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Example 2: Combined Row and Column Spanning -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-top="24pt" margin-bottom="12pt">
                Example 2: Combined Row &amp; Column Spanning
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="2pt" margin-bottom="24pt">
                <fo:table-column column-width="100pt"/>
                <fo:table-column column-width="100pt"/>
                <fo:table-column column-width="120pt"/>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell number-rows-spanned="2" number-columns-spanned="2" background-color="#FFE0B2" padding="8pt">
                      <fo:block font-weight="bold" text-align="center">2×2 Spanning Cell</fo:block>
                      <fo:block margin-top="6pt">Spans 2 rows and 2 columns</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Cell A</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#C8E6C9">
                      <fo:block>Cell B</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt">
                      <fo:block>Cell C</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Cell D</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Cell E</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Example 3: Complex Spanning Layout -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-top="24pt" margin-bottom="12pt">
                Example 3: Complex Spanning Layout
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="2pt" margin-bottom="24pt">
                <fo:table-column column-width="80pt"/>
                <fo:table-column column-width="80pt"/>
                <fo:table-column column-width="80pt"/>
                <fo:table-column column-width="80pt"/>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell number-rows-spanned="3" background-color="#F8BBD0" padding="6pt">
                      <fo:block font-weight="bold">R1C1</fo:block>
                      <fo:block font-size="10pt">(3 rows)</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt">
                      <fo:block>R1C2</fo:block>
                    </fo:table-cell>
                    <fo:table-cell number-rows-spanned="2" background-color="#BBDEFB" padding="6pt">
                      <fo:block font-weight="bold">R1C3</fo:block>
                      <fo:block font-size="10pt">(2 rows)</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt">
                      <fo:block>R1C4</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell number-rows-spanned="2" background-color="#C5E1A5" padding="6pt">
                      <fo:block font-weight="bold">R2C2</fo:block>
                      <fo:block font-size="10pt">(2 rows)</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt">
                      <fo:block>R2C4</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="6pt">
                      <fo:block>R3C3</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt">
                      <fo:block>R3C4</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="6pt">
                      <fo:block>R4C1</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt">
                      <fo:block>R4C2</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt">
                      <fo:block>R4C3</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt">
                      <fo:block>R4C4</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Technical Notes -->
              <fo:block font-family="Helvetica" font-size="14pt" margin-top="24pt" padding="12pt" background-color="#E8F5E9" border="1pt solid #4CAF50">
                <fo:inline font-weight="bold">Implementation Details:</fo:inline> Folly uses a TableCellGrid to track
                cell occupancy across rows and columns. The layout engine performs a two-pass layout: first calculating
                row heights, then rendering cells with proper spanning. This ensures row-spanning cells have the correct
                height across multiple rows.
              </fo:block>

              <fo:block font-family="Helvetica" font-size="12pt" margin-top="24pt" padding="8pt" background-color="#F5F5F5">
                Generated with Folly XSL-FO Processor - Phase 4.1 Complete: Row Spanning Support!
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateProportionalWidthsExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="210mm" page-height="297mm" margin="1in">
              <fo:region-body/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-family="Helvetica" font-size="28pt" font-weight="bold" color="#1976D2" margin-bottom="24pt">
                Proportional Column Widths
              </fo:block>

              <fo:block font-family="Helvetica" font-size="14pt" margin-bottom="24pt">
                This document demonstrates the proportional-column-width() function for flexible table layouts.
                Columns can be sized proportionally, allowing responsive designs that adapt to available space.
              </fo:block>

              <!-- Example 1: Equal Proportions -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-bottom="12pt">
                Example 1: Equal Proportions (1:1:1)
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="2pt" margin-bottom="24pt">
                <fo:table-column column-width="proportional-column-width(1)"/>
                <fo:table-column column-width="proportional-column-width(1)"/>
                <fo:table-column column-width="proportional-column-width(1)"/>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#E3F2FD">
                      <fo:block font-weight="bold">Column 1</fo:block>
                      <fo:block font-size="10pt">1 part</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#E3F2FD">
                      <fo:block font-weight="bold">Column 2</fo:block>
                      <fo:block font-size="10pt">1 part</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#E3F2FD">
                      <fo:block font-weight="bold">Column 3</fo:block>
                      <fo:block font-size="10pt">1 part</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt">
                      <fo:block>Equal width columns</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Each gets 33.3%</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>of available space</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Example 2: Different Proportions -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-top="24pt" margin-bottom="12pt">
                Example 2: Different Proportions (1:2:3)
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="2pt" margin-bottom="24pt">
                <fo:table-column column-width="proportional-column-width(1)"/>
                <fo:table-column column-width="proportional-column-width(2)"/>
                <fo:table-column column-width="proportional-column-width(3)"/>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#FFF9C4">
                      <fo:block font-weight="bold">Small</fo:block>
                      <fo:block font-size="10pt">1 part (16.7%)</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#FFE0B2">
                      <fo:block font-weight="bold">Medium</fo:block>
                      <fo:block font-size="10pt">2 parts (33.3%)</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#FFCCBC">
                      <fo:block font-weight="bold">Large</fo:block>
                      <fo:block font-size="10pt">3 parts (50%)</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt">
                      <fo:block>Narrow</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Wider than first</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Widest column - gets half the space</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Example 3: Mixed Fixed and Proportional -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-top="24pt" margin-bottom="12pt">
                Example 3: Mixed Fixed &amp; Proportional
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="2pt" margin-bottom="24pt">
                <fo:table-column column-width="100pt"/>
                <fo:table-column column-width="proportional-column-width(1)"/>
                <fo:table-column column-width="proportional-column-width(2)"/>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#C8E6C9">
                      <fo:block font-weight="bold">Fixed</fo:block>
                      <fo:block font-size="10pt">100pt</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#B3E5FC">
                      <fo:block font-weight="bold">Prop 1</fo:block>
                      <fo:block font-size="10pt">1 part of remainder</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#B2DFDB">
                      <fo:block font-weight="bold">Prop 2</fo:block>
                      <fo:block font-size="10pt">2 parts of remainder</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt">
                      <fo:block>Always 100pt</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Gets 1/3 of remaining space</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Gets 2/3 of remaining space (twice Column 2)</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Example 4: All Three Types -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-top="24pt" margin-bottom="12pt">
                Example 4: Fixed, Proportional &amp; Auto
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="2pt" margin-bottom="24pt">
                <fo:table-column column-width="80pt"/>
                <fo:table-column column-width="proportional-column-width(2)"/>
                <fo:table-column column-width="auto"/>
                <fo:table-column column-width="proportional-column-width(1)"/>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell padding="6pt" background-color="#F8BBD0">
                      <fo:block font-weight="bold" font-size="10pt">Fixed</fo:block>
                      <fo:block font-size="9pt">80pt</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt" background-color="#E1BEE7">
                      <fo:block font-weight="bold" font-size="10pt">Prop 2</fo:block>
                      <fo:block font-size="9pt">2 parts</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt" background-color="#D1C4E9">
                      <fo:block font-weight="bold" font-size="10pt">Auto</fo:block>
                      <fo:block font-size="9pt">Equal share</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt" background-color="#C5CAE9">
                      <fo:block font-weight="bold" font-size="10pt">Prop 1</fo:block>
                      <fo:block font-size="9pt">1 part</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="6pt">
                      <fo:block font-size="10pt">Fixed 80pt</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt">
                      <fo:block font-size="10pt">2x the proportional</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt">
                      <fo:block font-size="10pt">Auto-sized</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="6pt">
                      <fo:block font-size="10pt">1x proportion</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Technical Notes -->
              <fo:block font-family="Helvetica" font-size="14pt" margin-top="24pt" padding="12pt" background-color="#FFF3E0" border="1pt solid #FF9800">
                <fo:inline font-weight="bold">How It Works:</fo:inline> Folly calculates column widths in two passes.
                First, fixed-width columns are allocated their space. Then, the remaining width is distributed
                proportionally based on the ratios specified in proportional-column-width(). Auto columns share
                the remaining space equally.
              </fo:block>

              <fo:block font-family="Helvetica" font-size="12pt" margin-top="24pt" padding="8pt" background-color="#F5F5F5">
                Generated with Folly XSL-FO Processor - Phase 4.2 Complete: Proportional Column Widths!
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateContentBasedSizingExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="210mm" page-height="297mm">
              <fo:region-body margin="1in"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <!-- Title -->
              <fo:block font-family="Helvetica" font-size="24pt" font-weight="bold" text-align="center" color="#1976D2" margin-bottom="24pt">
                Content-Based Column Sizing
              </fo:block>

              <fo:block font-family="Helvetica" font-size="12pt" margin-bottom="24pt" padding="12pt" background-color="#E3F2FD" border="1pt solid #2196F3">
                <fo:inline font-weight="bold">Phase 4.3 Feature:</fo:inline> Auto columns now intelligently size based on content width,
                distributing space proportionally to the longest word in each column. This creates balanced, readable tables
                that adapt to your data.
              </fo:block>

              <!-- Example 1: Basic Content-Based Sizing -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-bottom="12pt">
                Example 1: Auto Columns with Different Content
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="8pt" color="#666666">
                Three auto columns distribute space based on content width. Notice how the column with the longest
                word gets more space.
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="2pt" margin-bottom="24pt">
                <fo:table-column column-width="auto"/>
                <fo:table-column column-width="auto"/>
                <fo:table-column column-width="auto"/>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#FFE0B2" border="1pt solid #FF9800">
                      <fo:block font-weight="bold">Short</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#FFF9C4" border="1pt solid #FBC02D">
                      <fo:block font-weight="bold">Medium Length</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#C8E6C9" border="1pt solid #4CAF50">
                      <fo:block font-weight="bold">VeryLongColumnTitle</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt">
                      <fo:block>A</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Content here</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>LongContentWordHere</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt">
                      <fo:block>B</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>More text</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>ExtraLongContent</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Example 2: Mixed Column Types -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-top="24pt" margin-bottom="12pt">
                Example 2: Auto Columns with Fixed Width
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="8pt" color="#666666">
                Combining fixed-width columns with auto columns. The auto columns share the remaining space
                based on their content requirements.
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="2pt" margin-bottom="24pt">
                <fo:table-column column-width="100pt"/>
                <fo:table-column column-width="auto"/>
                <fo:table-column column-width="auto"/>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#D1C4E9" border="1pt solid #673AB7">
                      <fo:block font-weight="bold">Fixed 100pt</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#B3E5FC" border="1pt solid #03A9F4">
                      <fo:block font-weight="bold">Auto Short</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#F8BBD0" border="1pt solid #E91E63">
                      <fo:block font-weight="bold">Auto WithLongerContent</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt">
                      <fo:block>Always 100pt</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>Small</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt">
                      <fo:block>LongerDescription</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Example 3: Product Catalog -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-top="24pt" margin-bottom="12pt">
                Example 3: Product Catalog (Real-World Use Case)
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="8pt" color="#666666">
                A practical example showing how content-based sizing creates readable tables for product catalogs.
                Each column automatically sizes to fit its content optimally.
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="0pt" margin-bottom="24pt">
                <fo:table-column column-width="auto"/>
                <fo:table-column column-width="auto"/>
                <fo:table-column column-width="auto"/>
                <fo:table-column column-width="auto"/>
                <fo:table-header>
                  <fo:table-row>
                    <fo:table-cell padding="10pt" background-color="#37474F" border="1pt solid #263238">
                      <fo:block font-weight="bold" color="white">SKU</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="10pt" background-color="#37474F" border="1pt solid #263238">
                      <fo:block font-weight="bold" color="white">Product</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="10pt" background-color="#37474F" border="1pt solid #263238">
                      <fo:block font-weight="bold" color="white">Category</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="10pt" background-color="#37474F" border="1pt solid #263238">
                      <fo:block font-weight="bold" color="white">Price</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-header>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" border="1pt solid #E0E0E0">
                      <fo:block>A101</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border="1pt solid #E0E0E0">
                      <fo:block>UltraWidescreenMonitor</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border="1pt solid #E0E0E0">
                      <fo:block>Electronics</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border="1pt solid #E0E0E0">
                      <fo:block>$499</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#FAFAFA" border="1pt solid #E0E0E0">
                      <fo:block>B205</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#FAFAFA" border="1pt solid #E0E0E0">
                      <fo:block>Keyboard</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#FAFAFA" border="1pt solid #E0E0E0">
                      <fo:block>Accessories</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#FAFAFA" border="1pt solid #E0E0E0">
                      <fo:block>$79</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" border="1pt solid #E0E0E0">
                      <fo:block>C312</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border="1pt solid #E0E0E0">
                      <fo:block>Mouse</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border="1pt solid #E0E0E0">
                      <fo:block>Accessories</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" border="1pt solid #E0E0E0">
                      <fo:block>$29</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#FAFAFA" border="1pt solid #E0E0E0">
                      <fo:block>D418</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#FAFAFA" border="1pt solid #E0E0E0">
                      <fo:block>ErgoStandingDesk</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#FAFAFA" border="1pt solid #E0E0E0">
                      <fo:block>Furniture</fo:block>
                    </fo:table-cell>
                    <fo:table-cell padding="8pt" background-color="#FAFAFA" border="1pt solid #E0E0E0">
                      <fo:block>$799</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Technical Notes -->
              <fo:block font-family="Helvetica" font-size="14pt" margin-top="24pt" padding="12pt" background-color="#FFF3E0" border="1pt solid #FF9800">
                <fo:inline font-weight="bold">How It Works:</fo:inline> Folly measures the longest word in each column
                across all rows to determine content requirements. Auto columns then share the available space
                proportionally based on these measurements. This creates balanced tables that adapt to your data
                while preventing overflow and ensuring readability.
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-top="12pt" padding="8pt" background-color="#E8F5E9" border="1pt solid #4CAF50">
                <fo:inline font-weight="bold">World-Class Feature:</fo:inline> Unlike simple equal distribution,
                content-based sizing intelligently allocates space where it's needed most, resulting in professional,
                publication-quality tables.
              </fo:block>

              <fo:block font-family="Helvetica" font-size="12pt" margin-top="24pt" padding="8pt" background-color="#F5F5F5">
                Generated with Folly XSL-FO Processor - Phase 4.3 Complete: Content-Based Column Sizing!
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateFooterRepetitionExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="210mm" page-height="120mm">
              <fo:region-body margin="20pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>
          <fo:page-sequence master-reference="A4">
            <fo:flow flow-name="xsl-region-body">
              <!-- Title -->
              <fo:block font-family="Helvetica" font-size="24pt" font-weight="bold" text-align="center" color="#1976D2" margin-bottom="24pt">
                Table Footer Repetition
              </fo:block>

              <fo:block font-family="Helvetica" font-size="12pt" margin-bottom="24pt" padding="12pt" background-color="#E3F2FD" border="1pt solid #2196F3">
                <fo:inline font-weight="bold">Phase 4.4 Feature:</fo:inline> Table footers now repeat at page breaks by default.
                Use <fo:inline font-family="Courier">table-omit-footer-at-break="true"</fo:inline> to show footers only at the table end.
              </fo:block>

              <!-- Example 1: Footer Repetition Enabled (Default) -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-bottom="12pt">
                Example 1: Footer Repetition (Default Behavior)
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="8pt" color="#666666">
                This table spans multiple pages. Notice the footer appears on every page.
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="2pt" margin-bottom="24pt">
                <fo:table-header>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#37474F">
                      <fo:block font-weight="bold" color="white">Page Header</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-header>
                <fo:table-footer>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#FFF3E0" border="1pt solid #FF9800">
                      <fo:block font-weight="bold" color="#E65100">Footer Repeats on Every Page</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-footer>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell padding="35pt"><fo:block>Row 1: This table has tall rows</fo:block></fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="35pt"><fo:block>Row 2: Causing page breaks</fo:block></fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="35pt"><fo:block>Row 3: Footer appears above</fo:block></fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="35pt"><fo:block>Row 4: On every page</fo:block></fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="35pt"><fo:block>Row 5: Final row</fo:block></fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Example 2: Footer Repetition Disabled -->
              <fo:block font-family="Helvetica" font-size="18pt" font-weight="bold" color="#424242" margin-top="24pt" margin-bottom="12pt">
                Example 2: Footer Only at End
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="8pt" color="#666666">
                Using <fo:inline font-family="Courier">table-omit-footer-at-break="true"</fo:inline> shows the footer
                only after the last row.
              </fo:block>

              <fo:table border="1pt solid black" border-collapse="separate" border-spacing="2pt" table-omit-footer-at-break="true" margin-bottom="24pt">
                <fo:table-header>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#37474F">
                      <fo:block font-weight="bold" color="white">Page Header</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-header>
                <fo:table-footer>
                  <fo:table-row>
                    <fo:table-cell padding="8pt" background-color="#E8F5E9" border="1pt solid #4CAF50">
                      <fo:block font-weight="bold" color="#2E7D32">Footer Only at Table End</fo:block>
                    </fo:table-cell>
                  </fo:table-row>
                </fo:table-footer>
                <fo:table-body>
                  <fo:table-row>
                    <fo:table-cell padding="35pt"><fo:block>Row 1: Footer omitted at breaks</fo:block></fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="35pt"><fo:block>Row 2: No footer above</fo:block></fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="35pt"><fo:block>Row 3: Still no footer</fo:block></fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="35pt"><fo:block>Row 4: Almost there</fo:block></fo:table-cell>
                  </fo:table-row>
                  <fo:table-row>
                    <fo:table-cell padding="35pt"><fo:block>Row 5: Footer appears after this</fo:block></fo:table-cell>
                  </fo:table-row>
                </fo:table-body>
              </fo:table>

              <!-- Technical Notes -->
              <fo:block font-family="Helvetica" font-size="14pt" margin-top="24pt" padding="12pt" background-color="#FFF3E0" border="1pt solid #FF9800">
                <fo:inline font-weight="bold">How It Works:</fo:inline> By default, footers repeat at every page break
                to provide context and summary information on each page (like headers). Set
                <fo:inline font-family="Courier">table-omit-footer-at-break="true"</fo:inline> when you want totals
                or concluding information to appear only once at the end.
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-top="12pt" padding="8pt" background-color="#E8F5E9" border="1pt solid #4CAF50">
                <fo:inline font-weight="bold">Use Cases:</fo:inline> Footer repetition is perfect for running totals,
                page references, or disclaimers that should appear on every page. Omit at break for final totals,
                signatures, or one-time summary information.
              </fo:block>

              <fo:block font-family="Helvetica" font-size="12pt" margin-top="24pt" padding="8pt" background-color="#F5F5F5">
                Generated with Folly XSL-FO Processor - Phase 4.4 Complete: Table Footer Repetition!
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateLetterheadExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="letterhead" page-width="8.5in" page-height="11in"
              margin-top="0.5in" margin-bottom="0.5in" margin-left="0.75in" margin-right="0.75in">
              <fo:region-body margin-top="1.25in" margin-bottom="1in"/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="letterhead">
            <fo:flow flow-name="xsl-region-body">
              <!-- Company Logo/Name - Absolutely Positioned Top Left -->
              <fo:block-container absolute-position="absolute" top="0.5in" left="0.75in"
                width="3in" height="0.75in">
                <fo:block font-family="Helvetica" font-size="24pt" font-weight="bold" color="#1976D2">
                  ACME Corporation
                </fo:block>
                <fo:block font-family="Helvetica" font-size="10pt" color="#666666" margin-top="2pt">
                  Excellence in Innovation
                </fo:block>
              </fo:block-container>

              <!-- Company Address - Absolutely Positioned Top Right -->
              <fo:block-container absolute-position="absolute" top="0.5in" right="0.75in"
                width="2.5in" height="0.75in">
                <fo:block font-family="Helvetica" font-size="9pt" text-align="end" color="#333333">
                  123 Business Ave, Suite 100
                </fo:block>
                <fo:block font-family="Helvetica" font-size="9pt" text-align="end" color="#333333">
                  San Francisco, CA 94102
                </fo:block>
                <fo:block font-family="Helvetica" font-size="9pt" text-align="end" color="#333333" margin-top="4pt">
                  Tel: (415) 555-0123
                </fo:block>
                <fo:block font-family="Helvetica" font-size="9pt" text-align="end" color="#1976D2">
                  www.acmecorp.example
                </fo:block>
              </fo:block-container>

              <!-- Decorative Header Line - Absolutely Positioned -->
              <fo:block-container absolute-position="absolute" top="1.4in" left="0.75in"
                width="7in" height="2pt" background-color="#1976D2"/>

              <!-- Footer - Absolutely Positioned at Bottom -->
              <fo:block-container absolute-position="absolute" bottom="0.5in" left="0.75in"
                width="7in" height="0.5in" border-before-width="1pt" border-before-style="solid"
                border-before-color="#CCCCCC" padding-before="8pt">
                <fo:block font-family="Helvetica" font-size="8pt" text-align="center" color="#666666">
                  ACME Corporation | Registered in Delaware | Company No. 12345678 | VAT No. US987654321
                </fo:block>
                <fo:block font-family="Helvetica" font-size="8pt" text-align="center" color="#666666" margin-top="2pt">
                  This communication is confidential and may contain privileged information.
                </fo:block>
              </fo:block-container>

              <!-- Letter Content - Normal Flow -->
              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="12pt">
                January 15, 2025
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="18pt">
                Mr. John Smith<fo:block/>
                Director of Operations<fo:block/>
                Global Industries Inc.<fo:block/>
                456 Corporate Blvd<fo:block/>
                New York, NY 10001
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" font-weight="bold" margin-bottom="12pt">
                RE: Partnership Proposal for Q1 2025
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="12pt">
                Dear Mr. Smith,
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="12pt" text-align="justify">
                I am writing to express ACME Corporation's strong interest in establishing a strategic
                partnership with Global Industries Inc. Our research indicates significant synergies
                between our organizations, particularly in the areas of supply chain optimization and
                sustainable manufacturing practices.
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="12pt" text-align="justify">
                ACME Corporation has been a leader in innovative business solutions for over 25 years.
                We believe a partnership would create substantial value through joint development of
                next-generation technologies, shared R&amp;D initiatives, and market expansion.
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="12pt" text-align="justify">
                I would welcome the opportunity to discuss this proposal. Please contact me at
                john.doe@acmecorp.example or at the number above.
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="12pt">
                I look forward to hearing from you soon.
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-bottom="6pt" margin-top="18pt">
                Sincerely,
              </fo:block>

              <fo:block font-family="Helvetica" font-size="11pt" margin-top="36pt" font-weight="bold">
                Jane Doe
              </fo:block>
              <fo:block font-family="Helvetica" font-size="11pt">
                Chief Executive Officer
              </fo:block>
              <fo:block font-family="Helvetica" font-size="11pt" color="#1976D2">
                ACME Corporation
              </fo:block>

              <!-- Demonstration info box -->
              <fo:block font-family="Helvetica" font-size="10pt" margin-top="36pt" padding="12pt"
                background-color="#E3F2FD" border="1pt solid #1976D2">
                <fo:inline font-weight="bold">Absolute Positioning Demo:</fo:inline> This letterhead uses
                fo:block-container with absolute-position="absolute" to place the company header, address,
                decorative line, and footer at fixed positions on the page. The letter content flows normally
                in the body area, unaffected by the absolutely positioned elements.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void GenerateSidebarsExample(string outputPath)
{
    var foXml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <!-- Layout Master Set with side regions -->
          <fo:layout-master-set>
            <fo:simple-page-master master-name="left-margin-notes" page-width="8.5in" page-height="11in"
              margin-top="0.75in" margin-bottom="0.75in" margin-left="0.5in" margin-right="1in">
              <!-- Body region with left margin for sidebar -->
              <fo:region-body margin-left="2.5in" margin-right="0.5in"/>
              <!-- Left sidebar for margin notes -->
              <fo:region-start extent="2in"/>
              <!-- Header and footer -->
              <fo:region-before extent="0.5in"/>
              <fo:region-after extent="0.5in"/>
            </fo:simple-page-master>

            <fo:simple-page-master master-name="both-sidebars" page-width="8.5in" page-height="11in"
              margin-top="0.75in" margin-bottom="0.75in" margin-left="0.5in" margin-right="0.5in">
              <!-- Body region with both side margins -->
              <fo:region-body margin-left="1.5in" margin-right="1.5in"/>
              <!-- Left sidebar -->
              <fo:region-start extent="1.25in"/>
              <!-- Right sidebar -->
              <fo:region-end extent="1.25in"/>
              <!-- Header and footer -->
              <fo:region-before extent="0.5in"/>
              <fo:region-after extent="0.5in"/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <!-- Page Sequence 1: Left margin notes (academic style) -->
          <fo:page-sequence master-reference="left-margin-notes">
            <fo:static-content flow-name="xsl-region-before">
              <fo:block font-size="10pt" text-align="center" border-bottom="0.5pt solid black" padding-bottom="6pt">
                Academic Paper with Margin Notes
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-after">
              <fo:block font-size="9pt" text-align="center" border-top="0.5pt solid black" padding-top="6pt">
                Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-start">
              <!-- Margin note 1 -->
              <fo:block font-size="8pt" font-style="italic" color="#666666"
                margin-bottom="18pt" space-after="12pt">
                <fo:inline font-weight="bold" color="#000000">Definition:</fo:inline>
                The term "region-start" refers to the left sidebar in left-to-right writing modes,
                or the right sidebar in right-to-left modes.
              </fo:block>

              <!-- Margin note 2 -->
              <fo:block font-size="8pt" font-style="italic" color="#666666"
                margin-bottom="18pt" space-after="12pt">
                <fo:inline font-weight="bold" color="#000000">Historical Note:</fo:inline>
                XSL-FO was developed by the W3C and became a recommendation in 2001.
                It provides precise control over page layout for print and PDF output.
              </fo:block>

              <!-- Margin note 3 -->
              <fo:block font-size="8pt" font-style="italic" color="#666666"
                margin-bottom="18pt" space-after="12pt">
                <fo:inline font-weight="bold" color="#000000">See Also:</fo:inline>
                Chapter 5 discusses multi-column layouts and how they interact with side regions.
              </fo:block>

              <!-- Margin note 4 -->
              <fo:block font-size="8pt" font-style="italic" color="#666666">
                <fo:inline font-weight="bold" color="#000000">Important:</fo:inline>
                Side regions are ideal for annotations, glossary terms, cross-references,
                and supplementary information that shouldn't interrupt the main text flow.
              </fo:block>
            </fo:static-content>

            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="18pt" font-weight="bold" space-after="12pt">
                Understanding XSL-FO Side Regions
              </fo:block>

              <fo:block font-size="12pt" text-align="justify" space-after="12pt">
                XSL-FO provides powerful layout capabilities through its region model. In addition to
                the standard region-before (header), region-after (footer), and region-body (main content),
                XSL-FO supports region-start and region-end for left and right sidebars respectively.
              </fo:block>

              <fo:block font-size="12pt" text-align="justify" space-after="12pt">
                These side regions are particularly useful in academic papers, technical documentation,
                and annotated texts where supplementary information needs to be visible alongside the
                main content without interrupting the reading flow.
              </fo:block>

              <fo:block font-size="12pt" text-align="justify" space-after="12pt">
                The key advantage of using side regions over other layout techniques is that they
                maintain a clean separation between the main content and supplementary material. The
                region-body automatically adjusts its width to accommodate the side regions, ensuring
                proper text flow and preventing overlap.
              </fo:block>

              <fo:block font-size="14pt" font-weight="bold" space-before="18pt" space-after="12pt">
                Implementation Details
              </fo:block>

              <fo:block font-size="12pt" text-align="justify" space-after="12pt">
                To implement side regions, you need to configure three components: the simple-page-master
                with region-start and/or region-end elements, the region-body with appropriate margins,
                and static-content sections that target the xsl-region-start and xsl-region-end flow names.
              </fo:block>

              <fo:block font-size="12pt" text-align="justify" space-after="12pt">
                The extent attribute on region-start and region-end specifies the width of each sidebar.
                The region-body's margin-left and margin-right should account for these extents plus any
                desired spacing between the regions.
              </fo:block>

              <fo:block font-size="12pt" text-align="justify">
                This flexibility allows for a wide range of layout designs, from minimal margin notes
                to full-width sidebars containing complex content like tables, lists, or images.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>

          <!-- Page Sequence 2: Both sidebars (magazine style) -->
          <fo:page-sequence master-reference="both-sidebars">
            <fo:static-content flow-name="xsl-region-before">
              <fo:block font-size="10pt" text-align="center" border-bottom="0.5pt solid black" padding-bottom="6pt">
                Magazine Layout with Both Sidebars
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-after">
              <fo:block font-size="9pt" text-align="center" border-top="0.5pt solid black" padding-top="6pt">
                Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-start">
              <!-- Left sidebar content -->
              <fo:block font-size="9pt" font-weight="bold" color="#003366"
                border="1pt solid #003366" padding="6pt" background-color="#E6F2FF"
                margin-bottom="12pt">
                Quick Facts
              </fo:block>

              <fo:block font-size="8pt" margin-bottom="12pt">
                <fo:block font-weight="bold" space-after="3pt">• Year Established</fo:block>
                <fo:block margin-left="12pt">2001 (W3C Recommendation)</fo:block>
              </fo:block>

              <fo:block font-size="8pt" margin-bottom="12pt">
                <fo:block font-weight="bold" space-after="3pt">• Current Version</fo:block>
                <fo:block margin-left="12pt">XSL-FO 1.1</fo:block>
              </fo:block>

              <fo:block font-size="8pt">
                <fo:block font-weight="bold" space-after="3pt">• Primary Use</fo:block>
                <fo:block margin-left="12pt">Professional document formatting and PDF generation</fo:block>
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-end">
              <!-- Right sidebar content -->
              <fo:block font-size="9pt" font-weight="bold" color="#006633"
                border="1pt solid #006633" padding="6pt" background-color="#E6FFE6"
                margin-bottom="12pt">
                Related Topics
              </fo:block>

              <fo:block font-size="8pt" margin-bottom="8pt">
                → Multi-column layouts
              </fo:block>

              <fo:block font-size="8pt" margin-bottom="8pt">
                → Absolute positioning
              </fo:block>

              <fo:block font-size="8pt" margin-bottom="8pt">
                → Conditional page masters
              </fo:block>

              <fo:block font-size="8pt" margin-bottom="12pt">
                → Flow mapping
              </fo:block>

              <fo:block font-size="8pt" font-style="italic" color="#666666"
                border-top="0.5pt solid #CCCCCC" padding-top="6pt">
                For more information, consult the XSL-FO 1.1 specification from the W3C.
              </fo:block>
            </fo:static-content>

            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="16pt" font-weight="bold" space-after="12pt" text-align="center">
                Advanced Page Layout with Side Regions
              </fo:block>

              <fo:block font-size="11pt" text-align="justify" space-after="10pt">
                This page demonstrates the use of both region-start (left sidebar) and region-end
                (right sidebar) simultaneously. This layout pattern is commonly seen in magazines,
                textbooks, and reference materials where supplementary information is displayed
                in the margins on both sides of the main text.
              </fo:block>

              <fo:block font-size="11pt" text-align="justify" space-after="10pt">
                The left sidebar typically contains quick reference information, definitions, or
                key facts, while the right sidebar often includes related topics, cross-references,
                or callout boxes. The main content flows naturally in the center column.
              </fo:block>

              <fo:block font-size="11pt" text-align="justify" space-after="10pt">
                Each sidebar can contain any valid XSL-FO content, including styled blocks, lists,
                tables, images, and borders. The styling is independent from the main body,
                allowing for visual distinction through different fonts, colors, or backgrounds.
              </fo:block>

              <fo:block font-size="11pt" text-align="justify" space-after="10pt">
                This three-column layout (left sidebar, main body, right sidebar) creates a
                professional appearance and maximizes the use of page space while maintaining
                excellent readability. The main content remains focused and uncluttered, while
                the sidebars provide context and additional depth.
              </fo:block>

              <fo:block font-size="11pt" text-align="justify">
                Side regions are static-content, meaning they remain consistent across pages
                (unless you use conditional page masters). This makes them ideal for persistent
                navigation aids, glossaries, or supplementary information that should appear
                on every page of a section.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}
static void GenerateImageFormatsExample(string outputPath, string examplesDir)
{
    // Create test images directory
    var testImagesDir = Path.Combine(examplesDir, "test-images");
    Directory.CreateDirectory(testImagesDir);

    // Generate test images for each format
    var bmpPath = Path.Combine(testImagesDir, "test.bmp");
    var gifPath = Path.Combine(testImagesDir, "test.gif");
    var tiffPath = Path.Combine(testImagesDir, "test.tif");

    CreateTestBmp(bmpPath);
    CreateTestGif(gifPath);
    CreateTestTiff(tiffPath);

    var foXml = $"""
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt"
                                   margin-top="36pt" margin-bottom="36pt"
                                   margin-left="54pt" margin-right="54pt">
              <fo:region-body margin-top="36pt" margin-bottom="36pt"/>
              <fo:region-before extent="36pt"/>
              <fo:region-after extent="24pt"/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="A4">
            <fo:static-content flow-name="xsl-region-before">
              <fo:block font-family="Helvetica" font-size="10pt" text-align="center"
                        border-bottom-width="0.5pt" border-bottom-style="solid"
                        border-bottom-color="#cccccc" padding-bottom="6pt">
                Folly Image Format Support Demonstration
              </fo:block>
            </fo:static-content>

            <fo:static-content flow-name="xsl-region-after">
              <fo:block font-family="Helvetica" font-size="9pt" text-align="center" color="#666666">
                Example 33: All Image Formats - Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <fo:flow flow-name="xsl-region-body">
              <fo:block font-family="Helvetica" font-size="24pt" font-weight="bold"
                        text-align="center" space-after="18pt" color="#2c3e50">
                Image Format Support
              </fo:block>

              <fo:block font-family="Times" font-size="11pt" text-align="justify"
                        space-after="18pt" line-height="1.5">
                Folly supports five major image formats with zero external dependencies.
                All parsers are custom-built in .NET for maximum portability and security.
              </fo:block>

              <fo:block font-family="Helvetica" font-size="16pt" font-weight="bold"
                        color="#34495e" space-before="18pt" space-after="12pt">
                BMP • GIF • TIFF
              </fo:block>

              <fo:block space-after="12pt">
                <fo:external-graphic src="{bmpPath}" content-width="48pt" content-height="48pt"/>
                <fo:inline> </fo:inline>
                <fo:external-graphic src="{gifPath}" content-width="48pt" content-height="48pt"/>
                <fo:inline> </fo:inline>
                <fo:external-graphic src="{tiffPath}" content-width="48pt" content-height="48pt"/>
              </fo:block>

              <fo:block font-family="Times" font-size="10pt" space-after="24pt">
                Test images demonstrating BMP (24-bit gradient), GIF (LZW-compressed indexed color),
                and TIFF (uncompressed RGB) successfully embedded and rendered.
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
    doc.SavePdf(outputPath);
}

static void CreateTestBmp(string path)
{
    int width = 64, height = 64;
    int rowStride = ((width * 3 + 3) / 4) * 4;
    int fileSize = 54 + rowStride * height;
    var bmp = new byte[fileSize];

    bmp[0] = 0x42; bmp[1] = 0x4D;
    WriteInt32LE(bmp, 2, fileSize);
    WriteInt32LE(bmp, 10, 54);
    WriteInt32LE(bmp, 14, 40);
    WriteInt32LE(bmp, 18, width);
    WriteInt32LE(bmp, 22, height);
    WriteInt16LE(bmp, 26, 1);
    WriteInt16LE(bmp, 28, 24);

    for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int offset = 54 + y * rowStride + x * 3;
            bmp[offset] = (byte)(x * 255 / width);
            bmp[offset + 1] = (byte)(y * 255 / height);
            bmp[offset + 2] = (byte)((x + y) * 255 / (width + height));
        }

    File.WriteAllBytes(path, bmp);
}

static void CreateTestGif(string path)
{
    var gif = new List<byte>();
    gif.AddRange(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 });
    gif.Add(64); gif.Add(0);
    gif.Add(64); gif.Add(0);
    gif.Add(0xF3); gif.Add(0); gif.Add(0);

    for (int i = 0; i < 16; i++)
    {
        gif.Add((byte)((i * 17) % 256));
        gif.Add((byte)((i * 31) % 256));
        gif.Add((byte)((i * 47) % 256));
    }

    gif.Add(0x2C);
    gif.AddRange(new byte[] { 0, 0, 0, 0 });
    gif.Add(64); gif.Add(0);
    gif.Add(64); gif.Add(0);
    gif.Add(0);
    gif.Add(0x04);

    var lzwData = EncodeLzwPattern(64 * 64, 4);
    int off = 0;
    while (off < lzwData.Length)
    {
        int sz = Math.Min(255, lzwData.Length - off);
        gif.Add((byte)sz);
        for (int i = 0; i < sz; i++) gif.Add(lzwData[off++]);
    }

    gif.Add(0); gif.Add(0x3B);
    File.WriteAllBytes(path, gif.ToArray());
}

static void CreateTestTiff(string path)
{
    int w = 64, h = 64;
    var tiff = new List<byte>();

    tiff.AddRange(new byte[] { 0x49, 0x49, 0x2A, 0 });
    WriteUInt32LEList(tiff, 8);
    WriteUInt16LEList(tiff, 8);

    WriteTiffEntry(tiff, 256, 3, 1, (uint)w);
    WriteTiffEntry(tiff, 257, 3, 1, (uint)h);
    WriteTiffEntry(tiff, 258, 3, 1, 8);
    WriteTiffEntry(tiff, 259, 3, 1, 1);
    WriteTiffEntry(tiff, 262, 3, 1, 2);
    WriteTiffEntry(tiff, 273, 4, 1, 106);
    WriteTiffEntry(tiff, 277, 3, 1, 3);
    WriteTiffEntry(tiff, 279, 4, 1, (uint)(w * h * 3));

    WriteUInt32LEList(tiff, 0);

    for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            tiff.Add((byte)((x * 255) / w));
            tiff.Add((byte)((y * 255) / h));
            tiff.Add((byte)(((x ^ y) * 255) / w));
        }

    File.WriteAllBytes(path, tiff.ToArray());
}

static byte[] EncodeLzwPattern(int pixels, int minCodeSize)
{
    int clearCode = 1 << minCodeSize;
    int eoiCode = clearCode + 1;
    int codeSize = minCodeSize + 1;

    var bits = new List<int> { clearCode };
    for (int i = 0; i < pixels; i++) bits.Add(i % 16);
    bits.Add(eoiCode);

    var bytes = new List<byte>();
    int currentByte = 0, bitPosition = 0;

    foreach (int code in bits)
    {
        for (int i = 0; i < codeSize; i++)
        {
            currentByte |= ((code >> i) & 1) << bitPosition++;
            if (bitPosition == 8)
            {
                bytes.Add((byte)currentByte);
                currentByte = 0;
                bitPosition = 0;
            }
        }
    }

    if (bitPosition > 0) bytes.Add((byte)currentByte);
    return bytes.ToArray();
}

static void WriteInt32LE(byte[] d, int o, int v) { d[o] = (byte)v; d[o+1] = (byte)(v >> 8); d[o+2] = (byte)(v >> 16); d[o+3] = (byte)(v >> 24); }
static void WriteInt16LE(byte[] d, int o, int v) { d[o] = (byte)v; d[o+1] = (byte)(v >> 8); }
static void WriteUInt16LEList(List<byte> d, int v) { d.Add((byte)v); d.Add((byte)(v >> 8)); }
static void WriteUInt32LEList(List<byte> d, uint v) { d.Add((byte)v); d.Add((byte)(v >> 8)); d.Add((byte)(v >> 16)); d.Add((byte)(v >> 24)); }
static void WriteTiffEntry(List<byte> d, ushort t, ushort y, uint c, uint v) { WriteUInt16LEList(d, t); WriteUInt16LEList(d, y); WriteUInt32LEList(d, c); WriteUInt32LEList(d, v); }

static void GenerateRoundedCornersExample(string outputPath)
{
    var xml = """
        <?xml version="1.0"?>
        <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
          <fo:layout-master-set>
            <fo:simple-page-master master-name="A4" page-width="8.5in" page-height="11in"
                                   margin-top="0.75in" margin-bottom="0.75in"
                                   margin-left="0.75in" margin-right="0.75in">
              <fo:region-body margin-top="0.5in" margin-bottom="0.5in"/>
              <fo:region-before extent="0.5in"/>
              <fo:region-after extent="0.5in"/>
            </fo:simple-page-master>
          </fo:layout-master-set>

          <fo:page-sequence master-reference="A4">
            <!-- Header -->
            <fo:static-content flow-name="xsl-region-before">
              <fo:block text-align="center" font-size="10pt" color="#666666" border-bottom="0.5pt solid #CCCCCC" padding-bottom="8pt">
                Example 34: Rounded Corners (Border Radius) - Page <fo:page-number/>
              </fo:block>
            </fo:static-content>

            <!-- Main Content -->
            <fo:flow flow-name="xsl-region-body">
              <fo:block font-size="24pt" font-weight="bold" space-after="12pt" text-align="center">
                Rounded Corners (Border Radius)
              </fo:block>

              <fo:block font-size="11pt" space-after="18pt" text-align="center" color="#555555">
                Demonstration of border-radius property for smooth, rounded corners
              </fo:block>

              <!-- Uniform Radius -->
              <fo:block font-size="14pt" font-weight="bold" space-before="18pt" space-after="12pt" color="#2C5282">
                1. Uniform Border Radius
              </fo:block>

              <fo:block border="3pt solid #2C5282" border-radius="10pt" padding="16pt"
                        background-color="#EBF8FF" space-after="12pt">
                <fo:block font-weight="bold" space-after="6pt">Uniform 10pt radius on all corners</fo:block>
                <fo:block>This block uses border-radius="10pt" to create smooth, equally rounded corners
                on all four sides. Perfect for modern, clean design.</fo:block>
              </fo:block>

              <fo:block border="2pt solid #38A169" border-radius="20pt" padding="14pt"
                        background-color="#F0FFF4" space-after="12pt">
                <fo:block font-weight="bold" space-after="6pt">Larger 20pt radius</fo:block>
                <fo:block>Increasing the radius to 20pt creates more pronounced curves. This is great for
                callout boxes and highlighted content.</fo:block>
              </fo:block>

              <!-- Individual Corner Radii -->
              <fo:block font-size="14pt" font-weight="bold" space-before="18pt" space-after="12pt" color="#2C5282">
                2. Individual Corner Radii
              </fo:block>

              <fo:block border="3pt solid #D97706"
                        border-top-left-radius="5pt"
                        border-top-right-radius="15pt"
                        border-bottom-right-radius="25pt"
                        border-bottom-left-radius="35pt"
                        padding="14pt" background-color="#FFFAF0" space-after="12pt">
                <fo:block font-weight="bold" space-after="6pt">Different radius per corner</fo:block>
                <fo:block>Top-left: 5pt, Top-right: 15pt, Bottom-right: 25pt, Bottom-left: 35pt</fo:block>
              </fo:block>

              <!--  Border Styles -->
              <fo:block font-size="14pt" font-weight="bold" space-before="18pt" space-after="12pt" color="#2C5282">
                3. Rounded Corners with Different Border Styles
              </fo:block>

              <fo:block border="2pt solid #9333EA" border-radius="12pt" padding="12pt"
                        background-color="#FAF5FF" space-after="12pt">
                <fo:block font-weight="bold">Solid Border</fo:block>
              </fo:block>

              <fo:block border="2pt dashed #DC2626" border-radius="12pt" padding="12pt"
                        background-color="#FEF2F2" space-after="12pt">
                <fo:block font-weight="bold">Dashed Border</fo:block>
              </fo:block>

              <fo:block border="2pt dotted #0891B2" border-radius="12pt" padding="12pt"
                        background-color="#ECFEFF" space-after="12pt">
                <fo:block font-weight="bold">Dotted Border</fo:block>
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

    using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
    using var doc = FoDocument.Load(stream);
    using var output = File.Create(outputPath);
    doc.SavePdf(output);
}
