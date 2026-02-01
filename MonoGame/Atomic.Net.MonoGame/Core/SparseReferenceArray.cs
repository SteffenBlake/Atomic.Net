using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core;

public sealed class SparseReferenceArray<T>(uint capacity) : IEnumerable<(ushort Index, T Value)>
    where T : class
{
    private readonly T?[] _sparse = new T?[capacity];
    private readonly int[] _denseIndices = [.. Enumerable.Repeat(-1, (int)capacity)];
    private readonly List<(ushort SparseIndex, T Value)> _dense = new((int)capacity);

    public uint Capacity => capacity;

    public T this[ushort index]
    {
        get => _sparse[index] ?? throw new InvalidOperationException(
            $"Attempted to access unset value at index {index}."
        );
        set
        {
            var denseIndex = _denseIndices[index];
            if (denseIndex < 0)
            {
                _denseIndices[index] = _dense.Count;
                _dense.Add((index, value));
            }
            else
            {
                _dense[denseIndex] = (index, value);
            }

            _sparse[index] = value;
        }
    }

    public T?[] Values => _sparse;

    public bool TryGetValue(
        ushort index, 
        [NotNullWhen(true)] out T? value
    )
    {
        if (_denseIndices[index] < 0)
        {
            value = null;
            return false;
        }

        value = _sparse[index]!;
        return true;
    }

    public bool HasValue(ushort index) => _denseIndices[index] >= 0;

    public bool Remove(ushort index)
    {
        var denseIndex = _denseIndices[index];
        if (denseIndex < 0)
        {
            return false;
        }

        var last = _dense.Last();
        _dense[denseIndex] = last;
        _denseIndices[last.SparseIndex] = denseIndex;

        _dense.RemoveAt(_dense.Count - 1);
        _denseIndices[index] = -1;
        _sparse[index] = null;

        return true;
    }

    public bool TryPop(
        [NotNullWhen(true)]
        out T? value,
        [NotNullWhen(true)]
        out ushort? index
    )
    {
        if (_dense.Count == 0)
        {
            value = null;
            index = null;
            return false;
        }

        var lastIndex = _dense.Count - 1;
        var (sparseIndex, val) = _dense[lastIndex];

        value = val;
        index = sparseIndex;

        // Clear sparse tracking and remove from dense
        _denseIndices[sparseIndex] = -1;
        _sparse[sparseIndex] = default;
        _dense.RemoveAt(lastIndex);

        return true;
    }

    public void Clear()
    {
        foreach(var (SparseIndex, _) in _dense)
        {
            _sparse[SparseIndex] = default;
            _denseIndices[SparseIndex] = -1;
        }
        _dense.Clear();
    }

    public int Count => _dense.Count;

    public IEnumerator<(ushort Index, T Value)> GetEnumerator() => _dense.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


