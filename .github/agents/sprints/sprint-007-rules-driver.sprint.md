# Sprint: RulesDriver - Frame Execution, World Context, and Complex Test Coverage

## Non-Technical Requirements

### What should the system do
- The RulesDriver executes all active rules in a single frame, mutating matched entities based on parsed logic.
- It processes all global and scene rules in the order of their SparseArray indices.
- Each rule is passed a world context object (at minimum: deltaTime), plus the current set of matched entities.
- WHERE and DO clauses both operate on full arrays of entities, supporting array and aggregate JsonLogic operations (e.g., filter, map, reduce, sum, min, max).
- The driver mutates each entity in-place using the results from DO, mapping results back to entities via their `_index` property.
- The RulesDriver exposes a `RunFrame(float deltaTime)` method used to process a single frame (integration tests will call this directly; future game loop integration may come later).

### Success Criteria
- Rules can access world.deltaTime in WHERE and DO clauses.
- Mutations from DO are correctly applied to all relevant entities.
- All core and aggregate JsonLogic operations are supported for both WHERE/DO clauses.
- Zero gameplay allocations beyond initial load (data-driven, cache friendly, no allocations on RunFrame).
- Integration and unit tests demonstrate all supported rule and world context logic, including advanced aggregates.
- Negative tests confirm robust error handling for malformed logic, missing _index, bounds checking, and non-array returns.

### World Context Structure
```csharp
public record World<T>(float DeltaTime, List<T> Entities);
```

**Note:** This may require allocation. Test if JsonNode.Parse can accept IEnumerable to avoid allocating a List, but allocation may be necessary.

The context passed to WHERE/DO:
```json
{
  "world": {
    "deltaTime": 0.016667
  },
  "entities": [
    { "_index": 42, "properties": { "health": 100 }, "tags": ["enemy"] },
    { "_index": 73, "properties": { "health": 50 }, "tags": ["enemy"] }
  ]
}
```

### Canonical Test Examples

#### Example 1: Sum Aggregate (Total Enemy Health)
All 3 enemies get `totalEnemyHealth: 350`

#### Example 2: Average Calculation (Party Health)
All 4 party members get `avgPartyHealth: 70`

#### Example 3: Min/Max (Weakest/Strongest Enemy)
All enemies get `weakestEnemyHealth: 50`, `strongestEnemyHealth: 250`

#### Example 4: DeltaTime-Based Damage Over Time
`RunFrame(0.5f)` → `health: 95` (100 - 10 * 0.5)

#### Example 5: Conditional Count (Nearby Allies)
All 4 units get `nearbyCount: 3` (u1, u2, u4 are within 10 units)

#### Example 6: Nested Conditionals (Health-Based Regen)
`RunFrame(0.5f)`:
- c1 (health 90): `healthRegen: 2.5`
- c2 (health 60): `healthRegen: 5.0`
- c3 (health 30): `healthRegen: 10.0`

#### Example 7: Multi-Condition WHERE (AND/OR Logic)
Only e1, e2, e4 get `afflicted: true` (e3 has health 0)

#### Example 8: Tag-Based Filtering Within Rule (Friend/Foe Count)
All 5 units get `enemyCount: 3`, `allyCount: 2`

### Required Test Scenarios

#### Integration Tests (Full Pipeline Examples)
- Single rule, single entity, simple mutation
- DeltaTime-based mutation (Example 4)
- Multiple entities, WHERE filter, verify only subset mutated
- Multiple rules, intersecting selectors, sequential mutations
- Aggregate operations: sum, avg, min/max
- Group size logic: conditional logic on entities.length
- Cross-entity example: count entities with a tag, proximity
- Nested conditionals (Example 6)
- Multi-condition WHERE with AND/OR (Example 7)
- DeltaTime = 0, mutation is a no-op
- Multiple RunFrame() calls (verify state stacks)
- Global and scene rules together (single frame)

