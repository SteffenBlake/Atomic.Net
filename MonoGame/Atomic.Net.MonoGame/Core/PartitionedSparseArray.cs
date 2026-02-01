using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Wrapper over two SparseArray instances: one for global partition, one for scene partition.
/// Routes access based on PartitionIndex variant type.
/// </summary>
/// <typeparam name="T">The struct type stored in the arrays.</typeparam>
public sealed class PartitionedSparseArray<T>(ushort globalCapacity, uint sceneCapacity)
    where T : struct
{
    /// <summary>
    /// Global partition array (ushort indices 0-255 by default).
    /// </summary>
    public readonly SparseArray<T> Global = new(globalCapacity);

    /// <summary>
    /// Scene partition array (uint indices 0-8191 by default).
    /// </summary>
    public readonly SparseArray<T> Scene = new((int)sceneCapacity);

    /// <summary>
    /// Indexer that routes to the correct partition based on PartitionIndex.
    /// </summary>
    public T this[PartitionIndex index]
    {
        get
        {
            if (index.TryMatch(out ushort? global))
            {
                return Global[global.Value];
            }
            if (index.TryMatch(out uint? scene))
            {
                return Scene[(ushort)scene.Value];
            }
            throw new InvalidOperationException("Invalid PartitionIndex state");
        }
    }

    /// <summary>
    /// Sets a value at the given partition index.
    /// </summary>
    public void Set(PartitionIndex index, T value)
    {
        if (index.TryMatch(out ushort? global))
        {
            Global.Set(global.Value, value);
            return;
        }
        if (index.TryMatch(out uint? scene))
        {
            Scene.Set((ushort)scene.Value, value);
            return;
        }
        throw new InvalidOperationException("Invalid PartitionIndex state");
    }

    /// <summary>
    /// Gets a mutable reference to a value at the given partition index.
    /// </summary>
    public SparseRef<T> GetMut(PartitionIndex index)
    {
        if (index.TryMatch(out ushort? global))
        {
            return Global.GetMut(global.Value);
        }
        if (index.TryMatch(out uint? scene))
        {
            return Scene.GetMut((ushort)scene.Value);
        }
        throw new InvalidOperationException("Invalid PartitionIndex state");
    }

    /// <summary>
    /// Removes a value at the given partition index.
    /// </summary>
    public bool Remove(PartitionIndex index)
    {
        if (index.TryMatch(out ushort? global))
        {
            return Global.Remove(global.Value);
        }
        if (index.TryMatch(out uint? scene))
        {
            return Scene.Remove((ushort)scene.Value);
        }
        throw new InvalidOperationException("Invalid PartitionIndex state");
    }

    /// <summary>
    /// Tries to get a value at the given partition index.
    /// </summary>
    public bool TryGetValue(
        PartitionIndex index,
        [NotNullWhen(true)] out T? value
    )
    {
        if (index.TryMatch(out ushort? global))
        {
            return Global.TryGetValue(global.Value, out value);
        }
        if (index.TryMatch(out uint? scene))
        {
            return Scene.TryGetValue((ushort)scene.Value, out value);
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Checks if a value exists at the given partition index.
    /// </summary>
    public bool HasValue(PartitionIndex index)
    {
        if (index.TryMatch(out ushort? global))
        {
            return Global.HasValue(global.Value);
        }
        if (index.TryMatch(out uint? scene))
        {
            return Scene.HasValue((ushort)scene.Value);
        }
        return false;
    }
}
