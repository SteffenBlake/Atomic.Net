# Sprint: RulesDriver Behavior Mutations and Negative Tests

## Non-Technical Requirements

### Problem
- The current rules engine (RulesDriver) only supports mutating and asserting against a subset of entity behaviors (mainly PropertiesBehavior)
- This limits what game logic can be authored in data and reduces regression coverage for the entity system as new types of behaviors are added or refactored

### Goal
- Rules and mutations in the data-driven engine must be able to mutate and assert against any writable behavior (except PersistToDiskBehavior, which is internal/persistence only; WorldTransformBehavior, which is read-only; and FlexBehavior, which is computed)
- Every user, gameplay designer, and automatable pipeline must have full test coverage and playground confidence for behavioral mutations in JSON (not in code)

### Requirements
- For each writable behavior, define specimen mutation and negative regression tests:
  - MUST provide a positive (mutation succeeds) test and a negative (mutation fails as expected) test per behavior
  - Behaviors tested:
    1. IdBehavior
    2. TagsBehavior
    3. TransformBehavior (fields: Position, Rotation, Scale, Anchor)
    4. ParentBehavior
    5. AlignItemsBehavior
    6. AlignSelfBehavior
    7. BorderBottomBehavior
    8. BorderLeftBehavior
    9. BorderRightBehavior
    10. BorderTopBehavior
    11. FlexDirectionBehavior
    12. FlexGrowBehavior
    13. FlexWrapBehavior
    14. FlexZOverride
    15. HeightBehavior
    16. JustifyContentBehavior
    17. MarginBottomBehavior
    18. MarginLeftBehavior
    19. MarginRightBehavior
    20. MarginTopBehavior
    21. PaddingBottomBehavior
    22. PaddingLeftBehavior
    23. PaddingRightBehavior
    24. PaddingTopBehavior
    25. PositionBottomBehavior
    26. PositionLeftBehavior
    27. PositionRightBehavior
    28. PositionTopBehavior
    29. PositionTypeBehavior
    30. WidthBehavior
- WorldTransformBehavior and FlexBehavior must have negative tests to show attempts at mutation fail and produce errors
- PersistToDiskBehavior is excluded (not accessible via rules)
- Each test must load a representative scene fixture, apply a RulesDriver mutation, and assert correct mutation or correct error
- Negative tests must demonstrate both error-event firing and non-mutation
- Tests must be granular (one test method per behavior per direction)
  - Example: `RunFrame_WithTagsBehaviorMutation_AppliesCorrectly`, `RunFrame_WithTagsBehaviorMutation_Invalid_FiresError`
- Tests must use data-driven JSON fixtures for both setup and rule definition
- Tests must be designed to avoid allocations during gameplay

### Examples
- Positive: Start with entity with Tag ['enemy'], rule adds 'boss', assert tag is present
- Negative: Try to assign invalid enum value to AlignSelf, assert error-event and non-mutation

### Success Criteria
- All writable behaviors are covered with a test for a valid mutation and a test for an invalid mutation
- Special-case behaviors (Flex, WorldTransform) have negative test coverage
- Tests are discoverable and maintainable (one method per behavior/direction)

---

## Technical Requirements

### Architecture

