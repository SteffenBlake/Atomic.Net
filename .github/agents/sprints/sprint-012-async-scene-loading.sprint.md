# Sprint: Async Scene Loading and SceneManager with Progress Tracking

## Non-Technical Requirements

### SceneManager Service
- SceneManager provides a single service to trigger scene transitions with a scene file identifier (path or id).

### Scene Transition Process (all steps REQUIRED for compliance):
1. SceneManager sets `IsLoading` to `true`.
2. Starts a background async thread/task:
    - Unloads current scene (clears scene partition only).
    - Opens and reads the scene JSON file from disk.
    - Runs json deserialization and creates new entities/rules/sequences in the scene partition.
    - Throughout loading, reports progress using four values: `BytesRead`, `TotalBytes`, `EntitiesLoaded`, `TotalEntities`.
        - During file read/deserialization, `EntitiesLoaded`/`TotalEntities` are zero or one.
        - After JSON is fully parsed, these switch to actual values as entities spawn.
    - All progress counter updates MUST use the Interlocked class (not volatile).
    - Progress calculation is:
      ```
      LoadingProgress = 0.66 * (BytesRead / TotalBytes) + 0.33 * (EntitiesLoaded / TotalEntities)
      ```
3. When background thread/task finishes loading and spawning:
    - Request the main/game thread run a forced GC (ensure all allocations are collected before gameplay resumes).
4. Only after the GC, SceneManager sets `IsLoading` to `false` (transition is now fully complete and visible).

### Exposing Progress/State to the Engine
- SequenceDriver and RulesDriver MUST, on every frame, read `SceneManager.IsLoading` and `SceneManager.LoadingProgress`.
- These values become `world.loading` (bool) and `world.sceneLoadProgress` (float, 0..1) for use in game logic.
- When no loading is underway, `IsLoading` is `false` and `LoadingProgress` is `1.0`.
- All scene transition visuals and logic should be triggered via rules/sequences by checking `world.loading`/`sceneLoadProgress` (never engine hardcoded; data-driven only).

### Test Strategy / Success Criteria
- Integration tests must:
    1. Use three scenes: `global`, `scene1`, `scene2`. Global scene has rules to trigger loading of another scene. After load, verify correct entities/rules/sequences are in the scene partition.
    2. Include global scene rule that increments a counter during loading. During async load of a very large scene file, verify at least two or more increments occur while scene is still loading and before main thread resumes normal operation (proves non-blocking).
    3. Include tests for scene file not found or corrupt file; SceneLoader must gracefully report error and IsLoading returns to false.
- All requirements above are mandatory for feature completion. No regressions or skipped tests allowed.

---

## Technical Requirements

### Architecture

#### 1. SceneManager Component

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/SceneManager.cs` (NEW)

**Responsibilities:**
- Singleton pattern using `ISingleton<T>`
- Exposes public API: `void LoadScene(string scenePath)`
- Manages async scene transition workflow
- Exposes loading state: `bool IsLoading`, `float LoadingProgress` (0.0 to 1.0)
- Thread-safe progress tracking using `Interlocked` class
- Coordinates with main thread for GC execution

**Key Design Decisions:**
- **State tracking:** Use `Interlocked` for all progress counters (BytesRead, TotalBytes, EntitiesLoaded, TotalEntities)
- **Async pattern:** Use `Task.Run()` to offload JSON parsing and entity spawning to background thread
- **Main thread coordination:** Use flag-based pattern for GC request (main thread polls and executes GC when ready)
- **Progress formula:** `0.66 * (BytesRead/TotalBytes) + 0.33 * (EntitiesLoaded/TotalEntities)`
- **Default state:** When not loading, `IsLoading = false`, `LoadingProgress = 1.0`
- **Error handling:** On error, set `IsLoading = false`, push `ErrorEvent`, return progress to 1.0

**Fields:**
```csharp
// Thread-safe state flags
private int _isLoading = 0; // 0 = false, 1 = true (use Interlocked)
private int _gcRequested = 0; // 0 = false, 1 = true (use Interlocked)

// Progress counters (use Interlocked for updates)
private long _bytesRead = 0;
private long _totalBytes = 0;
private int _entitiesLoaded = 0;
private int _totalEntities = 0;

