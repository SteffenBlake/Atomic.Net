namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Provides scoped mutable access to a value in a span.
/// Disposing invokes the provided callback.
/// </summary>
public ref struct SparseRef<T>(Span<T> span, int index, Action? onDispose = null)
    where T : struct
{
    private readonly Span<T> _span = span;
    private readonly int _index = index;
    private readonly Action? _onDispose = onDispose;
    private bool _disposed;

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
