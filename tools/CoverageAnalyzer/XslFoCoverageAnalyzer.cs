using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Folly.Tools.CoverageAnalyzer;

/// <summary>
/// Analyzes Folly's XSL-FO 1.1 specification coverage by examining implemented
/// formatting objects, properties, and test coverage.
/// </summary>
public class XslFoCoverageAnalyzer
{
    /// <summary>
    /// XSL-FO 1.1 formatting objects from the specification.
    /// Reference: https://www.w3.org/TR/xsl11/ Section 6
    /// </summary>
    private static readonly string[] AllXslFoElements =
    {
        // Document Structure (6.4.2-6.4.4)
        "root", "declarations", "color-profile", "page-sequence",
        "layout-master-set", "page-sequence-master", "single-page-master-reference",
        "repeatable-page-master-reference", "repeatable-page-master-alternatives",
        "conditional-page-master-reference", "simple-page-master", "region-body",
        "region-before", "region-after", "region-start", "region-end",

        // Flow Objects (6.4.5-6.4.7)
        "flow", "static-content", "title",

        // Block-level (6.5.2-6.5.6)
        "block", "block-container",

        // Inline-level (6.6.2-6.6.9)
        "bidi-override", "character", "initial-property-set", "external-graphic",
        "instream-foreign-object", "inline", "inline-container", "leader",
        "page-number", "page-number-citation", "page-number-citation-last",

        // Formatting Objects for Tables (6.7.2-6.7.9)
        "table-and-caption", "table", "table-column", "table-caption",
        "table-header", "table-footer", "table-body", "table-row", "table-cell",

        // Formatting Objects for Lists (6.8.2-6.8.5)
        "list-block", "list-item", "list-item-body", "list-item-label",

        // Dynamic Effects: Link and Multi (6.9.2-6.9.7)
        "basic-link", "multi-switch", "multi-case", "multi-toggle",
        "multi-properties", "multi-property-set",

        // Out-of-Line (6.10.2-6.10.4)
        "float", "footnote", "footnote-body",

        // Other (6.11.2-6.11.7)
        "wrapper", "marker", "retrieve-marker", "retrieve-table-marker",

        // Indexing (6.12.2-6.12.4)
        "index-page-citation-list", "index-key-reference", "index-range-begin",
        "index-range-end",

        // Bookmarks (6.13.2-6.13.3)
        "bookmark-tree", "bookmark"
    };

    /// <summary>
    /// Categories of XSL-FO elements for detailed coverage reporting.
    /// </summary>
    private static readonly Dictionary<string, string[]> ElementCategories = new()
    {
        ["Document Structure"] = new[]
        {
            "root", "declarations", "color-profile", "page-sequence",
            "layout-master-set", "page-sequence-master", "single-page-master-reference",
            "repeatable-page-master-reference", "repeatable-page-master-alternatives",
            "conditional-page-master-reference", "simple-page-master", "region-body",
            "region-before", "region-after", "region-start", "region-end"
        },
        ["Flow Objects"] = new[] { "flow", "static-content", "title" },
        ["Block-level"] = new[] { "block", "block-container" },
        ["Inline-level"] = new[]
        {
            "bidi-override", "character", "initial-property-set", "external-graphic",
            "instream-foreign-object", "inline", "inline-container", "leader",
            "page-number", "page-number-citation", "page-number-citation-last"
        },
        ["Tables"] = new[]
        {
            "table-and-caption", "table", "table-column", "table-caption",
            "table-header", "table-footer", "table-body", "table-row", "table-cell"
        },
        ["Lists"] = new[]
        {
            "list-block", "list-item", "list-item-body", "list-item-label"
        },
        ["Links and Multi"] = new[]
        {
            "basic-link", "multi-switch", "multi-case", "multi-toggle",
            "multi-properties", "multi-property-set"
        },
        ["Out-of-Line"] = new[] { "float", "footnote", "footnote-body" },
        ["Markers"] = new[] { "wrapper", "marker", "retrieve-marker", "retrieve-table-marker" },
        ["Indexing"] = new[]
        {
            "index-page-citation-list", "index-key-reference",
            "index-range-begin", "index-range-end"
        },
        ["Bookmarks"] = new[] { "bookmark-tree", "bookmark" }
    };

