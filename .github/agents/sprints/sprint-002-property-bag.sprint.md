# Sprint: Property Bag (Entity Metadata System)

## Non-Technical Requirements

### What & Why
- Designers need to attach arbitrary key-value pairs to entities in JSON without C# changes
- Enables experimentation, prototyping, and metadata-driven mechanics (loot types, AI roles, event flags)
- System allows adding, reading, and querying properties at load time
- No runtime allocations (all storage/indexing decided at load)
- Most entities have zero properties (sparse), some (bosses) may have 20-50+

### Supported Data Types (Phase 1)
- string
- float (all numbers are float)
- bool
- Arrays/objects: Out of scope (reject or fire ErrorEvent)

### Required Queries
- **Key-only query:** Get all entities that have a property key (e.g., "faction")
- **Key-value query:** Get all entities with property key AND specific value (e.g., "faction == 'horde'")

### Example JSON
```json
{
  "id": "goblin-1",
  "properties": {
    "enemy-type": "goblin",
    "max-health": 100,
    "is-boss": false
  }
}
```

### Success Criteria
- Designers define properties for any entity in JSON
- Engine loads properties with **zero runtime allocations**
- Both key-only and key-value lookups are lightning-fast (per-frame queries on 8k entities)
- Invalid JSON types (objects/arrays) are rejected and fire ErrorEvent (no crash)
- Duplicate property keys in single entity are rejected and fire ErrorEvent
- Properties with JSON `null` are skipped (no error)
- Property keys are case-insensitive ("enemy-type" == "Enemy-Type")
- ResetEvent and entity deactivation fully clear allocations/references

### Edge Cases to Test (MUST TEST)
1. Registry cleanup on entity deactivation or ResetEvent (no stale indices)
2. Empty string keys or duplicate keys in same entity (ErrorEvent, not loaded)
3. Unsupported types (array/object/null): skip key, fire ErrorEvent (no crash)
4. Querying when value is `false`, `0`, or empty string returns correct result
5. Scene reload/ResetEvent does not pollute properties of new scene/entities
6. Case-insensitive keys (camelCase, PascalCase, snake_case all match)

