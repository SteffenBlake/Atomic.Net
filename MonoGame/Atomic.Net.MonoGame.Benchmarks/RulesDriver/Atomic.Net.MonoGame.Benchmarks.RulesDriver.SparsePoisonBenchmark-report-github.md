# SparsePoisonBenchmark - RulesDriver with JsonLogic

**Baseline performance before JsonLogic → JsonExpression migration**

## Benchmark Configuration

- **Test:** 100 frames of poison damage simulation
- **Entities:** 7,900 scene entities
- **Poisoned:** ~25% with #poisoned tag (~1,975 entities)
- **PoisonStacks:** ~50% of poisoned have stacks (90-100 range)
- **Mutations:** ~967 property mutations per frame
- **Duration:** ~1.67 seconds simulated gameplay (60 FPS)

## Environment

```
BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 13 (trixie)
Intel Core i9-7960X CPU 2.80GHz (Max: 1.20GHz) (Kaby Lake), 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.200
  [Host]     : .NET 10.0.4 (10.0.4, 10.0.426.12010), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.4 (10.0.4, 10.0.426.12010), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1
```

## Results

| Method              | Mean     | Error   | StdDev  | Gen0       | Gen1       | Allocated |
|-------------------- |---------:|--------:|--------:|-----------:|-----------:|----------:|
| RunPoisonSimulation | 681.7 ms | 4.24 ms | 3.76 ms | 33000.0000 | 24000.0000 | 365.77 MB |

## Key Findings

- **Current system (JsonLogic):** 365.77 MB allocated per 100 frames
- Uses JsonNode for entity serialization (allocates per frame)
- JsonLogic.Apply() with dynamic JsonObject context
- **Target improvement:** Eliminate allocations via struct-based JsonExpression compilation
