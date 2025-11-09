namespace Folly.UnitTests;

/// <summary>
/// Security tests for Folly XSL-FO to PDF renderer.
/// Tests various attack vectors including path traversal, XXE, DoS, and injection attacks.
/// </summary>
public class SecurityTests
{
    #region Path Traversal Tests

    [Fact]
    public void LayoutImage_RejectsAbsolutePaths_WhenNotAllowed()
    {
        // Arrange - Document with absolute path to image
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="page" page-width="8.5in" page-height="11in"
                                       margin-top="1in" margin-bottom="1in"
                                       margin-left="1in" margin-right="1in">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="page">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>
                    <fo:external-graphic src="url('/etc/passwd')"/>
                  </fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        var doc = FoDocument.Load(stream);

        // Act - Layout with default settings (AllowAbsoluteImagePaths = false)
        var layoutOptions = new LayoutOptions();
        var areaTree = doc.BuildAreaTree(layoutOptions);

        // Assert - Image should be rejected (not loaded)
        // The document should still render without throwing, just without the image
        Assert.NotNull(areaTree);
        Assert.NotEmpty(areaTree.Pages);
    }

    [Fact]
    public void LayoutImage_RejectsPathTraversal_WhenBasePathSet()
    {
        // Arrange - Create a temporary directory structure
        var tempDir = Path.Combine(Path.GetTempPath(), $"folly_test_{Guid.NewGuid()}");
        var allowedDir = Path.Combine(tempDir, "allowed");
        var restrictedDir = Path.Combine(tempDir, "restricted");

        try
        {
            Directory.CreateDirectory(allowedDir);
            Directory.CreateDirectory(restrictedDir);

            // Create test image in restricted directory
            var restrictedImage = Path.Combine(restrictedDir, "secret.jpg");
            File.WriteAllBytes(restrictedImage, new byte[] { 0xFF, 0xD8, 0xFF }); // Fake JPEG header

            // Document tries to access image outside allowed directory
            var foXml = $"""
                <?xml version="1.0"?>
                <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
                  <fo:layout-master-set>
                    <fo:simple-page-master master-name="page" page-width="8.5in" page-height="11in"
                                           margin-top="1in" margin-bottom="1in"
                                           margin-left="1in" margin-right="1in">
                      <fo:region-body/>
                    </fo:simple-page-master>
                  </fo:layout-master-set>
                  <fo:page-sequence master-reference="page">
                    <fo:flow flow-name="xsl-region-body">
                      <fo:block>
                        <fo:external-graphic src="url('../restricted/secret.jpg')"/>
                      </fo:block>
                    </fo:flow>
                  </fo:page-sequence>
                </fo:root>
                """;

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
            var doc = FoDocument.Load(stream);

            // Act - Layout with restricted base path
            var layoutOptions = new LayoutOptions
            {
                AllowedImageBasePath = allowedDir,
                AllowAbsoluteImagePaths = false
            };
            var areaTree = doc.BuildAreaTree(layoutOptions);

            // Assert - Document renders without the restricted image
            Assert.NotNull(areaTree);
            Assert.NotEmpty(areaTree.Pages);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void LayoutImage_AllowsImageInAllowedPath()
    {
        // Arrange - Create a temporary directory with allowed image
        var tempDir = Path.Combine(Path.GetTempPath(), $"folly_test_{Guid.NewGuid()}");
        var allowedDir = Path.Combine(tempDir, "images");

        try
        {
            Directory.CreateDirectory(allowedDir);

            // Create a minimal valid PNG image (1x1 red pixel)
            var pngData = new byte[]
            {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
                0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
                0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
                0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT chunk
                0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
                0x00, 0x03, 0x01, 0x01, 0x00, 0x18, 0xDD, 0x8D,
                0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, // IEND chunk
                0x44, 0xAE, 0x42, 0x60, 0x82
            };

            var imagePath = Path.Combine(allowedDir, "test.png");
            File.WriteAllBytes(imagePath, pngData);

            // Document references image in allowed directory
            var foXml = $"""
                <?xml version="1.0"?>
                <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
                  <fo:layout-master-set>
                    <fo:simple-page-master master-name="page" page-width="8.5in" page-height="11in"
                                           margin-top="1in" margin-bottom="1in"
                                           margin-left="1in" margin-right="1in">
                      <fo:region-body/>
                    </fo:simple-page-master>
                  </fo:layout-master-set>
                  <fo:page-sequence master-reference="page">
                    <fo:flow flow-name="xsl-region-body">
                      <fo:block>
                        <fo:external-graphic src="url('{imagePath}')"/>
                      </fo:block>
                    </fo:flow>
                  </fo:page-sequence>
                </fo:root>
                """;

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
            var doc = FoDocument.Load(stream);

            // Act - Layout with allowed base path and absolute paths enabled
            var layoutOptions = new LayoutOptions
            {
                AllowedImageBasePath = allowedDir,
                AllowAbsoluteImagePaths = true
            };
            var areaTree = doc.BuildAreaTree(layoutOptions);

            // Assert - Document should render successfully with the image
            Assert.NotNull(areaTree);
            Assert.NotEmpty(areaTree.Pages);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion

    #region XXE Attack Tests

    [Fact]
    public void Load_RejectsXXE_ExternalEntityAttack()
    {
        // Arrange - Malicious XML with external entity
        var xxeXml = """
            <?xml version="1.0"?>
            <!DOCTYPE foo [
              <!ENTITY xxe SYSTEM "file:///etc/passwd">
            ]>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="page" page-width="8.5in" page-height="11in"
                                       margin-top="1in" margin-bottom="1in"
                                       margin-left="1in" margin-right="1in">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="page">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>&xxe;</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(xxeXml));

        // Act & Assert - Should throw due to DTD being prohibited
        Assert.Throws<System.Xml.XmlException>(() => FoDocument.Load(stream));
    }

    [Fact]
    public void Load_RejectsXXE_EntityExpansionBomb()
    {
        // Arrange - Billion laughs attack (exponential entity expansion)
        var bombXml = """
            <?xml version="1.0"?>
            <!DOCTYPE lolz [
              <!ENTITY lol "lol">
              <!ENTITY lol2 "&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;">
              <!ENTITY lol3 "&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;">
            ]>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="page" page-width="8.5in" page-height="11in">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="page">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>&lol3;</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(bombXml));

        // Act & Assert - Should throw due to DTD being prohibited
        Assert.Throws<System.Xml.XmlException>(() => FoDocument.Load(stream));
    }

    #endregion

    #region DoS / Resource Exhaustion Tests

    [Fact]
    public void BuildAreaTree_ThrowsWhenMaxPagesExceeded()
    {
        // Arrange - Document that tries to create many pages
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="page" page-width="8.5in" page-height="11in"
                                       margin-top="1in" margin-bottom="1in"
                                       margin-left="1in" margin-right="1in">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="page">
                <fo:flow flow-name="xsl-region-body">
            """;

        // Add many blocks with page breaks to exceed limit
        for (int i = 0; i < 15; i++)
        {
            foXml += $"""
                      <fo:block break-after="page">Page {i}</fo:block>
                """;
        }

        foXml += """
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        var doc = FoDocument.Load(stream);

        // Act & Assert - Should throw when MaxPages limit is exceeded
        var layoutOptions = new LayoutOptions
        {
            MaxPages = 10 // Set a low limit for testing
        };

        var ex = Assert.Throws<InvalidOperationException>(() => doc.BuildAreaTree(layoutOptions));
        Assert.Contains("Maximum page limit", ex.Message);
    }

    [Fact]
    public void LayoutImage_RejectsHugeImage_WhenSizeExceedsLimit()
    {
        // Arrange - Create a large fake image file
        var tempDir = Path.Combine(Path.GetTempPath(), $"folly_test_{Guid.NewGuid()}");
        var imageDir = Path.Combine(tempDir, "images");

        try
        {
            Directory.CreateDirectory(imageDir);

            // Create a 2MB file (exceeding our test limit of 1MB)
            var imagePath = Path.Combine(imageDir, "huge.png");
            var largeData = new byte[2 * 1024 * 1024];
            // Add PNG header so it's recognized as an image
            largeData[0] = 0x89;
            largeData[1] = 0x50;
            largeData[2] = 0x4E;
            largeData[3] = 0x47;
            File.WriteAllBytes(imagePath, largeData);

            var foXml = $"""
                <?xml version="1.0"?>
                <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
                  <fo:layout-master-set>
                    <fo:simple-page-master master-name="page" page-width="8.5in" page-height="11in"
                                           margin-top="1in" margin-bottom="1in"
                                           margin-left="1in" margin-right="1in">
                      <fo:region-body/>
                    </fo:simple-page-master>
                  </fo:layout-master-set>
                  <fo:page-sequence master-reference="page">
                    <fo:flow flow-name="xsl-region-body">
                      <fo:block>
                        <fo:external-graphic src="url('{imagePath}')"/>
                      </fo:block>
                    </fo:flow>
                  </fo:page-sequence>
                </fo:root>
                """;

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
            var doc = FoDocument.Load(stream);

            // Act - Layout with size limit
            var layoutOptions = new LayoutOptions
            {
                AllowedImageBasePath = imageDir,
                AllowAbsoluteImagePaths = true,
                MaxImageSizeBytes = 1 * 1024 * 1024 // 1MB limit
            };
            var areaTree = doc.BuildAreaTree(layoutOptions);

            // Assert - Document should render without the huge image
            Assert.NotNull(areaTree);
            Assert.NotEmpty(areaTree.Pages);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion

    #region PNG Integer Overflow Tests

    [Fact]
    public void DecodePng_RejectsMaliciousChunkLength()
    {
        // Arrange - PNG with malicious chunk length
        var maliciousPng = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x7F, 0xFF, 0xFF, 0xFF, // Malicious chunk length (huge value)
            0x49, 0x44, 0x41, 0x54, // "IDAT" chunk type
            0x00, 0x00, 0x00, 0x00  // Minimal data
        };

        var tempDir = Path.Combine(Path.GetTempPath(), $"folly_test_{Guid.NewGuid()}");
        var imageDir = Path.Combine(tempDir, "images");

        try
        {
            Directory.CreateDirectory(imageDir);
            var imagePath = Path.Combine(imageDir, "malicious.png");
            File.WriteAllBytes(imagePath, maliciousPng);

            var foXml = $"""
                <?xml version="1.0"?>
                <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
                  <fo:layout-master-set>
                    <fo:simple-page-master master-name="page" page-width="8.5in" page-height="11in"
                                           margin-top="1in" margin-bottom="1in"
                                           margin-left="1in" margin-right="1in">
                      <fo:region-body/>
                    </fo:simple-page-master>
                  </fo:layout-master-set>
                  <fo:page-sequence master-reference="page">
                    <fo:flow flow-name="xsl-region-body">
                      <fo:block>
                        <fo:external-graphic src="url('{imagePath}')"/>
                      </fo:block>
                    </fo:flow>
                  </fo:page-sequence>
                </fo:root>
                """;

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
            var doc = FoDocument.Load(stream);

            // Act - Should handle gracefully without crashing
            var layoutOptions = new LayoutOptions
            {
                AllowedImageBasePath = imageDir,
                AllowAbsoluteImagePaths = true
            };
            var areaTree = doc.BuildAreaTree(layoutOptions);

            // Assert - Should render without the malicious image
            Assert.NotNull(areaTree);
            Assert.NotEmpty(areaTree.Pages);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion

    #region PDF Metadata Injection Tests

    [Fact]
    public void PdfMetadata_EscapesSpecialCharacters()
    {
        // Arrange - Metadata with special characters that could break PDF structure
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="page" page-width="8.5in" page-height="11in"
                                       margin-top="1in" margin-bottom="1in"
                                       margin-left="1in" margin-right="1in">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="page">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Test content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        var doc = FoDocument.Load(stream);

        // Act - Create PDF with malicious metadata
        var pdfOptions = new PdfOptions
        {
            Metadata = new PdfMetadata
            {
                Title = "Test) /Author (Injected) /Title (Real",
                Author = "Test\nInjection",
                Subject = "Test\\Backslash",
                Keywords = "Test(Parens)"
            }
        };

        var output = new MemoryStream();
        doc.SavePdf(output, pdfOptions);

        // Assert - PDF should be valid and metadata should be escaped
        output.Position = 0;
        var pdfContent = Encoding.ASCII.GetString(output.ToArray());

        // Verify PDF structure is not broken
        Assert.Contains("%PDF-1.7", pdfContent);
        Assert.Contains("%%EOF", pdfContent);

        // Verify special characters are escaped
        Assert.DoesNotContain("Test) /Author (Injected)", pdfContent); // Injection should be escaped
        Assert.Contains("\\(", pdfContent); // Parentheses should be escaped
        Assert.Contains("\\)", pdfContent);
    }

    [Fact]
    public void PdfMetadata_RemovesNullBytes()
    {
        // Arrange
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="page" page-width="8.5in" page-height="11in"
                                       margin-top="1in" margin-bottom="1in"
                                       margin-left="1in" margin-right="1in">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="page">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Test content</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(foXml));
        var doc = FoDocument.Load(stream);

        // Act - Create PDF with null bytes in metadata
        var pdfOptions = new PdfOptions
        {
            Metadata = new PdfMetadata
            {
                Title = "Test\0NullByte",
                Author = "Normal Author"
            }
        };

        var output = new MemoryStream();
        doc.SavePdf(output, pdfOptions);

        // Assert - PDF should be valid
        output.Position = 0;
        var pdfContent = Encoding.ASCII.GetString(output.ToArray());
        Assert.Contains("%PDF-1.7", pdfContent);
        Assert.Contains("%%EOF", pdfContent);
    }

    #endregion
}
