using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Folly.Fonts;

/// <summary>
/// Manages persistent storage of discovered system fonts to disk.
/// Provides fast cold-start performance by avoiding expensive font scanning.
/// </summary>
public class PersistentFontCache
{
    private const string CacheFileName = "font-cache.json";

    /// <summary>
    /// Attempts to load the font cache from disk.
    /// </summary>
    /// <param name="cacheDirectory">Directory containing the cache file.</param>
    /// <param name="maxAge">Maximum age before cache is considered stale.</param>
    /// <param name="diagnosticCallback">Optional callback for diagnostic messages.</param>
    /// <returns>Dictionary of font family names to paths, or null if cache is invalid/missing/stale.</returns>
    public static Dictionary<string, string>? TryLoad(string cacheDirectory, TimeSpan maxAge, Action<string>? diagnosticCallback = null)
    {
        try
        {
            var cacheFilePath = Path.Combine(cacheDirectory, CacheFileName);

            if (!File.Exists(cacheFilePath))
            {
                diagnosticCallback?.Invoke($"Font cache not found at {cacheFilePath}, will perform full scan");
                return null;
            }

            // Check if cache is stale
            var fileInfo = new FileInfo(cacheFilePath);
            var age = DateTime.UtcNow - fileInfo.LastWriteTimeUtc;
            if (age > maxAge)
            {
                diagnosticCallback?.Invoke($"Font cache is stale (age: {age.TotalDays:F1} days, max: {maxAge.TotalDays:F1} days), will perform full scan");
                return null;
            }

            // Read and deserialize cache
            var json = File.ReadAllText(cacheFilePath);
            var cacheData = JsonSerializer.Deserialize<FontCacheData>(json);

            if (cacheData == null || cacheData.Fonts == null)
            {
                diagnosticCallback?.Invoke($"Font cache file is invalid or empty, will perform full scan");
                return null;
            }

            // Validate that font files still exist
            var validatedFonts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int invalidCount = 0;
            foreach (var kvp in cacheData.Fonts)
            {
                if (File.Exists(kvp.Value))
                {
                    validatedFonts[kvp.Key] = kvp.Value;
                }
                else
                {
                    invalidCount++;
                }
            }

            if (invalidCount > 0)
            {
                diagnosticCallback?.Invoke($"Font cache validation: {invalidCount} font(s) no longer exist, {validatedFonts.Count} font(s) loaded");
            }
            else
            {
                diagnosticCallback?.Invoke($"Font cache loaded successfully: {validatedFonts.Count} font(s)");
            }

            return validatedFonts;
        }
        catch (Exception ex) when (
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is JsonException ||
            ex is NotSupportedException)
        {
            // If cache load fails for any reason, return null to trigger rescan
            diagnosticCallback?.Invoke($"Font cache load failed ({ex.GetType().Name}: {ex.Message}), will perform full scan");
            return null;
        }
    }

    /// <summary>
    /// Saves the font cache to disk.
    /// </summary>
    /// <param name="cacheDirectory">Directory to save the cache file.</param>
    /// <param name="fonts">Dictionary of font family names to paths.</param>
    /// <param name="diagnosticCallback">Optional callback for diagnostic messages.</param>
    /// <returns>True if save succeeded, false otherwise.</returns>
    public static bool TrySave(string cacheDirectory, Dictionary<string, string> fonts, Action<string>? diagnosticCallback = null)
    {
        try
        {
            // Create cache directory if it doesn't exist
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            var cacheFilePath = Path.Combine(cacheDirectory, CacheFileName);

            var cacheData = new FontCacheData
            {
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                Fonts = fonts
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(cacheData, options);
            File.WriteAllText(cacheFilePath, json);

            diagnosticCallback?.Invoke($"Font cache saved successfully: {fonts.Count} font(s) to {cacheFilePath}");
            return true;
        }
        catch (Exception ex) when (
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is JsonException ||
            ex is NotSupportedException)
        {
            // Silently fail - persistent cache is optional
            diagnosticCallback?.Invoke($"Font cache save failed ({ex.GetType().Name}: {ex.Message})");
            return false;
        }
    }

    /// <summary>
    /// Gets the default cache directory path (~/.folly/).
    /// </summary>
    /// <returns>Default cache directory path.</returns>
    public static string GetDefaultCacheDirectory()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDirectory, ".folly");
    }

    /// <summary>
    /// Data structure for serializing font cache to JSON.
    /// </summary>
    private class FontCacheData
    {
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, string> Fonts { get; set; } = new();
    }
}
