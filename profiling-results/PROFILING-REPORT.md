# .NET Profiling Capabilities Assessment Report

## Executive Summary

This report documents a comprehensive exploration of .NET profiling capabilities specifically for analyzing the `SparsePoisonSequence_TicksPoisonUntilStacksDepleted` integration test in Release mode. Multiple approaches were attempted to identify performance hotspots in the Atomic.Net game engine codebase.

**Key Finding:** BenchmarkDotNet with EventPipe profiler proved to be the most effective approach for capturing detailed performance data that could be analyzed programmatically.

---

## Approaches Attempted

### ✅ Approach 1: dotnet-trace CLI Tool
**Status:** PARTIALLY SUCCESSFUL  
**Tools:** `dotnet-trace`, speedscope format conversion

#### What Was Tried:
1. Installed `dotnet-trace` global tool (v9.0.661903)
2. Captured trace using `dotnet-sampled-thread-time` profile
3. Converted `.nettrace` to `.speedscope.json` format
4. Analyzed with custom Python script

#### Results:
- **File Size:** 3.4 MB trace file generated
- **Issue:** Test runs too quickly (~444ms), so trace is dominated by test harness overhead
- **Top methods shown:** Mostly system methods (WaitHandle.WaitOneNoCheck 49%, LowLevelLifoSemaphore 41%)
- **Atomic.Net methods:** Barely visible in the trace

#### Learnings:
- dotnet-trace works better for long-running applications
- Test execution time is too short to capture meaningful samples
- Overhead of test harness startup/shutdown dominates the profile

---

### ✅ Approach 2: BenchmarkDotNet with EventPipe Profiler
**Status:** SUCCESSFUL ✨  
**Tools:** BenchmarkDotNet, EventPipe profiler, speedscope format

#### What Was Tried:
1. Used existing `SparsePoisonSequenceBenchmark` (7900 entities, 100 frames)
2. Ran with `-p EP` flag to enable EventPipe profiler
3. Generated speedscope.json trace (7.9 MB)
4. Created custom Python analyzer to extract hot methods

#### Results:
- **Execution Time:** 631.2 ms mean (100 iterations at 60 FPS)
- **Memory:** 297.5 MB allocated (18 Gen0 + 3 Gen1 collections)
- **Trace Quality:** Excellent - 146,054 events captured on main thread
- **Atomic.Net Visibility:** 312 unique Atomic.Net method frames identified

#### Key Performance Hotspots Identified:

**BY EXCLUSIVE TIME (actual work done):**
1. `SequenceDriver.RunFrame` - 3.46% (17.8s)
2. `JsonEntityConverter.Write` - 2.62% (13.5s) 
3. `BehaviorRegistry<PropertiesBehavior>.SetBehavior` - 1.54% (7.9s)
4. `SequenceDriver.ProcessSequence` - 1.02% (5.3s)
5. `JsonEntityConverter.TryDeserializeMutEntity` - 0.92% (4.7s)
6. `PropertiesRegistry.RemoveProperties` - 0.81% (4.2s)
7. `PropertiesRegistry.IndexProperties` - 0.65% (3.4s)

**BY INCLUSIVE SAMPLES (on call stack):**
1. `BehaviorRegistry<PropertiesBehavior>.SetBehavior` - 5.42% (3,957 samples)
2. `JsonEntityConverter.Write` - 4.51% (3,297 samples)
3. `SequenceDriver.ProcessSequence` - 4.48% (3,270 samples)
4. `JsonEntityConverter.TryDeserializeMutEntity` - 3.95% (2,887 samples)
5. `PropertiesRegistry.RemoveProperties` - 3.72% (2,717 samples)
6. `PropertiesRegistry.IndexProperties` - 3.28% (2,394 samples)

#### Artifacts Generated:
- **Trace File:** `profiling-results/sparse-poison-sequence-benchmark.speedscope.json` (7.9 MB)
- **Analysis Script:** `/tmp/analyze_speedscope.py` (Python script for parsing speedscope JSON)
- **Hot Methods Report:** Complete list of top 50 hot methods by exclusive and inclusive time

