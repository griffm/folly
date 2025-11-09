using System.Diagnostics;
using Folly;
using Folly.Pdf;

namespace Folly.Benchmarks;

/// <summary>
/// Lightweight performance test for CI integration.
/// Checks that rendering performance meets minimum standards.
/// </summary>
public static class PerformanceTest
{
    /// <summary>
    /// Performance thresholds (in milliseconds).
    /// </summary>
    private static class Thresholds
    {
        // Conservative thresholds: allow headroom for CI variance and document generation overhead
        public const double SimpleDocument10Pages = 50;     // Current: ~24ms (includes XML generation)
        public const double SimpleDocument50Pages = 200;    // Current: ~119ms (includes XML generation)
        public const double SimpleDocument100Pages = 400;   // Current: ~246ms (includes XML generation)
        public const double SimpleDocument200Pages = 800;   // Current: ~459ms (includes XML generation)
        public const double MixedDocument200Pages = 1500;   // Current: ~918ms (includes complex XML generation)
        public const double MaxMemoryMB = 600;              // Target: <600MB
    }

    public static int Run(string[] args)
    {
        Console.WriteLine("Folly Performance Test");
        Console.WriteLine("======================\n");

        var passed = true;
        var results = new List<(string Test, double Time, double Threshold, bool Passed)>();

        // Test 1: Simple 10-page document
        passed &= RunTest("Simple 10-page document",
            () => TestDocumentGenerator.GenerateSimpleDocument(10),
            Thresholds.SimpleDocument10Pages,
            results);

        // Test 2: Simple 50-page document
        passed &= RunTest("Simple 50-page document",
            () => TestDocumentGenerator.GenerateSimpleDocument(50),
            Thresholds.SimpleDocument50Pages,
            results);

        // Test 3: Simple 100-page document
        passed &= RunTest("Simple 100-page document",
            () => TestDocumentGenerator.GenerateSimpleDocument(100),
            Thresholds.SimpleDocument100Pages,
            results);

        // Test 4: Simple 200-page document (primary target)
        passed &= RunTest("Simple 200-page document",
            () => TestDocumentGenerator.GenerateSimpleDocument(200),
            Thresholds.SimpleDocument200Pages,
            results);

        // Test 5: Mixed 200-page document (complex)
        passed &= RunTest("Mixed 200-page document",
            () => TestDocumentGenerator.GenerateMixedDocument(200),
            Thresholds.MixedDocument200Pages,
            results);

        // Print results table
        Console.WriteLine("\nResults:");
        Console.WriteLine("┌────────────────────────────────┬────────────┬───────────┬────────┐");
        Console.WriteLine("│ Test                           │ Time (ms)  │ Threshold │ Status │");
        Console.WriteLine("├────────────────────────────────┼────────────┼───────────┼────────┤");

        foreach (var (test, time, threshold, testPassed) in results)
        {
            var status = testPassed ? "✓ PASS" : "✗ FAIL";
            Console.WriteLine($"│ {test,-30} │ {time,10:F2} │ {threshold,9:F2} │ {status,-6} │");
        }

        Console.WriteLine("└────────────────────────────────┴────────────┴───────────┴────────┘");

        // Overall result
        Console.WriteLine();
        if (passed)
        {
            Console.WriteLine("✓ All performance tests PASSED");
            return 0;
        }
        else
        {
            Console.WriteLine("✗ Some performance tests FAILED");
            return 1;
        }
    }

    private static bool RunTest(
        string name,
        Func<FoDocument> documentGenerator,
        double thresholdMs,
        List<(string, double, double, bool)> results)
    {
        Console.WriteLine($"Running: {name}...");

        // Warmup
        using (var warmupDoc = documentGenerator())
        {
            using var warmupMs = new MemoryStream();
            warmupDoc.SavePdf(warmupMs);
        }

        // Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Measure (3 iterations)
        var times = new List<double>();
        for (int i = 0; i < 3; i++)
        {
            using var doc = documentGenerator();
            using var outputStream = new MemoryStream();

            var sw = Stopwatch.StartNew();
            doc.SavePdf(outputStream);
            sw.Stop();

            times.Add(sw.Elapsed.TotalMilliseconds);
        }

        var avgTime = times.Average();
        var passed = avgTime <= thresholdMs;

        results.Add((name, avgTime, thresholdMs, passed));

        Console.WriteLine($"  Average time: {avgTime:F2}ms (threshold: {thresholdMs:F2}ms) - {(passed ? "PASS" : "FAIL")}");

        return passed;
    }
}
