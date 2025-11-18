using System.Text;
using System.Xml;

namespace Folly.Pdf;

/// <summary>
/// Generates XMP (Extensible Metadata Platform) metadata for PDF documents.
/// XMP is required for PDF/A compliance.
/// </summary>
internal static class XmpMetadataWriter
{
    /// <summary>
    /// Creates XMP metadata packet for PDF/A compliance.
    /// </summary>
    public static byte[] CreateXmpMetadata(PdfMetadata metadata, PdfALevel pdfALevel, string pdfVersion)
    {
        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false
        };

        using (var stringWriter = new StringWriter(sb))
        using (var writer = XmlWriter.Create(stringWriter, settings))
        {
            writer.WriteStartDocument();

            // Start XMP packet
            writer.WriteStartElement("x", "xmpmeta", "adobe:ns:meta/");
            writer.WriteAttributeString("xmlns", "x", null, "adobe:ns:meta/");

            // RDF wrapper
            writer.WriteStartElement("rdf", "RDF", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

            // RDF Description
            writer.WriteStartElement("rdf", "Description", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            writer.WriteAttributeString("rdf", "about", "http://www.w3.org/1999/02/22-rdf-syntax-ns#", "");

            // Dublin Core namespace for title, creator, description, subject
            writer.WriteAttributeString("xmlns", "dc", null, "http://purl.org/dc/elements/1.1/");

            // XMP namespace for metadata dates and tool info
            writer.WriteAttributeString("xmlns", "xmp", null, "http://ns.adobe.com/xap/1.0/");

            // PDF namespace for PDF-specific properties
            writer.WriteAttributeString("xmlns", "pdf", null, "http://ns.adobe.com/pdf/1.3/");

            // PDF/A namespace for PDF/A identification
            if (pdfALevel != PdfALevel.None)
            {
                writer.WriteAttributeString("xmlns", "pdfaid", null, "http://www.aiim.org/pdfa/ns/id/");
            }

            // Dublin Core: Title
            if (!string.IsNullOrWhiteSpace(metadata.Title))
            {
                writer.WriteStartElement("dc", "title", "http://purl.org/dc/elements/1.1/");
                writer.WriteStartElement("rdf", "Alt", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                writer.WriteStartElement("rdf", "li", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                writer.WriteAttributeString("xml", "lang", "http://www.w3.org/XML/1998/namespace", "x-default");
                writer.WriteString(metadata.Title);
                writer.WriteEndElement(); // li
                writer.WriteEndElement(); // Alt
                writer.WriteEndElement(); // title
            }

            // Dublin Core: Creator (author)
            if (!string.IsNullOrWhiteSpace(metadata.Author))
            {
                writer.WriteStartElement("dc", "creator", "http://purl.org/dc/elements/1.1/");
                writer.WriteStartElement("rdf", "Seq", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                writer.WriteStartElement("rdf", "li", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                writer.WriteString(metadata.Author);
                writer.WriteEndElement(); // li
                writer.WriteEndElement(); // Seq
                writer.WriteEndElement(); // creator
            }

            // Dublin Core: Description (subject)
            if (!string.IsNullOrWhiteSpace(metadata.Subject))
            {
                writer.WriteStartElement("dc", "description", "http://purl.org/dc/elements/1.1/");
                writer.WriteStartElement("rdf", "Alt", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                writer.WriteStartElement("rdf", "li", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                writer.WriteAttributeString("xml", "lang", "http://www.w3.org/XML/1998/namespace", "x-default");
                writer.WriteString(metadata.Subject);
                writer.WriteEndElement(); // li
                writer.WriteEndElement(); // Alt
                writer.WriteEndElement(); // description
            }

            // Dublin Core: Keywords (subject)
            if (!string.IsNullOrWhiteSpace(metadata.Keywords))
            {
                writer.WriteStartElement("dc", "subject", "http://purl.org/dc/elements/1.1/");
                writer.WriteStartElement("rdf", "Bag", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

                // Split keywords by comma or semicolon
                var keywords = metadata.Keywords.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var keyword in keywords)
                {
                    var trimmed = keyword.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        writer.WriteStartElement("rdf", "li", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                        writer.WriteString(trimmed);
                        writer.WriteEndElement(); // li
                    }
                }

                writer.WriteEndElement(); // Bag
                writer.WriteEndElement(); // subject
            }

            // XMP: Creator Tool
            if (!string.IsNullOrWhiteSpace(metadata.Creator))
            {
                writer.WriteStartElement("xmp", "CreatorTool", "http://ns.adobe.com/xap/1.0/");
                writer.WriteString(metadata.Creator);
                writer.WriteEndElement();
            }

            // XMP: Creation Date and Modification Date
            var now = DateTime.UtcNow;
            var timestamp = now.ToString("yyyy-MM-ddTHH:mm:ssZ");

            writer.WriteStartElement("xmp", "CreateDate", "http://ns.adobe.com/xap/1.0/");
            writer.WriteString(timestamp);
            writer.WriteEndElement();

            writer.WriteStartElement("xmp", "ModifyDate", "http://ns.adobe.com/xap/1.0/");
            writer.WriteString(timestamp);
            writer.WriteEndElement();

            // PDF: Producer
            if (!string.IsNullOrWhiteSpace(metadata.Producer))
            {
                writer.WriteStartElement("pdf", "Producer", "http://ns.adobe.com/pdf/1.3/");
                writer.WriteString(metadata.Producer);
                writer.WriteEndElement();
            }

            // PDF: Keywords (duplicate from Dublin Core for compatibility)
            if (!string.IsNullOrWhiteSpace(metadata.Keywords))
            {
                writer.WriteStartElement("pdf", "Keywords", "http://ns.adobe.com/pdf/1.3/");
                writer.WriteString(metadata.Keywords);
                writer.WriteEndElement();
            }

            // PDF/A Identification
            if (pdfALevel != PdfALevel.None)
            {
                var (part, conformance) = GetPdfAIdentification(pdfALevel);

                writer.WriteStartElement("pdfaid", "part", "http://www.aiim.org/pdfa/ns/id/");
                writer.WriteString(part);
                writer.WriteEndElement();

                writer.WriteStartElement("pdfaid", "conformance", "http://www.aiim.org/pdfa/ns/id/");
                writer.WriteString(conformance);
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // Description
            writer.WriteEndElement(); // RDF
            writer.WriteEndElement(); // xmpmeta
            writer.WriteEndDocument();
        }

        var xmpString = sb.ToString();

        // Add XMP packet wrapper with padding (required for in-place updates)
        var packet = new StringBuilder();
        packet.AppendLine("<?xpacket begin=\"\uFEFF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>");
        packet.Append(xmpString);
        packet.AppendLine();
        packet.AppendLine("<?xpacket end=\"w\"?>");

        return Encoding.UTF8.GetBytes(packet.ToString());
    }

    /// <summary>
    /// Gets the PDF/A part and conformance level for XMP metadata.
    /// </summary>
    private static (string part, string conformance) GetPdfAIdentification(PdfALevel level)
    {
        return level switch
        {
            PdfALevel.PdfA1b => ("1", "B"),
            PdfALevel.PdfA2b => ("2", "B"),
            PdfALevel.PdfA3b => ("3", "B"),
            _ => throw new ArgumentException($"Unsupported PDF/A level: {level}", nameof(level))
        };
    }
}
