# Sprint: RulesDriver – Execute Rules with Array-Based Mutations

## Non-Technical Requirements

### What should the system do
- The RulesDriver executes all active rules in a single frame, mutating matched entities based on the array-of-operations `mut` format.
- Processes all global and scene rules in the order of their SparseArray indices.
- Each rule evaluates WHERE with context: `{ deltaTime, entities }` to filter the entity array.
- Each rule evaluates DO with context: `{ deltaTime, entities, self }` where `self` is the current entity being mutated.
- The `mut` array contains operations with `target` (path object like `{ "properties": "health" }`) and `value` (JsonLogic expression).
- The driver mutates entities in-place using their `_index` property to map results back to the entity storage.
- Exposes `RunFrame(float deltaTime)` method for single-frame execution (integration tests call this directly; game loop integration is future work).
- All JsonLogic operations are supported: filter, map, reduce, arithmetic, conditionals, and aggregates (sum, min, max, avg via reduce).

### Success Criteria
- Rules access `world.deltaTime` in WHERE clauses via `{ "var": "world.deltaTime" }`.
- Rules access `entities` array in WHERE clauses via `{ "var": "entities" }`.
- Mutations access current entity via `{ "var": "self.properties.X" }` in DO `value` expressions.
- Mutations access world context via `{ "var": "world.deltaTime" }` in DO `value` expressions.
- Mutations access full entity array via `{ "var": "entities" }` in DO `value` expressions for aggregates.
- All mutations correctly apply to matched entities using their `_index` for lookup.
- Zero gameplay allocations during `RunFrame()` execution (all allocations at load time).
- Integration tests demonstrate all required rule patterns with correct results.
- Negative tests confirm robust error handling: malformed logic, missing `_index`, bounds errors, non-array WHERE returns.

### World Context Structure

**WHERE clause context:**
```json
{
  "world": { "deltaTime": 0.016667 },
  "entities": [
    { "_index": 42, "properties": { "health": 100 }, "tags": ["enemy"] },
    { "_index": 73, "properties": { "health": 50 }, "tags": ["enemy"] }
  ]
}
```

**DO clause context (per entity being mutated):**
```json
{
  "world": { "deltaTime": 0.016667 },
  "entities": [
    { "_index": 42, "properties": { "health": 100 }, "tags": ["enemy"] },
    { "_index": 73, "properties": { "health": 50 }, "tags": ["enemy"] }
  ],
  "self": { "_index": 42, "properties": { "health": 100 }, "tags": ["enemy"] }
}
```

---

## Technical Requirements

### Architecture

#### 1. RulesDriver Component

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/RulesDriver.cs` (NEW)

**Responsibilities:**
- Singleton pattern using `ISingleton<T>`
- Exposes `RunFrame(float deltaTime)` method
- Iterates all active rules in RuleRegistry.Rules SparseArray
- For each rule: evaluate WHERE, get filtered entities, execute DO mutations
- Uses JsonLogic library for WHERE filtering and DO value evaluation
- Mutates entities via registries (PropertiesRegistry, TagRegistry, etc.)

**Key Design Decisions:**
- **Pre-allocated buffers:** All context objects, entity arrays, and mutation buffers allocated at initialization
- **Reuse, don't reallocate:** JsonObject context is updated per-rule/per-entity, not recreated
- **SparseArray iteration:** Leverage existing pattern for cache-friendly rule processing
- **Entity index mapping:** Use `_index` property from serialized entities to map back to Entity struct
- **Error resilience:** Individual rule failures logged via ErrorEvent, frame continues

**Data Flow:**
1. RunFrame(deltaTime) called
2. Build world context: `{ world: { deltaTime }, entities: [...] }`
3. For each active rule in RuleRegistry:
   a. Evaluate WHERE clause with world context → filtered JsonArray of entities
   b. For each filtered entity:
      - Build DO context: `{ world, entities, self: currentEntity }`
      - For each operation in MutCommand.Operations:
        * Evaluate operation.Value with DO context → computed value
        * Parse operation.Target path → identify registry and property key
        * Apply mutation to entity via appropriate registry
4. Optional: Fire EntityMutatedEvent for changed entities (future work)

#### 2. Entity Serialization to JsonArray

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/EntitySerializer.cs` (NEW)

