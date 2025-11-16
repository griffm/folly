using System;
using System.Collections.Generic;
using System.IO;

namespace Folly.Fonts;

/// <summary>
/// Thread-safe LRU cache for font file data with size-based eviction.
/// Caches font file bytes to avoid repeated disk reads for the same fonts.
/// </summary>
public class FontDataCache
{
    private readonly long _maxCacheSizeBytes;
    private readonly Dictionary<string, LinkedListNode<CacheEntry>> _cache;
    private readonly LinkedList<CacheEntry> _lruList;
    private readonly object _lock = new object();
    private long _currentCacheSizeBytes;

    /// <summary>
    /// Creates a new font data cache with the specified size limit.
    /// </summary>
    /// <param name="maxCacheSizeBytes">Maximum cache size in bytes. Set to 0 to disable caching.</param>
    public FontDataCache(long maxCacheSizeBytes)
    {
        if (maxCacheSizeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(maxCacheSizeBytes), "Cache size cannot be negative.");

        _maxCacheSizeBytes = maxCacheSizeBytes;
        _cache = new Dictionary<string, LinkedListNode<CacheEntry>>(StringComparer.OrdinalIgnoreCase);
        _lruList = new LinkedList<CacheEntry>();
        _currentCacheSizeBytes = 0;
    }

    /// <summary>
    /// Gets the current size of the cache in bytes.
    /// </summary>
    public long CurrentSize
    {
        get
        {
            lock (_lock)
            {
                return _currentCacheSizeBytes;
            }
        }
    }

    /// <summary>
    /// Gets the maximum cache size in bytes.
    /// </summary>
    public long MaxSize => _maxCacheSizeBytes;

    /// <summary>
    /// Gets the number of font files currently cached.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    /// <summary>
    /// Attempts to get font data from the cache.
    /// </summary>
    /// <param name="fontPath">The font file path (case-insensitive).</param>
    /// <param name="fontData">The cached font data if found.</param>
    /// <returns>True if the font data was found in cache, false otherwise.</returns>
    public bool TryGetFontData(string fontPath, out byte[] fontData)
    {
        if (_maxCacheSizeBytes == 0)
        {
            fontData = Array.Empty<byte>();
            return false;
        }

        lock (_lock)
        {
            if (_cache.TryGetValue(fontPath, out var node))
            {
                // Move to front (most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);

                fontData = node.Value.Data;
                return true;
            }

            fontData = Array.Empty<byte>();
            return false;
        }
    }

    /// <summary>
    /// Adds or updates font data in the cache.
    /// If adding the data would exceed the cache size, least recently used entries are evicted.
    /// </summary>
    /// <param name="fontPath">The font file path (case-insensitive).</param>
    /// <param name="fontData">The font data to cache.</param>
    public void AddOrUpdate(string fontPath, byte[] fontData)
    {
        if (_maxCacheSizeBytes == 0)
            return;

        var dataSize = fontData.Length;

        // If the font itself is larger than the cache, don't cache it
        if (dataSize > _maxCacheSizeBytes)
            return;

        lock (_lock)
        {
            // If the font is already cached, update it
            if (_cache.TryGetValue(fontPath, out var existingNode))
            {
                var oldSize = existingNode.Value.Data.Length;
                _currentCacheSizeBytes -= oldSize;
                _lruList.Remove(existingNode);

                existingNode.Value.Data = fontData;
                _lruList.AddFirst(existingNode);
                _currentCacheSizeBytes += dataSize;

                return;
            }

            // Evict entries until we have enough space
            while (_currentCacheSizeBytes + dataSize > _maxCacheSizeBytes && _lruList.Count > 0)
            {
                var lruNode = _lruList.Last;
                if (lruNode != null)
                {
                    _currentCacheSizeBytes -= lruNode.Value.Data.Length;
                    _lruList.RemoveLast();
                    _cache.Remove(lruNode.Value.Path);
                }
            }

            // Add new entry
            var entry = new CacheEntry(fontPath, fontData);
            var node = new LinkedListNode<CacheEntry>(entry);
            _lruList.AddFirst(node);
            _cache[fontPath] = node;
            _currentCacheSizeBytes += dataSize;
        }
    }

    /// <summary>
    /// Loads font data from disk, using the cache if available.
    /// </summary>
    /// <param name="fontPath">Path to the font file.</param>
    /// <returns>Font data bytes.</returns>
    public byte[] LoadFontData(string fontPath)
    {
        // Try cache first
        if (TryGetFontData(fontPath, out var cachedData))
        {
            return cachedData;
        }

        // Load from disk
        var fontData = File.ReadAllBytes(fontPath);

        // Add to cache
        AddOrUpdate(fontPath, fontData);

        return fontData;
    }

    /// <summary>
    /// Clears all cached font data.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _lruList.Clear();
            _currentCacheSizeBytes = 0;
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Cache statistics.</returns>
    public FontDataCacheStatistics GetStatistics()
    {
        lock (_lock)
        {
            return new FontDataCacheStatistics
            {
                CachedFontCount = _cache.Count,
                CurrentCacheSizeBytes = _currentCacheSizeBytes,
                MaxCacheSizeBytes = _maxCacheSizeBytes,
                CacheUtilizationPercent = _maxCacheSizeBytes > 0
                    ? (double)_currentCacheSizeBytes / _maxCacheSizeBytes * 100
                    : 0
            };
        }
    }

    /// <summary>
    /// Internal cache entry structure.
    /// </summary>
    private class CacheEntry
    {
        public string Path { get; }
        public byte[] Data { get; set; }

        public CacheEntry(string path, byte[] data)
        {
            Path = path;
            Data = data;
        }
    }
}

/// <summary>
/// Statistics about the font data cache.
/// </summary>
public class FontDataCacheStatistics
{
    /// <summary>
    /// Number of fonts currently cached.
    /// </summary>
    public int CachedFontCount { get; set; }

    /// <summary>
    /// Current cache size in bytes.
    /// </summary>
    public long CurrentCacheSizeBytes { get; set; }

    /// <summary>
    /// Maximum cache size in bytes.
    /// </summary>
    public long MaxCacheSizeBytes { get; set; }

    /// <summary>
    /// Cache utilization as a percentage (0-100).
    /// </summary>
    public double CacheUtilizationPercent { get; set; }
}
