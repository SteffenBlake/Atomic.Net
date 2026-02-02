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
    public readonly SparseArray<T> Scene = new(sceneCapacity);

    /// <summary>
    /// Indexer that routes to the correct partition based on PartitionIndex.
    /// </summary>
    public T this[PartitionIndex index]
    {
        get
        {
            if (index.TryMatch(out ushort globalIdx))
            {
                return Global[globalIdx];
            }
            if (index.TryMatch(out uint sceneIdx))
            {
                return Scene[sceneIdx];
            }
            throw new InvalidOperationException("Invalid PartitionIndex state");
        }
    }

    /// <summary>
    /// Sets a value at the given partition index.
    /// </summary>
    public void Set(PartitionIndex index, T value)
    {
        if (index.TryMatch(out ushort globalIdx))
        {
            Global.Set(globalIdx, value);
            return;
        }
        if (index.TryMatch(out uint sceneIdx))
        {
            Scene.Set(sceneIdx, value);
            return;
        }
        throw new InvalidOperationException("Invalid PartitionIndex state");
    }

    /// <summary>
    /// Gets a mutable reference to a value at the given partition index.
    /// </summary>
    public SparseRef<T> GetMut(PartitionIndex index)
    {
        if (index.TryMatch(out ushort globalIdx))
        {
            return Global.GetMut(globalIdx);
        }
        if (index.TryMatch(out uint sceneIdx))
        {
            return Scene.GetMut(sceneIdx);
        }
        throw new InvalidOperationException("Invalid PartitionIndex state");
    }

    /// <summary>
    /// Removes a value at the given partition index.
    /// </summary>
    public bool Remove(PartitionIndex index)
    {
        if (index.TryMatch(out ushort globalIdx))
        {
            return Global.Remove(globalIdx);
        }
        if (index.TryMatch(out uint sceneIdx))
        {
            return Scene.Remove(sceneIdx);
        }
        return false;
    }

    /// <summary>
    /// Tries to get a value at the given partition index.
    /// </summary>
    public bool TryGetValue(
        PartitionIndex index,
        [NotNullWhen(true)] out T? value
    )
    {
        if (index.TryMatch(out ushort globalIdx))
        {
            return Global.TryGetValue(globalIdx, out value);
        }
        if (index.TryMatch(out uint sceneIdx))
        {
            return Scene.TryGetValue(sceneIdx, out value);
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Checks if a value exists at the given partition index.
    /// </summary>
    public bool HasValue(PartitionIndex index)
    {
        if (index.TryMatch(out ushort globalIdx))
        {
            return Global.HasValue(globalIdx);
        }
        if (index.TryMatch(out uint sceneIdx))
        {
            return Scene.HasValue(sceneIdx);
        }
        return false;
    }
    
    /// <summary>
    /// Gets the total count of elements across both partitions.
    /// </summary>
    public int Count => Global.Count + Scene.Count;
}
