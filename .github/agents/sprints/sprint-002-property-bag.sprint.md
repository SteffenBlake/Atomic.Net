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

#### 1. Property Value Type (Union Type Pattern)
- **File:** `Atomic.Net.MonoGame.BED/PropertyValue.cs`
  - `readonly record struct PropertyValue` with discriminated union pattern
  - Fields: `PropertyType Type { get; }`, `string StringValue`, `float FloatValue`, `bool BoolValue`
  - Enum: `PropertyType { None, String, Float, Bool }`
  - Factory methods: `PropertyValue.FromString(string)`, `PropertyValue.FromFloat(float)`, `PropertyValue.FromBool(bool)`
  - Accessors: `string AsString()`, `float AsFloat()`, `bool AsBool()` (throw if wrong type)
  - Comparison: Implement `IEquatable<PropertyValue>` for value-based equality
  - Pattern: Struct-based union type (no boxing, cache-friendly)

#### 2. Property Bag Behavior
- **File:** `Atomic.Net.MonoGame.BED/PropertyBag.cs`
  - `readonly record struct PropertyBag(int PropertySetIndex)` - behavior for entity property metadata
  - Implements `IBehavior<PropertyBag>`
  - Factory: `static PropertyBag CreateFor(Entity entity)` returns `new PropertyBag(-1)` (invalid/unassigned)
  - Index points into PropertyBagRegistry's dense storage
  - Pattern: Lightweight behavior (4 bytes) that acts as pointer to actual data

#### 3. Property Storage Registry
- **File:** `Atomic.Net.MonoGame.BED/PropertyBagRegistry.cs`
  - Singleton: `ISingleton<PropertyBagRegistry>`
  - Dense storage for property sets: `List<Dictionary<string, PropertyValue>>` (one dict per entity with properties)
  - Free list for reuse: `Stack<int>` for recycled indices
  - Tracks which entities have which property set via `SparseArray<PropertyBag>` (managed by BehaviorRegistry)
  - Methods:
    - `int AllocatePropertySet()` - returns index in dense storage (pops from free list or adds new)
    - `void FreePropertySet(int index)` - clears dict, pushes index to free list
    - `Dictionary<string, PropertyValue> GetPropertySet(int index)` - direct access to property dict
  - Event handlers:
    - `IEventHandler<ResetEvent>` - clear all property sets and free list
    - `IEventHandler<PreEntityDeactivatedEvent>` - free property set if entity has PropertyBag behavior
  - Pattern: Dense storage with free list for zero-alloc reuse

#### 4. Property Query Registry (Key-Based Lookup)
- **File:** `Atomic.Net.MonoGame.BED/PropertyQueryRegistry.cs`
  - Singleton: `ISingleton<PropertyQueryRegistry>`
  - Inverted index: `Dictionary<string, SparseArray<bool>>` (key → entities with that key)
    - Key is **case-insensitive** (use `StringComparer.OrdinalIgnoreCase`)
    - Value is `SparseArray<bool>` (entity index → true if entity has this key)
  - Value-based index: `Dictionary<string, Dictionary<PropertyValue, SparseArray<bool>>>` (key → value → entities)
    - Outer key is case-insensitive
    - Inner dict maps PropertyValue → entities
  - Methods:
    - `void IndexProperty(Entity entity, string key, PropertyValue value)` - add to both indices
    - `void RemoveProperty(Entity entity, string key, PropertyValue value)` - remove from both indices
    - `void RemoveAllProperties(Entity entity)` - full cleanup for entity
    - `IEnumerable<Entity> GetEntitiesWithKey(string key)` - iterate key-only index
    - `IEnumerable<Entity> GetEntitiesWithKeyValue(string key, PropertyValue value)` - iterate value-based index
  - Event handlers:
    - `IEventHandler<BehaviorAddedEvent<PropertyBag>>` - index all properties from newly added PropertyBag
    - `IEventHandler<PreBehaviorRemovedEvent<PropertyBag>>` - remove all indices for entity
    - `IEventHandler<ResetEvent>` - clear all indices
  - Pattern: Inverted index for O(1) key lookup, minimal iteration

#### 5. JSON Integration (Extend JsonEntity)
- **File:** `Atomic.Net.MonoGame.Scenes/JsonModels/JsonEntity.cs` (extend existing)
  - Add property: `Dictionary<string, JsonElement>? Properties { get; set; }`
  - Pattern: Use `JsonElement` to dynamically inspect value types during deserialization

- **File:** `Atomic.Net.MonoGame.Scenes/SceneLoader.cs` (extend existing)
  - **Pass 1 (after Transform, before Parent):**
    - For each `jsonEntity.Properties`:
      - Validate key is not empty/whitespace → fire ErrorEvent if invalid
      - Parse `JsonElement` to `PropertyValue`:
        - `JsonValueKind.String` → `PropertyValue.FromString()`
        - `JsonValueKind.Number` → `PropertyValue.FromFloat()` (parse as float)
        - `JsonValueKind.True/False` → `PropertyValue.FromBool()`
        - `JsonValueKind.Null` → skip (no error)
        - `JsonValueKind.Array/Object/Undefined` → fire ErrorEvent, skip
      - Check for duplicate keys (case-insensitive) → fire ErrorEvent if duplicate
      - Allocate PropertyBag if not already allocated for entity
      - Add to `PropertyBagRegistry.GetPropertySet(bag.PropertySetIndex)[key] = value`
    - Set `BehaviorRegistry<PropertyBag>.SetBehavior()` to attach property bag to entity
    - `PropertyQueryRegistry` automatically indexes properties via `BehaviorAddedEvent<PropertyBag>`
  - **Error handling:**
    - Empty/whitespace key: `ErrorEvent("Property key cannot be empty or whitespace")`
    - Duplicate key: `ErrorEvent("Duplicate property key 'key-name' in entity 'entity-id'")`
    - Invalid type: `ErrorEvent("Unsupported property type 'Object/Array' for key 'key-name'")`

