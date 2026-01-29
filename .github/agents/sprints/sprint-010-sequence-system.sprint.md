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

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/JsonSequence.cs`**
- `JsonSequence` record struct containing:
  - `string Id` - unique identifier for lookup
  - `JsonSequenceStep[] Steps` - array of steps to execute

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/JsonSequenceStep.cs`**
- `JsonSequenceStep` struct variant type (using dotVariant) with cases:
  - `DelayStep` - contains `float Duration`
  - `DoStep` - contains `SceneCommand Do`
  - `TweenStep` - contains `float From`, `float To`, `float Duration`, `SceneCommand Do`
  - `RepeatStep` - contains `float Every`, `JsonNode Until`, `SceneCommand Do`

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/JsonSequenceStepConverter.cs`**
- Custom JSON converter for `JsonSequenceStep` variant
- Deserializes based on presence of discriminator fields (`delay`, `do`, `tween`, `repeat`)
- Similar pattern to `SceneCommandConverter`

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/DelayStep.cs`**
- `readonly record struct DelayStep(float Duration)`

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/DoStep.cs`**
- `readonly record struct DoStep(SceneCommand Do)`

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/TweenStep.cs`**
- `readonly record struct TweenStep(float From, float To, float Duration, SceneCommand Do)`

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/RepeatStep.cs`**
- `readonly record struct RepeatStep(float Every, JsonNode Until, SceneCommand Do)`

#### Registry & State Management

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceRegistry.cs`**
- Manages sequence definitions loaded from JSON
- Pattern identical to `RuleRegistry` and `EntityRegistry`
- Fields:
  - `SparseArray<JsonSequence> _sequences` - capacity `Constants.MaxSequences`
  - `Dictionary<string, ushort> _idToIndex` - fast ID → index lookup (pre-allocated with capacity)
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

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceState.cs`**
- `readonly record struct SequenceState` tracking execution state of a sequence instance
- Fields:
  - `byte StepIndex` - which step is currently executing
  - `float StepElapsedTime` - time elapsed in current step (for delay, tween, repeat)

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceDriver.cs`**
- Singleton that processes all active sequences per-frame
- Called from main game loop after RulesDriver
- Fields:
  - `SparseReferenceArray<SparseArray<SequenceState>> _activeSequences` - outer index: entity, inner index: sequence
  - Pre-allocated at load time with `Constants.MaxEntities` × `Constants.MaxActiveSequencesPerEntity`
- Methods:
  - `void RunFrame(float deltaTime)` - main entry point
  - `void StartSequence(ushort entityIndex, ushort sequenceIndex)` - called by command drivers
  - `void StopSequence(ushort entityIndex, ushort sequenceIndex)` - called by command drivers
  - `void ResetSequence(ushort entityIndex, ushort sequenceIndex)` - called by command drivers
  - Private helper methods for processing different step types
- Responsibilities:
  - Iterate over all active sequences (entity-first, then sequence)
  - Retrieve sequence definition from `SequenceRegistry`
  - Track state per active sequence instance
  - Execute current step based on step type
  - Build context JsonNode for command execution
  - Call appropriate command driver
  - Track time and advance steps
  - Auto-remove from registry when sequence completes (delete entry from `_activeSequences`)
  - Process all sequences for an entity, then apply mutations once via `ApplyToTarget` helper
- Event handlers:
  - `OnEvent(PreEntityDeactivatedEvent)` - removes all sequences for that entity from `_activeSequences`

#### Command Layer (Execution)

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceStartCommand.cs`**
- `readonly record struct SequenceStartCommand(string SequenceId)`
- Added to `SceneCommand` variant

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceStopCommand.cs`**
- `readonly record struct SequenceStopCommand(string SequenceId)`
- Added to `SceneCommand` variant

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceResetCommand.cs`**
- `readonly record struct SequenceResetCommand(string SequenceId)`
- Added to `SceneCommand` variant

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/SceneCommand.cs`**
- Add variant cases: `SequenceStartCommand`, `SequenceStopCommand`, `SequenceResetCommand`

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/SceneCommandConverter.cs`**
- Handle deserialization of `sequence-start`, `sequence-stop`, `sequence-reset` fields

#### Driver Layer (Command Drivers)

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceStartCmdDriver.cs`**
- Executes sequence-start command
- Methods:
  - `void Execute(ushort entityIndex, string sequenceId)`
- Error handling:
  - Push `ErrorEvent` if sequence ID not found
  - Push `ErrorEvent` if entity not active
