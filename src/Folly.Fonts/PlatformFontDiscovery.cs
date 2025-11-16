using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Folly.Fonts;

/// <summary>
/// Platform-specific font discovery optimizations.
/// Uses native APIs and tools for faster font enumeration than filesystem scanning.
/// </summary>
internal static class PlatformFontDiscovery
{
    /// <summary>
    /// Attempts to discover fonts using platform-specific optimizations.
    /// Returns null if platform-specific discovery is not available or fails.
    /// </summary>
    /// <param name="timeoutSeconds">Maximum time to spend discovering fonts.</param>
    /// <returns>Dictionary of font family names to paths, or null if not available.</returns>
    public static Dictionary<string, string>? TryDiscover(int timeoutSeconds)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return DiscoverWindowsFonts(timeoutSeconds);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return DiscoverLinuxFonts(timeoutSeconds);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: Could use CoreText APIs via P/Invoke in the future
                // For now, fall back to filesystem scanning
                return null;
            }

            return null;
        }
        catch
        {
            // If platform-specific discovery fails, return null to fall back to filesystem scan
            return null;
        }
    }

    /// <summary>
    /// Discovers fonts on Windows using registry and filesystem.
    /// </summary>
    private static Dictionary<string, string>? DiscoverWindowsFonts(int timeoutSeconds)
    {
        // Note: We cannot use Microsoft.Win32.Registry on all platforms,
        // so we'll use a hybrid approach: check known Windows font directories
        // and parse font names from the filesystem.
        // A full implementation could use P/Invoke to query the registry directly.

        var fonts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var windir = Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows";
            var fontDir = Path.Combine(windir, "Fonts");

            if (Directory.Exists(fontDir))
            {
                // Scan Windows Fonts directory
                var fontFiles = Directory.GetFiles(fontDir, "*.ttf")
                    .Concat(Directory.GetFiles(fontDir, "*.otf"))
                    .ToArray();

                foreach (var fontFile in fontFiles)
                {
                    try
                    {
                        var font = FontParser.Parse(fontFile);
                        if (!string.IsNullOrEmpty(font.FamilyName))
                        {
                            if (!fonts.ContainsKey(font.FamilyName))
                            {
                                fonts[font.FamilyName] = fontFile;
                            }
                        }
                    }
                    catch
                    {
                        // Skip invalid fonts
                    }
                }
            }

            // Also check user fonts
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userFontDir = Path.Combine(localAppData, "Microsoft", "Windows", "Fonts");

            if (Directory.Exists(userFontDir))
            {
                var userFontFiles = Directory.GetFiles(userFontDir, "*.ttf")
                    .Concat(Directory.GetFiles(userFontDir, "*.otf"))
                    .ToArray();

                foreach (var fontFile in userFontFiles)
                {
                    try
                    {
                        var font = FontParser.Parse(fontFile);
                        if (!string.IsNullOrEmpty(font.FamilyName))
                        {
                            if (!fonts.ContainsKey(font.FamilyName))
                            {
                                fonts[font.FamilyName] = fontFile;
                            }
                        }
                    }
                    catch
                    {
                        // Skip invalid fonts
                    }
                }
            }

            return fonts.Count > 0 ? fonts : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Discovers fonts on Linux using fontconfig (fc-list).
    /// </summary>
    private static Dictionary<string, string>? DiscoverLinuxFonts(int timeoutSeconds)
    {
        try
        {
            // Try to use fc-list for fast font discovery
            var startInfo = new ProcessStartInfo
            {
                FileName = "fc-list",
                Arguments = ": family file",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return null;

            var fonts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var timeoutMs = timeoutSeconds * 1000;

            if (!process.WaitForExit(timeoutMs))
            {
                process.Kill();
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // fc-list output format: /path/to/font.ttf: Family Name:style=Regular
                var parts = line.Split(':');
                if (parts.Length >= 2)
                {
                    var fontPath = parts[0].Trim();
                    var familyName = parts[1].Trim();

                    // Remove style information if present
                    var styleIndex = familyName.IndexOf(":style=", StringComparison.Ordinal);
                    if (styleIndex > 0)
                    {
                        familyName = familyName.Substring(0, styleIndex).Trim();
                    }

                    // Only include TrueType and OpenType fonts
                    if ((fontPath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                         fontPath.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)) &&
                        !string.IsNullOrEmpty(familyName) &&
                        File.Exists(fontPath))
                    {
                        if (!fonts.ContainsKey(familyName))
                        {
                            fonts[familyName] = fontPath;
                        }
                    }
                }
            }

            return fonts.Count > 0 ? fonts : null;
        }
        catch (Exception ex) when (
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is System.ComponentModel.Win32Exception)
        {
            // fc-list not available or failed, fall back to filesystem scan
            return null;
        }
    }

    private static IEnumerable<string> Concat(this IEnumerable<string> first, IEnumerable<string> second)
    {
        foreach (var item in first)
            yield return item;
        foreach (var item in second)
            yield return item;
    }

    private static string[] ToArray(this IEnumerable<string> source)
    {
        var list = new List<string>();
        foreach (var item in source)
            list.Add(item);
        return list.ToArray();
    }
}
