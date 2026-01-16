using Atomic.Net.MonoGame.Core;
using BenchmarkDotNet.Attributes;

namespace Atomic.Net.MonoGame.Benchmarks.Core;

[MemoryDiagnoser]
public class SparseArrayBenchmark
{
    public int Capacity = 4096;

    [Params(0.05f, 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.90f)]
    public float FillPercent;

    private int?[] _array = [];
    private SparseArray<int> _sparseArray = new(0);
    private readonly Random _rng = new(42);

    [GlobalSetup]
    public void Setup()
    {
        _array = new int?[Capacity];
        _sparseArray = new SparseArray<int>((ushort)Capacity);

        for (ushort i = 0; i < Capacity; i++)
        {
            if (_rng.NextDouble() < FillPercent)
            {
                _array[i] = i;
                _sparseArray.Set(i, i);
            }
        }
    }

    [Benchmark]
    public int Read_Array()
    {
        var result = 0;
        for (ushort i = 0; i < _array.Length; i++)
        {
            var value = _array[i];
            if (value != null)
            {
                result += value.Value;
                result += i;
            }
        }

        return result;
    }


    [Benchmark]
    public int Read_Sparse()
    {
        var result = 0;

        foreach(var (i, value) in _sparseArray)
        {
            result += value;
            result += i;
        }

        return result;
    }

    [Benchmark]
    public int RandAccess_Array()
    {
        var result = 0;
        for (ushort i = 0; i < Capacity; i += 8)
        {
            var value = _array[i];
            if (value != null)
            {
                result += value.Value;
            }
        }

        return result;
    }

    [Benchmark]
    public int RandAccess_Sparse()
    {
        var result = 0;

        for (ushort i = 0; i < Capacity; i += 8)
        {
            var value = _sparseArray[i];
            result += value;
        }

        return result;
    }

}
