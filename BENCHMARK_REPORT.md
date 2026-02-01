# Scene Loading Performance Benchmark - Final Report

## Executive Summary

Benchmarked scene loading performance for a 1000-entity scene to determine the breakdown between JSON parsing and entity spawning phases. The goal was to assess whether async scene loading provides meaningful parallelism.

**Key Finding:** Entity spawning dominates scene loading time at **63.4%**, while JSON parsing accounts for **36.6%**.

---

## Benchmark Implementation

### Location
- **Benchmark**: `MonoGame/Atomic.Net.MonoGame.Benchmarks/Scenes/Integrations/SceneLoadingBenchmark.cs`
- **Fixture**: `MonoGame/Atomic.Net.MonoGame.Benchmarks/Scenes/Fixtures/large-scene.json`
- **Type**: Integration benchmark (tests full scene loading pipeline)

### Test Scenario
- **Scene Size**: 1000 entities
- **Entity Behaviors**: Transform, Properties, Tags, Parent (realistic game scenario)
- **Fixture Size**: 729 KB JSON file
- **Test Approach**: Glass-box benchmark measuring isolated phases

---

## Performance Results

### Benchmark Metrics

| Phase               | Mean Time | Memory    | % of Total |
|---------------------|-----------|-----------|------------|
| JSON Parsing        | 19.18 ms  | 4.68 MB   | 36.6%      |
| Entity Spawning     | 33.26 ms  | 14.63 MB  | 63.4%      |
| **Total**           | **52.44 ms** | **17.5 MB** | **100%**   |

### Phase Breakdown

#### 1. JSON Parsing Phase (36.6% of total time)
- **Time**: 19.18 ms ± 0.995 ms
- **Memory**: 4.68 MB (temporary allocations, GC-eligible after load)
- **Work**: `File.ReadAllText()` + `JsonSerializer.Deserialize<JsonScene>()`
- **Threading**: ✅ Can run on background thread
- **Potential Savings**: Full 19.18 ms if moved to async

#### 2. Entity Spawning Phase (63.4% of total time)
- **Time**: 33.26 ms ± 0.662 ms
- **Memory**: 14.63 MB (persistent entity data)
- **Work**: 
  - Entity activation (`EntityRegistry.Activate()`)
  - Behavior application (`jsonEntity.WriteToEntity()`)
  - Selector recalculation (`SelectorRegistry.Instance.Recalc()`)
  - Hierarchy recalculation (`HierarchyRegistry.Instance.Recalc()`)
- **Threading**: ❌ MUST run on main thread (MonoGame/ECS constraints)
- **Unavoidable Blocking**: Full 33.26 ms

---

## Analysis & Recommendations

### 1. Async Scene Loading Benefit Assessment

**Question**: Does async scene loading provide meaningful parallelism?

**Answer**: Yes, but with limitations.

Moving JSON parsing to a background thread provides:
- **Time Savings**: ~19ms per 1000 entities
- **Blocking Reduction**: 36% reduction in main thread blocking (52ms → 33ms)
- **Loading Screen Responsiveness**: Main thread can animate loading screen during parsing

However:
- **Remaining Block**: 33ms still blocks main thread during entity spawning
- **Not a Complete Solution**: Async parsing helps but doesn't eliminate loading pauses

### 2. Performance Expectations by Scene Size

| Scene Size | Total Time | Parsing  | Spawning | Frames @ 60 FPS |
|------------|------------|----------|----------|-----------------|
| 100 entities | ~5.2 ms   | 1.9 ms   | 3.3 ms   | <1 frame       |
| 500 entities | ~26 ms    | 9.6 ms   | 16.6 ms  | ~1.5 frames    |
| 1000 entities | ~52 ms   | 19 ms    | 33 ms    | ~3 frames      |

At 60 FPS (16.67ms/frame), scenes with >500 entities will cause visible frame drops during loading.

