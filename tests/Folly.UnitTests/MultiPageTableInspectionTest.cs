using Folly;
using Folly.Testing;
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

        // Inspect page 2 specifically (where the corrupted text appears)
        var output = new System.Text.StringBuilder();
        output.AppendLine("=== PAGE 2 INSPECTION ===");
        output.AppendLine($"Total pages: {areaTree.Pages.Count}");

        if (areaTree.Pages.Count >= 2)
        {
            var page2 = areaTree.Pages[1]; // 0-indexed
            output.AppendLine($"\nPage 2 dimensions: {page2.Width} x {page2.Height}");
            output.AppendLine($"Page 2 area count: {page2.Areas.Count}");

            // Use the query API to inspect the first table on page 2
            var query = areaTree.Query().Page(1);
            var tables = page2.Areas.OfType<TableArea>().ToList();

            output.AppendLine($"\nTables on page 2: {tables.Count}");

            if (tables.Count > 0)
            {
                var firstTable = tables[0];
                output.AppendLine($"\nFirst table: {firstTable.Rows.Count} rows");

                // Look at the first few rows in detail
                for (int rowIdx = 0; rowIdx < Math.Min(3, firstTable.Rows.Count); rowIdx++)
                {
                    var row = firstTable.Rows[rowIdx];
                    output.AppendLine($"\n  Row {rowIdx}: {row.Cells.Count} cells, Height={row.Height:F2}");

                    // Check each cell
                    for (int cellIdx = 0; cellIdx < row.Cells.Count; cellIdx++)
                    {
                        var cell = row.Cells[cellIdx];
                        output.AppendLine($"    Cell {cellIdx}: {cell.Children.Count} children");

                        // Look for blocks and their lines
                        foreach (var child in cell.Children)
                        {
                            if (child is BlockArea block)
                            {
                                output.AppendLine($"      Block: {block.Children.Count} children (lines)");

                                foreach (var lineChild in block.Children)
                                {
                                    if (lineChild is LineArea line)
                                    {
                                        output.AppendLine($"        Line: {line.Inlines.Count} inlines, Y={line.Y:F2}");

                                        foreach (var inline in line.Inlines)
                                        {
                                            output.AppendLine($"          Inline: '{inline.Text}' at Y={inline.Y:F2}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        Console.WriteLine(output.ToString());

        // Save JSON for detailed inspection
        var json = AreaTreeInspector.ToJson(areaTree, new AreaTreeSerializationOptions
        {
            IncludeTypography = true,
            IncludeTextContent = true,
            IncludeContent = true,
            IncludeSpacing = true
        });
        File.WriteAllText("/tmp/multipage_table_inspection.json", json);
        Console.WriteLine("\nArea tree JSON saved to: /tmp/multipage_table_inspection.json");
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
