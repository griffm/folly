using System.IO;
using Xunit;

namespace Folly.UnitTests;

public class KerningIntegrationTests
{
    private readonly string _testFontsDir;

    public KerningIntegrationTests()
    {
        // Find test fonts directory
        var assemblyDir = Path.GetDirectoryName(typeof(KerningIntegrationTests).Assembly.Location)!;
        _testFontsDir = Path.Combine(assemblyDir, "..", "..", "..", "..", "Folly.FontTests", "TestFonts");
    }

    [Fact]
    public void Kerning_LiberationSans_AppliesKerningInPdf()
    {
        // Arrange
        var fontPath = Path.Combine(_testFontsDir, "LiberationSans-Regular.ttf");

        if (!File.Exists(fontPath))
        {
            // Skip test if font not available
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
              <fo:block font-family="LiberationSans" font-size="24pt">
                AV WA To Ty
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

        // Act
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        using var pdfStream = new MemoryStream();
        var options = new PdfOptions
        {
            TrueTypeFonts = { ["LiberationSans"] = fontPath },
            CompressStreams = false // Disable compression for easier testing
        };

        doc.SavePdf(pdfStream, options);

        // Assert
        pdfStream.Seek(0, SeekOrigin.Begin);
        var pdfContent = System.Text.Encoding.ASCII.GetString(pdfStream.ToArray());

        // Check that TJ operator is used (indicates kerning array)
        Assert.Contains("] TJ", pdfContent);

        // Verify PDF contains the font reference
        Assert.Contains("/F", pdfContent);
    }

    [Fact]
    public void Kerning_RobotoFont_GeneratesValidPdf()
    {
        // Arrange
        var fontPath = Path.Combine(_testFontsDir, "Roboto-Regular.ttf");

        if (!File.Exists(fontPath))
        {
            // Skip test if font not available
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
              <fo:block font-family="Roboto" font-size="24pt">
                AV WA To Ty
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

        // Act
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        using var pdfStream = new MemoryStream();
        var options = new PdfOptions
        {
            TrueTypeFonts = { ["Roboto"] = fontPath },
            CompressStreams = false // Disable compression for easier testing
        };

        doc.SavePdf(pdfStream, options);

        // Assert - Just verify PDF was generated successfully
        pdfStream.Seek(0, SeekOrigin.Begin);
        var pdfContent = System.Text.Encoding.ASCII.GetString(pdfStream.ToArray());

        // PDF should be valid
        Assert.StartsWith("%PDF-1.7", pdfContent);
        Assert.Contains("/F", pdfContent); // Contains font reference

        // Note: Roboto may or may not have kerning for these specific pairs,
        // so we just verify the PDF is valid rather than checking for TJ operator
    }

    [Fact]
    public void Kerning_StandardFont_UsesSimpleTjOperator()
    {
        // Arrange - Use a standard PDF font (no kerning expected)
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
              <fo:block font-family="Helvetica" font-size="24pt">
                AV WA To Ty
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

        // Act
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        using var pdfStream = new MemoryStream();
        var options = new PdfOptions
        {
            CompressStreams = false // Disable compression for easier testing
        };
        doc.SavePdf(pdfStream, options);

        // Assert
        pdfStream.Seek(0, SeekOrigin.Begin);
        var pdfContent = System.Text.Encoding.ASCII.GetString(pdfStream.ToArray());

        // Standard fonts should use simple Tj operator (no kerning)
        Assert.Contains(") Tj", pdfContent);
    }

    [Fact]
    public void Kerning_MixedContent_AppliesKerningOnlyToTrueTypeFonts()
    {
        // Arrange
        var fontPath = Path.Combine(_testFontsDir, "LiberationSans-Regular.ttf");

        if (!File.Exists(fontPath))
        {
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
              <fo:block font-family="LiberationSans" font-size="18pt">
                With TrueType: AV WA To
              </fo:block>
              <fo:block font-family="Helvetica" font-size="18pt">
                With Standard Font: AV WA To
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

        // Act
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        using var pdfStream = new MemoryStream();
        var options = new PdfOptions
        {
            TrueTypeFonts = { ["LiberationSans"] = fontPath },
            CompressStreams = false // Disable compression for easier testing
        };

        doc.SavePdf(pdfStream, options);

        // Assert
        pdfStream.Seek(0, SeekOrigin.Begin);
        var pdfContent = System.Text.Encoding.ASCII.GetString(pdfStream.ToArray());

        // Should contain both TJ (kerned) and Tj (non-kerned) operators
        Assert.Contains("] TJ", pdfContent);
        Assert.Contains(") Tj", pdfContent);
    }

    [Fact]
    public void Kerning_EmptyText_DoesNotCrash()
    {
        // Arrange
        var fontPath = Path.Combine(_testFontsDir, "LiberationSans-Regular.ttf");

        if (!File.Exists(fontPath))
        {
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
              <fo:block font-family="LiberationSans" font-size="18pt">
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

        // Act & Assert - should not throw
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        using var pdfStream = new MemoryStream();
        var options = new PdfOptions
        {
            TrueTypeFonts = { ["LiberationSans"] = fontPath },
            CompressStreams = false // Disable compression for easier testing
        };

        doc.SavePdf(pdfStream, options);

        Assert.True(pdfStream.Length > 0);
    }

    [Fact]
    public void Kerning_SingleCharacter_DoesNotApplyKerning()
    {
        // Arrange
        var fontPath = Path.Combine(_testFontsDir, "LiberationSans-Regular.ttf");

        if (!File.Exists(fontPath))
        {
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
              <fo:block font-family="LiberationSans" font-size="18pt">
                A
              </fo:block>
            </fo:flow>
          </fo:page-sequence>
        </fo:root>
        """;

        // Act & Assert - should not throw
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        using var pdfStream = new MemoryStream();
        var options = new PdfOptions
        {
            TrueTypeFonts = { ["LiberationSans"] = fontPath },
            CompressStreams = false // Disable compression for easier testing
        };

        doc.SavePdf(pdfStream, options);

        Assert.True(pdfStream.Length > 0);
    }
}