// Public read-only properties
public bool IsLoading => Interlocked.CompareExchange(ref _isLoading, 0, 0) == 1;
public float LoadingProgress { get; private set; } = 1.0f;
```

**Data Flow:**
1. `LoadScene(scenePath)` called (from rule command)
2. Set `IsLoading = true` via `Interlocked.Exchange(ref _isLoading, 1)`
3. Reset progress counters to zero
4. Launch `Task.Run()` with async scene loading logic:
   a. Clear scene partition (EntityRegistry, RuleRegistry, SequenceRegistry)
   b. Open file, get file size → set `_totalBytes` via `Interlocked.Exchange`
   c. Read file in chunks, updating `_bytesRead` via `Interlocked.Add` after each chunk
   d. Deserialize JSON (JsonSerializer.Deserialize<JsonScene>)
   e. Set `_totalEntities` from scene.Entities.Count via `Interlocked.Exchange`
   f. For each entity, spawn and apply behaviors, increment `_entitiesLoaded` via `Interlocked.Increment`
   g. Load rules and sequences
   h. Recalc selectors and hierarchy
   i. Request GC via `Interlocked.Exchange(ref _gcRequested, 1)`
5. Main thread polls `_gcRequested` each frame in `Update()` method:
   - If `_gcRequested == 1`, execute GC and set `IsLoading = false`
6. Background task completes

**Progress Calculation (main thread, each frame):**
```csharp
// Read thread-safe counters (no Interlocked needed for reads on x64, but use for clarity)
var bytesRead = Interlocked.Read(ref _bytesRead);
var totalBytes = Interlocked.Read(ref _totalBytes);
var entitiesLoaded = Interlocked.CompareExchange(ref _entitiesLoaded, 0, 0);
var totalEntities = Interlocked.CompareExchange(ref _totalEntities, 0, 0);

// Calculate progress
var fileProgress = totalBytes > 0 ? (float)bytesRead / totalBytes : 0.0f;
var entityProgress = totalEntities > 0 ? (float)entitiesLoaded / totalEntities : 0.0f;
LoadingProgress = 0.66f * fileProgress + 0.33f * entityProgress;
```

**Error Handling:**
- File not found: Push `ErrorEvent`, set `IsLoading = false`, `LoadingProgress = 1.0`
- JSON parse error: Push `ErrorEvent`, set `IsLoading = false`, `LoadingProgress = 1.0`
- Exception during entity spawning: Push `ErrorEvent`, set `IsLoading = false`, `LoadingProgress = 1.0`

#### 2. SceneLoadCommand

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/SceneLoadCommand.cs` (NEW)

**Purpose:**
- New SceneCommand variant to trigger scene loading from rules/sequences
- Contains scene path string

**Definition:**
```csharp
public readonly record struct SceneLoadCommand(string ScenePath);
```

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/SceneCommand.cs`**
- Add variant case: `SceneLoadCommand`

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/SceneCommandConverter.cs`**
- Handle deserialization of `sceneLoad` field: `{ "sceneLoad": "Content/Scenes/level1.json" }`

#### 3. SceneLoadCmdDriver

**File:** `MonoGame/Atomic.Net.MonoGame/Scenes/SceneLoadCmdDriver.cs` (NEW)

**Responsibilities:**
- Executes `sceneLoad` command by calling `SceneManager.LoadScene(scenePath)`
- No entity context needed (scene loading is global operation)

**Methods:**
```csharp
public void Execute(SceneLoadCommand command)
{
    SceneManager.Instance.LoadScene(command.ScenePath);
}
```

**Integration:**
- Called from `RulesDriver` when processing `SceneLoadCommand` variant
- Called from `SequenceDriver` when processing `SceneLoadCommand` in `do` steps

#### 4. World Context Updates

**Update: `MonoGame/Atomic.Net.MonoGame/Scenes/RulesDriver.cs`**
- Add `world.loading` and `world.sceneLoadProgress` to context JsonObject
- Read from `SceneManager.Instance` each frame

**Context Structure:**
```json
{
  "world": {
    "deltaTime": 0.016667,
    "loading": false,
    "sceneLoadProgress": 1.0
  },
  "entities": [...],
  "index": -1
}
```

**Update: `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceDriver.cs`**
- Add `world.loading` and `world.sceneLoadProgress` to context JsonObject
- Read from `SceneManager.Instance` each frame

**Context Structure (for `do` steps):**
```json
{
  "world": {
    "deltaTime": 0.016667,
    "loading": false,
    "sceneLoadProgress": 1.0
  },
  "self": {...}
}
```

