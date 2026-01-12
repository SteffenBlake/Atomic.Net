```

BenchmarkDotNet v0.15.8, Linux 
Intel Core i9-7960X CPU 2.80GHz (Max: 1.20GHz) (Kaby Lake), 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4


```
| Method            | FillPercent | Mean        | Error    | StdDev   | Allocated |
|------------------ |------------ |------------:|---------:|---------:|----------:|
| **Read_Array**        | **0.05**        |  **3,562.5 ns** | **13.31 ns** | **12.45 ns** |         **-** |
| Read_Sparse       | 0.05        |    155.6 ns |  0.46 ns |  0.38 ns |         - |
| RandAccess_Array  | 0.05        |    540.4 ns |  3.85 ns |  3.60 ns |         - |
| RandAccess_Sparse | 0.05        |    342.8 ns |  0.99 ns |  0.82 ns |         - |
| **Read_Array**        | **0.1**         |  **3,339.4 ns** | **22.23 ns** | **20.79 ns** |         **-** |
| Read_Sparse       | 0.1         |    299.1 ns |  0.95 ns |  0.84 ns |         - |
| RandAccess_Array  | 0.1         |    546.8 ns |  3.60 ns |  3.36 ns |         - |
| RandAccess_Sparse | 0.1         |    345.3 ns |  1.29 ns |  1.14 ns |         - |
| **Read_Array**        | **0.15**        |  **3,531.3 ns** | **36.83 ns** | **34.45 ns** |         **-** |
| Read_Sparse       | 0.15        |    435.3 ns |  1.29 ns |  1.21 ns |         - |
| RandAccess_Array  | 0.15        |    544.4 ns |  2.53 ns |  2.36 ns |         - |
| RandAccess_Sparse | 0.15        |    345.4 ns |  1.42 ns |  1.33 ns |         - |
| **Read_Array**        | **0.2**         |  **3,848.4 ns** | **47.58 ns** | **44.51 ns** |         **-** |
| Read_Sparse       | 0.2         |    574.3 ns |  2.16 ns |  1.69 ns |         - |
| RandAccess_Array  | 0.2         |    374.6 ns |  3.09 ns |  2.89 ns |         - |
| RandAccess_Sparse | 0.2         |    346.0 ns |  1.64 ns |  1.53 ns |         - |
| **Read_Array**        | **0.25**        | **13,411.6 ns** | **42.09 ns** | **35.14 ns** |         **-** |
| Read_Sparse       | 0.25        |    723.1 ns |  2.84 ns |  2.66 ns |         - |
| RandAccess_Array  | 0.25        |    557.4 ns |  2.61 ns |  2.31 ns |         - |
| RandAccess_Sparse | 0.25        |    336.2 ns |  1.11 ns |  1.04 ns |         - |
| **Read_Array**        | **0.3**         |  **6,098.6 ns** | **39.97 ns** | **37.39 ns** |         **-** |
| Read_Sparse       | 0.3         |    853.4 ns |  3.18 ns |  2.98 ns |         - |
| RandAccess_Array  | 0.3         |    566.2 ns |  3.70 ns |  3.46 ns |         - |
| RandAccess_Sparse | 0.3         |    336.2 ns |  1.72 ns |  1.61 ns |         - |
| **Read_Array**        | **0.9**         |  **7,041.3 ns** | **22.09 ns** | **20.67 ns** |         **-** |
| Read_Sparse       | 0.9         |  2,486.5 ns | 12.98 ns | 12.14 ns |         - |
| RandAccess_Array  | 0.9         |    421.2 ns |  1.25 ns |  1.10 ns |         - |
| RandAccess_Sparse | 0.9         |    335.7 ns |  1.36 ns |  1.20 ns |         - |
