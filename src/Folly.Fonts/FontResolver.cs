using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

namespace Folly.Fonts;

/// <summary>
/// Resolves font families with fallback support and system font discovery.
/// Handles font family stacks like "Roboto, Arial, Helvetica, sans-serif".
/// </summary>
public class FontResolver
{
    private readonly Dictionary<string, string> _customFonts;
    private readonly Dictionary<string, string> _systemFontCache;
    private readonly Dictionary<string, string> _genericFamilyMap;
    private readonly object _scanLock = new object();
    private volatile bool _systemFontsScanned;

    /// <summary>
    /// Creates a new font resolver for font-family stack resolution.
    /// </summary>
    /// <param name="customFonts">Dictionary mapping font family names to TTF file paths.</param>
    public FontResolver(Dictionary<string, string>? customFonts = null)
    {
        _customFonts = customFonts ?? new Dictionary<string, string>();
        _systemFontCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _genericFamilyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _systemFontsScanned = false;

        InitializeGenericFamilyMap();
    }

    /// <summary>
    /// Resolves a font family stack to an actual font file path.
    /// Tries each family in order: custom fonts, system fonts, generic families.
    /// </summary>
    /// <param name="fontFamilyStack">Comma-separated list of font families (e.g., "Roboto, Arial, sans-serif")</param>
    /// <returns>Font file path if found, null otherwise</returns>
    public string? ResolveFontFamily(string fontFamilyStack)
    {
        if (string.IsNullOrWhiteSpace(fontFamilyStack))
            return null;

        // Split and trim font families
        var families = fontFamilyStack
            .Split(',')
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f));

        foreach (var family in families)
        {
            // Try custom fonts first
            if (_customFonts.TryGetValue(family, out var customPath))
            {
                if (File.Exists(customPath))
                    return customPath;
            }

            // Try system fonts (scan if needed with double-checked locking)
            if (!_systemFontsScanned)
            {
                lock (_scanLock)
                {
                    if (!_systemFontsScanned)
                    {
                        ScanSystemFonts();
                        _systemFontsScanned = true;
                    }
                }
            }

            if (_systemFontCache.TryGetValue(family, out var systemPath))
            {
                return systemPath;
            }

            // Try generic family mapping (with recursion protection)
            if (_genericFamilyMap.TryGetValue(family, out var genericFamily))
            {
                // Recursively resolve the generic family
                // Note: Recursion depth is limited by generic family map size (typically 5 entries)
                // and infinite loops are prevented by the fact that generic families don't
                // reference each other in the initialization
                var resolvedPath = ResolveFontFamily(genericFamily);
                if (resolvedPath != null)
                    return resolvedPath;
            }
        }

        // No font found in the stack
        return null;
    }

    /// <summary>
    /// Scans system font directories for available fonts.
    /// </summary>
    private void ScanSystemFonts()
    {
        var fontDirs = GetSystemFontDirectories();

        foreach (var dir in fontDirs)
        {
            if (!Directory.Exists(dir))
                continue;

            try
            {
                var fontFiles = Directory.GetFiles(dir, "*.ttf", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(dir, "*.otf", SearchOption.AllDirectories));

                foreach (var fontFile in fontFiles)
                {
                    try
                    {
                        // Parse font to get family name
                        var font = FontParser.Parse(fontFile);
                        var familyName = font.FamilyName;

                        if (!string.IsNullOrEmpty(familyName))
                        {
                            // Store first occurrence only (prefer earlier directories)
                            if (!_systemFontCache.ContainsKey(familyName))
                            {
                                _systemFontCache[familyName] = fontFile;
                            }
                        }
                    }
                    catch (Exception ex) when (
                        ex is IOException ||
                        ex is UnauthorizedAccessException ||
                        ex is InvalidDataException ||
                        ex is NotSupportedException)
                    {
                        // Skip fonts that fail to parse (corrupted files, unsupported formats, access denied, etc.)
                        // Silently continue to next font - this is expected for some system files
                    }
                }
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException)
            {
                // Skip directories that fail to enumerate (access denied, network issues, etc.)
                // Silently continue to next directory - this is expected for some system directories
            }
        }
    }

    /// <summary>
    /// Gets system font directories based on the current OS.
    /// </summary>
    private static List<string> GetSystemFontDirectories()
    {
        var dirs = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows font directories
            var windir = Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows";
            dirs.Add(Path.Combine(windir, "Fonts"));

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            dirs.Add(Path.Combine(localAppData, "Microsoft", "Windows", "Fonts"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS font directories
            dirs.Add("/Library/Fonts");
            dirs.Add("/System/Library/Fonts");

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            dirs.Add(Path.Combine(home, "Library", "Fonts"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux font directories
            dirs.Add("/usr/share/fonts");
            dirs.Add("/usr/local/share/fonts");

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            dirs.Add(Path.Combine(home, ".fonts"));
            dirs.Add(Path.Combine(home, ".local", "share", "fonts"));
        }

        return dirs;
    }

    /// <summary>
    /// Initializes the generic family mapping.
    /// Maps generic families to common font families available on most systems.
    /// </summary>
    private void InitializeGenericFamilyMap()
    {
        // sans-serif
        _genericFamilyMap["sans-serif"] = "Arial, Helvetica, Liberation Sans, DejaVu Sans, FreeSans";

        // serif
        _genericFamilyMap["serif"] = "Times New Roman, Times, Liberation Serif, DejaVu Serif, FreeSerif";

        // monospace
        _genericFamilyMap["monospace"] = "Courier New, Courier, Liberation Mono, DejaVu Sans Mono, FreeMono";

        // cursive
        _genericFamilyMap["cursive"] = "Comic Sans MS, Apple Chancery, Brush Script MT";

        // fantasy
        _genericFamilyMap["fantasy"] = "Impact, Papyrus, Copperplate";
    }

    /// <summary>
    /// Gets all available system fonts.
    /// </summary>
    /// <returns>Dictionary mapping font family names to file paths</returns>
    public Dictionary<string, string> GetAvailableSystemFonts()
    {
        if (!_systemFontsScanned)
        {
            ScanSystemFonts();
            _systemFontsScanned = true;
        }

        return new Dictionary<string, string>(_systemFontCache);
    }

    /// <summary>
    /// Checks if a specific font family is available.
    /// </summary>
    /// <param name="familyName">Font family name to check</param>
    /// <returns>True if the font is available (custom or system), false otherwise</returns>
    public bool IsFontAvailable(string familyName)
    {
        if (string.IsNullOrWhiteSpace(familyName))
            return false;

        // Check custom fonts
        if (_customFonts.ContainsKey(familyName))
            return true;

        // Check system fonts
        if (!_systemFontsScanned)
        {
            ScanSystemFonts();
            _systemFontsScanned = true;
        }

        return _systemFontCache.ContainsKey(familyName);
    }

    /// <summary>
    /// Clears the system font cache, forcing a rescan on next use.
    /// </summary>
    public void ClearCache()
    {
        _systemFontCache.Clear();
        _systemFontsScanned = false;
    }
}
