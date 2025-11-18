using Folly;
using Folly.Layout.Testing;
using Xunit;
using System;
using System.IO;
using System.Linq;

namespace Folly.UnitTests;

public class MultiPageTableInspectionTest
{
    [Fact]
    public void InspectMultiPageTable_PageBreakIssue()
    {
        // Generate the multi-page table example
        var tableRows = new System.Text.StringBuilder();
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
                <fo:simple-page-master master-name="A4" page-width="595pt" page-height="842pt">
                  <fo:region-body margin="72pt"/>
                  <fo:region-before extent="36pt"/>
                  <fo:region-after extent="36pt"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:static-content flow-name="xsl-region-before">
                  <fo:block font-size="10pt" font-family="Helvetica" text-align="end" padding-bottom="6pt" border-bottom-width="0.5pt" border-bottom-style="solid" border-bottom-color="black">
                    Page <fo:page-number/> - Multi-Page Table Example
                  </fo:block>
                </fo:static-content>
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
                  <fo:table border-collapse="separate" border-spacing="0pt" space-before="12pt">
                    <fo:table-column column-width="100pt"/>
                    <fo:table-column column-width="120pt"/>
                    <fo:table-column column-width="80pt"/>
                    <fo:table-column column-width="100pt"/>
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
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        // Inspect ALL pages to check spacing and footer issues
        var output = new System.Text.StringBuilder();
        output.AppendLine("=== MULTI-PAGE TABLE LAYOUT INSPECTION ===");
        output.AppendLine($"Total pages: {areaTree.Pages.Count}");

        for (int pageIdx = 0; pageIdx < Math.Min(4, areaTree.Pages.Count); pageIdx++)
        {
            var page = areaTree.Pages[pageIdx];
            output.AppendLine($"\n=== PAGE {pageIdx + 1} ===");
            output.AppendLine($"Page dimensions: {page.Width} x {page.Height}");
            output.AppendLine($"Total areas: {page.Areas.Count}");

            // Group areas by type
            var tables = page.Areas.OfType<TableArea>().ToList();
            var blocks = page.Areas.OfType<BlockArea>().ToList();

            output.AppendLine($"  Tables: {tables.Count}");
            output.AppendLine($"  Blocks: {blocks.Count}");

            // Check the first few areas to see their positions
            output.AppendLine("\n  First 5 areas:");
            for (int i = 0; i < Math.Min(5, page.Areas.Count); i++)
            {
                var area = page.Areas[i];
                var areaType = area.GetType().Name;
                output.AppendLine($"    {i}: {areaType} at Y={area.Y:F2}, Height={area.Height:F2}");
            }

            // Check if there's a large gap before the first table
            if (tables.Count > 0)
            {
                var firstTable = tables[0];
                output.AppendLine($"\n  First table position: Y={firstTable.Y:F2}");
                output.AppendLine($"  Body margin top (expected): 72pt (header) + margins");
            }

            // Check the last area to see if it overlaps the footer region
            if (page.Areas.Count > 0)
            {
                // Find the footer BlockArea (should be at Y=806 for region-after extent=36pt)
                var footerBlock = page.Areas.OfType<BlockArea>().FirstOrDefault(b => b.Y > 800);
                var footerTop = footerBlock?.Y ?? (page.Height - 36); // Default to page height - region-after extent

                // Find the last content area (excluding header/footer)
                var lastContentArea = page.Areas
                    .Where(a => a.Y > 50 && a.Y < footerTop) // Exclude header (near Y=0) and footer
                    .OrderByDescending(a => a.Y + a.Height)
                    .FirstOrDefault();

                if (lastContentArea != null)
                {
                    var lastAreaBottom = lastContentArea.Y + lastContentArea.Height;
                    var availableBodyHeight = footerTop - 108; // Body starts at Y=108

                    output.AppendLine($"\n  Last content area: {lastContentArea.GetType().Name}");
                    output.AppendLine($"    Y={lastContentArea.Y:F2}, Height={lastContentArea.Height:F2}");
                    output.AppendLine($"    Bottom at: {lastAreaBottom:F2}");
                    output.AppendLine($"  Footer region starts at: {footerTop:F2}");
                    output.AppendLine($"  Available body height: {availableBodyHeight:F2}");

                    var gapToFooter = footerTop - lastAreaBottom;
                    if (gapToFooter < 0)
                    {
                        output.AppendLine($"  ⚠ OVERFLOW: Content extends {-gapToFooter:F2}pt into footer region!");
                    }
                    else
                    {
                        output.AppendLine($"  Gap to footer: {gapToFooter:F2}pt");
                    }
                }
            }
        }

        // Console.WriteLine(output.ToString());

        // DETAILED: Check every single table row on page 2 to see where they actually are
        if (areaTree.Pages.Count >= 2)
        {
            var page2 = areaTree.Pages[1];
            var allTables = page2.Areas.OfType<TableArea>().ToList();

            output.Clear();
            output.AppendLine("\n=== DETAILED ROW POSITIONS ON PAGE 2 ===");
            output.AppendLine($"Page height: 842pt, Footer at: 806pt, Should stop at: ~734pt");
            output.AppendLine($"Total tables: {allTables.Count}\n");

            int rowNum = 0;
            int tableNum = 0;
            foreach (var table in allTables)
            {
                output.AppendLine($"  Table {tableNum++} at Y={table.Y:F2}:");
                foreach (var row in table.Rows)
                {
                    var absoluteRowY = table.Y + row.Y;
                    var rowBottom = absoluteRowY + row.Height;
                    var firstCellText = row.Cells.Count > 0 && row.Cells[0].Children.Count > 0
                        ? ExtractTextFromArea(row.Cells[0].Children[0])
                        : "";

                    output.AppendLine($"    Row {rowNum++}: absY={absoluteRowY:F2}, relY={row.Y:F2}, H={row.Height:F2}, Bottom={rowBottom:F2} - '{firstCellText}'");

                    if (rowBottom > 806)
                    {
                        output.AppendLine($"      ⚠ WARNING: Row extends {rowBottom - 806:F2}pt past footer!");
                    }
                    else if (rowBottom > 734)
                    {
                        output.AppendLine($"      ⚠ Past pageBottom ({rowBottom - 734:F2}pt over)");
                    }
                }
            }

            // Console.WriteLine(output.ToString());
        }

        // Save JSON for detailed inspection
        var json = AreaTreeInspector.ToJson(areaTree, new AreaTreeSerializationOptions
        {
            IncludeTypography = true,
            IncludeTextContent = true,
            IncludeContent = true,
            IncludeSpacing = true
        });
        File.WriteAllText("/tmp/multipage_table_inspection.json", json);
        // Console.WriteLine("\nArea tree JSON saved to: /tmp/multipage_table_inspection.json");
    }

