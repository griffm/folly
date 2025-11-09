using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Folly;
using Folly.Pdf;

namespace Folly.Benchmarks;

/// <summary>
/// Benchmarks for end-to-end document rendering performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, warmupCount: 3, iterationCount: 5)]
public class RenderingBenchmarks
{
    private FoDocument? _simpleDoc10;
    private FoDocument? _simpleDoc50;
    private FoDocument? _simpleDoc100;
    private FoDocument? _simpleDoc200;
    private FoDocument? _mixedDoc10;
    private FoDocument? _mixedDoc50;
    private FoDocument? _mixedDoc100;
    private FoDocument? _mixedDoc200;

    [GlobalSetup]
    public void Setup()
    {
        // Pre-generate all test documents
        _simpleDoc10 = TestDocumentGenerator.GenerateSimpleDocument(10);
        _simpleDoc50 = TestDocumentGenerator.GenerateSimpleDocument(50);
        _simpleDoc100 = TestDocumentGenerator.GenerateSimpleDocument(100);
        _simpleDoc200 = TestDocumentGenerator.GenerateSimpleDocument(200);

        _mixedDoc10 = TestDocumentGenerator.GenerateMixedDocument(10);
        _mixedDoc50 = TestDocumentGenerator.GenerateMixedDocument(50);
        _mixedDoc100 = TestDocumentGenerator.GenerateMixedDocument(100);
        _mixedDoc200 = TestDocumentGenerator.GenerateMixedDocument(200);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _simpleDoc10?.Dispose();
        _simpleDoc50?.Dispose();
        _simpleDoc100?.Dispose();
        _simpleDoc200?.Dispose();
        _mixedDoc10?.Dispose();
        _mixedDoc50?.Dispose();
        _mixedDoc100?.Dispose();
        _mixedDoc200?.Dispose();
    }

    [Benchmark]
    public void SimpleDocument_10Pages()
    {
        using var ms = new MemoryStream();
        _simpleDoc10!.SavePdf(ms);
    }

    [Benchmark]
    public void SimpleDocument_50Pages()
    {
        using var ms = new MemoryStream();
        _simpleDoc50!.SavePdf(ms);
    }

    [Benchmark]
    public void SimpleDocument_100Pages()
    {
        using var ms = new MemoryStream();
        _simpleDoc100!.SavePdf(ms);
    }

    [Benchmark]
    public void SimpleDocument_200Pages()
    {
        using var ms = new MemoryStream();
        _simpleDoc200!.SavePdf(ms);
    }

    [Benchmark]
    public void MixedDocument_10Pages()
    {
        using var ms = new MemoryStream();
        _mixedDoc10!.SavePdf(ms);
    }

    [Benchmark]
    public void MixedDocument_50Pages()
    {
        using var ms = new MemoryStream();
        _mixedDoc50!.SavePdf(ms);
    }

    [Benchmark]
    public void MixedDocument_100Pages()
    {
        using var ms = new MemoryStream();
        _mixedDoc100!.SavePdf(ms);
    }

    [Benchmark]
    public void MixedDocument_200Pages()
    {
        using var ms = new MemoryStream();
        _mixedDoc200!.SavePdf(ms);
    }
}
