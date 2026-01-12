```

BenchmarkDotNet v0.15.8, Linux 
Intel Core i9-7960X CPU 2.80GHz (Max: 1.20GHz) (Kaby Lake), 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4


```
| Method            | Size  | FillPercent | Mean         | Error      | StdDev     | Gen0   | Allocated |
|------------------ |------ |------------ |-------------:|-----------:|-----------:|-------:|----------:|
| **Read_Array**        | **1024**  | **0.05**        |    **409.20 ns** |   **0.815 ns** |   **0.681 ns** |      **-** |         **-** |
| Read_Sparse       | 1024  | 0.05        |    278.07 ns |   1.682 ns |   1.405 ns | 0.0062 |      72 B |
| RandAccess_Array  | 1024  | 0.05        |     58.10 ns |   0.438 ns |   0.410 ns |      - |         - |
| RandAccess_Sparse | 1024  | 0.05        |    337.20 ns |   1.093 ns |   1.023 ns |      - |         - |
| Write_Array       | 1024  | 0.05        |     32.61 ns |   0.272 ns |   0.242 ns |      - |         - |
| Write_Sparse      | 1024  | 0.05        |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **1024**  | **0.25**        |    **408.69 ns** |   **1.269 ns** |   **1.187 ns** |      **-** |         **-** |
| Read_Sparse       | 1024  | 0.25        |  1,324.75 ns |   6.105 ns |   5.711 ns | 0.0057 |      72 B |
| RandAccess_Array  | 1024  | 0.25        |     68.78 ns |   0.474 ns |   0.444 ns |      - |         - |
| RandAccess_Sparse | 1024  | 0.25        |    355.29 ns |   1.911 ns |   1.694 ns |      - |         - |
| Write_Array       | 1024  | 0.25        |     32.75 ns |   0.157 ns |   0.147 ns |      - |         - |
| Write_Sparse      | 1024  | 0.25        |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **1024**  | **0.5**         |    **410.01 ns** |   **1.314 ns** |   **1.229 ns** |      **-** |         **-** |
| Read_Sparse       | 1024  | 0.5         |  2,623.21 ns |  12.530 ns |  11.721 ns | 0.0038 |      72 B |
| RandAccess_Array  | 1024  | 0.5         |     69.79 ns |   0.481 ns |   0.450 ns |      - |         - |
| RandAccess_Sparse | 1024  | 0.5         |    542.94 ns |   2.443 ns |   2.285 ns |      - |         - |
| Write_Array       | 1024  | 0.5         |     32.77 ns |   0.226 ns |   0.211 ns |      - |         - |
| Write_Sparse      | 1024  | 0.5         |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **1024**  | **0.75**        |    **410.22 ns** |   **1.168 ns** |   **1.093 ns** |      **-** |         **-** |
| Read_Sparse       | 1024  | 0.75        |  3,927.78 ns |  28.022 ns |  26.212 ns |      - |      72 B |
| RandAccess_Array  | 1024  | 0.75        |     68.97 ns |   0.542 ns |   0.480 ns |      - |         - |
| RandAccess_Sparse | 1024  | 0.75        |    543.05 ns |   2.941 ns |   2.751 ns |      - |         - |
| Write_Array       | 1024  | 0.75        |     33.29 ns |   0.215 ns |   0.201 ns |      - |         - |
| Write_Sparse      | 1024  | 0.75        |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **1024**  | **0.95**        |    **410.72 ns** |   **1.627 ns** |   **1.522 ns** |      **-** |         **-** |
| Read_Sparse       | 1024  | 0.95        |  4,946.60 ns |  26.696 ns |  24.971 ns |      - |      72 B |
| RandAccess_Array  | 1024  | 0.95        |     69.40 ns |   0.492 ns |   0.460 ns |      - |         - |
| RandAccess_Sparse | 1024  | 0.95        |    536.19 ns |   2.761 ns |   2.582 ns |      - |         - |
| Write_Array       | 1024  | 0.95        |     32.55 ns |   0.234 ns |   0.219 ns |      - |         - |
| Write_Sparse      | 1024  | 0.95        |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **4096**  | **0.05**        |  **1,633.80 ns** |   **4.697 ns** |   **4.163 ns** |      **-** |         **-** |
| Read_Sparse       | 4096  | 0.05        |  1,062.44 ns |   6.837 ns |   6.395 ns | 0.0057 |      72 B |
| RandAccess_Array  | 4096  | 0.05        |    265.51 ns |   1.639 ns |   1.533 ns |      - |         - |
| RandAccess_Sparse | 4096  | 0.05        |  1,331.64 ns |   6.124 ns |   5.428 ns |      - |         - |
| Write_Array       | 4096  | 0.05        |     32.73 ns |   0.166 ns |   0.156 ns |      - |         - |
| Write_Sparse      | 4096  | 0.05        |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **4096**  | **0.25**        |  **1,628.63 ns** |   **3.957 ns** |   **3.508 ns** |      **-** |         **-** |
| Read_Sparse       | 4096  | 0.25        |  5,218.38 ns |  30.420 ns |  26.967 ns |      - |      72 B |
| RandAccess_Array  | 4096  | 0.25        |    265.93 ns |   1.794 ns |   1.678 ns |      - |         - |
| RandAccess_Sparse | 4096  | 0.25        |  1,416.13 ns |   5.536 ns |   5.178 ns |      - |         - |
| Write_Array       | 4096  | 0.25        |     32.69 ns |   0.185 ns |   0.173 ns |      - |         - |
| Write_Sparse      | 4096  | 0.25        |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **4096**  | **0.5**         |  **1,628.61 ns** |   **4.640 ns** |   **4.340 ns** |      **-** |         **-** |
| Read_Sparse       | 4096  | 0.5         | 10,434.98 ns |  48.765 ns |  45.615 ns |      - |      72 B |
| RandAccess_Array  | 4096  | 0.5         |    264.45 ns |   1.238 ns |   1.158 ns |      - |         - |
| RandAccess_Sparse | 4096  | 0.5         |  1,507.03 ns |   5.782 ns |   5.408 ns |      - |         - |
| Write_Array       | 4096  | 0.5         |     33.61 ns |   0.391 ns |   0.366 ns |      - |         - |
| Write_Sparse      | 4096  | 0.5         |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **4096**  | **0.75**        |  **1,632.50 ns** |   **4.612 ns** |   **4.314 ns** |      **-** |         **-** |
| Read_Sparse       | 4096  | 0.75        | 15,664.22 ns |  94.100 ns |  88.021 ns |      - |      72 B |
| RandAccess_Array  | 4096  | 0.75        |    265.04 ns |   1.603 ns |   1.421 ns |      - |         - |
| RandAccess_Sparse | 4096  | 0.75        |  2,139.47 ns |  12.125 ns |  11.342 ns |      - |         - |
| Write_Array       | 4096  | 0.75        |     33.17 ns |   0.162 ns |   0.126 ns |      - |         - |
| Write_Sparse      | 4096  | 0.75        |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **4096**  | **0.95**        |  **1,633.61 ns** |   **3.484 ns** |   **3.259 ns** |      **-** |         **-** |
| Read_Sparse       | 4096  | 0.95        | 19,805.17 ns | 112.212 ns | 104.963 ns |      - |      72 B |
| RandAccess_Array  | 4096  | 0.95        |    227.00 ns |   0.526 ns |   0.492 ns |      - |         - |
| RandAccess_Sparse | 4096  | 0.95        |  2,113.07 ns |  11.052 ns |  10.338 ns |      - |         - |
| Write_Array       | 4096  | 0.95        |     33.22 ns |   0.185 ns |   0.173 ns |      - |         - |
| Write_Sparse      | 4096  | 0.95        |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **16384** | **0.05**        |  **6,510.96 ns** |  **25.199 ns** |  **23.571 ns** |      **-** |         **-** |
| Read_Sparse       | 16384 | 0.05        |  4,180.71 ns |  23.366 ns |  21.857 ns |      - |      72 B |
| RandAccess_Array  | 16384 | 0.05        |  1,049.79 ns |   4.479 ns |   4.190 ns |      - |         - |
| RandAccess_Sparse | 16384 | 0.05        |  5,312.15 ns |  26.115 ns |  24.428 ns |      - |         - |
| Write_Array       | 16384 | 0.05        |     35.77 ns |   0.203 ns |   0.190 ns |      - |         - |
| Write_Sparse      | 16384 | 0.05        |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **16384** | **0.25**        |  **6,732.67 ns** |  **17.332 ns** |  **16.212 ns** |      **-** |         **-** |
| Read_Sparse       | 16384 | 0.25        | 20,829.65 ns | 111.676 ns | 104.461 ns |      - |      72 B |
| RandAccess_Array  | 16384 | 0.25        |  1,064.88 ns |   6.956 ns |   6.507 ns |      - |         - |
| RandAccess_Sparse | 16384 | 0.25        |  5,641.88 ns |  23.539 ns |  20.867 ns |      - |         - |
| Write_Array       | 16384 | 0.25        |     33.18 ns |   0.198 ns |   0.185 ns |      - |         - |
| Write_Sparse      | 16384 | 0.25        |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **16384** | **0.5**         |  **6,919.65 ns** |  **12.685 ns** |  **11.245 ns** |      **-** |         **-** |
| Read_Sparse       | 16384 | 0.5         | 41,634.11 ns | 173.091 ns | 153.441 ns |      - |      72 B |
| RandAccess_Array  | 16384 | 0.5         |  1,079.44 ns |   6.173 ns |   5.774 ns |      - |         - |
| RandAccess_Sparse | 16384 | 0.5         |  6,608.24 ns |  24.949 ns |  23.337 ns |      - |         - |
| Write_Array       | 16384 | 0.5         |     32.15 ns |   0.203 ns |   0.190 ns |      - |         - |
| Write_Sparse      | 16384 | 0.5         |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **16384** | **0.75**        |  **7,131.56 ns** |  **19.424 ns** |  **18.169 ns** |      **-** |         **-** |
| Read_Sparse       | 16384 | 0.75        | 62,547.65 ns | 469.045 ns | 438.745 ns |      - |      72 B |
| RandAccess_Array  | 16384 | 0.75        |  1,059.80 ns |   6.668 ns |   6.237 ns |      - |         - |
| RandAccess_Sparse | 16384 | 0.75        |  6,767.67 ns |  12.212 ns |  10.826 ns |      - |         - |
| Write_Array       | 16384 | 0.75        |     33.00 ns |   0.262 ns |   0.245 ns |      - |         - |
| Write_Sparse      | 16384 | 0.75        |           NA |         NA |         NA |     NA |        NA |
| **Read_Array**        | **16384** | **0.95**        |  **7,307.50 ns** |  **17.745 ns** |  **16.598 ns** |      **-** |         **-** |
| Read_Sparse       | 16384 | 0.95        | 79,150.25 ns | 422.316 ns | 395.035 ns |      - |      72 B |
| RandAccess_Array  | 16384 | 0.95        |  1,107.53 ns |   6.893 ns |   6.447 ns |      - |         - |
| RandAccess_Sparse | 16384 | 0.95        |  6,915.82 ns |  18.259 ns |  17.079 ns |      - |         - |
| Write_Array       | 16384 | 0.95        |     32.35 ns |   0.192 ns |   0.160 ns |      - |         - |
| Write_Sparse      | 16384 | 0.95        |           NA |         NA |         NA |     NA |        NA |

Benchmarks with issues:
  PackedBenchmark.Write_Sparse: DefaultJob [Size=1024, FillPercent=0.05]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=1024, FillPercent=0.25]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=1024, FillPercent=0.5]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=1024, FillPercent=0.75]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=1024, FillPercent=0.95]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=4096, FillPercent=0.05]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=4096, FillPercent=0.25]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=4096, FillPercent=0.5]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=4096, FillPercent=0.75]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=4096, FillPercent=0.95]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=16384, FillPercent=0.05]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=16384, FillPercent=0.25]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=16384, FillPercent=0.5]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=16384, FillPercent=0.75]
  PackedBenchmark.Write_Sparse: DefaultJob [Size=16384, FillPercent=0.95]
