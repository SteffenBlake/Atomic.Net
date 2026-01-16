using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core;

public sealed class SparseArray<T>(ushort capacity) : IEnumerable<(ushort Index, T Value)>
    where T: struct
{
    private readonly T[] _sparse = new T[capacity];
    private readonly int[] _denseIndices = [.. Enumerable.Repeat(-1, capacity)];
    private readonly List<(ushort SparseIndex, T Value)> _dense = new(capacity);

    public ushort Capacity => capacity;

    public T this[ushort index]
    {
        get
        {
            return _sparse[index];
        }
    }

    public bool TryGetValue(
        ushort index, 
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

    public bool HasValue(ushort index) => _denseIndices[index] >= 0;

    /// <summary>
    /// Sets a value at the given index.
    /// </summary>
    public void Set(ushort index, T value)
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
    /// Ensures a value exists at the given index. If it doesn't exist, initializes it with default(T).
    /// Note: The default value is typically overwritten immediately by the caller.
    /// </summary>
    public void Ensure(ushort index)
    {
        if (_denseIndices[index] >= 0)
        {
            return;
        }
        
        var value = default(T);
        _denseIndices[index] = _dense.Count;
        _dense.Add((index, value));
        _sparse[index] = value;
    }

    /// <summary>
    /// Gets a mutable reference to a value at the given index.
    /// If the value doesn't exist, it will be created with default(T).
    /// Returns a SparseRef that must be disposed to sync changes back to the dense array.
    /// </summary>
    public SparseRef<T> GetMut(ushort index)
    {
        Ensure(index);
        return new SparseRef<T>(this, index, ref _sparse[index]);
    }

    /// <summary>
    /// Syncs the sparse value at the given index back to the dense array.
    /// Called by SparseRef on disposal.
    /// </summary>
    internal void SyncDense(ushort index)
    {
        var denseIndex = _denseIndices[index];
        if (denseIndex < 0)
        {
            return;
        }
        _dense[denseIndex] = (index, _sparse[index]);
    }

    public bool Remove(ushort index)
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

    public void Clear()
    {
        foreach(var (SparseIndex, _) in _dense)
        {
            _sparse[SparseIndex] = default;
            _denseIndices[SparseIndex] = -1;
        }
        _dense.Clear();
    }

    public IEnumerator<(ushort Index, T Value)> GetEnumerator()
    {
        return _dense.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
