# TensorPrimitives.BitwiseAnd vs Plain For Loop Benchmark Results

## Summary

**Winner: TensorPrimitives.BitwiseAnd** - Dominates across ALL array sizes with 20-72x speedup.

**Conclusion**: No hybrid approach needed. TensorPrimitives.BitwiseAnd should ALWAYS be used over a plain for loop for bitwise AND operations on arrays, regardless of size.

## Performance Comparison

| Array Size | Plain For Loop (bool[]) | TensorPrimitives (byte[]) | Speedup | Ratio |
|------------|------------------------|---------------------------|---------|-------|
| 100        | 113.8 ns              | 5.3 ns                    | **21.5x** | 0.05  |
| 500        | 582.8 ns              | 16.5 ns                   | **35.3x** | 0.03  |
| 1,000      | 1,182 ns              | 16.3 ns                   | **72.4x** | 0.01  |
| 5,000      | 5,817 ns              | 95.3 ns                   | **61.1x** | 0.02  |
| 10,000     | 11,641 ns             | 208.1 ns                  | **55.9x** | 0.02  |
| 50,000     | 58,568 ns             | 1,927 ns                  | **30.4x** | 0.03  |
| 100,000    | 117,041 ns            | 3,791 ns                  | **30.9x** | 0.03  |

## Key Findings

1. **TensorPrimitives wins at ALL sizes** - From 100 to 100,000 elements, TensorPrimitives is consistently 20-72x faster
2. **No crossover point exists** - Unlike some optimizations, there's no small array size where the plain loop is competitive
3. **Peak advantage at 1,000 elements** - TensorPrimitives achieves its best speedup (72.4x) at 1,000 elements
4. **Steady performance at larger sizes** - Maintains 30x speedup even at 100,000 elements
5. **Zero allocations** - Both approaches have zero allocations (both pre-allocate result arrays)
6. **SIMD optimization works** - TensorPrimitives clearly leverages SIMD instructions for massive parallelization

## Why TensorPrimitives is Faster

- **SIMD vectorization**: Processes multiple bytes in parallel (likely 16-32 bytes per instruction)
- **Hardware optimization**: Uses CPU-specific instructions (AVX2/AVX-512) for bitwise operations
- **Minimal overhead**: Even at small sizes (100 elements), the overhead is negligible compared to the speedup

## Recommendation

**ALWAYS use TensorPrimitives.BitwiseAnd for bitwise AND operations on byte arrays**, regardless of size.

### Code Pattern

```csharp
// ✅ Use this - 20-72x faster
TensorPrimitives.BitwiseAnd(leftBytes, rightBytes, resultBytes);

// ❌ Don't use this - 20-72x slower
for (int i = 0; i < length; i++)
{
    resultBools[i] = leftBools[i] & rightBools[i];
}
```

### Trade-offs

**Pro:**
- Massive performance improvement (20-72x)
- Zero allocations (same as plain loop with pre-allocated array)
- Consistent speedup across all array sizes
- Simple API

**Con:**
- Requires byte[] instead of bool[] (1 byte vs 1 byte per element - same memory)
- Requires System.Numerics.Tensors package
- Less readable than plain for loop (but performance gain justifies it)

## Test Configuration

- **Runtime**: .NET 10.0.2, X64 RyuJIT x86-64-v4
- **CPU**: Intel Xeon Platinum 8370C @ 2.80GHz
- **Data Pattern**: ~5% of values are true/1 (pre-seeded Random(42))
- **Both approaches**: Pre-allocated result arrays (zero allocations during operation)

## Benchmark Details

**Test Methodology:**
- Plain loop uses `bool[]` arrays with bitwise AND operator
- TensorPrimitives uses `byte[]` arrays (0 = false, 1 = true)
- Same data pattern for fair comparison
- Both pre-allocate result arrays to isolate operation performance