#### 1. Extend RulesDriver Entity Serialization

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/RulesDriver.cs` (EXISTING - EXTEND)

**Current State:**
- RulesDriver.BuildEntitiesArray() currently serializes: `_index`, `id`, `tags`, `properties`
- RulesDriver.ApplyToTarget() currently handles only: `{ "properties": "key" }`

**New Requirements:**
- Extend BuildEntitiesArray() to serialize all writable behaviors into entity JsonObject
- Add serialization for:
  - `transform` → `{ "position": {...}, "rotation": {...}, "scale": {...}, "anchor": {...} }` (sub-object for TransformBehavior fields)
  - `parent` → `string` (entity selector as string for ParentBehavior)
  - Individual flex behaviors at root level (each XxxBehavior maps to its own key)
- Maintain zero-allocation pattern (reuse pre-allocated JsonObjects/arrays)

**Serialization Format (Extended):**
```json
{
  "_index": 42,
  "id": "entity1",
  "tags": ["enemy"],
  "properties": { "health": 100 },
  "transform": {
    "position": { "x": 10, "y": 20, "z": 0 },
    "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 },
    "scale": { "x": 1, "y": 1, "z": 1 },
    "anchor": { "x": 0, "y": 0, "z": 0 }
  },
  "parent": "#parentEntity",
  "alignItems": "center",
  "alignSelf": "flexStart",
  "borderBottom": 5.0,
  "borderLeft": 5.0,
  "borderRight": 5.0,
  "borderTop": 5.0,
  "flexDirection": "row",
  "flexGrow": 1.0,
  "flexWrap": "wrap",
  "flexZOverride": 10,
  "height": 100.0,
  "heightPercent": false,
  "justifyContent": "spaceBetween",
  "marginBottom": 10.0,
  "marginLeft": 10.0,
  "marginRight": 10.0,
  "marginTop": 10.0,
  "paddingBottom": 5.0,
  "paddingLeft": 5.0,
  "paddingRight": 5.0,
  "paddingTop": 5.0,
  "positionBottom": 0.0,
  "positionBottomPercent": false,
  "positionLeft": 0.0,
  "positionLeftPercent": false,
  "positionRight": 0.0,
  "positionRightPercent": false,
  "positionTop": 0.0,
  "positionTopPercent": false,
  "positionType": "relative",
  "width": 200.0,
  "widthPercent": false
}
```

**Key Design Principles:**
- **Behavior-to-JSON mapping:** Each behavior maps to its own JSON key (e.g., `BorderLeftBehavior` → `"borderLeft"`)
- **Composite behaviors use sub-objects:** TransformBehavior has multiple fields, so it uses a nested object
- **FlexBehavior is read-only:** The computed `FlexBehavior` struct is NOT serialized (only individual flex behaviors)
- **Lazy serialization:** Only serialize behaviors that exist on the entity (check TryGetBehavior first)
- **Reuse JsonObjects:** Pre-allocate transform sub-objects, update values in-place
- **Cache-friendly iteration:** Leverage SparseArray iteration pattern

#### 2. Extend RulesDriver Mutation Application

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/RulesDriver.cs` (EXISTING - EXTEND)

**Current State:**
- ApplyToTarget() handles: `{ "properties": "key" }`
- ApplyPropertyMutation() sets PropertiesBehavior values

**New Requirements:**
- Extend ApplyToTarget() to support additional target paths:
  - `{ "id": "newId" }` → Mutate IdBehavior
  - `{ "tags": { "add": "tag" } }` or `{ "tags": { "remove": "tag" } }` → Mutate TagsBehavior
  - `{ "transform": { "position": { "x": ..., "y": ..., "z": ... } } }` → Mutate TransformBehavior.Position
  - `{ "transform": { "rotation": {...} } }` → Mutate TransformBehavior.Rotation
  - `{ "transform": { "scale": {...} } }` → Mutate TransformBehavior.Scale
  - `{ "transform": { "anchor": {...} } }` → Mutate TransformBehavior.Anchor
  - `{ "parent": "#selector" }` → Mutate ParentBehavior
  - `{ "alignItems": "center" }` → Mutate AlignItemsBehavior
  - `{ "alignSelf": "flexStart" }` → Mutate AlignSelfBehavior
  - `{ "borderBottom": 5.0 }` → Mutate BorderBottomBehavior
  - (Similar patterns for all 30 individual flex behaviors listed in requirements)

**Mutation Methods (NEW):**
- `ApplyIdMutation(ushort entityIndex, JsonNode value)` → Parse string, set IdBehavior
- `ApplyTagsMutation(ushort entityIndex, JsonNode operation, JsonNode value)` → Parse add/remove operation, update TagsBehavior
- `ApplyTransformMutation(ushort entityIndex, JsonNode field, JsonNode value)` → Parse vector/quaternion, update TransformBehavior field
- `ApplyParentMutation(ushort entityIndex, JsonNode value)` → Parse selector string, set ParentBehavior
- Individual flex behavior mutation methods (e.g., `ApplyAlignItemsMutation()`, `ApplyBorderLeftMutation()`, etc.)

