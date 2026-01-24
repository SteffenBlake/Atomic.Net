# TensorPrimitives.BitwiseAnd vs Plain For Loop vs SparseArray.Intersect Benchmark Results

## Summary

**Winner: TensorPrimitives.BitwiseAnd** - Dominates across ALL array sizes with 2.6-2.8x speedup over plain for loop.

**Second Place: SparseArray.SelectThenIntersect** - Competitive at larger sizes (5K+ elements), beating plain loop by 19-24% when data is sparse.

**Key Finding**: Select→Intersect is consistently faster than Intersect→Select (13-27% faster), confirming the hypothesis.

**Conclusion**: Use TensorPrimitives.BitwiseAnd for dense boolean AND operations. For truly sparse data (5% infill), SparseArray becomes competitive at 5K+ elements but still slower than TensorPrimitives.

## Performance Comparison (with Iteration Overhead Included)

All tests now include fair comparison with:
- Writing results to output array/SparseArray
- Iterating over results and accumulating matched indexes

| Array Size | Plain Loop | TensorPrimitives | SparseArray (Select→Int) | SparseArray (Int→Select) | Best |
|------------|-----------|------------------|-------------------------|-------------------------|------|
| 100        | 191 ns    | **72 ns** (2.7x) | 529 ns (0.4x)          | 528 ns (0.4x)          | TensorPrimitives |
| 500        | 932 ns    | **336 ns** (2.8x)| 1,179 ns (0.8x)        | 1,256 ns (0.7x)        | TensorPrimitives |
| 1,000      | 1,871 ns  | **668 ns** (2.8x)| 2,093 ns (0.9x)        | 2,374 ns (0.8x)        | TensorPrimitives |
| 5,000      | 9,350 ns  | **3,300 ns** (2.8x) | 7,554 ns (1.2x)     | 9,595 ns (1.0x)        | TensorPrimitives |
| 10,000     | 18,747 ns | **6,747 ns** (2.8x) | 15,359 ns (1.2x)    | 19,634 ns (1.0x)       | TensorPrimitives |
| 50,000     | 93,612 ns | **33,866 ns** (2.8x) | 71,589 ns (1.3x)   | 90,314 ns (1.0x)       | TensorPrimitives |

*(100K results were not available due to benchmark timeout)*

## Key Findings

### TensorPrimitives Performance
1. **Consistent 2.6-2.8x speedup** over plain for loop across all sizes
2. **Zero allocations** - No GC pressure
3. **Best overall choice** for dense boolean AND operations
4. **SIMD vectorization** clearly working even with iteration overhead

### SparseArray Performance
1. **Select→Intersect is faster** than Intersect→Select (confirmed hypothesis)
   - 100: 529ns vs 528ns (essentially equal at small size)
   - 1,000: 2,093ns vs 2,374ns (**13% faster**)
   - 10,000: 15,359ns vs 19,634ns (**27% faster**)
   - 50,000: 71,589ns vs 90,314ns (**26% faster**)

2. **Becomes competitive at larger sizes** with sparse data
   - At 5K: 19% faster than plain loop
   - At 10K: 22% faster than plain loop  
   - At 50K: 24% faster than plain loop
   - Still **2.2x slower** than TensorPrimitives at 50K

3. **Allocates memory** - Unlike TensorPrimitives and plain loop
   - 1K: 3KB allocated
   - 10K: 28KB allocated
   - 50K: 124KB allocated

### Plain For Loop
- **Baseline** - Simple, zero allocations
- Slower than TensorPrimitives at all sizes
- Competitive with SparseArray at small sizes (< 5K)

## Performance Ratios (vs Plain Loop Baseline)

| Size   | TensorPrimitives | Select→Intersect | Intersect→Select |
|--------|-----------------|------------------|------------------|
| 100    | 0.38x           | 2.77x            | 2.76x            |
| 500    | 0.36x           | 1.26x            | 1.35x            |
| 1,000  | 0.36x           | 1.12x            | 1.27x            |
| 5,000  | 0.35x           | **0.81x**        | 1.03x            |
| 10,000 | 0.36x           | **0.82x**        | 1.05x            |
| 50,000 | 0.36x           | **0.76x**        | 0.96x            |

*(Values < 1.0 mean faster than baseline, values > 1.0 mean slower)*

## Recommendation by Use Case

### Dense Boolean Arrays (any size)
**Use TensorPrimitives.BitwiseAnd**
- 2.6-2.8x faster than plain loop
- Zero allocations
- Simple API

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

### Sparse Boolean Data (5K+ elements, <10% infill)
**Consider SparseArray.SelectThenIntersect**
- 19-24% faster than plain loop at large sizes
- Only stores true values (memory efficient for storage)
- Better when you need to iterate sparse results anyway
- **Still 2.2x slower than TensorPrimitives**

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
**Use TensorPrimitives.BitwiseAnd**
- SparseArray is slower than plain loop at small sizes
- TensorPrimitives dominates regardless

## Test Configuration

- **Runtime**: .NET 10.0.2, X64 RyuJIT x86-64-v3
- **CPU**: AMD EPYC 7763 @ 3.16GHz
- **Data Pattern**: ~5% of values are true/1 (pre-seeded Random(42))
- **Fair Testing**: All approaches write to output arrays AND iterate results to accumulate matched indexes

## Benchmark Details

**Four Approaches Tested:**
1. **PlainForLoop_Bools**: bool[] arrays with & operator + iteration
2. **TensorPrimitives_BitwiseAnd_Bytes**: byte[] arrays with SIMD + iteration
3. **SparseArray_SelectThenIntersect**: Extract indexes first, then intersect
4. **SparseArray_IntersectThenSelect**: Intersect tuples first, then extract indexes

All tests include:
- Writing results to output array/SparseArray  
- Iterating over all results
- Accumulating matched indexes (prevents compiler optimization)

