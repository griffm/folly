using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Folly.SourceGenerators
{
    /// <summary>
    /// Source generator that creates font metrics data for the Base 14 PDF fonts from AFM files.
    /// </summary>
    [Generator]
    public class Base14FontsGenerator : ISourceGenerator
    {
        private static readonly string[] Base14Fonts = new[]
        {
            "Courier",
            "Courier-Bold",
            "Courier-Oblique",
            "Courier-BoldOblique",
            "Helvetica",
            "Helvetica-Bold",
            "Helvetica-Oblique",
            "Helvetica-BoldOblique",
            "Times-Roman",
            "Times-Bold",
            "Times-Italic",
            "Times-BoldItalic",
            "Symbol",
            "ZapfDingbats"
        };

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                var fontDataList = new List<FontData>();

                // Load all Base 14 fonts from embedded resources
                foreach (var fontName in Base14Fonts)
                {
                    var fontData = LoadFontFromResource(fontName, context);
                    if (fontData != null)
                    {
                        fontDataList.Add(fontData);
                    }
                }

                if (fontDataList.Count == 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FOLLY010",
                            "No Base14 fonts found",
                            "Could not load any Base14 font metrics from embedded resources",
                            "SourceGenerator",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));
                    return;
                }

                // Generate the source code
                var sourceCode = GenerateSource(fontDataList);
                context.AddSource("Base14Fonts.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FOLLY011",
                        "Generation error",
                        $"Error generating Base14 fonts: {ex.Message}\n{ex.StackTrace}",
                        "SourceGenerator",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
            }
        }

        private FontData? LoadFontFromResource(string fontName, GeneratorExecutionContext context)
        {
            try
            {
                var assembly = typeof(Base14FontsGenerator).Assembly;
                var resourceName = $"Folly.SourceGenerators.base14.{fontName}.afm";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FOLLY012",
                            "AFM file not found",
                            $"Could not find embedded AFM file: {resourceName}",
                            "SourceGenerator",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));
                    return null;
                }

                using var reader = new StreamReader(stream);
                return ParseAfm(fontName, reader);
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FOLLY013",
                        "AFM parse error",
                        $"Error parsing AFM file for {fontName}: {ex.Message}",
                        "SourceGenerator",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));
                return null;
            }
        }

        private FontData? ParseAfm(string fontName, StreamReader reader)
        {
            var actualFontName = fontName;
            var ascender = 0.0;
            var descender = 0.0;
            var charWidths = new Dictionary<int, double>();
            var inCharMetrics = false;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Parse header fields
                if (line.StartsWith("FontName "))
                {
                    actualFontName = line.Substring("FontName ".Length).Trim();
                }
                else if (line.StartsWith("Ascender "))
                {
                    if (double.TryParse(line.Substring("Ascender ".Length).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                        ascender = value;
                }
                else if (line.StartsWith("Descender "))
                {
                    if (double.TryParse(line.Substring("Descender ".Length).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                        descender = value;
                }
                else if (line.StartsWith("StartCharMetrics"))
                {
                    inCharMetrics = true;
                }
                else if (line.StartsWith("EndCharMetrics"))
                {
                    inCharMetrics = false;
                }
                else if (inCharMetrics && line.StartsWith("C "))
                {
                    // Parse character metrics: C 32 ; WX 278 ; N space ; B 0 0 0 0 ;
                    var parts = line.Split(';');
                    if (parts.Length >= 2)
                    {
                        var charCode = -1;
                        var width = 0.0;

                        foreach (var part in parts)
                        {
                            var trimmed = part.Trim();

                            // Parse character code
                            if (trimmed.StartsWith("C "))
                            {
                                var codeStr = trimmed.Substring(2).Trim();
                                int.TryParse(codeStr, out charCode);
                            }
                            // Parse width
                            else if (trimmed.StartsWith("WX "))
                            {
                                var widthStr = trimmed.Substring(3).Trim();
                                double.TryParse(widthStr, NumberStyles.Float, CultureInfo.InvariantCulture, out width);
                            }
                        }

                        if (charCode >= 0 && width > 0)
                        {
                            charWidths[charCode] = width;
                        }
                    }
                }
            }

            // Calculate average width as default
            var defaultWidth = 500.0;
            if (charWidths.Count > 0)
            {
                defaultWidth = charWidths.Values.Average();
            }

            return new FontData
            {
                Name = actualFontName,
                Ascender = ascender,
                Descender = descender,
                CharWidths = charWidths,
                DefaultWidth = defaultWidth
            };
        }

        private string GenerateSource(List<FontData> fonts)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("// Generated from Base 14 PDF font AFM files");
            sb.AppendLine("// DO NOT EDIT - This file was generated automatically");
            sb.AppendLine();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace Folly.Fonts");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Generated font metrics for the Base 14 PDF fonts.");
            sb.AppendLine($"    /// Contains metrics for {fonts.Count} fonts.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    internal static partial class Base14FontMetrics");
            sb.AppendLine("    {");

            // Generate a method to get all fonts
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets all Base 14 font metrics.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static Dictionary<string, FontMetricsData> GetAllFonts()");
            sb.AppendLine("        {");
            sb.AppendLine("            return new Dictionary<string, FontMetricsData>");
            sb.AppendLine("            {");

            foreach (var font in fonts)
            {
                var escapedName = font.Name.Replace("\\", "\\\\").Replace("\"", "\\\"");
                sb.AppendLine($"                [\"{escapedName}\"] = Get{SanitizeName(font.Name)}Metrics(),");
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate a method for each font
            foreach (var font in fonts)
            {
                GenerateFontMethod(sb, font);
            }

            sb.AppendLine("    }");
            sb.AppendLine();

            // Generate the FontMetricsData struct
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Font metrics data structure.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    internal struct FontMetricsData");
            sb.AppendLine("    {");
            sb.AppendLine("        public string Name { get; init; }");
            sb.AppendLine("        public double Ascender { get; init; }");
            sb.AppendLine("        public double Descender { get; init; }");
            sb.AppendLine("        public double DefaultWidth { get; init; }");
            sb.AppendLine("        public Dictionary<int, double> CharWidths { get; init; }");
            sb.AppendLine("    }");

            sb.AppendLine("}");

            return sb.ToString();
        }

        private void GenerateFontMethod(StringBuilder sb, FontData font)
        {
            var methodName = $"Get{SanitizeName(font.Name)}Metrics";
            var escapedName = font.Name.Replace("\\", "\\\\").Replace("\"", "\\\"");

            sb.AppendLine($"        private static FontMetricsData {methodName}()");
            sb.AppendLine("        {");
            sb.AppendLine("            return new FontMetricsData");
            sb.AppendLine("            {");
            sb.AppendLine($"                Name = \"{escapedName}\",");
            sb.AppendLine($"                Ascender = {font.Ascender.ToString("F1", CultureInfo.InvariantCulture)},");
            sb.AppendLine($"                Descender = {font.Descender.ToString("F1", CultureInfo.InvariantCulture)},");
            sb.AppendLine($"                DefaultWidth = {font.DefaultWidth.ToString("F1", CultureInfo.InvariantCulture)},");
            sb.AppendLine("                CharWidths = new Dictionary<int, double>");
            sb.AppendLine("                {");

            // Sort by character code
            foreach (var kvp in font.CharWidths.OrderBy(x => x.Key))
            {
                sb.AppendLine($"                    [{kvp.Key}] = {kvp.Value.ToString("F1", CultureInfo.InvariantCulture)},");
            }

            sb.AppendLine("                }");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private string SanitizeName(string fontName)
        {
            // Replace hyphens with underscores for valid C# method names
            return fontName.Replace("-", "");
        }

        private class FontData
        {
            public string Name { get; set; } = "";
            public double Ascender { get; set; }
            public double Descender { get; set; }
            public double DefaultWidth { get; set; }
            public Dictionary<int, double> CharWidths { get; set; } = new();
        }
    }
}
