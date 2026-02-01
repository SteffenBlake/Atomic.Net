# Sprint: Partitioned Sparse Array Split

## Non-Technical Requirements

### What We're Building
- Split all partitioned systems (Entities, Behaviors, Rules, Sequences) from using a single large sparse array into two separate physical sparse arrays: one for global partition, one for scene partition
- Introduce a `PartitionIndex` variant type (`ushort` for global, `uint` for scene) to enforce type safety at compile time
- Introduce a `PartitionedSparseArray<T>` wrapper that accepts `PartitionIndex` and internally routes to the correct underlying array
- Update all constants to reflect the new partition split (remove combined capacity constants like `MaxEntities`, replace with `MaxGlobalEntities` (ushort) and `MaxSceneEntities` (uint))

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
- Constants updated everywhere: `MaxGlobalX` (ushort), `MaxSceneX` (uint), no combined `MaxX` constants
- Zero gameplay allocations (refactor only changes structure, not allocation behavior)
- Partitioned Sparse Array exposes both of its sparse arrays for access and enumeration, counting, etc (just expose the sparse arrays as public properties)

### Constraints
- Partition routing must be hidden inside `PartitionedSparseArray` — 99% of code should not see `Visit` or `TryMatch` patterns
- Global partition indices: `ushort`
- Scene partition indices: `uint`
- No pointer references, indices only
- Must support all existing partition behaviors (global persists through `ResetEvent`, scene clears)

### Example Scenario

**Before (current):**
```csharp
// Single array, implicit partition logic
SparseArray<Entity> _entities = new(capacity: 8192);
Entity entity = _entities[index]; // Is this global or scene? You have to remember.
```

