using System;
using System.Collections.Generic;
using System.Linq;

namespace Folly.Fonts;

/// <summary>
/// Thread-safe LRU (Least Recently Used) cache with configurable size limits.
/// When the cache reaches its capacity, the least recently used items are evicted.
/// </summary>
/// <typeparam name="TKey">The type of the cache keys.</typeparam>
/// <typeparam name="TValue">The type of the cache values.</typeparam>
public class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _maxCapacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheEntry>> _cache;
    private readonly LinkedList<CacheEntry> _lruList;
    private readonly object _lock = new object();
    private readonly IEqualityComparer<TKey>? _comparer;

    /// <summary>
    /// Creates a new LRU cache with the specified capacity.
    /// </summary>
    /// <param name="maxCapacity">Maximum number of items to store. Set to 0 for unlimited.</param>
    /// <param name="comparer">Optional key comparer.</param>
    public LruCache(int maxCapacity, IEqualityComparer<TKey>? comparer = null)
    {
        if (maxCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Capacity cannot be negative.");

        _maxCapacity = maxCapacity;
        _comparer = comparer;
        _cache = comparer != null
            ? new Dictionary<TKey, LinkedListNode<CacheEntry>>(comparer)
            : new Dictionary<TKey, LinkedListNode<CacheEntry>>();
        _lruList = new LinkedList<CacheEntry>();
    }

    /// <summary>
    /// Gets the number of items currently in the cache.
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
    /// Gets the maximum capacity of the cache (0 = unlimited).
    /// </summary>
    public int MaxCapacity => _maxCapacity;

    /// <summary>
    /// Attempts to get a value from the cache.
    /// If the key exists, it's marked as recently used.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The value if found.</param>
    /// <returns>True if the key was found, false otherwise.</returns>
    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);

                value = node.Value.Value;
                return true;
            }

            value = default!;
            return false;
        }
    }

    /// <summary>
    /// Adds or updates a value in the cache.
    /// If the cache is at capacity, the least recently used item is evicted.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="value">The value to store.</param>
    public void AddOrUpdate(TKey key, TValue value)
    {
        lock (_lock)
        {
            // If key exists, update it and move to front
            if (_cache.TryGetValue(key, out var existingNode))
            {
                _lruList.Remove(existingNode);
                existingNode.Value.Value = value;
                _lruList.AddFirst(existingNode);
                return;
            }

            // Check capacity and evict if necessary
            if (_maxCapacity > 0 && _cache.Count >= _maxCapacity)
            {
                // Remove least recently used item (last in list)
                var lruNode = _lruList.Last;
                if (lruNode != null)
                {
                    _lruList.RemoveLast();
                    _cache.Remove(lruNode.Value.Key);
                }
            }

            // Add new item to front
            var entry = new CacheEntry(key, value);
            var node = new LinkedListNode<CacheEntry>(entry);
            _lruList.AddFirst(node);
            _cache[key] = node;
        }
    }

    /// <summary>
    /// Checks if the cache contains a key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    public bool ContainsKey(TKey key)
    {
        lock (_lock)
        {
            return _cache.ContainsKey(key);
        }
    }

    /// <summary>
    /// Removes a specific key from the cache.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if the key was found and removed, false otherwise.</returns>
    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _cache.Remove(key);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Clears all items from the cache.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _lruList.Clear();
        }
    }

    /// <summary>
    /// Gets a snapshot of all keys currently in the cache.
    /// </summary>
    /// <returns>Array of keys in the cache.</returns>
    public TKey[] GetKeys()
    {
        lock (_lock)
        {
            return _cache.Keys.ToArray();
        }
    }

    /// <summary>
    /// Gets a snapshot of all key-value pairs currently in the cache.
    /// </summary>
    /// <returns>Dictionary containing a copy of the cache contents.</returns>
    public Dictionary<TKey, TValue> GetSnapshot()
    {
        lock (_lock)
        {
            var snapshot = _comparer != null
                ? new Dictionary<TKey, TValue>(_comparer)
                : new Dictionary<TKey, TValue>();

            foreach (var kvp in _cache)
            {
                snapshot[kvp.Key] = kvp.Value.Value.Value;
            }

            return snapshot;
        }
    }

    /// <summary>
    /// Internal cache entry structure.
    /// </summary>
    private class CacheEntry
    {
        public TKey Key { get; }
        public TValue Value { get; set; }

        public CacheEntry(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
