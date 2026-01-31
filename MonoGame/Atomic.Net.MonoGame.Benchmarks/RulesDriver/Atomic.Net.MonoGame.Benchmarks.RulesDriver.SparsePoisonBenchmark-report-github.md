```

BenchmarkDotNet v0.15.8, Linux 
Intel Core i9-7960X CPU 2.80GHz (Max: 1.20GHz) (Kaby Lake), 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method              | Mean     | Error   | StdDev  | Gen0       | Gen1       | Allocated |
|-------------------- |---------:|--------:|--------:|-----------:|-----------:|----------:|
| RunPoisonSimulation | 391.9 ms | 4.41 ms | 3.91 ms | 33000.0000 | 24000.0000 | 368.22 MB |