**Responsibilities:**
- Convert active entities from registries into JsonArray suitable for JsonLogic
- Each entity serialized as JsonObject with: `_index`, `id`, `tags`, `properties`, `transform` (if present)
- Pre-allocates JsonArray buffer to Constants.MaxEntities capacity
- Clears and rebuilds array each frame (reuse allocation)
- Skips inactive/disabled entities

**Serialization Format:**
```json
{
  "_index": 42,
  "id": "goblin1",
  "tags": ["enemy", "burning"],
  "properties": {
    "health": 100,
    "burnDPS": 10
  },
  "transform": {
    "position": { "x": 100, "y": 50 }
  }
}
```

**Integration Points:**
- Reads from EntityRegistry to get active entity indices
- Reads from TagRegistry for tags array
- Reads from PropertiesRegistry for properties dictionary
- Reads from TransformRegistry for position/rotation/scale (if applicable)
- Optionally reads from other behavior registries (future extensibility)

**Performance Considerations:**
- **Pre-allocated JsonArray:** Reuse same array instance across frames
- **JsonObject pooling:** Investigate ArrayPool<JsonObject> or similar for entity objects (benchmark needed)
- **Selective serialization:** Only serialize properties needed for rules (tags, properties, transform)
- **Early exit:** Skip serialization if no active rules


#### 3. Context Builder

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/RuleContextBuilder.cs` (NEW)

**Responsibilities:**
- Builds JsonObject contexts for WHERE and DO evaluation
- Manages pre-allocated buffers to avoid per-frame/per-entity allocations
- Provides `BuildWhereContext(float deltaTime, JsonArray entities)` → JsonObject
- Provides `BuildDoContext(JsonObject whereContext, JsonNode self)` → JsonObject

**Design Pattern:**
```csharp
// Pre-allocated contexts (reused each frame)
private readonly JsonObject _whereContext = new();
private readonly JsonObject _doContext = new();
private readonly JsonObject _worldObject = new();

public JsonObject BuildWhereContext(float deltaTime, JsonArray entities)
{
    // Update world object
    _worldObject["deltaTime"] = deltaTime;
    
    // Update context
    _whereContext["world"] = _worldObject;
    _whereContext["entities"] = entities;
    
    return _whereContext;
}

public JsonObject BuildDoContext(JsonObject whereContext, JsonNode self)
{
    // Reuse world and entities from whereContext
    _doContext["world"] = whereContext["world"];
    _doContext["entities"] = whereContext["entities"];
    _doContext["self"] = self;
    
    return _doContext;
}
```

**Performance Rationale:**
- Reuse same JsonObject instances instead of `new JsonObject()` per-evaluation
- Reference sharing where possible (world, entities)
- Only update changed values (self per-entity)

#### 4. Mutation Applicator

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/MutationApplicator.cs` (NEW)

**Responsibilities:**
- Parses `target` path objects (e.g., `{ "properties": "health" }`)
- Applies computed values to entity properties via appropriate registries
- Supports target paths: `properties`, `tags` (add/remove), `transform` (position, rotation, scale)
- Error handling for unsupported paths or missing entities

**Supported Target Formats:**

1. **Properties Mutation:**
   - `{ "properties": "health" }` → Set PropertiesRegistry value
   - `ApplyPropertyMutation(ushort entityIndex, string key, JsonNode value)`

2. **Tag Mutation (Future):**
   - `{ "tags": { "add": "burning" } }` → Add tag
   - `{ "tags": { "remove": "burning" } }` → Remove tag

