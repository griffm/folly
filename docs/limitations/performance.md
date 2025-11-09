# Performance Limitations

## Overview

Folly achieves excellent performance (66x faster than target), but the current architecture has limitations that may affect scalability for extreme workloads. This document outlines performance constraints and potential optimizations.

## Current Performance

**Benchmarks** (from PERFORMANCE.md):
- **200-page render**: ~150ms (target: <10s) - **66x faster**
- **Memory**: ~22MB (target: <600MB) - **27x better**
- **Throughput**: ~1,333 pages/second
- **Scaling**: Linear to sub-linear O(n)

**Strengths**:
- Single-pass layout algorithm
- Minimal allocations
- Efficient font metrics
- No iterative refinement

## Limitations

### 1. Single-Threaded Layout

**Severity**: Medium for very large documents
**Current**: All layout is single-threaded

**Description**:
- Layout engine processes one block at a time
- Cannot utilize multiple CPU cores
- Sequential page generation

**Impact**:
- Large documents (1000+ pages) could be faster with parallelization
- Multi-core systems underutilized
- Scalability limited

**Potential Parallelization Opportunities**:

#### Page-Level Parallelization
```csharp
// Each page sequence could be laid out in parallel
Parallel.ForEach(pageSequences, sequence =>
{
    LayoutPageSequence(sequence);
});
```
**Challenge**: Page numbers and cross-references depend on order

#### Block-Level Parallelization
```csharp
// Independent blocks could be laid out in parallel
Parallel.ForEach(independentBlocks, block =>
{
    LayoutBlock(block);
});
```
**Challenge**: Most blocks depend on vertical position from previous blocks

**Complexity**: High - requires thread-safe area tree, careful synchronization

### 2. No Streaming Support

**Severity**: Low to Medium
**Current**: Entire document in memory

**Description**:
- XML parsed completely into DOM
- Area tree stored completely in memory
- PDF generated from complete area tree

**Memory Usage**: O(input size + output size)

**Impact**:
- Cannot process documents larger than available memory
- Memory footprint scales with document size
- Cannot start PDF output before layout complete

**Streaming Approach** (not implemented):
1. Parse FO incrementally (SAX-style)
2. Layout pages as parsed
3. Write PDF pages immediately
4. Discard page after writing

**Benefits**:
- Constant memory usage
- Can process arbitrarily large documents
- Lower latency (start output sooner)

**Challenges**:
- Forward references (page-number-citation-last)
- Table of contents with page numbers
- Document-wide optimization
- More complex implementation

**Complexity**: Very High

### 3. No Layout Caching

**Severity**: Low
**Scenario**: Regenerating same document multiple times

**Description**:
- No caching of layout results
- Same FO document re-laid out from scratch each time
- No incremental layout for changes

**Impact**:
- Repeated generation of same document wastes CPU
- Small changes require full re-layout
- No warm-up optimization

**Potential Caching**:
```csharp
// Cache area tree by FO content hash
var hash = ComputeHash(foDocument);
if (areaTreeCache.TryGet(hash, out var cachedAreaTree))
{
    return cachedAreaTree;
}
```

**Use Case**: Server generating same invoice template with different data

**Complexity**: Medium

### 4. Font Loading Not Optimized

**Severity**: Low (currently, would be higher with TTF/OTF)
**Current**: AFM files parsed on demand

**Description**:
- Font metrics loaded per document
- No global font cache across documents
- Font parsing repeated

**With TrueType/OpenType** (future):
- Font file parsing expensive (multi-MB files)
- Glyph outline extraction slow
- Should cache parsed font data

**Proposed**:
```csharp
// Global font cache
private static readonly ConcurrentDictionary<string, Font> FontCache;

public Font LoadFont(string path)
{
    return FontCache.GetOrAdd(path, p => ParseFont(p));
}
```

### 5. No Lazy Property Resolution

**Severity**: Very Low
**Current**: All properties resolved during parsing

**Description**:
- Property inheritance computed eagerly
- All properties stored even if not used
- No lazy evaluation

**Alternative**:
```csharp
// Lazy property getter
public double FontSize => _fontSize ?? InheritFontSize();
```

**Impact**: Minimal - property resolution is fast

### 6. No Incremental Layout

**Severity**: Low
**Use Case**: Interactive editors, live preview

**Description**:
- Any change requires full re-layout
- No way to update just changed sections
- No layout state preservation

**Example**:
User changes one paragraph → entire document re-laid out

**Ideal**:
User changes one paragraph → only that paragraph and subsequent content re-laid out

**Complexity**: Very High - requires sophisticated change tracking

### 7. Memory Allocations in Hot Paths

**Severity**: Low
**Current**: Some allocations in line breaking

**Code** (`LayoutEngine.cs:871-921`):
```csharp
var lines = new List<string>();  // Allocation
var currentLine = new StringBuilder();  // Allocation

foreach (var word in words)
{
    // Per-word StringBuilder operations
}
```

**Optimization Potential**:
- Object pooling for StringBuilder
- ArrayPool for temporary buffers
- Span<T> for string manipulation

**Impact**: Minor - allocations relatively infrequent

**Complexity**: Low to Medium

### 8. No PDF Streaming

**Severity**: Low
**Current**: PDF built in memory, then written to stream

