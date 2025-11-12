using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Folly.SourceGenerators.Hyphenation
{
    /// <summary>
    /// Source generator that creates hyphenation pattern data from TeX pattern files.
    /// Implements Frank Liang's TeX hyphenation algorithm at build time.
    /// </summary>
    [Generator]
    public class HyphenationPatternsGenerator : ISourceGenerator
    {
        private static readonly Dictionary<string, string> Languages = new()
        {
            ["en-US"] = "hyph-en-us.pat.txt",
            ["de-DE"] = "hyph-de-1996.pat.txt",
            ["fr-FR"] = "hyph-fr.pat.txt",
            ["es-ES"] = "hyph-es.pat.txt"
        };

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                var languagePatterns = new Dictionary<string, LanguagePatterns>();

                // Load patterns for each language
                foreach (var kvp in Languages)
                {
                    var languageCode = kvp.Key;
                    var fileName = kvp.Value;
                    var patterns = LoadPatternsFromResource(fileName, context);
                    if (patterns != null && patterns.Count > 0)
                    {
                        languagePatterns[languageCode] = new LanguagePatterns
                        {
                            LanguageCode = languageCode,
                            Patterns = patterns
                        };
                    }
                }

                if (languagePatterns.Count == 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FOLLY020",
                            "No hyphenation patterns found",
                            "Could not load any hyphenation patterns from embedded resources",
                            "SourceGenerator",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));
                    return;
                }

                // Generate the source code
                var sourceCode = GenerateSource(languagePatterns);
                context.AddSource("HyphenationPatterns.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FOLLY021",
                        "Generation error",
                        $"Error generating hyphenation patterns: {ex.Message}\n{ex.StackTrace}",
                        "SourceGenerator",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
            }
        }

        private Dictionary<string, int[]>? LoadPatternsFromResource(string fileName, GeneratorExecutionContext context)
        {
            try
            {
                var assembly = typeof(HyphenationPatternsGenerator).Assembly;
                var resourceName = $"Folly.SourceGenerators.Hyphenation.patterns.{fileName}";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FOLLY022",
                            "Pattern file not found",
                            $"Could not find embedded pattern file: {resourceName}",
                            "SourceGenerator",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));
                    return null;
                }

                using var reader = new StreamReader(stream);
                return ParsePatterns(reader);
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FOLLY023",
                        "Pattern parse error",
                        $"Error parsing pattern file {fileName}: {ex.Message}",
                        "SourceGenerator",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));
                return null;
            }
        }

        /// <summary>
        /// Parses TeX hyphenation patterns into a dictionary.
        /// Pattern format: letters with interspersed numbers indicating hyphenation points.
        /// Example: ".ach4" means word-start + "ach" with priority 4 after "ach"
        /// The '.' represents word boundaries.
        /// </summary>
        private Dictionary<string, int[]> ParsePatterns(StreamReader reader)
        {
            var patterns = new Dictionary<string, int[]>();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var pattern = line.Trim();
                if (string.IsNullOrEmpty(pattern))
                    continue;

                // Parse the pattern into letters and numbers
                var letters = new StringBuilder();
                var numbers = new List<int>();

                // Start with implicit 0
                numbers.Add(0);

                foreach (var ch in pattern)
                {
                    if (char.IsDigit(ch))
                    {
                        // This number applies to the position after the last letter
                        if (numbers.Count == letters.Length + 1)
                        {
                            numbers[numbers.Count - 1] = ch - '0';
                        }
                    }
                    else
                    {
                        letters.Append(ch);
                        numbers.Add(0); // Implicit 0 after each letter
                    }
                }

                var patternKey = letters.ToString();
                if (!string.IsNullOrEmpty(patternKey))
                {
                    patterns[patternKey] = numbers.ToArray();
                }
            }

            return patterns;
        }

        private string GenerateSource(Dictionary<string, LanguagePatterns> languagePatterns)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("// Generated from TeX hyphenation pattern files");
            sb.AppendLine("// Based on Frank Liang's hyphenation algorithm");
            sb.AppendLine("// Pattern files are public domain from CTAN");
            sb.AppendLine("// DO NOT EDIT - This file was generated automatically");
            sb.AppendLine();
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace Folly.Core.Hyphenation");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Generated hyphenation patterns for multiple languages.");
            sb.AppendLine($"    /// Contains patterns for {languagePatterns.Count} languages.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    internal static partial class HyphenationPatterns");
            sb.AppendLine("    {");

            // Generate a method to get patterns for a specific language
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets hyphenation patterns for the specified language.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"languageCode\">The language code (e.g., 'en-US', 'de-DE').</param>");
            sb.AppendLine("        /// <returns>Dictionary mapping pattern strings to priority arrays, or null if language not found.</returns>");
            sb.AppendLine("        public static Dictionary<string, int[]>? GetPatterns(string languageCode)");
            sb.AppendLine("        {");
            sb.AppendLine("            return languageCode switch");
            sb.AppendLine("            {");

            foreach (var kvp in languagePatterns)
            {
                var languageCode = kvp.Key;
                var methodName = $"Get{SanitizeLanguageCode(languageCode)}Patterns";
                sb.AppendLine($"                \"{languageCode}\" => {methodName}(),");
            }

            sb.AppendLine("                _ => null");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate a method to get all supported languages
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets all supported language codes.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static string[] GetSupportedLanguages()");
            sb.AppendLine("        {");
            sb.AppendLine("            return new[]");
            sb.AppendLine("            {");

            foreach (var languageCode in languagePatterns.Keys)
            {
                sb.AppendLine($"                \"{languageCode}\",");
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate a method for each language
            foreach (var kvp in languagePatterns)
            {
                GenerateLanguageMethod(sb, kvp.Key, kvp.Value);
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private void GenerateLanguageMethod(StringBuilder sb, string languageCode, LanguagePatterns patterns)
        {
            var methodName = $"Get{SanitizeLanguageCode(languageCode)}Patterns";

            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Gets hyphenation patterns for {languageCode}.");
            sb.AppendLine($"        /// Contains {patterns.Patterns.Count} patterns.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        private static Dictionary<string, int[]> {methodName}()");
            sb.AppendLine("        {");
            sb.AppendLine("            return new Dictionary<string, int[]>");
            sb.AppendLine("            {");

            // Sort patterns alphabetically for better code organization
            foreach (var kvp in patterns.Patterns.OrderBy(x => x.Key))
            {
                var pattern = kvp.Key;
                var priorities = kvp.Value;
                var escapedPattern = pattern.Replace("\\", "\\\\").Replace("\"", "\\\"");
                var prioritiesStr = string.Join(", ", priorities);
                sb.AppendLine($"                [\"{escapedPattern}\"] = new[] {{ {prioritiesStr} }},");
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private string SanitizeLanguageCode(string languageCode)
        {
            // Convert language codes to valid C# method name parts
            // e.g., "en-US" -> "EnUS", "de-DE" -> "DeDE"
            return languageCode.Replace("-", "");
        }

        private class LanguagePatterns
        {
            public string LanguageCode { get; set; } = "";
            public Dictionary<string, int[]> Patterns { get; set; } = new();
        }
    }
}
