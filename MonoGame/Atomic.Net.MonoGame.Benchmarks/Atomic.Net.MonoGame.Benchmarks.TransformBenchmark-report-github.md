```

BenchmarkDotNet v0.15.8, Linux 
Intel Core i9-7960X CPU 2.80GHz (Max: 1.20GHz) (Kaby Lake), 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method            | EntityCount | Mean        | Error      | StdDev     | Allocated |
|------------------ |------------ |------------:|-----------:|-----------:|----------:|
| **Manual_MonoGame**   | **64**          |    **20.72 μs** |   **0.397 μs** |   **0.820 μs** |         **-** |
| SIMD_EntitySystem | 64          |   885.07 μs |  17.640 μs |  28.983 μs |   28280 B |
| **Manual_MonoGame**   | **256**         |    **71.55 μs** |   **1.433 μs** |   **2.655 μs** |         **-** |
| SIMD_EntitySystem | 256         | 3,283.53 μs | 212.090 μs | 562.434 μs |  414416 B |
| **Manual_MonoGame**   | **470**         |   **125.07 μs** |   **2.401 μs** |   **2.569 μs** |         **-** |
| SIMD_EntitySystem | 470         | 9,804.16 μs | 118.504 μs | 105.051 μs | 1380080 B |
