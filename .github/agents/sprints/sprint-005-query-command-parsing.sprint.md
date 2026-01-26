# Sprint: Query + Command System - Parser Infrastructure

## Non-Technical Requirements

### What We're Building
- Build parsing infrastructure for Query + Command system enabling complex, data-driven logic in JSON scene files
- **Stage 1: Parser Only** — no execution or evaluation
- **Input:** JSON scene file with `rules` and `entities`
- **Output:** Parsed `JsonRule` objects with validated AST structure

### Why Now?
- Unblocks all future gameplay authoring in data (core roadmap goal)
- Enables designers to write non-trivial game logic purely in JSON
- Replaces brittle old selector logic, unifies all entity selection under one parser
- Supports future additions (tags, events, set logic) without refactoring
- Needed for zero-allocation, test-driven pipelines

### Success Criteria
- FROM selectors parse with correct operator precedence (`,` = union, `:` = refinement)
- Supports: `@id`, `#tag`, `!enter`, `!exit` (more in future stages)
- WHERE and DO sections pass their raw JSON to the correct libraries (JsonLogic for WHERE/DO)
- All rules loaded from JSON scenes with `{ "entities": [...], "rules": [...] }` structure (rules optional)
- No allocations during gameplay (all allocations at scene load)
- All errors during parsing fire ErrorEvents, never throw or crash

### Migration Constraints
- Existing Parent selector (entity parenting in scenes) MUST migrate to use the new EntitySelector parser (no string-matching hacks)
- All current Parent/child scene relationships must be tested and pass

### Testing Criteria
- Full suite of complex unit tests for rule parsing, loading scenes from file (not inline strings)
- Tests must cover: single/compound selectors, precedence, invalid syntax, Parent migration, integration with entities/rules sections
- Every test scene must have `entities` (even if empty)
- Tests go in `Content/Scenes/Tests/QueryCommand/`

### Constraints
- Zero gameplay allocations, all parsing allocates at load
- All tests use file-based scenes, never inline strings
- All scene files for tests must have both `entities` and `rules` arrays declared (even if empty)

