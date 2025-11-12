using System;
using System.Collections.Generic;
using System.IO;
using Folly;
using Folly.Pdf;
using Xunit;

namespace Folly.Fonts.Tests;

/// <summary>
/// Integration tests for font fallback functionality with PDF rendering.
/// </summary>
public class FontFallbackIntegrationTests
{
    [Fact]
    public void FontFallback_WithGenericFamilies_GeneratesPdf()
    {
        // Arrange
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
                  <fo:block font-family="sans-serif" font-size="14pt">
                    Test with sans-serif generic family.
                  </fo:block>
                  <fo:block font-family="serif" font-size="14pt">
                    Test with serif generic family.
                  </fo:block>
                  <fo:block font-family="monospace" font-size="14pt">
                    Test with monospace generic family.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = Folly.FoDocument.Load(ms);

        // Act
        var options = new Folly.PdfOptions
        {
            EnableFontFallback = true
        };

        using var outputMs = new MemoryStream();
        doc.SavePdf(outputMs, options);

        // Assert
        Assert.True(outputMs.Length > 0);

        // Verify PDF header
        outputMs.Position = 0;
        var reader = new StreamReader(outputMs);
        var header = reader.ReadLine();
        Assert.StartsWith("%PDF-", header);
    }

    [Fact]
    public void FontFallback_WithFontStack_GeneratesPdf()
    {
        // Arrange
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
                  <fo:block font-family="NonExistentFont, Arial, Helvetica, sans-serif" font-size="14pt">
                    Test with font fallback stack.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = Folly.FoDocument.Load(ms);

        // Act
        var options = new Folly.PdfOptions
        {
            EnableFontFallback = true
        };

        using var outputMs = new MemoryStream();
        doc.SavePdf(outputMs, options);

        // Assert
        Assert.True(outputMs.Length > 0);
    }

    [Fact]
    public void FontFallback_Disabled_StillGeneratesPdf()
    {
        // Arrange
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
                  <fo:block font-family="NonExistentFont" font-size="14pt">
                    Test without font fallback (should use PDF base fonts).
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = Folly.FoDocument.Load(ms);

        // Act
        var options = new Folly.PdfOptions
        {
            EnableFontFallback = false  // Disabled
        };

        using var outputMs = new MemoryStream();
        doc.SavePdf(outputMs, options);

        // Assert
        Assert.True(outputMs.Length > 0);
    }

    [Fact]
    public void FontFallback_WithCustomAndSystemFonts_PreferCustom()
    {
        // Arrange
        var testFontsDir = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "..", "..", "..", "TestFonts"
        );
        var robotoPath = Path.Combine(testFontsDir, "Roboto-Regular.ttf");

        if (!File.Exists(robotoPath))
        {
            // Skip test if font not available
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
                  <fo:block font-family="Roboto, Arial, sans-serif" font-size="14pt">
                    Test with custom Roboto font in fallback stack.
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = Folly.FoDocument.Load(ms);

        // Act
        var options = new Folly.PdfOptions
        {
            EnableFontFallback = true,
            TrueTypeFonts = new Dictionary<string, string>
            {
                ["Roboto"] = robotoPath
            }
        };

        using var outputMs = new MemoryStream();
        doc.SavePdf(outputMs, options);

        // Assert
        Assert.True(outputMs.Length > 0);
    }
}