- Calls `SequenceDriver.StartSequence(entityIndex, sequenceIndex)`

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceStopCmdDriver.cs`**
- Executes sequence-stop command
- Methods:
  - `void Execute(ushort entityIndex, string sequenceId)`
- Error handling:
  - Push `ErrorEvent` if sequence ID not found
  - Ignore no-op cases (stopping non-running sequence)
- Calls `SequenceDriver.StopSequence(entityIndex, sequenceIndex)`

**File: `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceResetCmdDriver.cs`**
- Executes sequence-reset command
- Methods:
  - `void Execute(ushort entityIndex, string sequenceId)`
- Error handling:
  - Push `ErrorEvent` if sequence ID not found
  - Ignore no-op cases (resetting non-running sequence)
- Calls `SequenceDriver.ResetSequence(entityIndex, sequenceIndex)`

**Refactor: `MonoGame/Atomic.Net.MonoGame/Scenes/MutCmdDriver.cs`**
- Extract mutation logic from `RulesDriver` into separate driver class
- Methods:
  - `void Execute(MutCommand command, JsonNode context, ushort entityIndex)`
- Remove tight coupling to RuleContext - accept any JsonNode
- Called by both `RulesDriver` and `SequenceDriver`

**Refactor: Extract `RulesDriver.ApplyToTarget` to Shared Helper**
- Extract `RulesDriver.ApplyToTarget` (and all its child logic) into a shared helper function
- This helper converts mutations from JsonNode into actual entity behavior updates
- Reused by both `RulesDriver` and `SequenceDriver`
- SequenceDriver should run all sequences for an entity on the JsonNode, then call this helper once to apply all mutations

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/RulesDriver.cs`**
- Rename context variable from generic `context` to `ruleContext` for clarity
- Delegate `MutCommand` execution to `MutCmdDriver`
- Handle new sequence commands via appropriate command drivers
- Use extracted `ApplyToTarget` helper

#### Constants & Configuration

**Update: `MonoGame/Atomic.Net.MonoGame/Core/Constants.cs`**
- Add `MaxSequences` - total capacity (default 512)
- Add `MaxGlobalSequences` - partition boundary (default 256)
- Add `MaxActiveSequencesPerEntity` - concurrent sequences per entity (default 8)

#### Scene Loading

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/JsonScene.cs`**
- Add `List<JsonSequence>? Sequences { get; set; }` property

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/SceneLoader.cs`**
- Load sequences from JSON and register in `SequenceRegistry`
- Call `SequenceRegistry.Activate()` or `ActivateGlobal()` based on partition

#### Initialization

**Update: `MonoGame/Atomic.Net.MonoGame/Core/AtomicEngine.cs`** (or equivalent initialization point)
- Initialize `SequenceRegistry`
- Initialize `SequenceDriver`
- Wire `SequenceDriver.RunFrame()` into main game loop

### Integration Points

#### Context Building for Commands
When executing commands within sequence steps, build context as follows.

**IMPORTANT**: The `self` object must include ALL entity behaviors (not just properties). This logic already exists in `RulesDriver` for building entity JsonNodes and should be extracted to a helper function for reuse.

**For `do` steps (SequenceDoContext):**
```json
{
  "world": { "deltaTime": 0.016667 },
  "self": { 
    "_index": 42, 
    "properties": {...},
    "transform": {...},
    "tags": [...],
    "id": "...",
    "flexWidth": ...,
    "parent": "..."
  }
}
```

**For `tween` steps (SequenceTweenContext):**
```json
{
  "tween": 0.73,
  "self": { 
    "_index": 42, 
    "properties": {...},
    "transform": {...},
    "tags": [...],
    "id": "...",
    "flexWidth": ...,
    "parent": "..."
  }
}
```

**For `repeat` steps (SequenceRepeatContext):**
```json
{
  "elapsed": 2.35,
  "self": { 
    "_index": 42, 
    "properties": {...},
    "transform": {...},
    "tags": [...],
    "id": "...",
    "flexWidth": ...,
    "parent": "..."
  }
}
```

Context is built dynamically per-frame by `SequenceDriver` using entity index and current step state.

**Note on Repeat Steps**: Calculate next trigger time using `StepElapsedTime % interval` - no need for separate `RepeatNextTrigger` field.

#### Event Handling
- `PreEntityDeactivatedEvent` → `SequenceDriver` removes all sequences for that entity
- `ResetEvent` → `SequenceRegistry` clears scene sequences
- `ShutdownEvent` → `SequenceRegistry` clears all sequences

#### Command Execution Flow
1. `RulesDriver` evaluates rule → `sequence-start: "id"` command
2. `RulesDriver` → `SequenceStartCmdDriver.Execute(entityIndex, "id")`
3. `SequenceStartCmdDriver` → `SequenceRegistry.TryResolveById("id")` → gets index
4. `SequenceStartCmdDriver` → `SequenceDriver.StartSequence(entityIndex, sequenceIndex)`
5. Next frame: `SequenceDriver.RunFrame()` processes active sequence
6. `SequenceDriver` → builds context → `MutCmdDriver.Execute(command, context, entityIndex)`
7. `SequenceDriver` → after all sequences for entity complete, calls `ApplyToTarget` helper once
8. On sequence completion: `SequenceDriver` → removes entry from `_activeSequences`