### Out of Scope
- No execution of rules
- No tag or physics registry implementation (selectors parse, don't match)
- No mutation, events, or runtime logic
- No gameplay allocations

---

## Technical Requirements

### Architecture

#### Current Implementation Status (Post-Refactor)

The major refactor has reorganized the codebase with new namespaces and file locations:
- **Selectors** → `MonoGame/Atomic.Net.MonoGame/Selectors/`
- **Scenes** → `MonoGame/Atomic.Net.MonoGame/Scenes/`
- **BED** (Behavior-Entity-Driver) → `MonoGame/Atomic.Net.MonoGame/BED/`
- **Tests** → `MonoGame/Atomic.Net.MonoGame.Tests/`

#### 1. EntitySelector Parser ✅ **IMPLEMENTED**
**Location:** `MonoGame/Atomic.Net.MonoGame/Selectors/EntitySelector.cs`

**Current State:**
- ✅ Variant type with all selector variants (Union, Id, Tag, CollisionEnter, CollisionExit)
- ✅ `Recalc()` and `Matches` properties correctly delegate to variants
- ✅ `WriteTo()` for ToString serialization
- ✅ Parent behavior successfully migrated to use EntitySelector
- ✅ JsonConverter integration complete

**Variant Types (All Implemented):**
- `UnionEntitySelector` - holds `EntitySelector[]` for `,` operator
- `IdEntitySelector` - holds `string Id` + optional `Prior` chain
- `TagEntitySelector` - holds `string Tag` + optional `Prior` chain (Recalc throws NotImplementedException - expected)
- `CollisionEnterEntitySelector` - optional `Prior` chain (Recalc throws NotImplementedException - expected)
- `CollisionExitEntitySelector` - optional `Prior` chain (Recalc throws NotImplementedException - expected)

**Note:** Tag and Collision selectors correctly stub NotImplementedException in Recalc() as they require registry implementations (future work).

#### 2. SelectorRegistry ✅ **IMPLEMENTED**
**Location:** `MonoGame/Atomic.Net.MonoGame/Selectors/SelectorRegistry.cs`

**Current State:**
- ✅ `TryParse(ReadOnlySpan<char>, out EntitySelector?)` fully implemented
- ✅ Recursive descent parser with span slicing (zero-copy)
- ✅ Operator precedence: `:` binds tighter than `,`
- ✅ Error handling fires ErrorEvent, never throws
- ✅ Caching/deduplication via hash-based dictionaries
- ✅ Root selector tracking for bulk Recalc
- ✅ Event handlers for IdBehavior changes to mark selectors dirty
- ✅ Reset and Shutdown event handlers

**Precedence Implementation:**
- `:` chains are parsed first (refinement)
- `,` creates union of chains
- Examples correctly handled:
  - `@player, @boss` → `Union([@player, @boss])`
  - `!enter:#enemies` → `CollisionEnter(Prior: Tagged("enemies"))`
  - `@player, !enter:#enemies:#boss` → `Union([@player, CollisionEnter(Prior: Tagged("enemies", Prior: Tagged("boss")))])`

#### 3. EntitySelectorConverter ✅ **IMPLEMENTED**
**Location:** `MonoGame/Atomic.Net.MonoGame/Selectors/EntitySelectorConverter.cs`

**Current State:**
- ✅ Calls `SelectorRegistry.Instance.TryParse()` for reading
- ✅ Throws JsonException if parsing fails (standard JsonConverter error handling)
- ✅ Serialization via selector.ToString()
- ✅ Integrated with Parent field deserialization

#### 4. JsonRule and SceneCommand ✅ **IMPLEMENTED**
**Location:** `MonoGame/Atomic.Net.MonoGame/Scenes/JsonRule.cs` and `SceneCommand.cs`

**Current State:**
- ✅ `JsonRule` record struct with `EntitySelector From`, `JsonNode Where`, `SceneCommand Do`
- ✅ `SceneCommand` variant with `MutCommand` variant
- ✅ `MutCommand` holds `JsonNode Mut` for future JsonLogic evaluation

#### 5. JsonScene Structure ✅ **IMPLEMENTED**
**Location:** `MonoGame/Atomic.Net.MonoGame/Scenes/JsonScene.cs`

**Current State:**
- ✅ `Entities` list (required, can be empty)
- ✅ `Rules` list (nullable, optional)
- ✅ Correctly deserializes from JSON

#### 6. SceneLoader ⚠️ **PARTIALLY IMPLEMENTED**
**Location:** `MonoGame/Atomic.Net.MonoGame/Scenes/SceneLoader.cs`

**Current State:**
- ✅ Loads entities from JsonScene
- ✅ Calls `SelectorRegistry.Instance.Recalc()` after scene load
- ✅ Calls `HierarchyRegistry.Instance.Recalc()` after selector recalc
- ❌ **MISSING:** Does not process `JsonScene.Rules` (parses them via JsonScene but doesn't validate or store)

**Required Changes:**
- Parse rules from `JsonScene.Rules` if present
- Validate each rule's `from` field parses successfully (TryParse already validates during deserialization via EntitySelectorConverter)
- Fire ErrorEvent for any parsing failures during rule loading
- Store parsed rules somewhere (TBD - may just be in-memory List for Stage 1 since execution is out of scope)

### Integration Points

#### Event System
- ✅ **ErrorEvent:** Fired for all parsing failures (invalid syntax, unknown operators, etc.)
- ✅ **IdBehavior Events:** SelectorRegistry listens to mark selectors dirty
- ✅ **ResetEvent:** Recalcs all selectors to clear non-persistent entity matches
- ✅ **ShutdownEvent:** Clears all registries

#### Registries
- ✅ **EntityIdRegistry:** Used by `IdEntitySelector.Recalc()` - working
- ⚠️ **TagsRegistry:** Stubbed in `TagEntitySelector.Recalc()` - throws NotImplementedException (expected for Stage 1)
- ⚠️ **CollisionRegistry:** Stubbed in `CollisionEnter/Exit.Recalc()` - throws NotImplementedException (expected for Stage 1)

#### JsonLogic Library
- ✅ **Integration Point:** JsonRule's `Where` and `Do` fields are JsonNode
- ✅ **Current State:** Fields store raw JSON, no evaluation (Stage 1: parsing only)
- ✅ **Future Work:** Stage 2 will evaluate using json-everything library

### Performance Considerations

#### Allocation Strategy ✅ **CORRECT**
- **Load Time (ACCEPTABLE):** 
  - String parsing allocates variant objects
  - EntitySelector[] arrays for unions
  - JsonNode parsing (System.Text.Json)
- **Runtime (ZERO ALLOCATIONS):**
  - Parsed AST is readonly structs/classes
  - `Recalc()` walks AST using pre-allocated SparseArray<bool> Matches
  - Span-based parsing avoids temporary strings

#### Parser Implementation ✅ **OPTIMAL**
- ✅ Recursive descent parser with span slicing
- ✅ No Regex (would allocate)
- ✅ No string.Split (would allocate)
- ✅ No LINQ (would allocate)
- ✅ ReadOnlySpan<char> slicing for zero-copy tokenization
- ✅ Hash-based deduplication prevents duplicate selector instances

---

## Tasks

### Implementation Tasks (Mostly Complete)

- [x] **Task 1:** Implement EntitySelector variant type
  - All selector types implemented (Union, Id, Tag, CollisionEnter, CollisionExit)
  - Recalc() and Matches delegation working
  - ToString() serialization working

- [x] **Task 2:** Implement SelectorRegistry.TryParse
  - Recursive descent parser complete
  - Operator precedence correct (`:` binds tighter than `,`)
  - Span-based parsing for zero-copy
  - Error handling fires ErrorEvent

- [x] **Task 3:** Update EntitySelectorConverter
  - Integrated with SelectorRegistry.TryParse()
  - JsonException on parse failure
  - Serialization via ToString()

- [x] **Task 4:** Implement JsonRule and SceneCommand structures
  - JsonRule with EntitySelector From, JsonNode Where/Do
  - SceneCommand variant with MutCommand
  - JsonScene with optional Rules list

- [x] **Task 5:** Migrate Parent behavior to EntitySelector
  - Parent field uses EntitySelector type
  - EntitySelectorConverter handles deserialization
  - Backward compatible with `"@id"` syntax

- [ ] **Task 6:** Update SceneLoader to validate and store rules
  - ✅ JsonScene already deserializes Rules via System.Text.Json
  - ✅ EntitySelectorConverter already validates `from` selectors during deserialization
  - ❌ **MISSING:** SceneLoader should fire ErrorEvent if Rules contains invalid data
  - ❌ **MISSING:** SceneLoader should store parsed rules (or at least validate they parsed correctly)
  - **Note:** Since this is Stage 1 (parsing only, no execution), storing rules may just be keeping them in memory for validation. No execution infrastructure needed yet.

### Testing Tasks (Not Started)

- [ ] **Task 7:** Create test scene directory structure
  - Create `MonoGame/Content/Scenes/Tests/` directory
  - Create `MonoGame/Content/Scenes/Tests/QueryCommand/` subdirectory
  - Organize test scenes by category (valid, invalid, edge cases)

- [ ] **Task 8:** Create test scene JSON files
  - Create scenes for all example scenarios from requirements:
    - `poison-rule.json` (simple rule with #tag selector)
    - `complex-precedence.json` (union + refinement: `@player, !enter:#enemies:#boss`)
    - `invalid-selector.json` (@@invalid syntax)
    - `no-entities-with-rules.json` (empty entities array + rule)
    - `multiple-rules.json` (2+ rules in same scene)
    - `parent-valid.json` (existing parent behavior: `"@id"`)
    - `parent-invalid.json` (invalid parent: `"@@invalid"`)
    - `entities-only.json` (entities with no rules - should be valid)
  - **All scenes MUST have both `entities` and `rules` arrays declared** (even if empty, per requirements)

- [ ] **Task 9:** Create unit tests for EntitySelector parsing
  - Create `MonoGame/Atomic.Net.MonoGame.Tests/Selectors/` directory
  - Create `MonoGame/Atomic.Net.MonoGame.Tests/Selectors/Units/` subdirectory
  - Create `SelectorParsingUnitTests.cs` with tests for:
    - Single selectors: `@id`, `#tag`, `!enter`, `!exit`
    - Union: `@player, @boss`
    - Refinement chains: `!enter:#enemies`, `#tag1:#tag2`
    - Precedence: `@player, !enter:#enemies:#boss`
    - Invalid syntax: `@@invalid`, `!unknown`, `@`, `#`, empty string
    - Whitespace handling: `@player , @boss`, `!enter : #enemies`
    - Error events for all invalid cases

- [ ] **Task 10:** Create integration tests for rule loading
  - Create `MonoGame/Atomic.Net.MonoGame.Tests/Selectors/Integrations/` subdirectory
  - Create `RuleParsingIntegrationTests.cs` with tests for:
    - Load each test scene file via SceneLoader
    - Assert rules parse correctly (check parsed structure)
    - Assert invalid syntax fires ErrorEvent
    - Assert Parent migration works (valid/invalid cases)
    - Assert scenes without rules load successfully
    - Assert scenes with rules but no entities are valid (per requirements)
    - Assert selectors in rules are validated during scene load

- [ ] **Task 11:** Create negative tests for error handling
  - Test invalid selector syntax fires ErrorEvent, not exception
  - Test missing rule fields (from, where, do)
  - Test malformed JSON in where/do sections
  - Test unknown selector prefixes
  - Test empty selector strings
  - Test scenes missing entities array (should fire ErrorEvent per requirements)

- [ ] **Task 12:** Verify Parent behavior migration
  - Test existing parent syntax still works (`"@id"`)
  - Test invalid parent selectors fire ErrorEvent (`"@@invalid"`, `"#tag"`, etc.)
  - Test parent resolution after scene load
  - Test HierarchyRegistry.Recalc() works with new selectors

### Documentation Tasks

- [ ] **Task 13:** Add XML comments to public APIs
  - Document SelectorRegistry.TryParse
  - Document EntitySelector variant types
  - Document JsonRule structure
  - Document SceneCommand variant

---

## Example Test Scenarios (From Requirements)

These MUST be created as test scene files and tested:

### 1. Poison Rule
```json
{
  "entities": [
    { "id": "goblin", "tags": ["poisoned"], "properties": { "health": 50, "poisonStacks": 3 } }
  ],
  "rules": [
    {
      "from": "#poisoned",
      "where": { ">": [ { "var": "properties.poisonStacks" }, 0 ] },
      "do": { "mut": { "merge": [ { "var": "" }, { "properties": { "health": { "-": [ { "var": "properties.health" }, 1 ] } } } ] } }
    }
  ]
}
```

### 2. Complex Selector Precedence
```json
{
  "entities": [
    { "id": "player", "properties": { "health": 100 } },
    { "id": "boss", "tags": [ "enemies", "boss" ], "properties": { "health": 500 } }
  ],
  "rules": [
    {
      "from": "@player, !enter:#enemies:#boss",
      "where": { "<": [ { "var": "properties.distance" }, 10 ] },
      "do": { "mut": { "merge": [ { "var": "" }, { "properties": { "inCombat": true } } ] } }
    }
  ]
}
```

### 3. Invalid Selector
```json
{
  "entities": [ { "id": "player", "properties": { "health": 100 } } ],
  "rules": [
    {
      "from": "@@player",
      "where": {},
      "do": { "mut": {} }
    }
  ]
}
```

### 4. Scene with Rules but No Entities
```json
{
  "entities": [],
  "rules": [
    {
      "from": "#poisoned",
      "where": { ">": [ { "var": "properties.health" }, 0 ] },
      "do": { "mut": { "merge": [ { "var": "" }, { "properties": { "health": 0 } } ] } }
    }
  ]
}
```

### 5. Multiple Rules in Scene
```json
{
  "entities": [
    { "id": "goblin", "tags": ["poisoned", "burning"], "properties": { "health": 50, "poisonStacks": 2, "burnStacks": 1 } }
  ],
  "rules": [
    {
      "from": "#poisoned",
      "where": { ">": [ { "var": "properties.poisonStacks" }, 0 ] },
      "do": { "mut": { "merge": [ { "var": "" }, { "properties": { "health": { "-": [ { "var": "properties.health" }, { "var": "properties.poisonStacks" } ] } } } ] } }
    },
    {
      "from": "#burning",
      "where": { ">": [ { "var": "properties.burnStacks" }, 0 ] },
      "do": { "mut": { "merge": [ { "var": "" }, { "properties": { "health": { "-": [ { "var": "properties.health" }, { "*": [ { "var": "properties.burnStacks" }, 2 ] } ] } } } ] } }
    }
  ]
}
```

### 6. Parent Behavior Migration (Valid and Invalid)
```json
{
  "entities": [
    { "id": "player", "transform": { "position": [0, 0, 0] } },
    { "id": "weapon", "parent": "@player", "transform": { "position": [1, 0, 0] } }
  ]
}
```

```json
{
  "entities": [
    { "id": "player", "transform": { "position": [0, 0, 0] } },
    { "id": "weapon", "parent": "@@invalid", "transform": { "position": [1, 0, 0] } }
  ]
}
```

---

## Notes for Future Stages

**Stage 1 Deliverables (This Sprint):**
- ✅ Parsing infrastructure complete
- ❌ Comprehensive test coverage (in progress)
- Rules are parsed but not executed (correct for Stage 1)

**Stage 2 (Future):**
- Implement Tags registry (TagEntitySelector.Recalc)
- Implement Collision registry (CollisionEnter/Exit.Recalc)
- Evaluate JsonLogic `where` conditions
- Execute SceneCommand mutations
- Runtime rule evaluation engine

**Stage 3 (Future):**
- Event-driven rule triggers
- Complex set operations
- Performance optimizations