3. **Transform Mutation (Future):**
   - `{ "transform": "position" }` → Set position
   - `{ "transform": "rotation" }` → Set rotation

**Implementation Strategy:**
- Start with properties-only support (simplest)
- Validate path structure during parsing
- Throw clear exceptions for unsupported paths
- Future extensibility: add tag and transform support via pattern matching

**Error Handling:**
- Invalid path format → ErrorEvent, skip mutation
- Missing entity index → ErrorEvent, skip mutation
- Type conversion failure → ErrorEvent, skip mutation
- Out of bounds index → ErrorEvent, skip mutation

#### 5. Integration with PropertiesRegistry

**File:** `MonoGame/Atomic.Net.MonoGame/Properties/PropertiesRegistry.cs` (EXISTING)

**Required Interface:**
- Already has `Set(ushort entityIndex, string key, JsonNode value)` method
- RulesDriver calls this to apply property mutations
- No changes needed to PropertiesRegistry itself

**Integration Pattern:**
```csharp
// In MutationApplicator
var entityIndex = (ushort)entityJson["_index"]!.GetValue<int>();
var propertyKey = (string)target["properties"]!;
var computedValue = JsonLogic.Apply(operation.Value, doContext);

PropertiesRegistry.Instance.Set(entityIndex, propertyKey, computedValue);
```

### Integration Points

#### RulesDriver ↔ RuleRegistry
- **Direction:** RulesDriver reads from RuleRegistry
- **Interface:** `RuleRegistry.Instance.Rules` SparseArray
- **Pattern:** Iterate all active rules using `SparseArray.HasValue(i)` + `SparseArray.Get(i)`
- **Lifecycle:** RulesDriver does not manage rule lifecycle (loading handled by SceneLoader)

#### RulesDriver ↔ EntitySerializer
- **Direction:** RulesDriver calls EntitySerializer to build entities JsonArray
- **Interface:** `EntitySerializer.SerializeEntities()` → JsonArray
- **Pattern:** Call once per frame, reuses pre-allocated array
- **Allocation:** All allocations at EntitySerializer initialization

#### RulesDriver ↔ MutationApplicator
- **Direction:** RulesDriver calls MutationApplicator to apply mutations
- **Interface:** `MutationApplicator.ApplyMutation(JsonNode entity, MutOperation operation, JsonObject context)`
- **Pattern:** Call for each operation on each filtered entity
- **Error Handling:** MutationApplicator logs errors via ErrorEvent, returns success bool

#### EntitySerializer ↔ Registries
- **Reads from:**
  - EntityRegistry (active entity indices)
  - TagRegistry (entity tags)
  - PropertiesRegistry (entity properties)
  - TransformRegistry (optional - position/rotation/scale)
- **Pattern:** Read-only access, no mutations
- **Optimization:** Only serialize active entities (skip disabled)

#### MutationApplicator ↔ PropertiesRegistry
- **Direction:** MutationApplicator writes to PropertiesRegistry
- **Interface:** `PropertiesRegistry.Instance.Set(entityIndex, key, value)`
- **Pattern:** Direct registry updates, existing API
- **No changes needed:** PropertiesRegistry already supports this


### Performance Considerations

#### Allocation Hotspots (Benchmark Needed)

**Critical Question:** Can we achieve zero allocations during RunFrame()?

**Potential Allocations:**
1. **JsonArray entity serialization:**
   - Current approach: Create JsonObject per entity each frame
   - **@benchmarker:** Test object pooling vs. recreation
   - Hypothesis: ArrayPool<JsonObject> may help, but needs measurement

2. **JsonLogic.Apply() internal allocations:**
   - Library may allocate during evaluation
   - **@benchmarker:** Measure JsonLogic allocation behavior
   - Mitigation: If unavoidable, document as acceptable (library constraint)

