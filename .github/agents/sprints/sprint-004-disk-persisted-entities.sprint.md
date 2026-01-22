# Sprint: Disk-Persisted Entities

## Non-Technical Requirements

### IMPORTANT: Partitions vs Disk Persistence
**These are completely separate concepts:**
- **Persistent Partition** (indices 0-255): Entities survive **in-memory** across scene transitions (ResetEvent). Has NOTHING to do with disk.
- **Disk Persistence** (`PersistToDiskBehavior`): Saves entity data to LiteDB database. **Works for entities in EITHER partition** (persistent OR scene).

An entity can be:
1. Scene partition + no disk persistence (cleared on ResetEvent, not saved to disk)
2. Scene partition + disk persistence (cleared on ResetEvent, but saved to disk and can be reloaded)
3. Persistent partition + no disk persistence (survives ResetEvent in-memory, not saved to disk)
4. Persistent partition + disk persistence (survives ResetEvent in-memory AND saved to disk)

### Core Behavior
- `PersistToDiskBehavior(string Key)` marks entity for disk persistence, key is the DB pointer
- Key changes are supported — changing key swaps entity to different "save slot"
- Orphaned keys persist — old data stays in DB (can swap back later)
- Keys are pointers to data, not entity identities — nothing prevents changing a pointer and moving it around

### Mutation Tracking (Zero-Alloc)
- Any behavior mutation → `DatabaseRegistry.MarkDirty(entity.Index)` (sets `SparseArray<bool>`)
- No persistence check at mutation time → just flag and continue (zero overhead for non-persistent entities)
- Greedy flagging is faster — checking "does this persist?" on EVERY mutation would add overhead to 99% of entities that DON'T persist

### Batch Flush (Manual Trigger)
- Game engine/tests call `DatabaseRegistry.Flush()` at end-of-frame
- Flush logic:
  1. Get entities with `PersistToDiskBehavior` (from `BehaviorRegistry<PersistToDiskBehavior>`)
  2. Intersect with dirty flags (sparse array intersection)
  3. Serialize + write only dirty + persistent entities to LiteDB
  4. Clear dirty flags

### Serialization Rules
- Write to disk: All behaviors (including `PersistToDiskBehavior`)
- Load from disk: Apply all behaviors from JSON
- Infinite loop prevented by: `oldKey == newKey` check (skip DB operation when loading sets same key)

### Infinite Loop Prevention
On `BehaviorAddedEvent<PersistToDiskBehavior>` or `PostBehaviorUpdatedEvent<PersistToDiskBehavior>`:
- If **adding** → check DB (load if exists, write current state if not)
- If **updating** → compare `oldKey` vs `newKey`:
  - If `oldKey == newKey` → skip DB operation (prevents loop when loading from disk)
  - If `oldKey != newKey` → check DB for `newKey` (load if exists, write current state if not)

### Behavior Removal Handling
- Add `RemovalReason` enum: `Manual`, `Deactivation`, `Shutdown`
- Event signature: `PreBehaviorRemovedEvent<T>(Entity entity, RemovalReason reason)`
- If reason = `Deactivation` or `Shutdown`: do NOT write to disk (entity lifecycle event)
- If reason = `Manual`: write current state to disk, then stop persisting future mutations
- If `PersistToDiskBehavior` manually removed: entity becomes normal (no more persistence)

### Scene Load Protection
- Before scene load: `DatabaseRegistry.Disable()` (stops dirty tracking)
- After scene load: `DatabaseRegistry.Enable()` (resumes tracking)
- **Critical:** SceneLoader MUST apply `PersistToDiskBehavior` LAST (even after Parent), with comments enforcing this order for future maintainers

### Key Swap Feature
- Entity with `PersistToDiskBehavior("save-slot-1")` (HP=100 saved)
- Change to `PersistToDiskBehavior("save-slot-2")` (current HP=100)
- Current state (HP=100) writes to "save-slot-2", old "save-slot-1" still exists in DB
- Change back to `PersistToDiskBehavior("save-slot-1")` → loads HP=100 from slot 1
- **This is a feature, not a bug** — keys are pointers, data persists independently per key

