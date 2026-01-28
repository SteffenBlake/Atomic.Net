# Sprint: Migrate Rule Scenes to MutCommand Array-Of-Operations Format

## Non-Technical Requirements

- All JSON-based rules using `mut` must be rewritten so that:
  - Each entity mutation is an element in a `mut` array.
  - Each mutation must contain:
    - `target`: an explicit path object (e.g. `{ "properties": "health" }`).
    - `value`: a JsonLogic node to evaluate and set at that path.
- The SceneCommand, MutCommand object, and loader/parser code must require this new format and reject the old format.
- Update ALL existing JSON scene files referenced in tests (including but not limited to):
  - poison-rule.json
  - multiple-rules.json
  - complex-precedence.json
  - no-entities-with-rules.json
  - entities-only.json (no changes needed - has no rules)
  - invalid-selector.json
- All rule scenes, test files, and loader/parser logic use the new `mut` array format exclusively
- No gameplay allocations added
- All unit/integration tests pass
- This migration is required for zero-allocation, flexible, data-driven game logic, and to enable robust per-property mutations that are performant and maintainable

---

## Technical Requirements

### Architecture

#### Current State Analysis

**Old Format (Current):**
```json
{
  "do": {
    "mut": {
      "map": [
        { "var": "entities" },
        {
          "merge": [
            { "var": "" },
            { "properties": { "health": 0 } }
          ]
        }
      ]
    }
  }
}
```

**New Format (Target):**
```json
{
  "do": {
    "mut": [
      {
        "target": { "properties": "health" },
        "value": { "-": [ { "var": "self.properties.health" }, 1 ] }
      }
    ]
  }
}
```

**Key Differences:**
1. **Old**: `mut` is a single JsonLogic object (typically `map` with `merge`)
2. **New**: `mut` is an array of operation objects, each with `target` and `value`
3. **Old**: Uses `{ "var": "entities" }` and `{ "var": "" }` for context
4. **New**: Uses `{ "var": "self.properties.health" }` for explicit paths within the current entity being mutated
5. **Old**: Nested structure with `map`/`merge` wrappers
6. **New**: Flat array structure, each operation is independent

#### 1. MutCommand Structure Change

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/MutCommand.cs`

**Current State:**
```csharp
public readonly record struct MutCommand(JsonNode Mut);
```

**Required State:**
```csharp
public readonly record struct MutCommand(MutOperation[] Operations);
```

**Rationale:**
- **Greedy Deserialization:** JsonRules are immutable and loaded once at scene load, never mutate after
- **Zero Runtime Allocations:** All parsing and allocation happens during scene load
- **Performance:** Pre-parsed array ready for iteration during rule execution
- **Type Safety:** Enforces correct structure at the type level, prevents old object format
- **Cache-Friendly:** Array of structs provides better cache locality than JsonArray iteration

#### 2. MutOperation Record

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/MutOperation.cs` (NEW)

**Structure:**
```csharp
public readonly record struct MutOperation(
    JsonNode Target,
    JsonNode Value
);
```

**Purpose:**
- Represents a single mutation operation
- `Target` holds path object (e.g., `{ "properties": "health" }`)
- `Value` holds JsonLogic expression to evaluate
- Deserialized eagerly from each array element in `mut` at scene load time

**Rationale:**
- Struct type for zero-allocation iteration
- Immutable after construction (readonly record struct)
- Simple data holder, execution logic lives elsewhere

