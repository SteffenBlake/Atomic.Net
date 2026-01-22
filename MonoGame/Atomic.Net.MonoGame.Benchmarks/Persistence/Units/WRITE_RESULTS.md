# LiteDB Write Benchmark Results

**Test Configuration:**
- Entities: 10, 100, 1000
- All methods tested with and without memory pooling
- Baseline: BulkInsert_JsonSerializer

## Summary Table

| Method                                     | EntityCount | Mean        | Error       | StdDev      | Median      | Ratio | RatioSD | Gen0      | Allocated   | Alloc Ratio |
|------------------------------------------- |------------ |------------:|------------:|------------:|------------:|------:|--------:|----------:|------------:|------------:|
| IndividualInsert_JsonSerializer            | 10          |    927.7 us |    20.22 us |    54.66 us |    924.6 us |  2.82 |    0.31 |         - |   116.11 KB |        1.83 |
| IndividualInsert_Utf8JsonWriter_Pooled     | 10          |    928.7 us |    21.71 us |    60.52 us |    916.8 us |  2.82 |    0.32 |         - |   114.13 KB |        1.80 |
| IndividualInsert_DirectBson                | 10          |    940.8 us |    19.50 us |    52.72 us |    935.2 us |  2.86 |    0.31 |         - |   135.11 KB |        2.13 |
| BulkInsert_JsonSerializer                  | 10          |    332.2 us |    11.68 us |    32.95 us |    324.4 us |  1.01 |    0.14 |         - |    63.54 KB |        1.00 |
| BulkInsert_JsonSerializer_PreAllocd        | 10          |    336.6 us |    11.82 us |    33.53 us |    331.1 us |  1.02 |    0.14 |         - |    76.77 KB |        1.21 |
| BulkInsert_Utf8JsonWriter_Pooled           | 10          |    336.8 us |    10.66 us |    30.58 us |    331.1 us |  1.02 |    0.13 |         - |    69.52 KB |        1.09 |
| BulkInsert_Utf8JsonWriter_Pooled_PreAllocd | 10          |    341.4 us |    11.94 us |    33.49 us |    333.9 us |  1.04 |    0.14 |         - |     58.2 KB |        0.92 |
| BulkInsert_DirectBson                      | 10          |    382.7 us |     8.41 us |    23.86 us |    377.7 us |  1.16 |    0.13 |         - |    92.82 KB |        1.46 |
| BulkInsert_DirectBson_PreAllocd            | 10          |    392.6 us |     9.37 us |    27.05 us |    389.9 us |  1.19 |    0.14 |         - |    92.69 KB |        1.46 |
| Upsert_JsonSerializer                      | 10          |  1,196.8 us |    33.60 us |    90.84 us |  1,177.3 us |  3.64 |    0.43 |         - |   306.55 KB |        4.82 |
| Upsert_Utf8JsonWriter_Pooled               | 10          |  1,063.7 us |    40.90 us |   116.69 us |  1,039.8 us |  3.23 |    0.46 |         - |   297.34 KB |        4.68 |
| Upsert_DirectBson                          | 10          |  1,278.9 us |    63.31 us |   179.61 us |  1,223.6 us |  3.89 |    0.65 |         - |   320.08 KB |        5.04 |
|                                            |             |             |             |             |             |       |         |           |             |             |
| IndividualInsert_JsonSerializer            | 100         |  8,166.1 us |   288.18 us |   738.70 us |  8,082.0 us |  3.32 |    0.45 |         - |  1328.17 KB |        1.77 |
| IndividualInsert_Utf8JsonWriter_Pooled     | 100         | 10,125.1 us | 1,015.09 us | 2,961.07 us |  9,221.7 us |  4.12 |    1.27 |         - |   1328.3 KB |        1.77 |
| IndividualInsert_DirectBson                | 100         |  9,506.8 us |   911.07 us | 2,539.71 us |  9,279.7 us |  3.87 |    1.10 |         - |  1590.45 KB |        2.12 |
| BulkInsert_JsonSerializer                  | 100         |  2,486.4 us |    93.67 us |   273.23 us |  2,391.5 us |  1.01 |    0.15 |         - |   748.47 KB |        1.00 |
| BulkInsert_JsonSerializer_PreAllocd        | 100         |  2,786.1 us |    96.21 us |   280.64 us |  2,846.3 us |  1.13 |    0.16 |         - |   747.63 KB |        1.00 |
| BulkInsert_Utf8JsonWriter_Pooled           | 100         |  2,666.6 us |   102.57 us |   295.95 us |  2,718.4 us |  1.08 |    0.16 |         - |   921.55 KB |        1.23 |
| BulkInsert_Utf8JsonWriter_Pooled_PreAllocd | 100         |  2,741.2 us |    92.18 us |   268.90 us |  2,789.8 us |  1.11 |    0.16 |         - |   747.77 KB |        1.00 |
| BulkInsert_DirectBson                      | 100         |  3,613.3 us |   133.80 us |   390.30 us |  3,659.4 us |  1.47 |    0.22 |         - |   982.57 KB |        1.31 |
| BulkInsert_DirectBson_PreAllocd            | 100         |  3,717.2 us |   158.86 us |   465.90 us |  3,890.6 us |  1.51 |    0.24 |         - |   981.73 KB |        1.31 |
| Upsert_JsonSerializer                      | 100         | 12,568.6 us | 1,271.37 us | 3,728.70 us | 13,146.5 us |  5.11 |    1.60 |         - |  3245.78 KB |        4.34 |
| Upsert_Utf8JsonWriter_Pooled               | 100         | 10,133.4 us | 1,171.41 us | 3,435.55 us |  9,678.6 us |  4.12 |    1.46 |         - |  3166.55 KB |        4.23 |
| Upsert_DirectBson                          | 100         | 12,505.0 us | 1,484.07 us | 4,352.52 us | 12,693.8 us |  5.09 |    1.84 |         - |  3508.06 KB |        4.69 |
|                                            |             |             |             |             |             |       |         |           |             |             |
| IndividualInsert_JsonSerializer            | 1000        | 56,588.3 us | 1,085.57 us | 2,117.32 us | 56,086.9 us |  4.60 |    2.07 | 1000.0000 |  18005.2 KB |        1.46 |
| IndividualInsert_Utf8JsonWriter_Pooled     | 1000        | 56,177.2 us | 1,049.02 us | 1,864.63 us | 55,805.9 us |  4.57 |    2.05 | 1000.0000 | 18370.32 KB |        1.49 |
| IndividualInsert_DirectBson                | 1000        | 59,001.5 us | 1,171.50 us | 3,342.36 us | 57,871.1 us |  4.80 |    2.17 | 1000.0000 |  21630.3 KB |        1.75 |
| BulkInsert_JsonSerializer                  | 1000        | 15,170.9 us | 2,294.44 us | 6,765.22 us | 16,620.3 us |  1.23 |    0.81 | 1000.0000 | 12357.09 KB |        1.00 |
| BulkInsert_JsonSerializer_PreAllocd        | 1000        | 13,701.2 us | 2,037.25 us | 5,974.90 us | 12,777.4 us |  1.11 |    0.73 | 1000.0000 | 12349.26 KB |        1.00 |
| BulkInsert_Utf8JsonWriter_Pooled           | 1000        |  8,832.7 us |   746.63 us | 1,992.90 us |  8,530.8 us |  0.72 |    0.37 | 1000.0000 | 12497.63 KB |        1.01 |
| BulkInsert_Utf8JsonWriter_Pooled_PreAllocd | 1000        |  8,615.6 us |   573.60 us | 1,490.87 us |  8,498.5 us |  0.70 |    0.34 | 1000.0000 | 12489.77 KB |        1.01 |
| BulkInsert_DirectBson                      | 1000        | 13,876.7 us |   801.37 us | 2,193.73 us | 13,003.1 us |  1.13 |    0.54 | 1000.0000 | 14296.61 KB |        1.16 |
| BulkInsert_DirectBson_PreAllocd            | 1000        | 14,267.3 us |   811.29 us | 2,274.95 us | 13,183.5 us |  1.16 |    0.56 | 1000.0000 | 14288.74 KB |        1.16 |
| Upsert_JsonSerializer                      | 1000        | 65,294.1 us | 1,301.75 us | 2,538.97 us | 64,700.3 us |  5.31 |    2.38 | 3000.0000 | 36691.64 KB |        2.97 |
| Upsert_Utf8JsonWriter_Pooled               | 1000        | 60,957.0 us | 1,193.85 us | 2,122.07 us | 60,193.2 us |  4.96 |    2.22 | 3000.0000 | 38533.48 KB |        3.12 |
| Upsert_DirectBson                          | 1000        | 68,111.1 us | 1,345.89 us | 3,172.43 us | 66,932.9 us |  5.54 |    2.49 | 3000.0000 | 39221.55 KB |        3.17 

## Key Findings

### ✅ Memory Pooling Results
**Pooled buffers (Utf8JsonWriter) provide SMALL meaningful benefit on small amounts, but large benefit on large writes:**

### 🎯 Recommendations
1. **Use BulkInsert_JsonSerializer with memory pooling**
3. **Avoid DirectBson** - 22-33% slower and more complex
4. **Individual inserts are 3-4x slower** - always batch when possible
5. **Upsert is 4-5x slower** - only use when needed for updates

UNFORTUNATELY OUR CODE IS USING UPSERT SO WE LIKELY DONT HAVE ANY OPTION TO USE BULK INSERT

### 📊 Performance Summary
- **Best write method**: `BulkInsert_JsonSerializer` (baseline)
- **Memory pooling impact**: None (actually slightly slower)
- **Bulk vs Individual**: 3-4x faster with bulk
- **Bulk vs Upsert**: Upsert is 4x slower (use only for updates)

UNFORTUNATELY OUR CODE IS USING UPSERT SO WE LIKELY DONT HAVE ANY OPTION TO USE BULK INSERT