3. **JsonObject context updates:**
   - Updating JsonObject properties may allocate strings/nodes
   - **@benchmarker:** Test reference vs. value updates
   - Mitigation: Reuse same context objects, update values in place

4. **DeepClone for entity isolation:**
   - POC uses `entity.DeepClone()` to isolate mutations
   - **@benchmarker:** Test if DeepClone is necessary or can be avoided
   - Alternative: Mutate in-place, track changes separately

**Benchmark Priorities (High to Low):**
1. EntitySerializer pooling strategy (highest impact - per-entity cost)
2. JsonLogic.Apply allocation profile (library constraint)
3. Context object reuse pattern (per-rule cost)
4. DeepClone vs. in-place mutation (per-entity cost)

#### Cache-Friendly Iteration

**Positive:**
- SparseArray iteration for rules (already cache-friendly)
- Array-based MutOperation[] (struct array, cache-friendly)

**Concern:**
- JsonNode/JsonObject are reference types (heap allocations, pointer chasing)
- EntitySerializer creates array of JsonObject references

**Mitigation:**
- Accept JsonNode as necessary for JsonLogic integration
- Focus optimization on minimizing creation/disposal
- Future: Investigate struct-based alternative if JsonNode proves too costly

#### SIMD Opportunities (Future Work)

**None identified for this sprint:**
- JsonLogic evaluation is library-controlled (no SIMD)
- Entity filtering via JsonLogic (not manual loop)
- Property mutations are per-property, not bulk operations

**Future Consideration:**
- If custom filtering/aggregation needed, consider SIMD for WHERE clause
- Not applicable for initial implementation

#### Error Handling Performance

**Design:**
- Errors logged via EventBus<ErrorEvent> (existing pattern)
- Individual rule/mutation failures do not abort frame
- Continue processing remaining rules after error

**Cost:**
- ErrorEvent push is cheap (struct-based event bus)
- No exception throwing in hot path (validate at load time)
- Runtime errors rare (well-formed JSON from SceneLoader)

**Tradeoff:**
- Resilience over fail-fast (game continues despite rule errors)
- Acceptable: Better than frame stutter from exception unwinding

### Data Structure Design Decisions

#### Why JsonArray for Entities?

**Decision:** Use JsonArray (not custom struct array) for entity representation

**Rationale:**
1. **JsonLogic Requirement:** Library expects JsonNode inputs
2. **Flexibility:** Supports arbitrary entity properties without schema
3. **Integration:** Matches POC pattern (proven to work)
4. **Simplicity:** Avoid custom serialization layer

**Tradeoff:**
- **Cost:** Reference type allocations
- **Benefit:** Direct JsonLogic compatibility

**Future Optimization:**
- If allocations prove excessive, explore custom JsonNode-compatible struct
- Not blocking for initial implementation

#### Why Reuse Context Objects?

**Decision:** Pre-allocate and reuse JsonObject contexts across frames

**Rationale:**
1. **Zero Allocation Goal:** Avoid `new JsonObject()` per-rule/per-entity
2. **Reference Semantics:** JsonObject allows property updates without reallocation
3. **Proven Pattern:** POC demonstrates reuse in burn damage test

**Implementation:**
```csharp
// Initialization (once)
private readonly JsonObject _whereContext = new();
private readonly JsonObject _doContext = new();

// Frame execution (reuse)
public void RunFrame(float deltaTime)
{
    _whereContext["world"]["deltaTime"] = deltaTime; // Update in-place
    _whereContext["entities"] = serializedEntities; // Reference update
    
    foreach (var rule in rules)
    {
        var filtered = JsonLogic.Apply(rule.Where, _whereContext);
        // ...
    }
}
```

**Risk:**
- JsonNode property assignment may allocate
- **@benchmarker:** Measure to confirm

#### Why Validate `_index` at Runtime?

**Decision:** Check entity `_index` presence and bounds during mutation