### Success Criteria
- Any entity persisted to disk is reliably restored with all data when reloading, regardless of scene resets, shutdowns, or entity deactivations
- Deleting or deactivating an entity without an explicit command does NOT remove it from disk
- Mutations in the same frame batch together (one write per entity per frame)
- Key swaps and removal behave as expected (no data loss, keys freely reusable)
- No disk persistence test runs in parallel; database file wiped between tests
- Chaos test results validate all 27 edge cases (lifecycle, mutation timing, collisions, corruption, cross-scene, zero-alloc, LiteDB-specific, behavior order, test isolation, key swaps)

---

## Technical Requirements

### Architecture

#### 1. PersistToDiskBehavior (New Behavior)
- **File:** `Atomic.Net.MonoGame.BED/Persistence/PersistToDiskBehavior.cs`
  - `readonly record struct PersistToDiskBehavior(string Key)` — behavior marking entity for disk persistence
  - Implements `IBehavior<PersistToDiskBehavior>`
  - Uses `[JsonConverter(typeof(PersistToDiskBehaviorConverter))]` for JSON deserialization
  - Key is the database pointer (can be any non-empty string)
  - Empty/null keys should fire `ErrorEvent` and skip persistence

#### 2. RemovalReason Enum (Event System Enhancement)
- **File:** `Atomic.Net.MonoGame.Core/Events/RemovalReason.cs`
  - `enum RemovalReason { Manual, Deactivation, Shutdown }`
  - `Manual`: User explicitly removed behavior via `RemoveBehavior()` API
  - `Deactivation`: Entity deactivated, behaviors auto-removed
  - `Shutdown`: Engine shutdown, all behaviors cleared
  - Pattern: Enables different handling based on removal context

#### 3. PreBehaviorRemovedEvent Enhancement
- **File:** `Atomic.Net.MonoGame.Core/Events/PreBehaviorRemovedEvent.cs` (extend existing)
  - Add `RemovalReason Reason` property to event
  - Update all event publishers to pass appropriate reason
  - **Breaking change**: All `PreBehaviorRemovedEvent<T>` handlers must be updated
  - Pattern: Contextual information for removal handlers

#### 4. DatabaseRegistry (Central Persistence System)
- **File:** `Atomic.Net.MonoGame.Persistence/DatabaseRegistry.cs`
  - Singleton: `ISingleton<DatabaseRegistry>`
  - Responsibilities:
    - Track dirty entities via `SparseArray<bool>` (zero-alloc flagging)
    - Manage LiteDB connection lifecycle
    - Handle batch flush to disk
    - Disable/enable dirty tracking during scene loads
  - Properties:
    - `bool IsEnabled { get; private set; }` — dirty tracking enabled/disabled
    - `SparseArray<bool> _dirtyFlags` — tracks entities with mutations
  - Methods:
    - `void MarkDirty(ushort entityIndex)` — set dirty flag (no-op if disabled)
    - `void Flush()` — write dirty + persistent entities to LiteDB, clear flags
    - `void Disable()` — stop dirty tracking (for scene load)
    - `void Enable()` — resume dirty tracking (after scene load)
    - `void Initialize()` — setup LiteDB connection, create indexes
    - `void Shutdown()` — close LiteDB connection
  - Event handlers:
    - `IEventHandler<PostBehaviorUpdatedEvent<T>>` for ALL behaviors (generic subscription) → call `MarkDirty(entity.Index)` if enabled
    - `IEventHandler<BehaviorAddedEvent<PersistToDiskBehavior>>` → check DB, load if exists, write if not
    - `IEventHandler<PreBehaviorUpdatedEvent<PersistToDiskBehavior>>` → store old key
    - `IEventHandler<PostBehaviorUpdatedEvent<PersistToDiskBehavior>>` → compare old/new key, handle key swap
    - `IEventHandler<PreBehaviorRemovedEvent<PersistToDiskBehavior>>` → if Manual, write to disk; always stop future persistence
  - Pattern: Generic event handling for zero-alloc dirty tracking across all behavior types

#### 5. LiteDB Integration (External Dependency)
- **NuGet Package:** Add `LiteDB` to `Atomic.Net.MonoGame.Persistence` project
  - Version: Latest stable (5.x or 6.x as appropriate)
  - LiteDB provides embedded document database (NoSQL, file-based)
  - Zero-config, single-file storage (`persistence.db`)
