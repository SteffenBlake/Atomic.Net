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
- Delete old selector code after migration
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

#### 1. EntitySelector Parser (EntitySelector.cs replacement)
**Purpose:** Parse selector strings from JSON into validated AST

**Responsibility:**
- Replace existing `EntitySelector` struct with new implementation
- Delete `EntitySelectorV2` scaffolding and merge into single `EntitySelector` type
- Parse selector syntax: `@id`, `#tag`, `!enter`, `!exit`
- Handle operator precedence: `,` (union), `:` (refinement/chaining)
- Return parsed AST structure (using dotVariant discriminated unions)

**Key Design Decisions:**
- Use `EntitySelector.TryParse(ReadOnlySpan<char>, out EntitySelector?)` as entry point
- Use existing variant types: `UnionEntitySelector`, `IdEntitySelector`, `TaggedEntitySelector`, `CollisionEnterEntitySelector`, `CollisionExitEntitySelector`
- Parsing allocates at load time (acceptable), produces zero-allocation AST for runtime matching
- Parse errors return `false` and fire `ErrorEvent` (never throw)
- Use span slicing for efficient parsing without string allocations

**Data Structures:**
- `EntitySelector` (dotVariant union of selector types)
- `UnionEntitySelector` - holds `EntitySelector[]` for `,` operator
- `IdEntitySelector` - holds `string Id` + optional `Next` chain
- `TaggedEntitySelector` - holds `string Tag` + optional `Next` chain  
- `CollisionEnterEntitySelector` - optional `Next` chain (stub, not functional yet)
- `CollisionExitEntitySelector` - optional `Next` chain (stub, not functional yet)

**Precedence Rules:**
- `:` binds tighter than `,` (refinement chains before union)
- Examples:
  - `@player, @boss` → `Union([@player, @boss])`
  - `!enter:#enemies` → `CollisionEnter(Next: Tagged("enemies"))`
  - `@player, !enter:#enemies:#boss` → `Union([@player, CollisionEnter(Next: Tagged("enemies", Next: Tagged("boss")))])`

#### 2. EntitySelectorConverter (EntitySelectorConverter.cs update)
**Purpose:** JSON deserialization integration

**Responsibility:**
- Call `EntitySelector.TryParse()` when reading JSON strings
- Fire `ErrorEvent` if parsing fails
- Write selector strings back to JSON (for serialization)

**Key Design Decisions:**
- Minimal changes - replace hardcoded `@` logic with `TryParse` call
- Keep existing `JsonConverter<EntitySelector>` pattern
- Error handling fires `ErrorEvent` and returns default selector

#### 3. JsonRule Integration (JsonRule.cs - no changes needed)
**Purpose:** Container for parsed rules

**Current State:** Already defined as `record struct JsonRule(EntitySelector From, JsonNode Where, SceneCommand Do)`

**Validation:**
- Ensure `From` is parsed via new `EntitySelector.TryParse`
- `Where` and `Do` remain as raw `JsonNode` (future work: validate against JsonLogic)

#### 4. JsonScene Loading (SceneLoader.cs update)
**Purpose:** Load rules from JSON and validate structure

**Responsibility:**
- Parse `rules` array from JSON (if present, nullable)
- Validate each rule's `from` field parses successfully
- Store parsed rules in scene (no execution, just storage)
- Fire `ErrorEvent` for any parsing failures (continue loading other rules)

**Key Design Decisions:**
- Rules are optional (`JsonScene.Rules` is nullable `List<JsonRule>?`)
- All scenes MUST have `entities` array (even if empty) - validation check
- Scene without `rules` is valid (sets `Rules = null` or empty list)
- Invalid rule syntax fires error, continues loading remaining rules

#### 5. Migration: Parent Behavior (JsonEntity.cs update)
**Purpose:** Migrate Parent field to use new EntitySelector parser

**Responsibility:**
- Replace hardcoded `@` prefix logic with `EntitySelector.TryParse`
- Ensure backward compatibility - `"@player"` still works
- Fire `ErrorEvent` for invalid parent selectors (e.g., `"@@invalid"`)
- Keep existing `Parent.TryLocate()` behavior for runtime resolution

**Key Design Decisions:**
- Parent field already typed as `EntitySelector?` - no schema change
- EntitySelectorConverter already handles deserialization - just needs updated parser
- Migration is transparent to existing scene files (syntax unchanged)
- Invalid selectors fire error, entity spawns without parent (same as unresolved reference)

### Integration Points

#### Event System
- **ErrorEvent:** Fired for all parsing failures (invalid syntax, unknown operators, etc.)
- **Existing events:** No new events, uses current event bus pattern

#### Registries (NO CHANGES)
- **EntityIdRegistry:** Used by `IdEntitySelector.Matches()` - already exists
- **TagsRegistry:** Stubbed in `TaggedEntitySelector.Matches()` - throws NotImplementedException (future work)
- **CollisionRegistry:** Stubbed in `CollisionEnter/Exit` - throws NotImplementedException (future work)

#### JsonLogic Library
- **Integration Point:** JsonRule's `Where` and `Do` fields are JsonNode
- **Current State:** JsonLogic package already referenced in csproj
- **Action Needed:** None for Stage 1 (parsing only, no evaluation)

