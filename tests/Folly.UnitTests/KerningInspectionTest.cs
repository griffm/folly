using Folly.Testing;

namespace Folly.UnitTests;

/// <summary>
/// Inspection test to diagnose kerning and letter spacing issues in Example 26.
/// </summary>
public class KerningInspectionTest
{
    [Fact]
    public void InspectKerningExample_LetterSpacing()
    {
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body margin="20mm"/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block font-family="LiberationSans" font-size="48pt">
                    AV WA To Yo
                  </fo:block>
                  <fo:block font-family="LiberationSans" font-size="14pt" margin-top="20pt">
                    This is normal text with kerning pairs
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var doc = FoDocument.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml)));
        var areaTree = doc.BuildAreaTree();

        // Inspect the first block (large kerning pairs)
        var block1 = areaTree.Query().FirstPage().Block(0);
        var line1 = block1.Line(0);

        var output = new System.Text.StringBuilder();
        output.AppendLine("=== KERNING INSPECTION ===");
        output.AppendLine($"Block: Font={block1.FontFamily}, Size={block1.FontSize}");
        output.AppendLine($"Line: Width={line1.Width:F2}, Height={line1.Height:F2}");
        output.AppendLine($"Inline count: {line1.InlineCount}");
        output.AppendLine();

        var inlines = line1.Inlines().ToList();
        for (int i = 0; i < inlines.Count; i++)
        {
            var inline = inlines[i];
            output.AppendLine($"Inline {i}: Text='{inline.Text}', X={inline.X:F2}, Width={inline.Width:F2}, WordSpacing={inline.WordSpacing}");
        }

        output.AppendLine();
        output.AppendLine("=== SECOND BLOCK ===");
        var block2 = areaTree.Query().FirstPage().Block(1);
        var line2 = block2.Line(0);
        output.AppendLine($"Inline count: {line2.InlineCount}");

        var inlines2 = line2.Inlines().Take(10).ToList();
        for (int i = 0; i < inlines2.Count; i++)
        {
            var inline = inlines2[i];
            output.AppendLine($"Inline {i}: Text='{inline.Text}', Width={inline.Width:F2}");
        }

        // Write to console for inspection
        Console.WriteLine(output.ToString());

        // Also save JSON
        var json = AreaTreeInspector.ToJson(areaTree, new AreaTreeSerializationOptions
        {
            IncludeTypography = true,
            IncludeTextContent = true,
            IncludeContent = true,
            IncludeSpacing = true
        });

        // Save to test output directory
        var jsonPath = "/tmp/kerning_inspection.json";
        File.WriteAllText(jsonPath, json);
        Console.WriteLine($"\nArea tree JSON saved to: {jsonPath}");

        // This test always passes - it's just for inspection
        Assert.True(true);
    }
}