**Rationale:**
1. **Safety:** Prevent out-of-bounds registry access
2. **Debugging:** Clear error messages for malformed entities
3. **Robustness:** Handle edge cases (entity disabled mid-frame)

**Cost:**
- Per-entity bounds check (cheap - ushort comparison)
- ErrorEvent push on failure (rare)

**Benefit:**
- No crashes from bad indices
- Graceful degradation (skip entity, continue frame)

---

## Tasks

### Core Implementation

- [ ] **Task 1:** Create RulesDriver singleton
  - File: `MonoGame/Atomic.Net.MonoGame/Scenes/RulesDriver.cs`
  - Implement `ISingleton<RulesDriver>` pattern
  - Expose `RunFrame(float deltaTime)` method
  - Initialize pre-allocated buffers (context objects)
  - Register for ShutdownEvent cleanup

- [ ] **Task 2:** Create EntitySerializer
  - File: `MonoGame/Atomic.Net.MonoGame/Scenes/EntitySerializer.cs`
  - Implement `SerializeEntities()` → JsonArray method
  - Read from EntityRegistry for active entities
  - Read from TagRegistry for tags
  - Read from PropertiesRegistry for properties
  - Include `_index` in serialized JSON
  - Pre-allocate JsonArray buffer, reuse across calls

- [ ] **Task 3:** Create RuleContextBuilder
  - File: `MonoGame/Atomic.Net.MonoGame/Scenes/RuleContextBuilder.cs`
  - Implement `BuildWhereContext(deltaTime, entities)` method
  - Implement `BuildDoContext(whereContext, self)` method
  - Pre-allocate JsonObject buffers, reuse via property updates
  - Follow pattern from POC burn damage test

- [ ] **Task 4:** Create MutationApplicator
  - File: `MonoGame/Atomic.Net.MonoGame/Scenes/MutationApplicator.cs`
  - Implement `ApplyMutation(entity, operation, context)` method
  - Parse `target` path for `properties` format
  - Evaluate `value` JsonLogic expression with context
  - Call PropertiesRegistry.Set() for property mutations
  - Validate `_index` presence and bounds
  - Log ErrorEvent for failures, return success bool

- [ ] **Task 5:** Implement RulesDriver.RunFrame() orchestration
  - Iterate RuleRegistry.Rules SparseArray
  - Build WHERE context via RuleContextBuilder
  - Evaluate WHERE clause with JsonLogic.Apply()
  - Validate WHERE returns JsonArray (not JsonObject or null)
  - For each filtered entity, build DO context
  - For each MutOperation, call MutationApplicator
  - Log errors via ErrorEvent, continue on failure


### Integration Tests

- [ ] **Task 6:** Create integration test framework
  - File: `MonoGame/Atomic.Net.MonoGame.Tests/Scenes/Integrations/RulesDriverIntegrationTests.cs`
  - Helper methods: SetupEntities(), SetupRule(), RunFrameAndAssert()
  - Cleanup pattern: ShutdownEvent after each test

- [ ] **Task 7:** Integration test - Simple property mutation
  - Single entity, single rule, single property change
  - Verify property value updated after RunFrame()
  - Verify no other properties affected

- [ ] **Task 8:** Integration test - DeltaTime-based mutation (Example 4)
  - Burning entity with burnDPS property
  - Health decreases by burnDPS * deltaTime
  - Test with deltaTime = 0.5, verify health = 95 (from 100)

- [ ] **Task 9:** Integration test - WHERE filter (Example 7)
  - Multiple entities, WHERE clause filters subset
  - Verify only filtered entities mutated
  - Verify non-matching entities unchanged

- [ ] **Task 10:** Integration test - Sum aggregate (Example 1)
  - Multiple entities, reduce operation sums property
  - Each entity gets totalEnemyHealth property
  - Verify all entities have same sum value

- [ ] **Task 11:** Integration test - Average calculation (Example 2)
  - Multiple entities, division of sum by length
  - Verify avgPartyHealth calculated correctly
  - Test with 4 entities, average = 70

