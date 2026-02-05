using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Thread-safe sparse array implementation using ReaderWriterLockSlim.
/// Optimized for iteration over set values with O(1) access by index.
/// </summary>
public sealed class SparseArray<T> : IEnumerable<(uint Index, T Value)>
    where T : struct
{
    private readonly T[] _sparse;
    private readonly int[] _denseIndices;
    private readonly List<(uint SparseIndex, T Value)> _dense;
    private readonly ReaderWriterLockSlim _lock = new();

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
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _sparse[index];
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public T[] Values
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _sparse;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
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
            return true;
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
            return _denseIndices[index] >= 0;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Sets a value at the given index.
    /// </summary>
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

    /// <summary>
    /// Gets a mutable reference to a value at the given index.
    /// If the value doesn't exist, it will be created with default(T).
    /// Returns a SparseRef that must be disposed to sync changes back to the dense array.
    /// </summary>
    public SparseRef<T> GetMut(uint index)
    {
        _lock.EnterWriteLock();
        try
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
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Syncs the sparse value at the given index back to the dense array.
    /// </summary>
    internal void SyncDense(uint index)
    {
        _lock.EnterWriteLock();
        try
        {
            var denseIndex = _denseIndices[index];
            if (denseIndex < 0)
            {
                return;
            }
            _dense[denseIndex] = (index, _sparse[index]);
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
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool TryPop(
        [NotNullWhen(true)]
        out T? value,
        [NotNullWhen(true)]
        out uint? index
    )
    {
        _lock.EnterWriteLock();
        try
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
                _sparse[SparseIndex] = default;
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
