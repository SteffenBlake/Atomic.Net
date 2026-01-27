# Sprint: Tag System (Entity Tagging & Registry)

## Non-Technical Requirements

### Purpose
- Allow tagging of entities via JSON data
- Enable group selection of entities in rules, tests, and queries (e.g., all entities tagged `"enemy"` or `"boss"`)

### Constraints
- Zero gameplay allocations (all allocations at load time)
- Sparse/cache-friendly datastructures
- All tag data defined via JSON
- Tags are non-unique flat string labels (case-insensitive)
- Entities can have zero or many tags
- Many entities can share the same tag (e.g., 1,000 entities may share "enemy")

### Success Criteria
- Tags defined in entity JSON are loaded to entity behaviors at scene load
- The `#tag` selector in rules matches all entities with that tag
- SelectorRegistry/TagRegistry enables efficient lookup of all entities with a given tag
- TagRegistry supports add, remove, update scenarios (entity deactivation, scene transitions)
- Case-insensitive matching for tags (normalize to lowercase on registration)
- Error handling for invalid tags (null/empty/whitespace/duplicates) via ErrorEvent
- Zero gameplay allocations (enforced by tests)
- ResetEvent and entity deactivation fully clear allocations/references

### Tag Validation Rules
1. **Null/Empty Tags**: Fire ErrorEvent, skip tag, continue processing remaining tags
2. **Whitespace-Only Tags**: Fire ErrorEvent, skip tag, continue processing
3. **Duplicate Tags in Same Entity**: Fire ErrorEvent, keep first occurrence, skip duplicates
4. **Invalid JSON Type**: Fire ErrorEvent, skip invalid entry
5. **Case Normalization**: Convert all tags to lowercase before registration ("Enemy" → "enemy")
6. **Tag Limit**: No hard limit per entity (test with 1-50 tags per entity for performance validation)
7. **Valid Characters**: Tags only allow `a-z`, `A-Z`, `0-9`, `-`, and `_`

### Behavior Lifecycle
- **BehaviorAddedEvent**: Register ALL tags in TagRegistry, mark selectors dirty, validate tags
- **PreBehaviorUpdatedEvent**: Unregister ALL old tags, mark selectors dirty
- **PostBehaviorUpdatedEvent**: Register ALL new tags, mark selectors dirty, validate tags
- **PreBehaviorRemovedEvent**: Unregister ALL tags, mark selectors dirty
- **PreEntityDeactivatedEvent**: Handled automatically via behavior removal cascade
- **ResetEvent**: Scene partition tags cleared, global partition tags persist
- **ShutdownEvent**: ALL tags cleared, unregister from event buses

### TagEntitySelector Integration
- `#tag` selector matches ALL entities that have that tag
- Selector supports refinement chains (e.g., `!enter:#enemy`)
- Selector supports union (e.g., `#enemy,#boss`)
- Selector recalculates when TagsBehavior is added/updated/removed or prior selector changes

### Performance Targets
- Load 1,000 entities with 10 tags each: < 10ms, zero gameplay allocations
- Resolve tag with 1,000 entities: < 1ms
- Update tags on 1,000 entities: < 5ms, zero gameplay allocations
- Selector recalc for 1,000 entities: < 2ms

---

## Technical Requirements

### Architecture

#### 1. TagsBehavior (Multi-Value Collection Behavior)
- **File:** `MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs`
- **Type:** `readonly record struct TagsBehavior(ImmutableHashSet<string> Tags) : IBehavior<TagsBehavior>`
- **Purpose:** Store immutable set of tags for an entity
- **Default:** Empty ImmutableHashSet (via `CreateFor()`)
- **Rationale:**
  - ImmutableHashSet enables zero-allocation set difference during updates (old vs new)
  - Fast duplicate detection during validation
  - Cache-friendly iteration
  - Structural equality for comparison

#### 2. TagsBehaviorConverter (JSON Deserialization)
- **File:** `MonoGame/Atomic.Net.MonoGame/Tags/TagsBehaviorConverter.cs`
- **Type:** `JsonConverter<TagsBehavior>`
- **Purpose:** Deserialize JSON tag array into TagsBehavior
- **Validation:**
  - Fire ErrorEvent for null/empty/whitespace tags (skip tag)
  - Fire ErrorEvent for duplicate tags in same entity (keep first)
  - Fire ErrorEvent for invalid characters (skip tag)
  - Normalize all tags to lowercase before adding to set
- **Example JSON:**
  ```json
  {
    "tags": ["enemy", "goblin", "hostile"]
  }
  ```

