using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Folly.SourceGenerators.Glyphs
{
    /// <summary>
    /// Source generator that creates a mapping from Unicode code points to Adobe Glyph List names.
    /// </summary>
    [Generator]
    public class AdobeGlyphListGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                // Get the glyphlist.txt from additional files
                var glyphListFile = context.AdditionalFiles.FirstOrDefault(f =>
                    Path.GetFileName(f.Path).Equals("glyphlist.txt", StringComparison.OrdinalIgnoreCase));

                Dictionary<int, string> glyphMappings;

                if (glyphListFile == null)
                {
                    // Fallback: try to load from embedded resource
                    var embeddedMappings = LoadFromEmbeddedResource(context);
                    if (embeddedMappings != null && embeddedMappings.Count > 0)
                    {
                        glyphMappings = embeddedMappings;
                    }
                    else
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "FOLLY001",
                                "Glyph list not found",
                                "Could not find glyphlist.txt in additional files or embedded resources",
                                "SourceGenerator",
                                DiagnosticSeverity.Warning,
                                true),
                            Location.None));
                        return;
                    }
                }
                else
                {
                    var text = glyphListFile.GetText(context.CancellationToken);
                    if (text == null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "FOLLY002",
                                "Could not read glyph list",
                                "Could not read text from glyphlist.txt",
                                "SourceGenerator",
                                DiagnosticSeverity.Error,
                                true),
                            Location.None));
                        return;
                    }

                    using var reader = new StringReader(text.ToString());
                    glyphMappings = ParseGlyphList(reader);
                }

                // Generate the source code
                var sourceCode = GenerateSource(glyphMappings);
                context.AddSource("AdobeGlyphList.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FOLLY003",
                        "Generation error",
                        $"Error generating Adobe Glyph List: {ex.Message}\n{ex.StackTrace}",
                        "SourceGenerator",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
            }
        }

        private Dictionary<int, string>? LoadFromEmbeddedResource(GeneratorExecutionContext context)
        {
            try
            {
                // Try to read the embedded resource
                var assembly = typeof(AdobeGlyphListGenerator).Assembly;
                var resourceName = "Folly.SourceGenerators.Glyphs.glyphlist.txt";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    return null;

                using var reader = new StreamReader(stream);
                return ParseGlyphList(reader);
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FOLLY004",
                        "Embedded resource load failed",
                        $"Failed to load glyphlist.txt from embedded resources: {ex.Message}",
                        "SourceGenerator",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));
                return null;
            }
        }

        private Dictionary<int, string> ParseGlyphList(TextReader reader)
        {
            var mappings = new Dictionary<int, string>();
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                // Parse: glyphname;unicodevalue(s)
                var parts = line.Split(';');
                if (parts.Length != 2)
                    continue;

                var glyphName = parts[0].Trim();
                var unicodePart = parts[1].Trim();

                // Handle multiple Unicode values (space-separated)
                // We'll only use the first one for our purposes
                var unicodeValues = unicodePart.Split(' ');
                foreach (var unicodeHex in unicodeValues)
                {
                    if (int.TryParse(unicodeHex, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
                    {
                        // Only map if we don't already have this code point
                        // (First mapping wins - this matches PDF spec behavior)
                        if (!mappings.ContainsKey(codePoint))
                        {
                            mappings[codePoint] = glyphName;
                        }
                    }
                }
            }

            return mappings;
        }

        private string GenerateSource(Dictionary<int, string> mappings)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("// Generated from Adobe Glyph List (https://github.com/adobe-type-tools/agl-aglfn)");
            sb.AppendLine();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace Folly.Pdf");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Adobe Glyph List mappings from Unicode code points to PostScript glyph names.");
            sb.AppendLine($"    /// Contains {mappings.Count} glyph name mappings.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    internal static partial class AdobeGlyphList");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets the PostScript glyph name for a Unicode code point.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static bool TryGetGlyphName(int codePoint, out string glyphName)");
            sb.AppendLine("        {");
            sb.AppendLine("            return GlyphNames.TryGetValue(codePoint, out glyphName);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static readonly Dictionary<int, string> GlyphNames = new Dictionary<int, string>");
            sb.AppendLine("        {");

            // Sort by code point for better readability and potential performance
            var sortedMappings = mappings.OrderBy(kvp => kvp.Key);

            foreach (var mapping in sortedMappings)
            {
                // Escape the glyph name for C# string literal
                var escapedName = mapping.Value.Replace("\\", "\\\\").Replace("\"", "\\\"");
                sb.AppendLine($"            [{mapping.Key}] = \"{escapedName}\",");
            }

            sb.AppendLine("        };");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