**Context Structure (for `tween` steps):**
```json
{
  "tween": 0.73,
  "world": {
    "deltaTime": 0.016667,
    "loading": false,
    "sceneLoadProgress": 1.0
  },
  "self": {...}
}
```

**Context Structure (for `repeat` steps):**
```json
{
  "elapsed": 2.35,
  "world": {
    "deltaTime": 0.016667,
    "loading": false,
    "sceneLoadProgress": 1.0
  },
  "self": {...}
}
```

#### 5. Async Scene Loading Implementation

**Technical Approach:**
The existing `SceneLoader` performs all work synchronously. To support async loading with progress tracking, we need:

1. **Chunk-based file reading:** Use `FileStream` with buffer to read file in chunks (e.g., 8KB chunks)
   - Update `_bytesRead` after each chunk via `Interlocked.Add`
   - This provides granular progress during file I/O

2. **Entity spawning on background thread:**
   - DISCOVERY: Benchmark shows entity spawning takes ~63% of load time
   - Must marshal entity spawning work to background thread
   - **CRITICAL CONSTRAINT:** Many Unity/MonoGame APIs are main-thread-only
   - **SOLUTION:** JSON parsing and deserialization can happen on background thread
   - Entity creation (EntityRegistry.Activate) must happen on background thread as well
   - Behavior application (SetBehavior calls) can happen on background thread
   - **RISK:** If MonoGame textures/assets are loaded during entity spawning, this will fail
   - **MITIGATION:** For this sprint, assume entities are data-only (no texture loading)
   - Future work: If texture loading is needed, defer it to main thread after background work completes

3. **Progress tracking pattern:**
   ```csharp
   // In background thread
   var fileStream = new FileStream(scenePath, FileMode.Open, FileAccess.Read);
   var totalBytes = fileStream.Length;
   Interlocked.Exchange(ref _totalBytes, totalBytes);
   
   var buffer = new byte[8192]; // 8KB chunks
   var bytesRead = 0L;
   int readCount;
   using var memoryStream = new MemoryStream();
   while ((readCount = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
   {
       memoryStream.Write(buffer, 0, readCount);
       bytesRead += readCount;
       Interlocked.Exchange(ref _bytesRead, bytesRead);
   }
   
   // Parse JSON
   var jsonText = Encoding.UTF8.GetString(memoryStream.ToArray());
   var scene = JsonSerializer.Deserialize<JsonScene>(jsonText, _serializerOptions);
   
   // Set total entities
   Interlocked.Exchange(ref _totalEntities, scene.Entities.Count);
   
   // Spawn entities
   var entitiesLoaded = 0;
   foreach (var jsonEntity in scene.Entities)
   {
       var entity = EntityRegistry.Instance.Activate();
       jsonEntity.WriteToEntity(entity);
       Interlocked.Increment(ref _entitiesLoaded);
   }
   
   // Load rules and sequences
   // ... (existing SceneLoader logic)
   
   // Recalc selectors and hierarchy
   SelectorRegistry.Instance.Recalc();
   HierarchyRegistry.Instance.Recalc();
   
   // Request GC
   Interlocked.Exchange(ref _gcRequested, 1);
   ```

4. **Main thread GC execution:**
   ```csharp
   // In SceneManager.Update() (called each frame)
   public void Update()
   {
       // Update progress
       UpdateProgress();
       
       // Check for GC request
       if (Interlocked.CompareExchange(ref _gcRequested, 0, 0) == 1)
       {
           // Execute GC on main thread
           GC.Collect();
           GC.WaitForPendingFinalizers();
           GC.Collect(); // Second pass to collect finalized objects
           
           // Clear GC request flag
           Interlocked.Exchange(ref _gcRequested, 0);
           
           // Set loading to false (transition complete)
           Interlocked.Exchange(ref _isLoading, 0);
           
           // Reset progress to 1.0
           LoadingProgress = 1.0f;
       }
   }
   
   private void UpdateProgress()
   {
       if (!IsLoading)
       {
           LoadingProgress = 1.0f;
           return;
       }
       
       // Calculate progress from thread-safe counters
       var bytesRead = Interlocked.Read(ref _bytesRead);
       var totalBytes = Interlocked.Read(ref _totalBytes);
       var entitiesLoaded = Interlocked.CompareExchange(ref _entitiesLoaded, 0, 0);
       var totalEntities = Interlocked.CompareExchange(ref _totalEntities, 0, 0);
       
       var fileProgress = totalBytes > 0 ? (float)bytesRead / totalBytes : 0.0f;
       var entityProgress = totalEntities > 0 ? (float)entitiesLoaded / totalEntities : 0.0f;
       LoadingProgress = 0.66f * fileProgress + 0.33f * entityProgress;
   }
   ```

