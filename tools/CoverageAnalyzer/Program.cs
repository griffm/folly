using System;
using System.IO;

namespace Folly.Tools.CoverageAnalyzer;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Folly XSL-FO Coverage Analyzer");
        Console.WriteLine("================================");
        Console.WriteLine();

        // Determine Folly source path
        var sourcePath = args.Length > 0 ? args[0] : FindFollyRoot();
        if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
        {
            Console.WriteLine("Error: Folly source directory not found.");
            Console.WriteLine("Usage: dotnet run [path-to-folly-root]");
            Environment.Exit(1);
        }

        Console.WriteLine($"Analyzing Folly source at: {sourcePath}");
        Console.WriteLine();

        // Run analysis
        var analyzer = new XslFoCoverageAnalyzer();
        var report = analyzer.Analyze(sourcePath);

        // Display summary
        Console.WriteLine("Coverage Summary");
        Console.WriteLine("----------------");
        Console.WriteLine($"Overall Coverage: {report.OverallCoverage:F1}%");
        Console.WriteLine();
        Console.WriteLine($"Elements:  {report.Elements.Implemented}/{report.Elements.Total} ({report.Elements.Percentage:F1}%)");
        Console.WriteLine($"Tests:     {report.Tests.TestMethods} test methods in {report.Tests.TestFiles} files");
        Console.WriteLine($"Examples:  {report.Examples.FoExamples} XSL-FO examples, {report.Examples.SvgExamples} SVG examples");
        Console.WriteLine();

        // Display category breakdown
        Console.WriteLine("Coverage by Category");
        Console.WriteLine("--------------------");
        foreach (var (category, coverage) in report.Elements.Categories.OrderByDescending(c => c.Value.Percentage))
        {
            var bar = new string('█', (int)(coverage.Percentage / 5));
            Console.WriteLine($"{category,-20} {coverage.Percentage,5:F1}% {bar}");
        }
        Console.WriteLine();

        // Display not implemented
        if (report.Elements.NotImplemented.Any())
        {
            Console.WriteLine($"Not Implemented ({report.Elements.NotImplemented.Count})");
            Console.WriteLine("----------------");
            foreach (var element in report.Elements.NotImplemented.OrderBy(e => e).Take(10))
            {
                Console.WriteLine($"  - fo:{element}");
            }
            if (report.Elements.NotImplemented.Count > 10)
            {
                Console.WriteLine($"  ... and {report.Elements.NotImplemented.Count - 10} more");
            }
            Console.WriteLine();
        }

        // Generate reports
        var outputPath = Path.Combine(sourcePath, "docs", "validation");
        Directory.CreateDirectory(outputPath);
        analyzer.GenerateReport(report, outputPath);
        Console.WriteLine();
    }

    static string FindFollyRoot()
    {
        // Try to find Folly root by looking for src/Folly.Xslfo.Model directory
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            var testPath = Path.Combine(current, "src", "Folly.Xslfo.Model");
            if (Directory.Exists(testPath))
            {
                return current;
            }

            var parent = Directory.GetParent(current);
            current = parent?.FullName;
        }

        return Directory.GetCurrentDirectory();
    }
}