**Design Pattern (with else-if for performance):**
```csharp
private static bool ApplyToTarget(ushort entityIndex, JsonNode target, JsonNode value)
{
    if (target is not JsonObject targetObj)
    {
        EventBus<ErrorEvent>.Push(new ErrorEvent(
            $"Target must be a JsonObject for entity {entityIndex}"
        ));
        return false;
    }

    // Check each supported target type using else-if for short-circuit optimization
    if (targetObj.TryGetPropertyValue("properties", out var propertyKey))
    {
        return ApplyPropertyMutation(entityIndex, propertyKey, value);
    }
    else if (targetObj.TryGetPropertyValue("id", out _))
    {
        return ApplyIdMutation(entityIndex, value);
    }
    else if (targetObj.TryGetPropertyValue("tags", out var tagOp))
    {
        return ApplyTagsMutation(entityIndex, tagOp, value);
    }
    else if (targetObj.TryGetPropertyValue("transform", out var transformField))
    {
        return ApplyTransformMutation(entityIndex, transformField, value);
    }
    else if (targetObj.TryGetPropertyValue("parent", out _))
    {
        return ApplyParentMutation(entityIndex, value);
    }
    else if (targetObj.TryGetPropertyValue("alignItems", out _))
    {
        return ApplyAlignItemsMutation(entityIndex, value);
    }
    else if (targetObj.TryGetPropertyValue("alignSelf", out _))
    {
        return ApplyAlignSelfMutation(entityIndex, value);
    }
    else if (targetObj.TryGetPropertyValue("borderBottom", out _))
    {
        return ApplyBorderBottomMutation(entityIndex, value);
    }
    // ... (continue for all 30 individual flex behaviors)
    else
    {
        // Generic error for unrecognized targets
        var validTargets = string.Join(", ", new[]
        {
            "properties", "id", "tags", "transform", "parent",
            "alignItems", "alignSelf", "borderBottom", "borderLeft", "borderRight", "borderTop",
            "flexDirection", "flexGrow", "flexWrap", "flexZOverride",
            "height", "justifyContent",
            "marginBottom", "marginLeft", "marginRight", "marginTop",
            "paddingBottom", "paddingLeft", "paddingRight", "paddingTop",
            "positionBottom", "positionLeft", "positionRight", "positionTop", "positionType",
            "width"
        });
        EventBus<ErrorEvent>.Push(new ErrorEvent(
            $"Unrecognized target for entity {entityIndex}. Expected one of: {validTargets}"
        ));
        return false;
    }
}
```

**Error Handling:**
- All mutation methods return `bool` (success/failure)
- Fire `ErrorEvent` for:
  - Invalid JSON format (e.g., string instead of vector for position)
  - Invalid enum values (e.g., "invalidAlign" for AlignSelf)
  - Missing required fields (e.g., x/y/z for position)
  - Inactive entity (already handled)
  - Out-of-range values (e.g., negative flex grow)
  - Unrecognized target keys
- Mutation must be atomic: if parsing fails, do NOT partially update behavior

#### 3. Registry Integration via SetBehavior

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/RulesDriver.cs` (EXISTING - EXTEND)

**Key Insight:**
- Calling `entity.SetBehavior<TBehavior>()` automatically triggers registry updates via existing event listeners
- No need to manually call registry methods (e.g., `TagRegistry.Instance.AddTag()`)
- Registries subscribe to `EntityBehaviorChangedEvent` and update their internal state automatically

**Example:**
```csharp
// Setting IdBehavior automatically updates EntityIdRegistry
var entity = EntityRegistry.Instance[entityIndex];
entity.SetBehavior(new IdBehavior("newId"));
// EntityIdRegistry event listener picks up the change automatically