#### 6. Scene Partition Clearing

**Existing Pattern:**
- `EntityRegistry` responds to `ResetEvent` to clear scene partition
- `RuleRegistry` responds to `ResetEvent` to clear scene partition
- `SequenceRegistry` responds to `ResetEvent` to clear scene partition

**New Requirement:**
- SceneManager must clear scene partition before starting background load
- **SOLUTION:** SceneManager calls `EventBus<ResetEvent>.Push(new())` on background thread
- **RISK:** EventBus may not be thread-safe
- **MITIGATION:** Review EventBus implementation for thread safety; if not safe, marshal ResetEvent to main thread
- **ALTERNATIVE:** SceneManager directly calls registry clear methods (breaks encapsulation, not recommended)

**Recommended Approach:**
- Assume EventBus is thread-safe (uses ConcurrentQueue or similar)
- If tests reveal thread safety issues, address in EventBus implementation (out of scope for this sprint)

#### 7. Initialization

**Update: `MonoGame/Atomic.Net.MonoGame/AtomicSystem.cs`**
- Initialize `SceneManager` singleton

**Integration into Game Loop:**
- Main game loop must call `SceneManager.Instance.Update()` each frame
- **NOTE:** This sprint does NOT implement game loop integration (out of scope)
- Integration tests will call `Update()` manually in test code

### Integration Points

#### Event Handling
- `ResetEvent` → clears scene partition (EntityRegistry, RuleRegistry, SequenceRegistry)
- `ErrorEvent` → pushed on file not found, parse error, or load failure

#### Command Execution Flow
1. Rule evaluates → `{ "sceneLoad": "Content/Scenes/level1.json" }` command
2. `RulesDriver` → `SceneLoadCmdDriver.Execute(command)`
3. `SceneLoadCmdDriver` → `SceneManager.LoadScene(scenePath)`
4. `SceneManager` → starts background task, sets `IsLoading = true`
5. Background task → loads scene, updates progress counters
6. Main thread (each frame) → reads `SceneManager.IsLoading` and `LoadingProgress`
7. Main thread → `RulesDriver` and `SequenceDriver` expose `world.loading` and `world.sceneLoadProgress`
8. Global scene rules → check `world.loading` to show loading UI, update progress bar
9. Background task completes → requests GC
10. Main thread → executes GC, sets `IsLoading = false`

#### Driver Integration
- `RulesDriver.RunFrame()` must read `SceneManager.Instance.IsLoading` and `LoadingProgress` each frame
- Add to `_ruleContext["world"]` as `loading` (bool) and `sceneLoadProgress` (float)
- `SequenceDriver` must read same values and add to all step contexts

### Performance Considerations

#### Allocation Hotspots
- **File reading:** Use `FileStream` with fixed buffer (pre-allocated 8KB byte array)
- **JSON parsing:** Existing `JsonSerializer.Deserialize` already optimized (see DISCOVERIES.md)
- **Entity spawning:** No additional allocations beyond existing SceneLoader pattern
- **Progress tracking:** Use `Interlocked` operations (lock-free, zero allocations)

#### Thread Safety
- **CRITICAL:** All progress counters use `Interlocked` class for thread-safe updates
- No `volatile` keyword (not recommended for multi-threaded scenarios)
- `Interlocked.Exchange`, `Interlocked.Add`, `Interlocked.Increment`, `Interlocked.Read`

#### Async Performance
- **Benefit:** ~37% of load time (JSON parsing) can run on background thread (see DISCOVERIES.md)
- **Limitation:** ~63% of load time (entity spawning) must run on main thread due to MonoGame constraints
- **Result:** Main thread gains ~19ms per 1000 entities during file I/O phase
- **Trade-off:** Adds async complexity for moderate benefit; justified by smooth loading UX

#### GC Strategy
- **Forced GC after load:** Prevents GC pauses during gameplay
- **Double-collect pattern:** `GC.Collect() → GC.WaitForPendingFinalizers() → GC.Collect()`
- **Timing:** Only after all scene loading completes, before `IsLoading = false`

### Test Strategy

#### Integration Tests

