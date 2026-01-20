# Performance Discoveries

This file documents significant performance findings backed by benchmarks.   Only add entries when benchmarks show >10% performance difference or unexpected behavior.

---

## Transform Matrix Multiplication Order (Sprint 001 - January 2026)

**DISCOVERY:** MonoGame/XNA hierarchical transform matrix order is `(-Anchor)*Scale*Rotation*Anchor*Position`

**Problem:** During Sprint 001 JSON scene loading implementation, 2 Transform integration tests failed with apparent "matrix order bug". Investigation revealed the Transform system was CORRECT - the test expectations were wrong.

**Root Cause:** When migrating Transform unit tests to integration tests, test data was updated to include parents with BOTH position and rotation (old tests only had rotation). When position is zero, `Rotation*Translation(0,0,0)` equals `Translation(0,0,0)*Rotation` (both are just the rotation matrix), hiding the incorrect test order. With non-zero position, the difference became apparent.

**Correct Order:**
```csharp
// Local transform matrix:
(-Anchor) * Scale * Rotation * Anchor * Position

// When anchor is zero (default):
Scale * Rotation * Position

// MonoGame syntax:
Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position)
```

**Why This Matters:** For a parent at position (100,0,0) rotated 90° with child at local (50,0,0):
- ✅ CORRECT: Rotate child (50,0,0) by 90° → (0,50,0), then add parent position → (100,50,0)
- ❌ WRONG: Translate parent to (100,0,0), then rotate that position → nonsensical result

**Lesson:** Always test transforms with NON-ZERO values for position, rotation, scale, and anchor. Zero values can mask order-dependent bugs because `Identity * Matrix == Matrix * Identity`.

**Files:** See `/TRANSFORM_TEST_INVESTIGATION.md` for detailed analysis.

**Resolution:** Fixed 2 test expectations in `TransformRegistryIntegrationTests.cs`. Transform system implementation unchanged - it was already correct.

---

## Closures Allocate in Hot Paths

**Problem:** Lambdas with closures allocate ~80 bytes per invocation in tight loops, causing GC pressure

**Solution:** Pass captured variables as `in` parameters with static lambdas to eliminate allocations

**Example:**
```csharp
// ❌ Bad - allocates closure
var multiplier = 2;
entities.ForEach(entity => ProcessEntity(entity, multiplier));

// ✅ Good - zero allocations
var multiplier = 2;
ProcessEntities(in multiplier, static (ref readonly m, entity) => ProcessEntity(entity, m));
```

**Performance Improvement:** ~3x speedup, zero allocations in hot paths

**Benchmark:** *(No benchmark file yet - discovered during profiling)*
// SteffenBlake: @benchmarker maybe we should make a benchmark for this to cover our trail?