// Setting TagsBehavior automatically updates TagRegistry
entity.SetBehavior(new TagsBehavior { Tags = newTagSet });
// TagRegistry event listener updates tag-to-entity mappings automatically
```

**Exception:**
- `IdBehavior` changes may require explicit `EntityIdRegistry.Unregister()` / `Register()` calls if the ID changes (not just set for first time)
- Consult existing `IdBehavior` mutation code for correct pattern

#### 4. Extend RulesDriver to Handle Read-Only Behaviors

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/RulesDriver.cs` (EXISTING - EXTEND)

**New Requirements:**
- Detect attempts to mutate read-only/computed behaviors
- Fire `ErrorEvent` with descriptive message
- Do NOT apply any mutation

**Read-Only Behaviors:**
- `WorldTransformBehavior` (computed from Transform + hierarchy)
- `FlexBehavior` (computed from individual flex behaviors)

**Note:** These are handled by the generic "Unrecognized target" error in ApplyToTarget, since they won't be in the valid targets list.

### Integration Points

#### With Existing Registries
- **IdBehavior:** May require explicit `EntityIdRegistry.Instance.Unregister(oldId)` and `.Register(newId, entity)` if ID is being changed (not just set initially)
- **TagsBehavior:** Updates tracked automatically via `TagRegistry` event listeners when `SetBehavior` is called
- **TransformBehavior:** Use `BehaviorRegistry<TransformBehavior>` for mutations; updates tracked automatically
- **ParentBehavior:** Use `BehaviorRegistry<ParentBehavior>` for mutations; consider hierarchy invalidation side effects
- **Individual Flex Behaviors:** Use individual `BehaviorRegistry<TBehavior>` for each flex behavior type; FlexRegistry will recompute FlexBehavior automatically

#### Event System
- Fire `ErrorEvent` for all mutation failures (invalid format, read-only access, unrecognized targets, etc.)
- Existing `FakeEventListener<ErrorEvent>` in tests will capture these for assertions

### Test Structure

#### Test File Organization

**File:** `MonoGame/Atomic.Net.MonoGame.Tests/Scenes/Integrations/RulesDriverBehaviorMutationTests.cs` (NEW)

**Structure:**
- One test class for all behavior mutation tests
- Tests grouped by behavior category (Id, Tags, Transform, Parent, Individual Flex Behaviors)
- Each behavior has exactly 2 tests: positive mutation + negative mutation
- Total: ~32 behaviors × 2 tests = ~64 test methods

**Test Naming Convention:**
- Positive: `RunFrame_With{Behavior}Mutation_AppliesCorrectly`
- Negative: `RunFrame_With{Behavior}Mutation_Invalid_FiresError`