- [ ] **Task 12:** Integration test - Min/Max (Example 3)
  - Reduce operation for min and max values
  - Verify weakestEnemyHealth and strongestEnemyHealth
  - Test with 3 entities, min=50, max=250

- [ ] **Task 13:** Integration test - Conditional count (Example 5)
  - Filter entities by condition, count with reduce
  - Verify nearbyCount calculated correctly
  - Test with distance filter, expect count=3

- [ ] **Task 14:** Integration test - Nested conditionals (Example 6)
  - Health-based regen with if/else logic
  - Test with 3 entities at different health thresholds
  - Verify regen values: 2.5, 5.0, 10.0

- [ ] **Task 15:** Integration test - Multi-condition WHERE (Example 7)
  - AND/OR logic in WHERE clause
  - Verify afflicted property set only for matches
  - Test with 4 entities, expect 3 mutations

- [ ] **Task 16:** Integration test - Tag-based filtering (Example 8)
  - Filter by tags within DO reduce operations
  - Verify enemyCount and allyCount properties
  - Test with 5 entities, expect 3 enemies, 2 allies

- [ ] **Task 17:** Integration test - Multiple rules, sequential execution
  - Two rules with intersecting selectors
  - Verify second rule sees mutations from first rule
  - Confirm SparseArray iteration order respected

- [ ] **Task 18:** Integration test - Global and scene rules together
  - Load global rule, load scene rule
  - RunFrame processes both in order
  - Verify both mutations applied

- [ ] **Task 19:** Integration test - Multiple RunFrame calls
  - Call RunFrame() twice with different deltaTime
  - Verify state accumulates (e.g., health decreases each frame)
  - Confirm no state leakage between frames

- [ ] **Task 20:** Integration test - Empty entity set
  - No entities active
  - RunFrame() completes without error
  - No mutations attempted

- [ ] **Task 21:** Integration test - Empty rules set
  - No rules loaded
  - RunFrame() completes without error
  - Entities unchanged

### Unit Tests

- [ ] **Task 22:** Unit test - EntitySerializer output format
  - Create entity with tags and properties
  - Call SerializeEntities()
  - Verify JsonArray structure: _index, tags, properties
  - Verify inactive entities excluded

- [ ] **Task 23:** Unit test - RuleContextBuilder WHERE context
  - Call BuildWhereContext(0.5f, entities)
  - Verify context["world"]["deltaTime"] = 0.5
  - Verify context["entities"] references entities array

- [ ] **Task 24:** Unit test - RuleContextBuilder DO context
  - Call BuildDoContext(whereContext, selfEntity)
  - Verify context["world"] same reference as whereContext
  - Verify context["self"] references selfEntity

- [ ] **Task 25:** Unit test - MutationApplicator properties path
  - Create operation: `{ "properties": "health" }`, value=50
  - Call ApplyMutation on entity with index 0
  - Verify PropertiesRegistry.Get(0, "health") = 50

- [ ] **Task 26:** Unit test - MutationApplicator with JsonLogic value
  - Create operation with value: `{ "+": [10, 5] }`
  - Call ApplyMutation, verify result=15 applied

### Negative/Error Tests

- [ ] **Task 27:** Negative test - WHERE returns non-array
  - Rule WHERE clause: `{ "var": "world.deltaTime" }` (returns number)
  - RunFrame() logs ErrorEvent
  - No crash, frame continues

- [ ] **Task 28:** Negative test - Entity missing _index
  - Manually construct entity JSON without _index
  - RunFrame() logs ErrorEvent for that entity
  - Other entities processed normally

- [ ] **Task 29:** Negative test - _index out of bounds
  - Entity with _index=9999 (exceeds MaxEntities)
  - RunFrame() logs ErrorEvent
  - No registry access crash

