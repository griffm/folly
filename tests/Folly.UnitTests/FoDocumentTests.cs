namespace Folly.UnitTests;

public class FoDocumentTests
{
    [Fact]
    public void Load_ThrowsArgumentNullException_WhenPathIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => FoDocument.Load((string)null!));
    }

    [Fact]
    public void Load_ThrowsArgumentException_WhenPathIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => FoDocument.Load(string.Empty));
    }

    [Fact]
    public void Load_ThrowsArgumentNullException_WhenStreamIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => FoDocument.Load((Stream)null!));
    }

    [Fact]
    public void BuildAreaTree_ReturnsAreaTree()
    {
        // Arrange
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Hello, Folly!</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(stream);

        // Act
        var areaTree = doc.BuildAreaTree();

        // Assert
        Assert.NotNull(areaTree);
        Assert.NotNull(areaTree.Pages);
    }

    [Fact]
    public void SavePdf_CreatesNonEmptyOutput()
    {
        // Arrange
        var foXml = """
            <?xml version="1.0"?>
            <fo:root xmlns:fo="http://www.w3.org/1999/XSL/Format">
              <fo:layout-master-set>
                <fo:simple-page-master master-name="A4">
                  <fo:region-body/>
                </fo:simple-page-master>
              </fo:layout-master-set>
              <fo:page-sequence master-reference="A4">
                <fo:flow flow-name="xsl-region-body">
                  <fo:block>Hello, Folly!</fo:block>
                </fo:flow>
              </fo:page-sequence>
            </fo:root>
            """;

        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(foXml));
        using var doc = FoDocument.Load(inputStream);
        using var outputStream = new MemoryStream();

        // Act
        doc.SavePdf(outputStream);

        // Assert
        Assert.True(outputStream.Length > 0, "PDF output should not be empty");

        // Verify it starts with PDF header
        outputStream.Position = 0;
        var header = new byte[5];
        outputStream.Read(header, 0, 5);
        var headerString = System.Text.Encoding.ASCII.GetString(header);
        Assert.Equal("%PDF-", headerString);
    }
}
