# Sprint: Global Partition Naming Audit (formerly Persistent)

## Non-Technical Requirements

- Audit all code, documentation, constants, and comments related to the global partition for clarity of intent and correct naming.
- Rename all instances of "loading partition" and related terminology to clearly express their purpose (e.g., "global partition", "global entities", `GlobalPartitionSize`, etc.).
- Ensure all references (e.g., function names, constants, doc comments, API surface) are updated for maximum clarity and consistent domain language.
- **CRITICAL**: Distinguish "global" partition (entities surviving scene transitions) from database "persistence" (PersistToDiskBehavior, Persistence namespace).
- If architectural changes or technical scope expansion are found (e.g., functionality gaps), document them as additional requirements in your output.

### Success Criteria

- No remaining "loading" or "global partition" terminology in code, docs, or API surface.
- Clear distinction between "global" entities (survive ResetEvent) and database-persisted entities (PersistToDiskBehavior).
- All project contributors (design, code, docs) can intuitively understand the partition's role and usage.
- Any additional functional/architectural gaps discovered during the audit are captured and scheduled for future sprints.

---

## Technical Requirements

### Architecture

#### Current State Analysis

The entity registry uses a two-partition system:
- **Partition 0 (indices 0–255)**: Currently called "loading partition" — entities that persist across scene loads
- **Partition 1 (indices 256+)**: Called "scene partition" — entities that are cleared on `ResetEvent`

The "loading partition" name is misleading because:
1. It doesn't clearly communicate persistence behavior
2. It conflates the concept of "loading screen UI" with "persistent game state"
3. The ROADMAP.md explicitly mentions this needs to hold inventory, XP, and other persistent data — not just loading UI

#### Proposed Naming Convention

**Partition Terminology:**
- "loading partition" → **"global partition"**
- "loading entities" → **"global entities"**
- "scene partition" → **"scene partition"** (no change, already clear)
- "scene entities" → **"scene entities"** (no change, already clear)

**Constant Naming:**
- `MaxGlobalEntities` → **`MaxGlobalEntities`**
- Preprocessor: `MAX_LOADING_ENTITIES` → **`MAX_GLOBAL_ENTITIES`**

**Method Naming:**
- `ActivateLoading()` → **`ActivateGlobal()`**
- `GetLoadingRoot()` → **`GetGlobalRoot()`**
- `LoadLoadingScene()` → **`LoadGlobalScene()`**

**Variable Naming:**
- `_nextLoadingIndex` → **`_nextGlobalIndex`**
- `useLoadingPartition` → **`useGlobalPartition`**
- `loadingPath`, `loadingEntity`, etc. → **`globalPath`, `globalEntity`**, etc.

