using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Folly;
using Folly.Fonts;
using Xunit;

namespace Folly.FontTests;

/// <summary>
/// Tests for font caching performance optimizations (Phase 8.5).
/// </summary>
public class FontCachePerformanceTests
{
    [Fact]
    public void LruCache_AddsAndRetrievesItems()
    {
        var cache = new LruCache<string, string>(3);

        cache.AddOrUpdate("key1", "value1");
        cache.AddOrUpdate("key2", "value2");
        cache.AddOrUpdate("key3", "value3");

        Assert.True(cache.TryGetValue("key1", out var value1));
        Assert.Equal("value1", value1);

        Assert.True(cache.TryGetValue("key2", out var value2));
        Assert.Equal("value2", value2);

        Assert.Equal(3, cache.Count);
    }

    [Fact]
    public void LruCache_EvictsLeastRecentlyUsed()
    {
        var cache = new LruCache<string, string>(3);

        cache.AddOrUpdate("key1", "value1");
        cache.AddOrUpdate("key2", "value2");
        cache.AddOrUpdate("key3", "value3");

        // Access key1 to make it recently used
        cache.TryGetValue("key1", out _);

        // Add key4, should evict key2 (least recently used)
        cache.AddOrUpdate("key4", "value4");

        Assert.True(cache.ContainsKey("key1")); // Recently used
        Assert.False(cache.ContainsKey("key2")); // Evicted
        Assert.True(cache.ContainsKey("key3"));
        Assert.True(cache.ContainsKey("key4"));
    }

    [Fact]
    public void LruCache_UpdateExistingKey()
    {
        var cache = new LruCache<string, string>(3);

        cache.AddOrUpdate("key1", "value1");
        cache.AddOrUpdate("key1", "value1-updated");

        Assert.True(cache.TryGetValue("key1", out var value));
        Assert.Equal("value1-updated", value);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public async Task LruCache_ThreadSafety()
    {
        var cache = new LruCache<int, int>(100);
        var tasks = new List<Task>();

        // Run 10 concurrent threads adding/reading from cache
        for (int i = 0; i < 10; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    var key = threadId * 100 + j;
                    cache.AddOrUpdate(key, key * 2);
                    cache.TryGetValue(key, out _);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Should not crash and should have some items in cache
        Assert.True(cache.Count > 0);
        Assert.True(cache.Count <= 100); // Respects capacity
    }

    [Fact]
    public void LruCache_UnlimitedCapacity()
    {
        var cache = new LruCache<string, string>(0); // 0 = unlimited

        for (int i = 0; i < 1000; i++)
        {
            cache.AddOrUpdate($"key{i}", $"value{i}");
        }

        Assert.Equal(1000, cache.Count);
    }

    [Fact]
    public void LruCache_CaseInsensitiveKeys()
    {
        var cache = new LruCache<string, string>(10, StringComparer.OrdinalIgnoreCase);

        cache.AddOrUpdate("Key1", "value1");

        Assert.True(cache.TryGetValue("key1", out var value));
        Assert.Equal("value1", value);

        Assert.True(cache.TryGetValue("KEY1", out var value2));
        Assert.Equal("value1", value2);
    }

    [Fact]
    public void FontDataCache_CachesAndRetrievesFontData()
    {
        var cache = new FontDataCache(1024 * 1024); // 1MB cache

        var fontData = new byte[1000];
        for (int i = 0; i < fontData.Length; i++)
            fontData[i] = (byte)(i % 256);

        cache.AddOrUpdate("/path/to/font.ttf", fontData);

        Assert.True(cache.TryGetFontData("/path/to/font.ttf", out var retrieved));
        Assert.Equal(fontData.Length, retrieved.Length);
        Assert.Equal(fontData[0], retrieved[0]);
        Assert.Equal(fontData[999], retrieved[999]);
    }

    [Fact]
    public void FontDataCache_EvictsLargestWhenFull()
    {
        var cache = new FontDataCache(3000); // 3KB cache

        var font1 = new byte[1000]; // 1KB
        var font2 = new byte[1000]; // 1KB
        var font3 = new byte[1000]; // 1KB

        cache.AddOrUpdate("font1.ttf", font1);
        cache.AddOrUpdate("font2.ttf", font2);
        cache.AddOrUpdate("font3.ttf", font3);

        Assert.Equal(3, cache.Count);

        // Add another 1KB font - should evict least recently used (font1)
        var font4 = new byte[1000];
        cache.AddOrUpdate("font4.ttf", font4);

        Assert.False(cache.TryGetFontData("font1.ttf", out _)); // Evicted
        Assert.True(cache.TryGetFontData("font2.ttf", out _));
        Assert.True(cache.TryGetFontData("font3.ttf", out _));
        Assert.True(cache.TryGetFontData("font4.ttf", out _));
    }

    [Fact]
    public void FontDataCache_DoesNotCacheFontsLargerThanCacheSize()
    {
        var cache = new FontDataCache(1000); // 1KB cache

        var largeFontData = new byte[2000]; // 2KB font (larger than cache)

        cache.AddOrUpdate("large-font.ttf", largeFontData);

        // Should not be cached
        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGetFontData("large-font.ttf", out _));
    }

    [Fact]
    public void FontDataCache_DisabledWhenMaxSizeIsZero()
    {
        var cache = new FontDataCache(0); // Caching disabled

        var fontData = new byte[1000];
        cache.AddOrUpdate("font.ttf", fontData);

        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGetFontData("font.ttf", out _));
    }

