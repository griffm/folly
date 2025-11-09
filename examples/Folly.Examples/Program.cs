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