    public CoverageReport Analyze(string follySourcePath)
    {
        var report = new CoverageReport
        {
            Timestamp = DateTime.UtcNow,
            SpecificationVersion = "XSL-FO 1.1"
        };

        // Analyze implemented elements
        var implementedElements = FindImplementedElements(follySourcePath);
        report.Elements = AnalyzeElementCoverage(implementedElements);

        // Analyze test coverage
        report.Tests = AnalyzeTestCoverage(follySourcePath);

        // Analyze examples
        report.Examples = AnalyzeExampleCoverage(follySourcePath);

        // Calculate overall metrics
        report.OverallCoverage = CalculateOverallCoverage(report);

        return report;
    }

    private HashSet<string> FindImplementedElements(string sourcePath)
    {
        var implemented = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var domPath = Path.Combine(sourcePath, "src", "Folly.Xslfo.Model", "Dom");

        if (!Directory.Exists(domPath))
        {
            Console.WriteLine($"Warning: Dom directory not found at {domPath}");
            return implemented;
        }

        // Scan all .cs files in Dom directory
        var csFiles = Directory.GetFiles(domPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            // Convert class names like "FoBlock" to "block"
            if (fileName.StartsWith("Fo", StringComparison.OrdinalIgnoreCase))
            {
                var elementName = ConvertClassNameToElementName(fileName);
                if (AllXslFoElements.Contains(elementName, StringComparer.OrdinalIgnoreCase))
                {
                    implemented.Add(elementName);
                }
            }
        }

        return implemented;
    }

    private string ConvertClassNameToElementName(string className)
    {
        // Remove "Fo" prefix
        if (className.StartsWith("Fo", StringComparison.OrdinalIgnoreCase))
        {
            className = className.Substring(2);
        }

        // Convert PascalCase to kebab-case
        // e.g., "PageNumber" -> "page-number"
        var result = string.Concat(
            className.Select((c, i) =>
                i > 0 && char.IsUpper(c) && !char.IsUpper(className[i - 1])
                    ? "-" + char.ToLower(c)
                    : char.ToLower(c).ToString()
            )
        );

        return result;
    }

    private ElementCoverage AnalyzeElementCoverage(HashSet<string> implemented)
    {
        var coverage = new ElementCoverage
        {
            Total = AllXslFoElements.Length,
            Implemented = implemented.Count,
            NotImplemented = AllXslFoElements.Except(implemented, StringComparer.OrdinalIgnoreCase).ToList(),
            Categories = new Dictionary<string, CategoryCoverage>()
        };

        // Analyze by category
        foreach (var (categoryName, elements) in ElementCategories)
        {
            var categoryImplemented = elements.Count(e => implemented.Contains(e, StringComparer.OrdinalIgnoreCase));
            coverage.Categories[categoryName] = new CategoryCoverage
            {
                Total = elements.Length,
                Implemented = categoryImplemented,
                Percentage = (double)categoryImplemented / elements.Length * 100
            };
        }

        coverage.Percentage = (double)coverage.Implemented / coverage.Total * 100;
        return coverage;
    }

    private TestCoverage AnalyzeTestCoverage(string sourcePath)
    {
        var testsPath = Path.Combine(sourcePath, "tests");
        var coverage = new TestCoverage();

        if (!Directory.Exists(testsPath))
        {
            return coverage;
        }

        // Count test files
        var testFiles = Directory.GetFiles(testsPath, "*Tests.cs", SearchOption.AllDirectories);
        coverage.TestFiles = testFiles.Length;

        // Count test methods (simple heuristic: count [Fact] and [Theory])
        foreach (var file in testFiles)
        {
            var content = File.ReadAllText(file);
            coverage.TestMethods += CountOccurrences(content, "[Fact]");
            coverage.TestMethods += CountOccurrences(content, "[Theory]");
        }

        return coverage;
    }

    private ExampleCoverage AnalyzeExampleCoverage(string sourcePath)
    {
        var examplesPath = Path.Combine(sourcePath, "examples");
        var coverage = new ExampleCoverage();

        if (!Directory.Exists(examplesPath))
        {
            return coverage;
        }

        // Count example FO files
        coverage.FoExamples = Directory.GetFiles(examplesPath, "*.fo", SearchOption.AllDirectories).Length;

        // Count SVG examples
        var svgPath = Path.Combine(examplesPath, "svg-examples");
        if (Directory.Exists(svgPath))
        {
            coverage.SvgExamples = Directory.GetFiles(svgPath, "*.svg", SearchOption.AllDirectories).Length;
        }

        return coverage;
    }

