namespace Atomic.Net.MonoGame.Core;

public class FluentDictionary<TKey, TValue>(
    int capacity, IEqualityComparer<TKey>? comparer
) : Dictionary<TKey, TValue>(capacity, comparer)
    where TKey : notnull
{
    public FluentDictionary<TKey, TValue> With(TKey key, TValue value)
    {
        this[key] = value;
        return this;
    }

    public FluentDictionary<TKey, TValue> Without(TKey key)
    {
        Remove(key);
        return this;
    }
}

