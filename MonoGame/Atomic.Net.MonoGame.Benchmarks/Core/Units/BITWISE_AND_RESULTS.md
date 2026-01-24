# BitArray vs TensorPrimitives vs Plain Loop vs SparseArray Benchmark Results

## Summary

**Winner: TensorPrimitives.BitwiseAnd** - Dominates across ALL array sizes with 2.6-2.8x speedup over plain loop.

**Second Place: BitArray.And** - Consistent 2x speedup over plain loop with zero allocations, beating SparseArray at all sizes.

**Third Place: SparseArray.SelectThenIntersect** - Competitive at larger sizes (5K+ elements), beating plain loop by 18-23% when data is sparse.

**Key Finding**: 
- BitArray is the **best built-in .NET collection** for boolean AND operations (2x speedup, zero allocations)
- Select→Intersect is consistently faster than Intersect→Select (13-28% faster)
- TensorPrimitives still dominates all approaches (2.8x faster than plain loop, 1.4x faster than BitArray)

**Conclusion**: Use TensorPrimitives.BitwiseAnd for maximum performance. For simpler API without external dependencies, BitArray is excellent (2x speedup). For truly sparse data (5% infill), SparseArray becomes competitive at 5K+ elements.

## Performance Comparison (with Iteration Overhead Included)

All tests include fair comparison with:
- Writing results to output array/SparseArray/BitArray
- Iterating over results and accumulating matched indexes

| Array Size | Plain Loop | TensorPrimitives | BitArray | SparseArray (Select→Int) | SparseArray (Int→Select) | Best |
|------------|-----------|------------------|----------|-------------------------|-------------------------|------|
| 100        | 189 ns    | **72 ns** (2.6x) | 105 ns (1.8x) | 497 ns (0.4x)   | 519 ns (0.4x)          | TensorPrimitives |
| 500        | 943 ns    | **353 ns** (2.7x)| 489 ns (1.9x) | 1,117 ns (0.8x) | 1,258 ns (0.7x)        | TensorPrimitives |
| 1,000      | 1,892 ns  | **669 ns** (2.8x)| 972 ns (1.9x) | 2,053 ns (0.9x) | 2,324 ns (0.8x)        | TensorPrimitives |
| 5,000      | 9,339 ns  | **3,299 ns** (2.8x) | 4,798 ns (1.9x) | 7,697 ns (1.2x) | 9,460 ns (1.0x) | TensorPrimitives |
| 10,000     | 19,015 ns | **6,768 ns** (2.8x) | 9,689 ns (2.0x) | 14,964 ns (1.3x) | 19,170 ns (1.0x) | TensorPrimitives |
| 50,000     | 93,911 ns | **33,817 ns** (2.8x) | 47,899 ns (2.0x) | 72,175 ns (1.3x) | 86,033 ns (1.1x) | TensorPrimitives |

*(100K results were not available due to benchmark timeout)*

## Key Findings

### TensorPrimitives Performance
1. **Consistent 2.6-2.8x speedup** over plain loop across all sizes
2. **1.4x faster than BitArray** - SIMD vectorization advantage clear
3. **Zero allocations** - No GC pressure
4. **Best overall choice** for dense boolean AND operations

### BitArray Performance
1. **Consistent 1.8-2.0x speedup** over plain loop
2. **Zero allocations** - Built-in .NET collection efficiency
3. **Simpler API** than TensorPrimitives (no byte[] conversion needed)
4. **Best built-in .NET collection** for boolean operations
5. **Second place overall** - Great choice when TensorPrimitives isn't available

### SparseArray Performance
1. **Select→Intersect is faster** than Intersect→Select (confirmed hypothesis)
   - 100: 497ns vs 519ns (4% faster)
   - 1,000: 2,053ns vs 2,324ns (**13% faster**)
   - 10,000: 14,964ns vs 19,170ns (**28% faster**)
   - 50,000: 72,175ns vs 86,033ns (**19% faster**)

2. **Becomes competitive at larger sizes** with sparse data
   - At 5K: 18% faster than plain loop
   - At 10K: 21% faster than plain loop  
   - At 50K: 23% faster than plain loop
   - Still **2.2x slower** than TensorPrimitives at 50K
   - Still **1.5x slower** than BitArray at 50K

