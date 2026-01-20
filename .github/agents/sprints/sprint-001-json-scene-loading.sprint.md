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

#### 1. JSON Data Model (`JsonEntity` and related types)
- **File:** `Atomic.Net.MonoGame.Scenes/JsonModels/JsonEntity.cs`
  - Reference type class (big object, allocation at load time only)
  - Properties: `string? Id`, `JsonTransform? Transform`, `string? Parent`
  - All behavior properties are nullable (optional in JSON)

- **File:** `Atomic.Net.MonoGame.Scenes/JsonModels/JsonTransform.cs`
  - Reference type class for transform data
  - Properties: `float[]? Position`, `float[]? Rotation`, `float[]? Scale`
  - All arrays nullable (defaults: [0,0,0], [0,0,0,1], [1,1,1])

- **File:** `Atomic.Net.MonoGame.Scenes/JsonModels/JsonScene.cs`
  - Root container for scene JSON
  - Property: `List<JsonEntity> Entities`

#### 2. JSON Converters
- **File:** `Atomic.Net.MonoGame.Scenes/JsonConverters/JsonTransformConverter.cs`
  - Custom `System.Text.Json.JsonConverter<JsonTransform>`
  - Handles array parsing for Position/Rotation/Scale
  - Validates array lengths (3 for Position/Scale, 4 for Rotation)
  - Returns null on invalid data (graceful degradation)

#### 3. Scene Loader System
- **File:** `Atomic.Net.MonoGame.Scenes/SceneLoader.cs`
  - Singleton pattern via `ISingleton<SceneLoader>`
  - Method: `LoadScene(string scenePath)` - loads JSON from file path
  - Maintains `Dictionary<string, Entity>` for entity ID tracking (first-write-wins)
  - Two-pass loading:
    - **Pass 1:** Create all entities, apply non-reference behaviors (Transform), track IDs
    - **Pass 2:** Resolve parent references, fire error events for unresolved IDs
  - Clear ID dictionary after loading completes (no persistent state)

- **File:** `Atomic.Net.MonoGame.Scenes/EntityFactory.cs`
  - Static helper methods for creating entities from JSON
  - Method: `CreateEntity(JsonEntity jsonEntity)` - creates Entity, applies Transform behavior
  - Method: `ApplyTransform(Entity entity, JsonTransform transform)` - sets TransformBehavior
  - Uses `BehaviorRegistry<TransformBehavior>.Instance.SetBehavior()`

#### 4. Error Events
- **File:** `Atomic.Net.MonoGame.Scenes/SceneLoadErrorEvent.cs`
  - `readonly record struct SceneLoadErrorEvent(string Message, Exception? Exception)`
  - Fired for: file not found, JSON parse errors, unresolved references
  - Users register handlers via `EventBus<SceneLoadErrorEvent>`

#### 5. Integration with Existing Systems
- Uses `EntityRegistry.Instance.Activate()` to spawn scene entities
- Uses `BehaviorRegistry<TransformBehavior>` to apply transforms
- Uses `BehaviorRegistry<Parent>` to set parent relationships
- Uses `EventBus<SceneLoadErrorEvent>` for error handling

### Integration Points

#### Event Flow
1. User calls `SceneLoader.Instance.LoadScene("Content/scenes/main-menu.json")`
2. SceneLoader reads file, deserializes JSON
3. **Pass 1:** For each `JsonEntity`:
   - Call `EntityRegistry.Instance.Activate()` to spawn entity
   - If `jsonEntity.Id != null`, store in ID dictionary (first-write-wins)
   - If `jsonEntity.Transform != null`, call `EntityFactory.ApplyTransform()`
4. **Pass 2:** For each entity with `jsonEntity.Parent != null`:
   - Lookup parent entity by ID (strip `#` prefix)
   - If found, call `entity.WithParent(parentEntity)` (uses existing extension method)
   - If not found, fire `EventBus<SceneLoadErrorEvent>.Push(new("Unresolved parent reference: #id"))`
5. Clear ID dictionary

#### Registry Interactions
- **EntityRegistry:** Activates scene entities (indices ≥32)
- **BehaviorRegistry<TransformBehavior>:** Stores transform data
- **BehaviorRegistry<Parent>:** Stores parent references
- **HierarchyRegistry:** Automatically updated via Parent behavior events (no direct interaction)

#### Error Handling
- **File not found:** Fire `SceneLoadErrorEvent`, return early
- **Invalid JSON:** Catch `JsonException`, fire `SceneLoadErrorEvent`, return early
- **Unresolved reference:** Fire `SceneLoadErrorEvent`, continue loading other entities
- **Invalid transform data:** Skip behavior, fire `SceneLoadErrorEvent`, continue

### Performance Considerations

#### Allocation Strategy
- All JSON deserialization allocations happen at load time (acceptable)
- ID dictionary allocated per scene load, cleared after (no persistent overhead)
- `JsonEntity` objects are ephemeral (GC'd after loading completes)
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
- Future: If scenes become very large (>1000 entities), may need streaming/chunking

---

## Tasks

### Setup and Infrastructure
- [ ] Create `Atomic.Net.MonoGame.Scenes` project structure for JSON loading
- [ ] Add `System.Text.Json` NuGet dependency (if not already present)
- [ ] Define JSON data model classes (`JsonScene`, `JsonEntity`, `JsonTransform`)
- [ ] Implement custom JSON converters for transform arrays

### Core Scene Loading Logic
- [ ] Implement `SceneLoader` singleton with two-pass loading algorithm
- [ ] Implement `EntityFactory` helper methods for entity creation
- [ ] Define `SceneLoadErrorEvent` for graceful error handling
- [ ] Implement entity ID tracking with first-write-wins semantics
- [ ] Implement parent reference resolution with error events for unresolved IDs

### Integration Tests
- [ ] @test-architect Create integration test that loads a JSON file with 2-3 entities
- [ ] @test-architect Verify entities spawn with correct behaviors (Transform, Parent)
- [ ] @test-architect Verify parent-child relationships are established correctly
- [ ] @test-architect Test error handling (missing file, invalid JSON, unresolved references)
- [ ] @test-architect Test first-write-wins for duplicate entity IDs

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
- **Risk:** Large JSON files may be slow to parse
  - **Mitigation:** Accept for M1 (not hot path), revisit if >1000 entities
- **Risk:** Manual behavior mapping is tedious as behaviors grow
  - **Mitigation:** Accept for M1 (simple hardcoded mapping), auto-reflection in future sprint
- **Risk:** JSON schema errors are not caught at design time
  - **Mitigation:** Fire error events, designer sees issues in logs (acceptable for M1)