### Integration Points

#### Event Flow
1. SceneLoader deserializes JSON with `"properties": { ... }`
2. For each property entry, SceneLoader:
   - Validates key/value type
   - Allocates PropertyBag behavior if needed (first property for entity)
   - Adds property to `PropertyBagRegistry.GetPropertySet()`
3. SceneLoader calls `BehaviorRegistry<PropertyBag>.SetBehavior()`
4. `BehaviorAddedEvent<PropertyBag>` fires → `PropertyQueryRegistry` indexes all properties
5. During gameplay, systems query via `PropertyQueryRegistry.GetEntitiesWithKey()` or `GetEntitiesWithKeyValue()`
6. On entity deactivation, `PreEntityDeactivatedEvent` → `PropertyBagRegistry` frees property set
7. On ResetEvent, both registries clear all data

#### Registry Interactions
- **BehaviorRegistry<PropertyBag>:** Tracks which entities have property bags (sparse)
- **PropertyBagRegistry:** Stores actual property dictionaries (dense, free list)
- **PropertyQueryRegistry:** Maintains inverted indices for fast queries (key/key-value)
- **SceneLoader:** Populates properties during JSON loading (Pass 1)

#### Query API (for Gameplay Systems)
```csharp
// Key-only query (all entities with "faction")
var entitiesWithFaction = PropertyQueryRegistry.Instance.GetEntitiesWithKey("faction");

// Key-value query (all entities with "faction == 'horde'")
var hordeEntities = PropertyQueryRegistry.Instance.GetEntitiesWithKeyValue(
    "faction", 
    PropertyValue.FromString("horde")
);

// Read property value from entity
if (BehaviorRegistry<PropertyBag>.Instance.TryGetBehavior(entity, out var bag))
{
    var props = PropertyBagRegistry.Instance.GetPropertySet(bag.PropertySetIndex);
    if (props.TryGetValue("max-health", out var value))
    {
        float maxHealth = value.AsFloat(); // throws if wrong type
    }
}
```

### Performance Considerations

#### Allocation Strategy
- **Load time:**
  - `Dictionary<string, PropertyValue>` allocated per entity with properties (via PropertyBagRegistry)
  - `SparseArray<bool>` allocated per unique property key (via PropertyQueryRegistry)
  - All allocations via free list reuse where possible
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
- [ ] Create `PropertyValue` struct with union type pattern (String/Float/Bool)
- [ ] Create `PropertyBag` behavior struct (lightweight index wrapper)
- [ ] Implement `PropertyBagRegistry` singleton with dense storage and free list
- [ ] Implement `PropertyQueryRegistry` singleton with inverted indices (key and key-value)

### JSON Integration
- [ ] Extend `JsonEntity` with `Dictionary<string, JsonElement>? Properties`
- [ ] Extend `SceneLoader` to parse and validate property JSON
- [ ] Implement property validation (empty keys, duplicates, invalid types)
- [ ] Fire ErrorEvent for all validation failures (no crashes)

### Event Handling & Cleanup
- [ ] Handle `BehaviorAddedEvent<PropertyBag>` in PropertyQueryRegistry (index properties)
- [ ] Handle `PreBehaviorRemovedEvent<PropertyBag>` in PropertyQueryRegistry (remove indices)
- [ ] Handle `PreEntityDeactivatedEvent` in PropertyBagRegistry (free property set)
- [ ] Handle `ResetEvent` in both registries (clear all data)

### Edge Case Validation
- [ ] @test-architect Create integration tests for all edge cases listed in requirements
  - Empty/whitespace keys (ErrorEvent)
  - Duplicate keys in same entity (ErrorEvent)
  - Unsupported types (array/object → ErrorEvent, null → skip)
  - Querying `false`, `0`, empty string values (correct results)
  - Case-insensitive key matching
  - Registry cleanup on deactivation/ResetEvent (no stale data)

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
- All three entities have PropertyBag behaviors
- Query `GetEntitiesWithKey("faction")` returns `[goblin-1, goblin-boss]`
- Query `GetEntitiesWithKeyValue("is-boss", true)` returns `[goblin-boss]`
- Query `GetEntitiesWithKeyValue("loot-tier", 3)` returns `[treasure-chest]`
- Keys are case-insensitive: `"Faction"`, `"faction"`, `"FACTION"` all match

---

## Notes for Implementation

### Design Decisions
- **Union type for PropertyValue:** Avoids boxing, type-safe, cache-friendly (16 bytes struct)
- **Dense storage with free list:** Minimizes memory for sparse property bags
- **Inverted index for queries:** O(1) lookup instead of scanning all entities
- **Case-insensitive keys:** Matches designer expectations (JSON keys are often inconsistent)
- **Dictionary per entity:** Flexible, no pre-defined schema, designer-friendly
- **Reject arrays/objects:** Phase 1 simplification (future upgrade path)

### Integration with Future Sprints
- **Phase 2 (Arrays/Objects):** Extend PropertyValue with Array/Object types, nested PropertyBag storage
- **Query System (Future):** Property bag queries integrate with larger query language
- **Command System (Future):** Commands can read/write properties at runtime (if we allow mutation)
- **Reactive Logic (Future):** Triggers can fire when properties match conditions

### Risks and Mitigations
- **Risk:** Dictionary allocations per entity may cause memory pressure with 8000+ entities
  - **Mitigation:** Free list reuse + sparse design (most entities have zero properties)
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
