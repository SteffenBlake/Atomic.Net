using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core;

public sealed class SparseArray<T> : IEnumerable<(uint Index, T Value)>
    where T : struct
{
    private readonly T[] _sparse;
    private readonly int[] _denseIndices;
    private readonly List<(uint SparseIndex, T Value)> _dense;

    public uint Capacity { get; }

    public SparseArray(uint capacity)
    {
        Capacity = capacity;
        _sparse = new T[capacity];
        _denseIndices = new int[capacity];
        _dense = new List<(uint, T)>((int)capacity);

        // Initialize all dense indices to -1 (sentinel for "not set")
        Array.Fill(_denseIndices, -1);
    }

    public T this[uint index]
    {
        get => _sparse[index];
    }

    public T[] Values => _sparse;

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
        return true;
    }

    public int Count => _dense.Count;

    public bool HasValue(uint index) => _denseIndices[index] >= 0;

    /// <summary>
    /// Sets a value at the given index.
    /// </summary>
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

    /// <summary>
    /// Gets a mutable reference to a value at the given index.
    /// If the value doesn't exist, it will be created with default(T).
    /// Returns a SparseRef that must be disposed to sync changes back to the dense array.
    /// </summary>
    public SparseRef<T> GetMut(uint index)
    {
        if (_denseIndices[index] < 0)
        {
            var value = default(T);
            _denseIndices[index] = _dense.Count;
            _dense.Add((index, value));
            _sparse[index] = value;
        }

        return new SparseRef<T>(_sparse, index, this);
    }

    /// <summary>
    /// Syncs the sparse value at the given index back to the dense array.
    /// </summary>
    internal void SyncDense(uint index)
    {
        var denseIndex = _denseIndices[index];
        if (denseIndex < 0)
        {
            return;
        }
        _dense[denseIndex] = (index, _sparse[index]);
    }

    public bool Remove(uint index)
    {
        // Lookup where this index lives in the dense array
        var denseIndex = _denseIndices[index];
        if (denseIndex < 0)
        {
            // Not set yet
            return false;
        }

        // Swap-remove: move last dense element into the removed slot
        var last = _dense.Last();
        _dense[denseIndex] = last;
        _denseIndices[last.SparseIndex] = denseIndex;

        // Remove the last slot and clear sparse tracking
        _dense.RemoveAt(_dense.Count - 1);
        _denseIndices[index] = -1;
        _sparse[index] = default;

        return true;
    }

    public bool TryPop(
        [NotNullWhen(true)]
        out T? value,
        [NotNullWhen(true)]
        out uint? index
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
        foreach (var (SparseIndex, _) in _dense)
        {
            _sparse[SparseIndex] = default;
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

    /// <summary>
    /// Struct enumerator to avoid allocation on foreach.
    /// </summary>
    public struct Enumerator
    {
        private readonly List<(uint SparseIndex, T Value)> _dense;
        private int _index;

        internal Enumerator(SparseArray<T> array)
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