#### Unit Tests (Component/Logic Focused)
- Context builder with world/deltaTime
- Entity serialization, correct _index in group
- WHERE returns filtered array
- DO returns mutated array
- Error handling: WHERE/DO returns non-array, missing _index, out-of-bounds, invalid JsonLogic
- Mutation: applies only to correct entity by index

#### Negative/Error Tests
- Non-array returned from WHERE/DO
- Missing or misplaced _index
- Malformed JsonLogic (exception handling, no crash)
- Entity deactivated or gone mid-frame
- Rule capacity reached (verify all rules processed, no crash)
- Entities at capacity (mutation for all, no allocation)

#### Performance/Stress (signoff post-implementation)
- 1K entities, 10 rules, complex aggregates (verify <10ms)
- Zero gameplay allocation (benchmarked)

### Out of Scope
- Game loop integration (will be part of a later sprint)
- Rule priorities/custom ordering (SparseArray order only)
- Performance optimization is not part of the initial implementation, but will be benchmarked after initial release for further improvement.

---

## Technical Requirements

### Architecture

#### 1. RulesDriver (Frame Execution Engine)
**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/RulesDriver.cs`

**Responsibilities:**
- Singleton via `ISingleton<RulesDriver>`
- Exposes `RunFrame(float deltaTime)` method for frame execution
- Iterates all active rules in RuleRegistry.Rules SparseArray
- For each rule:
  1. Get matched entities via `rule.From.Matches` (SparseArray<bool>)
  2. Build World context with deltaTime and matched entities
  3. Evaluate WHERE clause via JsonLogic (if present)
  4. If WHERE passes, execute DO clause mutations
  5. Apply mutations back to entities via _index property

**Key Methods:**
- `RunFrame(float deltaTime)` - Execute all rules for a single frame
- `BuildWorldContext(float deltaTime, SparseArray<bool> matches)` - Create JsonNode context for JsonLogic evaluation
- `SerializeEntity(Entity entity)` - Serialize entity to JsonObject with _index, properties, tags
- `ApplyMutations(JsonArray mutations)` - Apply mutation results back to entities

**Data Structures:**
- Pre-allocated buffers for entity serialization (reuse each frame, zero allocations)
- Use `ArrayBufferWriter<byte>` or similar pooling for JSON serialization
- SparseArray for tracking which entities to process

**Error Handling:**
- Fire ErrorEvent for JsonLogic evaluation failures (never throw)
- Fire ErrorEvent for missing _index in mutation results
- Fire ErrorEvent for out-of-bounds _index values
- Fire ErrorEvent for non-array returns from WHERE/DO

#### 2. Entity Serialization (JsonNode Context)
**Approach:**

The World context needs to serialize entities to JSON for JsonLogic evaluation. Based on DISCOVERIES.md:
- Standard `JsonSerializer.Serialize()` is fastest (no pooling needed per LiteDB benchmarks)
- Serialize to JsonNode for JsonLogic compatibility
- Pre-allocate serialization buffers to reduce allocations

**Entity Structure for JsonLogic:**
```json
{
  "_index": 42,
  "properties": { "health": 100, "damage": 25 },
  "tags": ["enemy", "goblin"],
  "id": "goblin1"
}
```

**World Structure:**
```json
{
  "world": {
    "deltaTime": 0.016667
  },
  "entities": [ /* array of serialized entities */ ]
}
```

**Implementation Strategy:**
1. Iterate matched entities from selector's Matches SparseArray
2. For each matched entity:
   - Get _index (entity index in EntityRegistry)
   - Get id (via EntityIdRegistry if present)
   - Get properties (via PropertiesRegistry if present)
   - Get tags (via TagRegistry if present)
   - Serialize to JsonObject
3. Build World context JsonObject with deltaTime and entities array
4. Pass to JsonLogic evaluator

**Allocation Strategy:**
- Use `List<JsonObject>` pre-allocated to MaxEntities capacity (allocated once at driver init)
- Clear and reuse list each frame
- JsonNode serialization may allocate (acceptable during frame execution for MVP)
- Request @benchmarker to test pooling strategies if allocations are excessive

#### 3. JsonLogic Integration
**Library:** `json-everything` (already in dependencies from sprint-005)

**WHERE Clause Evaluation:**
- Input: World context with entities array
- JsonLogic expression evaluates to boolean or filter result
- If result is array, use filtered array for DO clause
- If result is boolean true, use all matched entities for DO
- If result is boolean false, skip DO clause
- If result is non-boolean/non-array, fire ErrorEvent and skip rule

**DO Clause Execution:**
- Input: World context with filtered entities (from WHERE or original matches)
- JsonLogic expression must use `map` operation to return array of mutations
- Each mutation must include `_index` property to identify target entity
- Apply mutations by looking up entity by _index and updating behaviors

**JsonLogic Operations Required:**
- Core: `var`, `if`, `==`, `!=`, `>`, `<`, `>=`, `<=`, `+`, `-`, `*`, `/`, `and`, `or`, `not`, `in`
- Array: `filter`, `map`, `reduce`, `merge`
- Aggregate: implemented via `reduce` (sum, avg, min, max)

**Note:** json-everything library supports all required operations. No custom operators needed.

#### 4. Mutation Application
**Approach:**

After DO clause evaluation, mutations need to be applied back to entities:

1. DO clause returns JsonArray of mutated entities (via `map` operation)
2. Each mutated entity has `_index` property indicating which entity to update
3. For each mutation:
   - Extract `_index` value
   - Validate _index is within bounds
   - Look up entity by index in EntityRegistry
   - Extract changed properties from mutation
   - Update entity behaviors (PropertiesBehavior, etc.)

**Behavior Updates:**
- Properties: Use PropertiesRegistry to update PropertiesBehavior
- Tags: Use TagRegistry to update TagsBehavior (if mutation includes tags)
- Other behaviors: Extract relevant fields from mutation JSON

**Immutable Behavior Pattern:**
- Get current behavior via registry
- Create new behavior with updated values
- Update registry with new behavior (fires BehaviorUpdatedEvent)

**Error Handling:**
- Missing _index: Fire ErrorEvent, skip mutation
- Out-of-bounds _index: Fire ErrorEvent, skip mutation
- Invalid JSON structure: Fire ErrorEvent, skip mutation
- Entity deactivated mid-frame: Fire ErrorEvent, skip mutation

#### 5. Event Lifecycle
**Events to Handle:**
- **InitializeEvent:** Create RulesDriver singleton, pre-allocate buffers
- **ShutdownEvent:** Clear buffers, unregister from event bus

**Events to Fire:**
- **ErrorEvent:** For all error conditions (never throw exceptions)

**No Event Firing During RunFrame:**
- RunFrame is explicit method call, not event-driven (per requirements)
- Future game loop integration will hook RunFrame to UpdateEvent

### Integration Points

#### Registries
- **RuleRegistry:** Source of all rules to execute (iterate Rules SparseArray)
- **EntityRegistry:** Lookup entities by index for mutation application
- **PropertiesRegistry:** Update entity properties from mutations
- **TagRegistry:** Read entity tags for serialization, update if mutation includes tags
- **EntityIdRegistry:** Read entity IDs for serialization (optional field)

#### JsonLogic Library
- Use `json-everything` library for WHERE/DO evaluation
- Pass World context as JsonNode
- Evaluate logic expressions against context
- Extract results as JsonNode for mutation application

#### SelectorRegistry
- Each rule's `From` selector provides `Matches` SparseArray<bool>
- Selectors are already recalculated (no recalc needed in RulesDriver)
- Iterate Matches to build entity list for WHERE/DO context

### Performance Considerations

#### Allocation Hotspots
1. **Entity Serialization to JsonNode:**
   - May allocate per frame (JsonObject, JsonArray creation)
   - Test if allocation is acceptable for MVP (<1ms for 1K entities)
   - Request @benchmarker to test pooling strategies if needed

2. **World Context Building:**
   - `List<JsonObject>` pre-allocated and cleared each frame (zero allocation)
   - JsonNode tree construction may allocate (acceptable for MVP)

3. **JsonLogic Evaluation:**
   - json-everything library may allocate internally
   - Acceptable for MVP, optimize later if benchmarks show issues

4. **Mutation Application:**
   - Behavior updates fire BehaviorUpdatedEvent (existing event bus pattern)
   - No allocations expected (struct-based events)

#### Performance Targets (Post-Implementation)
- 1K entities, 10 rules, complex aggregates: <10ms per RunFrame
- Zero allocations in hot paths (pre-allocated buffers reused)
- Request @benchmarker to validate after implementation

#### SIMD/Cache-Friendly Concerns
- SparseArray iteration is cache-friendly (existing pattern)
- Entity serialization may not be cache-optimal (acceptable for MVP)
- Future optimization: batch serialization, SIMD operations

#### Benchmarking Requests
- @benchmarker: Test entity serialization approaches (JsonSerializer vs manual JsonObject construction)
- @benchmarker: Test World context building with/without pooling
- @benchmarker: Stress test 1K entities, 10 rules, verify <10ms target
- @benchmarker: Allocation tracking to verify zero gameplay allocations

---

## Tasks

### Implementation Tasks

- [ ] Task 1: Create RulesDriver singleton with RunFrame(float deltaTime) method
  - Singleton pattern via ISingleton<RulesDriver>
  - Initialize in AtomicSystem.Initialize()
  - Pre-allocate entity serialization buffers (List<JsonObject> with MaxEntities capacity)
  - Event handlers for InitializeEvent, ShutdownEvent

- [ ] Task 2: Implement entity serialization to JsonObject with _index, properties, tags, id
  - SerializeEntity(Entity entity) method
  - Extract properties via PropertiesRegistry
  - Extract tags via TagRegistry
  - Extract id via EntityIdRegistry (if present)
  - Include _index as entity index in EntityRegistry
  - Reuse pre-allocated buffers to minimize allocations

- [ ] Task 3: Implement BuildWorldContext(float deltaTime, SparseArray<bool> matches)
  - Create JsonObject with "world" and "entities" properties
  - Serialize deltaTime to world.deltaTime
  - Iterate matches SparseArray, serialize matched entities to entities array
  - Return JsonNode for JsonLogic evaluation

- [ ] Task 4: Integrate json-everything library for WHERE clause evaluation
  - Evaluate WHERE JsonNode against World context
  - Handle boolean results (true = pass all, false = skip rule)
  - Handle array results (use filtered array for DO)
  - Fire ErrorEvent for non-boolean/non-array results

- [ ] Task 5: Integrate json-everything library for DO clause evaluation
  - Extract DO command (handle MutCommand variant)
  - Evaluate DO.Mut JsonNode against World context
  - Expect array result from map operation
  - Fire ErrorEvent for non-array results

- [ ] Task 6: Implement ApplyMutations(JsonArray mutations)
  - Iterate mutation array
  - Extract _index from each mutation
  - Validate _index bounds
  - Look up entity by index
  - Extract changed properties from mutation JSON
  - Update PropertiesBehavior with new values (immutable update pattern)
  - Fire ErrorEvent for missing _index, out-of-bounds, invalid structure

- [ ] Task 7: Implement RunFrame main loop
  - Iterate all rules in RuleRegistry.Rules SparseArray
  - For each active rule:
    - Get matches from rule.From.Matches
    - Build World context
    - Evaluate WHERE (if present)
    - Evaluate DO
    - Apply mutations
  - Error handling: never throw, always fire ErrorEvent

### Testing Tasks

- [ ] Task 8: Create test scene directory for RulesDriver tests
  - `MonoGame/Content/Scenes/Tests/RulesDriver/` directory
  - Organize by category: Aggregates/, DeltaTime/, Conditionals/, Negative/

- [ ] Task 9: Create integration test scenes for all 8 canonical examples
  - Example 1: Sum aggregate (total enemy health)
  - Example 2: Average calculation (party health)
  - Example 3: Min/Max (weakest/strongest enemy)
  - Example 4: DeltaTime-based damage over time
  - Example 5: Conditional count (nearby allies)
  - Example 6: Nested conditionals (health-based regen)
  - Example 7: Multi-condition WHERE (AND/OR logic)
  - Example 8: Tag-based filtering (friend/foe count)

- [ ] Task 10: Create integration tests in `MonoGame/Atomic.Net.MonoGame.Tests/Scenes/Integrations/RulesDriverIntegrationTests.cs`
  - Load each test scene
  - Call RulesDriver.Instance.RunFrame(deltaTime)
  - Assert mutations applied correctly
  - Assert deltaTime available in WHERE/DO
  - Assert aggregate operations work
  - Assert multiple rules execute in order
  - Assert global + scene rules both execute
  - Assert multiple RunFrame calls stack state correctly

- [ ] Task 11: Create unit tests in `MonoGame/Atomic.Net.MonoGame.Tests/Scenes/Units/RulesDriverUnitTests.cs`
  - Test BuildWorldContext with deltaTime and entities
  - Test SerializeEntity with _index, properties, tags, id
  - Test WHERE filtering returns correct subset
  - Test DO mutations return array with _index
  - Test ApplyMutations updates correct entities
  - Test error handling for all failure modes

- [ ] Task 12: Create negative tests for error handling
  - WHERE/DO returns non-array
  - Missing _index in mutation
  - Out-of-bounds _index
  - Malformed JsonLogic (invalid syntax)
  - Entity deactivated mid-frame
  - Rule capacity reached
  - All errors fire ErrorEvent, no exceptions thrown

- [ ] Task 13: Request benchmarking for performance validation
  - @benchmarker: Stress test 1K entities, 10 rules, complex aggregates (<10ms target)
  - @benchmarker: Allocation tracking (verify zero allocations on RunFrame)
  - @benchmarker: Compare entity serialization approaches (JsonSerializer vs manual)
  - @benchmarker: Test World context building with/without pooling

### Documentation Tasks

- [ ] Task 14: Add XML comments to RulesDriver public APIs
  - Document RunFrame method and parameters
  - Document World context structure
  - Document expected JsonLogic patterns
  - Document error handling behavior

---

## Notes for Implementation

### JsonLogic WHERE Clause Behavior
The WHERE clause can return:
1. **Boolean true:** Pass all matched entities to DO
2. **Boolean false:** Skip rule entirely
3. **Array:** Use filtered array for DO (WHERE acts as filter operation)
4. **Other:** Fire ErrorEvent, skip rule

### JsonLogic DO Clause Pattern
All DO clauses must use `map` operation to return array:
```json
{
  "mut": {
    "map": [
      { "var": "entities" },
      {
        "merge": [
          { "var": "" },
          { "properties": { "health": { "-": [{ "var": "properties.health" }, 10] } } }
        ]
      }
    ]
  }
}
```

This ensures:
- Each entity gets a mutation result
- Mutations include `_index` from original entity
- Parallel array structure for easy application

### Allocation Strategy Tradeoff
Initial implementation may allocate during JsonNode serialization. This is acceptable for MVP because:
1. Allocations occur once per frame (not per entity)
2. Json-everything library may require JsonNode input
3. Premature optimization without benchmarks is anti-pattern
4. Benchmarking will guide optimization if needed

If benchmarks show excessive allocations:
- Test Utf8JsonWriter with pooled buffers (per DISCOVERIES.md, may not help)
- Test manual JsonObject construction vs JsonSerializer
- Test entity serialization caching (if entities unchanged)

### Future Performance Optimizations (Out of Scope)
- Incremental evaluation (only process dirty entities)
- Parallel rule execution (if rules don't conflict)
- SIMD-based JsonLogic operations
- Entity serialization caching
- Custom JsonLogic evaluator (avoid json-everything allocations)

These optimizations should be benchmarked before implementation to ensure value.
