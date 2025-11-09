using Xunit;
using Folly.Fluent;
using System.IO;

namespace Folly.UnitTests;

public class FluentApiTests
{
    [Fact]
    public void FluentApi_SimpleDocument_ShouldCreatePdf()
    {
        // Arrange
        var outputPath = Path.GetTempFileName() + ".pdf";

        try
        {
            // Act
            var doc = Fo.Document();
            doc.Metadata(meta => meta
                .Title("Fluent API Test")
                .Author("Test Author"));
            doc.LayoutMasters(lm => lm
                .SimplePageMaster("A4", "210mm", "297mm", spm => spm
                    .RegionBody(rb => rb.Margin("1in"))));
            doc.PageSequence("A4", ps => ps
                .Flow(flow => flow
                    .Block("Hello, World!")));
            doc.SavePdf(outputPath);

            // Assert
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void FluentApi_DocumentWithHeaderAndFooter_ShouldCreatePdf()
    {
        // Arrange
        var outputPath = Path.GetTempFileName() + ".pdf";

        try
        {
            // Act
            var doc = Fo.Document();
            doc.Metadata(meta => meta
                .Title("Document with Header and Footer")
                .Author("Test Author"));
            doc.LayoutMasters(lm => lm
                .SimplePageMaster("A4", "210mm", "297mm", spm => spm
                    .RegionBody(rb => rb.Margin("1in", "0.5in", "1in", "0.5in"))
                    .RegionBefore("0.5in")
                    .RegionAfter("0.5in")));
            doc.PageSequence("A4", ps => ps
                .StaticContent("xsl-region-before", sc => sc
                    .Block(b => b
                        .Text("Header Content")
                        .TextAlign("center")
                        .FontSize("10pt")))
                .StaticContent("xsl-region-after", sc => sc
                    .Block(b => b
                        .Text("Footer Content")
                        .TextAlign("center")
                        .FontSize("10pt")))
                .Flow(flow => flow
                    .Block(b => b
                        .Text("Main Content")
                        .FontSize("12pt"))));
            doc.SavePdf(outputPath);

            // Assert
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void FluentApi_DocumentWithStyledBlocks_ShouldCreatePdf()
    {
        // Arrange
        var outputPath = Path.GetTempFileName() + ".pdf";

        try
        {
            // Act
            var doc = Fo.Document();
            doc.Metadata(meta => meta.Title("Styled Blocks Test"));
            doc.LayoutMasters(lm => lm
                .SimplePageMaster("A4", "210mm", "297mm", spm => spm
                    .RegionBody(rb => rb.Margin("1in"))));
            doc.PageSequence("A4", ps => ps
                .Flow(flow => flow
                    .Block(b => b
                        .Text("This is a heading")
                        .FontSize("24pt")
                        .FontFamily("Helvetica")
                        .Margin("0pt", "0pt", "12pt", "0pt"))
                    .Block(b => b
                        .Text("This is a paragraph with custom styling.")
                        .FontSize("12pt")
                        .FontFamily("Times")
                        .LineHeight("18pt")
                        .TextAlign("justify")
                        .Margin("0pt", "0pt", "12pt", "0pt"))
                    .Block(b => b
                        .Text("This is a highlighted block.")
                        .FontSize("12pt")
                        .BackgroundColor("#FFFF00")
                        .Padding("10pt")
                        .Border("2pt", "solid", "#FF0000"))));
            doc.SavePdf(outputPath);

            // Assert
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void FluentApi_SaveToStream_ShouldCreatePdf()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var doc = Fo.Document();
        doc.Metadata(meta => meta.Title("Stream Test"));
        doc.LayoutMasters(lm => lm
            .SimplePageMaster("A4", "210mm", "297mm", spm => spm
                .RegionBody(rb => rb.Margin("1in"))));
        doc.PageSequence("A4", ps => ps
            .Flow(flow => flow
                .Block("Content saved to stream")));
        doc.SavePdf(stream);

        // Assert
        Assert.True(stream.Length > 0);
        Assert.True(stream.Position > 0);
    }

    [Fact]
    public void FluentApi_DocumentWithMultiplePageSequences_ShouldCreatePdf()
    {
        // Arrange
        var outputPath = Path.GetTempFileName() + ".pdf";

        try
        {
            // Act
            var doc = Fo.Document();
            doc.Metadata(meta => meta.Title("Multiple Page Sequences Test"));
            doc.LayoutMasters(lm => lm
                .SimplePageMaster("A4", "210mm", "297mm", spm => spm
                    .RegionBody(rb => rb.Margin("1in"))));
            doc.PageSequence("A4", ps => ps
                .Flow(flow => flow
                    .Block(b => b
                        .Text("First Page Sequence")
                        .FontSize("18pt"))));
            doc.PageSequence("A4", ps => ps
                .Flow(flow => flow
                    .Block(b => b
                        .Text("Second Page Sequence")
                        .FontSize("18pt"))));
            doc.SavePdf(outputPath);

            // Assert
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