### Performance Considerations

#### Allocation Strategy
- **Load Time (ACCEPTABLE):** 
  - String parsing allocates variant objects
  - EntitySelector[] arrays for unions
  - JsonNode parsing (System.Text.Json)
- **Runtime (ZERO ALLOCATIONS):**
  - Parsed AST is readonly structs
  - `Matches()` walks AST without allocating
  - Span-based parsing avoids temporary strings

#### Parser Approach
- **Recommended:** Recursive descent parser with span slicing
- **Avoid:** Regex (allocates), string.Split (allocates), LINQ (allocates)
- **Use:** ReadOnlySpan<char> slicing, manual tokenization
- **Benchmark Request:** @benchmarker - If parser shows as bottleneck during load, test alternative approaches (but premature optimization unlikely needed per DISCOVERIES.md)

#### Cache-Friendly Concerns
- Parsed rules stored in List<JsonRule> (reference type acceptable for load-time data)
- Selector matching uses variant pattern (good branch prediction)
- No SIMD opportunities (string parsing is inherently sequential)

---

## Tasks

- [ ] **Task 1:** Implement EntitySelector.TryParse() - recursive descent parser for selector syntax
  - Parse single selectors: `@id`, `#tag`, `!enter`, `!exit`
  - Parse operators: `,` (union), `:` (refinement chain)
  - Handle precedence: `:` binds tighter than `,`
  - Use ReadOnlySpan<char> slicing for zero-copy parsing
  - Return false + fire ErrorEvent on invalid syntax

- [ ] **Task 2:** Delete EntitySelectorV2 scaffolding and merge into EntitySelector
  - Remove `EntitySelectorV2` class and all variant types from EntitySelector.cs
  - Promote variants to top-level (UnionEntitySelector, IdEntitySelector, etc.)
  - Update EntitySelector to be the dotVariant union type
  - Remove old ById field and TryLocate() method
  - Ensure Matches() method works with all variant types

- [ ] **Task 3:** Update EntitySelectorConverter to use TryParse
  - Replace hardcoded `@` logic with EntitySelector.TryParse() call
  - Fire ErrorEvent if TryParse returns false
  - Update Write() to serialize selectors back to string format
  - Test with existing parent references (`"@player"` syntax)

- [ ] **Task 4:** Update SceneLoader to parse rules array
  - Add logic to deserialize `rules` field from JsonScene (nullable)
  - Validate each rule's `from` field parses successfully
  - Validate all scenes have `entities` array (error if missing)
  - Fire ErrorEvent for invalid rules, continue loading others
  - Store parsed rules (no execution, just in-memory storage for future stages)

- [ ] **Task 5:** Migrate Parent behavior to use new parser
  - Update JsonEntity.WriteToEntity to handle new EntitySelector format
  - Ensure Parent resolution still works via EntitySelector.Matches or TryLocate-equivalent
  - Test backward compatibility with existing parent syntax (`"@id"`)
  - Fire ErrorEvent for invalid parent selectors (e.g., `"@@invalid"`, `"#tag"`)

- [ ] **Task 6:** Create test scene files in Content/Scenes/Tests/QueryCommand/
  - poison-rule.json (simple rule with #tag selector)
  - complex-precedence.json (union + refinement: `@player, !enter:#enemies:#boss`)
  - invalid-selector.json (@@invalid syntax)
  - no-entities.json (empty entities array + rule)
  - multiple-rules.json (2+ rules in same scene)
  - parent-valid.json (existing parent behavior: `"@id"`)
  - parent-invalid.json (invalid parent: `"@@invalid"`)

- [ ] **Task 7:** Write integration tests for rule parsing
  - Test in Atomic.Net.MonoGame.Tests/Scenes/Integrations/QueryCommandParsingTests.cs
  - Load each test scene file via SceneLoader
  - Assert rules parse correctly (check selector AST structure)
  - Assert invalid syntax fires ErrorEvent
  - Assert Parent migration works (valid/invalid cases)
  - Assert scenes without rules load successfully
  - Assert scenes without entities fire ErrorEvent

- [ ] **Task 8:** Write unit tests for EntitySelector.TryParse
  - Test in Atomic.Net.MonoGame.Tests/Scenes/Units/EntitySelectorParsingTests.cs
  - Test single selectors: `@id`, `#tag`, `!enter`, `!exit`
  - Test union: `@player, @boss`
  - Test refinement chains: `!enter:#enemies`, `#tag1:#tag2`
  - Test precedence: `@player, !enter:#enemies:#boss`
  - Test invalid syntax: `@@invalid`, `!unknown`, `@`, `#`, empty string
  - Test whitespace handling: `@player , @boss`, `!enter : #enemies`

- [ ] **Task 9:** Delete old EntitySelector code after migration verified
  - Remove `ById` field from EntitySelector (if still present)
  - Remove old `TryLocate()` method (if still present)
  - Remove old hardcoded `@` logic from EntitySelectorConverter
  - Run full test suite to ensure no regressions
  - Verify all Parent/child relationships from existing tests still pass
