// benchmarker: Comparing TensorPrimitives.BitwiseAnd (bytes) vs plain for loop (bools) for AND operations
// Testing to find the performance breakpoint where one approach outperforms the other
// TensorPrimitives uses byte arrays (0 for false, 1 for true)
// Plain loop uses bool arrays (true/false)
// Both have ~5% infill of true values

using System.Numerics.Tensors;
using BenchmarkDotNet.Attributes;

namespace Atomic.Net.MonoGame.Benchmarks.Core.Units;

/// <summary>
/// Glass-box benchmark comparing bitwise AND operations.
/// TensorPrimitives.BitwiseAnd uses byte arrays (0/1).
/// Plain for loop uses bool arrays (true/false).
/// Tests to find performance crossover point.
/// </summary>
[MemoryDiagnoser]
public class BitwiseAndBenchmark
{
    // Testing multiple scales to find the breakpoint
    [Params(100, 500, 1000, 5000, 10000, 50000, 100000)]
    public int ArraySize { get; set; }

    // Bool arrays for plain loop approach
    private bool[] _leftBools = null!;
    private bool[] _rightBools = null!;
    private bool[] _resultBools = null!;

    // Byte arrays for TensorPrimitives approach
    private byte[] _leftBytes = null!;
    private byte[] _rightBytes = null!;
    private byte[] _resultBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Use pre-seeded Random for reproducible results
        var rng = new Random(42);

        // Initialize bool arrays for plain loop
        _leftBools = new bool[ArraySize];
        _rightBools = new bool[ArraySize];
        _resultBools = new bool[ArraySize];

        // Initialize byte arrays for TensorPrimitives
        _leftBytes = new byte[ArraySize];
        _rightBytes = new byte[ArraySize];
        _resultBytes = new byte[ArraySize];

        // Fill both with ~5% infill of true values using same seed for fair comparison
        for (int i = 0; i < ArraySize; i++)
        {
            // 5% chance of being true/1
            bool leftValue = rng.Next(100) < 5;
            bool rightValue = rng.Next(100) < 5;

            _leftBools[i] = leftValue;
            _rightBools[i] = rightValue;

            _leftBytes[i] = (byte)(leftValue ? 1 : 0);
            _rightBytes[i] = (byte)(rightValue ? 1 : 0);
        }
    }

    [Benchmark(Baseline = true)]
    public bool[] PlainForLoop_Bools()
    {
        for (int i = 0; i < ArraySize; i++)
        {
            _resultBools[i] = _leftBools[i] & _rightBools[i];
        }
        return _resultBools;
    }

    [Benchmark]
    public byte[] TensorPrimitives_BitwiseAnd_Bytes()
    {
        TensorPrimitives.BitwiseAnd(_leftBytes, _rightBytes, _resultBytes);
        return _resultBytes;
    }
}
