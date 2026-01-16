namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Provides scoped mutable access to a value in a SparseArray.
/// Disposing syncs the sparse value back to the dense array.
/// </summary>
public ref struct SparseRef<T>(SparseArray<T> owner, ushort index, ref T value)
    where T : struct
{
    private readonly SparseArray<T> _owner = owner;
    private readonly ushort _index = index;
    private ref T _value = ref value;
    private bool _disposed;

    public ref T Value
    {
        get
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SparseRef<T>));
            }
            return ref _value;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _owner.SyncDense(_index);
    }
}