**Example Test Structure:**
```csharp
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class RulesDriverBehaviorMutationTests : IDisposable
{
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public RulesDriverBehaviorMutationTests()
    {
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        _errorListener = new FakeEventListener<ErrorEvent>();
    }

    public void Dispose()
    {
        _errorListener.Dispose();
        EventBus<ShutdownEvent>.Push(new());
    }

    // ========== IdBehavior Tests ==========
    
    [Fact]
    public void RunFrame_WithIdBehaviorMutation_AppliesCorrectly()
    {
        // Arrange: Load fixture with entity "entity1"
        SceneLoader.Instance.LoadGameScene("RulesDriver/Fixtures/id-mutation.json");
        
        // Act: Rule changes id from "entity1" to "renamedEntity"
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: Entity now has new id
        Assert.False(EntityIdRegistry.Instance.TryResolve("entity1", out _));
        Assert.True(EntityIdRegistry.Instance.TryResolve("renamedEntity", out var entity));
        Assert.True(BehaviorRegistry<IdBehavior>.Instance.TryGetBehavior(entity.Value, out var idBehavior));
        Assert.Equal("renamedEntity", idBehavior.Value.Id);
    }
    
    [Fact]
    public void RunFrame_WithIdBehaviorMutation_Invalid_FiresError()
    {
        // Arrange: Load fixture attempting to set non-string id
        SceneLoader.Instance.LoadGameScene("RulesDriver/Fixtures/id-mutation-invalid.json");
        
        // Act: Rule tries to set id to numeric value (invalid)
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        var error = _errorListener.ReceivedEvents.First();
        Assert.Contains("id", error.Message, StringComparison.OrdinalIgnoreCase);
        
        // Assert: Id unchanged
        Assert.True(EntityIdRegistry.Instance.TryResolve("entity1", out _));
    }
    
    // ========== TagsBehavior Tests ==========
    
    [Fact]
    public void RunFrame_WithTagsBehaviorMutation_AppliesCorrectly()
    {
        // Arrange: Entity starts with tags ["enemy"]
        SceneLoader.Instance.LoadGameScene("RulesDriver/Fixtures/tags-mutation.json");
        
        // Act: Rule adds "boss" tag
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: No errors
        Assert.Empty(_errorListener.ReceivedEvents);
        
        // Assert: Entity has both "enemy" and "boss"
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var entity));
        Assert.True(BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(entity.Value, out var tags));
        Assert.Contains("enemy", tags.Value.Tags);
        Assert.Contains("boss", tags.Value.Tags);
    }
    
    [Fact]
    public void RunFrame_WithTagsBehaviorMutation_Invalid_FiresError()
    {
        // Arrange: Fixture with malformed tags mutation (non-string tag)
        SceneLoader.Instance.LoadGameScene("RulesDriver/Fixtures/tags-mutation-invalid.json");
        
        // Act: Rule tries to add numeric tag
        RulesDriver.Instance.RunFrame(0.016f);
        
        // Assert: Error fired
        Assert.NotEmpty(_errorListener.ReceivedEvents);
        
        // Assert: Tags unchanged
        Assert.True(EntityIdRegistry.Instance.TryResolve("goblin", out var entity));
        Assert.True(BehaviorRegistry<TagsBehavior>.Instance.TryGetBehavior(entity.Value, out var tags));
        Assert.Single(tags.Value.Tags); // Only original "enemy" tag
    }
    
    // ... (repeat pattern for all 30+ behaviors)
}
```

#### JSON Fixture Organization

**Directory:** `MonoGame/Atomic.Net.MonoGame.Tests/RulesDriver/Fixtures/` (NEW)

**Fixture Naming:**
- `{behavior}-mutation.json` → Positive test fixture
- `{behavior}-mutation-invalid.json` → Negative test fixture

**Examples:**
- `id-mutation.json` / `id-mutation-invalid.json`
- `tags-mutation.json` / `tags-mutation-invalid.json`
- `transform-position-mutation.json` / `transform-position-mutation-invalid.json`
- `alignitems-mutation.json` / `alignitems-mutation-invalid.json`
- `borderleft-mutation.json` / `borderleft-mutation-invalid.json`

**Fixture Structure (Positive Example):**
```json
{
  "scene": "test-scene",
  "entities": [
    {
      "id": "entity1",
      "tags": ["enemy"],
      "transform": {
        "position": { "x": 10, "y": 20, "z": 0 }
      }
    }
  ],
  "rules": [
    {
      "from": "#enemy",
      "where": { "var": "entities" },
      "do": {
        "mut": [
          {
            "target": { "transform": { "position": true } },
            "value": { "x": 100, "y": 200, "z": 0 }
          }
        ]
      }
    }
  ]
}
```

**Fixture Structure (Negative Example):**
```json
{
  "scene": "test-scene-invalid",
  "entities": [
    {
      "id": "entity1",
      "alignItems": "center"
    }
  ],
  "rules": [
    {
      "from": "#entity1",
      "where": { "var": "entities" },
      "do": {
        "mut": [
          {
            "target": { "alignItems": true },
            "value": "invalidEnumValue"
          }
        ]
      }
    }
  ]
}
```

### Performance Considerations

#### Allocation Hotspots to Avoid
- **Per-entity JsonObject creation:** Pre-allocate and reuse entity JsonObjects
- **String allocations for enum conversions:** Use `Enum.Parse` with caching if needed
- **Vector/Quaternion serialization:** Reuse Vector3/Quaternion JsonObject structures
- **Tag add/remove:** Use ImmutableHashSet.Add/Remove (returns new set, but allocation is load-time via mutation path)

