# Sprint: Partitioned Sparse Array Split

## Non-Technical Requirements

### What We're Building
- Split all partitioned systems (Entities, Behaviors, Rules, Sequences) from using a single large sparse array into two separate physical sparse arrays: one for global partition, one for scene partition
- Introduce a `PartitionIndex` variant type (`ushort` for global, `int` for scene) to enforce type safety at compile time
- Introduce a `PartitionedSparseArray<T>` wrapper that accepts `PartitionIndex` and internally routes to the correct underlying array
- Update all constants to reflect the new partition split (remove combined capacity constants like `MaxEntities`, replace with `MaxGlobalEntities` (ushort) and `MaxSceneEntities` (int))

### Why Now?
- Async scene loading (next sprint) requires thread-safe architecture — shared sparse arrays create threading risks
- `ResetEvent` performance: clearing scene partition becomes O(1) instead of iterating indices 256-8191
- Cache locality: global entities (loading screen, UI, persistent state) packed into tight 256-element arrays instead of sparse 8192-element arrays
- Architectural clarity: partition distinction becomes structural (different arrays, different types) instead of implicit magic numbers
- The longer we wait, the more systems depend on the current architecture, making this refactor exponentially harder

### Success Criteria
- All partitioned systems use `PartitionedSparseArray<T>` wrapper with explicit global/scene capacities
- `PartitionIndex` variant enforces compile-time type safety (can't mix global/scene indices)
- Public APIs remain clean — callers use `registry[entity.Index]` without needing to know about partitions
- `ResetEvent` clears only scene partition in O(1) operation
- All existing tests pass with zero behavioral changes
- Constants updated everywhere: `MaxGlobalX` (ushort), `MaxSceneX` (int), no combined `MaxX` constants
- Zero gameplay allocations (refactor only changes structure, not allocation behavior)
- Partitioned Sparse Array exposes both of its sparse arrays for access and enumeration, counting, etc (just expose the sparse arrays as public properties)

### Constraints
- Partition routing must be hidden inside `PartitionedSparseArray` — 99% of code should not see `Visit` or `TryMatch` patterns
- Global partition indices: `ushort` (0-255 by default)
- Scene partition indices: `int` (0-based, capacity 8192 by default)
- No pointer references, indices only
- Must support all existing partition behaviors (global persists through `ResetEvent`, scene clears)

### Example Scenario

**Before (current):**
```csharp
// Single array, implicit partition logic
SparseArray<Entity> _entities = new(8192);
Entity entity = _entities[index]; // Is this global or scene? You have to remember.
```

**After (target):**
```csharp
// Two arrays, explicit partition via variant type
PartitionedSparseArray<Entity> _entities = new(
    Constants.MaxGlobalEntities,  // ushort: 256
    Constants.MaxSceneEntities     // int: 8192
);
Entity entity = _entities[partitionIndex]; // PartitionIndex routes correctly, caller doesn't care
```

## Update reset event code in various applicable registries

With 2 Seperate sparse arrays the enumeration in Reset can be made way simpler, no more for loop, now it can just be .Clear call!

---

## Technical Requirements

### Architecture

#### New Core Files

**1. `MonoGame/Atomic.Net.MonoGame/Core/PartitionIndex.cs`**
- Variant type with two cases: `GlobalIndex(ushort)` and `SceneIndex(int)`
- Provides type-safe compile-time distinction between partition types
- Implicit conversion operators from `ushort` (global) and `int` (scene) for ergonomic usage
- Zero-allocation struct-based design

**2. `MonoGame/Atomic.Net.MonoGame/Core/PartitionedSparseArray.cs`**
- Generic wrapper over two underlying `SparseArray<T>` instances
- Constructor: `PartitionedSparseArray(ushort globalCapacity, int sceneCapacity)`
- Indexer accepts `PartitionIndex` and routes to correct underlying array using `.Visit()` pattern
- Public properties exposing both sparse arrays:
  - `SparseArray<T> Global { get; }` - for direct access/enumeration of global partition
  - `SparseArray<T> Scene { get; }` - for direct access/enumeration of scene partition
- All `SparseArray<T>` methods exposed: `Set`, `GetMut`, `Remove`, `TryGetValue`, `HasValue`, `TryPop`, `Clear`
- `ClearScene()` method for O(1) scene partition clearing (just calls `Scene.Clear()`)
- `ClearAll()` method for clearing both partitions (used in `ShutdownEvent`)
- Routing logic stays internal — callers never see `Visit` or `TryMatch`

**3. Update `MonoGame/Atomic.Net.MonoGame/Core/SparseArray.cs`**
- Create new `SparseArray<T>(int capacity)` constructor overload accepting `int` for scene partition
- Existing `ushort capacity` constructor remains for global partition
- All internal arrays sized based on capacity parameter type
- Minimal changes — just support larger capacity values

**4. Update `MonoGame/Atomic.Net.MonoGame/Core/Constants.cs`**
- Remove combined capacity constants: `MaxEntities`, `MaxRules`, `MaxSequences`
- Replace with partition-specific constants:
  - **Entities:**
    - `MaxGlobalEntities` (ushort, default 256) — already exists
    - `MaxSceneEntities` (int, default 8192) — NEW
  - **Rules:**
    - `MaxGlobalRules` (ushort, default 128) — already exists
    - `MaxSceneRules` (int, default 896) — NEW (total 1024 - 128)
  - **Sequences:**
    - `MaxGlobalSequences` (ushort, default 256) — already exists  
    - `MaxSceneSequences` (int, default 256) — NEW (total 512 - 256)
- Add preprocessor directive support for compile-time overrides (same pattern as existing constants)

#### Registry Updates

**5. Update `MonoGame/Atomic.Net.MonoGame/Core/EntityRegistry.cs`**
- Replace `SparseArray<bool> _active` with `PartitionedSparseArray<bool> _active`
- Replace `SparseArray<bool> _enabled` with `PartitionedSparseArray<bool> _enabled`
- Update allocators: `_nextGlobalIndex` remains `ushort`, `_nextSceneIndex` becomes `int`
- `Activate()` returns scene entity with `int` scene index
- `ActivateGlobal()` returns global entity with `ushort` global index
- `ResetEvent` handler: replace loop with `_active.ClearScene()` and `_enabled.ClearScene()`
- `ShutdownEvent` handler: replace manual loop with `_active.ClearAll()` and `_enabled.ClearAll()`
- Entity lookup remains index-based (array of pre-allocated Entity structs)

**6. Update `MonoGame/Atomic.Net.MonoGame/BED/BehaviorRegistry.cs`**
- Replace `SparseArray<TBehavior> _behaviors` with `PartitionedSparseArray<TBehavior> _behaviors`
- No changes to public API (entity indices auto-route to correct partition)
- Handles `PreEntityDeactivatedEvent` without partition awareness

**7. Update `MonoGame/Atomic.Net.MonoGame/Scenes/RuleRegistry.cs`**
- Replace `SparseArray<JsonRule> _rules` with `PartitionedSparseArray<JsonRule> _rules`
- Update allocators: `_nextGlobalRuleIndex` remains `ushort`, `_nextSceneRuleIndex` becomes `int`
- `ResetEvent` handler: replace loop with `_rules.ClearScene()`
- `ShutdownEvent` handler: replace loop with `_rules.ClearAll()`
- Keep `Rules` public property exposing `PartitionedSparseArray<JsonRule>` for tests

**8. Update `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceRegistry.cs`**
- Replace `SparseArray<JsonSequence> _sequences` with `PartitionedSparseArray<JsonSequence> _sequences`
- Update allocators: `_nextGlobalSequenceIndex` remains `ushort`, `_nextSceneSequenceIndex` becomes `int`
- `ResetEvent` handler: replace loop with direct iteration over `_sequences.Scene` + `Clear()`
- `ShutdownEvent` handler: replace loop with `_sequences.ClearAll()` + `_idToIndex.Clear()`
- Keep `Sequences` public property exposing `PartitionedSparseArray<JsonSequence>` for tests

#### Entity Type Update

**9. Update `MonoGame/Atomic.Net.MonoGame/Core/Entity.cs`**
- Add `PartitionIndex Index` property returning correct variant based on internal `ushort` index
- Keep internal `ushort _index` field for backward compatibility
- Logic: `if (_index < Constants.MaxGlobalEntities) return new GlobalIndex(_index); else return new SceneIndex(_index - Constants.MaxGlobalEntities);`
- Maintain all existing properties/methods unchanged

### Integration Points

- **EventBus Integration:** No changes needed — events already use `Entity` which now provides `PartitionIndex`
- **Selector System:** No changes needed — selectors iterate over entities, not indices
- **Property Bag:** No changes needed — uses `BehaviorRegistry<PropertyBag>` which auto-updates
- **Hierarchy System:** No changes needed — uses entity indices via `BehaviorRegistry`
- **Transform System:** No changes needed — uses entity indices via `BehaviorRegistry`

### Performance Considerations

**Allocation Hotspots:**
- All allocations happen at initialization time (registry constructors)
- `PartitionedSparseArray<T>` constructor allocates two `SparseArray<T>` instances (one global, one scene)
- Zero allocations during gameplay — only struct access and variant routing

**Cache Locality:**
- Global entities packed into dense 256-element array (cache-friendly)
- Scene entities in separate 8192-element array (prevents global pollution)
- Better CPU cache utilization for global entity iteration (UI, persistent state)

**Reset Performance:**
- Current: O(n) iteration over indices 256-8191, checking `HasValue()` for each
- New: O(1) `Scene.Clear()` call (built-in sparse array method)
- Expected speedup: 10-100x for scene clearing (measured in previous benchmarks)

**Variant Routing Overhead:**
- `PartitionIndex.Visit()` compiles to simple if/else check (no allocations)
- Inline-friendly pattern, likely optimized away by JIT
- @benchmarker: Verify no measurable overhead vs direct array access

### Data Structure Design

**SparseArray<T> Enhancement:**
```csharp
public sealed class SparseArray<T> where T : struct
{
    // NEW: Support int capacity for scene partition
    public SparseArray(int capacity) { /* existing logic */ }
    
    // Existing: Support ushort capacity for global partition  
    public SparseArray(ushort capacity) { /* existing logic */ }
}
```

**PartitionedSparseArray<T> Structure:**
```csharp
public sealed class PartitionedSparseArray<T> where T : struct
{
    private readonly SparseArray<T> _global;
    private readonly SparseArray<T> _scene;
    
    public SparseArray<T> Global => _global;
    public SparseArray<T> Scene => _scene;
    
    public T this[PartitionIndex index] { get; } // Routes via Visit
    public void Set(PartitionIndex index, T value) // Routes via Visit
    public SparseRef<T> GetMut(PartitionIndex index) // Routes via Visit
    public bool Remove(PartitionIndex index) // Routes via Visit
    public bool TryGetValue(PartitionIndex index, out T? value) // Routes via Visit
    public bool HasValue(PartitionIndex index) // Routes via Visit
    
    public void ClearScene() => _scene.Clear();
    public void ClearAll() { _global.Clear(); _scene.Clear(); }
}
```

**PartitionIndex Variant:**
```csharp
[Variant]
public readonly partial struct PartitionIndex
{
    static partial void VariantOf(GlobalIndex global, SceneIndex scene);
    
    // Implicit conversions for ergonomics
    public static implicit operator PartitionIndex(ushort globalIndex) => new GlobalIndex(globalIndex);
    public static implicit operator PartitionIndex(int sceneIndex) => new SceneIndex(sceneIndex);
}

public readonly record struct GlobalIndex(ushort Value);
public readonly record struct SceneIndex(int Value);
```

---

## Tasks

- [ ] Create `Core/PartitionIndex.cs` with variant type (GlobalIndex/SceneIndex) and implicit conversions
- [ ] Create `Core/PartitionedSparseArray.cs` with dual sparse array wrapper and partition routing
- [ ] Update `Core/SparseArray.cs` to support `int capacity` constructor overload
- [ ] Update `Core/Constants.cs` to split combined constants into partition-specific constants
- [ ] Update `Core/EntityRegistry.cs` to use `PartitionedSparseArray` and optimize reset logic
- [ ] Update `Core/Entity.cs` to expose `PartitionIndex Index` property
- [ ] Update `BED/BehaviorRegistry.cs` to use `PartitionedSparseArray`
- [ ] Update `Scenes/RuleRegistry.cs` to use `PartitionedSparseArray` and optimize reset logic
- [ ] Update `Sequencing/SequenceRegistry.cs` to use `PartitionedSparseArray` and optimize reset logic
- [ ] Run all existing tests to verify zero behavioral changes
- [ ] @benchmarker: Verify `PartitionIndex` variant routing has no measurable overhead vs direct array access
- [ ] @benchmarker: Verify `ResetEvent` performance improvement (expect 10-100x speedup for scene clearing)
