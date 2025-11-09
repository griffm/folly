using Folly;
using Folly.Pdf;

Console.WriteLine("Folly XSL-FO to PDF Examples");
Console.WriteLine("=============================\n");

// Create output directory
var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
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
                This example demonstrates XSL-FO keep and break constraints for pagination control.
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

              <fo:block font-size="12pt" margin-bottom="12pt">
                These pagination controls give you precise control over document layout and ensure professional formatting.
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
