using System.Collections;

namespace Atomic.Net.MonoGame.Core;

public sealed class SparseArray<T>(ushort capacity) : IEnumerable<(ushort Index, T Value)>
{
    private readonly T[] _sparse = new T[capacity];
    private readonly List<(ushort, T)> _dense = new(capacity);

    public ushort Capacity => capacity;

    public T this[ushort index]
    {
        get
        {
            return _sparse[index];
        }
        set
        {
            var oldValue = _sparse[index];
            var i = _dense.IndexOf((index, oldValue));
            if (i >= 0)
            {
                _dense[i] = (index, value);
            }
            else
            {
                _dense.Add((index, value));
            }

            _sparse[index] = value;
        }
    }

    public int Count => _dense.Count;

    public IEnumerator<(ushort Index, T Value)> GetEnumerator()
    {
        return _dense.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
