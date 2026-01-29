namespace Atomic.Net.MonoGame.Core;

public class FluentHashSet<T>(
    int capacity, IEqualityComparer<T>? comparer
) : HashSet<T>(capacity, comparer)
{
    public FluentHashSet<T> With(T item)
    {
        Add(item);
        return this;
    }

    public FluentHashSet<T> Without(T item)
    {
        Remove(item);
        return this;
    }
}