#### 3. TagRegistry (One-to-Many Entity-Tag Mapping)
- **File:** `MonoGame/Atomic.Net.MonoGame/Tags/TagRegistry.cs`
- **Type:** Singleton via `ISingleton<TagRegistry>`
- **Data Structures:**
  - `Dictionary<string, SparseArray<bool>>` - tag name → entity set (one tag → many entities)
  - `SparseReferenceArray<ImmutableHashSet<string>>` - entity → tag set (reverse lookup)
  - `Dictionary<string, TagEntitySelector>` - tag name → selector (for dirty flag propagation)
- **Methods:**
  - `TryRegister(Entity entity, string tag)` - add entity to tag set
  - `TryResolve(string tag, out SparseArray<bool> entities)` - get all entities with tag
  - `RegisterSelector(string tag, TagEntitySelector selector)` - track selector for dirty marking
- **Event Handlers:**
  - `BehaviorAddedEvent<TagsBehavior>` - register all tags
  - `PreBehaviorUpdatedEvent<TagsBehavior>` - unregister old tags
  - `PostBehaviorUpdatedEvent<TagsBehavior>` - register new tags
  - `PreBehaviorRemovedEvent<TagsBehavior>` - unregister all tags
  - `ResetEvent` - clear scene partition tags only
  - `ShutdownEvent` - clear all tags and unregister from events
- **Dirty Propagation:**
  - When tag registration changes, mark ALL TagEntitySelectors matching that tag as dirty
  - Dirty selectors recalculate on `SelectorRegistry.Recalc()`
- **Rationale:**
  - SparseArray<bool> over HashSet<Entity> for zero allocations during add/remove (flip bool flag)
  - Cache-friendly iteration (contiguous memory)
  - Fast intersection with other selectors (bitwise AND with TensorPrimitives)

#### 4. TagEntitySelector (Already Exists - Needs Implementation)
- **File:** `MonoGame/Atomic.Net.MonoGame/Selectors/TagEntitySelector.cs` (ALREADY EXISTS)
- **Status:** Currently a stub (Stage 1 parsing only)
- **Needs:** Implement actual tag matching in `Recalc()`

#### 5. SelectorRegistry Integration (Already Exists - Needs Update)
- **File:** `MonoGame/Atomic.Net.MonoGame/Selectors/SelectorRegistry.cs` (ALREADY EXISTS)
- **Current State:** Has `_tagSelectorLookup` but doesn't propagate dirty flags
- **Needs:** Subscribe to TagsBehavior events and mark selectors dirty
- **New Event Handlers:**
  - `BehaviorAddedEvent<TagsBehavior>`
  - `PreBehaviorUpdatedEvent<TagsBehavior>`
  - `PostBehaviorUpdatedEvent<TagsBehavior>`
  - `PreBehaviorRemovedEvent<TagsBehavior>`
- **Dirty Propagation Logic:**
  - On tag mutation, iterate all affected tags
  - For each tag, look up TagEntitySelector in `_tagSelectorLookup`
  - Call `selector.MarkDirty()` on each

#### 6. Scene Loading Integration
- **File:** `MonoGame/Atomic.Net.MonoGame/Scenes/SceneLoader.cs` (ALREADY EXISTS)
- **Needs:** Add TagsBehavior deserialization support
- **JSON Model Update:**
  - Add `TagsBehavior? Tags` property to `JsonEntity`
- **Loading Logic:**
  - Deserialize `tags` array via `TagsBehaviorConverter`
  - Apply behavior via `entity.SetBehavior<TagsBehavior>(jsonEntity.Tags.Value)`
  - TagRegistry automatically registers tags via `BehaviorAddedEvent`

### Integration Points

#### Event Flow (Tag Registration)
1. SceneLoader deserializes `"tags": ["enemy", "goblin"]` → TagsBehavior
2. SceneLoader calls `entity.SetBehavior<TagsBehavior>(behavior)`
3. BehaviorRegistry fires `BehaviorAddedEvent<TagsBehavior>`
4. TagRegistry handles event → register tags, mark selectors dirty
5. SelectorRegistry.Recalc() called (typically end-of-frame or after scene load)
6. TagEntitySelector.Recalc() updates Matches array

#### Event Flow (Tag Update)
1. User calls `entity.SetBehavior<TagsBehavior>(newBehavior)`
2. BehaviorRegistry fires `PreBehaviorUpdatedEvent<TagsBehavior>` (old tags)
3. TagRegistry handles pre-event → unregister old tags, mark selectors dirty
4. BehaviorRegistry fires `PostBehaviorUpdatedEvent<TagsBehavior>` (new tags)
5. TagRegistry handles post-event → register new tags, mark selectors dirty
6. SelectorRegistry.Recalc() updates all dirty selectors

