using Atomic.Net.MonoGame.Benchmarks.Core;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<PackedBenchmark>();
BenchmarkRunner.Run<SparsityBenchmark>();