#### Cache-Friendly Patterns
- Leverage existing SparseArray iteration for entity serialization
- Batch mutations per-behavior-type to improve cache locality (future optimization)
- Consider struct-based mutation queues to avoid object allocations (future optimization)

---

## Tasks

- [ ] **Task 1:** Extend RulesDriver.BuildEntitiesArray() to serialize all writable behaviors
  - Add TransformBehavior serialization (position, rotation, scale, anchor as nested JsonObject)
  - Add ParentBehavior serialization (selector string)
  - Add individual Flex behavior serialization (30 behaviors as root-level keys in entity JsonObject)
  - Maintain zero-allocation pattern (reuse pre-allocated JsonObjects)

- [ ] **Task 2:** Implement mutation methods for Id and Tags
  - Implement `ApplyIdMutation()` with string validation and EntityIdRegistry integration
  - Implement `ApplyTagsMutation()` with add/remove operations
  - Add error handling for invalid id/tag formats
  - Create JSON fixtures: `id-mutation.json`, `id-mutation-invalid.json`, `tags-mutation.json`, `tags-mutation-invalid.json`
  - Write integration tests: 4 tests total (2 for Id, 2 for Tags)

- [ ] **Task 3:** Implement mutation methods for TransformBehavior
  - Implement `ApplyTransformMutation()` with field dispatch (position/rotation/scale/anchor)
  - Add Vector3 and Quaternion parsing from JsonNode with validation
  - Add error handling for missing x/y/z fields or invalid numeric values
  - Create JSON fixtures for each field: `transform-position-mutation.json`, `transform-rotation-mutation.json`, etc. (8 fixtures total)
  - Write integration tests: 8 tests total (4 fields × 2 directions)

- [ ] **Task 4:** Implement mutation methods for ParentBehavior
  - Implement `ApplyParentMutation()` with EntitySelector parsing
  - Add validation for selector string format
  - Note: `entity.SetBehavior()` will trigger hierarchy updates automatically via registries
  - Create JSON fixtures: `parent-mutation.json`, `parent-mutation-invalid.json`
  - Write integration tests: 2 tests total

- [ ] **Task 5:** Implement mutation methods for Flex behaviors (AlignItems, AlignSelf, Borders)
  - Implement mutations for: AlignItemsBehavior, AlignSelfBehavior, BorderBottomBehavior, BorderLeftBehavior, BorderRightBehavior, BorderTopBehavior
  - Add enum parsing with validation (Align enum)
  - Add float parsing with range validation for borders
  - Note: Each behavior is a separate root-level key in target object (e.g., `{ "alignItems": ... }`)
  - Create JSON fixtures: 12 fixtures (6 behaviors × 2 directions)
  - Write integration tests: 12 tests total

- [ ] **Task 6:** Implement mutation methods for Flex behaviors (Direction, Grow, Wrap, ZOverride)
  - Implement mutations for: FlexDirectionBehavior, FlexGrowBehavior, FlexWrapBehavior, FlexZOverride
  - Add enum parsing for FlexDirection, Wrap
  - Add float/int parsing with validation
  - Create JSON fixtures: 8 fixtures (4 behaviors × 2 directions)
  - Write integration tests: 8 tests total

- [ ] **Task 7:** Implement mutation methods for Flex behaviors (Dimensions)
  - Implement mutations for: HeightBehavior, WidthBehavior
  - Add parsing for value + percent flag (two-field behaviors)
  - Add validation for non-negative dimensions
  - Create JSON fixtures: 4 fixtures (2 behaviors × 2 directions)
  - Write integration tests: 4 tests total

- [ ] **Task 8:** Implement mutation methods for Flex behaviors (JustifyContent)
  - Implement mutation for: JustifyContentBehavior
  - Add enum parsing with validation (Justify enum)
  - Create JSON fixtures: 2 fixtures (1 behavior × 2 directions)
  - Write integration tests: 2 tests total

