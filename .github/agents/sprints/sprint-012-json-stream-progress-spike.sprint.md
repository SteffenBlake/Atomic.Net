# Sprint: JSON Stream Progress Spike

## Non-Technical Requirements

- Can we track byte-level progress during JSON scene deserialization for loading bars?
- Need to know if `JsonSerializer.DeserializeAsync()` consumes streams incrementally or buffers entirely up front
- Create a large representative JSON file (e.g., 10+ MB)
- Implement a `ProgressTrackingStream` wrapper that logs every `Read`/`ReadAsync` with timestamp and byte count
- Pass wrapped stream to `JsonSerializer.DeserializeAsync<JsonScene>()`
- Observe: are Read calls made in small chunks during deserialization, or buffered all at once?
- Document findings: can we use this approach for incremental loading progress?
- If not, suggest alternatives (manual parsing with `Utf8JsonReader`, chunk accumulation, track entities spawned, etc.)
- Do all of this in a Unit Test using xunit logging system (no Console.WriteLine)
- Have the writes also write to something we can assert on to confirm it logged status updates
- Unit test class should go in `Atomic.Net.MonoGame.Tests/POCs/Scenes/`

## Success Criteria
- We know with certainty whether byte-level progress can be measured during JSON deserialization
- We have an explicit recommendation for how to track/provide incremental loading bar updates

---

## Technical Requirements

### Architecture

#### 1. Progress Tracking Stream Wrapper
- **File:** `Atomic.Net.MonoGame.Tests/POCs/Scenes/ProgressTrackingStream.cs`
  - Wraps an existing `Stream` (decorator pattern)
  - Tracks cumulative bytes read
  - Logs each `Read()`/`ReadAsync()` call with:
    - Timestamp (relative to start)
    - Bytes read in this call
    - Cumulative bytes read
    - Total stream length (for percentage calculation)
  - Provides list of `ProgressSnapshot` records for assertions
  - `readonly record struct ProgressSnapshot(TimeSpan Timestamp, int BytesRead, long TotalBytesRead, long StreamLength)`

#### 2. Large JSON Scene Generator
- **File:** `Atomic.Net.MonoGame.Tests/POCs/Scenes/LargeSceneGenerator.cs`
  - Static utility class
  - Method: `GenerateLargeSceneJson(int entityCount)` returns JSON string
  - Creates JSON with many entities (each with Transform, EntityId, Parent, Properties)
  - Target size: 10+ MB (approximately 10,000-15,000 entities)
  - Use repetitive but valid scene data structure matching `JsonScene` format

#### 3. POC Unit Test
- **File:** `Atomic.Net.MonoGame.Tests/POCs/Scenes/JsonStreamProgressTests.cs`
  - Unit test class with xunit `ITestOutputHelper` for logging
  - Test: `DeserializeAsync_TracksStreamProgress_IncrementalReads()`
    - Generate large JSON scene (10+ MB)
    - Wrap stream in `ProgressTrackingStream`
    - Call `JsonSerializer.DeserializeAsync<JsonScene>(wrappedStream)`
    - Assert: Multiple progress snapshots were recorded (confirms incremental reads)
    - Assert: Total bytes read equals stream length
    - Log all progress snapshots to xunit output with timestamps, byte counts, percentages
  - Test: `DeserializeAsync_DocumentsReadPattern()`
    - Same as above but focuses on documenting the pattern
    - Calculate: How many Read calls? Average bytes per call? Time distribution?
    - Log recommendation: Can we use this for progress bars?
  - Both tests write findings to xunit output for manual review

### Integration Points

- Uses existing `JsonScene` and `JsonEntity` models from `Atomic.Net.MonoGame.Scenes`
- Uses `System.Text.Json.JsonSerializer.DeserializeAsync<T>()`
- No integration with actual scene loading system (this is a POC/spike)
- Results inform future async scene loading implementation (ROADMAP milestone)

### Performance Considerations

- This is a discovery spike, not production code
- Focus is on observability (how does JsonSerializer read streams?), not optimization
- Large file (10+ MB) ensures we can observe incremental behavior if it exists
- If findings show buffering, we may need to explore:
  - Manual parsing with `Utf8JsonReader` for true incremental reads
  - Tracking entity spawn count instead of byte progress
  - Pre-calculating entity count from JSON before deserialization
  - Chunked loading (parse N entities, yield, repeat)

---

## Tasks

- [ ] Create `ProgressTrackingStream.cs` in POCs/Scenes directory
  - Decorator pattern wrapping Stream
  - Records `ProgressSnapshot` for each Read/ReadAsync call
  - Provides list of snapshots for test assertions
  - Tracks cumulative bytes and timestamps

- [ ] Create `LargeSceneGenerator.cs` utility
  - Static method to generate large valid JSON scene
  - Target 10+ MB size (10k-15k entities)
  - Use realistic entity structure (Transform, EntityId, Parent, Properties)

- [ ] Create `JsonStreamProgressTests.cs` unit test class
  - Test 1: Verify stream progress is tracked during deserialization
  - Test 2: Document read pattern and make recommendation
  - Use xunit ITestOutputHelper for logging (no Console.WriteLine)
  - Assert on progress snapshot count (confirms logging occurred)
  - Log detailed findings: read pattern, byte distribution, timestamps

- [ ] Document findings in test output
  - Does JsonSerializer read incrementally or buffer up front?
  - Recommendation: Can we use byte progress for loading bars?
  - If not: What alternatives should we explore?

- [ ] Run tests and capture output
  - Verify tests pass
  - Review logged findings
  - Determine if byte-level progress tracking is viable
