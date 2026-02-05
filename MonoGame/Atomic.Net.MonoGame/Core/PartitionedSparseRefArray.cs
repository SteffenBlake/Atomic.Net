using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Wrapper over two SparseReferenceArray instances: one for global partition, one for scene partition.
/// Routes access based on PartitionIndex variant type.
/// </summary>
/// <typeparam name="T">The reference type stored in the arrays.</typeparam>
public sealed class PartitionedSparseRefArray<T>(ushort globalCapacity, uint sceneCapacity)
    where T : class
{
    /// <summary>
    /// Global partition array (ushort indices 0-255 by default).
    /// </summary>
    public readonly SparseReferenceArray<T> Global = new(globalCapacity);

    /// <summary>
    /// Scene partition array (uint indices 0-8191 by default).
    /// </summary>
    public readonly SparseReferenceArray<T> Scene = new(sceneCapacity);

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
        set
        {
            if (index.TryMatch(out ushort globalIdx))
            {
                Global[globalIdx] = value;
                return;
            }
            if (index.TryMatch(out uint sceneIdx))
            {
                Scene[sceneIdx] = value;
                return;
            }
            throw new InvalidOperationException("Invalid PartitionIndex state");
        }
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
    /// Gets the total count of elements across both partitions.
    /// </summary>
    public int Count => Global.Count + Scene.Count;
}
