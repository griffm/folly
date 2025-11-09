using System.Text;
using Folly;

namespace Folly.Benchmarks;

/// <summary>
/// Generates test documents of varying complexity for benchmarking.
/// </summary>
internal static class TestDocumentGenerator
{
    /// <summary>
    /// Generates a simple text document with the specified number of pages.
    /// </summary>
    public static FoDocument GenerateSimpleDocument(int pageCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<fo:root xmlns:fo=\"http://www.w3.org/1999/XSL/Format\">");
        sb.AppendLine("  <fo:layout-master-set>");
        sb.AppendLine("    <fo:simple-page-master master-name=\"A4\" page-width=\"595pt\" page-height=\"842pt\">");
        sb.AppendLine("      <fo:region-body margin=\"72pt\"/>");
        sb.AppendLine("    </fo:simple-page-master>");
        sb.AppendLine("  </fo:layout-master-set>");
        sb.AppendLine("  <fo:page-sequence master-reference=\"A4\">");
        sb.AppendLine("    <fo:flow flow-name=\"xsl-region-body\">");

        for (int i = 0; i < pageCount; i++)
        {
            // Page title
            sb.AppendLine($"      <fo:block font-size=\"24pt\" font-weight=\"bold\" space-after=\"12pt\">");
            sb.AppendLine($"        Page {i + 1}");
            sb.AppendLine("      </fo:block>");

            // Add 40 lines of text per page
            for (int line = 0; line < 40; line++)
            {
                sb.AppendLine("      <fo:block font-size=\"12pt\" space-after=\"2pt\">");
                sb.AppendLine($"        This is line {line + 1} of page {i + 1}. Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
                sb.AppendLine("      </fo:block>");
            }

            // Page break except on last page
            if (i < pageCount - 1)
            {
                sb.AppendLine("      <fo:block break-after=\"page\"/>");
            }
        }

        sb.AppendLine("    </fo:flow>");
        sb.AppendLine("  </fo:page-sequence>");
        sb.AppendLine("</fo:root>");

        return FoDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    /// <summary>
    /// Generates a mixed document with tables, images, and formatted text.
    /// </summary>
    public static FoDocument GenerateMixedDocument(int pageCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<fo:root xmlns:fo=\"http://www.w3.org/1999/XSL/Format\">");
        sb.AppendLine("  <fo:layout-master-set>");
        sb.AppendLine("    <fo:simple-page-master master-name=\"A4\" page-width=\"595pt\" page-height=\"842pt\">");
        sb.AppendLine("      <fo:region-body margin=\"72pt\"/>");
        sb.AppendLine("      <fo:region-before extent=\"36pt\"/>");
        sb.AppendLine("      <fo:region-after extent=\"36pt\"/>");
        sb.AppendLine("    </fo:simple-page-master>");
        sb.AppendLine("  </fo:layout-master-set>");
        sb.AppendLine("  <fo:page-sequence master-reference=\"A4\">");

        // Header
        sb.AppendLine("    <fo:static-content flow-name=\"xsl-region-before\">");
        sb.AppendLine("      <fo:block font-size=\"10pt\" text-align=\"center\" border-bottom=\"solid 1pt #000000\">");
        sb.AppendLine("        Performance Test Document");
        sb.AppendLine("      </fo:block>");
        sb.AppendLine("    </fo:static-content>");

        // Footer
        sb.AppendLine("    <fo:static-content flow-name=\"xsl-region-after\">");
        sb.AppendLine("      <fo:block font-size=\"10pt\" text-align=\"center\">");
        sb.AppendLine("        <fo:page-number/>");
        sb.AppendLine("      </fo:block>");
        sb.AppendLine("    </fo:static-content>");

        sb.AppendLine("    <fo:flow flow-name=\"xsl-region-body\">");

        for (int page = 0; page < pageCount; page++)
        {
            // Title
            sb.AppendLine($"      <fo:block font-size=\"20pt\" font-weight=\"bold\" space-after=\"12pt\">");
            sb.AppendLine($"        Section {page + 1}");
            sb.AppendLine("      </fo:block>");

            // Paragraph
            sb.AppendLine("      <fo:block font-size=\"12pt\" space-after=\"12pt\">");
            sb.AppendLine("        Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.");
            sb.AppendLine("      </fo:block>");

            // Table every other page
            if (page % 2 == 0)
            {
                sb.AppendLine("      <fo:table border-collapse=\"collapse\" space-after=\"12pt\">");
                sb.AppendLine("        <fo:table-column column-width=\"150pt\"/>");
                sb.AppendLine("        <fo:table-column column-width=\"150pt\"/>");
                sb.AppendLine("        <fo:table-column column-width=\"150pt\"/>");
                sb.AppendLine("        <fo:table-header>");
                sb.AppendLine("          <fo:table-row>");
                sb.AppendLine("            <fo:table-cell border=\"solid 1pt #000000\" padding=\"4pt\">");
                sb.AppendLine("              <fo:block font-weight=\"bold\">Column 1</fo:block>");
                sb.AppendLine("            </fo:table-cell>");
                sb.AppendLine("            <fo:table-cell border=\"solid 1pt #000000\" padding=\"4pt\">");
                sb.AppendLine("              <fo:block font-weight=\"bold\">Column 2</fo:block>");
                sb.AppendLine("            </fo:table-cell>");
                sb.AppendLine("            <fo:table-cell border=\"solid 1pt #000000\" padding=\"4pt\">");
                sb.AppendLine("              <fo:block font-weight=\"bold\">Column 3</fo:block>");
                sb.AppendLine("            </fo:table-cell>");
                sb.AppendLine("          </fo:table-row>");
                sb.AppendLine("        </fo:table-header>");
                sb.AppendLine("        <fo:table-body>");

                for (int row = 0; row < 10; row++)
                {
                    sb.AppendLine("          <fo:table-row>");
                    sb.AppendLine("            <fo:table-cell border=\"solid 1pt #000000\" padding=\"4pt\">");
                    sb.AppendLine($"              <fo:block>Data {row},1</fo:block>");
                    sb.AppendLine("            </fo:table-cell>");
                    sb.AppendLine("            <fo:table-cell border=\"solid 1pt #000000\" padding=\"4pt\">");
                    sb.AppendLine($"              <fo:block>Data {row},2</fo:block>");
                    sb.AppendLine("            </fo:table-cell>");
                    sb.AppendLine("            <fo:table-cell border=\"solid 1pt #000000\" padding=\"4pt\">");
                    sb.AppendLine($"              <fo:block>Data {row},3</fo:block>");
                    sb.AppendLine("            </fo:table-cell>");
                    sb.AppendLine("          </fo:table-row>");
                }

                sb.AppendLine("        </fo:table-body>");
                sb.AppendLine("      </fo:table>");
            }

            // List
            sb.AppendLine("      <fo:block font-weight=\"bold\" space-after=\"6pt\">Key Points:</fo:block>");
            for (int i = 1; i <= 5; i++)
            {
                sb.AppendLine("      <fo:list-block provisional-distance-between-starts=\"24pt\" provisional-label-separation=\"6pt\" space-after=\"4pt\">");
                sb.AppendLine("        <fo:list-item>");
                sb.AppendLine("          <fo:list-item-label end-indent=\"label-end()\">");
                sb.AppendLine($"            <fo:block>{i}.</fo:block>");
                sb.AppendLine("          </fo:list-item-label>");
                sb.AppendLine("          <fo:list-item-body start-indent=\"body-start()\">");
                sb.AppendLine($"            <fo:block>Important point number {i} about this topic.</fo:block>");
                sb.AppendLine("          </fo:list-item-body>");
                sb.AppendLine("        </fo:list-item>");
                sb.AppendLine("      </fo:list-block>");
            }

            // More text
            sb.AppendLine("      <fo:block font-size=\"12pt\" space-after=\"12pt\">");
            sb.AppendLine("        Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.");
            sb.AppendLine("      </fo:block>");

            // Page break except on last page
            if (page < pageCount - 1)
            {
                sb.AppendLine("      <fo:block break-after=\"page\"/>");
            }
        }

        sb.AppendLine("    </fo:flow>");
        sb.AppendLine("  </fo:page-sequence>");
        sb.AppendLine("</fo:root>");

        return FoDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    /// <summary>
    /// Generates a table-heavy document for stress testing.
    /// </summary>
    public static FoDocument GenerateTableDocument(int pageCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<fo:root xmlns:fo=\"http://www.w3.org/1999/XSL/Format\">");
        sb.AppendLine("  <fo:layout-master-set>");
        sb.AppendLine("    <fo:simple-page-master master-name=\"A4\" page-width=\"595pt\" page-height=\"842pt\">");
        sb.AppendLine("      <fo:region-body margin=\"36pt\"/>");
        sb.AppendLine("    </fo:simple-page-master>");
        sb.AppendLine("  </fo:layout-master-set>");
        sb.AppendLine("  <fo:page-sequence master-reference=\"A4\">");
        sb.AppendLine("    <fo:flow flow-name=\"xsl-region-body\">");

        int tablesPerPage = 3;
        for (int i = 0; i < pageCount * tablesPerPage; i++)
        {
            sb.AppendLine($"      <fo:block font-weight=\"bold\" space-after=\"6pt\">Table {i + 1}</fo:block>");
            sb.AppendLine("      <fo:table border-collapse=\"collapse\" space-after=\"12pt\">");
            sb.AppendLine("        <fo:table-column column-width=\"100pt\"/>");
            sb.AppendLine("        <fo:table-column column-width=\"100pt\"/>");
            sb.AppendLine("        <fo:table-column column-width=\"100pt\"/>");
            sb.AppendLine("        <fo:table-column column-width=\"100pt\"/>");
            sb.AppendLine("        <fo:table-column column-width=\"100pt\"/>");
            sb.AppendLine("        <fo:table-body>");

            for (int row = 0; row < 15; row++)
            {
                sb.AppendLine("          <fo:table-row>");
                for (int col = 1; col <= 5; col++)
                {
                    sb.AppendLine("            <fo:table-cell border=\"solid 1pt #000000\" padding=\"2pt\">");
                    sb.AppendLine($"              <fo:block>R{row}C{col}</fo:block>");
                    sb.AppendLine("            </fo:table-cell>");
                }
                sb.AppendLine("          </fo:table-row>");
            }

            sb.AppendLine("        </fo:table-body>");
            sb.AppendLine("      </fo:table>");
        }

        sb.AppendLine("    </fo:flow>");
        sb.AppendLine("  </fo:page-sequence>");
        sb.AppendLine("</fo:root>");

        return FoDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())));
    }
}
