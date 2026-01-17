```

BenchmarkDotNet v0.15.8, Linux 
Intel Core i9-7960X CPU 2.80GHz (Max: 1.20GHz) (Kaby Lake), 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method            | EntityCount | Mean          | Error        | StdDev       | Median        | Allocated |
|------------------ |------------ |--------------:|-------------:|-------------:|--------------:|----------:|
| **Manual_MonoGame**   | **200**         |      **65.62 μs** |     **2.672 μs** |     **7.753 μs** |      **61.89 μs** |         **-** |
| SIMD_EntitySystem | 200         |   2,165.11 μs |   192.935 μs |   504.878 μs |   2,001.05 μs |         - |
| **Manual_MonoGame**   | **1000**        |     **224.87 μs** |    **18.568 μs** |    **54.162 μs** |     **196.91 μs** |         **-** |
| SIMD_EntitySystem | 1000        |  21,664.71 μs |   246.350 μs |   230.436 μs |  21,736.45 μs |         - |
| **Manual_MonoGame**   | **8000**        |   **1,235.67 μs** |    **47.332 μs** |   **134.273 μs** |   **1,268.52 μs** |         **-** |
| SIMD_EntitySystem | 8000        | 336,550.10 μs | 4,123.999 μs | 3,857.591 μs | 337,638.03 μs |         - |
