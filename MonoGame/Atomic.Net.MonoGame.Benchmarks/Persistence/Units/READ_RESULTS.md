# LiteDB Read Benchmark Results

**Test Configuration:**
- Total entities in DB: 100, 1000, 10000
- Entities read: 1% of total (sparse reads - actual use case)
- All methods tested with and without memory pooling
- Baseline: IndividualFindById_JsonDeserialize

## Summary Table

| Method                             | TotalEntitiesInDb | Entities Read | Mean       | Ratio | Allocated  | Alloc Ratio |
|----------------------------------- |------------------:|--------------:|-----------:|------:|-----------:|------------:|
| **Individual FindById (Small Counts)** | | | | | | |
| IndividualFindById_JsonDeserialize | 100               | 1             |   16.25 us |  1.00 |   22.94 KB |        1.00 |
| IndividualFindById_Utf8JsonReader  | 100               | 1             |   16.09 us |  0.99 |   23.15 KB |        1.01 |
| IndividualFindById_DirectBson      | 100               | 1             |   12.22 us |  0.75 |   20.56 KB |        0.90 |
| **Batch Query (Larger Counts)** | | | | | | |
| BatchQuery_JsonDeserialize         | 100               | 1             |   40.52 us |  2.49 |   35.76 KB |        1.56 |
| BatchQuery_Utf8JsonReader          | 100               | 1             |   40.59 us |  2.50 |   35.97 KB |        1.57 |
| BatchQuery_DirectBson              | 100               | 1             |   33.84 us |  2.08 |   33.54 KB |        1.46 |
| **Hybrid Approach ⭐** | | | | | | |
| Hybrid_JsonDeserialize ⭐           | 100               | 1             |   15.84 us |  0.97 |   22.94 KB |        1.00 |
| Hybrid_Utf8JsonReader              | 100               | 1             |   15.65 us |  0.96 |   23.15 KB |        1.01 |
| | | | | | | |
| IndividualFindById_JsonDeserialize | 1000              | 10            |  186.81 us |  1.00 |  271.60 KB |        1.00 |
| IndividualFindById_Utf8JsonReader  | 1000              | 10            |  184.55 us |  0.99 |  273.32 KB |        1.01 |
| IndividualFindById_DirectBson      | 1000              | 10            |  145.59 us |  0.78 |  247.38 KB |        0.91 |
| BatchQuery_JsonDeserialize ⭐       | 1000              | 10            |  117.88 us |  0.63 |  156.35 KB |        0.58 |
| BatchQuery_Utf8JsonReader          | 1000              | 10            |  117.00 us |  0.63 |  158.59 KB |        0.58 |
| BatchQuery_DirectBson              | 1000              | 10            |   82.90 us |  0.44 |  132.53 KB |        0.49 |
| Hybrid_JsonDeserialize ⭐           | 1000              | 10            |  116.22 us |  0.62 |  156.35 KB |        0.58 |
| Hybrid_Utf8JsonReader              | 1000              | 10            |  119.20 us |  0.64 |  158.61 KB |        0.58 |
| | | | | | | |
| IndividualFindById_JsonDeserialize | 10000             | 100           | 2389.24 us |  1.00 | 3948.53 KB |        1.00 |
| IndividualFindById_Utf8JsonReader  | 10000             | 100           | 2351.91 us |  0.98 | 3970.25 KB |        1.01 |
| IndividualFindById_DirectBson      | 10000             | 100           | 2000.98 us |  0.84 | 3709.53 KB |        0.94 |
| BatchQuery_JsonDeserialize ⭐       | 10000             | 100           | 1221.61 us |  0.51 | 2395.81 KB |        0.61 |
| BatchQuery_Utf8JsonReader          | 10000             | 100           | 1237.40 us |  0.52 | 2412.95 KB |        0.61 |
| BatchQuery_DirectBson              | 10000             | 100           |  938.47 us |  0.39 | 2154.50 KB |        0.55 |
| Hybrid_JsonDeserialize ⭐           | 10000             | 100           | 1207.68 us |  0.51 | 2391.12 KB |        0.61 |
| Hybrid_Utf8JsonReader              | 10000             | 100           | 1204.71 us |  0.50 | 2412.63 KB |        0.61 |

## Key Findings

### ✅ Memory Pooling Results
**Pooled buffers (Utf8JsonReader) provide NO meaningful benefit:**
- 100 DB, 1 read: 16.25µs (standard) vs 16.09µs (pooled) - **essentially identical**
- 1000 DB, 10 reads: 187µs (standard) vs 185µs (pooled) - **1% improvement (negligible)**
- 10000 DB, 100 reads: 2389µs (standard) vs 2352µs (pooled) - **1.5% improvement (negligible)**

### 🎯 Recommendations
1. **Use Hybrid approach with JsonDeserialize** - optimal for all scenarios
2. **Avoid memory pooling** - adds complexity with minimal/no benefit  
3. **DirectBson only if you don't need full entity** - 25-45% faster but requires manual field extraction
4. **Batch query is 2x faster** for 10+ entities (vs individual FindById)

### 📊 Performance Summary by Use Case

**Small counts (1-5 entities):**
- Use: `IndividualFindById_JsonDeserialize`
- Performance: ~16µs per entity
- Hybrid automatically uses this

**Larger counts (10+ entities):**
- Use: `BatchQuery_JsonDeserialize` or `Hybrid_JsonDeserialize`
- Performance improvement: 37% faster (118µs vs 187µs for 10 entities)
- Performance improvement: 49% faster (1.2ms vs 2.4ms for 100 entities)

**DirectBson (metadata only):**
- 25% faster for small reads
- 56% faster for large reads  
- But requires manual field extraction - only use if you don't need full entity

### 💡 Hybrid Approach (RECOMMENDED)
```csharp
if (keysToLoad.Length <= 5)
{
    // Individual FindById - optimal for small counts
    foreach (var key in keysToLoad)
    {
        var doc = collection.FindById(key);
        var entity = JsonSerializer.Deserialize<TestEntity>(doc["data"]);
    }
}
else
{
    // Batch query - 2x faster for larger counts
    var keySet = new HashSet<string>(keysToLoad);
    var documents = collection.Find(doc => keySet.Contains(doc["_id"]));
    foreach (var doc in documents)
    {
        var entity = JsonSerializer.Deserialize<TestEntity>(doc["data"]);
    }
}
```