#### Registry Interactions
- **BehaviorRegistry<TagsBehavior>:** Stores tag data, fires lifecycle events
- **TagRegistry:** Tracks tag-to-entity mappings, propagates dirty flags
- **SelectorRegistry:** Parses `#tag` selectors, caches TagEntitySelector instances
- **TagEntitySelector:** Resolves entities matching tag, intersects with prior selectors

#### Error Handling
- **Null tag:** Fire ErrorEvent, skip tag, continue with remaining tags
- **Empty/whitespace tag:** Fire ErrorEvent, skip tag
- **Duplicate tag:** Fire ErrorEvent, keep first occurrence, skip duplicate
- **Invalid characters:** Fire ErrorEvent, skip tag
- **All errors:** Non-blocking, scene loading continues

### Performance Considerations

#### Allocation Strategy
- **TagsBehavior:** ImmutableHashSet allocated at load time, but its impossible to not have zero allocs for any type of "list"/"array" object, but it only allocs for the behavior which is low cost (SteffenBlake: Approved)
- Add a note that the allocation of the ImmutableHashSet for the behavior has been approved by SteffenBlake
- **TagRegistry dictionaries:** Pre-sized to `Constants.MaxEntities / 64`
- **SparseArray<bool>:** Allocated at load time, reused for all entities with tag
- **Tag update:** Set difference via ImmutableHashSet (zero allocations)

#### Cache-Friendly Data Layout
- **SparseArray<bool> iteration:** Contiguous memory, SIMD-friendly for intersection
- **TagEntitySelector.Recalc():** TensorSparse for fast intersection, see UnionSelector for an example

#### Potential Hotspots
- **Tag validation during deserialization:** O(n) per entity, acceptable at load time
- **Selector recalc:** O(entities_with_tag), bounded by Constants.MaxEntities
- **Tag update:** O(old_tags + new_tags), typically < 10 tags per entity

---

## Tasks

### Setup and Infrastructure
- [ ] Create `MonoGame/Atomic.Net.MonoGame/Tags/` directory
- [ ] Define `TagsBehavior` readonly record struct with ImmutableHashSet<string>
- [ ] Implement `TagsBehaviorConverter` for JSON deserialization with validation
- [ ] Update `JsonEntity` model to include `TagsBehavior? Tags` property

### TagRegistry Implementation
- [ ] Implement `TagRegistry` singleton with tag-to-entity and entity-to-tag mappings
- [ ] Implement `TryRegister(Entity, string)` and `TryResolve(string, out SparseArray<bool>)`
- [ ] Subscribe to `BehaviorAddedEvent<TagsBehavior>` and register tags
- [ ] Subscribe to `PreBehaviorUpdatedEvent<TagsBehavior>` and unregister old tags
- [ ] Subscribe to `PostBehaviorUpdatedEvent<TagsBehavior>` and register new tags
- [ ] Subscribe to `PreBehaviorRemovedEvent<TagsBehavior>` and unregister tags
- [ ] Implement `ShutdownEvent` handler (unregister from events)

### TagEntitySelector Implementation
- [ ] Update `TagEntitySelector.Recalc()` to resolve entities from TagRegistry
- [ ] Implement intersection with prior selector Matches
- [ ] Register selector with TagRegistry on creation (via SelectorRegistry)

### SelectorRegistry Integration
- [ ] Subscribe to `BehaviorAddedEvent<TagsBehavior>` in SelectorRegistry
- [ ] Subscribe to `PreBehaviorUpdatedEvent<TagsBehavior>` in SelectorRegistry
- [ ] Subscribe to `PostBehaviorUpdatedEvent<TagsBehavior>` in SelectorRegistry
- [ ] Subscribe to `PreBehaviorRemovedEvent<TagsBehavior>` in SelectorRegistry
- [ ] Implement dirty propagation logic (iterate tags, mark selectors)
- [ ] Update `TryGetOrCreateTagSelector` to register with TagRegistry

### Scene Loading Integration
- [ ] Update `SceneLoader` to deserialize `tags` field from JSON
- [ ] Apply TagsBehavior to entities via SetBehavior

### Testing Infrastructure
- [ ] Create test fixtures directory `MonoGame/Atomic.Net.MonoGame.Tests/Tags/Fixtures/`
- [ ] Create `tags-basic.json` fixture (3 entities with tags)
- [ ] Create `tags-edge-cases.json` fixture (empty tags, no tags, case variations)
- [ ] Create `tags-invalid.json` fixture (null, empty, whitespace, duplicates)
- [ ] Create `tags-many.json` fixture (entity with 50 tags)

