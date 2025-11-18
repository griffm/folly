using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Folly;
using Folly.Layout;
using Folly.Pdf;

namespace Folly.Benchmarks;

/// <summary>
/// Benchmarks for individual pipeline stages to identify bottlenecks.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, warmupCount: 3, iterationCount: 5)]
public class PipelineBenchmarks
{
    private FoDocument? _testDoc;
    private AreaTree? _testAreaTree;
    private MemoryStream? _xmlStream;

    [GlobalSetup]
    public void Setup()
    {
        // Create a medium-sized document for pipeline testing
        _testDoc = TestDocumentGenerator.GenerateMixedDocument(50);

        // Pre-build area tree for PDF rendering benchmark
        _testAreaTree = _testDoc.BuildAreaTree();

        // Serialize document to XML for parsing benchmark
        _xmlStream = new MemoryStream();
        var writer = System.Xml.XmlWriter.Create(_xmlStream, new System.Xml.XmlWriterSettings { Indent = false });
        // Note: FoDocument doesn't have a Save method, so we'll use the fluent document directly
        // For now, we'll skip the XML parsing benchmark and focus on layout/rendering
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _testDoc?.Dispose();
        _xmlStream?.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory("Layout")]
    public AreaTree BuildAreaTree()
    {
        return _testDoc!.BuildAreaTree();
    }

    [Benchmark]
    [BenchmarkCategory("Rendering")]
    public void RenderToPdf()
    {
        using var ms = new MemoryStream();
        using var renderer = new PdfRenderer(ms, new PdfOptions());
        renderer.Render(_testAreaTree!);
    }

    [Benchmark]
    [BenchmarkCategory("EndToEnd")]
    public void CompleteRendering()
    {
        using var ms = new MemoryStream();
        _testDoc!.SavePdf(ms);
    }
}
