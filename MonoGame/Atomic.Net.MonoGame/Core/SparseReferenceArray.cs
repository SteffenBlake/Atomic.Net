using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Thread-safe sparse reference array implementation using ReaderWriterLockSlim.
/// For reference types (classes). Optimized for iteration over set values with O(1) access by index.
/// </summary>
public sealed class SparseReferenceArray<T> : IEnumerable<(uint Index, T Value)>
    where T : class
{
    private readonly T?[] _sparse;
    private readonly int[] _denseIndices;
    private readonly List<(uint SparseIndex, T Value)> _dense;
    private readonly ReaderWriterLockSlim _lock = new();

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
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _sparse[index] ?? throw new InvalidOperationException($"No value at index {index}");
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        set => Set(index, value);
    }

    public bool TryGetValue(
        uint index,
        [NotNullWhen(true)]
        out T? value
    )
    {
        _lock.EnterReadLock();
        try
        {
            if (_denseIndices[index] < 0)
            {
                value = null;
                return false;
            }
            value = _sparse[index];
            return value != null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _dense.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public bool HasValue(uint index)
    {
        _lock.EnterReadLock();
        try
        {
            return _denseIndices[index] >= 0 && _sparse[index] != null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Set(uint index, T value)
    {
        _lock.EnterWriteLock();
        try
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
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool Remove(uint index)
    {
        _lock.EnterWriteLock();
        try
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
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var (SparseIndex, _) in _dense)
            {
                _sparse[SparseIndex] = null;
                _denseIndices[SparseIndex] = -1;
            }
            _dense.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
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
    /// Note: For thread safety, prefer LINQ enumeration which uses IEnumerable interface.
    /// This struct enumerator does NOT lock and should only be used when you know the array won't be modified.
    /// </summary>
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