- [ ] **Task 30:** Negative test - Malformed JsonLogic in WHERE
  - WHERE clause: `{ "invalid-op": [...] }`
  - JsonLogic.Apply() throws or returns null
  - RunFrame() logs ErrorEvent, continues to next rule

- [ ] **Task 31:** Negative test - Malformed JsonLogic in DO value
  - Operation value: `{ "invalid-op": [...] }`
  - JsonLogic.Apply() throws or returns null
  - RunFrame() logs ErrorEvent, skips mutation, continues

- [ ] **Task 32:** Negative test - Invalid target path format
  - Target: `{ "unsupported": "path" }` (not "properties")
  - MutationApplicator logs ErrorEvent
  - Mutation skipped, no crash

- [ ] **Task 33:** Negative test - Null target or value in operation
  - MutCommand with operation where target=null
  - Should not reach runtime (SceneCommandConverter validates)
  - If somehow reached, log ErrorEvent and skip

### Performance Benchmarking

- [ ] **Task 34:** @benchmarker Benchmark EntitySerializer pooling strategies
  - Compare: new JsonObject() per entity vs. ArrayPool<JsonObject> reuse
  - Measure: allocations and throughput
  - Scenarios: 100, 1000, 8000 entities
  - Recommendation: Which approach to use?

- [ ] **Task 35:** @benchmarker Benchmark JsonLogic.Apply() allocation profile
  - Measure allocations during WHERE and DO evaluation
  - Test with: simple var access, filter, reduce operations
  - Determine: Is zero-allocation achievable or is library cost acceptable?

- [ ] **Task 36:** @benchmarker Benchmark context object reuse
  - Compare: new JsonObject() per-rule vs. reuse with property updates
  - Measure: allocations and throughput
  - Confirm: Reuse strategy is zero-allocation

- [ ] **Task 37:** @benchmarker Benchmark DeepClone vs. in-place mutation
  - POC uses DeepClone for entity isolation
  - Test: Is DeepClone necessary or can we mutate original?
  - If DeepClone needed, measure cost and optimize

### Documentation

- [ ] **Task 38:** Add XML doc comments to RulesDriver
  - Document RunFrame() method and parameters
  - Document allocation guarantees (all at init time)
  - Document error handling behavior

- [ ] **Task 39:** Add XML doc comments to EntitySerializer
  - Document SerializeEntities() output format
  - Document which registries are read
  - Document reuse pattern (pre-allocated buffer)

- [ ] **Task 40:** Add XML doc comments to RuleContextBuilder
  - Document context structure for WHERE and DO
  - Document reuse pattern (pre-allocated objects)

- [ ] **Task 41:** Add XML doc comments to MutationApplicator
  - Document supported target path formats
  - Document error handling and return values


---

## Performance Expectations

**Target:** Zero allocations during RunFrame() execution after initialization

**Allocation Breakdown:**
- **At Initialization:**
  - RulesDriver buffers (context objects, temporary arrays)
  - EntitySerializer JsonArray buffer (Constants.MaxEntities capacity)
  - RuleContextBuilder JsonObject contexts

- **During RunFrame (Target: Zero):**
  - Entity serialization: Reuse JsonArray, investigate JsonObject pooling
  - Context building: Reuse JsonObject instances, update properties in-place
  - JsonLogic evaluation: Library-controlled (measure and document)
  - Mutation application: Registry Set() calls (existing APIs, likely zero-alloc)

**Benchmarking Required:**
- All allocation measurements must be benchmark-backed (see Tasks 34-37)
- If zero-allocation proves impossible due to JsonLogic library, document as acceptable constraint
- Focus optimization on controllable code (serialization, context building)

**Fallback Strategy:**
- If JsonLogic allocations are unavoidable, document as known limitation
- Optimize other paths to minimize total allocation
- Future: Consider custom JsonLogic implementation or alternative library

---

## Out of Scope (Future Work)

