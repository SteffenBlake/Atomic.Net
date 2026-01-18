namespace Atomic.Net.MonoGame.Core.Extensions;

public static class ArrayExtensions
{
    public static BackedFloat BackedFor(
        this float[] store, ushort entityIndex
    )
    {
        return new BackedFloat(store, entityIndex);
    }

    public static ReadOnlyBackedFloat ReadOnlyBackedFor(
        this float[] store, ushort entityIndex
    )
    {
        return new ReadOnlyBackedFloat(store, entityIndex);
    }
}

