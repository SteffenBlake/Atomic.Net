// benchmarker: Comparing approaches for AND operations on sparse boolean data
// 1. Plain for loop with bool arrays
// 2. TensorPrimitives.BitwiseAnd with byte arrays (SIMD optimized)
// 3. SparseArray.Intersect (Select→Intersect)
// 4. SparseArray.Intersect (Intersect→Select)
// 5. BitArray.And (built-in .NET collection)
// All tests iterate results and accumulate matched indexes for fairness

using System.Collections;
using System.Numerics.Tensors;
using BenchmarkDotNet.Attributes;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Benchmarks.Core.Units;

/// <summary>
/// Glass-box benchmark comparing bitwise AND operations on sparse boolean data.
/// Tests include iteration overhead to ensure fair comparison.
/// All approaches write to output arrays and accumulate matched indexes.
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

    // SparseArrays for sparse intersection approach
    private SparseArray<bool> _leftSparse = null!;
    private SparseArray<bool> _rightSparse = null!;
    private SparseArray<bool> _resultSparse = null!;

    // BitArrays for BitArray approach
    private BitArray _leftBitArray = null!;
    private BitArray _rightBitArray = null!;
    private BitArray _resultBitArray = null!;

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

        // Initialize SparseArrays
        _leftSparse = new SparseArray<bool>((ushort)ArraySize);
        _rightSparse = new SparseArray<bool>((ushort)ArraySize);
        _resultSparse = new SparseArray<bool>((ushort)ArraySize);

        // Fill all structures with ~5% infill of true values using same seed for fair comparison
        for (int i = 0; i < ArraySize; i++)
        {
            // 5% chance of being true/1
            bool leftValue = rng.Next(100) < 5;
            bool rightValue = rng.Next(100) < 5;

            _leftBools[i] = leftValue;
            _rightBools[i] = rightValue;

            _leftBytes[i] = (byte)(leftValue ? 1 : 0);
            _rightBytes[i] = (byte)(rightValue ? 1 : 0);

            // Only set in SparseArray if true (sparse storage)
            if (leftValue)
            {
                _leftSparse.Set((ushort)i, true);
            }
            if (rightValue)
            {
                _rightSparse.Set((ushort)i, true);
            }
        }

        // Initialize BitArrays from bool arrays
        _leftBitArray = new BitArray(_leftBools);
        _rightBitArray = new BitArray(_rightBools);
        _resultBitArray = new BitArray(ArraySize);
    }

    [Benchmark(Baseline = true)]
    public int PlainForLoop_Bools()
    {
        int result = 0;
        
        // Perform AND operation
        for (int i = 0; i < ArraySize; i++)
        {
            _resultBools[i] = _leftBools[i] & _rightBools[i];
        }

        // Iterate results and accumulate matched indexes
        for (int i = 0; i < ArraySize; i++)
        {
            if (_resultBools[i])
            {
                result += i;
            }
        }

        return result;
    }

    [Benchmark]
    public int TensorPrimitives_BitwiseAnd_Bytes()
    {
        int result = 0;

        // Perform SIMD AND operation
        TensorPrimitives.BitwiseAnd(_leftBytes, _rightBytes, _resultBytes);

        // Iterate results and accumulate matched indexes
        for (int i = 0; i < ArraySize; i++)
        {
            if (_resultBytes[i] == 1)
            {
                result += i;
            }
        }

        return result;
    }

    [Benchmark]
    public int SparseArray_SelectThenIntersect()
    {
        int result = 0;

        // Clear previous results
        _resultSparse.Clear();

        // Select indexes first, then intersect
        var indexesA = _leftSparse.Select(static v => v.Index);
        var indexesB = _rightSparse.Select(static v => v.Index);

        foreach (var indexMatch in indexesA.Intersect(indexesB))
        {
            result += indexMatch;
            _resultSparse.Set(indexMatch, true);
        }

        return result;
    }

    [Benchmark]
    public int SparseArray_IntersectThenSelect()
    {
        int result = 0;

        // Clear previous results
        _resultSparse.Clear();

        // Intersect tuples first, then select indexes
        var intersected = _leftSparse.Intersect(_rightSparse);

        foreach (var match in intersected)
        {
            var indexMatch = match.Index;
            result += indexMatch;
            _resultSparse.Set(indexMatch, true);
        }

        return result;
    }

    [Benchmark]
    public int BitArray_And()
    {
        int result = 0;

        // Perform AND operation
        _resultBitArray = _leftBitArray.And(_rightBitArray);

        // Iterate results and accumulate matched indexes
        for (int i = 0; i < ArraySize; i++)
        {
            if (_resultBitArray[i])
            {
                result += i;
            }
        }

        return result;
    }
}
