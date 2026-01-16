namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Provides scoped mutable access to a value in a span.
/// Disposing invokes the provided callback.
/// </summary>
public ref struct SparseRef<T>
    where T : struct
{
    private readonly Span<T> _span;
    private readonly int _index;
    private readonly Action? _onDispose;
    private bool _disposed;

    public SparseRef(Span<T> span, int index, Action? onDispose = null)
    {
        _span = span;
        _index = index;
        _onDispose = onDispose;
        _disposed = false;
    }

    public ref T Value
    {
        get
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SparseRef<T>));
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
        _onDispose?.Invoke();
    }
}