- **Database Schema:**
  - Collection: `entities`
  - Document structure: `{ "_id": "<key>", "behaviors": [ ... ] }`
  - Index on `_id` (automatic via LiteDB)
  - Each document stores JSON array of all behaviors for an entity
- **Pattern:** Embedded database with JSON document storage (no SQL, no schema)

#### 6. Entity Serialization Strategy
- **Serializer:** Use existing `System.Text.Json` with behavior converters
- **Serialization format:**
  ```json
  {
    "_id": "player-save-slot-1",
    "behaviors": {
      "transform": { "position": [10, 20, 0], ... },
      "properties": { "health": 100, "mana": 50 },
      "persistToDisk": { "key": "player-save-slot-1" },
      "parent": { "parentIndex": 42 }
    }
  }
  ```
- **Include all behaviors:** Transform, Properties, Parent, PersistToDisk, etc.
- **Behavior discovery:** Iterate all `BehaviorRegistry<T>` instances to collect entity's behaviors
- **Deserialization:** Apply behaviors via `BehaviorRegistry<T>.SetBehavior()` (same as SceneLoader)
- **Pattern:** Reuse existing JSON infrastructure for consistency

#### 7. Persistence Project Structure
- **New Project:** `Atomic.Net.MonoGame.Persistence` (class library)
  - Depends on: `Atomic.Net.MonoGame.Core`, `Atomic.Net.MonoGame.BED`
  - References: `LiteDB` NuGet package
  - Contains: `DatabaseRegistry`, persistence logic
- **Integration:** MonoGame application references Persistence project, calls `Initialize()` on startup

### Integration Points

#### Event Flow (Mutation Tracking)
1. Any behavior updated via `BehaviorRegistry<T>.SetBehavior()` → fires `PostBehaviorUpdatedEvent<T>`
2. `DatabaseRegistry` listens to generic `PostBehaviorUpdatedEvent<T>` (via reflection or explicit subscriptions)
3. If `IsEnabled == true`, call `MarkDirty(entity.Index)` (sets `_dirtyFlags[entity.Index] = true`)
4. At end-of-frame, game calls `DatabaseRegistry.Instance.Flush()`
5. Flush logic:
   - Get all entities with `PersistToDiskBehavior` (via `BehaviorRegistry<PersistToDiskBehavior>`)
   - Intersect with `_dirtyFlags` to find dirty + persistent entities
   - For each dirty + persistent entity:
     - Serialize all behaviors to JSON
     - Write to LiteDB with key from `PersistToDiskBehavior.Key`
     - Clear dirty flag
6. Zero allocations (SparseArray iteration, no LINQ)

#### Event Flow (PersistToDiskBehavior Added)
1. Entity gets `PersistToDiskBehavior` via `BehaviorRegistry<PersistToDiskBehavior>.SetBehavior()`
2. `BehaviorAddedEvent<PersistToDiskBehavior>` fires
3. `DatabaseRegistry` handler:
   - Check if LiteDB has document with `behavior.Key`
   - If **exists**: Deserialize all behaviors from DB, apply to entity (overwrites current state)
   - If **not exists**: Serialize current entity state, write to DB with `behavior.Key`
4. Infinite loop prevented: When loading from DB sets `PersistToDiskBehavior`, `oldKey == newKey` so no DB operation

#### Event Flow (PersistToDiskBehavior Updated / Key Swap)
1. User updates `PersistToDiskBehavior` with new key via `BehaviorRegistry<PersistToDiskBehavior>.SetBehavior()`
2. `PreBehaviorUpdatedEvent<PersistToDiskBehavior>` fires → `DatabaseRegistry` stores `oldKey`
3. `PostBehaviorUpdatedEvent<PersistToDiskBehavior>` fires
4. `DatabaseRegistry` handler:
   - If `oldKey == newKey`: **Skip** (prevents loop when loading from DB)
   - If `oldKey != newKey`:
     - Check if LiteDB has document with `newKey`
     - If **exists**: Load behaviors from `newKey`, apply to entity
     - If **not exists**: Serialize current state, write to DB with `newKey`
     - Old `oldKey` document remains in DB (orphaned, can be reused)

