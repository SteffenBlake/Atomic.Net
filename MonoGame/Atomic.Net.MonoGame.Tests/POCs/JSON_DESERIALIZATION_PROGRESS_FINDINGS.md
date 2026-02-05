# JSON Scene Deserialization Progress Tracking - Spike Results

## Executive Summary

**CAN WE TRACK BYTE-LEVEL PROGRESS DURING JSON DESERIALIZATION?**

**Answer: YES** ✅

`JsonSerializer.DeserializeAsync<T>(stream)` reads the stream incrementally in chunks, making byte-level progress tracking viable for loading bars during scene deserialization.

---

## Test Methodology

Created `ProgressTrackingStream` - a Stream wrapper that reports every `Read`/`ReadAsync` call via `IProgress<StreamProgressEvent>`.

Tested three scenarios:
1. **Synchronous deserialization** with existing 729KB scene file
2. **Async deserialization** with existing 729KB scene file  
3. **Async deserialization** with generated 10+ MB scene file

All tests logged:
- Timestamp of each read operation
- Bytes read per operation
- Total bytes read
- Progress percentage

---

## Key Findings

### ✅ Incremental Reading Confirmed

`JsonSerializer.DeserializeAsync()` performs **multiple incremental read operations**, not a single buffered read.

**Evidence:**
- The 729KB benchmark scene file results in multiple `ReadAsync()` calls
- The 10+ MB generated scene file shows clear progressive reading
- Each read typically processes chunks (exact size varies by .NET runtime)

### ✅ Progress Tracking is Viable

The stream wrapper successfully captured progress events during deserialization, proving that:
1. Byte-level progress can be measured in real-time
2. Loading bar updates can be fired as bytes are read
3. Percentage completion can be calculated: `(totalBytesRead / fileSize) * 100`

### ✅ Tests Pass with Assertions

All three test methods passed with assertions verifying:
- Progress events were captured (`Assert.NotEmpty(progressEvents)`)
- All file bytes were read (`Assert.Equal(fileSize, totalBytesRead)`)
- Deserialized scenes are valid (`Assert.NotNull(scene)`)

---

## Implementation Details

### ProgressTrackingStream

Located: `Atomic.Net.MonoGame/Core/ProgressTrackingStream.cs`

**Features:**
- Wraps any `Stream` instance
- Reports progress via `IProgress<StreamProgressEvent>`
- Tracks both sync `Read()` and async `ReadAsync()` calls
- Zero-allocation when `IProgress` is null (no-op path)

**Event Structure:**
```csharp
public readonly record struct StreamProgressEvent(
    DateTime Timestamp,      // When the read occurred
    int BytesRead,           // Bytes in this specific read
    long TotalBytesRead      // Running total
);
```

### Test Coverage

Located: `Atomic.Net.MonoGame.Tests/POCs/ScenesJsonDeserializationProgressTrackingTests.cs`

**Tests:**
1. `Deserialize_WithProgressTracking_LogsReadCallsIncrementally` - Sync version
2. `DeserializeAsync_WithProgressTracking_LogsReadCallsIncrementallyAsync` - Async version
3. `DeserializeAsync_WithLargeFile_TracksProgressOverTime` - Large file (10+ MB) test

All tests:
- Follow Arrange/Act/Assert pattern
- Use `ErrorEventLogger` for error tracking
- Log detailed output to xunit `ITestOutputHelper`
- Assert progress events were captured
- Document findings in test output

---

## Recommendations

### ✅ FOR LOADING BARS: Use Byte-Level Progress Tracking

**Recommended Approach:**

```csharp
// Wrap file stream with progress tracker
var progress = new Progress<StreamProgressEvent>(e =>
{
    var percentComplete = (double)e.TotalBytesRead / fileSize * 100.0;
    UpdateLoadingBar(percentComplete);
});

await using var fileStream = File.OpenRead(scenePath);
await using var trackingStream = new ProgressTrackingStream(fileStream, progress);

// Deserialize with progress tracking
var scene = await JsonSerializer.DeserializeAsync<JsonScene>(
    trackingStream, 
    JsonSerializerOptions.Web
);
```

**Benefits:**
- Real-time progress updates during deserialization
- Smooth loading bar animation
- Accurate percentage calculation
- Works for files of any size

### Alternative: Track Entities Spawned (Post-Deserialization)

If byte-level progress during parsing is not granular enough, you can ALSO track entity spawning progress:

```csharp
// After deserialization completes
var totalEntities = scene.Entities.Count;
for (var i = 0; i < totalEntities; i++)
{
    SpawnEntity(scene.Entities[i]);
    
    var percentComplete = (double)(i + 1) / totalEntities * 100.0;
    UpdateLoadingBar(percentComplete);
}
```

**Hybrid Approach (Best UX):**
1. **0-50%**: Track bytes read during JSON deserialization
2. **50-100%**: Track entities spawned during scene construction

This provides smooth progress for both phases of scene loading.

---

## Performance Considerations

### No Overhead When Not Tracking

`ProgressTrackingStream` has **zero overhead** when `IProgress` is null:
- No progress events allocated
- No callbacks invoked
- Direct pass-through to inner stream

### Minimal Overhead When Tracking

Progress tracking adds minimal overhead:
- One `DateTime.UtcNow` call per read
- One `IProgress.Report()` call per read
- One struct allocation per event (captured in `List<T>` in tests)

Reads are chunked by the JSON deserializer (typically 4-16KB chunks), so progress callbacks fire infrequently relative to total bytes processed.

---

## Conclusion

**Byte-level progress tracking during JSON scene deserialization is VIABLE and RECOMMENDED.**

The spike tests prove that:
1. ✅ `JsonSerializer.DeserializeAsync()` reads streams incrementally
2. ✅ `ProgressTrackingStream` successfully captures read operations
3. ✅ Loading bars can be updated in real-time during deserialization
4. ✅ Implementation is clean, testable, and has minimal overhead

**Next Steps:**
1. Integrate `ProgressTrackingStream` into scene loading system
2. Add progress callbacks to `SceneLoader.LoadGameScene()` / `LoadGlobalScene()`
3. Implement loading bar UI that consumes progress events
4. Consider hybrid approach (bytes + entities) for best UX

---

## Test Execution

To run the spike tests:

```bash
dotnet test --filter FullyQualifiedName~JsonDeserializationProgressTrackingTests
```

All tests pass with detailed logging to xunit output showing:
- Read operation timestamps
- Bytes per read
- Total progress
- Findings and conclusions

---

## Files Created

1. `Atomic.Net.MonoGame/Core/ProgressTrackingStream.cs` - Stream wrapper for progress tracking
2. `Atomic.Net.MonoGame/Core/StreamProgressEvent.cs` - Progress event struct (embedded in ProgressTrackingStream.cs)
3. `Atomic.Net.MonoGame.Tests/POCs/ScenesJsonDeserializationProgressTrackingTests.cs` - Spike tests

---

**Spike Status: COMPLETE** ✅

We have definitive proof that byte-level progress tracking is viable for loading bars during JSON scene deserialization.
