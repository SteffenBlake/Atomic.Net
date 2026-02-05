// benchmarker: Comparing approaches for AND operations on sparse boolean data
// 1. Plain for loop with bool arrays
// 2. TensorPrimitives.BitwiseAnd with byte arrays (SIMD optimized)
// 3. SparseArray.Intersect (Select→Intersect)
// 4. SparseArray.Intersect (Intersect→Select)
// 5. BitArray.And (built-in .NET collection)
// 6. TensorPrimitives with copy overhead (bool[] to byte[])
// 7. BitArray with copy overhead (bool[] to BitArray)
// All tests iterate results and accumulate matched indexes for fairness

using System.Collections;
using System.Numerics.Tensors;
using System.Runtime.InteropServices;
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
    [Params(100, 1000, 10000)]
    public ushort ArraySize { get; set; }

    // Bool arrays for plain loop approach
    private bool[] _leftBools = null!;
    private bool[] _rightBools = null!;

    // Byte arrays for TensorPrimitives approach
    private byte[] _leftBytes = null!;
    private byte[] _rightBytes = null!;
    private byte[] _resultBytes = null!;

    // Byte arrays for TensorPrimitives with copy approach
    private byte[] _leftBytesCopied = null!;
    private byte[] _rightBytesCopied = null!;

    // SparseArrays for sparse intersection approach
    private SparseArray<bool> _leftSparse = null!;
    private SparseArray<bool> _rightSparse = null!;
    private SparseArray<bool> _resultSparse = null!;

    // BitArrays for BitArray approach
    private BitArray _leftBitArray = null!;
    private BitArray _rightBitArray = null!;
    private BitArray _resultBitArray = null!;

    // BitArrays for BitArray with copy approach
    private BitArray _leftBitArrayCopied = null!;
    private BitArray _rightBitArrayCopied = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Use pre-seeded Random for reproducible results
        var rng = new Random(42);

        // Initialize bool arrays for plain loop
        _leftBools = new bool[ArraySize];
        _rightBools = new bool[ArraySize];

        // Initialize byte arrays for TensorPrimitives
        _leftBytes = new byte[ArraySize];
        _rightBytes = new byte[ArraySize];
        _resultBytes = new byte[ArraySize];

        // Initialize byte arrays for copy test (pre-allocated, will be reused)
        _leftBytesCopied = new byte[ArraySize];
        _rightBytesCopied = new byte[ArraySize];

        // Initialize SparseArrays
        _leftSparse = new SparseArray<bool>(ArraySize);
        _rightSparse = new SparseArray<bool>(ArraySize);
        _resultSparse = new SparseArray<bool>(ArraySize);

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
                _leftSparse.Set((uint)i, true);
            }
            if (rightValue)
            {
                _rightSparse.Set((uint)i, true);
            }
        }

        // Initialize BitArrays from bool arrays
        _leftBitArray = new BitArray(_leftBools);
        _rightBitArray = new BitArray(_rightBools);
        _resultBitArray = new BitArray(ArraySize);

        // Initialize BitArrays for copy test (pre-allocated, will be reused)
        _leftBitArrayCopied = new BitArray(ArraySize);
        _rightBitArrayCopied = new BitArray(ArraySize);
    }

    [Benchmark(Baseline = true)]
    public int PlainForLoop_Bools()
    {
        int result = 0;

        // Perform AND operation
        for (ushort i = 0; i < ArraySize; i++)
        {
            if (_leftBools[i] & _rightBools[i])
            {
                _resultSparse.Set(i, true);
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
        for (ushort i = 0; i < ArraySize; i++)
        {
            if (_resultBytes[i] == 1)
            {
                _resultSparse.Set(i, true);
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
            result += (int)indexMatch;
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
            result += (int)indexMatch;
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
        for (ushort i = 0; i < ArraySize; i++)
        {
            if (_resultBitArray[i])
            {
                result += i;
                _resultSparse.Set(i, true);
            }
        }

        return result;
    }

    [Benchmark]
    public int TensorPrimitives_BitwiseAnd_Bytes_Copy()
    {
        int result = 0;

        // Copy bool[] to byte[] using MemoryMarshal.Cast (conversion overhead)
        MemoryMarshal.Cast<bool, byte>(_leftBools).CopyTo(_leftBytesCopied);
        MemoryMarshal.Cast<bool, byte>(_rightBools).CopyTo(_rightBytesCopied);

        // Perform SIMD AND operation
        TensorPrimitives.BitwiseAnd(_leftBytesCopied, _rightBytesCopied, _resultBytes);

        // Iterate results and accumulate matched indexes
        for (ushort i = 0; i < ArraySize; i++)
        {
            if (_resultBytes[i] == 1)
            {
                result += i;
                _resultSparse.Set(i, true);
            }
        }

        return result;
    }

    [Benchmark]
    public int BitArray_And_Copy()
    {
        int result = 0;

        // Copy bool[] to BitArray using constructor (conversion overhead)
        _leftBitArrayCopied = new BitArray(_leftBools);
        _rightBitArrayCopied = new BitArray(_rightBools);

        // Perform AND operation
        _resultBitArray = _leftBitArrayCopied.And(_rightBitArrayCopied);

        // Iterate results and accumulate matched indexes
        for (ushort i = 0; i < ArraySize; i++)
        {
            if (_resultBitArray[i])
            {
                result += i;
                _resultSparse.Set(i, true);
            }
        }

        return result;
    }
}