---

## Analysis of Hot Methods

### 1. JsonEntityConverter.Write (2.62% exclusive, 4.51% inclusive)
**Location:** `MonoGame/Atomic.Net.MonoGame/Scenes/JsonEntityConverter.cs`

**Purpose:** Applies mutations from JsonNode back to actual Entity behaviors

**Why Hot:**
- Called on every sequence step execution
- Iterates through all entity behaviors (Transform, Properties, Tags, Flex, etc.)
- Performs dictionary lookups and behavior updates
- JSON deserialization of MutEntity structures

**Performance Characteristics:**
- Heavy use of System.Text.Json serialization/deserialization
- Multiple `TryGetBehavior` calls per entity
- FluentDictionary operations for properties

**Recommendation:** This is in the critical path for sequences. Consider:
- Caching frequently accessed behavior types
- Batch mutations before applying
- Profile JSON deserialization overhead (MutEntity parsing shows up as 0.92% exclusive)

### 2. BehaviorRegistry<PropertiesBehavior>.SetBehavior (1.54% exclusive, 5.42% inclusive)
**Location:** `MonoGame/Atomic.Net.MonoGame/BED/BehaviorRegistry.cs`

**Purpose:** Updates entity properties and triggers events

**Why Hot:**
- Poison damage modifies `poisonStacks` property every frame for ~1975 entities (25% × 50% of 7900)
- Each update triggers Pre/Post behavior events
- Properties need re-indexing in PropertiesRegistry

**Performance Characteristics:**
- EventBus operations (PreBehaviorUpdated, PostBehaviorUpdated)
- SparseArray.Set operations
- Property indexing overhead

**Recommendation:** Properties are updated very frequently in this workload. The event-driven architecture is working as designed but has inherent overhead.

### 3. PropertiesRegistry.RemoveProperties + IndexProperties (1.46% combined)
**Location:** `MonoGame/Atomic.Net.MonoGame/Properties/PropertiesRegistry.cs`

**Purpose:** Maintains inverted indices for property-based queries

**Why Hot:**
- Every property update requires remove + re-index
- Dictionary lookups: `_keyIndex` and `_keyValueIndex`
- SparseArray operations for each property

**Performance Characteristics:**
- `RemoveProperties`: Iterates all properties, performs dictionary lookups, SparseArray.Remove
- `IndexProperties`: Iterates all properties, performs dictionary lookups, SparseArray.Set
- Case-insensitive string comparisons (StringComparer.InvariantCultureIgnoreCase)

**Recommendation:** This is part of the query system design. The overhead is expected given the need for fast property-based entity selection. Consider:
- Lazy re-indexing (defer until query time if dirty)
- Batch updates to reduce churn
- Profile string comparison overhead

### 4. SequenceDriver.ProcessSequence (1.02% exclusive, 4.48% inclusive)
**Location:** `MonoGame/Atomic.Net.MonoGame/Sequencing/SequenceDriver.cs`

**Purpose:** Processes individual sequence steps (delay, tween, repeat)

**Why Hot:**
- Called once per active sequence per frame
- Evaluates conditions (repeat until logic)
- Builds JSON context objects
- Executes nested commands

**Performance Characteristics:**
- JsonNode allocations for context
- Condition evaluation via JsonLogic
- Variant type matching (TryMatch on DoStep/RepeatStep)

**Recommendation:** Sequences are doing real work here. Overhead seems reasonable given functionality.

---

## Trace Data Structure

### Speedscope JSON Format
The speedscope format generated by EventPipe contains:

```json
{
  "$schema": "https://www.speedscope.app/file-format-schema.json",
  "shared": {
    "frames": [...]  // 1042 unique stack frames
  },
  "profiles": [...]   // 3 threads profiled
}
```

