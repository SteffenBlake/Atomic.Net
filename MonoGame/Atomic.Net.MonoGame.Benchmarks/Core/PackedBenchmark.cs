using Atomic.Net.MonoGame.Core;
using BenchmarkDotNet.Attributes;

namespace Atomic.Net.MonoGame.Benchmarks.Core;

[MemoryDiagnoser]
public class PackedBenchmark
{
    [Params(1024, 4096, 16384)]
    public int Size;

    [Params(0.05f, 0.25f, 0.50f, 0.75f, 0.95f)]
    public float FillPercent;

    private int?[] _array = [];
    private SparseArray<int?> _sparseArray = new(0);

    private ushort _nextFreeIndex;
    private ushort _lastFreeIndex;

    [GlobalSetup]
    public void Setup()
    {
        _nextFreeIndex = (ushort)(Size * FillPercent);
        _lastFreeIndex = (ushort)(_nextFreeIndex + 50);

        _array = new int?[Size];
        _sparseArray = new SparseArray<int?>((ushort)Size);

        for (var i = 0; i < _nextFreeIndex; i++)
        {
            _array[i] = i;
            _sparseArray.Add(i);
        }
    }

   
    [Benchmark]
    public int Read_Array()
    {
        var result = 0;
        for (int i = 0; i < _array.Length; i++)
        {
            result += _array[i] ?? 0;
        }

        return result;
    }


    [Benchmark]
    public int Read_Sparse()
    {
        var result = 0;

        foreach(var (_, value) in _sparseArray.Index())
        {
            result += value ?? 0;
        }

        return result;
    }

    [Benchmark]
    public int RandAccess_Array()
    {
        var result = 0;
        for (var i = 0; i < Size; i += 8)
        {
            result += _array[i] ?? 0;
        }

        return result;
    }

    [Benchmark]
    public int RandAccess_Sparse()
    {
        var result = 0;

        for (ushort i = 0; i < Size; i += 8)
        {
            result += _sparseArray[i] ?? 0;
        }

        return result;
    }

    [Benchmark]
    public void Write_Array()
    {
        for (ushort i = _nextFreeIndex; i < _lastFreeIndex; i++)
        {
            _array[i] = i;
        }
    }

    [Benchmark]
    public void Write_Sparse()
    {
        for (ushort i = _nextFreeIndex; i < _lastFreeIndex; i++)
        {
            _sparseArray.Add(i);
        }
    }
}

[MemoryDiagnoser]
public class SparsityBenchmark
{
    [Params(1024, 4096, 16384)]
    public int Size;

    [Params(0.05f, 0.25f, 0.50f, 0.75f, 0.95f)]
    public float FillPercent;

    private int?[] _array = [];
    private SparseArray<int?> _sparseArray = new(0);
    private readonly Random _rng = new(42);

    [GlobalSetup]
    public void Setup()
    {
        _array = new int?[Size];
        _sparseArray = new SparseArray<int?>((ushort)Size);

        for (ushort i = 0; i < Size; i++)
        {
            if (_rng.NextDouble() < FillPercent)
            {
                _array[i] = i;
                _sparseArray[i] = i;
            }
        }
    }

    [Benchmark]
    public int Read_Array()
    {
        var result = 0;
        for (int i = 0; i < _array.Length; i++)
        {
            result += _array[i] ?? 0;
        }

        return result;
    }


    [Benchmark]
    public int Read_Sparse()
    {
        var result = 0;

        foreach(var (_, value) in _sparseArray.Index())
        {
            result += value ?? 0;
        }

        return result;
    }

    [Benchmark]
    public int RandAccess_Array()
    {
        var result = 0;
        for (var i = 0; i < Size; i += 8)
        {
            result += _array[i] ?? 0;
        }

        return result;
    }

    [Benchmark]
    public int RandAccess_Sparse()
    {
        var result = 0;

        for (ushort i = 0; i < Size; i += 8)
        {
            result += _sparseArray[i] ?? 0;
        }

        return result;
    }

}