### 3. Recommendations for Scene Loading System

#### Immediate Actions (High Value)
1. **Implement Async JSON Parsing**
   - Move `File.ReadAllText()` + `JsonSerializer.Deserialize()` to background thread
   - Saves ~19ms per 1000 entities
   - Allows loading screen to remain responsive
   - Low implementation cost, moderate benefit

#### Future Optimizations (For Large Scenes)
2. **Chunked Entity Spawning**
   - Spread entity spawning across multiple frames
   - Example: Spawn 100 entities/frame (3.3ms/frame) instead of 1000 at once (33ms)
   - Prevents frame drops during loading
   - Required for scenes >500 entities

3. **Priority-Based Loading**
   - Spawn essential entities first (player, UI, critical NPCs)
   - Defer decorative/background entities
   - Game becomes playable faster
   - "Streaming" appearance to scene loading

4. **Profile Recalc Performance**
   - Investigate if `SelectorRegistry.Recalc()` and `HierarchyRegistry.Recalc()` can be optimized
   - These are part of the 33ms entity spawning phase
   - May find additional optimization opportunities

### 4. Memory Allocation Analysis

**Total Allocations**: 17.5 MB per 1000 entities
- **JSON Parsing**: 4.68 MB (26.7%) - Temporary, GC-eligible
- **Entity Data**: 14.63 MB (83.6%) - Persistent

**Insight**: Most memory is entity data (expected). JSON parsing overhead is reasonable at ~27% of total allocations.

---

## Conclusion

**Does async scene loading provide meaningful parallelism?**

**Yes, with caveats:**
- ✅ Async JSON parsing reduces main thread blocking by **36%**
- ✅ Improves loading screen responsiveness
- ✅ Worth implementing (low cost, moderate benefit)
- ❌ Does NOT eliminate loading pauses (63% of work still blocks)
- ⚠️ For scenes >500 entities, chunked spawning is also needed

**Bottom Line**: Async scene loading is **moderately beneficial** but not a complete solution. The entity spawning phase (63.4% of load time) fundamentally blocks the main thread and requires different optimization strategies (chunked spawning, priority loading).

---

## Significant Finding for DISCOVERIES.md

This finding has been added to `.github/agents/DISCOVERIES.md` as it meets the >10% significance threshold:

- **Entity Spawning**: 63.4% of total time (>10% more than JSON parsing)
- **JSON Parsing**: 36.6% of total time
- **Difference**: 26.8 percentage points (significant)

The discovery provides actionable guidance for async scene loading implementation and sets performance expectations for different scene sizes.

---

## Artifacts

1. **Benchmark Code**: `MonoGame/Atomic.Net.MonoGame.Benchmarks/Scenes/Integrations/SceneLoadingBenchmark.cs`
2. **Test Fixture**: `MonoGame/Atomic.Net.MonoGame.Benchmarks/Scenes/Fixtures/large-scene.json` (1000 entities, 729 KB)
3. **Detailed Results**: `MonoGame/Atomic.Net.MonoGame.Benchmarks/Scenes/Integrations/SCENE_LOADING_RESULTS.md`
4. **Discovery Entry**: `.github/agents/DISCOVERIES.md` (Scene Loading section)

---

## Additional Notes

### Namespace Conflict Fix
As part of this work, fixed namespace conflicts in existing benchmarks:
- `RulesDriver/SparsePoisonBenchmark.cs`
- `SequenceDriver/SparsePoisonSequenceBenchmark.cs`

Issue: Creating `Atomic.Net.MonoGame.Benchmarks.Scenes` namespace conflicted with `using Atomic.Net.MonoGame.Scenes`.

Solution: Changed `Scenes.RulesDriver.Instance` to `global::Atomic.Net.MonoGame.Scenes.RulesDriver.Instance` for full qualification.

---

**Benchmark completed successfully.**  
**All findings documented and significant discoveries added to DISCOVERIES.md.**
