namespace Folly.Fluent;

/// <summary>
/// Fluent API entry point for building XSL-FO documents programmatically.
/// </summary>
public static class Fo
{
    /// <summary>
    /// Creates a new XSL-FO document.
    /// </summary>
    /// <param name="configure">Action to configure the document.</param>
    /// <returns>A fluent document builder.</returns>
    public static DocumentBuilder Document(Action<DocumentBuilder>? configure = null)
    {
        var builder = new DocumentBuilder();
        configure?.Invoke(builder);
        return builder;
    }
}

/// <summary>
/// Builder for constructing an XSL-FO document.
/// </summary>
public sealed class DocumentBuilder
{
    private PdfMetadata? _metadata;

    // TODO: Implement fluent builder
    // - Layout masters
    // - Page sequences
    // - Static content
    // - Flows
    // - Blocks
    // - Tables
    // - Lists
    // - Inline content

    /// <summary>
    /// Configures document metadata.
    /// </summary>
    public DocumentBuilder Metadata(Action<MetadataBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _metadata = new PdfMetadata();
        var builder = new MetadataBuilder(_metadata);
        configure(builder);
        return this;
    }

    /// <summary>
    /// Configures layout masters.
    /// </summary>
    public DocumentBuilder LayoutMasters(Action<LayoutMasterBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        // TODO: Implement
        return this;
    }

    /// <summary>
    /// Adds a page sequence.
    /// </summary>
    public DocumentBuilder PageSequence(string masterReference, Action<PageSequenceBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(masterReference);
        ArgumentNullException.ThrowIfNull(configure);
        // TODO: Implement
        return this;
    }

    /// <summary>
    /// Saves the document as PDF.
    /// </summary>
    public void SavePdf(string path, PdfOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        options ??= new PdfOptions();

        // Apply fluent metadata if configured
        if (_metadata != null)
        {
            options.Metadata = _metadata;
        }

        // TODO: Build FO XML and render to PDF
        throw new NotImplementedException("Fluent API not yet implemented");
    }

    /// <summary>
    /// Saves the document as PDF to a stream.
    /// </summary>
    public void SavePdf(Stream output, PdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(output);

        options ??= new PdfOptions();

        // Apply fluent metadata if configured
        if (_metadata != null)
        {
            options.Metadata = _metadata;
        }

        // TODO: Build FO XML and render to PDF
        throw new NotImplementedException("Fluent API not yet implemented");
    }
}

/// <summary>
/// Builder for document metadata.
/// </summary>
public sealed class MetadataBuilder
{
    private readonly PdfMetadata _metadata;

    internal MetadataBuilder(PdfMetadata metadata)
    {
        _metadata = metadata;
    }

    /// <summary>
    /// Sets the document title.
    /// </summary>
    public MetadataBuilder Title(string title)
    {
        _metadata.Title = title;
        return this;
    }

    /// <summary>
    /// Sets the document author.
    /// </summary>
    public MetadataBuilder Author(string author)
    {
        _metadata.Author = author;
        return this;
    }

    /// <summary>
    /// Sets the document subject.
    /// </summary>
    public MetadataBuilder Subject(string subject)
    {
        _metadata.Subject = subject;
        return this;
    }

    /// <summary>
    /// Sets the document keywords.
    /// </summary>
    public MetadataBuilder Keywords(string keywords)
    {
        _metadata.Keywords = keywords;
        return this;
    }

    /// <summary>
    /// Sets the application that created the document.
    /// </summary>
    public MetadataBuilder Creator(string creator)
    {
        _metadata.Creator = creator;
        return this;
    }

    /// <summary>
    /// Sets the producer (library name and version).
    /// </summary>
    public MetadataBuilder Producer(string producer)
    {
        _metadata.Producer = producer;
        return this;
    }
}

/// <summary>
/// Builder for layout masters.
/// </summary>
public sealed class LayoutMasterBuilder
{
    // TODO: Implement layout master builder
}

/// <summary>
/// Builder for page sequences.
/// </summary>
public sealed class PageSequenceBuilder
{
    // TODO: Implement page sequence builder
}
