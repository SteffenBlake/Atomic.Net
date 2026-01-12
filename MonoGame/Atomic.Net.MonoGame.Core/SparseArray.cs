namespace Atomic.Net.MonoGame.Core;

public sealed class SparseArray<T>(ushort size)
{
    private readonly ushort _pageSize = (ushort)Math.Floor(Math.Sqrt(size));
    private readonly T?[]?[] _pages =
        new T?[Math.Max(1, (int)Math.Ceiling(size / Math.Floor(Math.Sqrt(size))))][];

    private readonly HashSet<(ushort Page, ushort Offset)> _dense = [];

    private ushort _nextFreePage;
    private ushort _nextFreeOffset;
    private ushort _nextFreeIndex;

    public ushort Size => size;

    public T? this[ushort index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Size, nameof(index));

            var (pageIndex, offset) = GetPageOffset(index);

            var page = _pages[pageIndex];
            if (page == null)
            {
                return default;
            }

            return page[offset];
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Size, nameof(index));

            var (pageIndex, offset) = GetPageOffset(index);

            var page = _pages[pageIndex] ??= new T?[_pageSize];

            var hadValue = page[offset] != null;
            page[offset] = value;

            if (!hadValue && value != null)
            {
                _dense.Add((pageIndex, offset));
            }
            else if (hadValue && value == null)
            {
                _dense.Remove((pageIndex, offset));
            }

            if (index == _nextFreeIndex)
            {
                ScanNextFree();
            }
        }
    }

    public bool Exists(ushort index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Size, nameof(index));

        var (pageIndex, offset) = GetPageOffset(index);

        var page = _pages[pageIndex];
        if (page == null)
        {
            return false;
        }

        return page[offset] != null;
    }

    public void Remove(ushort index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Size, nameof(index));

        var (pageIndex, offset) = GetPageOffset(index);

        var page = _pages[pageIndex];
        if (page == null || page[offset] == null)
        {
            return;
        }

        page[offset] = default;
        _dense.Remove((pageIndex, offset));

        if (index < _nextFreeIndex)
        {
            _nextFreePage = pageIndex;
            _nextFreeOffset = offset;
            _nextFreeIndex = index;
        }
    }

    public int Add(T value)
    {
        var index = _nextFreeIndex;

        _pages[_nextFreePage] ??= new T?[_pageSize];
        _pages[_nextFreePage]![_nextFreeOffset] = value;

        _dense.Add((_nextFreePage, _nextFreeOffset));

        ScanNextFree();

        return index;
    }

    public IEnumerable<(int Index, T value)> Index()
    {
        foreach(var (Page, Offset) in _dense)
        {
            var index = Page * _pageSize + Offset;
            T value = _pages[Page]![Offset]!;
            yield return (index, value);
        }
    }

    private void ScanNextFree()
    {
        for (; _nextFreePage < _pages.Length; _nextFreePage++)
        {
            var page = _pages[_nextFreePage];
            if (page == null)
            {
                _nextFreeIndex = (ushort)(_nextFreePage * _pageSize + _nextFreeOffset);
                return;
            }

            for (; _nextFreeOffset < _pageSize; _nextFreeOffset++)
            {
                if (page[_nextFreeOffset] == null)
                {
                    _nextFreeIndex = (ushort)(_nextFreePage * _pageSize + _nextFreeOffset);
                    return;
                }
            }
            _nextFreeOffset = 0;
        }
    }

    private (ushort Page, ushort Offset) GetPageOffset(ushort index)
    {
        var page = Math.DivRem(index, _pageSize, out var offset);
        return ((ushort)page, (ushort)offset);
    }
}