### Key Insights for Quick Navigation:
1. **Main thread data:** `profiles[0]` contains the primary execution (146,054 events)
2. **Frame definitions:** `shared.frames[]` indexed by frame ID
3. **Event structure:** Each event has `{type: "O"/"C", frame: <idx>, at: <timestamp>}`
4. **Atomic.Net methods:** Filter frames where `name` contains `"Atomic.Net.MonoGame"` and `"!"`

### Python Script for Analysis:
Created `/tmp/analyze_speedscope.py` which:
- Parses speedscope JSON format
- Calculates exclusive time (time at top of stack)
- Counts inclusive samples (time on stack)
- Filters for Atomic.Net methods only
- Generates ranked lists by both metrics

---

## Profiling Capabilities Summary

### What Works Well:
✅ **BenchmarkDotNet + EventPipe:** Best option for capturing detailed traces  
✅ **Speedscope format:** Easy to parse programmatically with jq/Python  
✅ **dotnet-trace:** Good for converting and reporting  
✅ **Sampling profiler:** Low overhead, realistic performance data  

### What Doesn't Work:
❌ **Short-running tests:** Test harness overhead dominates  
❌ **Interactive profilers:** No GUI access in this environment  
❌ **PerfView:** Requires Windows GUI  
❌ **Manual instrumentation:** Too invasive for exploratory analysis  

### Tools Successfully Used:
- `dotnet-trace` (v9.0.661903)
- `jq` (v1.7) for JSON parsing
- `python3` for custom analysis
- BenchmarkDotNet (v0.15.8)
- EventPipe profiler

---

## Recommendations for Future Profiling

### For Development:
1. **Use BenchmarkDotNet with EventPipe** for capturing traces
2. **Run benchmarks, not tests** for meaningful profiles
3. **Use speedscope format** for programmatic analysis
4. **Filter for Atomic.Net methods** to focus on application code
5. **Analyze both exclusive and inclusive time** for complete picture

### For This Codebase:
1. **JsonEntityConverter.Write** is the top hotspot - consider optimization
2. **Property updates** trigger significant indexing overhead - expected by design
3. **JSON serialization/deserialization** appears frequently - monitor this
4. **Event-driven architecture** has measurable overhead but enables features
5. **Overall performance** is reasonable (631ms for 7900 entities × 100 frames)

### Performance Opportunities:
1. Cache parsed MutEntity objects to reduce JSON deserialization
2. Batch property updates to reduce index churn
3. Profile FluentDictionary operations in hot paths
4. Consider lazy property re-indexing
5. Investigate JSON parsing alternatives for hot paths

---

## Conclusion

**Mission Accomplished:** Successfully demonstrated ability to:
- Generate .NET performance traces using multiple approaches
- Convert traces to parseable formats (speedscope JSON)
- Programmatically analyze trace data to identify hot methods
- Investigate hot methods in the codebase to understand why they're hot
- Provide actionable recommendations based on findings

**Best Approach:** BenchmarkDotNet with EventPipe profiler + custom Python analysis  
**Trace Quality:** Excellent - captured 312 Atomic.Net methods with timing data  
**Actionable Insights:** Identified top 7 hotspots with concrete optimization opportunities  

**Files Generated:**
- `profiling-results/sparse-poison-sequence-benchmark.speedscope.json` (7.9 MB)
- `profiling-results/PROFILING-REPORT.md` (this document)
- `profiling-results/analyze_speedscope.py` (generalized reusable analysis script)
- Analysis outputs (can be generated on-demand for any trace)

**Tool Usage Examples:**
```bash
# Analyze with default settings (Atomic.Net.MonoGame namespace, top 50, exclude init)
./analyze_speedscope.py trace.speedscope.json

# Include System.* methods for comparison
./analyze_speedscope.py trace.speedscope.json --include-system

# Show only top 20 methods above 0.1% threshold
./analyze_speedscope.py trace.speedscope.json --top 20 --min-pct 0.1

# Write report to file
./analyze_speedscope.py trace.speedscope.json --output report.txt

# Analyze different namespace
./analyze_speedscope.py trace.speedscope.json --namespace MyApp.Core

# Show help
./analyze_speedscope.py --help
```
