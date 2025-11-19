using System.Text;

namespace Folly.Xslfo.Layout.Tests.Helpers;

/// <summary>
/// Helper utilities for extracting and analyzing PDF content in tests.
/// </summary>
public static class PdfContentHelper
{
    /// <summary>
    /// Extracts the PDF header line (e.g., "%PDF-1.7").
    /// </summary>
    public static string GetPdfHeader(byte[] pdfBytes)
    {
        if (pdfBytes == null || pdfBytes.Length < 8)
            return string.Empty;

        // PDF header is first line: "%PDF-X.Y"
        var headerBytes = pdfBytes.Take(8).ToArray();
        return Encoding.ASCII.GetString(headerBytes);
    }

    /// <summary>
    /// Extracts the full PDF content as a string for text searching.
    /// </summary>
    /// <remarks>
    /// This is useful for finding PDF operators, keywords, and uncompressed content.
    /// Compressed streams will appear as binary data.
    /// </remarks>
    public static string GetPdfContent(byte[] pdfBytes)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
            return string.Empty;

        // Convert entire PDF to string (binary-safe ASCII)
        return Encoding.Latin1.GetString(pdfBytes);
    }

    /// <summary>
    /// Extracts content stream for a specific page.
    /// </summary>
    /// <param name="pdfBytes">The PDF file bytes.</param>
    /// <param name="pageIndex">Zero-based page index (default: 0 for first page).</param>
    /// <returns>The page content stream as a string.</returns>
    /// <remarks>
    /// This is a simplified implementation that searches for stream markers.
    /// For compressed streams, content may not be readable.
    /// </remarks>
    public static string GetPageContent(byte[] pdfBytes, int pageIndex = 0)
    {
        var content = GetPdfContent(pdfBytes);

        // Simple heuristic: find stream/endstream markers
        // This is not a full PDF parser, just good enough for basic tests
        var streamMarkers = new List<(int start, int end)>();
        var streamIndex = 0;

        while ((streamIndex = content.IndexOf("stream\n", streamIndex, StringComparison.Ordinal)) != -1)
        {
            var endStreamIndex = content.IndexOf("\nendstream", streamIndex, StringComparison.Ordinal);
            if (endStreamIndex != -1)
            {
                streamMarkers.Add((streamIndex + 7, endStreamIndex)); // +7 to skip "stream\n"
                streamIndex = endStreamIndex;
            }
            else
            {
                break;
            }
        }

        // Return the requested page stream (if exists)
        if (pageIndex < streamMarkers.Count)
        {
            var (start, end) = streamMarkers[pageIndex];
            return content.Substring(start, end - start);
        }

        return string.Empty;
    }

    /// <summary>
    /// Counts the number of occurrences of a pattern in a string.
    /// </summary>
    public static int CountOccurrences(string content, string pattern)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(pattern))
            return 0;

        var count = 0;
        var index = 0;

        while ((index = content.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    /// <summary>
    /// Checks if the PDF contains a specific PDF operator.
    /// </summary>
    /// <param name="pdfBytes">The PDF file bytes.</param>
    /// <param name="pdfOperator">The PDF operator to search for (e.g., "W", "n", "re").</param>
    /// <returns>True if the operator is found in the PDF content.</returns>
    /// <remarks>
    /// This searches for the operator as a standalone word (surrounded by whitespace).
    /// PDF operators are case-sensitive.
    /// </remarks>
    public static bool ContainsPdfOperator(byte[] pdfBytes, string pdfOperator)
    {
        var content = GetPdfContent(pdfBytes);

        // Look for the operator surrounded by whitespace or newlines
        // This prevents false positives (e.g., "W" in "Width")
        var patterns = new[]
        {
            $" {pdfOperator}\n",
            $" {pdfOperator} ",
            $"\n{pdfOperator}\n",
            $"\n{pdfOperator} "
        };

        return patterns.Any(pattern => content.Contains(pattern));
    }

    /// <summary>
    /// Checks if the PDF contains a specific keyword or text.
    /// </summary>
    /// <param name="pdfBytes">The PDF file bytes.</param>
    /// <param name="keyword">The keyword to search for.</param>
    /// <param name="caseSensitive">Whether the search should be case-sensitive (default: true).</param>
    /// <returns>True if the keyword is found.</returns>
    public static bool ContainsKeyword(byte[] pdfBytes, string keyword, bool caseSensitive = true)
    {
        var content = GetPdfContent(pdfBytes);

        return caseSensitive
            ? content.Contains(keyword)
            : content.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts all PDF objects of a specific type (e.g., "/Font", "/XObject", "/Page").
    /// </summary>
    /// <param name="pdfBytes">The PDF file bytes.</param>
    /// <param name="objectType">The object type to search for (e.g., "/Font").</param>
    /// <returns>Count of objects of the specified type.</returns>
    /// <remarks>
    /// This is a simple pattern matcher, not a full PDF parser.
    /// </remarks>
    public static int CountObjectType(byte[] pdfBytes, string objectType)
    {
        var content = GetPdfContent(pdfBytes);
        return CountOccurrences(content, objectType);
    }

    /// <summary>
    /// Checks if a PDF is a valid PDF file (starts with %PDF header).
    /// </summary>
    public static bool IsValidPdf(byte[] pdfBytes)
    {
        if (pdfBytes == null || pdfBytes.Length < 5)
            return false;

        var header = Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        return header == "%PDF-";
    }

    /// <summary>
    /// Extracts the PDF version (e.g., "1.7") from the header.
    /// </summary>
    public static string? GetPdfVersion(byte[] pdfBytes)
    {
        var header = GetPdfHeader(pdfBytes);

        if (header.StartsWith("%PDF-") && header.Length >= 8)
        {
            return header.Substring(5, 3); // Extract "1.7" or "1.4", etc.
        }

        return null;
    }
}
