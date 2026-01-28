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
  - `transform` → `{ "position": {...}, "rotation": {...}, "scale": {...}, "anchor": {...} }`
  - `parent` → `string` (entity selector as string)
  - Individual flex behaviors → `{ "flex": { "alignItems": ..., "alignSelf": ..., ... } }`
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
  "flex": {
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
}
```

**Performance Considerations:**
- **Lazy serialization:** Only serialize behaviors that exist on the entity (check TryGetBehavior first)
- **Reuse JsonObjects:** Pre-allocate transform/flex sub-objects, update values in-place
- **Cache-friendly iteration:** Leverage SparseArray iteration pattern
- **@benchmarker:** Test allocation impact of extended serialization (before/after comparison)

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
  - `{ "flex": { "alignItems": "center" } }` → Mutate AlignItemsBehavior
  - `{ "flex": { "alignSelf": "flexStart" } }` → Mutate AlignSelfBehavior
  - `{ "flex": { "borderBottom": 5.0 } }` → Mutate BorderBottomBehavior
  - (Similar patterns for all 30 writable flex behaviors listed in requirements)

**Mutation Methods (NEW):**
- `ApplyIdMutation(ushort entityIndex, JsonNode value)` → Parse string, set IdBehavior
- `ApplyTagsMutation(ushort entityIndex, JsonNode value)` → Parse add/remove operation, update TagsBehavior
- `ApplyTransformMutation(ushort entityIndex, string field, JsonNode value)` → Parse vector/quaternion, update TransformBehavior field
- `ApplyParentMutation(ushort entityIndex, JsonNode value)` → Parse selector string, set ParentBehavior
- `ApplyFlexMutation(ushort entityIndex, string field, JsonNode value)` → Dispatch to specific flex behavior setter

**Design Pattern:**
```csharp
private static bool ApplyToTarget(ushort entityIndex, JsonNode target, JsonNode value)
{
    if (target is not JsonObject targetObj)
    {
        // ... error handling
    }

    // Check each supported target type in priority order
    if (targetObj.TryGetPropertyValue("properties", out var propertyKey))
    {
        return ApplyPropertyMutation(entityIndex, propertyKey, value);
    }
    
    if (targetObj.TryGetPropertyValue("id", out _))
    {
        return ApplyIdMutation(entityIndex, value);
    }
    
    if (targetObj.TryGetPropertyValue("tags", out var tagOp))
    {
        return ApplyTagsMutation(entityIndex, tagOp, value);
    }
    
    if (targetObj.TryGetPropertyValue("transform", out var transformField))
    {
        return ApplyTransformMutation(entityIndex, transformField, value);
    }
    
    if (targetObj.TryGetPropertyValue("parent", out _))
    {
        return ApplyParentMutation(entityIndex, value);
    }
    
    if (targetObj.TryGetPropertyValue("flex", out var flexField))
    {
        return ApplyFlexMutation(entityIndex, flexField, value);
    }

    // Error: unsupported target
    EventBus<ErrorEvent>.Push(new ErrorEvent($"Unsupported target: {targetObj}"));
    return false;
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
- Mutation must be atomic: if parsing fails, do NOT partially update behavior

#### 3. Extend RulesDriver to Handle Read-Only Behaviors

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/RulesDriver.cs` (EXISTING - EXTEND)

**New Requirements:**
- Detect attempts to mutate read-only/computed behaviors
- Fire `ErrorEvent` with descriptive message
- Do NOT apply any mutation

**Read-Only Behaviors:**
- `WorldTransformBehavior` (computed from Transform + hierarchy)
- `FlexBehavior` (computed from individual flex behaviors)

**Implementation:**
```csharp
// In ApplyToTarget, add early checks
if (targetObj.TryGetPropertyValue("worldTransform", out _))
{
    EventBus<ErrorEvent>.Push(new ErrorEvent(
        $"Cannot mutate read-only WorldTransformBehavior for entity {entityIndex}"
    ));
    return false;
}

// Note: "flex" as a mutation target refers to individual flex behaviors (writable)
// FlexBehavior itself (the computed struct) is not directly accessible via rules
```

### Integration Points

#### With Existing Registries
- **IdBehavior:** Use `EntityIdRegistry.Instance.Register()` and `.Unregister()` for id changes
- **TagsBehavior:** Use `TagRegistry.Instance` to update tag tracking (add/remove from sets)
- **TransformBehavior:** Use `BehaviorRegistry<TransformBehavior>` for mutations
- **ParentBehavior:** Use `BehaviorRegistry<ParentBehavior>` for mutations, consider hierarchy invalidation
- **Flex Behaviors:** Use individual `BehaviorRegistry<TBehavior>` for each flex behavior type

#### Event System
- Fire `ErrorEvent` for all mutation failures (invalid format, read-only access, etc.)
- Existing `FakeEventListener<ErrorEvent>` in tests will capture these for assertions

### Test Structure

#### Test File Organization

**File:** `MonoGame/Atomic.Net.MonoGame.Tests/Scenes/Integrations/RulesDriverBehaviorMutationTests.cs` (NEW)

**Structure:**
- One test class for all behavior mutation tests
- Tests grouped by behavior category (Id, Tags, Transform, Parent, Flex)
- Each behavior has exactly 2 tests: positive mutation + negative mutation
- Total: ~32 behaviors × 2 tests = ~64 test methods

**Test Naming Convention:**
- Positive: `RunFrame_With{Behavior}Mutation_AppliesCorrectly`
- Negative: `RunFrame_With{Behavior}Mutation_Invalid_FiresError`

(Test examples and other sections truncated for brevity...)

### Performance Considerations

#### Allocation Hotspots to Avoid
- **Per-entity JsonObject creation:** Pre-allocate and reuse entity JsonObjects
- **String allocations for enum conversions:** Use pre-allocated lookup tables or `Enum.Parse` with caching
- **Vector/Quaternion serialization:** Reuse Vector3/Quaternion JsonObject structures
- **Tag add/remove:** Use ImmutableHashSet.Add/Remove (returns new set, but allocation is load-time via mutation path)

#### Benchmarking Requests
- **@benchmarker:** Compare allocation impact of extended BuildEntitiesArray() before/after changes
- **@benchmarker:** Test TagsBehavior mutation performance (ImmutableHashSet.Add vs custom mutable approach)

---

## Tasks

- [ ] **Task 1:** Extend RulesDriver.BuildEntitiesArray() to serialize all writable behaviors
- [ ] **Task 2:** Implement mutation methods for Id and Tags
- [ ] **Task 3:** Implement mutation methods for TransformBehavior
- [ ] **Task 4:** Implement mutation methods for ParentBehavior
- [ ] **Task 5:** Implement mutation methods for Flex behaviors (AlignItems, AlignSelf, Borders)
- [ ] **Task 6:** Implement mutation methods for Flex behaviors (Direction, Grow, Wrap, ZOverride)
- [ ] **Task 7:** Implement mutation methods for Flex behaviors (Dimensions)
- [ ] **Task 8:** Implement mutation methods for Flex behaviors (JustifyContent)
- [ ] **Task 9:** Implement mutation methods for Flex behaviors (Margins)
- [ ] **Task 10:** Implement mutation methods for Flex behaviors (Paddings)
- [ ] **Task 11:** Implement mutation methods for Flex behaviors (Positions)
- [ ] **Task 12:** Implement read-only behavior protection
- [ ] **Task 13:** @benchmarker Test allocation impact of extended BuildEntitiesArray()
- [ ] **Task 14:** @benchmarker Test TagsBehavior mutation performance
- [ ] **Task 15:** Documentation and cleanup
