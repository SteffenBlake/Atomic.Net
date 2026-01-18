namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Provides scoped mutable access to a value in a span.
/// Disposing invokes the provided callback.
/// </summary>
public ref struct SparseRef<T>(Span<T> span, ushort index, SparseArray<T> owner)
    where T : struct
{
    private readonly Span<T> _span = span;
    private readonly ushort _index = index;
    private bool _disposed;

    public readonly ref T Value
    {
        get
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SparseRef<>));
            }
            return ref _span[_index];
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        owner.SyncDense(_index);
    }
}
