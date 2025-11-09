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