### Performance Target
- Complete game feature using only property bags (no C# changes)
- 8000 entities at 60+ FPS
- Zero GC allocations at runtime
- All property lookups fast enough for every-frame queries

---

## Technical Requirements

### Architecture

#### 1. Property Value Type (Union Type Pattern) (Complete)
- **File:** `Atomic.Net.MonoGame.BED/Properties/PropertyValue.cs`
  - `readonly struct PropertyValue` with discriminated union pattern
  - Pattern: Struct-based union type (no boxing, cache-friendly)
  - Uses `[JsonConverter(typeof(PropertyValueConverter))]` attribute for automatic deserialization
  - Uses dotVariant library to source gen the union type
  - Implicit cast from operators for its union types
  - "default" returns "empty" type

#### 2. Properties Behavior (Complete)
- **File:** `Atomic.Net.MonoGame.BED/Properties/PropertiesBehavior.cs`
  - `readonly record struct PropertiesBehavior` - behavior containing entity property metadata
  - Contains: `Dictionary<string, PropertyValue> Properties { get; init; }`
  - Implements `IBehavior<PropertiesBehavior>`
  - Uses `[JsonConverter(typeof(PropertiesBehaviorConverter))]` attribute for automatic deserialization
  - Factory: `static PropertiesBehavior CreateFor(Entity entity)` returns `new PropertiesBehavior(new Dictionary<string, PropertyValue>(StringComparer.OrdinalIgnoreCase))`
  - Dictionary uses case-insensitive key comparison
  - Pattern: Behavior directly owns the property dictionary (stored via BehaviorRegistry)

#### 3. Property Value JSON Converter (Complete)
- **File:** `Atomic.Net.MonoGame.BED/Properties/PropertyValueConverter.cs`
  - Custom `System.Text.Json.JsonConverter<PropertyValue>`
  - Deserializes JSON values directly into PropertyValue union type
  - Handles: `JsonValueKind.String` → `FromString()`, `JsonValueKind.Number` → `FromFloat()`, `JsonValueKind.True/False` → `FromBool()`
  - `JsonValueKind.Null` → skip (no error)
  - Pattern: Automatic type conversion via System.Text.Json attribute

#### 4. Properties Behavior JSON Converter (Complete)
- **File:** `Atomic.Net.MonoGame.BED/Properties/PropertiesBehaviorConverter.cs`
  - Custom `System.Text.Json.JsonConverter<PropertiesBehavior>`
  - Deserializes JSON object directly into PropertiesBehavior with Dictionary
  - Validates: empty/whitespace keys → fire ErrorEvent, throw JsonException
  - Validates: duplicate keys (case-insensitive) → fire ErrorEvent, throw JsonException
  - Uses PropertyValueConverter for values
  - Pattern: Automatic conversion with validation

#### 5. Property Bag Index (Query System)
- **File:** `Atomic.Net.MonoGame.BED/PropertyBagIndex.cs`
  - Singleton: `ISingleton<PropertyBagIndex>`
  - Inverted index: `Dictionary<string, SparseArray<bool>>` (key → entities with that key)
    - Key is **case-insensitive** (use `StringComparer.OrdinalIgnoreCase`)
    - Value is `SparseArray<bool>` (entity index → true if entity has this key)
  - Value-based index: `Dictionary<string, Dictionary<PropertyValue, SparseArray<bool>>>` (key → value → entities)
    - Outer key is case-insensitive
    - Inner dict maps PropertyValue → entities
  - Methods:
    - `void IndexProperties(Entity entity, Dictionary<string, PropertyValue> properties)` - add all to both indices
    - `void RemoveProperties(Entity entity, Dictionary<string, PropertyValue> properties)` - remove all from both indices
    - `IEnumerable<Entity> GetEntitiesWithKey(string key)` - iterate key-only index
    - `IEnumerable<Entity> GetEntitiesWithKeyValue(string key, PropertyValue value)` - iterate value-based index
  - Event handlers:
    - `IEventHandler<BehaviorAddedEvent<PropertiesBehavior>>` - index all properties from newly added behavior
    - `IEventHandler<PreBehaviorUpdatedEvent<PropertiesBehavior>>` - remove old indices before update
    - `IEventHandler<PostBehaviorUpdatedEvent<PropertiesBehavior>>` - re-index new properties after update
    - `IEventHandler<PreBehaviorRemovedEvent<PropertiesBehavior>>` - remove all indices for entity
  - Pattern: Inverted index for O(1) key lookup, minimal iteration
  - Note: No ResetEvent handler needed (entity deactivation → behavior removal handles cleanup)

#### 6. JSON Integration (Extend JsonEntity)
- **File:** `Atomic.Net.MonoGame.Scenes/JsonModels/JsonEntity.cs` (extend existing)
  - Add property: `PropertiesBehavior? Properties { get; set; }`
  - Pattern: Behavior directly on JsonEntity (like Transform, Parent, etc.)

- **File:** `Atomic.Net.MonoGame.Scenes/SceneLoader.cs` (extend existing)
  - **Pass 1 (after Transform, before Parent):**
    - If `jsonEntity.Properties != null`, call `BehaviorRegistry<PropertiesBehavior>.SetBehavior(entity, behavior => behavior = jsonEntity.Properties)`
    - `PropertyBagIndex` automatically indexes properties via `BehaviorAddedEvent<PropertiesBehavior>`
  - Pattern: Simplified - JsonConverter handles all parsing and validation

### Integration Points

#### Event Flow
1. SceneLoader deserializes JSON with `"properties": { ... }` using PropertiesBehaviorConverter
2. PropertiesBehaviorConverter validates and creates PropertiesBehavior with Dictionary
3. SceneLoader calls `BehaviorRegistry<PropertiesBehavior>.SetBehavior()`
4. `BehaviorAddedEvent<PropertiesBehavior>` fires → `PropertyBagIndex` indexes all properties
5. During gameplay, systems query via `PropertyBagIndex.GetEntitiesWithKey()` or `GetEntitiesWithKeyValue()`
6. On entity deactivation → `PreEntityDeactivatedEvent` → `PreBehaviorRemovedEvent<PropertiesBehavior>` → `PropertyBagIndex` removes indices (automatic cleanup)

#### Registry Interactions
- **BehaviorRegistry<PropertiesBehavior>:** Stores property dictionaries (sparse, owned by behavior)
- **PropertyBagIndex:** Maintains inverted indices for fast queries (key/key-value)
- **SceneLoader:** Populates properties during JSON loading (Pass 1)
- **Note:** Entity deactivation automatically triggers behavior removal, which triggers index cleanup

#### Query API (for Gameplay Systems)
```csharp
// Key-only query (all entities with "faction")
var entitiesWithFaction = PropertyBagIndex.Instance.GetEntitiesWithKey("faction");

// Key-value query (all entities with "faction == 'horde'")
var hordeEntities = PropertyBagIndex.Instance.GetEntitiesWithKeyValue(
    "faction", 
    PropertyValue.FromString("horde")
);

// Read property value from entity
if (BehaviorRegistry<PropertiesBehavior>.Instance.TryGetBehavior(entity, out var properties))
{
    if (properties.Properties.TryGetValue("max-health", out var value))
    {
        if (!value.TryMatch(out float valueF))
        {
            // throws if wrong type
        }
        ... operational logic on valueF here
    }
}
```

### Performance Considerations

#### Allocation Strategy
- **Load time:**
  - `Dictionary<string, PropertyValue>` allocated per entity with properties (owned by PropertiesBehavior)
  - `SparseArray<bool>` allocated per unique property key (via PropertyBagIndex)
  - Stored in BehaviorRegistry's SparseArray (standard pattern)
- **Runtime:**
  - Zero allocations (all dictionaries/arrays pre-allocated)
  - Queries return `IEnumerable` with zero-alloc struct enumerators
  - No LINQ (use foreach over SparseArray)

#### Query Performance
- **Key-only query:** O(1) dictionary lookup + O(active entities with key) iteration
  - Uses `SparseArray<bool>` → iterate dense list only (skip inactive)
- **Key-value query:** O(1) + O(1) + O(active entities with key+value) iteration
  - Two dictionary lookups, then iterate matched entities
- **Expected:** <0.1ms for 8000 entities with 50 unique keys

#### Potential Hotspots
- **Dictionary allocations:** Each entity with properties allocates a dictionary
  - **Mitigation:** Use free list, pre-allocate capacity (default 16 entries)
- **String comparisons:** Case-insensitive key lookups
  - **Mitigation:** Use `StringComparer.OrdinalIgnoreCase` (faster than manual ToLower)
- **SparseArray overhead:** One SparseArray per unique property key
  - **Mitigation:** Acceptable (sparse data structure, minimal memory)

#### Benchmarking Requests
- [ ] @benchmarker **Test property query performance**: Measure key-only vs key-value queries on 8000 entities with varying property densities (1%, 10%, 50%)
  - Hypothesis: Both should be <0.1ms per query
  - If slower, consider bloom filter or bitset optimization
- [ ] @benchmarker **Test case-insensitive lookup overhead**: Compare `StringComparer.OrdinalIgnoreCase` vs manual lowercasing
  - Hypothesis: Built-in comparer is faster (no allocation)

---

## Tasks

### Core Data Structures
- [x] Create `PropertyValue` struct with union type pattern (String/Float/Bool)
- [x] Create `PropertiesBehavior` struct with Dictionary<string, PropertyValue> property
- [ ] Implement `PropertyBagIndex` singleton with inverted indices (key and key-value)

### JSON Integration
- [x] Create `PropertyValueConverter` for automatic PropertyValue deserialization
- [x] Create `PropertiesBehaviorConverter` for automatic PropertiesBehavior deserialization with validation
- [ ] Extend `JsonEntity` with `PropertiesBehavior? Properties` (with JsonConverter attribute)
- [ ] Extend `SceneLoader` to set PropertiesBehavior via BehaviorRegistry

### Event Handling & Cleanup
- [ ] Handle `BehaviorAddedEvent<PropertiesBehavior>` in PropertyBagIndex (index properties)
- [ ] Handle `PreBehaviorUpdatedEvent<PropertiesBehavior>` in PropertyBagIndex (remove old indices)
- [ ] Handle `PostBehaviorUpdatedEvent<PropertiesBehavior>` in PropertyBagIndex (re-index new properties)
- [ ] Handle `PreBehaviorRemovedEvent<PropertiesBehavior>` in PropertyBagIndex (remove indices)

### Edge Case Validation
- [x] #test-architect Create integration tests for all edge cases listed in requirements
  - Empty/whitespace keys (ErrorEvent) - ✅ Tested in unit tests, will fail in integration until SceneLoader integrated
  - Duplicate keys in same entity (ErrorEvent) - ✅ Tested in unit tests, will fail in integration until SceneLoader integrated
  - Unsupported types (array/object → skip with default, null → skip) - ✅ Tested in unit and integration tests
  - Querying `false`, `0`, empty string values (correct results) - ✅ Tested in unit and integration tests
  - Case-insensitive key matching - ✅ Tested in unit and integration tests (awaiting SceneLoader integration)
  - Registry cleanup on deactivation (no stale data, via behavior removal events) - ✅ Tests created, awaiting PropertyBagIndex implementation
// test-architect: #test-architect All edge case tests have been created. Unit tests (47) all pass.
// Integration tests are created but will fail until SceneLoader is updated to load PropertiesBehavior from JSON.
// Once PropertyBagIndex is implemented, query tests can be activated.

### Performance Validation
- [ ] @benchmarker Benchmark property query performance (key-only and key-value on 8k entities)
- [ ] @benchmarker Benchmark case-insensitive string comparison approaches

### Documentation
- [ ] Add XML doc comments to all public APIs
- [ ] Create example JSON with properties in test files
- [ ] Document query API usage patterns

---

## Example JSON Structure

```json
{
  "entities": [
    {
      "id": "goblin-1",
      "transform": { "position": [10, 20, 0] },
      "properties": {
        "enemy-type": "goblin",
        "faction": "horde",
        "max-health": 100,
        "is-boss": false,
        "loot-tier": 2
      }
    },
    {
      "id": "goblin-boss",
      "transform": { "position": [50, 50, 0] },
      "properties": {
        "enemy-type": "goblin",
        "faction": "horde",
        "max-health": 500,
        "is-boss": true,
        "loot-tier": 5,
        "special-ability": "summon-minions"
      }
    },
    {
      "id": "treasure-chest",
      "transform": { "position": [100, 100, 0] },
      "properties": {
        "loot-tier": 3,
        "locked": true
      }
    }
  ]
}
```

**Expected Behavior:**
- All three entities have PropertyBehaviors
- Query `GetEntitiesWithKey("faction")` returns `[goblin-1, goblin-boss]`
- Query `GetEntitiesWithKeyValue("is-boss", true)` returns `[goblin-boss]`
- Query `GetEntitiesWithKeyValue("loot-tier", 3)` returns `[treasure-chest]`
- Keys are case-insensitive: `"Faction"`, `"faction"`, `"FACTION"` all match

---

## Notes for Implementation

### Design Decisions
- **Union type for PropertyValue:** Avoids boxing, type-safe, cache-friendly (16 bytes struct)
- **PropertiesBehavior owns Dictionary:** Simplifies architecture, uses existing BehaviorRegistry infrastructure
- **Inverted index for queries:** O(1) lookup instead of scanning all entities
- **Case-insensitive keys:** Matches designer expectations (JSON keys are often inconsistent)
- **Dictionary per entity:** Flexible, no pre-defined schema, designer-friendly
- **JsonConverter pattern:** Automatic deserialization with validation, follows existing pattern
- **Reject arrays/objects:** Phase 1 simplification (future upgrade path)

### Integration with Future Sprints
- **Phase 2 (Arrays/Objects):** Extend PropertyValue with Array/Object types
- **Query System (Future):** Property bag queries integrate with larger query language
- **Command System (Future):** Commands can read/write properties at runtime (if we allow mutation)
- **Reactive Logic (Future):** Triggers can fire when properties match conditions

### Risks and Mitigations
- **Risk:** Dictionary allocations per entity may cause memory pressure with 8000+ entities
  - **Mitigation:** Sparse design (most entities have zero properties), stored in BehaviorRegistry SparseArray
- **Risk:** Case-insensitive comparisons may be slow
  - **Mitigation:** Use built-in `StringComparer.OrdinalIgnoreCase` (fast, no allocation)
  - **Fallback:** Benchmark and optimize if needed (normalize keys at load time)
- **Risk:** Inverted index memory grows with unique keys
  - **Mitigation:** Accept for Phase 1 (expected <100 unique keys per game)
- **Risk:** No schema validation (typos in property keys go unnoticed)
  - **Mitigation:** Accept for Phase 1 (error events log issues, designer can spot in tests)

### Out of Scope / Deferred
- Runtime property mutation (all properties are read-only after load)
- Property inheritance (child entities don't inherit parent properties)
- Property schemas/validation (no enforcement of required keys or value ranges)
- Property serialization back to JSON (one-way load only)