- Game loop integration (MonoGame Update() method calling RunFrame)
- Rule priorities or custom ordering (this sprint uses SparseArray index order only)
- Tag mutations (`{ "tags": { "add": "burning" } }`) - only properties supported initially
- Transform mutations (`{ "transform": "position" }`) - properties only
- EntityMutatedEvent firing (optional, not required for success criteria)
- Rule hot-reloading or runtime rule creation
- Performance optimization beyond zero-allocation baseline

---

## Code Reference

**Proof of Concept:**
`MonoGame/Atomic.Net.MonoGame.Tests/POCs/JsonLogicTests/JsonLogicObjectLiteralTests.cs`

**Test:** `DO_Mutations_BurnDamageOverTime_AppliesCorrectly`

**Key Patterns Demonstrated:**
1. Build WHERE context: `{ world: { deltaTime }, entities: [...] }`
2. Evaluate WHERE with JsonLogic.Apply() → filtered JsonArray
3. Build DO context per entity: `{ world, entities, self }`
4. Parse `mut` array operations: `{ target, value }`
5. Evaluate value with JsonLogic.Apply(operation.value, doContext)
6. Apply computed value to target path

**Integration Pattern (from POC):**
```csharp
// WHERE: Filter entities
var filteredEntities = JsonLogic.Apply(whereRule, worldContext);

// DO: For each filtered entity
foreach (var entity in filteredEntities)
{
    var doContext = new JsonObject {
        ["world"] = worldData["world"],
        ["entities"] = filteredEntities,
        ["self"] = entity
    };

    // Process each mutation in mut array
    foreach (var mutation in mutArray)
    {
        var target = mutation["target"];
        var valueRule = mutation["value"];
        var computedValue = JsonLogic.Apply(valueRule, doContext);
        
        // Apply computed value to target path on entity
        ApplyMutation(entity, target, computedValue);
    }
}
```

**Adaptation for Production:**
- Replace `new JsonObject()` with reused instances
- Replace manual parsing with MutCommand.Operations array
- Replace inline mutation with MutationApplicator
- Add error handling and validation
- Optimize for zero allocations

---

## Notes for senior-dev

### Implementation Order

**Recommended sequence:**
1. Create data structures (EntitySerializer, RuleContextBuilder, MutationApplicator)
2. Write unit tests for each component independently
3. Create RulesDriver orchestration
4. Write integration tests with simple cases first
5. Add complex integration tests (aggregates, conditionals)
6. Add negative/error tests
7. Request benchmarks from @benchmarker
8. Optimize based on benchmark findings

### Critical Design Decisions Already Made

**No Decisions Needed From senior-dev:**
- Use JsonArray for entities (JsonLogic requirement)
- Use JsonObject for contexts (proven in POC)
- Use MutOperation[] (already implemented in Sprint 007)
- Use properties-only mutations first (simplest path)

**Decisions Deferred to Benchmarking:**
- JsonObject pooling strategy (Task 34)
- DeepClone necessity (Task 37)
- Context reuse optimizations (Task 36)

### Testing Strategy

**Bottom-Up Approach:**
1. Unit test each component in isolation
2. Integration test simple scenarios (1 entity, 1 rule)
3. Integration test complex scenarios (multiple entities, aggregates)
4. Negative tests for error paths
5. Benchmark for allocation profile

**Coverage Goals:**
- All 8 canonical examples from requirements implemented as integration tests
- All error paths covered by negative tests
- All components unit tested independently

### Error Handling Philosophy

**Fail Gracefully, Continue Frame:**
- Individual rule failures do not abort frame
- Log errors via EventBus<ErrorEvent>
- Skip failed mutation, continue to next operation
- Designer sees errors in logs, can fix JSON

**Benefits:**
1. Game remains playable despite rule errors
2. Other rules continue to function
3. Clear error messages guide debugging
4. No exception unwinding overhead

**Tradeoff:**
- Silent failures if errors not monitored
- **Mitigation:** Integration tests verify error events fired