    private static string ExtractTextFromArea(Area area)
    {
        if (area is BlockArea block)
        {
            var text = new System.Text.StringBuilder();
            foreach (var child in block.Children)
            {
                if (child is LineArea line)
                {
                    foreach (var inline in line.Inlines)
                    {
                        text.Append(inline.Text);
                    }
                }
            }
            return text.ToString();
        }
        return "";
    }

    private void InspectAreaPosition(Area area, System.Text.StringBuilder output, int depth, string context)
    {
        var indent = new string(' ', depth * 2);

        if (area is BlockArea block)
        {
            // Check for unusual positions
            if (block.Y < 0 || block.Y > 900)
            {
                output.AppendLine($"{indent}⚠ UNUSUAL Y: BlockArea at Y={block.Y:F2}, X={block.X:F2}, W={block.Width:F2}, H={block.Height:F2}");
            }

            foreach (var child in block.Children)
            {
                InspectAreaPosition(child, output, depth + 1, context);
            }
        }
        else if (area is LineArea line)
        {
            if (line.Y < 0 || line.Y > 900)
            {
                output.AppendLine($"{indent}⚠ UNUSUAL Y: LineArea at Y={line.Y:F2}, X={line.X:F2}");
                foreach (var inline in line.Inlines)
                {
                    output.AppendLine($"{indent}  Text: '{inline.Text}'");
                }
            }
        }
        else if (area is TableArea table)
        {
            if (table.Y < 0 || table.Y > 900)
            {
                output.AppendLine($"{indent}⚠ UNUSUAL Y: TableArea at Y={table.Y:F2}, X={table.X:F2}");
            }

            foreach (var row in table.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    foreach (var child in cell.Children)
                    {
                        InspectAreaPosition(child, output, depth + 1, context);
                    }
                }
            }
        }
    }
}