**File:** `MonoGame/Atomic.Net.MonoGame.Tests/Scenes/Integrations/SceneManagerIntegrationTests.cs` (NEW)

**Test Cases:**
1. **LoadScene_WithValidPath_LoadsSceneInBackground**
   - Arrange: Create scene file with 1000 entities
   - Act: Call `SceneManager.LoadScene(scenePath)`
   - Assert: `IsLoading = true`, `LoadingProgress < 1.0` during load
   - Poll `Update()` in loop, verify progress increases
   - Eventually `IsLoading = false`, `LoadingProgress = 1.0`
   - Verify entities loaded correctly

2. **LoadScene_DuringLoad_GlobalRuleExecutes**
   - Arrange: Global scene with rule that increments counter property each frame
   - Act: Load very large scene (5000+ entities) asynchronously
   - In test loop: Call `RulesDriver.RunFrame()` and `SceneManager.Update()` each iteration
   - Assert: Counter increments at least 2-3 times before load completes (proves non-blocking)

3. **LoadScene_WithFileNotFound_ReturnsToIdleState**
   - Arrange: Invalid scene path
   - Act: Call `SceneManager.LoadScene("invalid.json")`
   - Assert: `ErrorEvent` pushed, `IsLoading = false`, `LoadingProgress = 1.0`

4. **LoadScene_WithCorruptJSON_ReturnsToIdleState**
   - Arrange: Create malformed JSON file
   - Act: Call `SceneManager.LoadScene(corruptPath)`
   - Assert: `ErrorEvent` pushed, `IsLoading = false`, `LoadingProgress = 1.0`

5. **LoadScene_ProgressTracking_AccurateValues**
   - Arrange: Scene file with known byte size and entity count
   - Act: Load scene, capture progress at multiple points
   - Assert: Progress follows formula `0.66 * fileProgress + 0.33 * entityProgress`

6. **LoadScene_ClearsScenePartition_BeforeLoad**
   - Arrange: Load scene1 with entities/rules/sequences
   - Act: Load scene2
   - Assert: Only scene2 entities/rules/sequences exist in scene partition
   - Assert: Global partition unchanged

7. **WorldContext_ExposesLoadingState_ToRules**
   - Arrange: Global rule that reads `world.loading` and `world.sceneLoadProgress`
   - Act: Trigger scene load, execute rules each frame
   - Assert: Rule correctly reads loading state and progress values

**Test Fixtures:**
- `Scenes/Fixtures/large-scene-5k.json` - 5000 entities for async load testing
- `Scenes/Fixtures/scene1.json` - Basic scene with entities/rules
- `Scenes/Fixtures/scene2.json` - Different scene for transition testing
- `Scenes/Fixtures/corrupt-scene.json` - Malformed JSON for error testing
- `Scenes/Fixtures/global-loading-scene.json` - Global scene with loading UI rule

#### Negative Tests
- Loading non-existent file
- Loading file with invalid JSON
- Loading file with missing required fields
- Concurrent `LoadScene` calls (second call should queue or error)

---

## Tasks

### Phase 1: SceneManager Core
- [ ] Create `SceneManager` singleton with thread-safe state tracking (Interlocked counters)
- [ ] Implement `LoadScene(string scenePath)` with async background task
- [ ] Implement `Update()` method for progress calculation and GC execution
- [ ] Add `IsLoading` and `LoadingProgress` public properties
- [ ] Handle errors (file not found, parse error) with ErrorEvent and state reset

### Phase 2: Async Scene Loading Logic
- [ ] Implement chunk-based file reading with progress tracking
- [ ] Implement entity spawning on background thread with progress tracking
- [ ] Implement GC request mechanism (background → main thread coordination)
- [ ] Ensure scene partition clearing before load (ResetEvent)

### Phase 3: Command Integration
- [ ] Create `SceneLoadCommand` struct
- [ ] Update `SceneCommand` variant to include `SceneLoadCommand`
- [ ] Update `SceneCommandConverter` to deserialize `sceneLoad` field
- [ ] Create `SceneLoadCmdDriver` to execute scene load command
- [ ] Update `RulesDriver` to handle `SceneLoadCommand` variant
- [ ] Update `SequenceDriver` to handle `SceneLoadCommand` in `do` steps