3. **Allocates memory** - Unlike TensorPrimitives, BitArray, and plain loop
   - 1K: 3KB allocated
   - 10K: 28KB allocated
   - 50K: 124KB allocated

### Plain For Loop
- **Baseline** - Simple, zero allocations
- Slower than all optimized approaches
- Good reference point for measuring improvements

## Performance Ratios (vs Plain Loop Baseline)

| Size   | TensorPrimitives | BitArray | Select→Intersect | Intersect→Select |
|--------|-----------------|----------|------------------|------------------|
| 100    | 0.38x           | 0.56x    | 2.62x            | 2.74x            |
| 500    | 0.37x           | 0.52x    | 1.18x            | 1.33x            |
| 1,000  | 0.35x           | 0.51x    | 1.08x            | 1.23x            |
| 5,000  | 0.35x           | 0.51x    | **0.82x**        | 1.01x            |
| 10,000 | 0.36x           | 0.51x    | **0.79x**        | 1.01x            |
| 50,000 | 0.36x           | 0.51x    | **0.77x**        | 0.92x            |

*(Values < 1.0 mean faster than baseline, values > 1.0 mean slower)*

## Recommendation by Use Case

### Dense Boolean Arrays - Maximum Performance
**Use TensorPrimitives.BitwiseAnd**
- 2.6-2.8x faster than plain loop
- 1.4x faster than BitArray
- Zero allocations
- Requires byte[] (0/1 values)

```csharp
// ✅ Use this - 2.8x faster via SIMD
TensorPrimitives.BitwiseAnd(leftBytes, rightBytes, resultBytes);

// Iterate and accumulate if needed
int result = 0;
for (int i = 0; i < resultBytes.Length; i++)
{
    if (resultBytes[i] == 1)
    {
        result += i;
    }
}
```

### Dense Boolean Arrays - Best Built-in .NET Collection
**Use BitArray.And**
- 1.8-2.0x faster than plain loop
- Zero allocations
- Simple API (works directly with bool[])
- No external dependencies

```csharp
// ✅ Use this - 2x faster, built-in .NET
var leftBitArray = new BitArray(leftBools);
var rightBitArray = new BitArray(rightBools);
var resultBitArray = new BitArray(capacity);

resultBitArray = leftBitArray.And(rightBitArray);

// Iterate and accumulate if needed
int result = 0;
for (int i = 0; i < resultBitArray.Length; i++)
{
    if (resultBitArray[i])
    {
        result += i;
    }
}
```

### Sparse Boolean Data (5K+ elements, <10% infill)
**Consider SparseArray.SelectThenIntersect**
- 18-23% faster than plain loop at large sizes
- Only stores true values (memory efficient for storage)
- Better when you need to iterate sparse results anyway
- **Still 2.2x slower than TensorPrimitives**
- **Still 1.5x slower than BitArray**

```csharp
// ✅ For sparse data iteration (5K+ elements)
var indexesA = leftSparse.Select(static v => v.Index);
var indexesB = rightSparse.Select(static v => v.Index);

int result = 0;
foreach (var indexMatch in indexesA.Intersect(indexesB))
{
    result += indexMatch;
    resultSparse.Set(indexMatch, true);
}
```

### Small Arrays (< 5K elements)
**Use TensorPrimitives or BitArray**
- SparseArray is slower than plain loop at small sizes
- BitArray provides 2x speedup with simple API
- TensorPrimitives dominates regardless

## Test Configuration

- **Runtime**: .NET 10.0.2, X64 RyuJIT x86-64-v3
- **CPU**: AMD EPYC 7763 @ 2.45GHz
- **Data Pattern**: ~5% of values are true/1 (pre-seeded Random(42))
- **Fair Testing**: All approaches write to output arrays AND iterate results to accumulate matched indexes

## Benchmark Details

**Five Approaches Tested:**
1. **PlainForLoop_Bools**: bool[] arrays with & operator + iteration
2. **TensorPrimitives_BitwiseAnd_Bytes**: byte[] arrays with SIMD + iteration
3. **BitArray_And**: BitArray with built-in And() method + iteration
4. **SparseArray_SelectThenIntersect**: Extract indexes first, then intersect
5. **SparseArray_IntersectThenSelect**: Intersect tuples first, then extract indexes

All tests include:
- Writing results to output array/BitArray/SparseArray  
- Iterating over all results
- Accumulating matched indexes (prevents compiler optimization)