    [Fact]
    public void FontDataCache_TracksCurrentSize()
    {
        var cache = new FontDataCache(10000); // 10KB cache

        var font1 = new byte[1000]; // 1KB
        var font2 = new byte[2000]; // 2KB

        cache.AddOrUpdate("font1.ttf", font1);
        Assert.Equal(1000, cache.CurrentSize);

        cache.AddOrUpdate("font2.ttf", font2);
        Assert.Equal(3000, cache.CurrentSize);

        cache.Clear();
        Assert.Equal(0, cache.CurrentSize);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public async Task FontDataCache_ThreadSafety()
    {
        var cache = new FontDataCache(100 * 1024); // 100KB cache
        var tasks = new List<Task>();

        // Run 10 concurrent threads adding/reading font data
        for (int i = 0; i < 10; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    var fontData = new byte[1000];
                    var key = $"font-{threadId}-{j}.ttf";
                    cache.AddOrUpdate(key, fontData);
                    cache.TryGetFontData(key, out _);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Should not crash
        Assert.True(cache.Count > 0);
    }

    [Fact]
    public void FontCacheOptions_HasReasonableDefaults()
    {
        var options = new FontCacheOptions();

        Assert.Equal(500, options.MaxCachedFonts);
        Assert.Equal(TimeSpan.FromSeconds(10), options.ScanTimeout);
        Assert.True(options.EnablePersistentCache);
        Assert.Equal(TimeSpan.FromDays(7), options.CacheMaxAge);
        Assert.True(options.UsePlatformOptimizations);
        Assert.Equal(100 * 1024 * 1024, options.MaxFontDataCacheSize);
    }

    [Fact]
    public async Task FontResolver_ThreadSafety_ConcurrentResolution()
    {
        var options = new FontCacheOptions
        {
            MaxCachedFonts = 100,
            ScanTimeout = TimeSpan.FromSeconds(5),
            EnablePersistentCache = false, // Disable for test
            UsePlatformOptimizations = false // Use filesystem scan for consistency
        };

        var resolver = new FontResolver(null, options);
        var tasks = new List<Task<string?>>();

        // Run 20 concurrent font resolution tasks
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(() => resolver.ResolveFontFamily("Arial, sans-serif")));
        }

        var results = await Task.WhenAll(tasks);

        // Should not crash and all tasks should complete
        Assert.Equal(20, results.Length);

        // All results should be the same (or all null if no fonts available)
        var firstResult = results[0];
        Assert.True(results.All(r => r == firstResult));
    }

    [Fact]
    public void FontResolver_RespectsMaxCachedFonts()
    {
        var options = new FontCacheOptions
        {
            MaxCachedFonts = 10, // Very small cache
            EnablePersistentCache = false,
            ScanTimeout = TimeSpan.FromSeconds(2)
        };

        var resolver = new FontResolver(null, options);

        // Trigger font scan
        resolver.ResolveFontFamily("Arial");

        var stats = resolver.GetCacheStatistics();

        // Should respect max capacity (though actual count depends on system fonts available)
        Assert.True(stats.CachedFontCount <= 10);
        Assert.Equal(10, stats.MaxCacheCapacity);
    }

    [Fact]
    public void FontResolver_ScanTimeoutWorks()
    {
        var options = new FontCacheOptions
        {
            ScanTimeout = TimeSpan.FromMilliseconds(1), // Very short timeout
            EnablePersistentCache = false,
            UsePlatformOptimizations = false // Force filesystem scan
        };

        var resolver = new FontResolver(null, options);
        var stopwatch = Stopwatch.StartNew();

        // This should timeout quickly
        resolver.ResolveFontFamily("Arial");

        stopwatch.Stop();

        // Should complete within a reasonable time (much less than a full scan)
        Assert.True(stopwatch.ElapsedMilliseconds < 2000); // 2 seconds max
    }

    [Fact]
    public void FontResolver_GetCacheStatistics()
    {
        var customFonts = new Dictionary<string, string>
        {
            ["CustomFont1"] = "/path/to/font1.ttf",
            ["CustomFont2"] = "/path/to/font2.ttf"
        };

        var options = new FontCacheOptions
        {
            EnablePersistentCache = false
        };

        var resolver = new FontResolver(customFonts, options);
        var stats = resolver.GetCacheStatistics();

        Assert.Equal(2, stats.CustomFontCount);
        Assert.Equal(500, stats.MaxCacheCapacity); // Default
        Assert.False(stats.IsScanCompleted); // Not scanned yet

        // Trigger scan
        resolver.ResolveFontFamily("Arial");

        stats = resolver.GetCacheStatistics();
        Assert.True(stats.IsScanCompleted);
    }

    [Fact]
    public void PersistentFontCache_GetDefaultCacheDirectory()
    {
        var cacheDir = PersistentFontCache.GetDefaultCacheDirectory();

        Assert.NotNull(cacheDir);
        Assert.Contains(".folly", cacheDir);
    }
}
