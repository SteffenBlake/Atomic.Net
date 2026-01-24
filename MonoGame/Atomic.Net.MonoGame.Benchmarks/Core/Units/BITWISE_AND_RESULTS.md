# Boolean AND Operations Benchmark Results

## Summary

**TensorPrimitives ALWAYS Wins** - Use it in ALL cases, regardless of array size or data sparsity.

**Key Findings:**
- **TensorPrimitives (pre-converted byte[])**: 2.6-2.8x faster than plain loop
- **TensorPrimitives with copy overhead**: Still 2.6-2.7x faster than plain loop
- **MemoryMarshal.Cast copy is very efficient**: Only adds ~5ns (100 elements) to ~500ns (10K elements)
- **BitArray with copy**: Allocates memory, slower than TensorPrimitives even without copy overhead

**Conversion Overhead Analysis:**
- MemoryMarshal.Cast for bool[] → byte[] is extremely fast (negligible overhead)
- BitArray constructor allocates memory (144B-2.6KB depending on size)
- Even with conversion, TensorPrimitives remains the fastest approach

## Performance Comparison

| Size   | Plain Loop | TensorPrimitives | TensorPrimitives (Copy) | BitArray | BitArray (Copy) |
|--------|-----------|------------------|------------------------|----------|-----------------|
| 100    | 191 ns    | **72 ns** (2.6x) | 77 ns (2.5x)           | 104 ns (1.8x) | 134 ns (1.4x) |
| 1,000  | 1,895 ns  | **672 ns** (2.8x)| 696 ns (2.7x)          | 974 ns (1.9x) | 1,070 ns (1.8x) |
| 10,000 | 18,769 ns | **6,748 ns** (2.8x) | 7,235 ns (2.6x)     | 9,683 ns (1.9x) | 10,450 ns (1.8x) |

**Conversion Overhead:**
- TensorPrimitives copy adds: ~5ns (100), ~24ns (1K), ~487ns (10K)
- BitArray copy adds: ~30ns (100), ~96ns (1K), ~768ns (10K) + allocations

## Recommendation

**ALWAYS use TensorPrimitives.BitwiseAnd** - Even with bool[] → byte[] conversion overhead, it remains the fastest approach.

```csharp
// If you already have byte[] data
TensorPrimitives.BitwiseAnd(leftBytes, rightBytes, resultBytes);

// If converting from bool[]
MemoryMarshal.Cast<bool, byte>(leftBools).CopyTo(leftBytes);
MemoryMarshal.Cast<bool, byte>(rightBools).CopyTo(rightBytes);
TensorPrimitives.BitwiseAnd(leftBytes, rightBytes, resultBytes);
```

**Benchmark:** `MonoGame/Atomic.Net.MonoGame.Benchmarks/Core/Units/BitwiseAndBenchmark.cs`