**Description**:
- Entire PDF constructed in memory
- Written to output stream at end
- Cannot stream PDF during generation

**Alternative**: Write PDF objects incrementally

**Benefits**:
- Lower memory usage
- Start download sooner (web scenarios)

**Challenges**:
- PDF requires cross-reference table at end
- Some objects reference others (need offsets)
- More complex writer

**Complexity**: Medium

### 9. No GPU Acceleration

**Severity**: Very Low
**Applicability**: Text rendering, image processing

**Description**:
- All rendering CPU-based
- No GPU utilization

**Potential Use Cases**:
- Image scaling/filtering
- Complex path rendering (if SVG support added)
- Font rasterization (not needed for PDF)

**Reality**: PDF generation is mostly layout and encoding, not rendering
- GPU unlikely to help significantly
- CPU bottleneck is layout logic, not computation

**Complexity**: Very High, **Impact**: Minimal

### 10. Large Table Performance

**Severity**: Medium for huge tables
**Current**: O(rows × columns) layout

**Description**:
- Every cell laid out individually
- Large tables (10,000+ cells) slow

**Code**:
```csharp
foreach (var row in table.Rows)  // O(n)
{
    foreach (var cell in row.Cells)  // O(m)
    {
        LayoutCell(cell);  // O(content)
    }
}
```

**Optimization**:
- Cache repeated cell content
- Detect uniform rows (same height)
- Batch similar cells

**Security Limit**: `MaxTableCells = 100,000`

**Complexity**: Medium

## Performance vs. Quality Trade-offs

Several limitations are **intentional design choices** for performance:

| Feature | Quality | Performance | Current Choice |
|---------|---------|-------------|----------------|
| Line breaking | Knuth-Plass (optimal) | Greedy (fast) | **Greedy** ✅ |
| Page breaking | Optimal (DP) | Greedy (fast) | **Greedy** ✅ |
| Font rendering | Hinted glyphs | Direct outlines | **Direct** ✅ |
| Property resolution | Lazy (on-demand) | Eager (upfront) | **Eager** ✅ |
| Layout algorithm | Multi-pass (refine) | Single-pass | **Single-pass** ✅ |

**Philosophy**: "Good enough" quality with excellent performance

## Resource Limits

**Prevent DoS attacks** (from `LayoutOptions.cs`):

```csharp
public int MaxPages = 10000;  // Prevent infinite pagination
public long MaxImageSizeBytes = 50 * 1024 * 1024;  // 50 MB
public int MaxTableCells = 100000;  // Limit table complexity
public int MaxNestingDepth = 100;  // Prevent stack overflow
```

**Purpose**: Prevent malicious documents from:
- Consuming unbounded memory
- Running forever
- Crashing via stack overflow

**Trade-off**: Legitimate large documents may hit limits

## Benchmark Methodology

**Current** (from PERFORMANCE.md):
- BenchmarkDotNet framework
- Complex 200-page test document
- Warm-up and multiple iterations
- Memory profiling

**Recommended Additions**:
1. **Micro-benchmarks**: Individual operations (BreakLines, LayoutBlock)
2. **Stress tests**: 10,000-page documents
3. **Memory profiling**: Allocation tracking
4. **Regression tests**: CI performance gates

## Optimization Priorities

### High Impact, Low Effort
1. **Font caching** (for future TTF/OTF support)
2. **Area tree memory optimization** (reduce object size)
3. **String allocation reduction** (use Span<char>)

### High Impact, High Effort
4. **Page-level parallelization** (careful design needed)
5. **Streaming layout** (major refactor)
6. **Incremental layout** (complex change tracking)

### Low Impact
7. **GPU acceleration** (minimal benefit for PDF generation)
8. **Object pooling** (premature optimization)

## Scalability Limits

**Tested**: Up to 200 pages routinely
**Expected Maximum** (without streaming):
- **Pages**: ~100,000 (limited by time, not memory)
- **Memory**: ~10GB for 100,000 pages (estimate)
- **Time**: ~100 seconds for 100,000 pages (estimate)

**With Streaming** (future):
- **Pages**: Unlimited
- **Memory**: Constant (~100MB)
- **Time**: Linear with page count

## Comparison to Other Engines

| Engine | Language | Performance | Features |
|--------|----------|-------------|----------|
| **Folly** | C# | **150ms** (200p) | Good |
| Apache FOP | Java | ~5-10s (200p) | Excellent |
| XEP (RenderX) | Java | ~2-5s (200p) | Excellent |
| AH Formatter | C++ | ~1-2s (200p) | Excellent |

**Folly Advantage**: Speed (10-50x faster)
**Folly Disadvantage**: Feature completeness (~70% vs 95%+)

## References

1. **PERFORMANCE.md**: Detailed benchmarks
2. **BenchmarkDotNet**: https://benchmarkdotnet.org/
3. **C# Performance Best Practices**: https://docs.microsoft.com/en-us/dotnet/standard/performance/

## See Also

- [line-breaking-text-layout.md](line-breaking-text-layout.md) - Line breaking performance
- [page-breaking-pagination.md](page-breaking-pagination.md) - Page breaking performance
- [security-validation.md](security-validation.md) - Resource limits for DoS prevention