#### 3. SceneCommandConverter Update

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/SceneCommandConverter.cs`

**Current Behavior:**
- Reads `mut` property value as JsonNode
- Passes to MutCommand constructor

**Required Changes:**
- Read `mut` property value
- Validate it's a JsonArray (not JsonObject or other type)
- Throw JsonException if not an array
- Allocate `MutOperation[]` with length matching array size
- For each array element at index i:
  - Validate it's a JsonObject
  - Validate it has `target` and `value` properties
  - Validate both properties are not null
  - Create `MutOperation(target, value)` and store at index i
  - Throw JsonException for any validation failure
- Pass `MutOperation[]` to MutCommand constructor

**Error Messages:**
- "Expected 'mut' to be an array" (if not JsonArray)
- "Expected mutation operation to be an object at index {i}" (if array element not object)
- "Missing 'target' property in mutation operation at index {i}" (if no target)
- "Missing 'value' property in mutation operation at index {i}" (if no value)
- "'target' cannot be null in mutation operation at index {i}" (if target null)
- "'value' cannot be null in mutation operation at index {i}" (if value null)

**Rationale:**
- Strict validation at parse time prevents runtime errors
- Clear error messages help designers fix JSON quickly
- Reject old format explicitly (object vs array type check)
- Greedy deserialization optimizes for immutable, load-once-use-many pattern

#### 4. JSON Scene File Migrations

**Files to Update:**
1. `MonoGame/Atomic.Net.MonoGame.Tests/Selectors/Fixtures/poison-rule.json`
2. `MonoGame/Atomic.Net.MonoGame.Tests/Selectors/Fixtures/multiple-rules.json`
3. `MonoGame/Atomic.Net.MonoGame.Tests/Selectors/Fixtures/complex-precedence.json`
4. `MonoGame/Atomic.Net.MonoGame.Tests/Selectors/Fixtures/no-entities-with-rules.json`
5. `MonoGame/Atomic.Net.MonoGame.Tests/Selectors/Fixtures/invalid-selector.json`

**Migration Pattern:**

**Old (map + merge):**
```json
{
  "mut": {
    "map": [
      { "var": "entities" },
      {
        "merge": [
          { "var": "" },
          { "properties": { "health": NEW_VALUE } }
        ]
      }
    ]
  }
}
```

**New (array of operations):**
```json
{
  "mut": [
    {
      "target": { "properties": "health" },
      "value": NEW_VALUE
    }
  ]
}
```

**Context Variable Changes:**
- **Within `value` expressions:** Access current entity being mutated via `self.*`
  - `{ "var": "properties.health" }` → `{ "var": "self.properties.health" }` (inside old map body)
  - Example: `{ "-": [{ "var": "self.properties.health" }, 1] }`
- **Accessing other entities in where clause:** Continue using `entities` array
  - Where clause still has access to full entity array for filtering
  - Example in where: `{ "filter": [{ "var": "entities" }, { ">": [{ "var": "properties.health" }, 0] }] }`
- Remove all `map` and `merge` wrappers
- Each property mutation becomes a separate array element

**Multi-Property Mutations:**

**Old (single merge with multiple properties):**
```json
{
  "merge": [
    { "var": "" },
    {
      "properties": {
        "health": 0,
        "poisonStacks": 0
      }
    }
  ]
}
```

**New (multiple operations in array):**
```json
[
  {
    "target": { "properties": "health" },
    "value": 0
  },
  {
    "target": { "properties": "poisonStacks" },
    "value": 0
  }
]
```

### Integration Points

#### SceneLoader
- **File:** `MonoGame/Atomic.Net.MonoGame/Scenes/SceneLoader.cs`
- **Current State:** Already deserializes rules via SceneCommandConverter
- **No Changes Needed:** Converter handles format validation and deserialization to MutOperation[]

#### Test Infrastructure
- **Files:** All test files using rules fixtures
- **Required:** Tests must pass with new JSON format
- **Validation:** Any tests asserting on rule structure need updates

### Performance Considerations

#### Parsing Performance
- **Old Format:** Single JsonNode, minimal validation
- **New Format:** JsonArray iteration + per-element validation + eager MutOperation[] allocation
- **Impact:** Slightly more work at load time (acceptable - happens once)
- **Benefit:** Strict validation catches errors early + zero runtime overhead

#### Runtime Allocation
- **Old Format:** JsonNode stored, iteration/parsing deferred to runtime
- **New Format:** MutOperation[] pre-allocated at load time
- **Runtime:** Zero allocations - just iterate pre-parsed struct array
- **Target:** All allocations at scene load, zero allocations during gameplay execution

#### Validation Overhead
- **Cost:** O(n) where n = number of operations in array
- **Typical Case:** 1-5 operations per rule (negligible load time overhead)
- **Edge Case:** 50+ operations (still acceptable, happens once at scene load)

### Data Structure Design Decisions

#### Why MutOperation[] instead of JsonArray?

**Decision:** Use `MutOperation[]` in MutCommand, not `JsonArray`

**Rationale:**
1. **Immutable Load-Once Pattern:** JsonRules load at scene load and never mutate - optimize for this
2. **Greedy Deserialization:** Do all parsing work at load time for zero runtime cost
3. **Zero Runtime Allocations:** Pre-parsed struct array ready for iteration
4. **Cache-Friendly:** Contiguous array of structs better than JsonNode traversal
5. **Type Safety:** Enforces schema at deserialization, no runtime validation needed
6. **Simpler Execution:** Future execution code just iterates array, no parsing

**Performance Impact:**
- **Load Time:** Slightly more work (parse all operations immediately)
- **Runtime:** Zero overhead - just array iteration
- **Memory:** Pre-allocated array vs deferred parsing (acceptable tradeoff)

**Alignment with Project Principles:**
- Follows zero-allocation gameplay constraint (all allocs at load)
- Matches pattern used elsewhere (pre-parse, pre-allocate, execute fast)
- Data-driven design: JSON → strongly-typed structs → fast execution

#### Why Validate Array Elements?

**Decision:** Validate each array element has `target` and `value` during parsing

**Rationale:**
1. **Fail Fast:** Catch malformed JSON at load time, not execution time
2. **Clear Errors:** Designer sees "Missing target at index 2" immediately
3. **Type Safety:** Ensures all operations conform to schema
4. **Zero Runtime Surprises:** No null checks needed during execution

---

## Tasks

### Code Changes

- [ ] **Task 1:** Update MutCommand structure
  - Change `JsonNode Mut` to `MutOperation[] Operations`
  - Update file: `MonoGame/Atomic.Net.MonoGame/Scenes/MutCommand.cs`

- [ ] **Task 2:** Create MutOperation record
  - Create new file: `MonoGame/Atomic.Net.MonoGame/Scenes/MutOperation.cs`
  - Define `readonly record struct MutOperation(JsonNode Target, JsonNode Value)`
  - Add XML doc comments

- [ ] **Task 3:** Update SceneCommandConverter validation
  - Update file: `MonoGame/Atomic.Net.MonoGame/Scenes/SceneCommandConverter.cs`
  - Add JsonArray type check for `mut` property
  - Allocate `MutOperation[]` array with correct size
  - Add per-element parsing loop that creates MutOperation structs
  - Add error messages for all failure cases
  - Throw JsonException for old object format
  - Throw JsonException for missing/null target/value

### JSON File Migrations

- [ ] **Task 4:** Migrate poison-rule.json
  - Convert `map` + `merge` to array of operations
  - Update context variables (inside map: `properties.*` → `self.properties.*`)
  - Test file loads without errors

- [ ] **Task 5:** Migrate multiple-rules.json
  - Convert both rules to new format
  - Handle multiple properties per rule (multi-element arrays)
  - Test file loads without errors

- [ ] **Task 6:** Migrate complex-precedence.json
  - Convert to new format
  - Test file loads without errors

- [ ] **Task 7:** Migrate no-entities-with-rules.json
  - Convert to new format
  - Test file loads without errors

- [ ] **Task 8:** Migrate invalid-selector.json
  - Convert to new format (still invalid selector, but valid mutation format)
  - Test file loads and fires selector error (not mutation error)

### Testing and Validation

- [ ] **Task 9:** Create negative test for old format rejection
  - Create test scene with old `map`/`merge` format
  - Assert SceneLoader throws JsonException
  - Assert error message mentions "Expected 'mut' to be an array"

- [ ] **Task 10:** Create negative test for missing target
  - Create test scene with `mut: [{ "value": 10 }]` (no target)
  - Assert SceneLoader throws JsonException
  - Assert error message mentions "Missing 'target'"

- [ ] **Task 11:** Create negative test for missing value
  - Create test scene with `mut: [{ "target": {...} }]` (no value)
  - Assert SceneLoader throws JsonException
  - Assert error message mentions "Missing 'value'"

- [ ] **Task 12:** Create negative test for null target/value
  - Create test scene with `mut: [{ "target": null, "value": 10 }]`
  - Assert SceneLoader throws JsonException

- [ ] **Task 13:** Run all existing integration tests
  - Verify all tests pass with migrated JSON files
  - Verify no gameplay allocations introduced
  - Verify error handling still works

### Documentation

- [ ] **Task 14:** Add XML comments to MutOperation
  - Document Target and Value properties
  - Document purpose (data holder for mutation operations)

- [ ] **Task 15:** Update SceneCommandConverter comments
  - Document validation rules
  - Document error cases

---

## Example Conversions

### Example 1: Poison Rule (Single Property Mutation)

**Before:**
```json
{
  "do": {
    "mut": {
      "map": [
        { "var": "entities" },
        {
          "merge": [
            { "var": "" },
            {
              "properties": {
                "health": { "-": [{ "var": "properties.health" }, 1] }
              }
            }
          ]
        }
      ]
    }
  }
}
```

**After:**
```json
{
  "do": {
    "mut": [
      {
        "target": { "properties": "health" },
        "value": { "-": [ { "var": "self.properties.health" }, 1 ] }
      }
    ]
  }
}
```

**Changes:**
1. `mut` is now an array `[...]` instead of object `{...}`
2. Single operation object with `target` and `value`
3. `target` is path object: `{ "properties": "health" }`
4. `value` is the expression: `{ "-": [...] }`
5. Context variable changed: `properties.health` → `self.properties.health` (within the mutation value)
6. Removed `map` and `merge` wrappers

### Example 2: Multiple Properties (Multi-Operation Array)

**Before:**
```json
{
  "do": {
    "mut": {
      "map": [
        { "var": "entities" },
        {
          "merge": [
            { "var": "" },
            {
              "properties": {
                "health": { "-": [{ "var": "properties.health" }, { "var": "properties.poisonStacks" }] },
                "poisonStacks": 0
              }
            }
          ]
        }
      ]
    }
  }
}
```

**After:**
```json
{
  "do": {
    "mut": [
      {
        "target": { "properties": "health" },
        "value": { "-": [ { "var": "self.properties.health" }, { "var": "self.properties.poisonStacks" } ] }
      },
      {
        "target": { "properties": "poisonStacks" },
        "value": 0
      }
    ]
  }
}
```

**Changes:**
1. Each property mutation is a separate array element
2. First operation mutates `health`
3. Second operation mutates `poisonStacks`
4. All context variables updated to `self.*` prefix (within mutation values)
5. Removed all `map` and `merge` wrappers

### Example 3: Invalid Format (Should Fail)

**Old Format (Should Now Throw):**
```json
{
  "do": {
    "mut": {
      "merge": [
        { "var": "entities" },
        { "properties": { "health": 0 } }
      ]
    }
  }
}
```

**Expected Error:**
```
JsonException: Expected 'mut' to be an array
```

**Correct Format:**
```json
{
  "do": {
    "mut": [
      {
        "target": { "properties": "health" },
        "value": 0
      }
    ]
  }
}
```

---

## Migration Verification Checklist

For each JSON file migration, verify:

- [ ] File deserializes without JsonException
- [ ] `mut` is a JsonArray, not JsonObject
- [ ] Each array element has both `target` and `value` properties
- [ ] No `map` or `merge` JsonLogic operators remain in the `mut` section
- [ ] Context variables in mutation `value` expressions use `self.*` for the current entity being mutated
- [ ] Context variables in `where` clauses can still use `entities` to access the full entity array
- [ ] All `{ "var": "properties.*" }` (inside old map body) changed to `{ "var": "self.properties.*" }`
- [ ] Multi-property mutations split into separate array elements
- [ ] Tests using the file still pass

---

## World Object Structure Reference

**For Execution (Future Work):**

The world object structure during rule evaluation will be:

```typescript
{
  deltaTime: number,      // Frame delta time
  entities: Entity[],     // Full array of entities being evaluated (for where clause)
  self: Entity           // Current entity being mutated (for do.mut value expressions)
}
```

**Usage in JSON:**
- **In `where` clause:** Use `{ "var": "entities" }` to access array for filtering
  - Example: `{ "filter": [{ "var": "entities" }, { ">": [{ "var": "properties.health" }, 0] }] }`
- **In `do.mut` value:** Use `{ "var": "self.properties.X" }` to access current entity
  - Example: `{ "-": [{ "var": "self.properties.health" }, 1] }`

**Note:** This sprint only changes the JSON structure. Execution implementation is future work.

---

## Notes for Implementation

### Backward Compatibility

**Decision:** NO backward compatibility for old format

**Rationale:**
1. **Pre-release codebase:** No production users to migrate
2. **Clean break:** Prevents confusion about which format to use
3. **Simpler code:** No format detection or dual-path logic
4. **All files in repo:** We control all JSON files, can migrate them all

**Impact:**
- Old format throws JsonException immediately
- Clear error message guides users to new format
- Test failures are obvious and easy to fix

### Error Handling Philosophy

**Fail Fast at Parse Time:**
- Validate array structure during deserialization
- Validate each operation has target/value
- Throw JsonException with clear index and field name
- Never defer validation to execution time

**Benefits:**
1. Designer sees error immediately when loading scene
2. Error message pinpoints exact problem ("Missing 'target' at index 2")
3. No runtime surprises during gameplay
4. Simpler execution code (no null checks needed)

### Stage 1 vs Stage 2 Separation

**Stage 1 (This Sprint):**
- ✅ Change data structure (JsonNode → MutOperation[])
- ✅ Update validation rules
- ✅ Migrate all JSON files
- ✅ Ensure parsing works
- ✅ Greedy deserialization at load time
- ❌ NO execution of operations (future work)

**Stage 2 (Future Sprint):**
- Execute `mut` array during rule evaluation
- Parse `target` path and resolve entity property
- Evaluate `value` JsonLogic expression
- Set property value at target path
- Handle errors during execution (not parsing)

**Boundary:**
- This sprint: "Can we parse and validate the new format into MutOperation[]?"
- Next sprint: "Can we execute the parsed operations?"

### Why MutOperation Now?

**Question:** Why define and use MutOperation if we're not executing yet?

**Answer:**
1. **Greedy Deserialization:** Parse JSON into typed structs at load time (zero runtime cost)
2. **Type Safety:** Ensures all operations conform to schema before execution exists
3. **Testing:** Tests can validate structure without full execution
4. **Performance:** Pre-parsed array ready for fast iteration in future execution code
5. **Immutable Pattern:** Matches JsonRule load-once-use-many design

**Usage:**
- MutOperation[] is created during SceneCommandConverter.Read()
- Stored in MutCommand.Operations
- Ready for iteration when execution is implemented
