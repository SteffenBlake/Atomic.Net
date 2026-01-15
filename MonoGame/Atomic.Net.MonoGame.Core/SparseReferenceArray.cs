using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core;

public sealed class SparseReferenceArray<T>(ushort capacity) : IEnumerable<(ushort Index, T Value)>
    where T : class
{
    private readonly T?[] _sparse = new T?[capacity];
    private readonly int[] _denseIndices = [.. Enumerable.Repeat(-1, capacity)];
    private readonly List<(ushort SparseIndex, T Value)> _dense = new(capacity);

    public ushort Capacity => capacity;

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

    public int Count => _dense.Count;

    public IEnumerator<(ushort Index, T Value)> GetEnumerator() => _dense.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