**File Naming:**
- Test fixture: `single-loading-entity.json` → **`single-global-entity.json`**
- Default scene path: `"Content/Scenes/loading.json"` → **`"Content/Scenes/global.json"`** (or keep as `loading.json` if it's for loading screen UI specifically)

**Comment & Documentation Updates:**
- All XML doc comments mentioning "loading entities/partition"
- All inline comments explaining partition behavior
- ROADMAP.md milestone description
- Sprint file references
- Test comments

### Integration Points

No integration changes required — this is a pure naming refactor. The partition behavior remains identical:
- Global partition entities survive `ResetEvent` (scene transitions)
- Scene partition entities are cleared on `ResetEvent`
- `ShutdownEvent` clears both partitions (full cleanup)

### Performance Considerations

**Zero performance impact** — this is a naming-only change:
- No logic changes
- No behavior changes
- No allocation changes
- Same constants, same indices, same algorithms

**Testing impact:**
- All existing tests should pass after mechanical rename
- Test names/comments will be clearer

---

## Tasks

### Code Changes (Core Library)

- [ ] Rename `Constants.MaxGlobalEntities` to `Constants.MaxGlobalEntities` in `Atomic.Net.MonoGame.Core/Constants.cs`
- [ ] Update preprocessor symbol from `MAX_LOADING_ENTITIES` to `MAX_GLOBAL_ENTITIES` in `Constants.cs`
- [ ] Update all XML doc comments in `Constants.cs` to use "persistent" terminology
- [ ] Rename `EntityRegistry.ActivateLoading()` to `EntityRegistry.ActivateGlobal()` in `EntityRegistry.cs`
- [ ] Rename `EntityRegistry.GetLoadingRoot()` to `EntityRegistry.GetGlobalRoot()` in `EntityRegistry.cs`
- [ ] Rename `_nextLoadingIndex` to `_nextGlobalIndex` in `EntityRegistry.cs`
- [ ] Update all XML doc comments in `EntityRegistry.cs` to use "persistent" terminology
- [ ] Update inline comments in `EntityRegistry.cs` mentioning partition behavior
- [ ] Update `ShutdownEvent.cs` doc comment from "both loading and scene" to "both persistent and scene"

### Code Changes (Scene Loading)

- [ ] Rename `SceneLoader.LoadLoadingScene()` to `SceneLoader.LoadGlobalScene()` in `SceneLoader.cs`
- [ ] Rename `useLoadingPartition` parameter to `useGlobalPartition` in `SceneLoader.cs`
- [ ] Update XML doc comments in `SceneLoader.cs` to use "persistent" terminology
- [ ] Update default scene path parameter from `"Content/Scenes/loading.json"` to `"Content/Scenes/global.json"` (if applicable)
- [ ] Update inline comments in `SceneLoader.cs` explaining partition selection

### Test Changes

- [ ] Rename test method `LoadLoadingScene_AllocatesLoadingEntity` to `LoadGlobalScene_AllocatesPersistentEntity` in `EntityRegistryIntegrationTests.cs`
- [ ] Rename test method `LoadLoadingScene_AllocatesSequentialIndices` to `LoadGlobalScene_AllocatesSequentialIndices` in `EntityRegistryIntegrationTests.cs`
- [ ] Rename test method `GetLoadingRoot_ReturnsFirstLoadingEntity` to `GetGlobalRoot_ReturnsFirstPersistentEntity` in `EntityRegistryIntegrationTests.cs`
- [ ] Rename test method `ResetEvent_DeactivatesOnlySceneEntities` variables `loadingPath`, `loadingEntity` to `globalPath`, `globalEntity`
- [ ] Update all test comments in `EntityRegistryIntegrationTests.cs` to use "persistent" terminology
- [ ] Rename test method `LoadLoadingScene_WithBasicEntities_SpawnsEntitiesInLoadingPartition` to `LoadGlobalScene_WithBasicEntities_SpawnsEntitiesInPersistentPartition` in `SceneLoaderIntegrationTests.cs`
- [ ] Update test comments in `SceneLoaderIntegrationTests.cs` to use "persistent" terminology
- [ ] Rename test method `ResetEvent_PreservesLoadingEntityHierarchy` to `ResetEvent_PreservesPersistentEntityHierarchy` in `HierarchyRegistryIntegrationTests.cs`
- [ ] Update variable names `loadingParent`, `loadingChild` to `persistentParent`, `persistentChild` in `HierarchyRegistryIntegrationTests.cs`
- [ ] Update all test cleanup comments from "both loading and scene" to "both persistent and scene"
- [ ] Rename test fixture file `single-loading-entity.json` to `single-global-entity.json`
- [ ] Update JSON fixture content: `"id": "loading-entity"` to `"id": "persistent-entity"`
- [ ] Update all test file references from `single-loading-entity.json` to `single-global-entity.json`

### Documentation Changes

- [ ] Update ROADMAP.md milestone description from "Loading Scene partition" to "Global partition"
- [ ] Update sprint-001-json-scene-loading.sprint.md references from "loading scene" to "persistent scene" where appropriate
- [ ] Update sprint-001-json-scene-loading.sprint.md method names and descriptions
- [ ] Update sprint-002-property-bag.sprint.md test comments if any mention "loading entities"
- [ ] Update benchmark comments in `TransformBenchmark.cs` to use "persistent" terminology

### Verification

- [ ] Build solution and verify no compilation errors
- [ ] Run full test suite and verify all tests pass
- [ ] Search codebase for remaining "loading" references that should be "persistent"
- [ ] Verify XML doc comments are consistent and clear
- [ ] Review ROADMAP.md alignment with new terminology

---

## Architectural Gaps Discovered

### Gap 1: Persistent Scene Default Path

**Current behavior:** `LoadGlobalScene()` defaults to `"Content/Scenes/loading.json"`

**Gap:** The filename `loading.json` still implies loading screen UI, not persistent game state.

**Recommendation for future sprint:**
- Consider separating "loading screen UI scene" from "persistent game state scene"
- Option A: Rename to `global.json` (breaking change for existing projects)
- Option B: Support both `loading.json` (deprecated) and `global.json` (preferred)
- Option C: Allow multiple persistent scenes (inventory, player state, UI state) loaded independently

**Priority:** Low — can be deferred to M2 when scene system is expanded

### Gap 2: No Explicit Persistent/Scene Distinction in JSON

**Current behavior:** Designer must remember to call `LoadGlobalScene()` vs `LoadGameScene()` to control partition

**Gap:** No JSON-level marker to indicate "this scene should be persistent" vs "this scene should be cleared on reset"

**Recommendation for future sprint:**
- Add optional `"partition": "persistent"` or `"partition": "scene"` to JSON scene files
- `SceneLoader.Load(path)` automatically routes to correct partition
- Reduces API surface (one Load method instead of two)

**Priority:** Medium — consider for M2 scene system refactor

### Gap 3: Partition Size Configuration

**Current behavior:** `MaxGlobalEntities` is hardcoded to 256 (or compile-time override)

**Gap:** No runtime warning when global partition is nearing capacity

**Recommendation for future sprint:**
- Add optional warning event when global partition >80% full
- Helps designers catch over-allocation early

**Priority:** Low — nice-to-have for debugging

### Gap 4: Documentation of Partition Use Cases

**Current behavior:** No formal documentation of when to use global vs scene partition

**Gap:** Designers may not understand best practices for partition usage

**Recommendation for future sprint:**
- Add design guidelines document explaining:
  - Persistent partition: player state, inventory, quest progress, global UI
  - Scene partition: level geometry, NPCs, interactable objects
  - Rule of thumb: "Would this survive a scene transition?" → global

**Priority:** Medium — helps onboard new designers

---

## Notes for Implementation

### Design Decisions

- **Terminology choice:** "Persistent" clearly communicates cross-scene survival behavior
- **Mechanical rename:** Low-risk change with high clarity benefit
- **No behavior changes:** Identical semantics, just better names
- **Test coverage:** All existing tests validate behavior remains unchanged

### Risks and Mitigations

- **Risk:** Breaking change for external code using `ActivateLoading()` API
  - **Mitigation:** This is internal engine code (not public library), acceptable breaking change
- **Risk:** Missed references during rename (stale "loading" terminology)
  - **Mitigation:** Comprehensive grep/search before finalizing, code review will catch stragglers
- **Risk:** Test fixture file rename might break running tests
  - **Mitigation:** Update file references in same commit as rename

### Out of Scope

- Changing partition indices or sizes (0-255 remains persistent, 256+ remains scene)
- Changing partition behavior (persistent still survives ResetEvent)
- Adding new partition types (e.g., "global", "level", "temporary")
- Runtime partition configuration
- JSON-level partition hints

---

## Example: Before and After

### Before (Current State)

```csharp
// Constants.cs
public const ushort MaxGlobalEntities = 256;

// EntityRegistry.cs
public Entity ActivateLoading() { ... }
public Entity GetLoadingRoot() => _entities[0];

// SceneLoader.cs
public void LoadLoadingScene(string scenePath = "Content/Scenes/loading.json") { ... }
```

### After (Proposed State)

```csharp
// Constants.cs
public const ushort MaxGlobalEntities = 256;

// EntityRegistry.cs
public Entity ActivateGlobal() { ... }
public Entity GetGlobalRoot() => _entities[0];

// SceneLoader.cs
public void LoadGlobalScene(string scenePath = "Content/Scenes/global.json") { ... }
```

### Test Before

```csharp
[Fact]
public void LoadLoadingScene_AllocatesLoadingEntity()
{
    var scenePath = "Core/Fixtures/single-loading-entity.json";
    SceneLoader.Instance.LoadLoadingScene(scenePath);
    Assert.True(EntityIdRegistry.Instance.TryResolve("loading-entity", out var entity));
    Assert.True(entity.Value.Index < Constants.MaxGlobalEntities);
}
```

### Test After

```csharp
[Fact]
public void LoadGlobalScene_AllocatesPersistentEntity()
{
    var scenePath = "Core/Fixtures/single-global-entity.json";
    SceneLoader.Instance.LoadGlobalScene(scenePath);
    Assert.True(EntityIdRegistry.Instance.TryResolve("persistent-entity", out var entity));
    Assert.True(entity.Value.Index < Constants.MaxGlobalEntities);
}
```

---

## Success Metrics

- **Code clarity:** Contributor survey confirms "persistent" is clearer than "loading"
- **Zero regressions:** All existing tests pass after rename
- **Complete coverage:** No remaining "loading" terminology for partition (verified via grep)
- **Documentation alignment:** ROADMAP, sprint files, and comments use consistent terminology
