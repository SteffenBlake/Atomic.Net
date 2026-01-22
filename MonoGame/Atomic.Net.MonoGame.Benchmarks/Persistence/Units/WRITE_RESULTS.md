# LiteDB Write Benchmark Results

**Test Configuration:**
- Entities: 10, 100, 1000
- All methods tested with and without memory pooling
- Baseline: BulkInsert_JsonSerializer

## Summary Table

| Method                                 | EntityCount | Mean        | Ratio | Allocated   | Alloc Ratio |
|--------------------------------------- |------------ |------------:|------:|------------:|------------:|
| **Individual Insert Methods** | | | | | |
| IndividualInsert_JsonSerializer        | 10          |  1,099 us   |  3.03 |   119.73 KB |        1.88 |
| IndividualInsert_Utf8JsonWriter_Pooled | 10          |  1,060 us   |  2.93 |   121.05 KB |        1.91 |
| IndividualInsert_DirectBson            | 10          |  1,157 us   |  3.19 |   153.45 KB |        2.41 |
| **Bulk Insert Methods (RECOMMENDED)** | | | | | |
| BulkInsert_JsonSerializer ⭐           | 10          |    364 us   |  1.00 |    63.54 KB |        1.00 |
| BulkInsert_Utf8JsonWriter_Pooled       | 10          |    374 us   |  1.03 |    65.42 KB |        1.03 |
| BulkInsert_DirectBson                  | 10          |    442 us   |  1.22 |    84.64 KB |        1.33 |
| **Upsert Methods** | | | | | |
| Upsert_JsonSerializer                  | 10          |  1,481 us   |  4.09 |   306.55 KB |        4.82 |
| Upsert_Utf8JsonWriter_Pooled           | 10          |  1,482 us   |  4.09 |   314.36 KB |        4.95 |
| Upsert_DirectBson                      | 10          |  1,521 us   |  4.20 |   336.52 KB |        5.30 |
| | | | | | |
| IndividualInsert_JsonSerializer        | 100         | 10,894 us   |  4.62 |  1328.17 KB |        1.77 |
| IndividualInsert_Utf8JsonWriter_Pooled | 100         | 11,646 us   |  4.94 |  1341.45 KB |        1.79 |
| IndividualInsert_DirectBson            | 100         | 12,511 us   |  5.31 |  1640.48 KB |        2.19 |
| BulkInsert_JsonSerializer ⭐           | 100         |  2,426 us   |  1.03 |   748.47 KB |        1.00 |
| BulkInsert_Utf8JsonWriter_Pooled       | 100         |  2,618 us   |  1.11 |   761.75 KB |        1.02 |
| BulkInsert_DirectBson                  | 100         |  3,605 us   |  1.53 |  1009.98 KB |        1.35 |
| Upsert_JsonSerializer                  | 100         | 12,533 us   |  5.32 |  3245.78 KB |        4.34 |
| Upsert_Utf8JsonWriter_Pooled           | 100         | 14,359 us   |  6.09 |  3259.06 KB |        4.35 |
| Upsert_DirectBson                      | 100         | 15,233 us   |  6.46 |  3541.69 KB |        4.73 |
| | | | | | |
| IndividualInsert_JsonSerializer        | 1000        | 56,684 us   |  3.99 | 19363.75 KB |        1.57 |
| IndividualInsert_Utf8JsonWriter_Pooled | 1000        | 56,140 us   |  3.95 | 18786.99 KB |        1.52 |
| IndividualInsert_DirectBson            | 1000        | 59,758 us   |  4.21 | 21499.95 KB |        1.74 |
| BulkInsert_JsonSerializer ⭐           | 1000        | 16,163 us   |  1.14 | 12353.88 KB |        1.00 |
| BulkInsert_Utf8JsonWriter_Pooled       | 1000        | 16,084 us   |  1.13 | 12486.69 KB |        1.01 |
| BulkInsert_DirectBson                  | 1000        | 17,640 us   |  1.24 | 14693.60 KB |        1.19 |
| Upsert_JsonSerializer                  | 1000        | 64,610 us   |  4.55 | 37395.24 KB |        3.03 |
| Upsert_Utf8JsonWriter_Pooled           | 1000        | 65,980 us   |  4.65 | 37968.41 KB |        3.07 |
| Upsert_DirectBson                      | 1000        | 68,082 us   |  4.80 | 40805.45 KB |        3.30 |

## Key Findings

### ✅ Memory Pooling Results
**Pooled buffers (Utf8JsonWriter) provide NO meaningful benefit:**
- 10 entities: 364µs (standard) vs 374µs (pooled) - **3% SLOWER**
- 100 entities: 2.4ms (standard) vs 2.6ms (pooled) - **8% SLOWER**
- 1000 entities: 16.2ms (standard) vs 16.1ms (pooled) - **essentially identical**

### 🎯 Recommendations
1. **Use BulkInsert_JsonSerializer** - simplest and fastest
2. **Avoid memory pooling** - adds complexity with no benefit
3. **Avoid DirectBson** - 22-33% slower and more complex
4. **Individual inserts are 3-4x slower** - always batch when possible
5. **Upsert is 4-5x slower** - only use when needed for updates

### 📊 Performance Summary
- **Best write method**: `BulkInsert_JsonSerializer` (baseline)
- **Memory pooling impact**: None (actually slightly slower)
- **Bulk vs Individual**: 3-4x faster with bulk
- **Bulk vs Upsert**: Upsert is 4x slower (use only for updates)