#### Event Flow (PersistToDiskBehavior Removed)
1. User removes `PersistToDiskBehavior` via `BehaviorRegistry<PersistToDiskBehavior>.RemoveBehavior()`
2. `PreBehaviorRemovedEvent<PersistToDiskBehavior>` fires with `RemovalReason.Manual`
3. `DatabaseRegistry` handler:
   - If `reason == Manual`: Serialize current entity state, write to DB (final save)
   - If `reason == Deactivation` or `Shutdown`: Skip write (entity lifecycle event)
   - Stop future dirty tracking for this entity's persistence key
4. Entity becomes normal (no more automatic disk persistence)

#### Scene Load Integration
1. Before `SceneLoader.LoadGameScene()` or `LoadPersistentScene()`:
   - Call `DatabaseRegistry.Instance.Disable()` (stops dirty tracking)
2. SceneLoader loads entities, applies behaviors from JSON
   - **Critical:** Apply `PersistToDiskBehavior` LAST (even after Parent)
   - Add comment: "PersistToDiskBehavior MUST be applied last to prevent unwanted DB loads during scene construction"
3. After SceneLoader completes:
   - Call `DatabaseRegistry.Instance.Enable()` (resumes dirty tracking)
4. Pattern: Prevents scene load mutations from triggering disk writes

