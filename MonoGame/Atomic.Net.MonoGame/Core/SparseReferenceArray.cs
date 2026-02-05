using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core;

public sealed class SparseReferenceArray<T> : IEnumerable<(uint Index, T Value)>
    where T : class
{
    private readonly T?[] _sparse;
    private readonly int[] _denseIndices;
    private readonly List<(uint SparseIndex, T Value)> _dense;

    public uint Capacity { get; }

    public SparseReferenceArray(uint capacity)
    {
        Capacity = capacity;
        _sparse = new T?[capacity];
        _denseIndices = new int[capacity];
        _dense = new List<(uint, T)>((int)capacity);

        // Initialize all dense indices to -1 (sentinel for "not set")
        Array.Fill(_denseIndices, -1);
    }

    public T this[uint index]
    {
        get => _sparse[index] ?? throw new InvalidOperationException($"No value at index {index}");
        set => Set(index, value);
    }

    public bool TryGetValue(
        uint index,
        [NotNullWhen(true)]
        out T? value
    )
    {
        if (_denseIndices[index] < 0)
        {
            value = null;
            return false;
        }
        value = _sparse[index];
        return value != null;
    }

    public int Count => _dense.Count;

    public bool HasValue(uint index) => _denseIndices[index] >= 0 && _sparse[index] != null;

    public void Set(uint index, T value)
    {
        var denseIndex = _denseIndices[index];
        if (denseIndex < 0)
        {
            // Not set yet
            _denseIndices[index] = _dense.Count;
            _dense.Add((index, value));
        }
        else
        {
            // Overwrite existing value
            _dense[denseIndex] = (index, value);
        }

        _sparse[index] = value;
    }

    public bool Remove(uint index)
    {
        var denseIndex = _denseIndices[index];
        if (denseIndex < 0 || _sparse[index] == null)
        {
            return false;
        }

        // Swap-remove: move last dense element into the removed slot
        var last = _dense.Last();
        _dense[denseIndex] = last;
        _denseIndices[last.SparseIndex] = denseIndex;

        // Remove the last slot and clear sparse tracking
        _dense.RemoveAt(_dense.Count - 1);
        _denseIndices[index] = -1;
        _sparse[index] = null;

        return true;
    }

    public void Clear()
    {
        foreach (var (SparseIndex, _) in _dense)
        {
            _sparse[SparseIndex] = null;
            _denseIndices[SparseIndex] = -1;
        }
        _dense.Clear();
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<(uint Index, T Value)> IEnumerable<(uint Index, T Value)>.GetEnumerator()
    {
        return _dense.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _dense.GetEnumerator();
    }

    public struct Enumerator
    {
        private readonly List<(uint SparseIndex, T Value)> _dense;
        private int _index;

        internal Enumerator(SparseReferenceArray<T> array)
        {
            _dense = array._dense;
            _index = -1;
        }

        public readonly (uint Index, T Value) Current => _dense[_index];

        public bool MoveNext()
        {
            _index++;
            return _index < _dense.Count;
        }
    }
}
