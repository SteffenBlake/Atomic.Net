# RulesDriver Sparse Poison Benchmark - Baseline Results

## Overview

This benchmark establishes a performance baseline for RulesDriver executing complex mutation logic on a sparse entity distribution. It simulates realistic poison damage-over-time (DOT) mechanics in a game scenario.

## Test Configuration

**Entity Setup (7900 total, programmatic generation):**
- **Health**: ALL entities get `random.Next(0, 61) * 100` (range: 0-6000, steps of 100)
- **#poisoned tag**: 24.87% of entities (1965 entities)
- **poisonStacks**: 12.46% of all entities (984 entities with 90-100 stacks)
- **Dead entities**: ~1.67% start with health=0 (132 entities)
- **Expected mutations/frame**: ~967 entities

**Rule Logic:**
```json
{
  "from": "#poisoned",
  "where": {
    "filter": [
      { "var": "entities" },
      { "and": [
        { ">": [{ "var": "properties.health" }, 0] },
        { ">": [{ "var": "properties.poisonStacks" }, 0] }
      ]}
    ]
  },
  "do": {
    "mut": [
      {
        "target": { "properties": "health" },
        "value": { "-": [
          { "var": "self.properties.health" },
          { "var": "self.properties.poisonStacks" }
        ]}
      },
      {
        "target": { "properties": "poisonStacks" },
        "value": { "-": [
          { "var": "self.properties.poisonStacks" },
          1
        ]}
      }
    ]
  }
}
```

**Benchmark Parameters:**
- **Iterations**: 100 frames (simulates ~1.67 seconds at 60 FPS)
- **Delta time**: 0.016667f (60 FPS)
- **Random seed**: 42 (reproducible)

## Baseline Results

**Environment:**
- .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v4
- GC: Concurrent Workstation
- CPU: Intel Xeon Platinum 8370C @ 2.80GHz

**Performance (100 frames with ~967 mutations each):**

| Metric | Value | Per Frame |
|--------|-------|-----------|
| **Mean** | 1.022 s | **10.22 ms** |
| **Median** | 1.026 s | **10.26 ms** |
| **Min** | 1.010 s | **10.10 ms** |
| **Max** | 1.031 s | **10.31 ms** |
| **StdDev** | 0.011 s | 0.11 ms |

**Memory:**
- **Allocated**: 716.67 MB (7.17 MB per frame)
- **GC Gen0**: 29,000 collections
- **GC Gen1**: 27,000 collections

## Analysis

### Performance Status

✅ **Benchmark is functional** - Successfully measures RulesDriver performance with complex mutation logic

⚠️ **Performance Gap vs Target:**
- **Current**: 10.22ms per frame
- **Aspirational target**: <4ms per frame
- **Gap**: **2.55x slower** than target

### Key Concerns

1. **Excessive Allocations**
   - 7.17 MB allocated per frame is far too high for a zero-allocation target
   - Likely causes: ImmutableDictionary mutations, JsonLogic evaluation, selector recalculation

2. **Heavy GC Pressure**
   - 29,000 Gen0 + 27,000 Gen1 collections indicates allocation storm
   - GC pauses likely contributing to frame time variance

3. **Mutation Overhead**
   - ~967 mutations × 100 frames = 96,700 total mutations
   - Average: ~10.6 microseconds per mutation
   - This suggests room for optimization in the mutation pipeline

### Optimization Opportunities

Based on this baseline, key optimization targets:

1. **Reduce property mutation allocations** - ImmutableDictionary.SetItem creates new instances
2. **Cache selector results** - Avoid recalculating #poisoned matches every frame
3. **Optimize JsonLogic evaluation** - Profile WHERE filter execution
4. **Batch mutations** - Process entities in SIMD-friendly batches

## Usage

Run this benchmark to measure performance improvements:

```bash
cd MonoGame/Atomic.Net.MonoGame.Benchmarks
dotnet run -c Release -- --filter "*SparsePoisonBenchmark*"
```

For quick testing (faster, less accurate):
```bash
dotnet run -c Release -- --filter "*SparsePoisonBenchmark*" --job Dry
```

## Next Steps

This benchmark provides a reproducible baseline for iterative performance improvements. Future optimization efforts should:

1. Re-run this benchmark after each optimization
2. Track both performance AND allocation metrics
3. Aim for sub-4ms per frame with zero allocations post-warmup
4. Document optimization techniques that work

**Status**: 🔴 **Needs Optimization** - Performance is 2.55x slower than target with excessive allocations
