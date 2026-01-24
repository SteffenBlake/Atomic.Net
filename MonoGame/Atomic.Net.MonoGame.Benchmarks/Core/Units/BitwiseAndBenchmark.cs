// benchmarker: Comparing TensorPrimitives.BitwiseAnd vs plain for loop for AND operations on bool arrays
// Testing the hypothesis that TensorPrimitives may use SIMD optimizations for larger arrays,
// but could have overhead for smaller arrays where a simple loop might be faster.
//
// Goal: Find the "breakpoint" where one approach becomes faster than the other,
// to determine if a hybrid strategy is needed (choose approach based on array size).
//
// FINDING: TensorPrimitives.BitwiseAnd does NOT support bool type directly.
// It requires IBitwiseOperators constraint. For bool arrays, we need to either:
// 1. Use byte arrays where 0=false, 1=true
// 2. Convert bool[] to byte[] for TensorPrimitives, then convert back
// 
// This benchmark compares pure for-loop on bool[] vs TensorPrimitives on byte[].

using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Atomic.Net.MonoGame.Benchmarks.Core.Units;

[MemoryDiagnoser]
public class BitwiseAndBenchmark
{
    // Test multiple array sizes to find the breakpoint
    [Params(10, 50, 100, 500, 1000, 5000, 10000)]
    public int ArraySize { get; set; }

    private bool[] _leftBool = null!;
    private bool[] _rightBool = null!;
    private bool[] _resultBool = null!;
    
    // For TensorPrimitives approach, we need byte arrays
    private byte[] _leftByte = null!;
    private byte[] _rightByte = null!;
    private byte[] _resultByte = null!;
    
    private readonly Random _rng = new(42); // Fixed seed for reproducibility

    [GlobalSetup]
    public void Setup()
    {
        _leftBool = new bool[ArraySize];
        _rightBool = new bool[ArraySize];
        _resultBool = new bool[ArraySize];
        
        _leftByte = new byte[ArraySize];
        _rightByte = new byte[ArraySize];
        _resultByte = new byte[ArraySize];

        // Fill with random boolean values
        for (int i = 0; i < ArraySize; i++)
        {
            var leftVal = _rng.Next(2) == 1;
            var rightVal = _rng.Next(2) == 1;
            
            _leftBool[i] = leftVal;
            _rightBool[i] = rightVal;
            
            _leftByte[i] = (byte)(leftVal ? 1 : 0);
            _rightByte[i] = (byte)(rightVal ? 1 : 0);
        }
    }

    // ========== BASELINE: Plain For Loop on bool[] ==========

    /// <summary>
    /// Simple for loop performing AND operation element-by-element on bool arrays.
    /// Should have minimal overhead, but no SIMD optimizations.
    /// This is the most straightforward approach for bool arrays.
    /// </summary>
    [Benchmark(Baseline = true)]
    public bool[] ForLoop_BoolArray()
    {
        for (int i = 0; i < ArraySize; i++)
        {
            _resultBool[i] = _leftBool[i] & _rightBool[i];
        }
        return _resultBool;
    }

    // ========== TENSOR PRIMITIVES ON BYTE ARRAYS ==========

    /// <summary>
    /// Using TensorPrimitives.BitwiseAnd on byte arrays (0=false, 1=true).
    /// May leverage SIMD instructions for large arrays.
    /// This approach requires byte arrays instead of bool arrays.
    /// </summary>
    [Benchmark]
    public byte[] TensorPrimitives_ByteArray()
    {
        TensorPrimitives.BitwiseAnd(_leftByte, _rightByte, _resultByte);
        return _resultByte;
    }
    
    // ========== TENSOR PRIMITIVES WITH CONVERSION ==========

    /// <summary>
    /// Converting bool[] to byte[], using TensorPrimitives, then converting back.
    /// This shows the full cost of using TensorPrimitives with bool arrays.
    /// </summary>
    [Benchmark]
    public bool[] TensorPrimitives_WithConversion()
    {
        // Convert bool to byte
        for (int i = 0; i < ArraySize; i++)
        {
            _leftByte[i] = (byte)(_leftBool[i] ? 1 : 0);
            _rightByte[i] = (byte)(_rightBool[i] ? 1 : 0);
        }
        
        // Use TensorPrimitives
        TensorPrimitives.BitwiseAnd(_leftByte, _rightByte, _resultByte);
        
        // Convert back to bool
        for (int i = 0; i < ArraySize; i++)
        {
            _resultBool[i] = _resultByte[i] != 0;
        }
        
        return _resultBool;
    }

    // ========== UNSAFE POINTER CASTING (if applicable) ==========

    /// <summary>
    /// Attempting to use TensorPrimitives by reinterpreting bool[] memory as byte[].
    /// This tests if we can avoid conversion overhead using unsafe memory reinterpretation.
    /// Note: bool is 1 byte in .NET, same as byte, so this should be safe.
    /// </summary>
    [Benchmark]
    public bool[] TensorPrimitives_UnsafeCast()
    {
        // Cast bool arrays to byte spans using MemoryMarshal
        // Need to use AsSpan() to get Span<T>, then cast
        var leftSpan = MemoryMarshal.Cast<bool, byte>(_leftBool.AsSpan());
        var rightSpan = MemoryMarshal.Cast<bool, byte>(_rightBool.AsSpan());
        var resultSpan = MemoryMarshal.Cast<bool, byte>(_resultBool.AsSpan());
        
        TensorPrimitives.BitwiseAnd(leftSpan, rightSpan, resultSpan);
        
        return _resultBool;
    }

    // ========== HYBRID APPROACH ==========

    /// <summary>
    /// Hybrid approach that switches between ForLoop and TensorPrimitives
    /// based on array size. Breakpoint will be adjusted after initial results.
    /// </summary>
    [Benchmark]
    public bool[] Hybrid_Approach()
    {
        // Placeholder breakpoint - will be tuned based on results
        const int breakpoint = 1000;

        if (ArraySize < breakpoint)
        {
            // Use for loop for small arrays
            for (int i = 0; i < ArraySize; i++)
            {
                _resultBool[i] = _leftBool[i] & _rightBool[i];
            }
        }
        else
        {
            // Use TensorPrimitives with unsafe cast for large arrays
            var leftSpan = MemoryMarshal.Cast<bool, byte>(_leftBool.AsSpan());
            var rightSpan = MemoryMarshal.Cast<bool, byte>(_rightBool.AsSpan());
            var resultSpan = MemoryMarshal.Cast<bool, byte>(_resultBool.AsSpan());
            
            TensorPrimitives.BitwiseAnd(leftSpan, rightSpan, resultSpan);
        }
        
        return _resultBool;
    }
}
