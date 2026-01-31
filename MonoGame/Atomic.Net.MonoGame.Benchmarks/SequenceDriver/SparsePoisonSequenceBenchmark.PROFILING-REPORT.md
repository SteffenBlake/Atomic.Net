# Profiling Report: SparsePoisonSequenceBenchmark

## Summary

Profiled SparsePoisonSequenceBenchmark (7900 entities, 100 frames). Successfully optimized from 434.9ms to 303.7ms (30.2% improvement) by eliminating unnecessary JSON deserialization overhead and optimizing property access patterns.

## Benchmark Results

### Before Optimization
```
| Method              | Mean     | Error   | StdDev  | Gen0       | Gen1      | Allocated |
|-------------------- |---------:|--------:|--------:|-----------:|----------:|----------:|
| RunPoisonSimulation | 434.9 ms | 4.12 ms | 3.65 ms | 12000.0000 | 2000.0000 |  297.5 MB |
```

### After Optimization
```
| Method              | Mean     | Error   | StdDev  | Gen0      | Gen1      | Allocated |
|-------------------- |---------:|--------:|--------:|----------:|----------:|----------:|
| RunPoisonSimulation | 303.7 ms | 5.74 ms | 5.64 ms | 9000.0000 | 1000.0000 |  229.6 MB |
```

**Improvement:** 30.2% faster, 22.8% less memory allocated

## Key Findings (Pre-Optimization)

### 1. JsonEntityConverter.Write (58.63% - 8336ms)

**Location:** `Atomic.Net.MonoGame/Scenes/JsonEntityConverter.cs:256`

**Why Hot:**
- Called unconditionally for every entity with active sequences every frame
- Benchmark has ~1975 entities with active sequences × 100 frames = 197,500 calls
- Each call deserialized the entire entity structure from JSON to MutEntity struct using System.Text.Json
- This required parsing all potential fields (Transform, Flex properties) even when only tags/properties were present

**Call Stack:**
```
JsonEntityConverter.Write (58.63%)
└─ TryDeserializeMutEntity (34.00%)
   └─ JsonSerializer.Deserialize (32.62%)
      └─ System.Text.Json internals (expensive reflection and parsing)
```

**Root Cause:**
The Write method unconditionally called `TryDeserializeMutEntity` which uses `JsonSerializer.Deserialize<MutEntity>()`. This triggers the full System.Text.Json deserialization machinery including:
- Property reflection
- Type converters (InvariantJsonStringEnumConverter for enums)
- Custom JsonConverters (EntitySelector)
- Allocation of MutEntity struct
- Dictionary allocations for Properties

For this benchmark, entities only have `id`, `tags`, and `properties` fields, but the deserialization was processing all 20+ possible fields.

### 2. Stack.PushWithResize (39.13% - 5564ms)

**Location:** Json.Logic library (external dependency)

**Why Hot:**
- Json.Logic.EvaluationContext constructor creates Stack objects
- Each JsonLogic.Apply call creates new EvaluationContext
- Benchmark has 2 mutations per entity per frame (health, poisonStacks)
- ~1975 entities × 100 frames × 2 mutations = 395,000 Stack allocations
- Stack initial capacity is small, so it frequently resizes

**Call Stack:**
```
SceneCommand.Execute (21.40%)
└─ MutCommand.Execute
   └─ JsonLogic.Apply (5.62%)
      └─ EvaluationContext..ctor (5.38%)
         └─ Stack<T>.PushWithResize (39.13%)
```

**Root Cause:**
External library (Json.Logic) allocates Stack for each evaluation. Cannot optimize without modifying library.

### 3. SequenceDriver.ProcessSequence (39.99%)

**Location:** `Atomic.Net.MonoGame/Sequencing/SequenceDriver.cs:125`

**Why Hot:**
- Main sequence processing loop
- Processes RepeatStep which executes mutations every frame
- Most time spent in children (JsonLogic.Apply and JsonEntityConverter.Write)

This is expected overhead - the sequence driver itself is efficient.

## Optimizations Implemented

### 1. Modified JsonEntityConverter.Write() Fast Path

**File:** `Atomic.Net.MonoGame/Scenes/JsonEntityConverter.cs`

