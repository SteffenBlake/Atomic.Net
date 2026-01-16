namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Provides scoped mutable access to a value in a span.
/// Disposing invokes the provided callback.
/// </summary>
public ref struct SparseRef<T>(Action? onDispose = null)
    where T : struct
{
    private Span<T> _span;
    private int _index;
    private readonly Action? _onDispose = onDispose;
    private bool _disposed;

    /// <summary>
    /// Initializes the span and index for this ref.
    /// Separate from constructor because Span<T> cannot be used in primary constructors (ref-like type limitation).
    /// </summary>
    public void Initialize(Span<T> span, int index)
    {
        _span = span;
        _index = index;
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