### Unit Tests - TagRegistry
- [ ] @senior-dev Create `TagRegistryUnitTests.cs` with positive tests (single tag, multiple tags, multiple entities)
- [ ] @senior-dev Add negative tests (null, empty, whitespace, duplicates)
- [ ] @senior-dev Add lifecycle tests (add, update, remove, deactivate)
- [ ] @senior-dev Add partition tests (scene vs global tags, ResetEvent behavior)

### Integration Tests - TagEntitySelector
- [ ] @senior-dev Create `TagEntitySelectorIntegrationTests.cs` with selector matching tests
- [ ] @senior-dev Add refinement tests (`#enemy:#boss`, `@player:#enemy`)
- [ ] @senior-dev Add union tests (`#enemy,#boss`)
- [ ] @senior-dev Add dirty tracking tests (add/remove tags, selector recalc)
- [ ] @senior-dev Add error handling tests (empty selector, invalid characters)

### Integration Tests - Scene Loading
- [ ] @senior-dev Create `SceneLoaderTagIntegrationTests.cs` with scene loading tests
- [ ] @senior-dev Test tag deserialization from JSON
- [ ] @senior-dev Test error handling (null tags, invalid types, duplicates)
- [ ] @senior-dev Test partition behavior (scene vs global tags, ResetEvent)

### Integration Tests - Error Handling
- [ ] @senior-dev Create `TagErrorHandlingTests.cs` with error event validation
- [ ] @senior-dev Verify ErrorEvent fired for all invalid cases
- [ ] @senior-dev Verify graceful degradation (valid tags registered, invalid skipped)
- [ ] @senior-dev Verify error recovery (scene loads despite errors)

### Documentation
- [ ] Add XML doc comments to TagsBehavior, TagRegistry, public APIs
- [ ] Update ROADMAP.md to mark Tag System milestone complete

---

## Notes for Implementation

### Design Decisions

**ImmutableHashSet for TagsBehavior:**

Code hint on how you can have a ref type not be null in a struct:
```
readonly record struct SomeStruct
{
    private readonly SomeRefType? _foo;
    public SomeRefType Foo
    {
        init => _foo = value;
        get => _foo ?? SomeRefType.Empty;
    }
}

var x = new SomeStruct();
x = x with { Foo = x.Foo.ButChanged(...) }
```
This should work with ImmutableHashset inside of SetBehavior()!

YOU SHOULD NOT NEED TO CREATE AN INSTANCE OF THE STRUCT MANUALLY, THE BEHAVIORREGISTRY SHOULD SET IT TO `default` AND THAT SHOULD WORK IF YOU DO IT RIGHT

**Case-Insensitive Tags:**
- Normalize to lowercase during deserialization (TagsBehaviorConverter)
- Prevents case-sensitivity bugs in selectors (`#Enemy` vs `#enemy`)
- Matches user expectation (tags are labels, not code identifiers)

**First-Occurrence-Wins for Duplicates:**
- Prevents exceptions, allows scene loading to continue
- Fire ErrorEvent so designer knows about issue
- Matches EntityIdRegistry pattern (first-write-wins)

### Integration with Other Sprints

**Query System (Sprint 005):**
- `#tag` selectors already integrated into query parsing
- TagEntitySelector provides entity set for refinement chains
- Example: `#enemy WHERE distance < 100` - filter enemies by distance

**Command System (Future):**
- Commands can target tag selectors: `#enemy TAKE_DAMAGE 10`
- TagEntitySelector.Matches provides entity set to iterate

**State Machine System (Future):**
- Tags can represent entity states: `["idle", "walking", "attacking"]`
- Tag updates during state transitions automatically mark selectors dirty

### Risks and Mitigations

**Risk:** ImmutableHashSet may allocate more than expected during updates

**Risk:** Tag validation during deserialization may be slow
- **Mitigation:** Validation is O(n) per entity, acceptable at load time (not hot path)

**Risk:** Case normalization may surprise users who expect case-sensitive tags
- **Mitigation:** Document in error messages and XML comments

### Reference Implementation: EntityIdRegistry

TagRegistry follows the same pattern as EntityIdRegistry but with one-to-many mapping:

**EntityIdRegistry (one-to-one):**
- `Dictionary<string, Entity>` - id → entity
- `SparseReferenceArray<string>` - entity → id

**TagRegistry (one-to-many):**
- `Dictionary<string, SparseArray<bool>>` - tag → entities (bool set)
- `SparseReferenceArray<ImmutableHashSet<string>>` - entity → tags (immutable set)

Both registries:
- Subscribe to BehaviorAdded/Updated/Removed events
- First-write-wins (or skip duplicates)
- Fire ErrorEvent for invalid data