    private double CalculateOverallCoverage(CoverageReport report)
    {
        // Weighted average: elements (60%), tests (20%), examples (20%)
        var elementWeight = 0.6;
        var testWeight = 0.2;
        var exampleWeight = 0.2;

        var elementScore = report.Elements.Percentage;
        var testScore = Math.Min(100, (report.Tests.TestMethods / 500.0) * 100); // Target: 500+ tests
        var exampleScore = Math.Min(100, (report.Examples.FoExamples / 50.0) * 100); // Target: 50+ examples

        return (elementScore * elementWeight) + (testScore * testWeight) + (exampleScore * exampleWeight);
    }

    private int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    public void GenerateReport(CoverageReport report, string outputPath)
    {
        // Generate JSON report
        var jsonPath = Path.Combine(outputPath, "coverage-report.json");
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(jsonPath, json);

        // Generate Markdown report
        var mdPath = Path.Combine(outputPath, "coverage-report.md");
        var markdown = GenerateMarkdownReport(report);
        File.WriteAllText(mdPath, markdown);

        Console.WriteLine($"Coverage reports generated:");
        Console.WriteLine($"  JSON: {jsonPath}");
        Console.WriteLine($"  Markdown: {mdPath}");
    }

    private string GenerateMarkdownReport(CoverageReport report)
    {
        var md = new System.Text.StringBuilder();

        md.AppendLine("# Folly XSL-FO Coverage Report");
        md.AppendLine();
        md.AppendLine($"**Generated**: {report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        md.AppendLine($"**Specification**: {report.SpecificationVersion}");
        md.AppendLine($"**Overall Coverage**: {report.OverallCoverage:F1}%");
        md.AppendLine();

        // Element coverage summary
        md.AppendLine("## Element Coverage");
        md.AppendLine();
        md.AppendLine($"**Total**: {report.Elements.Implemented} / {report.Elements.Total} ({report.Elements.Percentage:F1}%)");
        md.AppendLine();

        // Category breakdown
        md.AppendLine("### By Category");
        md.AppendLine();
        md.AppendLine("| Category | Implemented | Total | Coverage |");
        md.AppendLine("|----------|-------------|-------|----------|");

        foreach (var (category, coverage) in report.Elements.Categories.OrderByDescending(c => c.Value.Percentage))
        {
            md.AppendLine($"| {category} | {coverage.Implemented} | {coverage.Total} | {coverage.Percentage:F1}% |");
        }
        md.AppendLine();

        // Not implemented elements
        if (report.Elements.NotImplemented.Any())
        {
            md.AppendLine("### Not Implemented Elements");
            md.AppendLine();
            foreach (var element in report.Elements.NotImplemented.OrderBy(e => e))
            {
                md.AppendLine($"- `fo:{element}`");
            }
            md.AppendLine();
        }

        // Test coverage
        md.AppendLine("## Test Coverage");
        md.AppendLine();
        md.AppendLine($"- **Test Files**: {report.Tests.TestFiles}");
        md.AppendLine($"- **Test Methods**: {report.Tests.TestMethods}");
        md.AppendLine();

        // Example coverage
        md.AppendLine("## Example Coverage");
        md.AppendLine();
        md.AppendLine($"- **XSL-FO Examples**: {report.Examples.FoExamples}");
        md.AppendLine($"- **SVG Examples**: {report.Examples.SvgExamples}");
        md.AppendLine();

        return md.ToString();
    }
}

public class CoverageReport
{
    public DateTime Timestamp { get; set; }
    public string SpecificationVersion { get; set; } = "";
    public double OverallCoverage { get; set; }
    public ElementCoverage Elements { get; set; } = new();
    public TestCoverage Tests { get; set; } = new();
    public ExampleCoverage Examples { get; set; } = new();
}

public class ElementCoverage
{
    public int Total { get; set; }
    public int Implemented { get; set; }
    public double Percentage { get; set; }
    public List<string> NotImplemented { get; set; } = new();
    public Dictionary<string, CategoryCoverage> Categories { get; set; } = new();
}

public class CategoryCoverage
{
    public int Total { get; set; }
    public int Implemented { get; set; }
    public double Percentage { get; set; }
}

public class TestCoverage
{
    public int TestFiles { get; set; }
    public int TestMethods { get; set; }
}

public class ExampleCoverage
{
    public int FoExamples { get; set; }
    public int SvgExamples { get; set; }
}