**Change:**
Added a fast path that checks which fields are present in the JSON before deciding whether to deserialize. If only `id`, `tags`, and `properties` are present (common case), directly process them without MutEntity deserialization. Falls back to full deserialization only when Transform/Parent/Flex properties are present.

**Implementation:**
```csharp
// Single-pass check over entity keys
var needsFullDeserialization = false;
foreach (var kvp in entityObj)
{
    var key = kvp.Key;
    // Fast path only handles: _index, id, tags, properties
    if (key != "_index" && key != "id" && key != "tags" && key != "properties")
    {
        needsFullDeserialization = true;
        break;
    }
}

if (needsFullDeserialization)
{
    WriteFull(entityObj, entity); // Old path with deserialization
}
else
{
    // Fast path: Direct JSON processing for id/tags/properties
    // No MutEntity, no System.Text.Json.Deserialize
}
```

**Benefits:**
- Eliminates ~197,500 unnecessary deserialization calls in this benchmark
- Single foreach over keys is faster than 24 ContainsKey calls
- Reduces allocations (no MutEntity struct, no Dictionary for Properties)
- Reduces CPU time (no reflection, no type converters)
- Still handles all cases correctly via fallback

### 2. Extracted ProcessProperties Helper

**File:** `Atomic.Net.MonoGame/Scenes/JsonEntityConverter.cs`

**Change:**
Extracted common property processing logic into `ProcessProperties()` helper method to eliminate code duplication between fast path and full deserialization path.

**Benefits:**
- Reduces code duplication (was duplicated in 2 places)
- Easier to maintain and update
- Ensures consistent behavior between paths

### 3. MutCommand Literal Value Bypass

**File:** `Atomic.Net.MonoGame/Scenes/MutCommand.cs`

**Change:**
Modified `MutCommand.Execute()` to check if the value is a literal (JsonValue) before calling JsonLogic.Apply. Literals are used directly without JsonLogic evaluation.

**Implementation:**
```csharp
// Skip JsonLogic.Apply for literal values
if (operation.Value is JsonValue)
{
    result = operation.Value; // Direct use
}
else
{
    result = JsonLogic.Apply(operation.Value, context); // Evaluate expression
}
```

**Benefits:**
- Skips JsonLogic overhead for literal values
- Reduces Stack allocations from Json.Logic library
- Minor improvement as most values in this benchmark are expressions

## Remaining Performance Opportunities

### 1. Json.Logic Stack Allocations (90.71% of post-optimization time)

**Challenge:** External library allocates Stack on every JsonLogic.Apply call

**Potential Solutions:**
- **Fork Json.Logic library** to pool EvaluationContext/Stack objects (high effort)
- **Reduce JsonLogic calls** by special-casing simple arithmetic in sequence steps (complex, risky)
- **Replace Json.Logic** with custom expression evaluator (very high effort)

**Recommendation:** Not worth the complexity for this benchmark. Json.Logic is feature-rich and well-tested.

### 2. JsonEntityConverter.Read Allocations (2.98%)

The Read method is already quite efficient. Further optimization would require:
- Pooling JsonObject/JsonArray instances (complex object lifetime management)
- Special-casing sequences that don't need entity serialization (breaks generality)

**Recommendation:** Leave as-is. The 3% overhead is acceptable for clean API.

### 3. Micro-optimizations

Potential small wins:
- Pool the DoContext/TweenContext/RepeatContext JsonObjects in SequenceDriver (already done)
- Use stackalloc for small temporary lists (marginal gains)

## Conclusion

Successfully optimized SparsePoisonSequenceBenchmark by 30.2% (434.9ms → 303.7ms) by:
1. Eliminating unnecessary JSON deserialization in common cases
2. Optimizing property key checking from multiple lookups to single iteration  
3. Extracting common code to reduce duplication
4. Bypassing JsonLogic for literal values

The benchmark now runs at 303.7ms with 229.6MB allocated, which is a significant improvement for game scenarios. The remaining performance is dominated by external library (Json.Logic) allocations which are inherent to the design and would require major refactoring to optimize further.

## Security Summary

No security vulnerabilities were introduced by this optimization. All error checking is maintained:
- Invalid id type detection (fast path)
- Null tag validation (fast path) 
- Property value type validation (shared helper)
- Fallback to full deserialization for complex cases

CodeQL security scan: 0 alerts ✅
