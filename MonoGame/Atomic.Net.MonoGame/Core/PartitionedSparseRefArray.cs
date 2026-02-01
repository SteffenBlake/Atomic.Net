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
    public readonly SparseReferenceArray<T> Scene = new((int)sceneCapacity);

    /// <summary>
    /// Indexer that routes to the correct partition based on PartitionIndex.
    /// </summary>
    public T this[PartitionIndex index]
    {
        get
        {
            return index.Visit(
                global => Global[global],
                scene => Scene[(ushort)scene],
                () => throw new InvalidOperationException("Invalid PartitionIndex state")
            );
        }
        set
        {
            index.Visit(
                global => { Global[global] = value; return 0; },
                scene => { Scene[(ushort)scene] = value; return 0; },
                () => throw new InvalidOperationException("Invalid PartitionIndex state")
            );
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
        if (index.TryMatch(out ushort? globalVal) && globalVal.HasValue)
        {
            ushort global = globalVal.Value;
            return Global.TryGetValue(global, out value);
        }
        if (index.TryMatch(out uint? sceneVal) && sceneVal.HasValue)
        {
            ushort scene = (ushort)sceneVal.Value;
            return Scene.TryGetValue(scene, out value);
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Checks if a value exists at the given partition index.
    /// </summary>
    public bool HasValue(PartitionIndex index)
    {
        return index.Visit(
            global => Global.HasValue(global),
            scene => Scene.HasValue((ushort)scene),
            () => false
        );
    }

    /// <summary>
    /// Removes a value at the given partition index.
    /// </summary>
    public bool Remove(PartitionIndex index)
    {
        return index.Visit(
            global => Global.Remove(global),
            scene => Scene.Remove((ushort)scene),
            () => false
        );
    }
}
