using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;

namespace Atomic.Net.MonoGame.GreedyPool;

public class GreedyObjectPool<T> : ObjectPool<T>
    where T : class, new()
{
    private readonly ConcurrentQueue<T> _items = new();
    private readonly IPooledObjectPolicy<T> _policy;
    private readonly int _maxCapacity;
    private T? _fastItem;
    private int _numItems;

    public GreedyObjectPool(IPooledObjectPolicy<T> policy, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _maxCapacity = capacity - 1;

        // Preopoulate
        _fastItem = _policy.Create();
        for (int i = 0; i < _maxCapacity; i++)
            _items.Enqueue(_policy.Create());
        _numItems = _maxCapacity;
    }

    public static GreedyObjectPool<T> Default(int maximumCapacity)
    {
        return new GreedyObjectPool<T>(
            new DefaultPooledObjectPolicy<T>(), maximumCapacity
        );
    }

    public override T Get()
    {
        var item = _fastItem;
        if (item != null && Interlocked.CompareExchange(ref _fastItem, null, item) == item)
        {
            return item;
        }

        if (_items.TryDequeue(out item))
        {
            Interlocked.Decrement(ref _numItems);
            return item;
        }

        throw new InvalidOperationException(
            $"GreedyObjectPool<{typeof(T)}> exhausted: no more items available."
        );
    }

    public override void Return(T obj)
    {
        if (!_policy.Return(obj))
        {
            return;
        }

        if (_fastItem == null && Interlocked.CompareExchange(ref _fastItem, obj, null) == null)
        {
            return;
        }

        if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
        {
            _items.Enqueue(obj);
            return;
        }

        Interlocked.Decrement(ref _numItems);
        throw new InvalidOperationException(
            "Pool overflow: returned more items than capacity."
        );
    }
}