### Performance Considerations

#### Allocation Hotspots
- **Context JsonNode creation**: Built per-frame for each active sequence step. Reuse entity JsonNode across multiple sequences for same entity.
- **String ID lookups**: `SequenceRegistry._idToIndex` uses string keys. Use `StringComparer.OrdinalIgnoreCase` for dictionary. Normalize IDs at load time.

#### Data Structure Optimization
- Store sequences as `SparseReferenceArray<SparseArray<SequenceState>>` with entity index first, sequence index second
- This allows easy cleanup when entity deactivates (clear entire entity's sequences)
- Allows reusing entity JsonNode across all sequences for that entity
- Enables batched mutation application (run all sequences, apply once)

#### Command Driver Separation
- Extracting `MutCmdDriver` enables reuse without duplication
- Extracting `ApplyToTarget` helper prevents duplication between RulesDriver and SequenceDriver
- Prevents inline allocation patterns in multiple places

---

## Tasks

### Phase 1: Data Structures & Registry
- [ ] Create JsonSequence, JsonSequenceStep variant and related step structs (DelayStep, DoStep, TweenStep, RepeatStep) in `Sequencing/` folder
- [ ] Create JsonSequenceStepConverter for deserialization
- [ ] Create SequenceRegistry with ID↔index lookup and global/scene partitioning
- [ ] Create SequenceState struct for tracking runtime state (byte StepIndex, float StepElapsedTime)
- [ ] Update Constants.cs with MaxSequences (512), MaxGlobalSequences (256), MaxActiveSequencesPerEntity (8)
- [ ] Update JsonScene to include Sequences list

### Phase 2: Command Layer
- [ ] Create SequenceStartCommand, SequenceStopCommand, SequenceResetCommand structs in `Sequencing/` folder
- [ ] Update SceneCommand variant to include new sequence commands
- [ ] Update SceneCommandConverter to deserialize sequence-start/stop/reset fields
- [ ] Extract MutCmdDriver from RulesDriver with generic JsonNode context parameter
- [ ] Extract ApplyToTarget helper from RulesDriver for reuse
- [ ] Create SequenceStartCmdDriver, SequenceStopCmdDriver, SequenceResetCmdDriver in `Sequencing/` folder
- [ ] Update RulesDriver to use MutCmdDriver, rename context to ruleContext, and use extracted ApplyToTarget helper

### Phase 3: Driver Implementation
- [ ] Create SequenceDriver with `SparseReferenceArray<SparseArray<SequenceState>> _activeSequences` field
- [ ] Implement StartSequence, StopSequence, ResetSequence methods
- [ ] Implement RunFrame() with entity-first iteration (process all sequences per entity, then apply mutations once)
- [ ] Implement step processing logic (delay, do, tween, repeat) - use modulo for repeat timing
- [ ] Implement PreEntityDeactivatedEvent handler to cleanup sequences
- [ ] Wire SequenceDriver.RunFrame into main game loop (after RulesDriver)

### Phase 4: Scene Loading & Initialization
- [ ] Update SceneLoader to load sequences from JSON and register in SequenceRegistry
- [ ] Initialize SequenceRegistry and SequenceDriver in AtomicEngine

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
- [ ] Write tests for sequence chaining (sequence-start command within a sequence step)

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
- **Sequence completion**: Entry is deleted from `_activeSequences` (no IsComplete flag needed)
- **Sequence chaining**: A `do` step can contain `sequence-start` command to trigger another sequence

### Alignment with Existing Patterns
- Use `ISingleton<T>` for registries and drivers
- Use `IEventHandler<T>` for lifecycle events
- Follow `RuleRegistry` partition pattern exactly
- Use `dotVariant` for step types (like `SceneCommand`)
- Use `SparseArray` for entity-indexed data
- No allocations in `RunFrame()` after initialization

### Batched Mutation Application
- Process all sequences for an entity on the entity's JsonNode
- After all sequences for that entity complete, call `ApplyToTarget` helper once
- This minimizes repeated writes to entity behaviors
- Example: 8 parallel sequences on 1 entity = 1 `ApplyToTarget` call, not 8

### Repeat Step Timing
- Use `StepElapsedTime % interval` to determine when to execute next iteration
- No need for separate `RepeatNextTrigger` field in SequenceState
- Simpler state management and calculation

### Future Extensibility
- Additional step types (e.g., `parallel`, `random`) can be added to `JsonSequenceStep` variant
- Easing functions for tweens (linear only for now)