#### Registry Interactions
- **BehaviorRegistry<PersistToDiskBehavior>:** Stores persistence keys (sparse, most entities don't have this)
- **All BehaviorRegistry<T> instances:** Fire update events → DatabaseRegistry tracks dirty flags
- **DatabaseRegistry:** Central coordinator for dirty tracking and disk I/O
- **SceneLoader:** Calls Disable/Enable around scene loads, applies PersistToDiskBehavior last
- **LiteDB:** External persistence layer (file-based document store)

### Performance Considerations

#### Allocation Strategy
- **Dirty tracking:** `SparseArray<bool>` allocated once at startup (zero runtime allocation)
- **MarkDirty:** Single boolean set operation (no allocation, cache-friendly)
- **Flush:** Iterates sparse arrays (zero-alloc enumerators), serializes JSON (acceptable batch allocation)
- **LiteDB writes:** File I/O allocations (acceptable, not hot path)
- **Pattern:** All gameplay allocations are zero, batch operations allocate (load/save only)

#### Dirty Tracking Performance
- **Greedy flagging:** Every behavior update calls `MarkDirty()` (even non-persistent entities)
- **Why greedy?** Checking "does this entity have PersistToDiskBehavior?" on EVERY mutation would require BehaviorRegistry lookup (slower than setting bool)
- **Cost:** Single boolean set per mutation (~1 CPU cycle)
- **Benefit:** No branching, no lookups, zero overhead for non-persistent entities
- **Trade-off:** Acceptable (dirty flags cleared on flush, sparse storage)

#### Batch Flush Performance
- **Intersection logic:** Iterate `BehaviorRegistry<PersistToDiskBehavior>`, check `_dirtyFlags[entity.Index]`
- **Complexity:** O(persistent entities) — most entities don't persist, so loop is tiny
- **Expected:** <1ms for 100 persistent entities with 10 dirty per frame
- **Pattern:** Batch writes at end-of-frame (not per-mutation)

#### LiteDB Performance
- **Embedded database:** No network overhead, single-file storage
- **Write batching:** Flush writes all dirty entities in single transaction (fast)
- **Index lookups:** O(1) via LiteDB's internal B-tree index on `_id`
- **Expected:** <10ms for 100 entity writes (file I/O bound)
- **Pattern:** Acceptable for end-of-frame batch operation

#### Potential Hotspots
- **Generic event subscription:** Subscribing to `PostBehaviorUpdatedEvent<T>` for ALL behavior types
  - **Challenge:** C# doesn't support generic constraints on event handlers easily
  - **Solution:** Use reflection at startup to subscribe to all known `BehaviorRegistry<T>` types, or explicit subscriptions
  - **Alternative:** Manual event bus registration per behavior type (more code, but clearer)
- **JSON serialization during flush:** May allocate temp strings/buffers
  - **Mitigation:** Acceptable (batch operation, not hot path)
- **LiteDB file I/O:** Disk writes may stall if file is locked
  - **Mitigation:** Handle exceptions, fire `ErrorEvent`, continue gameplay

#### Benchmarking Requests
- [ ] @benchmarker **Test dirty tracking overhead**: Measure `MarkDirty()` cost per mutation (should be <1ns)
  - Hypothesis: Single boolean set is negligible overhead
  - Compare: Greedy flagging vs conditional check (is entity persistent?)
- [ ] @benchmarker **Test flush performance**: Measure flush time for varying dirty entity counts (10, 100, 1000)
  - Hypothesis: Linear scaling, <10ms for 100 entities
- [ ] @benchmarker **Test LiteDB write throughput**: Measure batch write performance for large entity counts
  - Hypothesis: File I/O is bottleneck, ~100-1000 entities/sec

---

## Tasks

### Infrastructure Setup
- [ ] Create `Atomic.Net.MonoGame.Persistence` project (class library)
- [ ] Add `LiteDB` NuGet package to Persistence project
- [ ] Reference Persistence project from MonoGame application
- [ ] Define `RemovalReason` enum in Core project
- [ ] Update `PreBehaviorRemovedEvent<T>` to include `RemovalReason Reason` property
- [ ] Update all `PreBehaviorRemovedEvent<T>` publishers to pass appropriate `RemovalReason`

### Core Persistence Behavior
- [ ] Create `PersistToDiskBehavior` struct with `string Key` property
- [ ] Create `PersistToDiskBehaviorConverter` for JSON deserialization
- [ ] Add `PersistToDiskBehavior?` property to `JsonEntity` (extend existing)
- [ ] Validate empty/null keys fire `ErrorEvent` and skip persistence

### Database Registry (Dirty Tracking)
- [ ] Create `DatabaseRegistry` singleton with `SparseArray<bool> _dirtyFlags`
- [ ] Implement `MarkDirty(ushort entityIndex)` method (set dirty flag if enabled)
- [ ] Implement `Disable()` and `Enable()` methods (scene load protection)
- [ ] Subscribe to `PostBehaviorUpdatedEvent<T>` for all behavior types (generic or explicit)
- [ ] Implement dirty flag clearing after flush

### Database Registry (LiteDB Integration)
- [ ] Implement `Initialize()` method (create/open LiteDB connection to `persistence.db`)
- [ ] Implement `Shutdown()` method (close LiteDB connection)
- [ ] Create LiteDB collection `entities` with `_id` index
- [ ] Implement entity serialization (collect all behaviors from BehaviorRegistry instances)
- [ ] Implement entity deserialization (apply behaviors via `BehaviorRegistry<T>.SetBehavior()`)

### Database Registry (Flush Logic)
- [ ] Implement `Flush()` method:
  - Get all entities with `PersistToDiskBehavior`
  - Intersect with `_dirtyFlags` to find dirty + persistent entities
  - Serialize each dirty entity to JSON
  - Write to LiteDB with key from `PersistToDiskBehavior.Key`
  - Clear dirty flags
- [ ] Handle LiteDB exceptions (locked file, disk full, etc.) → fire `ErrorEvent`, continue

### Database Registry (PersistToDiskBehavior Events)
- [ ] Handle `BehaviorAddedEvent<PersistToDiskBehavior>`:
  - Check DB for existing document with `behavior.Key`
  - If exists: Load all behaviors from DB, apply to entity
  - If not exists: Serialize current entity state, write to DB
- [ ] Handle `PreBehaviorUpdatedEvent<PersistToDiskBehavior>`:
  - Store `oldKey` for comparison in post-update handler
- [ ] Handle `PostBehaviorUpdatedEvent<PersistToDiskBehavior>`:
  - Compare `oldKey` vs `newKey`
  - If equal: Skip (infinite loop prevention)
  - If different: Check DB for `newKey`, load if exists, write if not
- [ ] Handle `PreBehaviorRemovedEvent<PersistToDiskBehavior>`:
  - If `reason == Manual`: Serialize current state, write to DB (final save)
  - If `reason == Deactivation` or `Shutdown`: Skip write
  - Stop future dirty tracking for this entity

### SceneLoader Integration
- [ ] Update `SceneLoader` to call `DatabaseRegistry.Instance.Disable()` before loading
- [ ] Update `SceneLoader` to call `DatabaseRegistry.Instance.Enable()` after loading
- [ ] Apply `PersistToDiskBehavior` LAST in SceneLoader (after Transform, Properties, Parent)
- [ ] Add comment: "PersistToDiskBehavior MUST be applied last to prevent unwanted DB loads during scene construction"

### Chaos Testing (27 Edge Cases)
- [ ] #test-architect Create integration tests for all 27 chaos scenarios:
  - **Lifecycle edge cases** (4 tests):
    - ResetEvent does NOT delete from disk
    - ShutdownEvent does NOT delete from disk
    - Entity deactivation does NOT delete from disk
    - Behavior removal DOES persist to disk (if `RemovalReason = Manual`)
  - **Mutation & save timing** (3 tests):
    - Rapid mutations don't corrupt save (only last value per frame written)
    - Mutations during scene load are saved (after re-enable)
    - Mutations before PersistToDiskBehavior applied are NOT saved (disabled during load)
  - **Collision & duplicate keys** (4 tests):
    - Duplicate keys in same scene (first-write-wins or ErrorEvent?)
    - Duplicate keys across scenes (design decision needed)
    - Empty string key (ErrorEvent)
    - Null key (skip behavior, no persistence)
  - **Disk file corruption** (3 tests):
    - Corrupt JSON in LiteDB (ErrorEvent, fall back to defaults)
    - Missing disk file (use scene defaults, no error)
    - Partial entity save (disk has subset of behaviors, scene provides rest)
  - **Cross-scene persistence** (2 tests):
    - Persistent partition entity survives ResetEvent in-memory (regardless of disk persistence)
    - Scene partition entity with `PersistToDiskBehavior` does NOT survive ResetEvent in-memory, but disk data persists and can be reloaded
  - **Zero-allocation validation** (2 tests):
    - Save operation allocates zero bytes (event handling must be zero-alloc)
    - Load operation allocates only at scene load
  - **LiteDB-specific edge cases** (3 tests):
    - Database locked by external process (ErrorEvent, no crash)
    - Database file deleted mid-game (ErrorEvent or recreate?)
    - Database grows indefinitely (cleanup strategy needed?)
  - **Behavior order & dependencies** (2 tests):
    - PersistToDiskBehavior applied LAST (even after Parent, with comment)
    - Removing PersistToDiskBehavior stops persistence (no future writes)
  - **Test isolation** (2 tests):
    - LiteDB wiped between tests
    - Tests CANNOT run in parallel
  - **Key swap scenarios** (2 tests):
    - Key swap preserves both save slots
    - Rapid key swapping doesn't corrupt data

### Performance Validation
- [ ] @benchmarker Benchmark dirty tracking overhead (`MarkDirty()` cost per mutation)
- [ ] @benchmarker Benchmark flush performance (varying dirty entity counts)
- [ ] @benchmarker Benchmark LiteDB write throughput (batch write performance)

### Documentation
- [ ] Add XML doc comments to `DatabaseRegistry` public APIs
- [ ] Add XML doc comments to `PersistToDiskBehavior`
- [ ] Document `RemovalReason` usage patterns
- [ ] Create example JSON with `persistToDisk` behavior
- [ ] Document key swap feature and orphaned key behavior
- [ ] Add design guidelines for when to use disk persistence

---

## Example JSON Structure

```json
{
  "entities": [
    {
      "id": "player-character",
      "transform": { "position": [100, 200, 0] },
      "properties": {
        "health": 100,
        "mana": 50,
        "level": 5
      },
      "persistToDisk": { "key": "player-save-slot-1" }
    },
    {
      "id": "inventory-manager",
      "properties": {
        "gold": 1000,
        "inventory-slots": 20
      },
      "persistToDisk": { "key": "inventory-persistent" }
    }
  ]
}
```

**Expected Behavior:**
- On first load: Both entities created, current state written to LiteDB
- On scene reload: Both entities load state from LiteDB (Transform, Properties restored)
- During gameplay: Any property mutation → `MarkDirty()` → next `Flush()` writes to disk
- On `ResetEvent`: Entities deactivated, but disk data remains intact
- On next scene load: Entities restored from disk with all saved state

---

## Key Swap Example

```json
// Initial scene load
{
  "id": "player",
  "properties": { "health": 100 },
  "persistToDisk": { "key": "save-slot-1" }
}
```

**Sequence:**
1. Load scene → player created with HP=100, written to LiteDB at key `"save-slot-1"`
2. Gameplay → player takes damage, HP=50, `Flush()` writes to `"save-slot-1"`
3. Update behavior: `PersistToDiskBehavior("save-slot-2")`
   - Current state (HP=50) written to `"save-slot-2"`
   - `"save-slot-1"` still exists in DB with HP=50 (orphaned)
4. Update behavior: `PersistToDiskBehavior("save-slot-1")`
   - Load state from `"save-slot-1"` → HP=50 restored
   - `"save-slot-2"` still exists in DB (orphaned)

**Result:** Both save slots persist independently, keys are freely reusable.

---

## Notes for Implementation

### Design Decisions
- **Greedy dirty tracking:** Mark all mutations dirty (no per-entity persistence check)
  - **Rationale:** Faster than checking persistence on every mutation (99% of entities don't persist)
- **Batch flush:** Write to disk at end-of-frame, not per-mutation
  - **Rationale:** Reduces I/O overhead, consolidates writes
- **Key swap behavior:** Orphaned keys persist in DB (reusable)
  - **Rationale:** Keys are pointers, not identities — enables save slot management
- **Manual removal saves:** `RemovalReason.Manual` writes final state to disk
  - **Rationale:** User explicitly removed persistence, save current state before stopping
- **Scene load disable:** Stop dirty tracking during scene load
  - **Rationale:** Prevents scene construction from triggering disk writes
- **PersistToDiskBehavior applied last:** SceneLoader applies this behavior after all others
  - **Rationale:** Prevents DB loads from overwriting scene construction (infinite loop prevention)

### Integration with Future Sprints
- **Query System (Future):** Disk-persisted entities can be queried via PropertyBag or custom behaviors
- **Command System (Future):** Commands can trigger `Flush()` or manage save slots
- **Scene Transitions (Future):** Scene manager can call `Flush()` before transitions

### Risks and Mitigations
- **Risk:** Generic event subscription complexity (subscribing to all `PostBehaviorUpdatedEvent<T>` types)
  - **Mitigation:** Use reflection at startup to discover all `BehaviorRegistry<T>` types and subscribe
  - **Alternative:** Explicit subscriptions per known behavior type (more code, but clearer)
- **Risk:** LiteDB file corruption or lock contention
  - **Mitigation:** Fire `ErrorEvent`, continue gameplay, attempt retry on next flush
- **Risk:** Infinite loop if PersistToDiskBehavior loading triggers itself
  - **Mitigation:** `oldKey == newKey` check skips DB operation when loading sets same key
- **Risk:** Scene load mutations trigger unwanted disk writes
  - **Mitigation:** `Disable()` dirty tracking before scene load, `Enable()` after
- **Risk:** Test isolation issues (parallel tests, shared DB file)
  - **Mitigation:** Tests must run sequentially, wipe DB between tests

### Out of Scope / Deferred
- Runtime DB compaction/cleanup (orphaned keys accumulate indefinitely)
- Encryption or compression of saved data
- Cloud sync or multi-device save sync
- Versioning or migration of saved data (schema changes)
- Undo/redo for saved state
- Save slot UI or management commands (designer-facing tools)

---

## Success Metrics

- **Reliability:** 100% of chaos tests pass (27/27 edge cases validated)
- **Performance:** Zero allocations during gameplay (dirty tracking only), <10ms flush time for 100 entities
- **Developer experience:** Designers can mark any entity for persistence without C# changes
- **Data integrity:** No data loss from key swaps, removal, deactivation, or corruption scenarios
- **Test coverage:** All lifecycle, mutation timing, collision, corruption, cross-scene, and key swap scenarios covered