**After (target):**
```csharp
// Two arrays, explicit partition via variant type
PartitionedSparseArray<Entity> _entities = new(
    Constants.MaxGlobalEntities,  // ushort: 256
    Constants.MaxSceneEntities     // uint: 8192
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
- Variant type with two cases: `ushort` (global) and `uint` (scene)
- Provides type-safe compile-time distinction between partition types
- Implicit conversion operators from `ushort` (global) and `uint` (scene) for ergonomic usage
- Zero-allocation struct-based design

**2. `MonoGame/Atomic.Net.MonoGame/Core/PartitionedSparseArray.cs`**
- Generic wrapper over two underlying `SparseArray<T>` instances
- Primary constructor: `PartitionedSparseArray(ushort globalCapacity, uint sceneCapacity)`
- Public readonly fields exposing both sparse arrays:
  - `public readonly SparseArray<T> Global` - for direct access/enumeration of global partition
  - `public readonly SparseArray<T> Scene` - for direct access/enumeration of scene partition
- Indexer accepts `PartitionIndex` and routes to correct underlying array using `.TryMatch()` pattern (Visit would closure)
- Routing logic stays internal

**3. `MonoGame/Atomic.Net.MonoGame/Core/PartitionedSparseRefArray.cs`**
- Same as PartitionedSparseArray but for `SparseReferenceArray<T>` (used by SequenceDriver)
- Primary constructor: `PartitionedSparseRefArray(ushort globalCapacity, uint sceneCapacity)`
- Public readonly fields exposing both sparse arrays:
  - `public readonly SparseArray<T> Global` - for direct access/enumeration of global partition
  - `public readonly SparseArray<T> Scene` - for direct access/enumeration of scene partition
- Indexer accepts `PartitionIndex` and routes to correct underlying array using `.TryMatch()` pattern (Visit would closure)
- Routing logic stays internal

**4. Update `MonoGame/Atomic.Net.MonoGame/Core/SparseArray.cs`**
- Update existing `ushort capacity` constructor to accept `int capacity` instead (Yes, PartitionedSparseArray will pass in a ushort to become int here)
- All internal arrays sized based on capacity parameter
- Minimal changes — just support larger capacity values via `int`

**5. Update `MonoGame/Atomic.Net.MonoGame/Core/Constants.cs`**
- Remove combined capacity constants: `MaxEntities`, `MaxRules`, `MaxSequences`
- Replace with partition-specific constants:
  - **Entities:**
    - `MaxGlobalEntities` (ushort, default 256) — already exists
    - `MaxSceneEntities` (uint, default 8192) — NEW
  - **Rules:**
    - `MaxGlobalRules` (ushort, default 128) — already exists
    - `MaxSceneRules` (uint, default 1024) — NEW
  - **Sequences:**
    - `MaxGlobalSequences` (ushort, default 256) — already exists  
    - `MaxSceneSequences` (uint, default 512) — NEW
- Add preprocessor directive support for compile-time overrides (same pattern as existing constants)

#### Registry Updates

**6. Update `MonoGame/Atomic.Net.MonoGame/Core/EntityRegistry.cs`**
- Replace `SparseArray<bool> _active` with `PartitionedSparseArray<bool> _active`
- Replace `SparseArray<bool> _enabled` with `PartitionedSparseArray<bool> _enabled`
- Update allocators: `_nextGlobalIndex` remains `ushort`, `_nextSceneIndex` becomes `uint`
- `Activate()` returns scene entity with `uint` based scene index (implicit cast to a PartitionIndex)
- `ActivateGlobal()` returns global entity with `ushort` global index  (implicit cast to a PartitionIndex)
- `ResetEvent` handler: replace loop with `_active.Scene.Clear()` and `_enabled.Scene.Clear()`
- `ShutdownEvent` handler: replace manual loop with clearing both partitions
- Entity lookup remains index-based (array of pre-allocated Entity structs)

**7. Update `MonoGame/Atomic.Net.MonoGame/BED/BehaviorRegistry.cs`**
- Replace `SparseArray<TBehavior> _behaviors` with `PartitionedSparseArray<TBehavior> _behaviors`
- No major changes to public API (entity indices auto-route to correct partition)
- Handles `PreEntityDeactivatedEvent` without partition awareness

**8. DELETE `MonoGame/Atomic.Net.MonoGame/BED/RefBehaviorRegistry.cs`**
- NOTE: this can be deleted. You can also delete IBehavior, remove the CreateFor method off all entities, everything that was involved RefBehaviorRegistry is not actually used, only BehaviorRegistry is used
- Yes, IBehavior CAN be deleted, it is an old remnant from the RefBehaviorRegistry
- Yes, remove CreateFor off all behaviors
- Yes, remove the IBehavior constraints off anything that has them (leave the struct constraint tho)

**9. Update `MonoGame/Atomic.Net.MonoGame/Scenes/RuleRegistry.cs`**
- Replace `SparseArray<JsonRule> _rules` with `PartitionedSparseArray<JsonRule> _rules`
- Update allocators: `_nextGlobalRuleIndex` remains `ushort`, `_nextSceneRuleIndex` becomes `uint`
- `ResetEvent` handler: replace loop with `_rules.Scene.Clear()`
- `ShutdownEvent` handler: replace loop with clearing both partitions
- Keep `Rules` public property exposing `PartitionedSparseArray<JsonRule>` for tests

**10. Update `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceRegistry.cs`**
- Replace `SparseArray<JsonSequence> _sequences` with `PartitionedSparseArray<JsonSequence> _sequences`
- Update allocators: `_nextGlobalSequenceIndex` remains `ushort`, `_nextSceneSequenceIndex` becomes `uint`
- `ResetEvent` handler: replace loop with direct iteration over `_sequences.Scene` + `_sequences.Scene.Clear()`
- `ShutdownEvent` handler: replace loop with clearing both partitions + `_idToIndex.Clear()`
- _idToIndex needs to map to a PartitionIndex now
- Keep `Sequences` public property exposing `PartitionedSparseArray<JsonSequence>` for tests

**11. Update `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceDriver.cs`**
- Replace `SparseReferenceArray<SparseArray<SequenceState>> _activeSequences` with `PartitionedSparseRefArray<PartitionedSparseArray<SequenceState>> _activeSequences`
- No major changes to public API (entity indices should auto-route to correct partition)
- Some functions may need to use a PartitionIndex now instead of just ushort here (_index cant be passed as its Variant type into being set as a JsonNode, you will probably need to put a .ToJsonValue() function on PartitionIndex and use Visit on it to convert its "inner" types to a JsonValue

**12. Update `MonoGame/Atomic.Net.MonoGame/Selectors/*EntitySelector.cs`**
- Replace `SparseArray<bool> Matches` with `PartitionedSparseArray<bool> Matches` in all selector implementations
- Update capacity initialization to use partition-specific constants
- No changes to selector logic (just underlying storage)

#### Entity Type Update

**13. Update `MonoGame/Atomic.Net.MonoGame/Core/Entity.cs`**
- Change `Index` property type from `ushort` to `PartitionIndex`
- Maintain all existing properties/methods

### Integration Points

- **EventBus Integration:** No changes needed — events already use `Entity` which now provides `PartitionIndex`
- **Selector System:** Updated to use `PartitionedSparseArray<bool>` for match tracking
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
public sealed class SparseArray<T>(int capacity) where T : struct
{
    // Updated: Single constructor accepting int capacity for both partitions
    // Existing ushort constructor replaced with int
}
```

**PartitionedSparseArray<T> Structure:**
```csharp
public sealed class PartitionedSparseArray<T>(ushort globalCapacity, uint sceneCapacity) 
    where T : struct
{
    public readonly SparseArray<T> Global = new(globalCapacity);
    public readonly SparseArray<T> Scene = new(sceneCapacity);

    // CRITICAL: We cant use .Visit on any of these methods below because it will closure, which is not okay
    // MUST use TryMatch
    public T this[PartitionIndex index]
    {
        ...
    }
    
    public void Set(PartitionIndex index, T value) => ...
    
    public SparseRef<T> GetMut(PartitionIndex index) => ...
    
    public bool Remove(PartitionIndex index) => ...
    
    public bool TryGetValue(PartitionIndex index, out T? value) => ...
    
    public bool HasValue(PartitionIndex index) => ...
}
```

**PartitionIndex Variant:**
```csharp
[Variant]
public readonly partial struct PartitionIndex
{
    static partial void VariantOf(ushort global, uint scene);

    public AsJsonValue() ...
}
```

---

## Tasks

- [ ] Create `Core/PartitionIndex.cs` with variant type (ushort/int) and implicit conversions
- [ ] Create `Core/PartitionedSparseArray.cs` with dual sparse array wrapper and partition routing
- [ ] Create `Core/PartitionedSparseRefArray.cs` for SparseReferenceArray variant
- [ ] Update `Core/SparseArray.cs` to use `int capacity` constructor (remove ushort overload)
- [ ] Update `Core/Constants.cs` to split combined constants into partition-specific constants
- [ ] Update `Core/EntityRegistry.cs` to use `PartitionedSparseArray` and optimize reset logic
- [ ] Update `Core/Entity.cs` to change `Index` property type to `PartitionIndex`
- [ ] Update `BED/BehaviorRegistry.cs` to use `PartitionedSparseArray`
- [ ] Update `BED/RefBehaviorRegistry.cs` to use `PartitionedSparseArray`
- [ ] Update `Scenes/RuleRegistry.cs` to use `PartitionedSparseArray` and optimize reset logic
- [ ] Update `Sequencing/SequenceRegistry.cs` to use `PartitionedSparseArray` and optimize reset logic
- [ ] Update `Sequencing/SequenceDriver.cs` to use `PartitionedSparseRefArray`
- [ ] Update all `Selectors/*EntitySelector.cs` to use `PartitionedSparseArray<bool>`
- [ ] Run all existing tests to verify zero behavioral changes
