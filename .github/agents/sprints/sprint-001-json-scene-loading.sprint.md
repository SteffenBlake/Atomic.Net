# Sprint: JSON Scene Loading (M1)

## Non-Technical Requirements

### User Story
- As a game designer, I want to define a scene in a JSON file so that I can create game content without writing C# code

### Success Criteria
- A scene can be defined in `Content/scenes/main-menu.json`
- The file contains an array of entities with optional behaviors
- Each entity in the JSON array creates an `Entity` in the engine
- Entities are spawned in the **scene partition** (indices ≥32)
- If an entity has a `"transform"` property, it gets a `TransformBehavior`
- If an entity has a `"parent"` property, it gets a `Parent` behavior
- All behavior fields are optional (missing fields use C# defaults)
- An entity with `"id": "player"` can be referenced by `"parent": "#player"`
- First entity with an ID wins (duplicates are ignored, no error)
- Unresolved references (`"parent": "#missing-id"`) fire an error event but don't crash
- Invalid JSON emits error event (doesn't throw)
- Missing files emit error event (doesn't throw)
- Users can listen for error events to log issues

### Out of Scope for M1
- Loading screen (`loading.json`) - M1.5/M2
- Scene transitions (switching between scenes) - M1.5/M2
- Scene unloading (cleanup) - M1.5/M2
- Tags/classes for entity selection - Future sprint
- Advanced selectors (query multiple entities) - Future sprint
- Gameplay logic behaviors - M3+

---

## Technical Requirements

### Architecture

#### 1. JSON Data Model
- **File:** `Atomic.Net.MonoGame.Scenes/JsonModels/JsonEntity.cs`
  - Reference type class (big object, allocation at load time only)
  - Properties: `string? Id`, `TransformBehavior? Transform`, `string? Parent`
  - All behavior properties are nullable (optional in JSON)
  - Uses actual behavior structs directly (no separate JSON models)

- **File:** `Atomic.Net.MonoGame.Scenes/JsonModels/JsonScene.cs`
  - Root container for scene JSON
  - Property: `List<JsonEntity> Entities`

#### 2. JSON Converters
- **File:** `Atomic.Net.MonoGame.Scenes/JsonConverters/TransformBehaviorConverter.cs`
  - Custom `System.Text.Json.JsonConverter<TransformBehavior>`
  - Deserializes transform data directly into TransformBehavior struct
  - Handles Position (Vector3), Rotation (Quaternion), Scale (Vector3), Anchor (Vector3)
  - Missing fields use C# defaults (Position/Anchor: Zero, Rotation: Identity, Scale: One)
  - Returns default behavior on invalid data (graceful degradation)

#### 3. Entity ID Registry
- **File:** `Atomic.Net.MonoGame.Scenes/EntityId.cs`
  - `readonly record struct EntityId(string Id)` - behavior for entity ID tracking
  - Implements `IBehavior<EntityId>`

- **File:** `Atomic.Net.MonoGame.Scenes/EntityIdRegistry.cs`
  - Singleton pattern via `ISingleton<EntityIdRegistry>`
  - Maintains `Dictionary<string, Entity>` for ID-to-Entity lookup
  - Method: `TryRegister(Entity entity, string id)` - returns true if registered (first-write-wins)
  - Method: `TryResolve(string id, out Entity entity)` - lookup entity by ID
  - Handles `BehaviorAddedEvent<EntityId>` to track IDs
  - Handles `PreEntityDeactivatedEvent` to clean up IDs
  - Handles `ResetEvent` to clear scene entity IDs

#### 4. Scene Loader System
- **File:** `Atomic.Net.MonoGame.Scenes/SceneLoader.cs`
  - Singleton pattern via `ISingleton<SceneLoader>`
  - Method: `LoadGameScene(string scenePath)` - loads game scene JSON (uses `EntityRegistry.Activate()`)
  - Method: `LoadGlobalScene(string scenePath)` - loads persistent scene JSON (uses `EntityRegistry.ActivatePersistent()`)
  - Two-pass loading:
    - **Pass 1:** Create all entities, apply behaviors (Transform, EntityId)
    - **Pass 2:** Resolve parent references, fire error events for unresolved IDs
  - Forces GC after loading completes (prevents lag in game scene)

#### 5. Error Events
- **File:** `Atomic.Net.MonoGame.Core/ErrorEvent.cs`
  - `readonly record struct ErrorEvent(string Message, Exception? Exception)`
  - General-purpose error event (not scene-specific)
  - Fired for: file not found, JSON parse errors, unresolved references, invalid data
  - Users register handlers via `EventBus<ErrorEvent>`

#### 6. Integration with Existing Systems
- Uses `EntityRegistry.Instance.Activate()` for game scene entities (indices ≥256)
- Uses `EntityRegistry.Instance.ActivatePersistent()` for global scene entities (indices <256)
- Uses `BehaviorRegistry<TransformBehavior>.Instance.SetBehavior()` to apply transforms
- Uses `BehaviorRegistry<Parent>.Instance.SetBehavior()` to set parent relationships
- Uses `BehaviorRegistry<EntityId>.Instance.SetBehavior()` to track entity IDs
- Uses `EventBus<ErrorEvent>` for error handling

### Integration Points

#### Event Flow
1. User calls `SceneLoader.Instance.LoadGameScene("Content/scenes/main-menu.json")` or `LoadGlobalScene(...)`
2. SceneLoader reads file, deserializes JSON using `System.Text.Json`
3. **Pass 1:** For each `JsonEntity`:
   - Call `EntityRegistry.Instance.Activate()` (or `ActivatePersistent()`) to spawn entity
   - If `jsonEntity.Transform != null`, call `BehaviorRegistry<TransformBehavior>.Instance.SetBehavior(entity, behavior => behavior = jsonEntity.Transform.Value)`
   - If `jsonEntity.Id != null`, call `BehaviorRegistry<EntityId>.Instance.SetBehavior(entity, behavior => behavior = new EntityId(jsonEntity.Id))`
4. **Pass 2:** For each entity with `jsonEntity.Parent != null`:
   - Call `EntityIdRegistry.Instance.TryResolve(parentId, out parentEntity)` (strip `#` prefix)
   - If found, call `BehaviorRegistry<Parent>.Instance.SetBehavior(entity, behavior => behavior = new Parent(parentEntity.Index))`
   - If not found, fire `EventBus<ErrorEvent>.Push(new("Unresolved parent reference: #id", null))`
5. Force GC: `GC.Collect()` and `GC.WaitForPendingFinalizers()` to clean up ephemeral JSON objects

#### Registry Interactions
- **EntityRegistry:** Activates game scene entities (indices ≥32) or loading scene entities (indices <32)
- **BehaviorRegistry<TransformBehavior>:** Stores transform data via `SetBehavior()`
- **BehaviorRegistry<Parent>:** Stores parent references via `SetBehavior()`
- **BehaviorRegistry<EntityId>:** Stores entity ID mappings via `SetBehavior()`
- **EntityIdRegistry:** Tracks ID-to-Entity lookup (updated via `BehaviorAddedEvent<EntityId>`)
- **HierarchyRegistry:** Automatically updated via Parent behavior events (no direct interaction)

#### Error Handling
- **File not found:** Fire `ErrorEvent`, return early
- **Invalid JSON:** Catch `JsonException`, fire `ErrorEvent`, return early
- **Unresolved reference:** Fire `ErrorEvent`, continue loading other entities
- **Invalid transform data:** Converter returns default behavior, fire `ErrorEvent`, continue

### Performance Considerations

#### Allocation Strategy
- All JSON deserialization allocations happen at load time (acceptable)
- `JsonEntity` objects are ephemeral (GC'd after loading completes via forced GC)
- `EntityIdRegistry` maintains persistent ID dictionary (cleared on `ResetEvent`)
- No allocations during gameplay (entities/behaviors already in SparseArrays)

#### Two-Pass Loading
- **Why:** Ensures all entities exist before resolving parent references
- **Trade-off:** Iterates entities twice, but keeps code simple
- **Alternative (not recommended):** Deferred resolution with queues (more complex, same perf)

#### Potential Hotspots
- JSON deserialization (uses `System.Text.Json`, fast)
- Dictionary lookups for ID resolution (O(1) per reference)
- No SIMD/cache concerns (load-time only)

#### Benchmarking Requests
- None needed for M1 (loading is not hot path)
- **M1.5 consideration:** Need background thread loading for non-blocking scene loads (thousands of entities)
  - Loading must not block game thread (allows loading scene animations to continue)
  - Defer actual loading to background thread, keep main thread responsive

---

## Tasks

### Setup and Infrastructure
- [ ] Create `Atomic.Net.MonoGame.Scenes` project structure for JSON loading
- [ ] Define JSON data model classes (`JsonScene`, `JsonEntity`)
- [ ] Implement custom JSON converter for `TransformBehavior` struct
- [ ] Define `ErrorEvent` in Core (general-purpose error event)

### Entity ID System
- [ ] Define `EntityId` behavior struct
- [ ] Implement `EntityIdRegistry` singleton with ID-to-Entity lookup
- [ ] Handle `BehaviorAddedEvent<EntityId>` for tracking
- [ ] Handle `PreEntityDeactivatedEvent` and `ResetEvent` for cleanup

### Core Scene Loading Logic
- [ ] Implement `SceneLoader` singleton with two-pass loading algorithm
- [ ] Implement `LoadGameScene(string scenePath)` method (spawns game scene entities)
- [ ] Implement `LoadGlobalScene(string scenePath)` method (spawns global scene entities)
- [ ] Force GC after loading completes (`GC.Collect()` + `GC.WaitForPendingFinalizers()`)
- [ ] Implement parent reference resolution with error events for unresolved IDs

### Integration Tests
- [x] #test-architect Migrate all existing tests to integration tests
- [x] #test-architect Create integration test that loads a JSON file with 2-3 entities
- [x] #test-architect Verify entities spawn with correct behaviors (Transform, Parent, EntityId)
- [x] #test-architect Verify parent-child relationships are established correctly
- [x] #test-architect Test error handling (missing file, invalid JSON, unresolved references)
- [x] #test-architect Test first-write-wins for duplicate entity IDs
- [x] #test-architect Test `LoadGameScene` vs `LoadGlobalScene` (different entity partitions)


### Documentation
- [ ] Add XML doc comments to public APIs
- [ ] Create example JSON file in `Content/scenes/example.json` (for documentation/tests)

---

## Example JSON Structure

```json
{
  "entities": [
    {
      "id": "root-container",
      "transform": {
        "position": [0, 0, 0],
        "rotation": [0, 0, 0, 1],
        "scale": [1, 1, 1]
      }
    },
    {
      "id": "menu-button",
      "transform": {
        "position": [100, 50, 0]
      },
      "parent": "#root-container"
    }
  ]
}
```

**Expected Behavior:**
- `root-container` spawns with full transform data
- `menu-button` spawns with position only (rotation/scale use defaults)
- `menu-button` has Parent behavior pointing to `root-container`
- No errors fired

---

## Notes for Implementation

### Design Decisions
- **JSON over XML/YAML:** `System.Text.Json` is fast, built-in, and widely used
- **Reference type for JSON models:** Acceptable since they're ephemeral (load-time only)
- **Two-pass loading:** Simple, clear, no performance penalty for M1 scene sizes
- **First-write-wins for IDs:** Prevents exceptions, designer can see duplicate IDs in error logs
- **String-based ID references:** Easy to author in JSON, matches game designer expectations

### Integration with Future Sprints
- **M1.5/M2 (Scene transitions):** `SceneLoader` will be called by `SceneManager`
- **M2+ (More behaviors):** Add nullable properties to `JsonEntity`, extend `EntityFactory`
- **M3+ (Gameplay logic):** Behaviors defined in JSON will reference reusable logic scripts

### Risks and Mitigations
- **Risk:** Large JSON files (thousands of entities) may block game thread during loading
  - **Mitigation:** Accept for M1 (synchronous loading), **M1.5 will add background thread loading**
- **Risk:** Manual behavior mapping is tedious as behaviors grow
  - **Mitigation:** Accept for M1 (simple hardcoded mapping), auto-reflection in future sprint
- **Risk:** JSON schema errors are not caught at design time
  - **Mitigation:** Fire error events, designer sees issues in logs (acceptable for M1)
- **Risk:** Forced GC may cause frame drops if called at wrong time
  - **Mitigation:** Only call GC at end of loading (before entering game scene), not during gameplay
