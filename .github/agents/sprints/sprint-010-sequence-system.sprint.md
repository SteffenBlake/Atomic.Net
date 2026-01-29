# Sprint: Sequence System - Data-driven Multi-step Animation & Effects

## Non-Technical Requirements

- Sequences must be fully specified via scene JSON (never coded in C#)
- Each sequence is a list of step objects (delay, tween, repeat, do)
- Each sequence has a string 'id' (for referencing)
- All memory allocations for sequences must occur at scene load time
- Step Types:
  - **Delay** — pauses execution for N seconds, then advances to next step
  - **Do** — executes a SceneCommand immediately with deltaTime context, advances to next step on next frame
  - **Tween** — interpolates from 'from' to 'to' over a duration, running sub-commands per-frame with interpolated value
  - **Repeat-until** — runs sub-command every interval until condition met, exposes 'elapsed' time
- Sequences are triggered via new rule commands: `sequence-start: "id"`, `sequence-stop: "id"`, `sequence-reset: "id"`
- Rules/events reference sequences by string ID, not index
- Sequences lock onto entity index when started; entity selector only matters at start
- Sequences auto-remove from active registry when complete
- Entity deactivation immediately terminates all running sequences for that entity
- Commands nested within sequence steps receive minimal context (see step types above)
- `do` steps receive: `world.deltaTime`, `self` (SequenceDoContext)
- `tween` steps receive: `tween` (interpolated value), `self` (SequenceTweenContext)
- `repeat` steps receive: `elapsed` (seconds since step started), `self` (SequenceRepeatContext)
- Step context does NOT include global entity lists
- Any SceneCommand type is legal inside 'do' of a step (not restricted to mut)
- MutCommand should be refactored to accept generic JsonNode context
- Sequence registry must allow global vs scene sequences
- Must support dual lookup (ID ↔ index)
- No pointer references, indices only
- Zero allocations after scene load

## Technical Requirements

### Architecture

#### Data Structures (JSON Layer)

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/JsonSequence.cs`**
- `JsonSequence` record struct containing:
  - `string Id` - unique identifier for lookup
  - `JsonSequenceStep[] Steps` - array of steps to execute

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/JsonSequenceStep.cs`**
- `JsonSequenceStep` variant type (using dotVariant) with cases:
  - `DelayStep` - contains `float Duration`
  - `DoStep` - contains `SceneCommand Do`
  - `TweenStep` - contains `float From`, `float To`, `float Duration`, `SceneCommand Do`
  - `RepeatStep` - contains `float Every`, `JsonNode Until`, `SceneCommand Do`

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/JsonSequenceStepConverter.cs`**
- Custom JSON converter for `JsonSequenceStep` variant
- Deserializes based on presence of discriminator fields (`delay`, `do`, `tween`, `repeat`)
- Similar pattern to `SceneCommandConverter`

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/DelayStep.cs`**
- `readonly record struct DelayStep(float Duration)`

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/DoStep.cs`**
- `readonly record struct DoStep(SceneCommand Do)`

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/TweenStep.cs`**
- `readonly record struct TweenStep(float From, float To, float Duration, SceneCommand Do)`

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/RepeatStep.cs`**
- `readonly record struct RepeatStep(float Every, JsonNode Until, SceneCommand Do)`

#### Registry & State Management

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/SequenceRegistry.cs`**
- Manages sequence definitions loaded from JSON
- Pattern identical to `RuleRegistry` and `EntityRegistry`
- Fields:
  - `SparseArray<JsonSequence> _sequences` - capacity `Constants.MaxSequences`
  - `Dictionary<string, ushort> _idToIndex` - fast ID → index lookup
  - `ushort _nextSceneSequenceIndex` - starts at `Constants.MaxGlobalSequences`
  - `ushort _nextGlobalSequenceIndex` - starts at 0
- Methods:
  - `ushort Activate(JsonSequence sequence)` - allocates scene partition
  - `ushort ActivateGlobal(JsonSequence sequence)` - allocates global partition
  - `bool TryResolveByIndex(ushort index, out JsonSequence sequence)`
  - `bool TryResolveById(string id, out ushort index)`
- Event handlers:
  - `OnEvent(ResetEvent)` - clears scene partition only
  - `OnEvent(ShutdownEvent)` - clears all partitions
- Enforce unique IDs within each partition

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/ActiveSequenceRegistry.cs`**
- Tracks actively running sequences per-entity
- Fields:
  - `SparseArray<SparseArray<ushort>> _activeSequences` - outer: entity index, inner: sequence indices
  - Pre-allocated at load time with `Constants.MaxEntities` × `Constants.MaxActiveSequencesPerEntity`
- Methods:
  - `void StartSequence(ushort entityIndex, ushort sequenceIndex)`
  - `void StopSequence(ushort entityIndex, ushort sequenceIndex)`
  - `void ResetSequence(ushort entityIndex, ushort sequenceIndex)`
  - `void StopAllForEntity(ushort entityIndex)` - called on entity deactivation
  - `IEnumerable<(ushort entityIndex, ushort sequenceIndex)> GetActiveSequences()`
- Event handlers:
  - `OnEvent(PreEntityDeactivatedEvent)` - stops all sequences for that entity

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/SequenceStateRegistry.cs`**
- Tracks runtime state per active sequence instance
- Uses composite key: `(ushort entityIndex, ushort sequenceIndex)`
- Fields:
  - `Dictionary<(ushort, ushort), SequenceState> _states` - pre-allocated with capacity
- Methods:
  - `SequenceState GetOrCreate((ushort entityIndex, ushort sequenceIndex) key)`
  - `void Remove((ushort entityIndex, ushort sequenceIndex) key)`
  - `void Clear()` - for reset/shutdown
  
**File: `MonoGame/Atomic.Net.MonoGame/Scenes/SequenceState.cs`**
- Mutable class tracking execution state of a sequence instance
- Fields:
  - `ushort CurrentStepIndex` - which step is currently executing
  - `float StepElapsedTime` - time elapsed in current step (for delay, tween, repeat)
  - `float RepeatNextTrigger` - for repeat steps, when to next execute
  - `bool IsComplete` - whether sequence has finished all steps
- No allocations during gameplay - reused when sequences restart

#### Command Layer (Execution)

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/SequenceStartCommand.cs`**
- `readonly record struct SequenceStartCommand(string SequenceId)`
- Added to `SceneCommand` variant

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/SequenceStopCommand.cs`**
- `readonly record struct SequenceStopCommand(string SequenceId)`
- Added to `SceneCommand` variant

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/SequenceResetCommand.cs`**
- `readonly record struct SequenceResetCommand(string SequenceId)`
- Added to `SceneCommand` variant

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/SceneCommand.cs`**
- Add variant cases: `SequenceStartCommand`, `SequenceStopCommand`, `SequenceResetCommand`

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/SceneCommandConverter.cs`**
- Handle deserialization of `sequence-start`, `sequence-stop`, `sequence-reset` fields

#### Driver Layer (Orchestration)

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/SequenceDriver.cs`**
- Singleton that processes all active sequences per-frame
- Called from main game loop after RulesDriver
- Methods:
  - `void RunFrame(float deltaTime)` - main entry point
  - `void ProcessSequence(ushort entityIndex, ushort sequenceIndex, float deltaTime)` - private
  - `void ProcessDelayStep(...)` - private
  - `void ProcessDoStep(...)` - private
  - `void ProcessTweenStep(...)` - private
  - `void ProcessRepeatStep(...)` - private
  - `void AdvanceToNextStep(...)` - private
  - `void CompleteSequence(...)` - private, removes from active registry
- Responsibilities:
  - Iterate over `ActiveSequenceRegistry.GetActiveSequences()`
  - Retrieve sequence definition from `SequenceRegistry`
  - Retrieve/update state from `SequenceStateRegistry`
  - Execute current step based on step type
  - Build context JsonNode for command execution
  - Call appropriate command driver
  - Track time and advance steps
  - Auto-complete when all steps done

**File: `MonoGame/Atomic.Net.MonoGame/Scenes/SequenceCommandDriver.cs`**
- Executes sequence-start/stop/reset commands
- Methods:
  - `void ExecuteStart(ushort entityIndex, string sequenceId)`
  - `void ExecuteStop(ushort entityIndex, string sequenceId)`
  - `void ExecuteReset(ushort entityIndex, string sequenceId)`
- Error handling:
  - Push `ErrorEvent` if sequence ID not found
  - Push `ErrorEvent` if entity not active
  - Ignore no-op cases (e.g., stopping non-running sequence)

**Refactor: `MonoGame/Atomic.Net.MonoGame/Scenes/MutCommandDriver.cs`**
- Extract mutation logic from `RulesDriver` into separate driver class
- Methods:
  - `void Execute(MutCommand command, JsonNode context, ushort entityIndex)`
- Remove tight coupling to RuleContext - accept any JsonNode
- Called by both `RulesDriver` and `SequenceDriver`

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/RulesDriver.cs`**
- Rename context variable from generic `context` to `ruleContext` for clarity
- Delegate `MutCommand` execution to `MutCommandDriver`
- Handle new sequence commands via `SequenceCommandDriver`

#### Constants & Configuration

**Update: `MonoGame/Atomic.Net.MonoGame/Core/Constants.cs`**
- Add `MaxSequences` - total capacity (default 512)
- Add `MaxGlobalSequences` - partition boundary (default 64)
- Add `MaxActiveSequencesPerEntity` - concurrent sequences per entity (default 8)

#### Scene Loading

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/JsonScene.cs`**
- Add `List<JsonSequence>? Sequences { get; set; }` property

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/SceneLoader.cs`**
- Load sequences from JSON and register in `SequenceRegistry`
- Call `SequenceRegistry.Activate()` or `ActivateGlobal()` based on partition hint (future: use scene metadata)

#### Initialization

**Update: `MonoGame/Atomic.Net.MonoGame/Core/AtomicEngine.cs`** (or equivalent initialization point)
- Initialize `SequenceRegistry`
- Initialize `ActiveSequenceRegistry`
- Initialize `SequenceStateRegistry`
- Initialize `SequenceDriver`
- Wire `SequenceDriver.RunFrame()` into main game loop

### Integration Points

#### Context Building for Commands
When executing commands within sequence steps, build context as follows:

**For `do` steps (SequenceDoContext):**
```json
{
  "world": { "deltaTime": 0.016667 },
  "self": { "_index": 42, "properties": {...} }
}
```

**For `tween` steps (SequenceTweenContext):**
```json
{
  "tween": 0.73,
  "self": { "_index": 42, "properties": {...} }
}
```

**For `repeat` steps (SequenceRepeatContext):**
```json
{
  "elapsed": 2.35,
  "self": { "_index": 42, "properties": {...} }
}
```

Context is built dynamically per-frame by `SequenceDriver` using entity index and current step state.

#### Event Handling
- `PreEntityDeactivatedEvent` → `ActiveSequenceRegistry.StopAllForEntity(entity.Index)`
- `ResetEvent` → `SequenceRegistry` clears scene sequences, `ActiveSequenceRegistry` clears scene entities
- `ShutdownEvent` → All registries clear all data

#### Command Execution Flow
1. `RulesDriver` evaluates rule → `sequence-start: "id"` command
2. `RulesDriver` → `SequenceCommandDriver.ExecuteStart(entityIndex, "id")`
3. `SequenceCommandDriver` → `SequenceRegistry.TryResolveById("id")` → gets index
4. `SequenceCommandDriver` → `ActiveSequenceRegistry.StartSequence(entityIndex, sequenceIndex)`
5. Next frame: `SequenceDriver.RunFrame()` processes active sequence
6. `SequenceDriver` → builds context → `MutCommandDriver.Execute(command, context, entityIndex)`
7. On completion: `SequenceDriver` → `ActiveSequenceRegistry` removes entry

### Performance Considerations

#### Allocation Hotspots
- **Context JsonNode creation**: Built per-frame for each active sequence step. Use pooled JsonObject instances or pre-allocated structure.
  - **@benchmarker**: Test if pooling JsonObject instances provides benefit vs. allocation per-frame for small objects
- **Dictionary lookups**: `SequenceStateRegistry` uses `Dictionary<(ushort, ushort), SequenceState>`. Tuple key may cause boxing.
  - **@benchmarker**: Compare `Dictionary<(ushort, ushort), T>` vs `Dictionary<uint, T>` with manual key packing `(entityIndex << 16) | sequenceIndex`
- **String ID lookups**: `SequenceRegistry._idToIndex` uses string keys. Case-insensitive lookups may allocate.
  - Use `StringComparer.OrdinalIgnoreCase` for dictionary. Normalize IDs at load time.

#### SIMD/Cache-Friendly Concerns
- Active sequences stored as `SparseArray<SparseArray<ushort>>` - iteration is cache-friendly
- `SequenceStateRegistry` uses Dictionary - not cache-friendly, but small working set expected (max ~64 active sequences typical)
- Consider: If `MaxActiveSequencesPerEntity` is small (e.g., 8), could use `SparseArray<SequenceState>` with packed key instead of Dictionary
  - **@benchmarker**: Test Dictionary vs SparseArray for 10-100 active sequences (realistic game scenario)

#### Lazy Evaluation
- Delay steps: simple timer, no command execution
- Tween steps: linear interpolation, no complex math
- Repeat steps: condition evaluation via JsonLogic (existing pattern, allocates minimally)

#### Command Driver Separation
- Extracting `MutCommandDriver` enables reuse without duplication
- Prevents inline allocation patterns in multiple places

---

## Tasks

### Phase 1: Data Structures & Registry
- [ ] Create JsonSequence, JsonSequenceStep variant and related step structs (DelayStep, DoStep, TweenStep, RepeatStep)
- [ ] Create JsonSequenceStepConverter for deserialization
- [ ] Create SequenceRegistry with ID↔index lookup and global/scene partitioning
- [ ] Create ActiveSequenceRegistry to track running sequences per entity
- [ ] Create SequenceStateRegistry and SequenceState for runtime tracking
- [ ] Update Constants.cs with MaxSequences, MaxGlobalSequences, MaxActiveSequencesPerEntity
- [ ] Update JsonScene to include Sequences list

### Phase 2: Command Layer
- [ ] Create SequenceStartCommand, SequenceStopCommand, SequenceResetCommand structs
- [ ] Update SceneCommand variant to include new sequence commands
- [ ] Update SceneCommandConverter to deserialize sequence-start/stop/reset fields
- [ ] Extract MutCommandDriver from RulesDriver with generic JsonNode context parameter
- [ ] Create SequenceCommandDriver for executing sequence lifecycle commands
- [ ] Update RulesDriver to use MutCommandDriver and rename context to ruleContext

### Phase 3: Driver Implementation
- [ ] Create SequenceDriver.RunFrame() skeleton with active sequence iteration
- [ ] Implement ProcessDelayStep with timer logic
- [ ] Implement ProcessDoStep with SequenceDoContext building and command execution
- [ ] Implement ProcessTweenStep with interpolation and SequenceTweenContext building
- [ ] Implement ProcessRepeatStep with interval timer and SequenceRepeatContext building
- [ ] Implement AdvanceToNextStep and CompleteSequence with cleanup
- [ ] Wire SequenceDriver.RunFrame into main game loop (after RulesDriver)

### Phase 4: Scene Loading & Initialization
- [ ] Update SceneLoader to load sequences from JSON and register in SequenceRegistry
- [ ] Initialize all sequence registries and driver in AtomicEngine (or equivalent)
- [ ] Register ActiveSequenceRegistry for PreEntityDeactivatedEvent to cleanup sequences

### Phase 5: Testing & Validation
- [ ] Write integration tests for delay steps (pause and advance)
- [ ] Write integration tests for do steps (command execution with deltaTime context)
- [ ] Write integration tests for tween steps (interpolation and command execution)
- [ ] Write integration tests for repeat steps (interval execution until condition)
- [ ] Write integration tests for sequence start/stop/reset commands from rules
- [ ] Write integration tests for entity deactivation canceling sequences
- [ ] Write integration tests for sequence completion and auto-removal
- [ ] Write negative tests for invalid sequence IDs and malformed steps
- [ ] Write tests for concurrent sequences on same entity
- [ ] Write tests for sequence restart after completion

### Phase 6: Performance Validation
- [ ] @benchmarker Compare JsonObject pooling vs allocation for context building (10-100 active sequences)
- [ ] @benchmarker Compare Dictionary<(ushort,ushort)> vs Dictionary<uint> with packed keys for SequenceStateRegistry
- [ ] @benchmarker Test Dictionary vs SparseArray for active sequence tracking (10-100 instances)
- [ ] Profile full sequence pipeline (start → delay → tween → repeat → complete) for allocation hotspots

---

## Notes for Implementation

### Error Handling
- All errors should push `ErrorEvent` with descriptive messages
- Invalid sequence IDs → "Sequence with ID '{id}' not found"
- Malformed steps (e.g., missing duration) → caught at deserialization, push error during scene load
- Entity not active when starting sequence → "Cannot start sequence on inactive entity {index}"

### Edge Cases
- **Empty sequences**: Complete immediately on start
- **Zero-duration steps**: Execute and advance same frame
- **Repeat with indeterminate duration**: Track per-entity step index, not global
- **Sequence restart after completion**: Reset state, re-add to active registry
- **Tag/selector change after start**: Sequence continues (locked to entity index)

### Alignment with Existing Patterns
- Use `ISingleton<T>` for registries and drivers
- Use `IEventHandler<T>` for lifecycle events
- Follow `RuleRegistry` partition pattern exactly
- Use `dotVariant` for step types (like `SceneCommand`)
- Use `SparseArray` for entity-indexed data
- No allocations in `RunFrame()` after initialization

### Future Extensibility
- Additional step types (e.g., `parallel`, `random`) can be added to `JsonSequenceStep` variant
- Easing functions for tweens (linear only for now)
- Sequence chaining (one sequence triggers another on completion)