- [ ] **Task 9:** Implement mutation methods for Flex behaviors (Margins)
  - Implement mutations for: MarginBottomBehavior, MarginLeftBehavior, MarginRightBehavior, MarginTopBehavior
  - Add float parsing with validation
  - Create JSON fixtures: 8 fixtures (4 behaviors × 2 directions)
  - Write integration tests: 8 tests total

- [ ] **Task 10:** Implement mutation methods for Flex behaviors (Paddings)
  - Implement mutations for: PaddingBottomBehavior, PaddingLeftBehavior, PaddingRightBehavior, PaddingTopBehavior
  - Add float parsing with validation
  - Create JSON fixtures: 8 fixtures (4 behaviors × 2 directions)
  - Write integration tests: 8 tests total

- [ ] **Task 11:** Implement mutation methods for Flex behaviors (Positions)
  - Implement mutations for: PositionBottomBehavior, PositionLeftBehavior, PositionRightBehavior, PositionTopBehavior, PositionTypeBehavior
  - Add parsing for value + percent flag for position behaviors
  - Add enum parsing for PositionTypeBehavior
  - Create JSON fixtures: 10 fixtures (5 behaviors × 2 directions)
  - Write integration tests: 10 tests total

- [ ] **Task 12:** Update ApplyToTarget() with else-if pattern and generic error message
  - Convert existing if-chain to else-if for short-circuit optimization
  - Add all 30+ individual flex behavior checks
  - Replace specific error messages with generic "Unrecognized target, expected one of: [list]" message
  - Ensure read-only behaviors (WorldTransformBehavior, FlexBehavior) are not in valid targets list

- [ ] **Task 13:** Documentation and cleanup
  - Update RulesDriver.cs XML documentation with new mutation target formats
  - Add code comments explaining:
    - Enum/vector parsing strategies
    - How SetBehavior automatically triggers registry updates via event listeners
    - Why individual flex behaviors are root-level keys (not nested under "flex")
  - Verify all tests pass and no unhandled errors in logs
  - Ensure all JSON fixtures are committed and properly organized

---

## Notes for Implementation

### Behavior-to-JSON Key Mapping
- Each behavior type maps directly to a camelCase JSON key
- Examples:
  - `IdBehavior` → `"id"`
  - `TagsBehavior` → `"tags"`
  - `TransformBehavior` → `"transform"` (sub-object with position/rotation/scale/anchor)
  - `ParentBehavior` → `"parent"`
  - `AlignItemsBehavior` → `"alignItems"`
  - `BorderLeftBehavior` → `"borderLeft"`
  - `FlexGrowBehavior` → `"flexGrow"`
  - etc.

### Enum Parsing Strategy
- Use `Enum.TryParse<T>(string, ignoreCase: true, out result)` for all enum-based behaviors
- Return false and fire ErrorEvent if parsing fails
- Pre-validate string is not null/empty before parsing

### Vector3/Quaternion Parsing Strategy
- Expect JsonObject with fields: `{ "x": 1.0, "y": 2.0, "z": 3.0 }`
- Quaternion adds "w" field: `{ "x": 0, "y": 0, "z": 0, "w": 1 }`
- Validate all required fields present before constructing vector
- Use `TryGetValue<float>()` for each field with error handling

### Tags Add/Remove Operations
- Support two formats:
  - `{ "tags": { "add": "tag" } }` → Add single tag
  - `{ "tags": { "remove": "tag" } }` → Remove single tag
- Do NOT support adding/removing multiple tags in one operation (future enhancement)
- TagRegistry updates automatically when `entity.SetBehavior<TagsBehavior>()` is called

### Registry Auto-Update via SetBehavior
- All registries subscribe to `EntityBehaviorChangedEvent<TBehavior>`
- When `entity.SetBehavior<TBehavior>()` is called, the event is fired automatically
- Registries update their internal tracking structures in response
- Exception: IdBehavior may require explicit Unregister/Register if ID is changing (not just being set initially)

### Integration with Existing Tests
- Existing RulesDriverIntegrationTests should continue to pass unchanged
- New tests go in separate file to maintain organization
- Both test files use same fixture infrastructure (Scenes/Selectors/RulesDriver directories)
