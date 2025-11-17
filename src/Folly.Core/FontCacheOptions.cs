using System;
using Folly.Core.Logging;

namespace Folly;

/// <summary>
/// Configuration options for font caching and system font discovery.
/// </summary>
public class FontCacheOptions
{
    /// <summary>
    /// Gets or sets the logger for font-related diagnostic messages, warnings, and errors.
    /// If not set, a null logger is used (messages are discarded).
    /// </summary>
    public ILogger Logger { get; set; } = NullLogger.Instance;
    /// <summary>
    /// Gets or sets the maximum number of system fonts to cache.
    /// When the cache exceeds this limit, least recently used fonts will be evicted.
    /// Default is 500 fonts.
    /// Set to 0 for unlimited cache size (not recommended for systems with many fonts).
    /// </summary>
    public int MaxCachedFonts { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum time allowed for scanning system fonts.
    /// If the scan exceeds this timeout, it will be aborted and cached fonts will be used.
    /// Default is 10 seconds.
    /// Set to 0 to disable timeout (not recommended).
    /// </summary>
    public TimeSpan ScanTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets whether to enable persistent font cache.
    /// When enabled, discovered fonts are saved to disk and reloaded on subsequent runs
    /// for faster startup. Cache file is stored at ~/.folly/font-cache.json.
    /// Default is true.
    /// </summary>
    public bool EnablePersistentCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum age of the persistent cache before it's considered stale.
    /// If the cache file is older than this duration, a new scan will be performed.
    /// Default is 7 days.
    /// </summary>
    public TimeSpan CacheMaxAge { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets the directory where the persistent font cache is stored.
    /// Default is ~/.folly/ (user's home directory + .folly folder).
    /// </summary>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether to use platform-specific font discovery optimizations.
    /// - Windows: Query font registry instead of filesystem scanning
    /// - Linux: Use fontconfig (fc-list) if available
    /// - macOS: Use CoreText APIs if available
    /// Default is true.
    /// Falls back to filesystem scanning if platform-specific APIs fail.
    /// </summary>
    public bool UsePlatformOptimizations { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of font file bytes to cache in memory.
    /// This applies to the PdfWriter font data cache.
    /// When the cache exceeds this limit, least recently used font data will be evicted.
    /// Default is 100 MB (104,857,600 bytes).
    /// Set to 0 to disable font data caching.
    /// </summary>
    public long MaxFontDataCacheSize { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// Gets or sets an optional callback for receiving diagnostic messages from the font system.
    /// Use this to log cache load failures, scan timeouts, and other diagnostic events.
    /// Default is null (no diagnostics).
    /// Example: options.DiagnosticCallback = msg => Console.WriteLine($"[Font] {msg}");
    /// </summary>
    /// <remarks>
    /// This callback is invoked for non-critical diagnostic information such as:
    /// - Persistent cache load failures
    /// - Persistent cache save failures
    /// - Font file parse errors during scanning
    /// - System font scan timeouts
    /// This is designed for debugging and monitoring - the font system will continue
    /// to function normally even when these events occur.
    /// </remarks>
    public Action<string>? DiagnosticCallback { get; set; }
}