### Phase 4: World Context Updates
- [ ] Update `RulesDriver._ruleContext` to include `world.loading` and `world.sceneLoadProgress`
- [ ] Update `SequenceDriver._doContext` to include `world.loading` and `world.sceneLoadProgress`
- [ ] Update `SequenceDriver._tweenContext` to include `world` object with loading state
- [ ] Update `SequenceDriver._repeatContext` to include `world` object with loading state

### Phase 5: Initialization
- [ ] Update `AtomicSystem.Initialize()` to initialize `SceneManager`

### Phase 6: Testing
- [ ] Create `SceneManagerIntegrationTests.cs` with full test suite
- [ ] Create test fixtures: large-scene-5k.json, scene1.json, scene2.json, corrupt-scene.json, global-loading-scene.json
- [ ] Write test: LoadScene_WithValidPath_LoadsSceneInBackground
- [ ] Write test: LoadScene_DuringLoad_GlobalRuleExecutes (proves non-blocking)
- [ ] Write test: LoadScene_WithFileNotFound_ReturnsToIdleState
- [ ] Write test: LoadScene_WithCorruptJSON_ReturnsToIdleState
- [ ] Write test: LoadScene_ProgressTracking_AccurateValues
- [ ] Write test: LoadScene_ClearsScenePartition_BeforeLoad
- [ ] Write test: WorldContext_ExposesLoadingState_ToRules

---

## Notes for Implementation

### Error Handling
- All errors should push `ErrorEvent` with descriptive messages
- After error, immediately set `IsLoading = false`, `LoadingProgress = 1.0`
- File not found → "Scene file not found: '{scenePath}'"
- JSON parse error → "Failed to parse scene JSON: {scenePath} - {exception.Message}"
- Entity spawn error → "Failed to spawn entity during scene load: {exception.Message}"

### Edge Cases
- **Concurrent LoadScene calls:** First load still in progress, second call made
  - **Solution:** Check `IsLoading` flag; if true, push `ErrorEvent` and return
  - Alternative: Queue second load (out of scope for this sprint)
- **LoadScene on empty/zero-entity scene:** Handle divide-by-zero in progress calculation
  - **Solution:** Check `totalEntities > 0` before dividing
- **LoadScene canceled mid-load:** User exits game or triggers new load
  - **Solution:** Out of scope for this sprint; future work (cancellation tokens)

### Thread Safety Considerations
- **EventBus:** Assume thread-safe (uses concurrent collections internally)
- **EntityRegistry:** Review for thread safety in `Activate()` method
  - **Risk:** SparseArray may not be thread-safe for concurrent writes
  - **Mitigation:** If issues found, marshal entity spawning to main thread (fallback approach)
- **Progress counters:** Use `Interlocked` for all reads and writes

### Alignment with Existing Patterns
- Use `ISingleton<T>` for `SceneManager`
- Use `Task.Run()` for background work (no async/await in main API for simplicity)
- Follow `SceneCommand` variant pattern for `SceneLoadCommand`
- Follow existing command driver pattern for `SceneLoadCmdDriver`
- Reuse `EventBus<ErrorEvent>` for error reporting

### Performance Expectations
- Loading a 1000-entity scene should block main thread for <35ms (down from ~52ms)
- Main thread should be able to render frames and execute rules during file I/O phase
- Progress should update smoothly (not jump from 0 to 1.0 instantly)
- At least 2-3 rule executions should occur during load of large scene (5000+ entities)

### Future Extensibility
- **Cancellation support:** Add `CancellationToken` parameter to `LoadScene()` (out of scope)
- **Scene queuing:** Queue multiple scene loads (out of scope)
- **Streaming entity spawning:** Spawn entities incrementally over multiple frames (out of scope)
- **Texture loading:** Defer texture/asset loading to main thread (out of scope; assume data-only entities for now)
- **Progress callbacks:** Fire `SceneLoadProgressEvent` for UI updates (out of scope; use world context instead)

### CRITICAL: Thread Safety Verification
Before finalizing implementation, verify that:
1. `EntityRegistry.Activate()` is thread-safe (or can be made so)
2. `EventBus<T>.Push()` is thread-safe (or can be made so)
3. If either is not thread-safe, fallback to main-thread-only loading with chunked entity spawning

### GC Strategy Rationale
- **Why force GC:** Scene loading allocates JSON parsing buffers, intermediate objects
- **When to GC:** After all scene work completes, before returning to gameplay
- **How to GC:** Main thread only (GC.Collect is not thread-safe in all contexts)
- **Pattern:** Double-collect to ensure finalized objects are collected
